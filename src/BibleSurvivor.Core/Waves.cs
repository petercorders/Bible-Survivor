using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    internal static class Guard
    {
        internal static void NonNegative(double value, string name)
        { if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(name); }
    }

    public sealed class SpawnGroup
    {
        public string MonsterId { get; }
        public int Count { get; }
        public int Health { get; }
        public int ContactDamage { get; }
        public SpawnGroup(string monsterId, int count, int health, int contactDamage)
        {
            if (string.IsNullOrWhiteSpace(monsterId) || count <= 0 || health <= 0 || contactDamage <= 0)
                throw new ArgumentException("Invalid spawn group");
            MonsterId = monsterId; Count = count; Health = health; ContactDamage = contactDamage;
        }
    }

    public sealed class WaveDefinition
    {
        public double Start { get; }
        public double End { get; }
        public IReadOnlyList<SpawnGroup> Groups { get; }
        public WaveDefinition(double start, double end, IEnumerable<SpawnGroup> groups)
        {
            Guard.NonNegative(start, nameof(start)); Guard.NonNegative(end, nameof(end));
            var copy = groups.ToArray();
            if (end <= start || end > 170 || copy.Length == 0 || copy.Any(x => x == null))
                throw new ArgumentException("Invalid wave");
            Start = start; End = end; Groups = Array.AsReadOnly(copy);
        }
    }

    public sealed class RoundDefinition
    {
        public int Number { get; }
        public int ConcurrencyCap { get; }
        public int RawExperience { get; }
        public int CompletionGold { get; }
        public IReadOnlyList<WaveDefinition> Waves { get; }
        public RoundDefinition(int number, int concurrencyCap, int rawExperience, int completionGold, IEnumerable<WaveDefinition> waves)
        {
            var copy = waves.ToArray();
            if (number <= 0 || concurrencyCap <= 0 || rawExperience <= 0 || completionGold < 0 || copy.Length == 0)
                throw new ArgumentException("Invalid round");
            var previous = 0.0;
            foreach (var wave in copy) { if (wave.Start != previous) throw new ArgumentException("Noncontiguous waves"); previous = wave.End; }
            Number = number; ConcurrencyCap = concurrencyCap; RawExperience = rawExperience;
            CompletionGold = completionGold; Waves = Array.AsReadOnly(copy);
        }
    }

    [DataContract]
    public sealed class SpawnedEnemy
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string MonsterId { get; set; } = "";
        [DataMember] public double Health { get; set; }
        [DataMember] public int ContactDamage { get; set; }
        [DataMember] public int RawExperience { get; set; }
        internal SpawnedEnemy Copy() => (SpawnedEnemy)MemberwiseClone();
    }

    [DataContract]
    public sealed class WaveSnapshot
    {
        [DataMember] public double Elapsed { get; set; }
        [DataMember] public int Cursor { get; set; }
        [DataMember] public bool Paused { get; set; }
        [DataMember] public bool Expired { get; set; }
        [DataMember] public List<SpawnedEnemy> Active { get; set; } = new List<SpawnedEnemy>();
    }

    public interface IWaveView
    {
        double Elapsed {get;}
        bool Paused {get;}
        int SpawnedCount {get;}
        bool IsNormalComplete {get;}
        IReadOnlyDictionary<int,SpawnedEnemy> Active {get;}
    }

    public sealed class WaveDirector : IWaveView
    {
        private sealed class PlannedSpawn
        {
            internal double At;
            internal SpawnedEnemy Enemy = new SpawnedEnemy();
        }
        private readonly RoundDefinition definition;
        private readonly List<PlannedSpawn> schedule = new List<PlannedSpawn>();
        private readonly Dictionary<int, SpawnedEnemy> active = new Dictionary<int, SpawnedEnemy>();
        private int cursor;
        private bool expired;
        public double Elapsed { get; private set; }
        public bool Paused { get; set; }
        public int SpawnedCount => cursor;
        // Defensive copies: an adapter cannot mutate health or bypass kill accounting.
        public IReadOnlyDictionary<int, SpawnedEnemy> Active => new ReadOnlyDictionary<int, SpawnedEnemy>(active.ToDictionary(x => x.Key, x => x.Value.Copy()));
        public bool IsNormalComplete => Elapsed >= 180 || (cursor == schedule.Count && active.Count == 0);

        public WaveDirector(RoundDefinition definition)
        {
            this.definition = definition;
            foreach (var wave in definition.Waves)
            {
                // Stable, evenly interleaved species; no random draw can change the finite budget.
                var entries = wave.Groups.SelectMany((group, groupIndex) => Enumerable.Range(0, group.Count)
                    .Select(index => new { Group = group, Fraction = (index + 0.5) / group.Count, GroupIndex = groupIndex }))
                    .OrderBy(x => x.Fraction).ThenBy(x => x.GroupIndex).ToArray();
                for (var i = 0; i < entries.Length; i++)
                {
                    var group = entries[i].Group;
                    schedule.Add(new PlannedSpawn { At = wave.Start + (i + 1.0) / entries.Length * (wave.End - wave.Start),
                        Enemy = new SpawnedEnemy { Id = schedule.Count + 1, MonsterId = group.MonsterId, Health = group.Health, ContactDamage = group.ContactDamage } });
                }
            }
            var baseXp = definition.RawExperience / schedule.Count;
            var extra = definition.RawExperience % schedule.Count;
            foreach (var spawn in schedule) spawn.Enemy.RawExperience = baseXp;
            foreach (var spawn in schedule.OrderByDescending(x => x.Enemy.Health).ThenBy(x => x.Enemy.Id).Take(extra)) spawn.Enemy.RawExperience++;
        }

        public void Advance(double seconds)
        {
            Guard.NonNegative(seconds, nameof(seconds));
            if (Paused || IsNormalComplete) return;
            Elapsed = Math.Min(180, Elapsed + seconds);
            if (Elapsed >= 180) { expired = true; active.Clear(); return; }
            // Catch-up represents spawn opportunities in the elapsed interval up to the cutoff.
            while (cursor < schedule.Count && schedule[cursor].At <= Math.Min(Elapsed, 170) + 1e-8 && active.Count < definition.ConcurrencyCap)
            {
                if (expired) break;
                var spawn = schedule[cursor++]; active.Add(spawn.Enemy.Id, spawn.Enemy.Copy());
            }
            if (Elapsed > 170) expired = true;
        }

        public int Kill(int id)
        {
            if (!active.TryGetValue(id, out var enemy)) return 0;
            active.Remove(id); return enemy.RawExperience;
        }

        public int Hit(int id, double damage)
        {
            Guard.NonNegative(damage, nameof(damage));
            if (Paused || IsNormalComplete || !active.TryGetValue(id, out var enemy)) return 0;
            if (damage >= enemy.Health) return Kill(id);
            enemy.Health -= damage; return 0;
        }

        public WaveSnapshot Snapshot() => new WaveSnapshot { Elapsed = Elapsed, Cursor = cursor, Paused = Paused,
            Expired = expired, Active = active.Values.Select(x => x.Copy()).OrderBy(x => x.Id).ToList() };

        public static WaveDirector Restore(RoundDefinition definition, WaveSnapshot state)
        {
            if(state==null) throw new ArgumentNullException(nameof(state));
            var wave = new WaveDirector(definition);
            Guard.NonNegative(state.Elapsed, nameof(state.Elapsed));
            if (state.Elapsed > 180 || state.Cursor < 0 || state.Cursor > wave.schedule.Count || state.Active == null ||
                state.Active.Count > definition.ConcurrencyCap || (state.Elapsed >= 180 && state.Active.Count != 0) ||
                (state.Elapsed > 170 && !state.Expired) || (state.Expired && state.Elapsed <= 170))
                throw new ArgumentException("Invalid wave snapshot");
            foreach (var enemy in state.Active)
            {
                if(enemy==null) throw new ArgumentException("Null active enemy");
                if (enemy.Id <= 0 || enemy.Id > state.Cursor) throw new ArgumentException("Invalid spawned ID");
                var planned = wave.schedule[enemy.Id - 1].Enemy;
                if (double.IsNaN(enemy.Health) || double.IsInfinity(enemy.Health) || enemy.Health <= 0 || enemy.Health > planned.Health || enemy.MonsterId != planned.MonsterId ||
                    enemy.RawExperience != planned.RawExperience || enemy.ContactDamage != planned.ContactDamage)
                    throw new ArgumentException("Invalid active enemy");
                wave.active.Add(enemy.Id, enemy.Copy());
            }
            if (state.Cursor > 0 && wave.schedule[state.Cursor - 1].At > Math.Min(state.Elapsed, 170) + 1e-8)
                throw new ArgumentException("Future spawn cursor");
            wave.Elapsed = state.Elapsed; wave.cursor = state.Cursor; wave.Paused = state.Paused; wave.expired = state.Expired;
            return wave;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    [DataContract]
    public sealed class ExpandedOriginalHitLedger
    {
        [DataMember(EmitDefaultValue = false)] public long LatestCycle { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<long>? HitCycles { get; set; }
        [DataMember(EmitDefaultValue = false)] public int LastTargetId { get; set; }
        [DataMember(EmitDefaultValue = false)] public int ConsecutiveHits { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<BattleHitState>? Marks { get; set; }

        // All active original attacks expire long before 128 minimum-length
        // cycles. Out-of-order collisions still see their own cycle's receipt.
        private const int CycleWindow = 128;
        private const double MarkDuration = 3;
        private const double TimeEpsilon = 1e-9;

        internal bool IsFirst(long cycle) => cycle > 0 && cycle > LatestCycle - CycleWindow &&
            (HitCycles == null || !HitCycles.Contains(cycle));
        internal bool IsMarked(int target, double now) =>
            Marks?.Any(mark => mark.Id == target && now - mark.At <= MarkDuration + TimeEpsilon) == true;

        internal void RecordCycle(long cycle)
        {
            if (cycle <= 0 || cycle <= LatestCycle - CycleWindow) return;
            LatestCycle = Math.Max(LatestCycle, cycle);
            HitCycles ??= new List<long>();
            HitCycles.RemoveAll(previous => previous <= LatestCycle - CycleWindow);
            var index = HitCycles.BinarySearch(cycle);
            if (index < 0) HitCycles.Insert(~index, cycle);
        }

        internal void RecordTarget(int target, double now)
        {
            if (target != LastTargetId)
            {
                LastTargetId = target;
                ConsecutiveHits = 0;
            }
            ConsecutiveHits = Math.Min(3, ConsecutiveHits + 1);
            Marks?.RemoveAll(mark => now - mark.At > MarkDuration + TimeEpsilon);
            if (ConsecutiveHits == 3)
            {
                Marks ??= new List<BattleHitState>();
                var mark = Marks.FirstOrDefault(entry => entry.Id == target);
                if (mark == null) Marks.Add(new BattleHitState { Id = target, At = now });
                else mark.At = now;
                ConsecutiveHits = 0;
            }
        }

        internal void RemoveTarget(int target)
        {
            Marks?.RemoveAll(mark => mark.Id == target);
            if (LastTargetId != target) return;
            LastTargetId = 0;
            ConsecutiveHits = 0;
        }

        internal void Validate(double now)
        {
            if (LatestCycle < 0 || (HitCycles == null && LatestCycle != 0) ||
                HitCycles != null && (HitCycles.Count == 0 || HitCycles.Count > CycleWindow ||
                    HitCycles[^1] != LatestCycle || HitCycles[0] <= 0 || HitCycles[0] <= LatestCycle - CycleWindow ||
                    HitCycles.Zip(HitCycles.Skip(1), (left, right) => left < right).Any(ordered => !ordered)) ||
                ConsecutiveHits < 0 || ConsecutiveHits > 2 || LastTargetId < 0 ||
                LastTargetId == 0 && ConsecutiveHits != 0 ||
                Marks != null && (Marks.Count > 120 || Marks.Any(mark => mark == null || mark.Id <= 0 ||
                    !BattleSnapshot.Finite(mark.At) || mark.At < 0 || mark.At > now + TimeEpsilon) ||
                    Marks.Select(mark => mark.Id).Distinct().Count() != Marks.Count))
                throw new ArgumentException("Invalid original hit history");
        }

        internal ExpandedOriginalHitLedger Copy() => new ExpandedOriginalHitLedger
        {
            LatestCycle = LatestCycle,
            HitCycles = HitCycles?.ToList(),
            LastTargetId = LastTargetId,
            ConsecutiveHits = ConsecutiveHits,
            Marks = Marks?.Select(mark => mark.Copy()).ToList(),
        };
    }
}

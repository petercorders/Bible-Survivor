using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;

public static class CatalogReader
{
    public static RoundDefinition Adam(string file)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(file));
        var root=document.RootElement;
        if(root.GetProperty("schema_version").GetInt32()!=1 || root.GetProperty("chapter_id").GetString()!="genesis")
            throw new InvalidDataException("Unsupported content catalog");
        var rules=root.GetProperty("rules");
        if(rules.GetProperty("normal_duration_seconds").GetInt32()!=180 || rules.GetProperty("spawn_cutoff_seconds").GetInt32()!=170)
            throw new InvalidDataException("Incompatible wave clock");
        var monsters=root.GetProperty("monsters").EnumerateArray().ToDictionary(x=>x.GetProperty("id").GetString()!,x=>x);
        var round=root.GetProperty("rounds").EnumerateArray().Single(x=>x.GetProperty("number").GetInt32()==1);
        var waves=round.GetProperty("waves").EnumerateArray().Select(w=>new WaveDefinition(
            w.GetProperty("start_seconds").GetDouble(),w.GetProperty("end_seconds").GetDouble(),
            w.GetProperty("composition").EnumerateArray().Select(g=> {
                var id=g.GetProperty("monster_id").GetString()!;var monster=monsters[id];
                return new SpawnGroup(id,g.GetProperty("count").GetInt32(),monster.GetProperty("max_health").GetInt32(),monster.GetProperty("contact_damage").GetInt32());
            }))).ToArray();
        if(waves.SelectMany(w=>w.Groups).Sum(x=>x.Count)!=round.GetProperty("spawn_budget").GetInt32())
            throw new InvalidDataException("Round budget mismatch");
        return new RoundDefinition(1,round.GetProperty("concurrency_cap").GetInt32(),
            round.GetProperty("raw_xp_budget").GetInt32(),round.GetProperty("completion_gold").GetInt32(),waves);
    }
}

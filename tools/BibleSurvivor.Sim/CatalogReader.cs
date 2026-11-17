using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;

public static class CatalogReader
{
    public static IReadOnlyDictionary<string,double> MonsterSpeeds(string file)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(file));
        return document.RootElement.GetProperty("monsters").EnumerateArray()
            .ToDictionary(x=>x.GetProperty("id").GetString()!,x=>x.GetProperty("speed_ratio").GetDouble());
    }
    public static RoundDefinition Exodus(string file,int number=10)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(file));
        var root=document.RootElement;
        if(root.GetProperty("schema_version").GetInt32()!=1||root.GetProperty("chapter_id").GetString()!=ExodusContent.ChapterId)
            throw new ArgumentOutOfRangeException(nameof(file),"Unsupported Exodus catalog");
        var rules=root.GetProperty("rules");
        if(rules.GetProperty("normal_duration_seconds").GetInt32()!=180||rules.GetProperty("spawn_cutoff_seconds").GetInt32()!=170)
            throw new InvalidDataException("Incompatible Exodus clock");
        var monsters=root.GetProperty("monsters").EnumerateArray().ToDictionary(x=>x.GetProperty("id").GetString()!,x=>x);
        if(monsters.Count!=6||ExodusContent.SpeedsFor(number).Any(x=>!monsters.TryGetValue(x.Key,out var m)||m.GetProperty("speed_ratio").GetDouble()!=x.Value||m.GetProperty("behavior_id").GetString()!="chase"))
            throw new InvalidDataException("Unsupported Exodus monster contract");
        var round=root.GetProperty("rounds").EnumerateArray().Single(x=>x.GetProperty("number").GetInt32()==number);
        if((number!=10&&number!=11)||round.GetProperty("local_number").GetInt32()!=number-9||round.GetProperty("boss_id").ValueKind!=JsonValueKind.Null||round.GetProperty("spawn_budget").GetInt32()!=(number==10?900:960))
            throw new InvalidDataException("Unsupported Exodus identity");
        var waves=round.GetProperty("waves").EnumerateArray().Select(w=>new WaveDefinition(w.GetProperty("start_seconds").GetDouble(),w.GetProperty("end_seconds").GetDouble(),
            w.GetProperty("composition").EnumerateArray().Select(g=>{var id=g.GetProperty("monster_id").GetString()!;var monster=monsters[id];return new SpawnGroup(id,g.GetProperty("count").GetInt32(),monster.GetProperty("max_health").GetInt32(),monster.GetProperty("contact_damage").GetInt32());}))).ToArray();
        var result=new RoundDefinition(number,round.GetProperty("concurrency_cap").GetInt32(),round.GetProperty("raw_xp_budget").GetInt32(),round.GetProperty("completion_gold").GetInt32(),waves,round.GetProperty("id").GetString(),round.GetProperty("unlock_weapon_id").GetString());
        ExodusContent.Validate(result);return result;
    }
    public static RoundDefinition Adam(string file) => Round(file,1);
    public static RoundDefinition Round(string file,int number)
    {
        if(number==10||number==11) return Exodus(file,number);
        if(number<1 || number>9) throw new ArgumentOutOfRangeException(nameof(number),"Only R01 through R09 core is implemented");
        using var document=JsonDocument.Parse(File.ReadAllText(file));
        var root=document.RootElement;
        if(root.GetProperty("schema_version").GetInt32()!=1 || root.GetProperty("chapter_id").GetString()!="genesis")
            throw new InvalidDataException("Unsupported content catalog");
        var rules=root.GetProperty("rules");
        if(rules.GetProperty("normal_duration_seconds").GetInt32()!=180 || rules.GetProperty("spawn_cutoff_seconds").GetInt32()!=170)
            throw new InvalidDataException("Incompatible wave clock");
        var monsters=root.GetProperty("monsters").EnumerateArray().ToDictionary(x=>x.GetProperty("id").GetString()!,x=>x);
        var round=root.GetProperty("rounds").EnumerateArray().Single(x=>x.GetProperty("number").GetInt32()==number);
        if(round.GetProperty("id").GetString()!="genesis_"+number.ToString("D2") || ((number==3||number==6||number==9) ? round.GetProperty("boss_id").GetString()!=(number==3?"nephilim_tyrant":number==6?"sitnah_keeper":"silver_cup_accuser") : round.GetProperty("boss_id").ValueKind!=JsonValueKind.Null))
            throw new InvalidDataException("Incompatible round identity");
        if(number==3)
        {
            var boss=root.GetProperty("bosses").EnumerateArray().Single(x=>x.GetProperty("id").GetString()=="nephilim_tyrant");
            var attack=boss.GetProperty("attack");var support=boss.GetProperty("support_summons").EnumerateArray().Single();
            if(boss.GetProperty("max_health").GetInt32()!=1800||boss.GetProperty("contact_damage").GetInt32()!=12||boss.GetProperty("gold").GetInt32()!=100||
                attack.GetProperty("cooldown_seconds").GetDouble()!=6||attack.GetProperty("telegraph_seconds").GetDouble()!=1.2||
                attack.GetProperty("damage").GetInt32()!=15||attack.GetProperty("projectile_count").GetInt32()!=0||
                attack.GetProperty("pattern_id").GetString()!="telegraphed_club_slam"||support.GetProperty("health_ratio").GetDouble()!=.5||
                support.GetProperty("monster_id").GetString()!="c1"||support.GetProperty("count").GetInt32()!=6||
                rules.GetProperty("boss_intro_seconds").GetInt32()!=2||rules.GetProperty("boss_support_cap").GetInt32()!=12||
                monsters["c1"].GetProperty("max_health").GetInt32()!=30||monsters["c1"].GetProperty("contact_damage").GetInt32()!=6||
                monsters["c1"].GetProperty("speed_ratio").GetDouble()!=.55)
                throw new InvalidDataException("Unsupported first boss contract");
        }
        if(number==4)
        {
            var dash=monsters["d3"].GetProperty("dash");
            if(monsters["d3"].GetProperty("behavior_id").GetString()!="telegraphed_dash"||
                monsters["d1"].GetProperty("speed_ratio").GetDouble()!=.65||monsters["d2"].GetProperty("speed_ratio").GetDouble()!=.5||monsters["d3"].GetProperty("speed_ratio").GetDouble()!=.6||
                dash.GetProperty("telegraph_seconds").GetDouble()!=1.2||dash.GetProperty("speed_ratio").GetDouble()!=1.3||
                dash.GetProperty("duration_seconds").GetDouble()!=.6||dash.GetProperty("cooldown_seconds").GetDouble()!=6)
                throw new InvalidDataException("Unsupported R04 charge contract");
        }
        if(number==5&&(monsters["e1"].GetProperty("behavior_id").GetString()!="chase"||monsters["e2"].GetProperty("behavior_id").GetString()!="slow_chase"||monsters["e3"].GetProperty("behavior_id").GetString()!="fast_chase"||
            monsters["e1"].GetProperty("speed_ratio").GetDouble()!=.6||monsters["e2"].GetProperty("speed_ratio").GetDouble()!=.45||monsters["e3"].GetProperty("speed_ratio").GetDouble()!=.95))throw new InvalidDataException("Unsupported R05 chase contract");
        if(number==6)
        {
            var boss=root.GetProperty("bosses").EnumerateArray().Single(x=>x.GetProperty("id").GetString()=="sitnah_keeper");var attack=boss.GetProperty("attack");
            var summons=boss.GetProperty("support_summons").EnumerateArray().ToArray();
            if(boss.GetProperty("max_health").GetInt32()!=6500||boss.GetProperty("contact_damage").GetInt32()!=16||boss.GetProperty("gold").GetInt32()!=200||
                attack.GetProperty("pattern_id").GetString()!="telegraphed_dirt_throw"||attack.GetProperty("cooldown_seconds").GetDouble()!=7||attack.GetProperty("telegraph_seconds").GetDouble()!=1.2||attack.GetProperty("damage").GetInt32()!=18||attack.GetProperty("projectile_count").GetInt32()!=0||
                summons.Length!=2||summons[0].GetProperty("health_ratio").GetDouble()!=.7||summons[1].GetProperty("health_ratio").GetDouble()!=.35||summons.Any(x=>x.GetProperty("monster_id").GetString()!="f1"||x.GetProperty("count").GetInt32()!=6)||
                monsters["f1"].GetProperty("max_health").GetInt32()!=80||monsters["f1"].GetProperty("contact_damage").GetInt32()!=8||monsters["f1"].GetProperty("speed_ratio").GetDouble()!=.7||monsters["f2"].GetProperty("speed_ratio").GetDouble()!=.5||monsters["f3"].GetProperty("speed_ratio").GetDouble()!=.35)
                throw new InvalidDataException("Unsupported R06 boss contract");
        }
        if(number==7&&(monsters["g1"].GetProperty("behavior_id").GetString()!="chase"||monsters["g2"].GetProperty("behavior_id").GetString()!="chase"||monsters["g3"].GetProperty("behavior_id").GetString()!="two_sided_chase"||monsters["g1"].GetProperty("speed_ratio").GetDouble()!=.65||monsters["g2"].GetProperty("speed_ratio").GetDouble()!=.55||monsters["g3"].GetProperty("speed_ratio").GetDouble()!=.65))throw new InvalidDataException("Unsupported R07 chase contract");
        if(number==8&&(monsters["h1"].GetProperty("behavior_id").GetString()!="chase"||monsters["h2"].GetProperty("behavior_id").GetString()!="slow_chase"||monsters["h3"].GetProperty("behavior_id").GetString()!="chase"||monsters["h1"].GetProperty("speed_ratio").GetDouble()!=.65||monsters["h2"].GetProperty("speed_ratio").GetDouble()!=.4||monsters["h3"].GetProperty("speed_ratio").GetDouble()!=.55))throw new InvalidDataException("Unsupported R08 chase contract");
        if(number==9)
        {
            var boss=root.GetProperty("bosses").EnumerateArray().Single(x=>x.GetProperty("id").GetString()=="silver_cup_accuser");var attack=boss.GetProperty("attack");var summons=boss.GetProperty("support_summons").EnumerateArray().ToArray();
            if(boss.GetProperty("max_health").GetInt32()!=16000||boss.GetProperty("contact_damage").GetInt32()!=20||boss.GetProperty("gold").GetInt32()!=350||attack.GetProperty("pattern_id").GetString()!="three_way_chain_projectiles"||attack.GetProperty("cooldown_seconds").GetDouble()!=5||attack.GetProperty("telegraph_seconds").GetDouble()!=1||attack.GetProperty("damage").GetInt32()!=15||attack.GetProperty("projectile_count").GetInt32()!=3||summons.Length!=2||summons[0].GetProperty("health_ratio").GetDouble()!=.6||summons[1].GetProperty("health_ratio").GetDouble()!=.3||summons.Any(x=>x.GetProperty("monster_id").GetString()!="i2"||x.GetProperty("count").GetInt32()!=6)||
                monsters["i1"].GetProperty("behavior_id").GetString()!="fast_chase"||monsters["i2"].GetProperty("behavior_id").GetString()!="chase"||monsters["i3"].GetProperty("behavior_id").GetString()!="slow_chase"||monsters["i1"].GetProperty("speed_ratio").GetDouble()!=.85||monsters["i2"].GetProperty("speed_ratio").GetDouble()!=.6||monsters["i3"].GetProperty("speed_ratio").GetDouble()!=.35)throw new InvalidDataException("Unsupported R09 chain contract");
        }
        var waves=round.GetProperty("waves").EnumerateArray().Select(w=>new WaveDefinition(
            w.GetProperty("start_seconds").GetDouble(),w.GetProperty("end_seconds").GetDouble(),
            w.GetProperty("composition").EnumerateArray().Select(g=> {
                var id=g.GetProperty("monster_id").GetString()!;var monster=monsters[id];
                return new SpawnGroup(id,g.GetProperty("count").GetInt32(),monster.GetProperty("max_health").GetInt32(),monster.GetProperty("contact_damage").GetInt32());
            }))).ToArray();
        if(waves.SelectMany(w=>w.Groups).Sum(x=>x.Count)!=round.GetProperty("spawn_budget").GetInt32())
            throw new InvalidDataException("Round budget mismatch");
        return new RoundDefinition(number,round.GetProperty("concurrency_cap").GetInt32(),
            round.GetProperty("raw_xp_budget").GetInt32(),round.GetProperty("completion_gold").GetInt32(),waves,
            round.GetProperty("id").GetString(),round.GetProperty("unlock_weapon_id").GetString());
    }
}

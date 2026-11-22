using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace BibleSurvivor.Core.Tests;

internal static class Program
{
    private static int Main(string[] args)
    {
        var results = new List<(string Name, Exception? Error)>();
        void Test(string name, Action action)
        {
            if(args.Contains("--r60-only")&&!name.StartsWith("R60 ",StringComparison.Ordinal))return;
            if(args.Contains("--r50-only")&&!name.StartsWith("R50 ",StringComparison.Ordinal))return;
            if(args.Contains("--r40-only")&&!name.StartsWith("R40 ",StringComparison.Ordinal))return;
            if(args.Contains("--r30-only")&&!name.StartsWith("R30 ",StringComparison.Ordinal))return;
            if(args.Contains("--r20-only")&&!name.StartsWith("R20 ",StringComparison.Ordinal))return;
            if(args.Contains("--r16-only")&&!name.StartsWith("R16 ",StringComparison.Ordinal))return;
            if(args.Contains("--r15-only")&&!name.StartsWith("R15 ",StringComparison.Ordinal))return;
            if(args.Contains("--r14-only")&&!name.StartsWith("R14 ",StringComparison.Ordinal))return;
            if(args.Contains("--r13-only")&&!name.StartsWith("R13 ",StringComparison.Ordinal))return;
            if(args.Contains("--r12-only")&&!name.StartsWith("R12 ",StringComparison.Ordinal))return;
            if(args.Contains("--r11-only")&&!name.StartsWith("R11 ",StringComparison.Ordinal))return;
            if(args.Contains("--r11-related")&&!(name.StartsWith("R10 ",StringComparison.Ordinal)||name.StartsWith("R11 ",StringComparison.Ordinal)))return;
            if(args.Contains("--r10-only")&&!name.StartsWith("R10 ",StringComparison.Ordinal))return;
            if(args.Contains("--r09-related")&&!(name.StartsWith("R09 ",StringComparison.Ordinal)||name.StartsWith("R06 ",StringComparison.Ordinal)||name.StartsWith("R08 ",StringComparison.Ordinal)||name=="Schema2 reader preserves future boss content instead of recovering older normal backup"||name=="Schema1 R01 and R02 active and between checkpoints resume without fingerprint or economy changes"))return;
            if(args.Contains("--r09-only")&&!name.StartsWith("R09 ",StringComparison.Ordinal))return;
            if(args.Contains("--r08-related")&&!(name.StartsWith("R08 ",StringComparison.Ordinal)||name.StartsWith("R07 ",StringComparison.Ordinal)||name=="Schema2 reader preserves future boss content instead of recovering older normal backup"||name=="Schema1 R01 and R02 active and between checkpoints resume without fingerprint or economy changes"))return;
            if(args.Contains("--r08-only")&&!name.StartsWith("R08 ",StringComparison.Ordinal))return;
            if(args.Contains("--r07-related")&&!(name.StartsWith("R07 ",StringComparison.Ordinal)||name.StartsWith("R06 ",StringComparison.Ordinal)||name.StartsWith("Schema",StringComparison.Ordinal)||name.StartsWith("Round ",StringComparison.Ordinal)))return;
            if(args.Contains("--r07-only")&&!name.StartsWith("R07 ",StringComparison.Ordinal))return;
            if(args.Contains("--r06-only")&&!name.StartsWith("R06 ",StringComparison.Ordinal))return;
            if(args.Contains("--r05-only")&&!name.StartsWith("R05 ",StringComparison.Ordinal))return;
            if(args.Contains("--r04-only")&&!name.StartsWith("R04 ",StringComparison.Ordinal))return;
            try { action(); results.Add((name, null)); }
            catch (Exception error) { results.Add((name, error)); Console.Error.WriteLine($"FAIL {name}: {error}"); }
        }
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))
        {
        WaveTests.Register(Test);
        GrowthTests.Register(Test);
        CombatTests.Register(Test);
        SaveTests.Register(Test);
        SaveSchemaTests.Register(Test);
        BattleWorldTests.Register(Test);
        ImpactTests.Register(Test);
        BattleSaveTests.Register(Test);
        RoundTransitionTests.Register(Test);
        }
        if(!args.Contains("--reward-only"))BossTests.Register(Test);
        if(!args.Contains("--reward-only"))BossActionTests.Register(Test);
        if(!args.Contains("--boss-only"))RewardEvolutionTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundFourTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundFiveTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundSixTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundSevenTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundEightTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundNineTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundTenTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundElevenTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundTwelveTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundThirteenTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundFourteenTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundFifteenTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))RoundSixteenTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))LeviticusSeriesTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignThirtyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignFortyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignFiftyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignSixtyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignSeventyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignEightyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignNinetyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignHundredTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignHundredTenTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignHundredTwentyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignHundredThirtyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignHundredFortyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignHundredFiftyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignHundredSixtyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignHundredSeventyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))CampaignHundredEightyTests.Register(Test);
        if(!args.Contains("--boss-only")&&!args.Contains("--reward-only"))NaturalGrowthTrialTests.Register(Test);
        var failed = results.Count(r => r.Error != null);
        Console.WriteLine($"Core checks: {results.Count - failed}/{results.Count} passed, {failed} failed");
        if (args.Length == 2 && args[0] == "--results")
        {
            Directory.CreateDirectory(args[1]);
            var summary = new { status = failed == 0 ? "PASS" : "FAIL", total = results.Count,
                passed = results.Count - failed, failed, unity = "NOT_RUN", mobile = "NOT_RUN" };
            File.WriteAllText(Path.Combine(args[1], "core-summary.json"), JsonSerializer.Serialize(summary));
            var suite = new XElement("testsuite", new XAttribute("tests", results.Count), new XAttribute("failures", failed),
                results.Select(r => new XElement("testcase", new XAttribute("name", r.Name),
                    r.Error == null ? null : new XElement("failure", r.Error.ToString()))));
            suite.Save(Path.Combine(args[1], "core-tests.xml"));
        }
        return failed == 0 && results.Count > 0 ? 0 : 1;
    }
}

internal static class Check
{
    public static void True(bool value) { if (!value) throw new Exception("Expected true"); }
    public static void Equal<T>(T expected, T actual)
    { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, actual {actual}"); }
    public static void Near(double expected, double actual, double tolerance = 1e-8)
    { if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance) throw new Exception($"Expected {expected}, actual {actual}"); }
    public static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
}

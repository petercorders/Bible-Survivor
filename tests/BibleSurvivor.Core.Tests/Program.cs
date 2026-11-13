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
            try { action(); results.Add((name, null)); }
            catch (Exception error) { results.Add((name, error)); Console.Error.WriteLine($"FAIL {name}: {error}"); }
        }
        WaveTests.Register(Test);
        GrowthTests.Register(Test);
        CombatTests.Register(Test);
        SaveTests.Register(Test);
        BattleWorldTests.Register(Test);
        BattleSaveTests.Register(Test);
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

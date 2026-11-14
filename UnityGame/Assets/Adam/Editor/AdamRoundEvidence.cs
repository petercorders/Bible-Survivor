using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace BibleSurvivor.Unity.Editor
{
    public static class AdamRoundEvidence
    {
        public static void ValidateIsolation(string project,string output,string run)
        {
            if(!File.Exists(Path.Combine(project,".adam-rounds-qa")))throw new InvalidOperationException("Requires isolated product rounds QA project");
            if(run==null || !Regex.IsMatch(run,"\\A[a-z][a-z0-9]{5,23}\\z"))throw new InvalidOperationException("Requires unique lowercase alphanumeric QA run suffix (6-24 characters)");
            if(string.IsNullOrEmpty(output) || !Path.IsPathRooted(output) || !output.EndsWith(".app",StringComparison.OrdinalIgnoreCase) || Directory.Exists(output) || File.Exists(output))throw new InvalidOperationException("Requires fresh absolute QA app output");
        }
        public static void BuildMac()
        {
            var project=Path.GetDirectoryName(Application.dataPath);
            var args=Environment.GetCommandLineArgs();var i=Array.IndexOf(args,"-adamRoundsOutput");var j=Array.IndexOf(args,"-adamRoundsRun");
            var output=i>=0 && i+1<args.Length?args[i+1]:null;var run=j>=0 && j+1<args.Length?args[j+1]:null;
            ValidateIsolation(project,output,run);
            AdamArtImporter.ValidateBuild();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.biblesurvivor.adam.roundsqa."+run);
            PlayerSettings.companyName="BibleSurvivorQA";PlayerSettings.productName="Adam R02 QA "+run;PlayerSettings.defaultScreenWidth=1100;PlayerSettings.defaultScreenHeight=720;PlayerSettings.resizableWindow=true;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var gate=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var type=Type.GetType("RoundPathGate, Assembly-CSharp",true);var component=new GameObject("No-save QA path gate").AddComponent(type);type.GetField("Destination").SetValue(component,"Adam");type.GetField("RunSuffix").SetValue(component,run);EditorSceneManager.SaveScene(gate,"Assets/RoundsPathGate.unity");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Adam.unity",OpenSceneMode.Single);
            var driver=Type.GetType("RoundProductCapture, Assembly-CSharp",true);
            if(UnityEngine.Object.FindObjectsByType(driver,FindObjectsSortMode.None).Length!=0)throw new InvalidOperationException("Fresh QA scene required; duplicate evidence driver forbidden");
            new GameObject("Optional deterministic product evidence").AddComponent(driver);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new[]{"Assets/RoundsPathGate.unity","Assets/Scenes/Adam.unity"},output,BuildTarget.StandaloneOSX,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("R02 QA build failed");
        }
    }
}

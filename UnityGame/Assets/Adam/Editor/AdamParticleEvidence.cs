using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace BibleSurvivor.Unity.Editor
{
    public static class AdamParticleEvidence
    {
        public static void ValidateIsolation(string project,string output,string run)
        {
            if(!File.Exists(Path.Combine(project,".adam-particles-qa")))throw new InvalidOperationException("Requires isolated product particles QA project");
            if(run==null || !Regex.IsMatch(run,"\\A[a-z][a-z0-9]{5,23}\\z"))throw new InvalidOperationException("Requires unique lowercase alphanumeric QA run suffix (6-24 characters)");
            if(string.IsNullOrEmpty(output) || !Path.IsPathRooted(output) || !output.EndsWith(".app",StringComparison.OrdinalIgnoreCase) || Directory.Exists(output) || File.Exists(output))throw new InvalidOperationException("Requires fresh absolute QA app output");
        }
        public static void BuildMac()
        {
            var project=Path.GetDirectoryName(Application.dataPath);
            var args=Environment.GetCommandLineArgs();var i=Array.IndexOf(args,"-adamParticlesOutput");var j=Array.IndexOf(args,"-adamParticlesRun");
            var output=i>=0 && i+1<args.Length?args[i+1]:null;var run=j>=0 && j+1<args.Length?args[j+1]:null;
            ValidateIsolation(project,output,run);
            AdamArtImporter.ValidateBuild();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.biblesurvivor.adam.particlesqa."+run);
            PlayerSettings.companyName="BibleSurvivorQA";PlayerSettings.productName="Adam Particles QA "+run;PlayerSettings.defaultScreenWidth=1100;PlayerSettings.defaultScreenHeight=720;PlayerSettings.resizableWindow=true;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var gate=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var type=Type.GetType("ProductParticlesPathGate, Assembly-CSharp",true);var component=new GameObject("No-save QA path gate").AddComponent(type);type.GetField("Destination").SetValue(component,"Adam");type.GetField("RunSuffix").SetValue(component,run);EditorSceneManager.SaveScene(gate,"Assets/ParticlesPathGate.unity");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Adam.unity",OpenSceneMode.Single);
            var driver=Type.GetType("ParticleProductCapture, Assembly-CSharp",true);new GameObject("Optional deterministic product evidence").AddComponent(driver);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new[]{"Assets/ParticlesPathGate.unity","Assets/Scenes/Adam.unity"},output,BuildTarget.StandaloneOSX,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Product particle QA build failed");
        }
    }
}

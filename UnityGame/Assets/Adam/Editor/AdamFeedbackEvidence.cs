using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BibleSurvivor.Unity.Editor
{
    public static class AdamFeedbackEvidence
    {
        public static void BuildGameMac()
        {
            var project=Path.GetDirectoryName(Application.dataPath);
            if(!File.Exists(Path.Combine(project,".adam-feedback-qa")))throw new InvalidOperationException("Requires isolated feedback QA copy");
            var args=Environment.GetCommandLineArgs();var i=Array.IndexOf(args,"-adamFeedbackOutput");
            if(i<0 || i+1>=args.Length || !Path.IsPathRooted(args[i+1]) || !args[i+1].EndsWith(".app",StringComparison.OrdinalIgnoreCase) || Directory.Exists(args[i+1]) || File.Exists(args[i+1]))throw new InvalidOperationException("Requires fresh absolute QA game output");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.biblesurvivor.adam.combatfeedbackqa");
            AdamArtImporter.ValidateBuild();PlayerSettings.companyName="BibleSurvivorQA";PlayerSettings.productName="Adam Combat QA Game";
            PlayerSettings.defaultScreenWidth=1100;PlayerSettings.defaultScreenHeight=720;PlayerSettings.resizableWindow=true;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var report=BuildPipeline.BuildPlayer(new[]{PathGate("Adam"),"Assets/Scenes/Adam.unity"},args[i+1],BuildTarget.StandaloneOSX,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Feedback game build failed");
        }
        static string PathGate(string destination)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var type=Type.GetType("FeedbackPathGate, Assembly-CSharp",true);
            var gate=new GameObject("QA save path gate").AddComponent(type);
            type.GetField("Destination").SetValue(gate,destination);
            const string path="Assets/FeedbackPathGate.unity";
            EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();return path;
        }
        // Separate project copy only: no user save path, canonical scene or platform settings are used.
        public static void BuildQaMac()
        {
            var project=Path.GetDirectoryName(Application.dataPath);
            if(!File.Exists(Path.Combine(project,".adam-feedback-qa")))throw new InvalidOperationException("Requires isolated feedback QA copy");
            var args=Environment.GetCommandLineArgs();var i=Array.IndexOf(args,"-adamFeedbackOutput");
            if(i<0 || i+1>=args.Length || !Path.IsPathRooted(args[i+1]) || !args[i+1].EndsWith(".app",StringComparison.OrdinalIgnoreCase) || Directory.Exists(args[i+1]) || File.Exists(args[i+1]))throw new InvalidOperationException("Requires fresh absolute QA app output");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.biblesurvivor.adam.combatfeedbackqa");
            AdamArtImporter.ValidateBuild();
            PlayerSettings.companyName="BibleSurvivorQA";PlayerSettings.productName="Adam Combat QA";
            PlayerSettings.defaultScreenWidth=1100;PlayerSettings.defaultScreenHeight=720;PlayerSettings.resizableWindow=true;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("QA Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.09f,.1f);
            var type=Type.GetType("FeedbackCapture, Assembly-CSharp",true);
            new GameObject("QA scripted capture").AddComponent(type);
            EditorSceneManager.SaveScene(scene,"Assets/FeedbackQa.unity");AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new[]{PathGate("FeedbackQa"),"Assets/FeedbackQa.unity"},args[i+1],BuildTarget.StandaloneOSX,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Feedback QA build failed");
        }
    }
}

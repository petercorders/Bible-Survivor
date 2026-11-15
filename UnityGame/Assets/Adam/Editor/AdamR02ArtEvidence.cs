using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace BibleSurvivor.Unity.Editor
{
    public static class AdamR02ArtEvidence
    {
        public static void ValidateIsolation(string project,string output,string run)
        {
            if(!File.Exists(Path.Combine(project,".adam-r02-art-qa")))throw new InvalidOperationException("Requires isolated product rounds QA project");
            if(run==null || !Regex.IsMatch(run,"\\A[a-z][a-z0-9]{5,23}\\z"))throw new InvalidOperationException("Requires unique lowercase alphanumeric QA run suffix (6-24 characters)");
            if(string.IsNullOrEmpty(output) || !Path.IsPathRooted(output) || !output.EndsWith(".app",StringComparison.OrdinalIgnoreCase) || Directory.Exists(output) || File.Exists(output))throw new InvalidOperationException("Requires fresh absolute QA app output");
        }
        [Serializable] sealed class ImportEvidence{public string[] before,after;}
        static string[] Registration()=>new AdamR02Art().Index.assets.SelectMany(entry=>AssetDatabase.LoadAllAssetsAtPath(AdamR02ArtImporter.Root+entry.clip.id+".png").OfType<Sprite>().Select(sprite=>{
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long local);
            return sprite.name+"|"+guid+"|"+local+"|"+sprite.rect+"|"+sprite.pivot+"|"+sprite.border+"|"+sprite.pixelsPerUnit;
        })).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        public static void BuildMac()
        {
            var project=Path.GetDirectoryName(Application.dataPath);
            var args=Environment.GetCommandLineArgs();var i=Array.IndexOf(args,"-adamR02ArtOutput");var j=Array.IndexOf(args,"-adamR02ArtRun");
            var output=i>=0 && i+1<args.Length?args[i+1]:null;var run=j>=0 && j+1<args.Length?args[j+1]:null;
            ValidateIsolation(project,output,run);
            AdamArtImporter.ValidateBuild();AdamR02ArtImporter.ValidateBuild();
            var before=Registration();
            foreach(var entry in new AdamR02Art().Index.assets)AssetDatabase.ImportAsset(AdamR02ArtImporter.Root+entry.clip.id+".png",ImportAssetOptions.ForceUpdate);
            var after=Registration();if(!before.SequenceEqual(after))throw new InvalidOperationException("R02 reimport changed sprite GUID/registration");
            var evidence=Path.Combine(Path.GetDirectoryName(output),"registration.json");Directory.CreateDirectory(Path.GetDirectoryName(evidence));
            if(File.Exists(evidence))throw new InvalidOperationException("Fresh registration output required");
            File.WriteAllText(evidence,JsonUtility.ToJson(new ImportEvidence{before=before,after=after},true));
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.biblesurvivor.adam.r02artqa."+run);
            PlayerSettings.companyName="BibleSurvivorQA";PlayerSettings.productName="Adam R02 Art QA "+run;PlayerSettings.defaultScreenWidth=1100;PlayerSettings.defaultScreenHeight=720;PlayerSettings.resizableWindow=true;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var gate=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var type=Type.GetType("R02ArtPathGate, Assembly-CSharp",true);var component=new GameObject("No-save QA path gate").AddComponent(type);type.GetField("Destination").SetValue(component,"Adam");type.GetField("RunSuffix").SetValue(component,run);EditorSceneManager.SaveScene(gate,"Assets/R02ArtPathGate.unity");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Adam.unity",OpenSceneMode.Single);
            var driver=Type.GetType("R02ArtCapture, Assembly-CSharp",true);
            if(UnityEngine.Object.FindObjectsByType(driver,FindObjectsSortMode.None).Length!=0)throw new InvalidOperationException("Fresh QA scene required; duplicate evidence driver forbidden");
            new GameObject("Optional deterministic product evidence").AddComponent(driver);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new[]{"Assets/R02ArtPathGate.unity","Assets/Scenes/Adam.unity"},output,BuildTarget.StandaloneOSX,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("R02 QA build failed");
        }
    }
}

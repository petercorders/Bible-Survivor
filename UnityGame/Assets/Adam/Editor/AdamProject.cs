using UnityEditor;
using UnityEditor.SceneManagement;

namespace BibleSurvivor.Unity.Editor
{
    public static class AdamProject
    {
        [MenuItem("Bible Survivor/Prepare Adam Scene")]
        public static void Prepare()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new UnityEngine.GameObject("Camera").AddComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
            camera.backgroundColor = new UnityEngine.Color(.06f, .09f, .1f);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Adam.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Adam.unity", true) };
            AssetDatabase.SaveAssets();
        }
        public static void BuildMac()
        {
            AdamArtImporter.ValidateBuild();
            PlayerSettings.companyName = "BibleSurvivor";
            PlayerSettings.productName = "Adam Prototype";
            PlayerSettings.defaultScreenWidth = 1100;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = UnityEngine.FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            AssetDatabase.SaveAssets();
            var report = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/Adam.unity" }, "Builds/Adam.app", BuildTarget.StandaloneOSX, BuildOptions.Development);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Adam player build failed: " + report.summary.result);
            // Keep the unmodified OFL notice accessible beside the distributed player resources too.
            var resources=System.IO.Path.Combine(report.summary.outputPath,"Contents/Resources");
            System.IO.File.Copy("Assets/Adam/Resources/ArtFonts/LICENSE.txt",System.IO.Path.Combine(resources,"Galmuri-LICENSE.txt"),true);
            System.IO.File.Copy("Assets/Adam/Resources/ArtFonts/font-manifest.json",System.IO.Path.Combine(resources,"Galmuri-font-manifest.json"),true);
        }
    }
}

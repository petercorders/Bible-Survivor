using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BibleSurvivor.Unity.Editor
{
    // Simulator-only QA export. Never change canonical platform settings or use signing identities.
    public static class AdamMobileBuild
    {
        public static void BuildIosSimulator()
        {
            var project = Path.GetDirectoryName(Application.dataPath);
            if (!File.Exists(Path.Combine(project, ".adam-mobile-qa")))
                throw new InvalidOperationException("iOS export requires an isolated .adam-mobile-qa project copy");
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-adamIosOutput");
            if (index < 0 || index + 1 >= args.Length || !Path.IsPathRooted(args[index + 1]))
                throw new InvalidOperationException("Specify an absolute -adamIosOutput QA directory");
            var output = Path.GetFullPath(args[index + 1]);
            if (Directory.Exists(output)) throw new InvalidOperationException("Use a fresh iOS output directory");
            AdamArtImporter.ValidateBuild();
            PlayerSettings.companyName = "BibleSurvivorQA";
            PlayerSettings.productName = "Adam Simulator QA";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.biblesurvivor.adam.simqa");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.iOS.appleDeveloperTeamID = "";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            Debug.Log("ADAM_IOS_BUILD stripping=" + PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.iOS) + " backend=IL2CPP sdk=" + PlayerSettings.iOS.sdkVersion + " arch=" + PlayerSettings.iOS.simulatorSdkArchitecture);
            AssetDatabase.SaveAssets();
            var report = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/Adam.unity" }, output,
                BuildTarget.iOS, BuildOptions.Development);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Adam iOS Simulator export failed: " + report.summary.result);
            File.Copy("Assets/Adam/Resources/ArtFonts/LICENSE.txt", Path.Combine(output, "Galmuri-LICENSE.txt"));
            File.Copy("Assets/Adam/Resources/ArtFonts/font-manifest.json", Path.Combine(output, "Galmuri-font-manifest.json"));
        }
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace BibleSurvivor.Unity.Editor
{
    // Local emulator QA only. Platform settings and the dedicated debug key live in the isolated copy.
    public static class AdamAndroidBuild
    {
        public static void BuildQaApk()
        {
            var project = Path.GetDirectoryName(Application.dataPath);
            if (!File.Exists(Path.Combine(project, ".adam-mobile-qa")))
                throw new InvalidOperationException("Android export requires an isolated .adam-mobile-qa project copy");
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-adamAndroidOutput");
            if (index < 0 || index + 1 >= args.Length || !Path.IsPathRooted(args[index + 1]))
                throw new InvalidOperationException("Specify an absolute -adamAndroidOutput QA APK path");
            var output = Path.GetFullPath(args[index + 1]);
            if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) || File.Exists(output) || Directory.Exists(output))
                throw new InvalidOperationException("Use a fresh .apk output path");
            var key = Path.Combine(project, ".android-debug.keystore");
            var password = Environment.GetEnvironmentVariable("ADAM_QA_KEYSTORE_PASS");
            if (!File.Exists(key) || string.IsNullOrEmpty(password))
                throw new InvalidOperationException("Provide the isolated QA debug key and ADAM_QA_KEYSTORE_PASS");
            AdamArtImporter.ValidateBuild();
            PlayerSettings.companyName = "BibleSurvivorQA";
            PlayerSettings.productName = "Adam Android QA";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.biblesurvivor.adam.androidqa");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = key;
            PlayerSettings.Android.keystorePass = password;
            PlayerSettings.Android.keyaliasName = "androiddebugkey";
            PlayerSettings.Android.keyaliasPass = password;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            Debug.Log("ADAM_ANDROID_BUILD backend=IL2CPP arch=ARM64 stripping=Minimal minAPI=26 targetAPI=36 GLES3 QA debug key");
            AssetDatabase.SaveAssets();
            var report = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/Adam.unity" }, output,
                BuildTarget.Android, BuildOptions.Development);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Adam Android QA build failed: " + report.summary.result);
        }
    }
}

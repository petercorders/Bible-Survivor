using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BibleSurvivor.Unity.Editor
{
    // The native Unity entry point consumes the display preferences before managed startup.
    public sealed class AdamDesktopStartupBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder => 1000;

        public void OnPostprocessBuild(BuildReport report)
        {
            var summary = report.summary;
            bool mac = summary.platform == BuildTarget.StandaloneOSX;
            bool windows = summary.platform == BuildTarget.StandaloneWindows ||
                summary.platform == BuildTarget.StandaloneWindows64;
            if ((!mac && !windows) || summary.GetSubtarget<StandaloneBuildSubtarget>() != StandaloneBuildSubtarget.Player ||
                PlayerSettings.defaultScreenWidth != 1920 || PlayerSettings.defaultScreenHeight != 1080)
                return; // The isolated evidence builds use 1100×720.

            var source = Path.GetFullPath("EditorNative");
            var scratch = Path.Combine(Path.GetTempPath(), "adam-display-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                string output = Path.GetFullPath(summary.outputPath);
                if (mac)
                    InstallMac(output, source, scratch);
                else
                    InstallWindows(output, source, scratch);
            }
            finally
            {
                Directory.Delete(scratch, true);
            }
        }

        static void InstallMac(string app, string source, string scratch)
        {
            var contents = Path.Combine(app, "Contents");
            var plist = Path.Combine(contents, "Info.plist");
            string name = Run("/usr/libexec/PlistBuddy", "-c", "Print CFBundleExecutable", plist).Trim();
            string minimumOS = Run("/usr/libexec/PlistBuddy", "-c", "Print LSMinimumSystemVersion", plist).Trim();
            var executable = Path.Combine(contents, "MacOS", name);
            var architectures = Run("/usr/bin/lipo", "-archs", executable)
                .Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var args = new System.Collections.Generic.List<string> {
                "-Wall", "-Wextra", "-Werror", "-std=c++17",
                "-mmacosx-version-min=" + minimumOS,
                "-DADAM_DEFAULT_WIDTH=" + PlayerSettings.defaultScreenWidth,
                "-DADAM_DEFAULT_HEIGHT=" + PlayerSettings.defaultScreenHeight
            };
            foreach (string architecture in architectures)
            {
                args.Add("-arch");
                args.Add(architecture);
            }
            var compiled = Path.Combine(scratch, "Adam");
            args.AddRange(new[] { "-framework", "CoreFoundation",
                Path.Combine(source, "MacDisplayStartup.cpp"),
                Path.Combine(contents, "Frameworks", "UnityPlayer.dylib"), "-o", compiled });
            Run("/usr/bin/clang++", args.ToArray());
            File.Copy(compiled, executable, true);
            var resources = Path.Combine(contents, "Resources");
            const string fontAssets = "Assets/Adam/Resources/ArtFonts/";
            File.Copy(fontAssets + "LICENSE.txt", Path.Combine(resources, "Galmuri-LICENSE.txt"), true);
            File.Copy(fontAssets + "font-manifest.json", Path.Combine(resources, "Galmuri-font-manifest.json"), true);
            Run("/usr/bin/codesign", "--force", "--sign", "-", "--preserve-metadata=entitlements,flags",
                app);
        }

        static void InstallWindows(string executable, string source, string scratch)
        {
            var compiled = Path.Combine(scratch, "Adam.exe");
            var registry = @"Software\" + PlayerSettings.companyName + "\\" + PlayerSettings.productName;
            File.WriteAllText(Path.Combine(scratch, "AdamDisplayDefaults.h"),
                "#define ADAM_DEFAULT_WIDTH " + PlayerSettings.defaultScreenWidth + "\n" +
                "#define ADAM_DEFAULT_HEIGHT " + PlayerSettings.defaultScreenHeight + "\n" +
                "#define ADAM_PREFS_PATH L\"" + registry.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"\n",
                new UTF8Encoding(false));
            Run("cl.exe", "/nologo", "/std:c++17", "/utf-8", "/W4", "/EHsc", "/O2", "/MT",
                "/I" + scratch, "/Fo" + Path.Combine(scratch, "Adam.obj"), "/Fe" + compiled,
                Path.Combine(source, "WindowsDisplayStartup.cpp"), "/link", "/SUBSYSTEM:WINDOWS",
                "advapi32.lib", "user32.lib");
            if (Machine(executable) != Machine(compiled))
                throw new BuildFailedException("Build Adam in the Visual Studio Native Tools command prompt that matches the Player architecture.");
            var manifest = Path.Combine(scratch, "Player.manifest");
            Run("mt.exe", "-nologo", "-inputresource:" + executable + ";#1", "-out:" + manifest);
            Run("mt.exe", "-nologo", "-manifest", manifest, "-outputresource:" + compiled + ";#1");
            File.Copy(compiled, executable, true);
        }

        static ushort Machine(string executable)
        {
            using var file = new BinaryReader(File.OpenRead(executable));
            file.BaseStream.Position = 0x3c;
            file.BaseStream.Position = file.ReadUInt32() + 4;
            return file.ReadUInt16();
        }

        static string Run(string executable, params string[] args)
        {
            var start = new ProcessStartInfo(executable) {
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (string arg in args)
                start.ArgumentList.Add(arg);

            try
            {
                using var process = Process.Start(start);
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new BuildFailedException(executable + " failed (" + process.ExitCode + ")\n"
                        + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
                return output.GetAwaiter().GetResult();
            }
            catch (System.ComponentModel.Win32Exception failure)
            {
                throw new BuildFailedException("Adam display startup requires macOS Xcode command-line tools or the Visual Studio Native Tools prompt for the Windows Player architecture. " + failure.Message);
            }
        }
    }
}

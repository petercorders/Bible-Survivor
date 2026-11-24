using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BibleSurvivor.Unity.Editor
{
    public sealed class AdamMacAppName : IPostprocessBuildWithReport
    {
        public const string AppFileName = AdamGameTitle.English + ".app";

        // Packaging must precede the desktop startup wrapper's final signature.
        public int callbackOrder => 900;

        public void OnPostprocessBuild(BuildReport report)
        {
            var app = Path.GetFullPath(report.summary.outputPath);
            if (report.summary.platform != BuildTarget.StandaloneOSX || Path.GetFileName(app) != AppFileName)
                return;

            var contents = Path.Combine(app, "Contents");
            var info = Path.Combine(contents, "Info.plist");
            var plist = XDocument.Load(info);
            var values = plist.Root.Element("dict");
            var displayName = values.Elements("key").SingleOrDefault(key => key.Value == "CFBundleDisplayName");
            if (displayName == null)
                values.Add(new XElement("key", "CFBundleDisplayName"), new XElement("string", AdamGameTitle.English));
            else
                displayName.ElementsAfterSelf().First().ReplaceWith(new XElement("string", AdamGameTitle.English));
            // XDocument can add an empty internal subset to Unity's DOCTYPE, which CFPropertyList rejects.
            plist.DocumentType?.Remove();
            plist.Save(info);

            var resources = Path.Combine(contents, "Resources");
            // Distribute the unmodified font license with the player.
            File.Copy("Assets/Adam/Resources/ArtFonts/LICENSE.txt", Path.Combine(resources, "Galmuri-LICENSE.txt"), true);
            File.Copy("Assets/Adam/Resources/ArtFonts/font-manifest.json", Path.Combine(resources, "Galmuri-font-manifest.json"), true);

            // Seal updated resources and metadata for the local macOS build.
            if (UnityEngine.Application.platform == UnityEngine.RuntimePlatform.OSXEditor)
                Sign(app);
        }

        static void Sign(string app)
        {
            var quotedPath = "\"" + app.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            using var process = Process.Start(new ProcessStartInfo("/usr/bin/codesign", "--force --sign - " + quotedPath)
            {
                UseShellExecute = false,
                RedirectStandardError = true
            });
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new BuildFailedException("Could not sign " + AppFileName + ": " + error);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    /// <summary>
    /// Replaces Unity's generated placeholder launcher resources with the main artist's
    /// density-matched originals. Files are copied byte-for-byte; no image API is used.
    /// </summary>
    public sealed class FormalAndroidAppIconPostprocessor : IPostGenerateGradleAndroidProject
    {
        private static readonly string[] ManagedResourceNames =
        {
            "app_icon.png",
            "app_icon_round.png",
            "app_icon.xml",
            "app_icon_round.xml",
            "ic_launcher_background.png",
            "ic_launcher_foreground.png"
        };

        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var gradleRoot = FindGradleRoot(path);
            var launcherRoot = Path.Combine(gradleRoot, "launcher", "src", "main");
            var launcherResources = Path.Combine(launcherRoot, "res");
            var sourceResources = Path.Combine(
                Application.dataPath,
                "Plugins",
                "Android",
                "LightseekerAppIcon.androidlib",
                "res");

            if (!Directory.Exists(sourceResources))
                throw new DirectoryNotFoundException("Formal app icon resources are missing: " + sourceResources);
            if (!Directory.Exists(launcherResources))
                throw new DirectoryNotFoundException("Android launcher resources are missing: " + launcherResources);

            RemoveGeneratedLauncherIcons(launcherResources);
            CopyDirectoryExactly(sourceResources, launcherResources);
            ApplyLauncherManifestIconContract(Path.Combine(launcherRoot, "AndroidManifest.xml"));
            DisablePngCrunching(Path.Combine(gradleRoot, "launcher", "build.gradle"));

            Debug.Log("Formal Android app icon resources copied byte-for-byte into launcher module: " + launcherResources);
        }

        private static string FindGradleRoot(string callbackPath)
        {
            var candidate = new DirectoryInfo(Path.GetFullPath(callbackPath));
            for (var i = 0; i < 4 && candidate != null; i++, candidate = candidate.Parent)
            {
                if (Directory.Exists(Path.Combine(candidate.FullName, "launcher", "src", "main")))
                    return candidate.FullName;
            }

            throw new DirectoryNotFoundException("Unable to locate Android launcher module from: " + callbackPath);
        }

        private static void RemoveGeneratedLauncherIcons(string launcherResources)
        {
            foreach (var directory in Directory.GetDirectories(launcherResources, "mipmap*", SearchOption.TopDirectoryOnly))
            {
                foreach (var resourceName in ManagedResourceNames)
                {
                    var target = Path.Combine(directory, resourceName);
                    if (File.Exists(target))
                        File.Delete(target);
                }
            }
        }

        private static void CopyDirectoryExactly(string sourceRoot, string destinationRoot)
        {
            foreach (var sourceFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                var relativePath = sourceFile.Substring(sourceRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var destinationFile = Path.Combine(destinationRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile) ?? destinationRoot);
                File.Copy(sourceFile, destinationFile, true);

                if (!File.ReadAllBytes(sourceFile).SequenceEqual(File.ReadAllBytes(destinationFile)))
                    throw new IOException("Formal app icon copy verification failed: " + relativePath);
            }
        }

        private static void ApplyLauncherManifestIconContract(string manifestPath)
        {
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Android launcher manifest is missing.", manifestPath);

            var document = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
            var manifest = document.Root ?? throw new InvalidDataException("Android launcher manifest has no root element.");
            var application = manifest.Element("application");
            if (application == null)
            {
                application = new XElement("application");
                manifest.AddFirst(application);
            }

            XNamespace android = "http://schemas.android.com/apk/res/android";
            application.SetAttributeValue(android + "icon", "@mipmap/app_icon");
            application.SetAttributeValue(android + "roundIcon", "@mipmap/app_icon_round");
            document.Save(manifestPath);
        }

        private static void DisablePngCrunching(string gradlePath)
        {
            if (!File.Exists(gradlePath))
                throw new FileNotFoundException("Android launcher Gradle file is missing.", gradlePath);

            var text = File.ReadAllText(gradlePath);
            const string marker = "    aaptOptions {";
            const string contract = "    aaptOptions {\n        // Keep accepted launcher PNG bytes unchanged in the APK.\n        cruncherEnabled = false";
            if (text.Contains("cruncherEnabled = false"))
                return;
            if (!text.Contains(marker))
                throw new InvalidDataException("Android launcher Gradle file has no aaptOptions block.");

            text = text.Replace(marker, contract);
            File.WriteAllText(gradlePath, text);
        }
    }
}

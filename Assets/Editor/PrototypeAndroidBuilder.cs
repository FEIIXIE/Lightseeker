using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    public static class PrototypeAndroidBuilder
    {
        private const string ExpectedBundleVersion = "0.43.247";
        private const int ExpectedAndroidVersionCode = 485;
        // A project-root request file lets an already-open Editor build without a second Unity process.
        private const string RequestFileName = "BuildVisualTest.request";
        private const string HarborVerificationRequestFileName = "VerifyFormalHarbor.request";
        static PrototypeAndroidBuilder()
        {
            EditorApplication.update -= BuildWhenRequested;
            EditorApplication.update += BuildWhenRequested;
        }

        private static void BuildWhenRequested()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var requestPath = Path.Combine(projectRoot, RequestFileName);
            if (!File.Exists(requestPath)) return;
            // A request can arrive before Auto Refresh notices source/resource changes.
            // Force a synchronous refresh first; if it triggers compilation/domain reload,
            // the request remains and the freshly loaded builder will consume it afterwards.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            // BuildVisualTest.request may be used only as a refresh bridge for an
            // already-open Editor whose Auto Refresh is disabled. When the dedicated
            // Harbor verification request is present, consume the bridge and never
            // start an APK build.
            if (File.Exists(Path.Combine(projectRoot, HarborVerificationRequestFileName)))
            {
                File.Delete(requestPath);
                return;
            }

            EditorApplication.update -= BuildWhenRequested;
            File.Delete(requestPath);
            try
            {
                BuildVisualTestApk();
            }
            finally
            {
                // A failed native/Gradle build must not disable later retries in the same Editor session.
                EditorApplication.update -= BuildWhenRequested;
                EditorApplication.update += BuildWhenRequested;
            }
        }

        [MenuItem("Seeker/Build Android Visual Test APK _F9")]
        public static void BuildVisualTestApk()
        {
            // The project settings file can be changed while this Editor instance is open.
            // Synchronize the in-memory PlayerSettings before naming or producing the build.
            PlayerSettings.bundleVersion = ExpectedBundleVersion;
            PlayerSettings.Android.bundleVersionCode = ExpectedAndroidVersionCode;
            AssetDatabase.SaveAssets();
            var buildVersion = PlayerSettings.bundleVersion;
            var buildVersionCode = PlayerSettings.Android.bundleVersionCode;
            if (string.IsNullOrWhiteSpace(buildVersion))
                throw new InvalidOperationException("PlayerSettings.bundleVersion must be set before building.");
            if (buildVersionCode <= 0)
                throw new InvalidOperationException("PlayerSettings.Android.bundleVersionCode must be positive before building.");

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes are configured in Editor Build Settings.");

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs"));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory,
                "SeekerAutoChess-v" + buildVersion + "-arm64-test.apk");

            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                // Device-acceptance packages use a non-development player so Unity's
                // "Development Build" watermark never leaks into the game canvas.
                options = BuildOptions.CleanBuildCache
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result);

            Debug.Log("Android visual-test APK built: " + outputPath);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
    }
}

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
        // A project-root request file lets an already-open Editor build without a second Unity process.
        private const string RequestFileName = "BuildVisualTest.request";
        private const string BuildVersion = "0.43.179";
        private const int BuildVersionCode = 417;

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
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying)
                return;

            EditorApplication.update -= BuildWhenRequested;
            File.Delete(requestPath);
            BuildVisualTestApk();
        }

        [MenuItem("Seeker/Build Android Visual Test APK _F9")]
        public static void BuildVisualTestApk()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes are configured in Editor Build Settings.");

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs"));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory,
                "SeekerAutoChess-v" + BuildVersion + "-arm64-test.apk");

            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.bundleVersion = BuildVersion;
            PlayerSettings.Android.bundleVersionCode = BuildVersionCode;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.CleanBuildCache
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result);

            Debug.Log("Android visual-test APK built: " + outputPath);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
    }
}

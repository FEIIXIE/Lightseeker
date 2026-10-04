using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    public static class PrototypeHarborVerificationPlayerBuilder
    {
        private const string RequestFileName = "BuildHarborVerificationPlayer.request";

        static PrototypeHarborVerificationPlayerBuilder()
        {
            EditorApplication.update -= BuildWhenRequested;
            EditorApplication.update += BuildWhenRequested;
        }

        private static void BuildWhenRequested()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            var requestPath = Path.Combine(projectRoot, RequestFileName);
            if (!File.Exists(requestPath)) return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying)
                return;

            File.Delete(requestPath);
            try
            {
                BuildVerificationPlayer(false);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildVerificationPlayer(true);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildVerificationPlayer(bool commandLine)
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No enabled scenes are configured");
            var outputDirectory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "../../../outputs/harbor-verification-player"));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "SeekerHarborVerification.exe");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,
                ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Harbor verification player build failed: " +
                                                    report.summary.result);
            UnityEngine.Debug.Log("[HarborVerification] Player built: " + outputPath +
                                  (commandLine ? " (command line)" : " (open editor request)"));
        }
    }
}

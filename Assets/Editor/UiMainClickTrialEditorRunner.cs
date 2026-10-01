using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    public static class UiMainClickTrialEditorRunner
    {
        private const string RequestFileName = "RunUiMainClickTrial.request";
        private const string TestAssemblyName = "SeekerAutoChess.EditModeTests";
        private const string TestFixtureName = "SeekerAutoChess.Tests.AudioEventRouterTests";
        private const string AudioAssetPath = "Assets/Resources/Audio/UI/SFX_UI_CLICK_MAIN_B_V08.wav";
        private const string ResultFileName = "ui-main-click-editmode-v0.43.124.xml";
        private const string LogFileName = "ui-main-click-editmode-v0.43.124-editor.log";

        private static TestRunnerApi testRunnerApi;
        private static TrialCallbacks callbacks;
        private static string outputDirectory;
        private static string logPath;

        static UiMainClickTrialEditorRunner()
        {
            EditorApplication.update -= RunWhenRequested;
            EditorApplication.update += RunWhenRequested;
        }

        [MenuItem("Seeker/Run UI Main Click Trial")]
        public static void RunFromMenu()
        {
            RunTrial();
        }

        private static void RunWhenRequested()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var requestPath = Path.Combine(projectRoot, RequestFileName);
            if (!File.Exists(requestPath)) return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying)
                return;

            File.Delete(requestPath);
            RunTrial();
        }

        private static void RunTrial()
        {
            outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/test-results"));
            Directory.CreateDirectory(outputDirectory);
            logPath = Path.Combine(outputDirectory, LogFileName);

            var log = new StringBuilder();
            log.AppendLine("UI MAIN CLICK TRIAL EDITOR VALIDATION");
            log.AppendLine("timestamp=" + DateTimeOffset.Now.ToString("O"));
            log.AppendLine("unityVersion=" + Application.unityVersion);
            log.AppendLine("projectPath=" + Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
            log.AppendLine("testFixture=" + TestFixtureName);

            var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioAssetPath);
            var importer = AssetImporter.GetAtPath(AudioAssetPath) as AudioImporter;
            if (audioClip == null || importer == null)
            {
                log.AppendLine("audioImport=FAIL asset or importer missing");
                File.WriteAllText(logPath, log.ToString(), Encoding.UTF8);
                Debug.LogError("UI main click trial: approved WAV could not be loaded.");
                return;
            }

            var settings = importer.defaultSampleSettings;
            log.AppendLine("audioImport=PASS");
            log.AppendLine("clipName=" + audioClip.name);
            log.AppendLine("clipChannels=" + audioClip.channels);
            log.AppendLine("clipFrequency=" + audioClip.frequency);
            log.AppendLine("clipLengthSeconds=" + audioClip.length.ToString("F6"));
            log.AppendLine("forceToMono=" + importer.forceToMono);
            log.AppendLine("preloadAudioData=" + settings.preloadAudioData);
            log.AppendLine("loadInBackground=" + importer.loadInBackground);
            log.AppendLine("loadType=" + settings.loadType);
            log.AppendLine("compressionFormat=" + settings.compressionFormat);

            var previewResult = PlayPreview(audioClip);
            log.AppendLine("editorPreview=" + previewResult);
            File.WriteAllText(logPath, log.ToString(), Encoding.UTF8);

            testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            callbacks = new TrialCallbacks(logPath, Path.Combine(outputDirectory, ResultFileName));
            testRunnerApi.RegisterCallbacks(callbacks);
            testRunnerApi.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { TestAssemblyName },
                groupNames = new[] { "^" + TestFixtureName.Replace(".", "\\.") + "($|\\.)" }
            })
            {
                runSynchronously = true
            });
        }

        private static string PlayPreview(AudioClip clip)
        {
            try
            {
                var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
                var method = audioUtil == null
                    ? null
                    : audioUtil.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(candidate => candidate.Name == "PlayPreviewClip");
                if (method == null) return "BLOCKED: AudioUtil.PlayPreviewClip unavailable";

                var parameters = method.GetParameters();
                object[] arguments;
                if (parameters.Length == 1)
                    arguments = new object[] { clip };
                else if (parameters.Length == 2)
                    arguments = new object[] { clip, 0 };
                else
                    arguments = new object[] { clip, 0, false };
                method.Invoke(null, arguments);
                return "PASS invokedAt=" + DateTimeOffset.Now.ToString("O");
            }
            catch (Exception exception)
            {
                return "BLOCKED: " + exception.GetBaseException().Message;
            }
        }

        private sealed class TrialCallbacks : ICallbacks
        {
            private readonly string editorLogPath;
            private readonly string resultPath;

            public TrialCallbacks(string editorLogPath, string resultPath)
            {
                this.editorLogPath = editorLogPath;
                this.resultPath = resultPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                File.AppendAllText(editorLogPath,
                    "testRunStarted=" + DateTimeOffset.Now.ToString("O") + Environment.NewLine,
                    Encoding.UTF8);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                File.WriteAllText(resultPath, result.ToXml().OuterXml, Encoding.UTF8);
                var summary = "testRunFinished=" + DateTimeOffset.Now.ToString("O") + Environment.NewLine
                    + "passed=" + result.PassCount + Environment.NewLine
                    + "failed=" + result.FailCount + Environment.NewLine
                    + "skipped=" + result.SkipCount + Environment.NewLine
                    + "inconclusive=" + result.InconclusiveCount + Environment.NewLine
                    + "durationSeconds=" + result.Duration.ToString("F6") + Environment.NewLine;
                File.AppendAllText(editorLogPath, summary, Encoding.UTF8);
                Debug.Log("UI main click trial finished: " + result.PassCount + " passed, "
                    + result.FailCount + " failed, " + result.SkipCount + " skipped. XML: " + resultPath);
                testRunnerApi.UnregisterCallbacks(this);
                callbacks = null;
                testRunnerApi = null;
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.IsSuite) return;
                var line = "test=" + result.FullName + " result=" + result.ResultState
                    + " duration=" + result.Duration.ToString("F6");
                if (!string.IsNullOrEmpty(result.Message))
                    line += " message=" + result.Message.Replace(Environment.NewLine, " | ");
                File.AppendAllText(editorLogPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}

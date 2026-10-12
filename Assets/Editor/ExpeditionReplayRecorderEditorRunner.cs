using System;
using System.IO;
using System.Text;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    internal static class ExpeditionReplayRecorderEditorRunner
    {
        private const string RequestFileName = "VerifyExpeditionReplayRecorder.request";
        private const string TestAssemblyName = "SeekerAutoChess.EditModeTests";
        private const string TestFixtureName = "SeekerAutoChess.Tests.ExpeditionReplayRecorderTests";
        private const string ResultFileName =
            "主程-v0.43.316权威远征动作记录器-EditMode-v1.0.xml";
        private const string LogFileName =
            "主程-v0.43.316权威远征动作记录器-Unity-v1.0.log";

        private static TestRunnerApi testRunnerApi;
        private static RecorderCallbacks callbacks;

        static ExpeditionReplayRecorderEditorRunner()
        {
            EditorApplication.update -= RunWhenRequested;
            EditorApplication.update += RunWhenRequested;
        }

        private static void RunWhenRequested()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var requestPath = Path.Combine(projectRoot, RequestFileName);
            if (!File.Exists(requestPath)) return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlaying)
                return;

            File.Delete(requestPath);
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../../outputs/test-results"));
            Directory.CreateDirectory(outputDirectory);
            var resultPath = Path.Combine(outputDirectory, ResultFileName);
            var logPath = Path.Combine(outputDirectory, LogFileName);
            File.WriteAllText(logPath,
                "EXPEDITION REPLAY RECORDER EDITMODE VALIDATION" + Environment.NewLine +
                "documentVersion=v1.0" + Environment.NewLine +
                "clientVersion=v0.43.316" + Environment.NewLine +
                "timestamp=" + DateTimeOffset.Now.ToString("O") + Environment.NewLine,
                Encoding.UTF8);

            testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            callbacks = new RecorderCallbacks(logPath, resultPath);
            testRunnerApi.RegisterCallbacks(callbacks);
            testRunnerApi.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { TestAssemblyName },
                groupNames = new[]
                {
                    "^" + TestFixtureName.Replace(".", "\\.") + "($|\\.)"
                }
            }) { runSynchronously = true });
        }

        private sealed class RecorderCallbacks : ICallbacks
        {
            private readonly string logPath;
            private readonly string resultPath;

            public RecorderCallbacks(string logPath, string resultPath)
            {
                this.logPath = logPath;
                this.resultPath = resultPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                File.AppendAllText(logPath,
                    "testRunStarted=" + DateTimeOffset.Now.ToString("O") +
                    Environment.NewLine, Encoding.UTF8);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                File.WriteAllText(resultPath, result.ToXml().OuterXml, Encoding.UTF8);
                File.AppendAllText(logPath,
                    "testRunFinished=" + DateTimeOffset.Now.ToString("O") + Environment.NewLine +
                    "passed=" + result.PassCount + Environment.NewLine +
                    "failed=" + result.FailCount + Environment.NewLine +
                    "skipped=" + result.SkipCount + Environment.NewLine +
                    "durationSeconds=" + result.Duration.ToString("F6") + Environment.NewLine,
                    Encoding.UTF8);
                Debug.Log("Expedition replay recorder tests finished: " + result.PassCount +
                          " passed, " + result.FailCount + " failed. XML: " + resultPath);
                testRunnerApi.UnregisterCallbacks(this);
                callbacks = null;
                testRunnerApi = null;
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.IsSuite) return;
                var line = "test=" + result.FullName + " result=" + result.ResultState +
                           " duration=" + result.Duration.ToString("F6");
                if (!string.IsNullOrEmpty(result.Message))
                    line += " message=" + result.Message.Replace(Environment.NewLine, " | ");
                File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}

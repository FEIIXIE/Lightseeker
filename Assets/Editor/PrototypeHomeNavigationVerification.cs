using System;
using System.Collections;
using System.IO;
using System.Reflection;
using SeekerAutoChess.Prototype;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    internal static class PrototypeHomeNavigationVerification
    {
        private const string RequestFileName = "VerifyHomeNavigation.request";
        private const string ActiveKey = "seeker.verify.homeNavigation.active";
        private const string StageKey = "seeker.verify.homeNavigation.stage";
        private const string FinalWaitKey = "seeker.verify.homeNavigation.finalWait";
        private const string Version = "v0.43.213";
        private static readonly string[] ViewNames = { "Store", "Deck", "Home", "Arena", "Harbor" };
        private static readonly string[] FileNames =
        {
            "SeekerAutoChess-v0.43.213-navigation-shop-1080x2400.png",
            "SeekerAutoChess-v0.43.213-navigation-deck-1080x2400.png",
            "SeekerAutoChess-v0.43.213-navigation-home-1080x2400.png",
            "SeekerAutoChess-v0.43.213-navigation-arena-1080x2400.png",
            "SeekerAutoChess-v0.43.213-navigation-harbor-1080x2400.png"
        };
        private static double nextActionAt;
        private static bool captureCompleted;
        private static string captureFailure;

        static PrototypeHomeNavigationVerification()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        [MenuItem("Seeker/Verify Home Navigation Five States")]
        private static void StartFromMenu()
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(StageKey, 0);
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        private static void Update()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var requestPath = Path.Combine(projectRoot, RequestFileName);
            if (File.Exists(requestPath))
            {
                File.Delete(requestPath);
                SessionState.SetBool(ActiveKey, true);
                SessionState.SetInt(StageKey, 0);
                SessionState.SetInt(FinalWaitKey, 0);
                nextActionAt = EditorApplication.timeSinceStartup + 1d;
            }

            if (!SessionState.GetBool(ActiveKey, false) ||
                EditorApplication.timeSinceStartup < nextActionAt) return;

            var stage = SessionState.GetInt(StageKey, 0);
            if (!EditorApplication.isPlaying)
            {
                if (stage == 0) EditorApplication.EnterPlaymode();
                return;
            }

            var bootstrap = UnityEngine.Object.FindObjectOfType<PrototypeBootstrap>();
            if (bootstrap == null)
            {
                nextActionAt = EditorApplication.timeSinceStartup + 1d;
                return;
            }

            try
            {
                if (stage == 0)
                {
                    DeletePriorCaptures();
                    Screen.SetResolution(1080, 2400, false);
                    SetView(bootstrap, 0);
                    SessionState.SetInt(StageKey, 1);
                    nextActionAt = EditorApplication.timeSinceStartup + 2d;
                    return;
                }

                if (stage >= 1 && stage <= ViewNames.Length * 2)
                {
                    if (stage % 2 == 1)
                    {
                        var captureIndex = (stage - 1) / 2;
                        Require(CurrentView(bootstrap) == ViewNames[captureIndex],
                            "Unexpected view before navigation capture: " + CurrentView(bootstrap));
                        captureCompleted = false;
                        captureFailure = null;
                        bootstrap.StartCoroutine(CaptureFrame(FileNames[captureIndex]));
                        SessionState.SetInt(StageKey, stage + 1);
                        SessionState.SetInt(FinalWaitKey, 0);
                        nextActionAt = EditorApplication.timeSinceStartup + 1d;
                        return;
                    }

                    var completedIndex = stage / 2 - 1;
                    if (!captureCompleted)
                    {
                        var waitCount = SessionState.GetInt(FinalWaitKey, 0) + 1;
                        SessionState.SetInt(FinalWaitKey, waitCount);
                        if (waitCount <= 10)
                        {
                            EditorApplication.QueuePlayerLoopUpdate();
                            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                            nextActionAt = EditorApplication.timeSinceStartup + 1d;
                            return;
                        }
                        throw new TimeoutException("Timed out waiting for frame-end navigation capture");
                    }
                    if (!string.IsNullOrEmpty(captureFailure))
                        throw new IOException(captureFailure);
                    var completedPath = Path.Combine(OutputDirectory(), FileNames[completedIndex]);
                    if (!File.Exists(completedPath))
                    {
                        var waitCount = SessionState.GetInt(FinalWaitKey, 0) + 1;
                        SessionState.SetInt(FinalWaitKey, waitCount);
                        if (waitCount <= 10)
                        {
                            nextActionAt = EditorApplication.timeSinceStartup + 1d;
                            return;
                        }
                        throw new IOException("Navigation screenshot was not written: " + FileNames[completedIndex]);
                    }

                    if (completedIndex + 1 < ViewNames.Length)
                    {
                        SetView(bootstrap, completedIndex + 1);
                        SessionState.SetInt(StageKey, stage + 1);
                        SessionState.SetInt(FinalWaitKey, 0);
                        nextActionAt = EditorApplication.timeSinceStartup + 2d;
                        return;
                    }
                }
                RequireCapturedFiles();
                WriteResult(projectRoot, true,
                    "Five approved navigation states captured at 1080x2400 using one neutral base and one movable overlay.");
                Finish();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                WriteResult(projectRoot, false, exception.GetBaseException().Message);
                Finish();
            }
        }

        private static void SetView(PrototypeBootstrap bootstrap, int index)
        {
            var field = FindField("currentView");
            field.SetValue(bootstrap, Enum.Parse(field.FieldType, ViewNames[index]));
            var topMenu = FindField("homeTopMenu");
            topMenu.SetValue(bootstrap, 0);
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static string CurrentView(PrototypeBootstrap bootstrap)
        {
            return FindField("currentView").GetValue(bootstrap).ToString();
        }

        private static FieldInfo FindField(string name)
        {
            var field = typeof(PrototypeBootstrap).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(typeof(PrototypeBootstrap).FullName, name);
            return field;
        }

        private static IEnumerator CaptureFrame(string fileName)
        {
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            try
            {
                var outputDirectory = OutputDirectory();
                Directory.CreateDirectory(outputDirectory);
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null) throw new InvalidOperationException("Frame-end screenshot texture is null");
                File.WriteAllBytes(Path.Combine(outputDirectory, fileName), texture.EncodeToPNG());
            }
            catch (Exception exception)
            {
                captureFailure = exception.GetBaseException().Message;
            }
            finally
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                captureCompleted = true;
            }
        }

        private static string OutputDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/screenshots"));
        }

        private static void RequireCapturedFiles()
        {
            var directory = OutputDirectory();
            for (var index = 0; index < FileNames.Length; index++)
                Require(File.Exists(Path.Combine(directory, FileNames[index])),
                    "Navigation screenshot was not written: " + FileNames[index]);
        }

        private static void DeletePriorCaptures()
        {
            var directory = OutputDirectory();
            for (var index = 0; index < FileNames.Length; index++)
            {
                var path = Path.Combine(directory, FileNames[index]);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static void WriteResult(string projectRoot, bool passed, string message)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(projectRoot, "../../outputs/test-results"));
            Directory.CreateDirectory(outputDirectory);
            var result = JsonUtility.ToJson(new Result
            {
                version = Version,
                passed = passed,
                message = message,
                checkedAt = DateTime.UtcNow.ToString("O")
            }, true);
            File.WriteAllText(Path.Combine(outputDirectory,
                "主程-v0.43.213底部导航回归-运行时验证-v1.0.json"), result);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish()
        {
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetInt(FinalWaitKey, 0);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        [Serializable]
        private sealed class Result
        {
            public string version;
            public bool passed;
            public string message;
            public string checkedAt;
        }
    }
}

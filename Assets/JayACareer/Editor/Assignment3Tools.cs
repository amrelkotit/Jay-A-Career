using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor menu for the automated playthrough (Assignment3AutoPilot):
///   Test ▸ one branch / all three branches — fast validation run, report in Assignment 3/Evidence/AutoPilot
///   Record Video — cinematic run (Unity Recorder records the Game view when the package is installed)
/// The runner starts Play mode from the MainMenu scene and stops it when the autopilot reports done.
/// </summary>
[InitializeOnLoad]
public static class Assignment3Tools
{
    const string QueueKey = "jay.autopilot.queue", RecordKey = "jay.record";
    public static string OutputDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Evidence", "AutoPilot"));

    static Assignment3Tools() { EditorApplication.update += Watch; EditorApplication.playModeStateChanged += OnPlayMode; }

    [MenuItem("Jay A Career/Assignment 3/Test Playthrough/All three branches")]
    public static void TestAll() { SessionState.SetString(QueueKey, "accepted,pushed,refused"); Next(); }
    [MenuItem("Jay A Career/Assignment 3/Test Playthrough/Accept Luka's price")] public static void TestAccept() => Start("validate", "accepted");
    [MenuItem("Jay A Career/Assignment 3/Test Playthrough/Push Luka")] public static void TestPush() => Start("validate", "pushed");
    [MenuItem("Jay A Career/Assignment 3/Test Playthrough/Refuse Luka")] public static void TestRefuse() => Start("validate", "refused");

    [MenuItem("Jay A Career/Assignment 3/Record Video (cinematic run)")]
    public static void RecordVideo() { EditorPrefs.SetBool(RecordKey, true); Start("cinematic", "pushed"); }
    [MenuItem("Jay A Career/Assignment 3/Cinematic Run (no recording)")]
    public static void CinematicOnly() { EditorPrefs.SetBool(RecordKey, false); Start("cinematic", "pushed"); }

    public static bool RecordingRequested => EditorPrefs.GetBool(RecordKey, false);

    static void Next()
    {
        var queue = SessionState.GetString(QueueKey, "");
        if (string.IsNullOrEmpty(queue)) return;
        var parts = queue.Split(new[] { ',' }, 2);
        SessionState.SetString(QueueKey, parts.Length > 1 ? parts[1] : "");
        Start("validate", parts[0]);
    }

    static void Start(string mode, string branch)
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(OutputDir);
        PlayerPrefs.SetString(Assignment3AutoPilot.RequestKey, mode + "|" + branch + "|" + OutputDir);
        PlayerPrefs.SetInt(Assignment3AutoPilot.DoneKey, 0);
        PlayerPrefs.Save();
        EditorSceneManager.OpenScene("Assets/JayACareer/Scenes/MainMenu.unity");
        EditorApplication.isPlaying = true;
    }

    static void Watch()
    {
        if (EditorApplication.isPlaying && PlayerPrefs.GetInt(Assignment3AutoPilot.DoneKey, 0) == 1)
        {
            PlayerPrefs.SetInt(Assignment3AutoPilot.DoneKey, 0);
            EditorApplication.isPlaying = false;
        }
    }

    static void OnPlayMode(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            EditorPrefs.SetBool(RecordKey, false);
            if (!string.IsNullOrEmpty(SessionState.GetString(QueueKey, ""))) EditorApplication.delayCall += Next;
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>
/// Records the Game view to MP4 (1920x1080, 30 fps, with audio) while the cinematic autopilot
/// plays the game. Lives in its own assembly so the project still compiles if the Unity Recorder
/// package is missing. Started by "Record Video (cinematic run)" via EditorPrefs "jay.record".
/// </summary>
[InitializeOnLoad]
static class AutoPilotRecorder
{
    static RecorderController controller;

    static AutoPilotRecorder() { EditorApplication.playModeStateChanged += OnPlayMode; }

    static void OnPlayMode(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool("jay.record", false)) Begin();
        if (change == PlayModeStateChange.ExitingPlayMode && controller != null) { controller.StopRecording(); controller = null; Debug.Log("AUTOPILOT RECORDING STOPPED"); }
    }

    static void Begin()
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Video"));
        Directory.CreateDirectory(dir);
        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Jay A Career Prototype";
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings { Codec = CoreEncoderSettings.OutputCodec.MP4, EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High };
        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
        movie.AudioInputSettings.PreserveAudio = true;
        movie.OutputFile = Path.Combine(dir, "Jay_A_Career_Assignment3_Prototype");
        settings.AddRecorderSettings(movie);
        settings.SetRecordModeToManual();
        settings.FrameRate = 30;
        settings.FrameRatePlayback = FrameRatePlayback.Constant;
        settings.CapFrameRate = true;
        controller = new RecorderController(settings);
        controller.PrepareRecording();
        controller.StartRecording();
        Debug.Log("AUTOPILOT RECORDING STARTED -> " + movie.OutputFile + ".mp4");
    }
}

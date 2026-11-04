using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// TRANSITION SCRIPT — every scene change goes through here (brief Step 8).
///
/// Levels never load because the player walked somewhere; a <see cref="LevelDirector"/> calls
/// <see cref="LoadLevel"/> only after the story flag that ends the act is set. The Loading
/// scene then shows a story recap (a "narrative progress marker") while the next level
/// loads asynchronously.
/// </summary>
public static class LevelTransitionManager
{
    public const string MainMenuScene = "MainMenu";
    public const string LoadingScene = "Loading";
    public const string Level1 = "L1_FirstDelivery", Level2 = "L2_Middleman", Level3 = "L3_BigBuyer", EndingScene = "Ending";

    public static string PendingScene { get; private set; }
    public static string PendingTitle { get; private set; }
    public static string PendingRecap { get; private set; }

    /// <summary>Loads a scene via the Loading screen, showing a title and recap text.</summary>
    public static void LoadLevel(string scene, string title = "", string recap = "")
    {
        Time.timeScale = 1;
        PendingScene = scene; PendingTitle = title; PendingRecap = recap;
        if (Application.CanStreamedLevelBeLoaded(LoadingScene)) SceneManager.LoadScene(LoadingScene);
        else SceneManager.LoadScene(scene);
    }

    /// <summary>Rewinds the story to the start of this level and reloads it.</summary>
    public static void RestartLevel()
    {
        StoryStateManager.Instance.RestoreLevelSnapshot();
        string scene = SceneManager.GetActiveScene().name;
        LoadLevel(scene, "Restarting", "Back to the start of this day.");
    }

    public static void ToMainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(MainMenuScene);
    }

    public static void Quit()
    {
        Time.timeScale = 1;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}


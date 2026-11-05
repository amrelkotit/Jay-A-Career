using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI SCRIPT — the starting menu scene (brief "User Experience Flow": start the game, navigate
/// through levels in narrative order, exit/restart).
///   • New Game   — resets the story state and loads Day 1 through the Loading screen
///   • Chapters   — replays any day already reached, in story order (Day 1 → 2 → 3); a day
///                  unlocks the first time the player arrives in it. Starting a later day seeds
///                  the state earlier days would have produced (LevelDirector.directPlayDefaults)
///   • Settings / Credits / Quit
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public const string ChapterKey = "jay.chapterUnlocked";

    [System.Serializable]
    public class Chapter
    {
        public string scene;
        public string title;
        [TextArea(2, 4)] public string recap;
        public Button button;
        public TMP_Text status;
    }

    public Button newGameButton, chaptersButton, settingsButton, creditsButton, quitButton, creditsCloseButton, chaptersCloseButton;
    public CanvasGroup creditsPanel, chaptersPanel;
    public SettingsManager settings;
    public Chapter[] chapters = new Chapter[0];
    [TextArea(3, 6)] public string openingRecap =
        "Calder Flats. Jay is twenty-three, broke, and three days away from losing his room.\n" +
        "The only job on offer is across the road, in the laboratory of a man the town avoids: Dr. Elias Hale.";

    /// <summary>Called by each level when the player arrives, so the chapter list grows in story order.</summary>
    public static void UnlockChapter(int day)
    {
        if (day > PlayerPrefs.GetInt(ChapterKey, 1)) { PlayerPrefs.SetInt(ChapterKey, day); PlayerPrefs.Save(); }
    }

    void Start()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        if (newGameButton) { newGameButton.onClick.AddListener(NewGame); newGameButton.Select(); }
        if (chaptersButton) chaptersButton.onClick.AddListener(() => ShowChapters(true));
        if (chaptersCloseButton) chaptersCloseButton.onClick.AddListener(() => ShowChapters(false));
        if (settingsButton) settingsButton.onClick.AddListener(() => { if (settings) settings.Open(); });
        if (creditsButton) creditsButton.onClick.AddListener(() => ShowCredits(true));
        if (creditsCloseButton) creditsCloseButton.onClick.AddListener(() => ShowCredits(false));
        if (quitButton) quitButton.onClick.AddListener(LevelTransitionManager.Quit);
        for (int i = 0; i < chapters.Length; i++)
        {
            int index = i;
            if (chapters[i].button) chapters[i].button.onClick.AddListener(() => PlayChapter(index));
        }
        ShowCredits(false);
        ShowChapters(false);
    }

    public void NewGame()
    {
        StoryStateManager.Instance.StartNewGame();
        LevelTransitionManager.LoadLevel(LevelTransitionManager.Level1, "DAY 1 — FIRST DELIVERY", openingRecap);
    }

    /// <summary>Day 1 is a normal new game; later days start from their seeded story state.</summary>
    public void PlayChapter(int index)
    {
        if (index < 0 || index >= chapters.Length || index + 1 > PlayerPrefs.GetInt(ChapterKey, 1)) return;
        if (index == 0) { NewGame(); return; }
        StoryStateManager.Instance.ApplyNewGameDefaults();   // not "started": the level seeds earlier days
        var c = chapters[index];
        LevelTransitionManager.LoadLevel(c.scene, c.title, c.recap);
    }

    void ShowChapters(bool show)
    {
        if (!chaptersPanel) return;
        chaptersPanel.alpha = show ? 1 : 0; chaptersPanel.blocksRaycasts = show; chaptersPanel.interactable = show;
        if (!show) return;
        int unlocked = PlayerPrefs.GetInt(ChapterKey, 1);
        for (int i = 0; i < chapters.Length; i++)
        {
            bool open = i + 1 <= unlocked;
            if (chapters[i].button) chapters[i].button.interactable = open;
            if (chapters[i].status) chapters[i].status.text = open ? (i == 0 ? "Start here" : "Replay") : "Locked — reach this day first";
        }
        if (chapters.Length > 0 && chapters[0].button) chapters[0].button.Select();
    }

    void ShowCredits(bool show)
    {
        if (!creditsPanel) return;
        creditsPanel.alpha = show ? 1 : 0; creditsPanel.blocksRaycasts = show; creditsPanel.interactable = show;
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI SCRIPT — the starting menu scene (brief "Start menu: start the game, play levels in
/// narrative order, exit"). New Game resets the story state and loads Level 1 through the
/// Loading screen with the opening recap.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public Button newGameButton, settingsButton, creditsButton, quitButton, creditsCloseButton;
    public CanvasGroup creditsPanel;
    public SettingsManager settings;
    [TextArea(3, 6)] public string openingRecap =
        "Calder Flats. Jay is twenty-three, broke, and three days away from losing his room.\n" +
        "The only job on offer is across the road, in the laboratory of a man the town avoids: Dr. Elias Hale.";

    void Start()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        if (newGameButton) { newGameButton.onClick.AddListener(NewGame); newGameButton.Select(); }
        if (settingsButton) settingsButton.onClick.AddListener(() => settings?.Open());
        if (creditsButton) creditsButton.onClick.AddListener(() => ShowCredits(true));
        if (creditsCloseButton) creditsCloseButton.onClick.AddListener(() => ShowCredits(false));
        if (quitButton) quitButton.onClick.AddListener(LevelTransitionManager.Quit);
        ShowCredits(false);
    }

    public void NewGame()
    {
        StoryStateManager.Instance.StartNewGame();
        LevelTransitionManager.LoadLevel(LevelTransitionManager.Level1, "DAY 1 — FIRST DELIVERY", openingRecap);
    }

    void ShowCredits(bool show)
    {
        if (!creditsPanel) return;
        creditsPanel.alpha = show ? 1 : 0; creditsPanel.blocksRaycasts = show; creditsPanel.interactable = show;
    }
}


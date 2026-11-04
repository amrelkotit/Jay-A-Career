using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI SCRIPT — the pause menu (Esc): Resume, Restart Level, Settings, Main Menu, Quit.
/// Pausing itself is owned by JayGameManager (time scale + cursor); this only wires buttons.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public JayGameManager game;
    public Button resumeButton, restartButton, settingsButton, mainMenuButton, quitButton;
    public SettingsManager settings;

    void Start()
    {
        if (!game) game = FindFirstObjectByType<JayGameManager>();
        if (resumeButton) resumeButton.onClick.AddListener(() => game?.Resume());
        if (restartButton) restartButton.onClick.AddListener(LevelTransitionManager.RestartLevel);
        if (settingsButton) settingsButton.onClick.AddListener(() => settings?.Open());
        if (mainMenuButton) mainMenuButton.onClick.AddListener(LevelTransitionManager.ToMainMenu);
        if (quitButton) quitButton.onClick.AddListener(LevelTransitionManager.Quit);
    }
}

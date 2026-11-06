using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI SCRIPT — the closing screen after the ending cutscene. Summarises the consequences of
/// the player's choices (rent paid or not, how Luka was handled, Trust and Heat) and ends on
/// the cliffhanger line.
/// </summary>
public class EndScreenController : MonoBehaviour
{
    public TMP_Text titleText, summaryText;
    public Button mainMenuButton, quitButton;

    void Start()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        var s = StoryStateManager.Instance;
        s.SetFlag(StoryKeys.EndingSeen, true);
        int cash = s.GetInt(StoryKeys.Cash);
        string deal = s.GetText(StoryKeys.LukaDeal);
        string lukaLine = deal == "accepted" ? "You took Luka's price and kept things quiet."
            : deal == "pushed" ? "You squeezed Luka for more. Paul noticed."
            : deal == "refused" ? "You said no to Luka. Hale will not forget it."
            : "You never dealt with Luka.";
        string rent = cash >= StoryKeys.RentDue
            ? "<color=#9BE39B>Rent paid.</color> You keep your room, for now. ($" + cash + ")"
            : "<color=#E8866A>Rent short by $" + (StoryKeys.RentDue - cash) + ".</color> The eviction notice is still on the door.";
        int trust = s.GetInt(StoryKeys.Trust), heat = s.GetInt(StoryKeys.Heat);
        string trustLine = trust >= StoryKeys.TrustHigh ? "Hale trusted you with his story and his money."
            : trust < StoryKeys.TrustLow ? "Hale never trusted you. He told you only what he had to."
            : "Hale told you the truth, but not all of it.";
        string heatLine = heat >= StoryKeys.HeatHigh ? "Your name reached Paul before you did. Loud work cost you $50."
            : heat < StoryKeys.HeatLow ? "Nobody had heard of you. Paul paid extra for that."
            : "Paul knows your face now.";
        if (titleText) titleText.text = "TO BE CONTINUED";
        if (summaryText)
            summaryText.text =
                "\"Tomorrow. Without the old man.\"\n\n" +
                rent + "\n" + lukaLine + "\n\n" +
                "Trust with Dr. Hale: " + trust + " / 5   —   " + trustLine + "\n" +
                "Heat: " + heat + " / 5   —   " + heatLine + "\n\n" +
                "<size=80%><color=#A0A0A0>Jay has his rent. What he no longer has is a simple answer:\nwhose career is this?</color></size>";
        if (mainMenuButton) { mainMenuButton.onClick.AddListener(LevelTransitionManager.ToMainMenu); mainMenuButton.Select(); }
        if (quitButton) quitButton.onClick.AddListener(LevelTransitionManager.Quit);
    }
}

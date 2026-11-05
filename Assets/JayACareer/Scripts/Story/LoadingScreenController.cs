using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Lives in the Loading scene: shows the recap, loads the next level asynchronously and waits
/// for the player to press a key, so the recap can actually be read.
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    public TMP_Text titleText, recapText, statusText, tipText;
    public Image progressFill;
    [TextArea] public string[] tips =
    {
        "Press [Tab] for your inventory and [J] for the journal.",
        "Choices with buyers change how Dr. Hale treats you.",
        "Heat is attention. The more you have, the more people watch you.",
        "Press [F] for the flashlight when the lab is dark."
    };

    /// <summary>Set by the automated playthrough: continue on its own after this many seconds (-1 = wait for the player).</summary>
    public static float AutoContinueAfter = -1;

    IEnumerator Start()
    {
        string scene = string.IsNullOrEmpty(LevelTransitionManager.PendingScene) ? LevelTransitionManager.MainMenuScene : LevelTransitionManager.PendingScene;
        if (titleText) titleText.text = LevelTransitionManager.PendingTitle;
        if (recapText) recapText.text = LevelTransitionManager.PendingRecap;
        if (tipText && tips.Length > 0) tipText.text = "TIP: " + tips[Random.Range(0, tips.Length)];
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        yield return null;
        var op = SceneManager.LoadSceneAsync(scene);
        op.allowSceneActivation = false;
        float minShow = Time.realtimeSinceStartup + 1.5f;   // never flash the recap away instantly
        while (op.progress < .9f || Time.realtimeSinceStartup < minShow)
        {
            if (progressFill) progressFill.fillAmount = Mathf.Clamp01(op.progress / .9f);
            if (statusText) statusText.text = "Loading...";
            yield return null;
        }
        if (progressFill) progressFill.fillAmount = 1;
        if (statusText) statusText.text = "Press [E], [Space] or click to continue";
        float readyAt = Time.realtimeSinceStartup;
        while (true)
        {
            if (AutoContinueAfter >= 0 && Time.realtimeSinceStartup - readyAt >= AutoContinueAfter) break;
            var k = UnityEngine.InputSystem.Keyboard.current; var m = UnityEngine.InputSystem.Mouse.current;
            if ((k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                || (m != null && m.leftButton.wasPressedThisFrame)) break;
            yield return null;
        }
        op.allowSceneActivation = true;
    }
}

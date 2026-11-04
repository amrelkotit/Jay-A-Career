using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// UI SCRIPT — Jay's notebook with two tabs:
///   • INVENTORY [Tab]: collected lore items and their descriptions (required by the brief)
///   • JOURNAL   [J]  : clues and story notes discovered so far (optional codex)
/// It reads directly from <see cref="StoryStateManager"/>, so items picked up in Level 1 are
/// still listed in Level 3.
/// </summary>
public class NotebookUI : MonoBehaviour
{
    public static NotebookUI Instance { get; private set; }
    public static bool IsOpen => Instance && Instance.open;

    public CanvasGroup panel;
    public TMP_Text headerText;
    public TMP_Text listText;
    public TMP_Text detailText;
    public Button inventoryTab, journalTab, closeButton;

    bool open, showingJournal;
    int selected;

    void Awake()
    {
        Instance = this;
        if (inventoryTab) inventoryTab.onClick.AddListener(() => Show(false));
        if (journalTab) journalTab.onClick.AddListener(() => Show(true));
        if (closeButton) closeButton.onClick.AddListener(Close);
        SetVisible(false);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null || Time.timeScale == 0 && !open) return;
        if (DialogueManager.IsOpen || CutsceneController.IsPlaying) { if (open) Close(); return; }
        if (k.tabKey.wasPressedThisFrame) { if (open && !showingJournal) Close(); else Show(false); }
        else if (k.jKey.wasPressedThisFrame) { if (open && showingJournal) Close(); else Show(true); }
        else if (open && k.escapeKey.wasPressedThisFrame) Close();
        else if (open && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) { selected++; Render(); }
        else if (open && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) { selected--; Render(); }
    }

    public void Show(bool journal)
    {
        showingJournal = journal; selected = int.MaxValue; // newest entry first
        SetVisible(true);
        Render();
    }

    public void Close() => SetVisible(false);

    void SetVisible(bool value)
    {
        open = value;
        if (panel) { panel.alpha = value ? 1 : 0; panel.blocksRaycasts = value; panel.interactable = value; }
        if (Application.isPlaying)
        {
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value;
        }
    }

    void Render()
    {
        var s = StoryStateManager.Instance;
        int count = showingJournal ? s.Journal.Count : s.Inventory.Count;
        if (headerText) headerText.text = showingJournal ? "JOURNAL  <size=60%>[J]</size>" : "INVENTORY  <size=60%>[Tab]</size>";
        if (count == 0)
        {
            if (listText) listText.text = "<i>Nothing yet.</i>";
            if (detailText) detailText.text = showingJournal ? "Clues you discover are written down here." : "Look around. Some objects tell you more than people do.";
            return;
        }
        selected = Mathf.Clamp(selected, 0, count - 1);
        var list = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            string title = showingJournal ? s.Journal[i].title : s.Inventory[i].title;
            list.Append(i == selected ? "<color=#F2C46D>> " + title + "</color>" : "   " + title).Append('\n');
        }
        if (listText) listText.text = list.ToString();
        if (detailText)
        {
            string title = showingJournal ? s.Journal[selected].title : s.Inventory[selected].title;
            string body = showingJournal ? s.Journal[selected].body : s.Inventory[selected].description;
            detailText.text = "<b>" + title + "</b>\n\n" + body + "\n\n<size=75%><color=#9A9A9A>W/S or arrow keys to browse</color></size>";
        }
    }
}

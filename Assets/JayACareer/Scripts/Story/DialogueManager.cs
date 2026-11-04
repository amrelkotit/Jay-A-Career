using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// NARRATIVE SCRIPT #3 — plays <see cref="DialogueData"/> conversations in the dialogue box.
///
/// Responsibilities (brief Steps 3–4):
///   • shows speaker name + portrait + text with a typewriter effect
///   • advances with E / Space / Enter / left-click; a press while typing completes the line
///   • shows up to three choices (keys 1–3 or mouse) and applies their effects
///   • follows a choice's "next" conversation (dialogue-tree branches)
///   • applies onComplete effects and marks play-once conversations as seen
///
/// It never decides WHICH conversation plays — characters and triggers do that through
/// preconditions — keeping data, logic and presentation separate.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    /// <summary>True while a conversation is on screen; the player controller freezes movement.</summary>
    public static bool IsOpen => Instance != null && Instance.open;
    /// <summary>Frame on which the last conversation closed. The player ignores the same key press so
    /// the final [E] does not immediately re-open the conversation.</summary>
    public static int LastClosedFrame { get; private set; } = -1;
    int openedFrame = -1;

    [Header("UI references (built by Assignment3Builder)")]
    public CanvasGroup panel;
    public TMP_Text speakerNameText;
    public TMP_Text bodyText;
    public Image portraitImage;
    public GameObject portraitFrame;
    public RectTransform choiceContainer;
    public Button choiceTemplate;
    public TMP_Text continueHint;

    [Header("Feel")]
    [Range(10, 120)] public float charactersPerSecond = 48;
    public AudioSource uiAudio;
    public AudioClip openSound, choiceSound;

    /// <summary>The profile of whoever is currently speaking (used by character animation).</summary>
    public CharacterProfile CurrentSpeaker { get; private set; }

    bool open, typing, awaitingChoice;
    float baseBodySize;
    readonly List<DialogueLine> visibleLines = new List<DialogueLine>();
    readonly List<DialogueChoice> visibleChoices = new List<DialogueChoice>();
    readonly List<Button> choiceButtons = new List<Button>();
    readonly Queue<Request> pending = new Queue<Request>();
    DialogueData current;
    int lineIndex;
    Coroutine typer;
    Request active;
    CursorLockMode previousLock;
    bool previousCursorVisible;

    class Request
    {
        public DialogueData data;
        public Action<DialogueChoice> onChoice;
        public Action onFinished;
    }

    void Awake()
    {
        Instance = this;
        if (bodyText) baseBodySize = bodyText.fontSize;
        if (choiceTemplate) choiceTemplate.gameObject.SetActive(false);
        Hide();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ------------------------------------------------------------------ public API
    /// <summary>Starts a conversation. If one is already open the request is queued.</summary>
    public void Begin(DialogueData data, Action<DialogueChoice> onChoice = null, Action onFinished = null)
    {
        if (!data) { onFinished?.Invoke(); return; }
        var request = new Request { data = data, onChoice = onChoice, onFinished = onFinished };
        if (open) { pending.Enqueue(request); return; }
        active = request;
        previousLock = Cursor.lockState; previousCursorVisible = Cursor.visible;
        open = true; openedFrame = Time.frameCount;
        if (panel) { panel.alpha = 1; panel.blocksRaycasts = true; panel.interactable = true; }
        Play(openSound);
        StartConversation(data);
    }

    // ------------------------------------------------------------------ flow
    void StartConversation(DialogueData data)
    {
        current = data;
        var state = StoryStateManager.Instance;
        visibleLines.Clear();
        foreach (var line in data.lines)
            if (line != null && StoryCondition.AllMet(line.conditions, state)) visibleLines.Add(line);
        lineIndex = -1;
        if (data.playOnce) state.SetFlag(data.SeenFlag, true);
        NextLine();
    }

    void NextLine()
    {
        lineIndex++;
        if (lineIndex < visibleLines.Count) { ShowLine(visibleLines[lineIndex]); return; }
        // All lines shown: offer choices or finish the conversation.
        var state = StoryStateManager.Instance;
        visibleChoices.Clear();
        foreach (var c in current.choices)
            if (c != null && StoryCondition.AllMet(c.conditions, state)) visibleChoices.Add(c);
        if (visibleChoices.Count > 0) ShowChoices();
        else FinishConversation(null);
    }

    void ShowLine(DialogueLine line)
    {
        CurrentSpeaker = line.speaker;
        if (speakerNameText)
        {
            speakerNameText.text = line.speaker ? line.speaker.displayName.ToUpperInvariant() : "";
            speakerNameText.color = line.speaker ? line.speaker.nameColor : Color.white;
        }
        bool hasPortrait = line.speaker && line.speaker.portrait;
        if (portraitFrame) portraitFrame.SetActive(hasPortrait);
        if (portraitImage && hasPortrait) portraitImage.sprite = line.speaker.portrait;
        if (bodyText && baseBodySize > 0) bodyText.fontSize = baseBodySize * SettingsManager.SubtitleScale;
        if (continueHint) continueHint.text = "";
        if (typer != null) StopCoroutine(typer);
        typer = StartCoroutine(TypeLine(line.text ?? ""));
    }

    IEnumerator TypeLine(string text)
    {
        typing = true;
        if (bodyText) { bodyText.text = text; bodyText.maxVisibleCharacters = 0; bodyText.ForceMeshUpdate(); }
        int total = bodyText ? bodyText.textInfo.characterCount : 0;
        float shown = 0;
        while (bodyText && shown < total)
        {
            shown += Time.unscaledDeltaTime * charactersPerSecond;
            bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
        CompleteTyping();
    }

    void CompleteTyping()
    {
        if (typer != null) { StopCoroutine(typer); typer = null; }
        typing = false;
        if (bodyText) bodyText.maxVisibleCharacters = int.MaxValue;
        if (continueHint)
        {
            bool last = lineIndex >= visibleLines.Count - 1;
            continueHint.text = last && HasPendingChoices() ? "[E] Choose a response" : "[E] Continue";
        }
    }

    bool HasPendingChoices()
    {
        var state = StoryStateManager.Instance;
        foreach (var c in current.choices) if (c != null && StoryCondition.AllMet(c.conditions, state)) return true;
        return false;
    }

    void ShowChoices()
    {
        awaitingChoice = true;
        if (continueHint) continueHint.text = "Choose with [1]-[" + visibleChoices.Count + "] or the mouse";
        // Choices need the mouse, so release the cursor until a choice is made.
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        for (int i = 0; i < visibleChoices.Count; i++)
        {
            var button = i < choiceButtons.Count ? choiceButtons[i] : CreateChoiceButton();
            int index = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Choose(index));
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label) label.text = (i + 1) + ".  " + visibleChoices[i].text;
            button.gameObject.SetActive(true);
        }
        for (int i = visibleChoices.Count; i < choiceButtons.Count; i++) choiceButtons[i].gameObject.SetActive(false);
        if (choiceButtons.Count > 0) choiceButtons[0].Select();
    }

    Button CreateChoiceButton()
    {
        var b = Instantiate(choiceTemplate, choiceContainer);
        b.name = "Choice " + (choiceButtons.Count + 1);
        choiceButtons.Add(b);
        return b;
    }

    void Choose(int index)
    {
        if (!awaitingChoice || index < 0 || index >= visibleChoices.Count) return;
        awaitingChoice = false;
        var choice = visibleChoices[index];
        foreach (var b in choiceButtons) b.gameObject.SetActive(false);
        Cursor.lockState = previousLock; Cursor.visible = previousCursorVisible;
        Play(choiceSound);
        StoryEffect.ApplyAll(choice.effects, StoryStateManager.Instance);
        active.onChoice?.Invoke(choice);
        if (choice.next) { ApplyCompletion(current); StartConversation(choice.next); }
        else FinishConversation(choice);
    }

    void ApplyCompletion(DialogueData data) => StoryEffect.ApplyAll(data.onComplete, StoryStateManager.Instance);

    void FinishConversation(DialogueChoice choice)
    {
        ApplyCompletion(current);
        var finished = active;
        // A conversation-level loop action (e.g. briefing done) is reported as a pseudo choice.
        if (current.loopActionOnComplete != DialogueLoopAction.None)
            finished.onChoice?.Invoke(new DialogueChoice { loopAction = current.loopActionOnComplete });
        Hide();
        finished.onFinished?.Invoke();
        if (pending.Count > 0)
        {
            var next = pending.Dequeue();
            Begin(next.data, next.onChoice, next.onFinished);
        }
    }

    void Hide()
    {
        if (open) LastClosedFrame = Time.frameCount;
        open = false; typing = false; awaitingChoice = false; CurrentSpeaker = null;
        if (panel) { panel.alpha = 0; panel.blocksRaycasts = false; panel.interactable = false; }
        foreach (var b in choiceButtons) if (b) b.gameObject.SetActive(false);
        if (Cursor.lockState != previousLock && Application.isPlaying && active != null)
        { Cursor.lockState = previousLock; Cursor.visible = previousCursorVisible; }
    }

    // ------------------------------------------------------------------ input
    void Update()
    {
        if (!open || Time.timeScale == 0 || Time.frameCount == openedFrame) return;
        var k = Keyboard.current; var m = Mouse.current;
        if (awaitingChoice)
        {
            if (k == null) return;
            if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) Choose(0);
            else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) Choose(1);
            else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) Choose(2);
            return;
        }
        bool advance = (k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                       || (m != null && m.leftButton.wasPressedThisFrame);
        if (!advance) return;
        if (typing) CompleteTyping();
        else NextLine();
    }

    void Play(AudioClip clip) { if (uiAudio && clip) uiAudio.PlayOneShot(clip, .5f); }
}

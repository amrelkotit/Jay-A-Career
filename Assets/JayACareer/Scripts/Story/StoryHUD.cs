using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI SCRIPT — the in-game HUD (brief "UI Features"):
///   • objective tracker with a "NEW OBJECTIVE" flash and an on-screen marker
///   • status indicators: Cash (with the $400 rent goal), Day, Trust (Dr. Hale) and Heat bars
///   • contextual interaction prompt and short message toasts
///   • chapter title card at the start of each level and cutscene subtitles
///
/// Every value is read from <see cref="StoryStateManager"/>, so the HUD itself owns no story
/// state — the "signs and feedback" principle from the lecture: the interface only reports
/// what the game world already knows.
/// </summary>
public class StoryHUD : MonoBehaviour
{
    public static StoryHUD Instance { get; private set; }

    [Header("Root")]
    public CanvasGroup hudGroup;
    [Header("Objective")]
    public TMP_Text objectiveLabel;
    public TMP_Text objectiveText;
    public RectTransform marker;
    public TMP_Text markerDistance;
    [Header("Status")]
    public TMP_Text cashText;
    public TMP_Text dayText;
    public Image trustFill;
    public Image heatFill;
    [Header("Prompt & messages")]
    public TMP_Text promptText;
    public CanvasGroup toastGroup;
    public TMP_Text toastText;
    [Header("Chapter card & subtitles")]
    public CanvasGroup chapterGroup;
    public TMP_Text chapterTitle;
    public TMP_Text chapterSubtitle;
    public TMP_Text subtitleText;

    static readonly Queue<string> earlyToasts = new Queue<string>();
    readonly Queue<string> toasts = new Queue<string>();
    Transform markerTarget;
    Coroutine toastRoutine, flashRoutine;
    float subtitleBaseSize;

    void Awake()
    {
        Instance = this;
        if (toastGroup) toastGroup.alpha = 0;
        if (chapterGroup) chapterGroup.alpha = 0;
        if (subtitleText) { subtitleBaseSize = subtitleText.fontSize; subtitleText.text = ""; }
        if (promptText) promptText.text = "";
        while (earlyToasts.Count > 0) toasts.Enqueue(earlyToasts.Dequeue());
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
    void OnEnable() { StoryStateManager.Instance.Changed += RefreshStatus; RefreshStatus(); }
    void OnDisable() { if (StoryStateManager.Exists) StoryStateManager.Instance.Changed -= RefreshStatus; }

    // ------------------------------------------------------------------ static helpers (safe before the HUD exists)
    public static void Toast(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (Instance) Instance.EnqueueToast(message); else earlyToasts.Enqueue(message);
    }

    public static void SetObjective(string text, Transform target, bool flash)
    {
        if (!Instance) return;
        Instance.markerTarget = target;
        if (Instance.objectiveText) Instance.objectiveText.text = text;
        if (flash && Instance.isActiveAndEnabled && !string.IsNullOrEmpty(text))
        {
            if (Instance.flashRoutine != null) Instance.StopCoroutine(Instance.flashRoutine);
            Instance.flashRoutine = Instance.StartCoroutine(Instance.FlashObjective());
        }
    }

    public static void SetPrompt(string text) { if (Instance && Instance.promptText) Instance.promptText.text = text ?? ""; }

    public static void SetSubtitle(string speaker, string text)
    {
        if (!Instance || !Instance.subtitleText) return;
        Instance.subtitleText.fontSize = Instance.subtitleBaseSize * SettingsManager.SubtitleScale;
        Instance.subtitleText.text = string.IsNullOrEmpty(text) ? "" :
            (string.IsNullOrEmpty(speaker) ? text : "<b>" + speaker.ToUpperInvariant() + ":</b> " + text);
    }

    /// <summary>Hides gameplay HUD during cutscenes (subtitles stay visible).</summary>
    public static void SetGameplayVisible(bool visible) { if (Instance && Instance.hudGroup) Instance.hudGroup.alpha = visible ? 1 : 0; }

    public static void ShowChapter(string title, string subtitle)
    {
        if (Instance && Instance.isActiveAndEnabled) Instance.StartCoroutine(Instance.ChapterCard(title, subtitle));
    }

    // ------------------------------------------------------------------ status
    void RefreshStatus()
    {
        var s = StoryStateManager.Instance;
        int cash = s.GetInt(StoryKeys.Cash);
        if (cashText)
        {
            bool rentCovered = cash >= StoryKeys.RentDue;
            cashText.text = "$" + cash + "  <size=70%><color=" + (rentCovered ? "#9BE39B" : "#E8B36A") + ">/ $" + StoryKeys.RentDue + " rent</color></size>";
        }
        if (dayText) dayText.text = "DAY " + Mathf.Max(1, s.GetInt(StoryKeys.Day)) + " / 3";
        if (trustFill) trustFill.fillAmount = s.GetInt(StoryKeys.Trust) / 5f;
        if (heatFill) heatFill.fillAmount = s.GetInt(StoryKeys.Heat) / 5f;
    }

    // ------------------------------------------------------------------ objective marker
    void LateUpdate()
    {
        if (!marker) return;
        var cam = Camera.main;
        bool show = markerTarget && cam && markerTarget.gameObject.activeInHierarchy && !DialogueManager.IsOpen && !CutsceneController.IsPlaying;
        if (show)
        {
            Vector3 screen = cam.WorldToScreenPoint(markerTarget.position + Vector3.up * .3f);
            float distance = Vector3.Distance(cam.transform.position, markerTarget.position);
            // Hide the marker when the target is close and visible, so it never covers the object itself.
            bool onScreen = screen.z > 0 && screen.x > 0 && screen.x < Screen.width && screen.y > 0 && screen.y < Screen.height;
            show = !(onScreen && distance < 3.5f);
            if (show)
            {
                if (screen.z < 0) { screen.x = Screen.width - screen.x; screen.y = 0; } // behind the camera -> clamp to bottom
                float m = 60;
                screen.x = Mathf.Clamp(screen.x, m, Screen.width - m);
                screen.y = Mathf.Clamp(screen.y, m, Screen.height - m);
                marker.position = new Vector3(screen.x, screen.y, 0);
                if (markerDistance) markerDistance.text = Mathf.RoundToInt(distance) + " m";
            }
        }
        if (marker.gameObject.activeSelf != show) marker.gameObject.SetActive(show);
    }

    // ------------------------------------------------------------------ animations (unscaled so they work while paused)
    void EnqueueToast(string message)
    {
        toasts.Enqueue(message);
        if (toastRoutine == null && isActiveAndEnabled) toastRoutine = StartCoroutine(ShowToasts());
    }

    void Start() { if (toasts.Count > 0 && toastRoutine == null) toastRoutine = StartCoroutine(ShowToasts()); }

    IEnumerator ShowToasts()
    {
        while (toasts.Count > 0)
        {
            if (toastText) toastText.text = toasts.Dequeue();
            yield return Fade(toastGroup, 0, 1, .2f);
            yield return new WaitForSecondsRealtime(toasts.Count > 0 ? 2.2f : 3.6f);
            yield return Fade(toastGroup, 1, 0, .3f);
        }
        toastRoutine = null;
    }

    IEnumerator FlashObjective()
    {
        if (objectiveLabel) objectiveLabel.text = "NEW OBJECTIVE";
        var rt = objectiveText ? objectiveText.rectTransform : null;
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime * 2.5f)
        {
            if (rt) rt.localScale = Vector3.one * (1 + .08f * Mathf.Sin(t * Mathf.PI));
            if (objectiveLabel) objectiveLabel.color = Color.Lerp(new Color(1, .75f, .3f), new Color(.85f, .85f, .85f), t);
            yield return null;
        }
        if (rt) rt.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(2f);
        if (objectiveLabel) objectiveLabel.text = "OBJECTIVE";
    }

    IEnumerator ChapterCard(string title, string subtitle)
    {
        if (!chapterGroup) yield break;
        if (chapterTitle) chapterTitle.text = title;
        if (chapterSubtitle) chapterSubtitle.text = subtitle;
        yield return Fade(chapterGroup, 0, 1, .8f);
        yield return new WaitForSecondsRealtime(3f);
        yield return Fade(chapterGroup, 1, 0, 1f);
    }

    static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
    {
        if (!group) yield break;
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / Mathf.Max(.01f, seconds))
        { group.alpha = Mathf.Lerp(from, to, t); yield return null; }
        group.alpha = to;
    }
}

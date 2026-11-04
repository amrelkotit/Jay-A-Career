using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

/// <summary>
/// NARRATIVE SCRIPT #7 — plays the opening / closing narrative (brief Step 7).
///
/// A Timeline (PlayableDirector) drives Cinemachine shot cameras; this component adds what
/// the story layer needs around it:
///   • timed subtitles shown in the HUD subtitle bar
///   • gameplay HUD hidden and player input frozen while it plays
///   • hold [Space] to skip (with a small progress ring)
///   • on finish (played or skipped): set a story flag, apply effects, raise an event and
///     optionally load the next scene through <see cref="LevelTransitionManager"/>
/// Without a Timeline it falls back to cutting between the listed shot cameras.
/// </summary>
public class CutsceneController : MonoBehaviour
{
    public static bool IsPlaying => active != null;
    static CutsceneController active;

    [Serializable]
    public class Subtitle
    {
        public float time;
        public float duration = 3f;
        public string speaker;
        [TextArea] public string text;
    }

    [Serializable]
    public class Shot
    {
        public CinemachineCamera camera;
        public float duration = 3f;
    }

    [Header("Playback")]
    public string cutsceneId = "cutscene";
    public PlayableDirector director;
    [Tooltip("Used only when no Timeline is assigned: cameras cut in order.")]
    public List<Shot> fallbackShots = new List<Shot>();
    public bool playOnStart;
    public List<StoryCondition> startConditions = new List<StoryCondition>();
    public List<Subtitle> subtitles = new List<Subtitle>();

    [Header("Skip")]
    public float holdToSkipSeconds = .8f;
    public CanvasGroup skipHint;
    public UnityEngine.UI.Image skipFill;

    [Header("When finished")]
    public string flagOnFinish = "";
    public List<StoryEffect> effectsOnFinish = new List<StoryEffect>();
    public UnityEvent onFinished;
    [Tooltip("Optional: scene to load when the cutscene ends (e.g. the Ending scene).")]
    public string loadSceneOnFinish = "";
    public string loadTitle = "", loadRecap = "";
    public GameObject[] hideWhilePlaying;

    float elapsed, skipHeld, length;
    bool finished;

    void Start()
    {
        if (skipHint) skipHint.alpha = 0;
        if (playOnStart) Play();
    }

    /// <summary>Plays the cutscene unless it has already been seen or its conditions fail.</summary>
    public void Play()
    {
        var state = StoryStateManager.Instance;
        if (active || (!string.IsNullOrEmpty(flagOnFinish) && state.GetFlag(flagOnFinish))) return;
        if (!StoryCondition.AllMet(startConditions, state)) return;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        active = this; finished = false; elapsed = 0; skipHeld = 0;
        StoryHUD.SetGameplayVisible(false);
        foreach (var g in hideWhilePlaying) if (g) g.SetActive(false);
        if (skipHint) skipHint.alpha = 1;
        length = 0;
        if (director && director.playableAsset)
        {
            length = (float)director.playableAsset.duration;
            director.time = 0; director.Play();
        }
        else foreach (var s in fallbackShots) length += s.duration;
        foreach (var s in subtitles) length = Mathf.Max(length, s.time + s.duration);

        int shotIndex = -1; float shotStart = 0;
        while (!finished && elapsed < length)
        {
            elapsed += Time.deltaTime;
            if (!director || !director.playableAsset) UpdateFallbackShots(ref shotIndex, ref shotStart);
            UpdateSubtitles();
            UpdateSkip();
            yield return null;
        }
        Finish();
    }

    void UpdateFallbackShots(ref int index, ref float start)
    {
        float t = 0;
        for (int i = 0; i < fallbackShots.Count; i++)
        {
            if (elapsed < t + fallbackShots[i].duration)
            {
                if (i != index)
                {
                    if (index >= 0 && fallbackShots[index].camera) fallbackShots[index].camera.Priority = 0;
                    if (fallbackShots[i].camera) fallbackShots[i].camera.Priority = 100;
                    index = i; start = t;
                }
                return;
            }
            t += fallbackShots[i].duration;
        }
    }

    void UpdateSubtitles()
    {
        Subtitle current = null;
        foreach (var s in subtitles) if (elapsed >= s.time && elapsed < s.time + s.duration) current = s;
        StoryHUD.SetSubtitle(current?.speaker, current?.text);
    }

    void UpdateSkip()
    {
        var k = Keyboard.current;
        bool held = k != null && (k.spaceKey.isPressed || k.escapeKey.isPressed);
        skipHeld = held ? skipHeld + Time.unscaledDeltaTime : 0;
        if (skipFill) skipFill.fillAmount = Mathf.Clamp01(skipHeld / holdToSkipSeconds);
        if (skipHeld >= holdToSkipSeconds) finished = true;
    }

    void Finish()
    {
        if (director) director.Stop();
        foreach (var s in fallbackShots) if (s.camera) s.camera.Priority = 0;
        StoryHUD.SetSubtitle(null, null);
        if (skipHint) skipHint.alpha = 0;
        foreach (var g in hideWhilePlaying) if (g) g.SetActive(true);
        StoryHUD.SetGameplayVisible(true);
        active = null;
        var state = StoryStateManager.Instance;
        if (!string.IsNullOrEmpty(flagOnFinish)) state.SetFlag(flagOnFinish, true);
        StoryEffect.ApplyAll(effectsOnFinish, state);
        onFinished?.Invoke();
        if (!string.IsNullOrEmpty(loadSceneOnFinish)) LevelTransitionManager.LoadLevel(loadSceneOnFinish, loadTitle, loadRecap);
    }

    void OnDisable() { if (active == this) active = null; }
}

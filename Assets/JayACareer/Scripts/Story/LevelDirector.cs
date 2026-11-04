using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NARRATIVE SCRIPT #8 — one per level scene. Connects the existing delivery loop
/// (JayGameManager / JayDailyLoop) to the story layer and owns the level's pacing:
///   • starts the level: day number, chapter card, level snapshot for "Restart Level"
///   • mirrors loop events (sealed, delivered, sold, cleaned, slept...) into story flags
///   • drives the time of day from story beats (sunrise in L1, dusk falls during Luka's deal)
///   • when the act's completion flag is set, loads the next level with a recap
///     (transitions are gated by STORY STATE, not by where the player stands)
/// When a level is opened directly in the Editor it seeds the state the previous levels
/// would have produced, so every level is testable on its own.
/// </summary>
public class LevelDirector : MonoBehaviour
{
    [Serializable]
    public class LoopEventEffects
    {
        [Tooltip("briefed, prepared, pickedup, delivered, sold, refused, returned, cleaned, slept")]
        public string loopEvent;
        public List<StoryCondition> conditions = new List<StoryCondition>();
        public List<StoryEffect> effects = new List<StoryEffect>();
    }

    [Serializable]
    public class TimeOfDayKey
    {
        public string note;
        public List<StoryCondition> conditions = new List<StoryCondition>();
        [Range(0, 1)] [Tooltip("0/1 = midday, .25 = late afternoon, .4 = dusk, .5 = midnight, .75 = dawn")]
        public float cycleFraction;
        public float blendSeconds;
    }

    [Serializable]
    public class RecapVariant
    {
        public List<StoryCondition> conditions = new List<StoryCondition>();
        [TextArea(3, 8)] public string recap;
    }

    [Header("Level")]
    public int dayNumber = 1;
    public string chapterTitle = "Day 1";
    public string chapterSubtitle = "";
    public JayGameManager game;
    public JayLightingController lighting;

    [Header("Direct play from the Editor (state the earlier levels would have produced)")]
    public List<StoryEffect> directPlayDefaults = new List<StoryEffect>();

    [Header("Story mapping")]
    public List<LoopEventEffects> loopEventEffects = new List<LoopEventEffects>();
    public List<TimeOfDayKey> timeOfDay = new List<TimeOfDayKey>();

    [Header("Completion")]
    public string completionFlag = "act1Complete";
    public string nextScene = LevelTransitionManager.Level2;
    public string nextTitle = "Day 2";
    public List<RecapVariant> recaps = new List<RecapVariant>();
    public float transitionDelay = 2.5f;

    bool leaving;
    TimeOfDayKey currentKey;
    Coroutine timeBlend;

    void Awake()
    {
        if (!game) game = FindFirstObjectByType<JayGameManager>();
        if (!lighting && game) lighting = game.lighting;
    }

    void Start()
    {
        var state = StoryStateManager.Instance;
        if (!state.GetFlag(StoryKeys.GameStarted))
        {
            // Opened directly in the Editor: build the state of a player arriving from earlier levels.
            state.StartNewGame();
            StoryEffect.ApplyAll(directPlayDefaults, state);
        }
        state.SetInt(StoryKeys.Day, dayNumber);
        state.TakeLevelSnapshot();
        if (game)
        {
            game.Loop.SetDay(dayNumber);
            game.Loop.SetCash(state.GetInt(StoryKeys.Cash));
            game.LoopEvent += OnLoopEvent;
            game.MirrorLoopToStory();
        }
        if (lighting) lighting.autoAdvance = false;
        state.Changed += OnStateChanged;
        OnStateChanged();
        StoryHUD.ShowChapter(chapterTitle, chapterSubtitle);
    }

    void OnDestroy()
    {
        if (game) game.LoopEvent -= OnLoopEvent;
        if (StoryStateManager.Exists) StoryStateManager.Instance.Changed -= OnStateChanged;
    }

    void OnLoopEvent(string loopEvent)
    {
        var state = StoryStateManager.Instance;
        foreach (var map in loopEventEffects)
            if (map.loopEvent == loopEvent && StoryCondition.AllMet(map.conditions, state))
                StoryEffect.ApplyAll(map.effects, state);
    }

    void OnStateChanged()
    {
        var state = StoryStateManager.Instance;
        UpdateTimeOfDay(state);
        if (!leaving && !string.IsNullOrEmpty(completionFlag) && state.GetFlag(completionFlag))
        {
            leaving = true;
            StartCoroutine(Leave());
        }
    }

    // ------------------------------------------------------------------ time of day
    void UpdateTimeOfDay(StoryStateManager state)
    {
        if (!lighting || timeOfDay.Count == 0) return;
        TimeOfDayKey target = null;
        foreach (var key in timeOfDay) if (StoryCondition.AllMet(key.conditions, state)) target = key; // last match wins
        if (target == null || target == currentKey) return;
        bool first = currentKey == null;
        currentKey = target;
        if (timeBlend != null) StopCoroutine(timeBlend);
        if (first || target.blendSeconds <= 0) lighting.SetCycleFraction(target.cycleFraction);
        else timeBlend = StartCoroutine(BlendTime(target.cycleFraction, target.blendSeconds));
    }

    IEnumerator BlendTime(float to, float seconds)
    {
        float from = lighting.CycleFraction;
        if (to < from) to += 1;          // always move forward in time
        for (float t = 0; t < 1; t += Time.deltaTime / seconds)
        {
            lighting.SetCycleFraction(Mathf.Repeat(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t)), 1));
            yield return null;
        }
        lighting.SetCycleFraction(Mathf.Repeat(to, 1));
    }

    // ------------------------------------------------------------------ completion
    IEnumerator Leave()
    {
        yield return new WaitForSeconds(transitionDelay);
        while (DialogueManager.IsOpen || CutsceneController.IsPlaying) yield return null;
        if (string.IsNullOrEmpty(nextScene)) yield break;
        var state = StoryStateManager.Instance;
        string recap = "";
        foreach (var r in recaps) if (StoryCondition.AllMet(r.conditions, state)) { recap = r.recap; break; }
        LevelTransitionManager.LoadLevel(nextScene, nextTitle, recap);
    }
}

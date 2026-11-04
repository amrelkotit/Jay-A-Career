using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// NARRATIVE SCRIPT #5 — fires a story beat once (brief Step 6).
///
/// Activation modes:
///   • EnterZone  — the player walks into this trigger collider
///   • OnStart    — as soon as the level starts (and the conditions pass)
///   • WhenConditionsMet — the moment the story state satisfies the conditions
///                  (e.g. Hale returns as soon as notesFound becomes true)
///
/// When fired it can: apply effects (set flags, change trust/heat, add journal entries),
/// play a conversation, show/hide objects and raise a UnityEvent. The "fired" record lives in
/// <see cref="StoryStateManager"/> (flag "trigger.&lt;id&gt;"), so a beat can never repeat — even
/// after re-entering the zone or restarting from a snapshot taken after it fired.
/// </summary>
public class StoryTrigger : MonoBehaviour
{
    public enum Activation { EnterZone, OnStart, WhenConditionsMet }

    [Tooltip("Unique id; used for the 'fired' flag and the beat table.")]
    public string triggerId = "";
    public Activation activation = Activation.EnterZone;
    public bool fireOnce = true;
    [Tooltip("Delay in seconds between activation and the beat (gives cameras/audio time to settle).")]
    public float delay;

    [Header("Preconditions")]
    public List<StoryCondition> conditions = new List<StoryCondition>();
    [Header("Actions")]
    public List<StoryEffect> effects = new List<StoryEffect>();
    public DialogueData dialogue;
    [Tooltip("Camera focus during the conversation (optional).")]
    public Transform dialogueFocus;
    [System.Serializable] public class Move { public Transform target; public Transform destination; }
    [Tooltip("Teleport characters/props to marker transforms when the beat fires (e.g. Hale to the doorway).")]
    public List<Move> moves = new List<Move>();
    public GameObject[] activateObjects;
    public GameObject[] deactivateObjects;
    public AudioSource playAudio;
    public UnityEvent onFired;

    bool running;
    string FiredFlag => "trigger." + (string.IsNullOrEmpty(triggerId) ? gameObject.name : triggerId);

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void OnEnable() { if (activation == Activation.WhenConditionsMet) StoryStateManager.Instance.Changed += CheckConditions; }
    void OnDisable() { if (StoryStateManager.Exists) StoryStateManager.Instance.Changed -= CheckConditions; }

    void Start()
    {
        if (activation == Activation.OnStart || activation == Activation.WhenConditionsMet) TryFire();
    }

    void CheckConditions() => TryFire();

    void OnTriggerEnter(Collider other)
    {
        if (activation != Activation.EnterZone) return;
        if (other.GetComponentInParent<JayPlayerController>()) TryFire();
    }

    /// <summary>Fires the beat if its preconditions pass and it has not already fired.</summary>
    public bool TryFire()
    {
        var state = StoryStateManager.Instance;
        if (running || !isActiveAndEnabled) return false;
        if (fireOnce && state.GetFlag(FiredFlag)) return false;
        if (!StoryCondition.AllMet(conditions, state)) return false;
        running = true;
        // Record first so effects that change state cannot re-enter this trigger.
        if (fireOnce) state.SetFlag(FiredFlag, true);
        StartCoroutine(Fire());
        return true;
    }

    IEnumerator Fire()
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        // Never interrupt a running cutscene; wait for it to finish first.
        while (CutsceneController.IsPlaying) yield return null;
        foreach (var m in moves) if (m != null && m.target && m.destination) m.target.SetPositionAndRotation(m.destination.position, m.destination.rotation);
        foreach (var g in activateObjects) if (g) g.SetActive(true);
        foreach (var g in deactivateObjects) if (g) g.SetActive(false);
        if (playAudio) playAudio.Play();
        StoryEffect.ApplyAll(effects, StoryStateManager.Instance);
        onFired?.Invoke();
        if (dialogue)
        {
            var game = FindFirstObjectByType<JayGameManager>();
            if (game) game.StartStoryDialogue(dialogue, null, dialogueFocus);
            else if (DialogueManager.Instance) DialogueManager.Instance.Begin(dialogue);
        }
        running = false;
    }
}

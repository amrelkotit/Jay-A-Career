using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One objective in a level's ordered list.</summary>
[Serializable]
public class ObjectiveStep
{
    [Tooltip("Shown on the HUD, e.g. 'Seal Luka's order at the preparation bench'.")]
    public string text;
    [Tooltip("The step is available only when these conditions are met.")]
    public List<StoryCondition> showWhen = new List<StoryCondition>();
    [Tooltip("The step is finished as soon as ALL of these conditions are met.")]
    public List<StoryCondition> completeWhen = new List<StoryCondition>();
    [Tooltip("Optional world target for the on-screen objective marker.")]
    public Transform marker;
}

/// <summary>
/// NARRATIVE SCRIPT #4 — story-driven objectives (brief Step 5).
///
/// Each level authors an ordered list of <see cref="ObjectiveStep"/>s. Whenever the story
/// state changes, the manager picks the FIRST step that is available and not yet complete
/// and shows it on the HUD. Because the current objective is derived from the state rather
/// than advanced by hand, it can never fall out of sync with the story — exploring out of
/// order, restarting a level, or skipping a cutscene all still show the right objective.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    public List<ObjectiveStep> steps = new List<ObjectiveStep>();
    [Tooltip("Shown when every step is complete.")]
    public string allCompleteText = "";

    public ObjectiveStep Current { get; private set; }
    public event Action<ObjectiveStep> ObjectiveChanged;

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnEnable()
    {
        StoryStateManager.Instance.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (StoryStateManager.Exists) StoryStateManager.Instance.Changed -= Refresh;
    }

    /// <summary>Re-evaluates every step against the current story state.</summary>
    public void Refresh()
    {
        var state = StoryStateManager.Instance;
        ObjectiveStep next = null;
        foreach (var step in steps)
        {
            if (step == null || !StoryCondition.AllMet(step.showWhen, state)) continue;
            bool complete = step.completeWhen.Count > 0 && StoryCondition.AllMet(step.completeWhen, state);
            if (!complete) { next = step; break; }
        }
        if (next == Current) return;
        bool isNew = Current != null || next != null;
        Current = next;
        StoryHUD.SetObjective(next != null ? next.text : allCompleteText, next != null ? next.marker : null, isNew);
        ObjectiveChanged?.Invoke(next);
    }
}

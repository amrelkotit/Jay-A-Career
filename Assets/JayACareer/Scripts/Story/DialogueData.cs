using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>What a dialogue choice or conversation does to the existing delivery loop.</summary>
public enum DialogueLoopAction
{
    None,
    CompleteBriefing,   // Hale's briefing finished  -> JayDailyLoop.Brief()
    SellPackage,        // buyer pays "payment"      -> JayDailyLoop.ResolveStory(sold)
    RefusePackage       // no sale                    -> package must go back to the shelf
}

/// <summary>One line of a conversation. Optional conditions allow small per-choice variations.</summary>
[Serializable]
public class DialogueLine
{
    public CharacterProfile speaker;
    [TextArea(2, 5)] public string text;
    [Tooltip("Line is skipped unless all conditions are met (empty = always shown).")]
    public List<StoryCondition> conditions = new List<StoryCondition>();
}

/// <summary>A player choice shown after the last line. Choices are the dialogue tree's branches.</summary>
[Serializable]
public class DialogueChoice
{
    public string text;
    [Tooltip("Choice is hidden unless all conditions are met.")]
    public List<StoryCondition> conditions = new List<StoryCondition>();
    [Tooltip("Actions applied when the player picks this choice (set flags, change trust/heat...).")]
    public List<StoryEffect> effects = new List<StoryEffect>();
    public DialogueLoopAction loopAction = DialogueLoopAction.None;
    public int payment;
    [Tooltip("Optional follow-up conversation (the next node of the tree).")]
    public DialogueData next;
}

/// <summary>
/// NARRATIVE SCRIPT #2 (data) — a conversation stored as a ScriptableObject.
///
/// Why ScriptableObjects (GDD §7): they are native to Unity, editable in the Inspector
/// without touching code, reusable across scenes, need no extra package (unlike Ink/Yarn)
/// and keep story content separate from game logic — the brief's "common mistake" of
/// hard-coding dialogue inside scripts is avoided.
/// </summary>
[CreateAssetMenu(menuName = "Jay A Career/Dialogue", fileName = "Dialogue")]
public class DialogueData : ScriptableObject
{
    [Tooltip("Readable id used in the beat table, e.g. L1_HaleBriefing.")]
    public string id;
    public List<DialogueLine> lines = new List<DialogueLine>();
    public List<DialogueChoice> choices = new List<DialogueChoice>();
    [Tooltip("Actions applied when the conversation ends (after any choice).")]
    public List<StoryEffect> onComplete = new List<StoryEffect>();
    public DialogueLoopAction loopActionOnComplete = DialogueLoopAction.None;
    [Tooltip("When true the conversation can only ever be played once; afterwards it is skipped.")]
    public bool playOnce;

    /// <summary>State flag recording that a play-once conversation has been seen.</summary>
    public string SeenFlag => "dialogue." + (string.IsNullOrEmpty(id) ? name : id);
}

/// <summary>
/// A conversation a character can start, with preconditions. Characters keep a prioritised
/// list of these; the first whose conditions pass is played (dialogue tree root selection).
/// </summary>
[Serializable]
public class ConversationOption
{
    public string note;
    public List<StoryCondition> conditions = new List<StoryCondition>();
    public DialogueData dialogue;

    public bool IsAvailable(StoryStateManager state)
    {
        if (!dialogue) return false;
        if (dialogue.playOnce && state.GetFlag(dialogue.SeenFlag)) return false;
        return StoryCondition.AllMet(conditions, state);
    }
}

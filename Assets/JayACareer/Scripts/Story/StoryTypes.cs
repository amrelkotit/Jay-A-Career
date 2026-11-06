using System;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================================
//  Jay A Career — Assignment 3 narrative layer
//  StoryTypes.cs: small serializable building blocks shared by every narrative system.
//
//  The lecture's dialogue-tree model describes each node as having PRECONDITIONS
//  (what must be true before it can be used) and ACTIONS (what it changes afterwards).
//  StoryCondition is a precondition and StoryEffect is an action. Dialogue choices,
//  objectives, triggers, lore items and level exits all reuse these two types, so the
//  whole story can be authored as data instead of being hard-coded in scripts.
// =====================================================================================

/// <summary>Which kind of story value a condition inspects.</summary>
public enum StoryConditionType
{
    FlagIsTrue,      // flag "key" has been set
    FlagIsFalse,     // flag "key" has NOT been set
    IntAtLeast,      // integer "key" >= intValue
    IntBelow,        // integer "key" <  intValue
    TextEquals,      // text "key" == textValue (e.g. lukaDeal == "pushed")
    TextNotEquals    // text "key" != textValue
}

/// <summary>A single precondition evaluated against <see cref="StoryStateManager"/>.</summary>
[Serializable]
public class StoryCondition
{
    public StoryConditionType type = StoryConditionType.FlagIsTrue;
    public string key = "";
    public int intValue;
    public string textValue = "";

    public StoryCondition() { }
    public StoryCondition(StoryConditionType type, string key, int intValue = 0, string textValue = "")
    { this.type = type; this.key = key; this.intValue = intValue; this.textValue = textValue; }

    // Short factory helpers keep editor-authored content readable.
    public static StoryCondition Flag(string key) => new StoryCondition(StoryConditionType.FlagIsTrue, key);
    public static StoryCondition NotFlag(string key) => new StoryCondition(StoryConditionType.FlagIsFalse, key);
    public static StoryCondition Text(string key, string value) => new StoryCondition(StoryConditionType.TextEquals, key, 0, value);
    public static StoryCondition NotText(string key, string value) => new StoryCondition(StoryConditionType.TextNotEquals, key, 0, value);
    public static StoryCondition AtLeast(string key, int value) => new StoryCondition(StoryConditionType.IntAtLeast, key, value);
    public static StoryCondition Below(string key, int value) => new StoryCondition(StoryConditionType.IntBelow, key, value);

    public bool IsMet(StoryStateManager state)
    {
        if (state == null) return false;
        switch (type)
        {
            case StoryConditionType.FlagIsTrue: return state.GetFlag(key);
            case StoryConditionType.FlagIsFalse: return !state.GetFlag(key);
            case StoryConditionType.IntAtLeast: return state.GetInt(key) >= intValue;
            case StoryConditionType.IntBelow: return state.GetInt(key) < intValue;
            case StoryConditionType.TextEquals: return state.GetText(key) == textValue;
            case StoryConditionType.TextNotEquals: return state.GetText(key) != textValue;
            default: return false;
        }
    }

    /// <summary>True when every condition in the list is met (an empty list always passes).</summary>
    public static bool AllMet(IList<StoryCondition> conditions, StoryStateManager state)
    {
        if (conditions == null) return true;
        foreach (var c in conditions) if (c != null && !c.IsMet(state)) return false;
        return true;
    }
}

/// <summary>Which change a story effect makes.</summary>
public enum StoryEffectType
{
    SetFlag,        // flag "key" = true
    ClearFlag,      // flag "key" = false
    AddInt,         // integer "key" += intValue (clamped by the state manager for trust/heat)
    SetInt,         // integer "key" = intValue
    SetText,        // text "key" = textValue
    AddJournal,     // journal entry: key = id, textValue = "Title|Body"
    AddItem,        // inventory item: key = id, textValue = "Title|Description"
    ShowMessage     // short on-screen toast: textValue
}

/// <summary>A single action applied to <see cref="StoryStateManager"/> after a beat happens.</summary>
[Serializable]
public class StoryEffect
{
    public StoryEffectType type = StoryEffectType.SetFlag;
    public string key = "";
    public int intValue;
    [TextArea] public string textValue = "";

    public StoryEffect() { }
    public StoryEffect(StoryEffectType type, string key, int intValue = 0, string textValue = "")
    { this.type = type; this.key = key; this.intValue = intValue; this.textValue = textValue; }

    public static StoryEffect Set(string key) => new StoryEffect(StoryEffectType.SetFlag, key);
    public static StoryEffect Add(string key, int amount) => new StoryEffect(StoryEffectType.AddInt, key, amount);
    public static StoryEffect Text(string key, string value) => new StoryEffect(StoryEffectType.SetText, key, 0, value);
    public static StoryEffect Journal(string id, string title, string body) => new StoryEffect(StoryEffectType.AddJournal, id, 0, title + "|" + body);
    public static StoryEffect Item(string id, string title, string description) => new StoryEffect(StoryEffectType.AddItem, id, 0, title + "|" + description);
    public static StoryEffect SetNumber(string key, int value) => new StoryEffect(StoryEffectType.SetInt, key, value);
    public static StoryEffect Message(string text) => new StoryEffect(StoryEffectType.ShowMessage, "", 0, text);

    public void Apply(StoryStateManager state)
    {
        if (state == null) return;
        switch (type)
        {
            case StoryEffectType.SetFlag: state.SetFlag(key, true); break;
            case StoryEffectType.ClearFlag: state.SetFlag(key, false); break;
            case StoryEffectType.AddInt: state.AddInt(key, intValue); break;
            case StoryEffectType.SetInt: state.SetInt(key, intValue); break;
            case StoryEffectType.SetText: state.SetText(key, textValue); break;
            case StoryEffectType.AddJournal:
                int split = textValue.IndexOf('|');
                string title = split >= 0 ? textValue.Substring(0, split) : textValue;
                string body = split >= 0 ? textValue.Substring(split + 1) : "";
                state.AddJournalEntry(key, title, body);
                break;
            case StoryEffectType.AddItem:
                int cut = textValue.IndexOf('|');
                state.AddItem(key, cut >= 0 ? textValue.Substring(0, cut) : textValue, cut >= 0 ? textValue.Substring(cut + 1) : "");
                break;
            case StoryEffectType.ShowMessage: StoryHUD.Toast(textValue); break;
        }
    }

    public static void ApplyAll(IList<StoryEffect> effects, StoryStateManager state)
    {
        if (effects == null) return;
        foreach (var e in effects) e?.Apply(state);
    }
}

/// <summary>Well-known story keys, so code and authored data never disagree on spelling.</summary>
public static class StoryKeys
{
    // Integers / status values shown on the HUD
    public const string Cash = "cash", Trust = "trust", Heat = "heat", Day = "day";
    // Text values
    public const string LukaDeal = "lukaDeal";          // "", "accepted", "pushed", "refused"
    public const string LoopStage = "loop.stage";       // mirror of JayDailyLoop.Stage for conditions
    // Flags (see Docs/Story Bible.md §10)
    public const string GameStarted = "gameStarted";
    public const string IntroSeen = "introSeen", BriefingDone = "briefingDone", BadgeFound = "badgeFound",
        CartonSealed = "cartonSealed", LeoPaid = "leoPaid", Act1Complete = "act1Complete",
        LukaWarned = "lukaWarned", LukaDealt = "lukaDealt", PhoneNoteFound = "phoneNoteFound", HaleReacted = "haleReacted",
        HaleEvening = "haleEvening", ToldHale = "toldHale", HaleGift = "haleGift",
        Act2Complete = "act2Complete", HaleMissingSeen = "haleMissingSeen", NotesFound = "notesFound",
        ConfessionHeard = "confessionHeard", PaulMet = "paulMet", EndingSeen = "endingSeen";
    // Loop mirror flags
    public const string CanNegotiate = "loop.canNegotiate", PackageSold = "loop.sold", PackageReturned = "loop.returned",
        WorkspaceClean = "loop.cleaned";
    // Rent target from the story bible
    public const int RentDue = 400;
    public const int TotalDays = 2;          // Day 1: Leo (morning) and Luka (evening); Day 2: Paul
    // Thresholds at which Trust and Heat change what characters say and pay.
    public const int TrustHigh = 4, TrustLow = 2, HeatHigh = 3, HeatLow = 2;
}

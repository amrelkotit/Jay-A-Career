using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NARRATIVE SCRIPT #6 — an interactive story object (brief "Interactive narrative elements").
///
/// Examples: Hale's old Corvane badge, the "P." message on Luka's phone, Hale's ledger.
/// Pressing [E] adds it to the inventory with a lore description, optionally adds a journal
/// entry, applies effects (e.g. sets badgeFound) and can play a short inner-monologue line.
/// Items can require conditions (e.g. the phone can only be read after Luka arrives).
/// </summary>
public class LoreItem : ContextualInteractable
{
    [Header("Inventory")]
    public string itemId = "item";
    public string title = "Item";
    [TextArea(3, 8)] public string description = "";
    [Tooltip("Pick the object up (hide it) or only inspect it in place.")]
    public bool removeFromWorld = true;

    [Header("Journal (optional)")]
    public string journalTitle = "";
    [TextArea(3, 8)] public string journalBody = "";

    [Header("Story")]
    public List<StoryCondition> conditions = new List<StoryCondition>();
    [TextArea] public string lockedMessage = "";
    public List<StoryEffect> effects = new List<StoryEffect>();
    public DialogueData inspectDialogue;

    public override string DisplayName => title;
    public override string PromptText => Available ? "[E] " + (removeFromWorld ? "Take " : "Inspect ") + title : lockedMessage;

    bool Available => StoryCondition.AllMet(conditions, StoryStateManager.Instance) && !StoryStateManager.Instance.HasItem(itemId);

    void Start()
    {
        // If the item was already collected (e.g. after restarting a level) keep the world consistent.
        if (removeFromWorld && StoryStateManager.Instance.HasItem(itemId)) gameObject.SetActive(false);
    }

    public override void Interact(JayGameManager game)
    {
        var state = StoryStateManager.Instance;
        if (state.HasItem(itemId)) return;
        if (!StoryCondition.AllMet(conditions, state))
        { if (!string.IsNullOrEmpty(lockedMessage)) StoryHUD.Toast(lockedMessage); return; }
        state.AddItem(itemId, title, description);
        if (!string.IsNullOrEmpty(journalTitle)) state.AddJournalEntry("journal." + itemId, journalTitle, journalBody);
        StoryEffect.ApplyAll(effects, state);
        if (inspectDialogue && game) game.StartStoryDialogue(inspectDialogue, null, null);
        if (removeFromWorld) gameObject.SetActive(false);
    }
}

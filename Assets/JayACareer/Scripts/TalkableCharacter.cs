using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// A character the player can talk to. Assignment 3 adds a CharacterProfile (name + portrait)
/// and a prioritised list of story conversations: the first whose preconditions pass is played
/// by the DialogueManager. Without conversations the original A2 behaviour is used.
/// </summary>
public class TalkableCharacter : ContextualInteractable
{
    public string CharacterName="Character";
    public Transform headTarget;
    public CharacterProfile profile;
    public List<ConversationOption> conversations=new List<ConversationOption>();
    public string Name=>profile?profile.displayName:CharacterName;
    public override string DisplayName=>Name;
    public override string PromptText=>"[E] Talk to "+Name;
    public override void Interact(JayGameManager game){if(game)game.TalkTo(this);}
    /// <summary>First conversation whose conditions pass, or null.</summary>
    public DialogueData PickConversation()
    {
        var state=StoryStateManager.Instance;
        foreach(var option in conversations)if(option!=null&&option.IsAvailable(state))return option.dialogue;
        return null;
    }
}

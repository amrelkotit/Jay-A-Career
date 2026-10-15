using UnityEngine;
public class TalkableCharacter : ContextualInteractable
{
    public string CharacterName="Character";
    public Transform headTarget;
    public override string DisplayName=>CharacterName;
    public override string PromptText=>"[T] Talk to "+CharacterName;
    public override void Interact(JayGameManager game){if(game)game.TalkTo(this);}
}

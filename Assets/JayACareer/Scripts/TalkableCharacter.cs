using UnityEngine;
public class TalkableCharacter : ContextualInteractable
{
    public string CharacterName="Character"; public override string DisplayName=>CharacterName; public override string PromptText=>"[T] Talk"; public override void Interact(JayGameManager game){game.TalkTo(CharacterName);}
}

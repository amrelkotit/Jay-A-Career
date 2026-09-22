using UnityEngine;
public class CleanableStation : ContextualInteractable
{
    public override string DisplayName=>"Cleaning Station"; public override string PromptText=>"[C] Clean"; public override void Interact(JayGameManager game){game.CleanWorkspace();}
}

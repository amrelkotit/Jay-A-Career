using UnityEngine;
public class SimpleInteractable : ContextualInteractable
{
    public string Prompt="Interact"; public string ActionText="Interaction complete.";
    public override string DisplayName=>string.IsNullOrWhiteSpace(gameObject.name)?"Object":gameObject.name;
    public override string PromptText=>Prompt;
    public override void Interact(JayGameManager game){game.ShowMessage(ActionText);game.RegisterInteraction(name);}
}

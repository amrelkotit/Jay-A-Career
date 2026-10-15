using UnityEngine;
public class JayStation : ContextualInteractable
{
    public enum Action { Prepare, Sleep, Lights }
    public Action action;
    public override string PromptText => action == Action.Prepare ? "[O] Prepare / recover delivery package" : action == Action.Sleep ? "[O] Sleep" : "[O] Toggle laboratory lights";
    public override void Interact(JayGameManager game)
    {
        if (action == Action.Prepare) game.Prepare();
        else if (action == Action.Sleep) game.Sleep();
        else game.ShowMessage(game.lighting.ToggleWorkspaceLights() ? "Laboratory lights on." : "Laboratory lights off.");
    }
}

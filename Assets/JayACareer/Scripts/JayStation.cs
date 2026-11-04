using UnityEngine;
public class JayStation : ContextualInteractable
{
    public enum Action { Prepare, Sleep, Lights, BedroomLights }
    public Action action;
    public override string PromptText => action == Action.Prepare ? "[E] Prepare / recover delivery package" : action == Action.Sleep ? "[E] Sleep" : action == Action.BedroomLights ? "[E] Switch bedroom light on / off" : "[E] Toggle laboratory lights";
    public override void Interact(JayGameManager game)
    {
        if (action == Action.Prepare) game.Prepare();
        else if (action == Action.Sleep) game.Sleep();
        else if (action == Action.BedroomLights) game.ShowMessage(game.lighting.ToggleBedroomLights() ? "Bedroom light on." : "Bedroom light off.");
        else game.ShowMessage(game.lighting.ToggleWorkspaceLights() ? "Laboratory lights on." : "Laboratory lights off.");
    }
}

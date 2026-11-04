using UnityEngine;
public class JayPackageDock : ContextualInteractable
{
    public JayDailyLoop.PackageLocation destination = JayDailyLoop.PackageLocation.Counter;
    public override string PromptText => destination == JayDailyLoop.PackageLocation.Counter ? "Place the sealed package here [E]" : "Return an unsold package here [E]";
    public override void Interact(JayGameManager game) { game.ShowMessage(PromptText); }
}

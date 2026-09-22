using UnityEngine;
public class DoorInteractable : ContextualInteractable
{
    public override string DisplayName=>"Door"; public override string PromptText=>IsOpen?"[Q] Close":"[O] Open"; public bool IsOpen{get;private set;} public bool HideVisual;
    Quaternion closedRotation; Quaternion openRotation; Vector3 closedPosition; Collider[] doorColliders; Renderer[] doorRenderers;
    void Awake(){closedRotation=transform.localRotation;closedPosition=transform.localPosition;openRotation=closedRotation*Quaternion.Euler(0,-90,0);doorColliders=GetComponentsInChildren<Collider>(true);doorRenderers=GetComponentsInChildren<Renderer>(true);if(HideVisual)foreach(var r in doorRenderers)if(r!=null)r.enabled=false;}
    public override void Interact(JayGameManager game){if(IsOpen)Close(game);else Open(game);} public void Open(JayGameManager game){IsOpen=true;transform.localRotation=openRotation;transform.localPosition=closedPosition+new Vector3(1.05f,0,0);foreach(var c in doorColliders)if(c!=null)c.enabled=false;game.ShowMessage("Door opened.");} public void Close(JayGameManager game){IsOpen=false;transform.localRotation=closedRotation;transform.localPosition=closedPosition;foreach(var c in doorColliders)if(c!=null)c.enabled=true;foreach(var r in doorRenderers)if(r!=null)r.enabled=!HideVisual;game.ShowMessage("Door closed.");}
}

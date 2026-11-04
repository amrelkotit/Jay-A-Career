using UnityEngine;
public class DoorInteractable : ContextualInteractable
{
    public override string DisplayName=>"Door";
    public override string PromptText=>IsOpen?"[E] Close door":Locked?lockedMessage:"[E] Open door";
    [Header("Assignment 3: story lock")]
    [Tooltip("The door stays locked until these story conditions are met (e.g. briefingDone).")]
    public System.Collections.Generic.List<StoryCondition> unlockConditions=new System.Collections.Generic.List<StoryCondition>();
    public string lockedMessage="Locked.";
    public bool Locked=>unlockConditions.Count>0&&!StoryCondition.AllMet(unlockConditions,StoryStateManager.Instance);
    public bool IsOpen {get;private set;}
    public bool HideVisual;
    public float openAngle=-95;
    public AudioSource doorAudio;
    public AudioClip doorSound;
    Quaternion closedRotation;
    Collider[] doorColliders;
    void Awake(){closedRotation=transform.localRotation;doorColliders=GetComponentsInChildren<Collider>();}
    public override void Interact(JayGameManager game){if(IsOpen)Close(game);else Open(game);}
    public void Open(JayGameManager game)
    {
        if(IsOpen)return;
        if(Locked){game.ShowMessage(lockedMessage);return;}
        // Keep the collider on the panel as it swings around the authored hinge.
        IsOpen=true;transform.localRotation=closedRotation*Quaternion.Euler(0,openAngle,0);
        foreach(var c in doorColliders)c.enabled=true;
        if(doorAudio&&doorSound)doorAudio.PlayOneShot(doorSound,.3f);
        game.ShowMessage("Door opened.");
    }
    public void Close(JayGameManager game)
    {
        if(!IsOpen)return;
        Quaternion old=transform.localRotation;
        transform.localRotation=closedRotation;
        Physics.SyncTransforms();
        foreach(var c in doorColliders)
        {
            var bounds=c.bounds;
            foreach(var overlap in Physics.OverlapBox(bounds.center,bounds.extents*.95f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
            {
                if(overlap.GetComponentInParent<JayPlayerController>() || overlap.GetComponentInParent<LiftableObject>())
                {transform.localRotation=old;game.ShowMessage("Clear the doorway before closing it.");return;}
            }
        }
        IsOpen=false;foreach(var c in doorColliders)c.enabled=true;
        if(doorAudio&&doorSound)doorAudio.PlayOneShot(doorSound,.3f);
        game.ShowMessage("Door closed.");
    }
}

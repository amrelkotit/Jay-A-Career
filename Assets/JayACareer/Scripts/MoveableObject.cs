using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>A bounded bedroom furniture layout tool with reversible, collision checked placement.</summary>
public class MoveableObject : ContextualInteractable
{
    public bool IsPlaced { get; private set; }
    public override string PromptText => "[O] Arrange chair: WASD move, Q/R rotate, Enter confirm, Esc cancel";
    Vector3 startPosition; Quaternion startRotation; bool editing; JayGameManager owner; Collider shape;
    public override void Interact(JayGameManager game)
    {
        if(editing || game.CarriedObject || game.DialogueActive)return;
        startPosition=transform.position;startRotation=transform.rotation;editing=true;IsPlaced=false;owner=game;
        shape=GetComponent<Collider>(); if(shape)shape.enabled=false;game.BeginPlacement(this);
    }
    bool Clear()
    {
        var box=shape as BoxCollider;
        if(!box)return true;
        Vector3 center=transform.TransformPoint(box.center);
        var half=Vector3.Scale(box.size,transform.lossyScale)*.47f;
        return !Physics.CheckBox(center,half,transform.rotation,~0,QueryTriggerInteraction.Ignore);
    }
    public void Cancel()
    {
        transform.SetPositionAndRotation(startPosition,startRotation);Finish();owner.ShowMessage("Chair arrangement cancelled.");
    }
    void Finish(){editing=false;IsPlaced=true;if(shape)shape.enabled=true;owner.activePlacement=null;}
    public void UpdatePlacement()
    {
        if(!editing||Keyboard.current==null)return;
        var k=Keyboard.current;
        Vector3 input=new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
        Vector3 next=transform.position+input.normalized*1.5f*Time.deltaTime;
        next.x=Mathf.Clamp(next.x,8,11.5f);next.z=Mathf.Clamp(next.z,6.3f,10.6f);transform.position=next;
        if(k.qKey.isPressed)transform.Rotate(Vector3.up,-75*Time.deltaTime);
        if(k.rKey.isPressed)transform.Rotate(Vector3.up,75*Time.deltaTime);
        if(k.enterKey.wasPressedThisFrame)
        {if(Clear()){Finish();owner.ShowMessage("Chair arrangement confirmed.");}else owner.ShowMessage("Chair overlaps another object. Move it into a clear space.");}
    }
}

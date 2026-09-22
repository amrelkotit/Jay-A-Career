using UnityEngine;
using UnityEngine.InputSystem;
public class MoveableObject : ContextualInteractable
{
    public string Prompt="[O] Move / Rotate\nWASD Move  Q/R Rotate\nEnter Confirm  Esc Cancel"; public bool IsPlaced; Vector3 startPosition; Quaternion startRotation; bool editing; public override string PromptText=>Prompt;
    void Start(){startPosition=transform.position;startRotation=transform.rotation;} public override void Interact(JayGameManager game){if(!editing){editing=true;game.BeginPlacement(this);}}
    public void UpdatePlacement(){if(!editing||Keyboard.current==null)return;Vector3 input=new Vector3((Keyboard.current.dKey.isPressed?1:0)-(Keyboard.current.aKey.isPressed?1:0),0,(Keyboard.current.wKey.isPressed?1:0)-(Keyboard.current.sKey.isPressed?1:0));transform.position+=input*2.5f*Time.deltaTime;if(Keyboard.current.qKey.isPressed)transform.Rotate(Vector3.up,-90f*Time.deltaTime);if(Keyboard.current.rKey.isPressed)transform.Rotate(Vector3.up,90f*Time.deltaTime);if(Keyboard.current.enterKey.wasPressedThisFrame||(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)){editing=false;IsPlaced=true;}if(Keyboard.current.escapeKey.wasPressedThisFrame){transform.position=startPosition;transform.rotation=startRotation;editing=false;}}
}

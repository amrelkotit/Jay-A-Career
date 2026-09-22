using UnityEngine;
public class LiftableObject : ContextualInteractable
{
    public string DisplayLabel="Object"; public override string DisplayName=>string.IsNullOrWhiteSpace(DisplayLabel)?gameObject.name:DisplayLabel; public override string PromptText=>"[L] Pick Up";
    [HideInInspector] public bool IsCarried; Rigidbody body; Collider[] colliders;
    public override void Interact(JayGameManager game){game.BeginCarry(this);}
    public void PickUp(Transform carryPoint){if(IsCarried)return;IsCarried=true;foreach(Transform child in GetComponentsInChildren<Transform>(true))if(child.name.EndsWith("_LiftMarker"))child.gameObject.SetActive(false);body=GetComponent<Rigidbody>();if(body){body.isKinematic=true;body.detectCollisions=false;}colliders=GetComponentsInChildren<Collider>();foreach(var c in colliders)c.enabled=false;transform.SetParent(carryPoint);transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;}
    public void Drop(Vector3 position,Quaternion rotation){transform.SetParent(null);transform.position=position;transform.rotation=rotation;IsCarried=false;foreach(Transform child in GetComponentsInChildren<Transform>(true))if(child.name.EndsWith("_LiftMarker"))child.gameObject.SetActive(true);foreach(var c in colliders??GetComponentsInChildren<Collider>())c.enabled=true;if(body){body.detectCollisions=true;body.isKinematic=false;}}
    public void RotateCarried(){if(IsCarried)transform.Rotate(Vector3.up,45f,Space.World);}
}

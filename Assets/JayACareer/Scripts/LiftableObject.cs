using UnityEngine;
public class LiftableObject : ContextualInteractable
{
    public string DisplayLabel="Object"; public override string DisplayName=>string.IsNullOrWhiteSpace(DisplayLabel)?gameObject.name:DisplayLabel; public override string PromptText=>DisplayName+"\n[L] Lift   [R] Rotate while carrying";
    [HideInInspector] public bool IsCarried; Rigidbody body; Collider[] colliders; float carriedYaw;
    public override void Interact(JayGameManager game){game.BeginCarry(this);}
    public void PickUp(Transform carryPoint){if(IsCarried)return;IsCarried=true;carriedYaw=0;foreach(Transform child in GetComponentsInChildren<Transform>(true))if(child.name.EndsWith("_LiftMarker"))child.gameObject.SetActive(false);body=GetComponent<Rigidbody>();if(body){body.isKinematic=true;body.detectCollisions=false;}colliders=GetComponentsInChildren<Collider>();foreach(var c in colliders)c.enabled=false;transform.SetParent(carryPoint);transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;}
    public void Drop(Vector3 position,Quaternion rotation){transform.SetParent(null);transform.position=position;transform.rotation=rotation;IsCarried=false;foreach(Transform child in GetComponentsInChildren<Transform>(true))if(child.name.EndsWith("_LiftMarker"))child.gameObject.SetActive(true);foreach(var c in colliders??GetComponentsInChildren<Collider>())c.enabled=true;if(body){body.detectCollisions=true;body.isKinematic=false;}}
    public void ResetAt(Transform slot){if(!slot)return;transform.SetParent(null);transform.SetPositionAndRotation(slot.position,slot.rotation);IsCarried=false;foreach(var c in GetComponentsInChildren<Collider>(true))c.enabled=true;if(TryGetComponent<Rigidbody>(out var rb)){rb.isKinematic=true;rb.detectCollisions=true;}}
    public void RotateCarried(){if(IsCarried){carriedYaw+=45;transform.localRotation=Quaternion.Euler(0,carriedYaw,0);}}
}

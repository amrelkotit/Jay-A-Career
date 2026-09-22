using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class JayPlayerController : MonoBehaviour
{
    public float speed = 5f, lookSpeed = 2f, gravity = -18f, interactionRange = 3f;
    CharacterController controller; Camera cam; float pitch; JayGameManager game; Vector3 spawnPosition; ContextualInteractable highlightedTarget; Renderer highlightedRenderer; Color highlightedColor;
    [Header("Playable Area Safety")]
    public float minX = -22f, maxX = 22f, minZ = -3f, maxZ = 24f, fallRespawnY = -3f;
    void Start() { controller = GetComponent<CharacterController>(); cam = GetComponentInChildren<Camera>(); game = FindFirstObjectByType<JayGameManager>(); spawnPosition = transform.position; Cursor.lockState = CursorLockMode.Locked; }
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
        if (Cursor.lockState != CursorLockMode.Locked) return;
        Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero; transform.Rotate(Vector3.up, mouseDelta.x * lookSpeed * .05f);
        pitch = Mathf.Clamp(pitch - mouseDelta.y * lookSpeed * .05f, -80, 80); cam.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        Vector2 input = Vector2.zero; if (Keyboard.current != null) input = new Vector2((Keyboard.current.dKey.isPressed?1:0)-(Keyboard.current.aKey.isPressed?1:0),(Keyboard.current.wKey.isPressed?1:0)-(Keyboard.current.sKey.isPressed?1:0));
        Vector3 move = transform.right * input.x + transform.forward * input.y; if (move.sqrMagnitude > 1) move.Normalize(); controller.Move(move * speed * Time.deltaTime); if (!controller.isGrounded) controller.Move(Vector3.up * gravity * Time.deltaTime); KeepPlayerInBounds();
        var carried = game != null ? game.CarriedObject : null;
        if (carried != null && carried.IsCarried)
        {
            game.SetPrompt("[L] Drop " + carried.DisplayName + "\n[R] Rotate");
            if (Pressed(Key.L)) game.DropCarried();
            if (Pressed(Key.R)) carried.RotateCarried();
            ClearHighlight();
            return;
        }
        if (game != null && game.DialogueActive)
        {
            game.SetPrompt("Choose a response: [A] Accept   [C] Counter   [R] Reject");
            if (Pressed(Key.A)) game.ResolveDialogue("accept");
            else if (Pressed(Key.C)) game.ResolveDialogue("counter");
            else if (Pressed(Key.R)) game.ResolveDialogue("reject");
            ClearHighlight();
            return;
        }
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.SphereCast(ray, .45f, out RaycastHit hit, interactionRange))
        {
            ContextualInteractable target = FindContextualTarget(hit.collider);
            if (target == null) target = FindNearbyDoorTarget();
            if (target != null)
            {
                game.SetPrompt(ContextPrompt(target));
                SetHighlight(target);
                HandleInteraction(target);
            }
            else { game.SetPrompt(""); ClearHighlight(); }
        }
        else
        {
            var nearbyDoor = FindNearbyDoorTarget();
            if (nearbyDoor != null) { game.SetPrompt(ContextPrompt(nearbyDoor)); SetHighlight(nearbyDoor); HandleInteraction(nearbyDoor); }
            else { game.SetPrompt(""); ClearHighlight(); }
        }
    }
    ContextualInteractable FindNearbyDoorTarget()
    {
        var door = FindFirstObjectByType<DoorInteractable>();
        if (door == null) return null;
        Vector3 toDoor = door.transform.position - cam.transform.position;
        if (toDoor.magnitude <= interactionRange + .8f && Vector3.Dot(cam.transform.forward, toDoor.normalized) > .15f) return door;
        return null;
    }
    void KeepPlayerInBounds()
    {
        if (transform.position.y < fallRespawnY) { controller.enabled=false; transform.position=spawnPosition; controller.enabled=true; if(game!=null)game.ShowMessage("Jay returned to the safe area."); return; }
        Vector3 p=transform.position; p.x=Mathf.Clamp(p.x,minX,maxX); p.z=Mathf.Clamp(p.z,minZ,maxZ); transform.position=p;
    }
    ContextualInteractable FindContextualTarget(Collider hitCollider)
    {
        var go = hitCollider.GetComponentInParent<Transform>();
        if (go == null) return null;
        var lift = go.GetComponentInParent<LiftableObject>(); if (lift != null) return lift;
        var door = go.GetComponentInParent<DoorInteractable>(); if (door != null) return door;
        var talk = go.GetComponentInParent<TalkableCharacter>(); if (talk != null) return talk;
        var clean = go.GetComponentInParent<CleanableStation>(); if (clean != null) return clean;
        var move = go.GetComponentInParent<MoveableObject>(); if (move != null) return move;
        return go.GetComponentInParent<SimpleInteractable>();
    }
    string ContextPrompt(ContextualInteractable target)
    {
        if (target is LiftableObject lift) return "[L] Lift " + lift.DisplayName;
        if (target is DoorInteractable door) return door.IsOpen ? "[Q] Close Door" : "[O] Open Door";
        if (target is TalkableCharacter talk) return "[T] Talk to " + talk.CharacterName;
        return target.PromptText;
    }
    void SetHighlight(ContextualInteractable target)
    {
        if (highlightedTarget == target) return;
        ClearHighlight(); highlightedTarget = target; highlightedRenderer = target.GetComponentInChildren<Renderer>();
        if (highlightedRenderer != null) { highlightedColor = highlightedRenderer.material.color; highlightedRenderer.material.color = Color.Lerp(highlightedColor, new Color(1f, .82f, .28f), .32f); }
    }
    void ClearHighlight()
    {
        if (highlightedRenderer != null) highlightedRenderer.material.color = highlightedColor;
        highlightedTarget = null; highlightedRenderer = null;
    }
    void HandleInteraction(ContextualInteractable target)
    {
        if (target is LiftableObject lift)
        {
            if (lift.IsCarried) { if (Pressed(Key.L)) game.DropCarried(); if (Pressed(Key.R)) lift.RotateCarried(); }
            else if (Pressed(Key.L)) game.BeginCarry(lift);
        }
        else if (target is DoorInteractable door)
        {
            if (!door.IsOpen && Pressed(Key.O)) door.Open(game); else if (door.IsOpen && Pressed(Key.Q)) door.Close(game);
        }
        else if (target is TalkableCharacter && Pressed(Key.T)) target.Interact(game);
        else if (target is CleanableStation && Pressed(Key.C)) target.Interact(game);
        else if (target is MoveableObject && Pressed(Key.O)) target.Interact(game);
        else if (target is SimpleInteractable && Pressed(Key.O)) target.Interact(game);
    }
    bool Pressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
}

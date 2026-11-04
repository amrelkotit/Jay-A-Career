using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class JayPlayerController : MonoBehaviour
{
    public float speed=5, lookSpeed=2, gravity=-18, interactionRange=3;
    public float minX=-22, maxX=22, minZ=-3, maxZ=24, fallRespawnY=-3;
    public Camera outputCamera;
    public JayGameManager game;
    public Transform bodyVisual;
    public AudioSource footsteps;
    public AudioClip asphaltStep, concreteStep, sandStep;
    CharacterController controller;
    JayCameraRig rig;
    Vector3 spawn;
    float pitch, verticalSpeed, stepTimer;
    public float MovementAmount {get; private set;}
    public void SetPresentationMovement(float value){MovementAmount=Mathf.Clamp01(value);}
    void Start()
    {
        controller=GetComponent<CharacterController>(); spawn=transform.position;
        if(!game)game=FindFirstObjectByType<JayGameManager>();
        if(!outputCamera)outputCamera=GetComponentInChildren<Camera>();
        rig=GetComponent<JayCameraRig>(); if(!rig)rig=gameObject.AddComponent<JayCameraRig>();
        rig.Initialize(outputCamera);
        Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
    }
    void Update()
    {
        if(!game || !outputCamera)return;
        // Assignment 3: cutscenes and the notebook own the keyboard while they are open.
        if(CutsceneController.IsPlaying){MovementAmount=0;game.SetPrompt("");return;}
        if(NotebookUI.IsOpen){MovementAmount=0;return;}
        if(Pressed(Key.Escape)){if(game.activePlacement!=null)game.activePlacement.Cancel();else game.TogglePause();return;}
        if(game.IsPaused)return;
        if(game.IsPerformingAction){MovementAmount=0;game.SetPrompt("Action in progress...");return;}
        // Story conversations handle their own keys inside DialogueManager.
        if(DialogueManager.IsOpen){MovementAmount=0;game.SetPrompt("");return;}
        // Dialogue owns response keys before movement, cleaning or rotation.
        if(game.DialogueActive)
        {
            MovementAmount=0;
            if(game.CustomerDialogue)
            {
                game.SetPrompt("[A] $50   [C] $75 / 50% chance   [R] Reject");
                if(Pressed(Key.A))game.ResolveDialogue("accept");
                else if(Pressed(Key.C))game.ResolveDialogue("counter");
                else if(Pressed(Key.R))game.ResolveDialogue("reject");
            }
            else {game.SetPrompt("[T] Continue briefing");if(Pressed(Key.T))game.ContinueBriefing();}
            return;
        }
        if(game.activePlacement!=null)return;
        if(Cursor.lockState!=CursorLockMode.Locked)return;
        UpdateWorldInput();
    }
    void UpdateWorldInput()
    {
        Vector2 delta=Mouse.current!=null?Mouse.current.delta.ReadValue():Vector2.zero;
        float sensitivity=lookSpeed*.05f*SettingsManager.MouseSensitivity;
        transform.Rotate(Vector3.up,delta.x*sensitivity);
        pitch=Mathf.Clamp(pitch-delta.y*sensitivity*(SettingsManager.InvertY?-1:1),-80,80);
        rig.LookPivot.localEulerAngles=new Vector3(pitch,0,0);
        Vector2 input=Keyboard.current==null?Vector2.zero:new Vector2((Held(Key.D)?1:0)-(Held(Key.A)?1:0),(Held(Key.W)?1:0)-(Held(Key.S)?1:0));
        input=Vector2.ClampMagnitude(input,1); MovementAmount=input.magnitude;
        verticalSpeed=controller.isGrounded?-2:verticalSpeed+gravity*Time.deltaTime;
        if(controller.enabled)controller.Move((transform.right*input.x*speed+transform.forward*input.y*speed+Vector3.up*verticalSpeed)*Time.deltaTime);
        KeepInBounds();
        if(Pressed(Key.V))rig.ToggleView(game);
        if(bodyVisual)bodyVisual.gameObject.SetActive(rig.IsThirdPerson);
        if(Pressed(Key.F)){game.lighting.ToggleFlashlight();game.ShowMessage(game.lighting.FlashlightOn?"Flashlight on.":"Flashlight off.");}
        UpdateFootsteps();
        // One interact key: [E]. The A2 keys (L, T, O, C, Q) still work as aliases.
        bool use=Pressed(Key.E)&&DialogueManager.LastClosedFrame!=Time.frameCount;
        if(game.CarriedObject)
        {
            game.SetPrompt("[E] Place "+game.CarriedObject.DisplayName+"   [R] Rotate\nAim at the counter or return shelf to hand off the package.");
            if(Pressed(Key.R))game.CarriedObject.RotateCarried();
            if(use||Pressed(Key.L))game.DropCarried();
            if(Pressed(Key.T))game.ShowMessage("Look down at the customer counter and press [E] to place the package. Then look at the customer and press [E].");
            return;
        }
        var ray=new Ray(outputCamera.transform.position,outputCamera.transform.forward);
        if(Physics.SphereCast(ray,.1f,out var hit,interactionRange))
        {
            var target=hit.collider.GetComponentInParent<ContextualInteractable>();
            game.SetPrompt(target==game.customer?game.CustomerPrompt:target?target.PromptText:"");
            if(!target)return;
            if(target is LiftableObject && (use||Pressed(Key.L)))target.Interact(game);
            else if(target is TalkableCharacter && (use||Pressed(Key.T)))target.Interact(game);
            else if(target is CleanableStation && (use||Pressed(Key.C)))target.Interact(game);
            else if(target is DoorInteractable door){if(use)door.Interact(game);else if(Pressed(Key.O))door.Open(game);else if(Pressed(Key.Q))door.Close(game);}
            else if((target is JayStation || target is SimpleInteractable || target is MoveableObject) && (use||Pressed(Key.O)))target.Interact(game);
            else if(use)target.Interact(game);   // LoreItem and any other story interactable
        }
        else game.SetPrompt("");
    }
    void KeepInBounds()
    {
        var p=transform.position;
        if(p.y<fallRespawnY){controller.enabled=false;transform.position=spawn;controller.enabled=true;verticalSpeed=0;game.ShowMessage("Returned to the safe path.");return;}
        p.x=Mathf.Clamp(p.x,minX,maxX);p.z=Mathf.Clamp(p.z,minZ,maxZ);transform.position=p;
    }
    void UpdateFootsteps()
    {
        stepTimer-=Time.deltaTime;
        if(MovementAmount<.1f || !controller.isGrounded || stepTimer>0 || !footsteps)return;
        AudioClip clip=sandStep;
        if(Physics.Raycast(transform.position+Vector3.up*.1f,Vector3.down,out var floor,2))
        {
            var surface=floor.collider.GetComponent<JaySurface>();
            if(surface)clip=surface.kind==JaySurface.Kind.Asphalt?asphaltStep:surface.kind==JaySurface.Kind.Concrete?concreteStep:sandStep;
        }
        if(clip)footsteps.PlayOneShot(clip,.22f);stepTimer=.43f;
    }
    bool Pressed(Key key)=>Keyboard.current!=null&&Keyboard.current[key].wasPressedThisFrame;
    bool Held(Key key)=>Keyboard.current!=null&&Keyboard.current[key].isPressed;
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene references and presentation for the deterministic session rules.</summary>
public class JayGameManager : MonoBehaviour
{
    public int day = 1, money = 100;
    public string objective;
    public Text titleText, dayText, cashText, objectiveText, promptText, messageText, dialogueText;
    public MoveableObject activePlacement;
    public JayDailyLoop Loop { get; private set; } = new JayDailyLoop();
    [Header("Authored references")]
    public JayPlayerController player;
    public Camera playerCamera;
    public JayLightingController lighting;
    public LiftableObject deliveryPackage;
    public Transform preparationSlot, counterSlot, returnSlot, carryPoint;
    public TalkableCharacter hale, customer;
    public GameObject workspaceClutter, pausePanel;
    public Renderer customerClothing;
    public Material customerVariantA, customerVariantB;
    public AudioSource feedbackAudio;
    public AudioClip confirmSound, placementSound;
    public LiftableObject CarriedObject { get; private set; }
    public bool DialogueActive { get; private set; }
    public bool CustomerDialogue { get; private set; }
    public bool IsPaused { get; private set; }
    float messageTimer, recoveryTimer;
    void Start()
    {
        Time.timeScale = 1;
        Application.targetFrameRate = 60;
        if (deliveryPackage) deliveryPackage.gameObject.SetActive(false);
        if (workspaceClutter) workspaceClutter.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (dialogueText) dialogueText.text = "";
        RefreshHud();
        ShowMessage("Day 1. Speak to Dr. Hale inside the laboratory.");
    }
    void Update()
    {
        RefreshHud();
        if (IsPaused) return;
        if (activePlacement != null) { var placement = activePlacement; placement.UpdatePlacement(); if (placement.IsPlaced) activePlacement = null; }
        if (messageTimer > 0 && (messageTimer -= Time.deltaTime) <= 0 && messageText) messageText.text = "";
        if ((recoveryTimer -= Time.deltaTime) <= 0)
        {
            recoveryTimer = 1;
            if (deliveryPackage && deliveryPackage.gameObject.activeSelf && !deliveryPackage.IsCarried)
            {
                Vector3 p = deliveryPackage.transform.position;
                if (p.y < -2 || p.x < -22 || p.x > 22 || p.z < -3 || p.z > 24) RecoverPackage();
            }
        }
    }
    void LateUpdate()
    {
        if(!carryPoint||!player||!playerCamera)return;
        bool third=player.GetComponent<JayCameraRig>().IsThirdPerson;
        Transform parent=third?player.transform:playerCamera.transform;
        if(carryPoint.parent!=parent)carryPoint.SetParent(parent,false);
        carryPoint.localPosition=third?new Vector3(.05f,1.1f,.6f):new Vector3(.22f,-.25f,1.1f);
        carryPoint.localRotation=Quaternion.identity;
    }
    void RefreshHud()
    {
        day = Loop.Day; money = Loop.Cash; objective = Loop.Objective;
        if (dayText) dayText.text = "DAY " + day;
        if (cashText) cashText.text = "$" + money;
        if (objectiveText) objectiveText.text = objective;
    }
    public void Prepare()
    {
        if (Loop.Prepare())
        {
            deliveryPackage.ResetAt(preparationSlot);
            deliveryPackage.gameObject.SetActive(true);
            workspaceClutter.SetActive(true);
            Play(confirmSound);
        }
        else if ((Loop.Current == JayDailyLoop.Stage.Delivery || Loop.Current == JayDailyLoop.Stage.Return) && Loop.Package != JayDailyLoop.PackageLocation.Carried)
        { RecoverPackage(); return; }
        ShowMessage(Loop.Feedback);
    }
    public void RecoverPackage()
    {
        if (Loop.Recover()) { deliveryPackage.ResetAt(preparationSlot); deliveryPackage.gameObject.SetActive(true); }
        ShowMessage(Loop.Feedback);
    }
    public void BeginCarry(LiftableObject item)
    {
        if (IsPaused || DialogueActive || CarriedObject || !carryPoint) return;
        if (item == deliveryPackage && !Loop.PickUp()) { ShowMessage(Loop.Feedback); return; }
        CarriedObject = item; item.PickUp(carryPoint); Play(placementSound);
        ShowMessage(item.DisplayName + " picked up. [L] Place  [R] Rotate");
    }
    public bool PlacePackageAt(JayDailyLoop.PackageLocation destination)
    {
        if (CarriedObject != deliveryPackage || !Loop.Drop(destination)) { ShowMessage(Loop.Feedback); return false; }
        Transform slot = destination == JayDailyLoop.PackageLocation.Counter ? counterSlot : destination == JayDailyLoop.PackageLocation.Returned ? returnSlot : preparationSlot;
        deliveryPackage.Drop(slot.position, slot.rotation); CarriedObject = null;
        Play(placementSound); ShowMessage(Loop.Feedback); return true;
    }
    public void DropCarried()
    {
        if (!CarriedObject || !playerCamera) return;
        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.SphereCast(ray, .08f, out var hit, player.interactionRange))
        {
            var dock = hit.collider.GetComponentInParent<JayPackageDock>();
            if (CarriedObject == deliveryPackage && dock) { PlacePackageAt(dock.destination); return; }
        }
        Vector3 ahead = player.transform.position + player.transform.forward * 1.25f;
        var item = CarriedObject;
        var coll = item.GetComponent<Collider>();
        Quaternion placementRotation=Quaternion.Euler(0,item.transform.eulerAngles.y,0);
        float halfHeight = coll is BoxCollider box ? box.size.y * item.transform.lossyScale.y * .5f : .3f;
        Vector3 centerOffset = coll is BoxCollider cb ? placementRotation * Vector3.Scale(cb.center, item.transform.lossyScale) : Vector3.zero;
        Vector3 candidate;
        if (hit.collider && hit.normal.y > .65f) candidate = hit.point + Vector3.up * (halfHeight + .025f) - centerOffset;
        else if (Physics.Raycast(ahead + Vector3.up * 2, Vector3.down, out var floor, 5)) candidate = floor.point + Vector3.up * (halfHeight + .025f) - centerOffset;
        else { ShowMessage("Aim at a clear floor or work surface to place this object."); return; }
        Vector3 half = coll is BoxCollider b ? Vector3.Scale(b.size, item.transform.lossyScale) * .48f : Vector3.one * .25f;
        if (Physics.CheckBox(candidate + centerOffset, half, placementRotation, ~0, QueryTriggerInteraction.Ignore))
        { ShowMessage("The placement space is blocked. Aim at a clear surface."); return; }
        if (item == deliveryPackage && !Loop.Drop(JayDailyLoop.PackageLocation.World)) { ShowMessage(Loop.Feedback); return; }
        item.Drop(candidate, placementRotation); CarriedObject = null;
        Play(placementSound); ShowMessage("Object placed.");
    }
    public void TalkTo(TalkableCharacter speaker)
    {
        if (DialogueActive || CarriedObject || IsPaused) return;
        CustomerDialogue = speaker == customer;
        if (speaker != hale && speaker != customer) return;
        if (CustomerDialogue && !Loop.CanNegotiate) { ShowMessage(Loop.Objective); return; }
        DialogueActive = true;
        if(player.bodyVisual)player.bodyVisual.gameObject.SetActive(false);
        if (dialogueText) dialogueText.text = CustomerDialogue
            ? "CUSTOMER\nI'll pay $50 for the sealed package.\n[A] Accept $50   [C] Counter $75 (50% chance)   [R] Reject\nOne decision per day. A refused counter pays $0."
            : "DR. HALE\nPrepare one sealed package, take it to the customer counter, and settle the offer.\nReturn any unsold package, clean the work area, then rest.\n[T] Continue";
        FocusCamera(speaker.headTarget ? speaker.headTarget : speaker.transform);
    }
    // Compatibility for older external components; routing uses identity, never object names.
    public void TalkTo(string speakerName) { if (hale && speakerName == hale.CharacterName) TalkTo(hale); else if (customer && speakerName == customer.CharacterName) TalkTo(customer); }
    public void ContinueBriefing()
    {
        if (!DialogueActive || CustomerDialogue) return;
        Loop.Brief(); ShowMessage(Loop.Feedback); EndDialogue();
    }
    public void ResolveDialogue(string choice)
    {
        if (!DialogueActive || !CustomerDialogue) return;
        JayDailyLoop.Deal deal;
        if (choice == "accept") deal = JayDailyLoop.Deal.Accept;
        else if (choice == "counter") deal = JayDailyLoop.Deal.Counter;
        else if (choice == "reject") deal = JayDailyLoop.Deal.Reject;
        else return;
        if (Loop.Resolve(deal, Random.value))
        {
            if (Loop.Package == JayDailyLoop.PackageLocation.Sold) deliveryPackage.gameObject.SetActive(false);
            Play(confirmSound); ShowMessage(Loop.Feedback);
        }
        EndDialogue();
    }
    public void EndDialogue()
    {
        DialogueActive = false; CustomerDialogue = false;
        if(player && player.bodyVisual)player.bodyVisual.gameObject.SetActive(player.GetComponent<JayCameraRig>().IsThirdPerson);
        if (dialogueText) dialogueText.text = "";
        if (player && player.GetComponent<JayCameraRig>()) player.GetComponent<JayCameraRig>().EndFocus();
    }
    public void CleanWorkspace()
    {
        if (Loop.Clean()) { workspaceClutter.SetActive(false); Play(confirmSound); }
        ShowMessage(Loop.Feedback);
    }
    public void Sleep()
    {
        if (Loop.Sleep())
        {
            deliveryPackage.gameObject.SetActive(false); lighting.BeginNextDay();
            if (customerClothing) customerClothing.sharedMaterial = Loop.Day % 2 == 0 ? customerVariantB : customerVariantA;
            Play(confirmSound);
        }
        RefreshHud(); ShowMessage(Loop.Feedback);
    }
    public void TogglePause()
    {
        IsPaused = !IsPaused; Time.timeScale = IsPaused ? 0 : 1;
        if (pausePanel) pausePanel.SetActive(IsPaused);
        Cursor.lockState = IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsPaused;
    }
    public void Resume() { if (IsPaused) TogglePause(); }
    public void Quit()
    {
        Time.timeScale = 1;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void BeginPlacement(MoveableObject item) { activePlacement = item; ShowMessage("Place the prop with WASD; Q/R rotate, Enter confirms."); }
    public void RegisterInteraction(string unused) { }
    public void FocusCamera(Transform target) { if (player) player.GetComponent<JayCameraRig>()?.Focus(target); }
    public void ShowMessage(string text) { if (messageText) messageText.text = text; messageTimer = 5; }
    public void ShowDialogue(string text) { if (dialogueText) dialogueText.text = text; }
    public void SetPrompt(string text) { if (promptText) promptText.text = text; }
    public void Play(AudioClip clip) { if (feedbackAudio && clip) feedbackAudio.PlayOneShot(clip, .35f); }
}
public class WorldLabelBillboard : MonoBehaviour
{
    void LateUpdate() { if (Camera.main) transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position); }
}

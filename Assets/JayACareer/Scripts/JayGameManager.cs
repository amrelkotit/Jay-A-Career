using UnityEngine;
using UnityEngine.UI;

public class JayGameManager : MonoBehaviour
{
    public int day = 1; public int money = 100; public string objective = "Enter the laboratory and prepare today's delivery.";
    public Text titleText, dayText, cashText, objectiveText, promptText, messageText, dialogueText;
    public MoveableObject activePlacement; private float messageTimer; private int placedCount; private LiftableObject carriedObject; private Transform carryPoint; private bool dialogueActive; private string dialogueSpeaker;
    public LiftableObject CarriedObject => carriedObject;
    public bool DialogueActive => dialogueActive;
    void Start()
    {
        ConfigureHud(); PositionBottom(promptText, 40); PositionBottom(messageText, 90); PositionBottom(dialogueText, 145);
        EnsureContextualComponents();
        KeepLiftablesInsideLaboratory();
        RemoveOutOfBoundsInteractables();
        PolishPresentation();
        BuildProfessionalLab(); BuildBoundaryWalls();
        var playerCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (playerCamera != null) { var point = new GameObject("CarryPoint"); point.transform.SetParent(playerCamera.transform); point.transform.localPosition = new Vector3(0, 0, 2.2f); point.transform.localRotation = Quaternion.identity; carryPoint = point.transform; }
    }
    void ConfigureHud()
    {
        PositionTopLeft(titleText, 24, -18); titleText.fontSize = 18; titleText.text = "JAY A CAREER  •  JAY";
        PositionTopCenter(dayText, 0, -18); dayText.fontSize = 18;
        PositionTopLeft(objectiveText, 24, -50); objectiveText.fontSize = 14; objectiveText.rectTransform.sizeDelta = new Vector2(650, 42);
        if (cashText == null && titleText != null)
        {
            var go = new GameObject("CashText"); go.transform.SetParent(titleText.transform.parent, false); cashText = go.AddComponent<Text>(); cashText.font = titleText.font; cashText.color = Color.white; cashText.alignment = TextAnchor.UpperRight; cashText.rectTransform.sizeDelta = new Vector2(220, 42);
        }
        PositionTopRight(cashText, -24, -18); cashText.fontSize = 18;
        CreateInteractionMessageBackdrop();
        CreateControlsPanel();
    }
    void CreateInteractionMessageBackdrop()
    {
        if (promptText == null || promptText.transform.parent == null || GameObject.Find("InteractionMessageBackdrop") != null) return;
        var go = new GameObject("InteractionMessageBackdrop"); go.transform.SetParent(promptText.transform.parent, false);
        var image = go.AddComponent<Image>(); image.color = new Color(0.02f, 0.03f, 0.05f, 0.88f); image.raycastTarget = false;
        var r = go.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.zero; r.pivot = Vector2.zero; r.anchoredPosition = new Vector2(20, 24); r.sizeDelta = new Vector2(980, 92);
        go.transform.SetSiblingIndex(Mathf.Max(0, promptText.transform.GetSiblingIndex()));
    }
    void CreateControlsPanel()
    {
        if (titleText == null || titleText.transform.parent == null || GameObject.Find("ControlsHelpPanel") != null) return;
        var panel = new GameObject("ControlsHelpPanel"); panel.transform.SetParent(titleText.transform.parent, false);
        var bg = panel.AddComponent<Image>(); bg.color = new Color(0.03f, 0.05f, 0.08f, 0.86f);
        var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(1, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(1, 1); rect.anchoredPosition = new Vector2(-24, -58); rect.sizeDelta = new Vector2(238, 156);
        var textGo = new GameObject("ControlsText"); textGo.transform.SetParent(panel.transform, false); var text = textGo.AddComponent<Text>(); text.font = titleText.font; text.fontSize = 13; text.color = Color.white; text.alignment = TextAnchor.UpperLeft; text.text = "CONTROLS\nWASD   Move     Mouse   Look\nL        Lift / Drop\nR        Rotate carried object\nO        Open / Interact\nQ        Close door\nT        Talk       C        Clean\n\nHighlighted objects are interactable.";
        var textRect = text.rectTransform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(12, 10); textRect.offsetMax = new Vector2(-10, -10);
        var eyeGo = new GameObject("ControlsEyeButton"); eyeGo.transform.SetParent(panel.transform, false); var eye = eyeGo.AddComponent<Button>(); eye.targetGraphic = eyeGo.AddComponent<Image>(); eyeGo.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.28f, 0.95f); var eyeTextGo = new GameObject("EyeText"); eyeTextGo.transform.SetParent(eyeGo.transform, false); var eyeText = eyeTextGo.AddComponent<Text>(); eyeText.font = titleText.font; eyeText.fontSize = 15; eyeText.alignment = TextAnchor.MiddleCenter; eyeText.color = Color.white; eyeText.text = "◉"; var eyeRect = eyeGo.GetComponent<RectTransform>(); eyeRect.anchorMin = new Vector2(1, 1); eyeRect.anchorMax = new Vector2(1, 1); eyeRect.pivot = new Vector2(1, 1); eyeRect.anchoredPosition = new Vector2(-8, -8); eyeRect.sizeDelta = new Vector2(28, 24); var eyeTextRect = eyeText.rectTransform; eyeTextRect.anchorMin = Vector2.zero; eyeTextRect.anchorMax = Vector2.one; eyeTextRect.offsetMin = Vector2.zero; eyeTextRect.offsetMax = Vector2.zero;
        eye.onClick.AddListener(() => { textGo.SetActive(!textGo.activeSelf); bg.color = textGo.activeSelf ? new Color(0.03f, 0.05f, 0.08f, 0.86f) : new Color(0.03f, 0.05f, 0.08f, 0.35f); eyeText.text = textGo.activeSelf ? "◉" : "○"; });
    }
    void PositionTopLeft(Text t, float x, float y) { if(t==null)return; var r=t.rectTransform; r.anchorMin=new Vector2(0,1);r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,y); }
    void PositionTopCenter(Text t, float x, float y) { if(t==null)return; var r=t.rectTransform; r.anchorMin=new Vector2(.5f,1);r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(x,y);t.alignment=TextAnchor.UpperCenter; }
    void PositionTopRight(Text t, float x, float y) { if(t==null)return; var r=t.rectTransform; r.anchorMin=new Vector2(1,1);r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(1,1);r.anchoredPosition=new Vector2(x,y); }
    void EnsureContextualComponents()
    {
        AddLiftable("PrefabInstance_Chair", "Chair"); AddLiftable("PrefabInstance_Barrel", "Barrel"); AddLiftable("PrefabInstance_WoodenCrate", "Wooden Crate"); AddLiftable("PrefabInstance_StorageContainer", "Storage Container"); AddLiftable("PrefabInstance_LaboratoryEquipment", "Laboratory Equipment"); AddLiftable("PackageBox", "Package Box"); AddLiftable("Barrel", "Barrel"); AddLiftable("WoodenCrate", "Wooden Crate"); AddLiftable("StorageContainer", "Storage Container");
        var door = GameObject.Find("PrefabInstance_Door"); if (door != null && door.GetComponent<DoorInteractable>() == null) door.AddComponent<DoorInteractable>();
        var hale = GameObject.Find("Partner_Dr_Hale"); if (hale != null && hale.GetComponent<TalkableCharacter>() == null) { var t=hale.AddComponent<TalkableCharacter>(); t.CharacterName="Dr. Hale"; }
        var customer = GameObject.Find("Customer"); if (customer != null && customer.GetComponent<TalkableCharacter>() == null) { var t=customer.AddComponent<TalkableCharacter>(); t.CharacterName="Customer"; }
        var clean = GameObject.Find("PrefabInstance_CleaningStation"); if (clean != null && clean.GetComponent<CleanableStation>() == null) clean.AddComponent<CleanableStation>();
    }
    void AddLiftable(string objectName, string label) { var go=GameObject.Find(objectName); if(go==null){string fallback=objectName.Replace("PrefabInstance_","").ToLowerInvariant();foreach(var t in FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(t.name.ToLowerInvariant()==fallback||t.name.ToLowerInvariant().Contains(fallback)){go=t.gameObject;break;}}} if(go!=null){var l=go.GetComponent<LiftableObject>();if(l==null){l=go.AddComponent<LiftableObject>();}l.DisplayLabel=label;CreateLiftMarker(go.transform,label);} }
    void KeepLiftablesInsideLaboratory(){int slot=0;foreach(var lift in FindObjectsByType<LiftableObject>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(lift==null||lift.IsCarried)continue;Vector3 p=lift.transform.position;if(p.x<-12f||p.x>-2f||p.z<5.8f||p.z>16f){lift.transform.position=new Vector3(-10.2f+(slot%3)*3.2f,.75f,9.2f+(slot/3)*2.6f);slot++;}}foreach(var t in FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(t==null||t.GetComponent<LiftableObject>()!=null)continue;string n=t.name.ToLowerInvariant();if(!n.Contains("box")&&!n.Contains("crate"))continue;Vector3 p=t.position;if(p.x<-12f||p.x>-2f||p.z<5.8f||p.z>16f){t.position=new Vector3(-8.5f,.75f,12f);}}}
    void RemoveOutOfBoundsInteractables(){foreach(var t in FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(t==null)continue;var go=t.gameObject;var lift=go.GetComponent<LiftableObject>();var move=go.GetComponent<MoveableObject>();if(lift!=null||move==null)continue;Vector3 p=go.transform.position;bool outsideMap=p.x<minPlayableX||p.x>maxPlayableX||p.z<minPlayableZ||p.z>maxPlayableZ;if(outsideMap)Destroy(go);}}
    const float minPlayableX=-22f,maxPlayableX=22f,minPlayableZ=-3f,maxPlayableZ=24f;
    void CreateLiftMarker(Transform target,string label){string name=target.name+"_LiftMarker";if(target.Find(name)!=null)return;var g=new GameObject(name);g.transform.SetParent(target);g.transform.localPosition=new Vector3(0,1.2f,0);var tm=g.AddComponent<TextMesh>();tm.text="[L] LIFT\n"+label.ToUpper();tm.fontSize=28;tm.characterSize=.055f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(1f,.82f,.25f);g.AddComponent<WorldLabelBillboard>();}
    void PolishPresentation()
    {
        var hale=GameObject.Find("Partner_Dr_Hale"); if(hale!=null){CreatePersonVisual(hale,"DrHaleVisual","DrHale",new Color(.78f,.82f,.86f),new Color(.38f,.22f,.14f),new Color(.12f,.32f,.62f));}
        var customer=GameObject.Find("Customer"); if(customer!=null){CreatePersonVisual(customer,"CustomerVisual","Customer",new Color(.07f,.08f,.1f),new Color(.38f,.22f,.14f),new Color(.65f,.12f,.08f));}
        var counter=GameObject.Find("DeliveryCounter"); if(counter!=null){var counterInteraction=counter.GetComponent<SimpleInteractable>();if(counterInteraction!=null)Destroy(counterInteraction);counter.transform.position=new Vector3(9.4f,0.55f,14.2f);counter.transform.localScale=new Vector3(1.2f,.55f,1.1f);}
        if(hale!=null)CreateWorldLabel(hale.transform,"DR. HALE",new Vector3(0,1.55f,0),"DrHale_Label"); if(customer!=null)CreateWorldLabel(customer.transform,"CUSTOMER",new Vector3(0,1.55f,0),"Customer_Label");
        var door=GameObject.Find("PrefabInstance_Door"); if(door!=null)PolishDoor(door,"EXIT"); var labDoor=GameObject.Find("LabDoor"); if(labDoor!=null){if(labDoor.GetComponent<DoorInteractable>()==null)labDoor.AddComponent<DoorInteractable>();PolishDoor(labDoor,"LABORATORY");}
        if(GameObject.Find("LabAccessRoad")==null){var road=GameObject.CreatePrimitive(PrimitiveType.Cube);road.name="LabAccessRoad";road.transform.position=new Vector3(-7,.02f,3.1f);road.transform.localScale=new Vector3(3,.08f,4.2f);road.GetComponent<Renderer>().material.color=new Color(.07f,.08f,.09f);}
        if(GameObject.Find("RoadShoulder_Left")==null){CreateRoadPiece("RoadShoulder_Left",new Vector3(-3.1f,.01f,10),new Vector3(.7f,.06f,25),new Color(.28f,.22f,.16f));CreateRoadPiece("RoadShoulder_Right",new Vector3(3.1f,.01f,10),new Vector3(.7f,.06f,25),new Color(.28f,.22f,.16f));for(int i=0;i<7;i++)CreateRoadPiece("RoadMarker_"+i,new Vector3(0,.08f,0+i*4),new Vector3(.12f,.02f,1.8f),new Color(.9f,.75f,.25f));}
        if(GameObject.Find("Laboratory_Label")==null)CreateWorldLabel(GameObject.Find("Laboratory").transform,"LABORATORY",new Vector3(-7,5.3f,5.2f),"Laboratory_Label");
        if(GameObject.Find("LabCeilingLights") == null){var lights=new GameObject("LabCeilingLights");for(int i=0;i<3;i++){var l=new GameObject("LabCeilingLight_"+i);l.transform.SetParent(lights.transform);l.transform.position=new Vector3(-10+i*3,4.2f,11);var light=l.AddComponent<Light>();light.type=LightType.Point;light.range=5;light.intensity=3;light.color=new Color(.65f,.82f,1f);}}
    }
    void PolishDoor(GameObject door,string label)
    {
        var r=door.GetComponentInChildren<Renderer>();if(r!=null)r.material.color=new Color(.12f,.18f,.24f);
        if(door.transform.Find("DoorHandle")==null){var h=GameObject.CreatePrimitive(PrimitiveType.Cylinder);h.name="DoorHandle";h.transform.SetParent(door.transform);h.transform.localPosition=new Vector3(.48f,0,.08f);h.transform.localRotation=Quaternion.Euler(90,0,0);h.transform.localScale=new Vector3(.06f,.12f,.06f);h.GetComponent<Renderer>().material.color=new Color(.85f,.55f,.18f);}
        if(door.transform.parent!=null && door.transform.parent.Find(door.name+"_StationaryFrame")==null){var frameRoot=new GameObject(door.name+"_StationaryFrame");frameRoot.transform.SetParent(door.transform.parent);frameRoot.transform.position=door.transform.position;frameRoot.transform.rotation=door.transform.rotation;CreateDoorFrame(frameRoot.transform,"DoorFrame_Left",new Vector3(-1.15f,0,0),new Vector3(.12f,1.25f,.16f));CreateDoorFrame(frameRoot.transform,"DoorFrame_Right",new Vector3(1.15f,0,0),new Vector3(.12f,1.25f,.16f));CreateDoorFrame(frameRoot.transform,"DoorFrame_Top",new Vector3(0,1.18f,0),new Vector3(2.4f,.12f,.16f));}
        CreateWorldLabel(door.transform,label,new Vector3(0,1.45f,-.12f),door.name+"_Label");
    }
    void CreateDoorFrame(Transform parent,string name,Vector3 localPosition,Vector3 localScale){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.localPosition=localPosition;g.transform.localScale=localScale;g.GetComponent<Renderer>().material.color=new Color(.04f,.06f,.08f);var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);}
    void CreatePersonVisual(GameObject person,string visualName,string prefix,Color suitColor,Color skinColor,Color accentColor)
    {
        if(person.transform.Find(visualName)!=null)return;
        var old=person.GetComponent<Renderer>();if(old!=null)old.enabled=false;
        var root=new GameObject(visualName);root.transform.SetParent(person.transform,false);
        var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name=prefix+"Body";body.transform.SetParent(root.transform,false);body.transform.localPosition=new Vector3(0,0,0);body.transform.localScale=new Vector3(.62f,.9f,.5f);body.GetComponent<Renderer>().material.color=suitColor;Destroy(body.GetComponent<Collider>());
        var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name=prefix+"Head";head.transform.SetParent(root.transform,false);head.transform.localPosition=new Vector3(0,1.25f,0);head.transform.localScale=new Vector3(.5f,.5f,.5f);head.GetComponent<Renderer>().material.color=skinColor;Destroy(head.GetComponent<Collider>());
        var tie=GameObject.CreatePrimitive(PrimitiveType.Cube);tie.name=prefix+"Tie";tie.transform.SetParent(root.transform,false);tie.transform.localPosition=new Vector3(0,.35f,-.28f);tie.transform.localScale=new Vector3(.1f,.38f,.04f);tie.GetComponent<Renderer>().material.color=accentColor;Destroy(tie.GetComponent<Collider>());
        for(int side=-1;side<=1;side+=2){var leg=GameObject.CreatePrimitive(PrimitiveType.Cube);leg.name=prefix+(side<0?"Leg_Left":"Leg_Right");leg.transform.SetParent(root.transform,false);leg.transform.localPosition=new Vector3(.18f*side,-.95f,0);leg.transform.localScale=new Vector3(.2f,.65f,.25f);leg.GetComponent<Renderer>().material.color=suitColor;Destroy(leg.GetComponent<Collider>());var arm=GameObject.CreatePrimitive(PrimitiveType.Capsule);arm.name=prefix+(side<0?"Arm_Left":"Arm_Right");arm.transform.SetParent(root.transform,false);arm.transform.localPosition=new Vector3(.48f*side,.05f,0);arm.transform.localRotation=Quaternion.Euler(0,0,side*12f);arm.transform.localScale=new Vector3(.16f,.5f,.16f);arm.GetComponent<Renderer>().material.color=suitColor;Destroy(arm.GetComponent<Collider>());}
    }
    void CreateRoadPiece(string name,Vector3 position,Vector3 scale,Color color){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=position;g.transform.localScale=scale;g.GetComponent<Renderer>().material.color=color;}
    void CreateWorldLabel(Transform target,string text,Vector3 offset,string name){if(GameObject.Find(name)!=null)return;var g=new GameObject(name);g.transform.SetParent(target);g.transform.localPosition=offset;var tm=g.AddComponent<TextMesh>();tm.text=text;tm.fontSize=32;tm.characterSize=.08f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(1f,.86f,.45f);g.AddComponent<WorldLabelBillboard>();}
    void BuildProfessionalLab()
    {
        if(GameObject.Find("ProfessionalLabShell")!=null)return;
        var old=GameObject.Find("LabBuilding"); if(old!=null){var r=old.GetComponent<Renderer>();if(r!=null)r.enabled=false;foreach(var c in old.GetComponents<Collider>())c.enabled=false;}
        var interior=GameObject.Find("LabInteriorWall");if(interior!=null){var r=interior.GetComponent<Renderer>();if(r!=null)r.enabled=false;}
        var shell=new GameObject("ProfessionalLabShell"); Color wall=new Color(.16f,.19f,.24f); Color trim=new Color(.06f,.08f,.11f);
        CreateLabBox(shell.transform,"Lab_BackWall",new Vector3(-7,2,16.8f),new Vector3(12,.2f, .25f),wall); CreateLabBox(shell.transform,"Lab_ExteriorBackWall",new Vector3(-7,2.2f,17.15f),new Vector3(13.5f,4.4f,.35f),wall); CreateLabBox(shell.transform,"Lab_LeftWall",new Vector3(-13,2,11),new Vector3(.25f,4,12),wall); CreateLabBox(shell.transform,"Lab_RightWall",new Vector3(-1,2,11),new Vector3(.25f,4,12),wall);
        // Keep a realistic doorway opening around LabDoor (x=-7, width about 2.2), then close the remaining side gaps.
        CreateLabBox(shell.transform,"Lab_FrontWall_Left",new Vector3(-10.8f,2,5.1f),new Vector3(4.4f,4,.25f),wall); CreateLabBox(shell.transform,"Lab_FrontWall_Right",new Vector3(-3.2f,2,5.1f),new Vector3(4.4f,4,.25f),wall);
        CreateLabBox(shell.transform,"Lab_DoorReveal_Left",new Vector3(-8.45f,2,5.05f),new Vector3(.45f,4,.35f),trim); CreateLabBox(shell.transform,"Lab_DoorReveal_Right",new Vector3(-5.55f,2,5.05f),new Vector3(.45f,4,.35f),trim);
        CreateLabBox(shell.transform,"Lab_Roof_Clean",new Vector3(-7,4.2f,11),new Vector3(12,.25f,12),trim);
        CreateLabBox(shell.transform,"Lab_EntranceHeader",new Vector3(-7,3.65f,5.1f),new Vector3(4,.35f,.35f),trim);
        for(int i=0;i<3;i++){if(i==1)continue;var w=CreateLabBox(shell.transform,"Lab_Window_"+i,new Vector3(-11+i*3.5f,2.5f,5.0f),new Vector3(1.8f,1.1f,.05f),new Color(.08f,.28f,.36f));}
    }
    GameObject CreateLabBox(Transform parent,string name,Vector3 position,Vector3 scale,Color color){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.position=position;g.transform.localScale=scale;g.GetComponent<Renderer>().material.color=color;return g;}
    void BuildBoundaryWalls()
    {
        if(GameObject.Find("PlayableBoundary_North")!=null)return;
        CreateBoundary("PlayableBoundary_West",new Vector3(-23,2,10),new Vector3(.3f,4,30)); CreateBoundary("PlayableBoundary_East",new Vector3(23,2,10),new Vector3(.3f,4,30)); CreateBoundary("PlayableBoundary_South",new Vector3(0,2,-4),new Vector3(46,4,.3f)); CreateBoundary("PlayableBoundary_North",new Vector3(0,2,25),new Vector3(46,4,.3f));
    }
    void CreateBoundary(string name,Vector3 position,Vector3 scale){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=position;g.transform.localScale=scale;g.GetComponent<Renderer>().enabled=false;}
    void PositionBottom(Text t, float y) { if (t == null) return; var r=t.rectTransform; r.anchorMin=Vector2.zero; r.anchorMax=Vector2.zero; r.pivot=Vector2.zero; r.anchoredPosition=new Vector2(38,y+8); r.sizeDelta=new Vector2(920,76); r.localScale=Vector3.one; r.localRotation=Quaternion.identity; t.fontSize=t==promptText?24:20; t.alignment=TextAnchor.LowerLeft; t.horizontalOverflow=HorizontalWrapMode.Overflow; t.verticalOverflow=VerticalWrapMode.Overflow; t.gameObject.SetActive(true); t.transform.SetAsLastSibling(); if(t.GetComponent<Outline>()==null){var outline=t.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,0,0,.95f);outline.effectDistance=new Vector2(2,-2);} }
    void Update()
    {
        if (activePlacement != null) { activePlacement.UpdatePlacement(); if (activePlacement.IsPlaced) { placedCount++; activePlacement = null; ShowMessage("Placement confirmed."); if (placedCount >= 3) objective = "Use the preparation station to receive the delivery objective."; } }
        if (messageTimer > 0) { messageTimer -= Time.deltaTime; if (messageTimer <= 0 && messageText) messageText.text = ""; }
        if (dayText) dayText.text = "DAY " + day;
        if (cashText) cashText.text = "CASH $" + money;
        if (objectiveText) objectiveText.text = "OBJECTIVE  " + objective;
        UpdateInteriorLabelVisibility();
    }
    void UpdateInteriorLabelVisibility(){var player=FindFirstObjectByType<JayPlayerController>();if(player==null)return;Vector3 p=player.transform.position;bool inside=p.x>-12.2f&&p.x<-1.8f&&p.z>5.4f&&p.z<16.8f;foreach(var tm in FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(tm==null)continue;string n=tm.gameObject.name;bool interior=n.EndsWith("_LiftMarker")||n=="DrHale_Label"||n=="Laboratory_Label"||n=="LabDoor_Label";if(interior)tm.gameObject.SetActive(inside);}}
    public void BeginPlacement(MoveableObject item) { activePlacement = item; objective = "Move with WASD, rotate with Q/R, confirm with Enter."; ShowMessage("Editing: " + item.name); }
    public void BeginCarry(LiftableObject item)
    {
        if (carriedObject != null || carryPoint == null) return;
        carriedObject = item; item.PickUp(carryPoint); ShowMessage(item.DisplayName + " picked up. Press L to drop or R to rotate.");
    }
    public void DropCarried()
    {
        if (carriedObject == null) return;
        var player = FindFirstObjectByType<JayPlayerController>(); Vector3 dropPosition = player.transform.position + player.transform.forward * 2f; dropPosition.y = .75f;
        carriedObject.Drop(dropPosition, Quaternion.Euler(0, player.transform.eulerAngles.y, 0)); ShowMessage(carriedObject.DisplayName + " dropped."); carriedObject = null;
    }
    public void TalkTo(string characterName)
    {
        dialogueSpeaker = characterName; dialogueActive = true;
        if (characterName.ToLower().Contains("customer")) { objective = "Choose Accept, Counter, or Reject."; ShowDialogue("Customer: I am interested in the delivery. What is your offer?"); }
        else { objective = "Prepare the laboratory for today's delivery."; ShowDialogue("Dr. Hale: We are ready. Prepare the workspace, then deliver the package."); }
    }
    public void ResolveDialogue(string choice)
    {
        if (!dialogueActive) return;
        dialogueActive = false;
        bool customer = dialogueSpeaker.ToLower().Contains("customer");
        if (customer && choice == "accept") { money += 50; objective = "Return to the laboratory and clean the workspace."; ShowMessage("Accepted. Payment received: $50."); }
        else if (customer && choice == "counter") { money += 75; objective = "Return to the laboratory and clean the workspace."; ShowMessage("Counter offer accepted. Payment received: $75."); }
        else if (customer && choice == "reject") { objective = "Return to the laboratory and clean the workspace."; ShowMessage("Offer rejected. Return to the laboratory."); }
        else { objective = "Prepare the laboratory for today's delivery."; ShowMessage("Conversation complete."); }
        if (dialogueText) dialogueText.text = "";
    }
    public void CleanWorkspace() { objective = "Go to the bedroom and sleep to begin Day 2."; ShowMessage("Workspace cleaned. The lab is ready for tonight."); }
    public void RegisterInteraction(string objectName)
    {
        string n = objectName.ToLower();
        if (n.Contains("preparation")) objective = "Travel to the customer area and negotiate.";
        else if (n.Contains("customer")) { objective = "Return to the laboratory and clean the workspace."; money += 50; ShowDialogue("Customer: The deal is accepted. Payment received."); }
        else if (n.Contains("clean")) objective = "Go to the bedroom and sleep to begin Day 2.";
        else if (n.Contains("bed") || n.Contains("sleep")) { day++; objective = "Day 2: Prepare a new delivery."; ShowMessage("A new day begins."); }
    }
    public void ShowMessage(string text) { if (messageText) { messageText.text = text; messageTimer = 4f; } }
    public void ShowDialogue(string text) { if (dialogueText) dialogueText.text = text + "\n\n[A] Accept     [C] Counter     [R] Reject"; }
    public void SetPrompt(string text) { if (promptText) promptText.text = text; }
}

public class WorldLabelBillboard : MonoBehaviour
{
    void LateUpdate(){if(Camera.main!=null){Vector3 toCamera=Camera.main.transform.position-transform.position;if(toCamera.sqrMagnitude>.001f){transform.rotation=Quaternion.LookRotation(toCamera,Vector3.up);if(Vector3.Dot(transform.up,Vector3.up)<0)transform.Rotate(0,0,180);}}}
}

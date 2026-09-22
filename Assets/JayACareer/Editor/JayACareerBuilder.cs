using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

[InitializeOnLoad]
public static class JayACareerBuilder
{
    const string Root = "Assets/JayACareer";
    static readonly string[] matNames = { "Floor","Concrete","DirtyConcrete","Sand","Asphalt","Gravel","Metal","RustedMetal","StainlessSteel","Glass","DarkGlass","Wood","PaintedWood","Plastic","Rubber","Cardboard","Fabric","Leather","Ceramic","Carpet","DarkWall","PaintedWall","Vehicle","WarningSign","Emissive","LabAccent","Storage","Equipment" };
    static readonly Color[] colors = { new(.18f,.16f,.14f),new(.38f,.39f,.38f),new(.22f,.22f,.20f),new(.72f,.52f,.29f),new(.08f,.09f,.10f),new(.30f,.28f,.24f),new(.35f,.39f,.42f),new(.35f,.12f,.06f),new(.62f,.65f,.67f),new(.25f,.65f,.72f),new(.05f,.12f,.15f),new(.37f,.20f,.08f),new(.58f,.30f,.12f),new(.65f,.68f,.62f),new(.06f,.07f,.07f),new(.56f,.32f,.14f),new(.28f,.12f,.12f),new(.18f,.08f,.05f),new(.72f,.74f,.68f),new(.15f,.12f,.18f),new(.06f,.07f,.09f),new(.27f,.30f,.35f),new(.08f,.18f,.28f),new(.85f,.45f,.06f),new(.95f,.55f,.08f),new(.10f,.55f,.55f),new(.22f,.30f,.20f),new(.32f,.38f,.42f) };
    static JayACareerBuilder() { EditorApplication.delayCall += EnsureBuilt; }
    [MenuItem("Jay A Career/Build Prototype Scene")] public static void EnsureBuilt()
    {
        if (EditorApplication.isCompiling) return;
        EnsureFolders();
        string scenePath = Root + "/Scenes/JayACareer_Main.unity";
        if (File.Exists(scenePath)) return;
        BuildAssets(); BuildScene(scenePath);
    }
    static void EnsureFolders()
    {
        foreach (string f in new[]{Root,Root+"/Scenes",Root+"/Materials",Root+"/Textures",Root+"/Prefabs",Root+"/Models",Root+"/Scripts",Root+"/UI",Root+"/Audio",Root+"/Editor"}) if (!AssetDatabase.IsValidFolder(f)) { string p = f.Substring(0,f.LastIndexOf('/')); AssetDatabase.CreateFolder(p,f.Substring(f.LastIndexOf('/')+1)); }
    }
    static void BuildAssets()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        for (int i=0;i<matNames.Length;i++) { string path=Root+"/Materials/MAT_"+matNames[i]+".mat"; if (!File.Exists(path)) { var m=new Material(shader){name="MAT_"+matNames[i], color=colors[i]}; m.SetFloat("_Metallic", i==6||i==7||i==8 ? .75f : .05f); m.SetFloat("_Smoothness", i==9||i==10||i==24 ? .7f : .25f); AssetDatabase.CreateAsset(m,path); } }
        for (int i=0;i<25;i++) { string path=Root+"/Textures/TEX_Surface_"+(i+1).ToString("00")+".asset"; if (!File.Exists(path)) { var t=new Texture2D(4,4); Color c=Color.HSVToRGB((i*.071f)%1f,.35f,.45f+.02f*i); for(int x=0;x<4;x++)for(int y=0;y<4;y++)t.SetPixel(x,y,Color.Lerp(c,Color.white,(x+y)%2*.08f)); t.Apply(); t.name="TEX_Surface_"+(i+1).ToString("00"); AssetDatabase.CreateAsset(t,path); } }
        AssetDatabase.SaveAssets();
    }
    static Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/MAT_"+n+".mat");
    static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, string mat, bool collider=true)
    { var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent); g.transform.localPosition=pos; g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=M(mat); if(!collider) Object.DestroyImmediate(g.GetComponent<Collider>()); return g; }
    static GameObject Sphere(string name, Transform parent, Vector3 pos, Vector3 scale, string mat) { var g=GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name=name; g.transform.SetParent(parent); g.transform.localPosition=pos; g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=M(mat); return g; }
    static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 scale, string mat) { var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name=name; g.transform.SetParent(parent); g.transform.localPosition=pos; g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=M(mat); return g; }
    static void Interact(GameObject g,string prompt,string action) { var i=g.AddComponent<SimpleInteractable>(); i.Prompt=prompt; i.ActionText=action; }
    static GameObject Moveable(string name, Transform p, Vector3 pos, Vector3 scale, string mat) { var g=Cube(name,p,pos,scale,mat); g.AddComponent<MoveableObject>(); return g; }
    static void BuildScene(string path)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); RenderSettings.ambientLight=new Color(.18f,.20f,.24f); RenderSettings.fog=true; RenderSettings.fogColor=new Color(.38f,.28f,.20f); RenderSettings.fogDensity=.008f;
        var env=new GameObject("Environment").transform; var exterior=new GameObject("Exterior_Desert").transform; exterior.SetParent(env); var lab=new GameObject("Laboratory").transform; lab.SetParent(env); var delivery=new GameObject("DeliveryArea").transform; delivery.SetParent(env); var bedroom=new GameObject("Bedroom").transform; bedroom.SetParent(env); var characters=new GameObject("Characters").transform; characters.SetParent(env); var inter=new GameObject("Interactables").transform; inter.SetParent(env);
        Cube("MainFloor",exterior,new(0,-.25f,10),new(24,.5f,30),"Floor"); Cube("Road",exterior,new(0,.02f,0),new(5,.08f,28),"Asphalt"); Cube("LabBuilding",lab,new(-7,2,11),new(7,2.5f,6),"PaintedWall"); Cube("LabInteriorWall",lab,new(-7,2,11),new(6.7f,2.2f,5.7f),"DarkWall",false); Cube("LabRoof",lab,new(-7,4.8f,11),new(7.2f,.3f,6.2f),"Metal"); Cube("DeliveryCounter",delivery,new(8,1,14),new(4,1,1),"Wood"); Cube("BedroomRoom",bedroom,new(11,2,10),new(5,2.5f,5),"PaintedWall");
        for(int x=-20;x<=20;x+=4) { Cylinder("DesertRock",exterior,new(x,.25f,24-(Mathf.Abs(x)%5)),new(.7f,.7f,.7f),"Sand"); }
        Cube("LabDoor",lab,new(-7,1.5f,5.1f),new(2.2f,2.2f,.2f),"Metal");
        Moveable("LabTable",lab,new(-9,.8f,10),new(2.5f,.8f,1),"StainlessSteel"); Moveable("EquipmentBench",lab,new(-5,.8f,10),new(1.5f,.8f,1),"Equipment"); Moveable("StorageContainer",lab,new(-7,.6f,13),new(1.2f,.6f,1),"Storage");
        for(int i=0;i<4;i++) { Cylinder("Beaker_"+i,lab,new(-10+i*.8f,1.8f,10),new(.22f,.45f,.22f),i%2==0?"Glass":"Ceramic"); }
        Cylinder("Flask",lab,new(-5,1.9f,10),new(.4f,.65f,.4f),"Glass"); Cube("TestTubeRack",lab,new(-3.5f,1.1f,10),new(.7f,.15f,.3f),"Plastic"); Cube("MetalCabinet",lab,new(-2,1.3f,12.5f),new(1,.1f,1),"Metal"); Cube("StorageShelf",lab,new(-11,1.5f,13),new(.25f,1.5f,2),"Wood"); Cube("Computer",lab,new(-3,1.5f,12),new(.7f,.6f,.3f),"Plastic"); Cube("Pipes",lab,new(-12,3.2f,10),new(.2f,.2f,3),"RustedMetal"); Cylinder("VentilationUnit",lab,new(-12,4,13),new(1,.3f,1),"Metal"); Cube("Generator",exterior,new(3,1,9),new(1.5f,1,1),"Equipment");
        Cube("Desk",bedroom,new(10,1,9),new(1.5f,.8f,.7f),"Wood"); Cube("Bed",bedroom,new(13,1,11),new(2, .6f, 1.4f),"Fabric"); Interact(GameObject.Find("Bed"),"Sleep","Rest for the night."); Cube("BedroomTable",bedroom,new(9,1,12),new(.7f,.7f,.7f),"Wood"); Cylinder("Lamp",bedroom,new(9,2,12),new(.25f,.8f,.25f),"Emissive"); Cube("Car",exterior,new(3,.7f,2),new(2,.7f,3),"Vehicle"); Cube("Barrel",exterior,new(-2,.8f,5),new(.7f,.8f,.7f),"RustedMetal"); Cube("WoodenCrate",delivery,new(9,.5f,16),new(.7f,.5f,.7f),"Cardboard"); Cube("Dumpster",exterior,new(16,1,18),new(1.5f,1,1.2f),"Metal"); Cube("WarehouseDoor",exterior,new(16,2,14),new(2,.1f,2),"Metal"); Cylinder("TelephonePole",exterior,new(18,3,5),new(.25f,3,.25f),"Wood"); Cube("StreetLight",exterior,new(-16,3,5),new(.2f,3,.2f),"Metal"); Cube("CleaningStation",lab,new(-7,1,15),new(1,.8f,.8f),"Plastic"); Cube("PackageBox",delivery,new(8,1.1f,14),new(.7f,.5f,.7f),"Cardboard");
        Interact(GameObject.Find("CleaningStation"),"Clean workspace","Workspace cleaned."); var prep=GameObject.Find("StorageContainer"); Interact(prep,"Prepare operation","Preparation complete. Delivery objective received."); var customer=GameObject.Find("DeliveryCounter"); Interact(customer,"Talk to customer","Negotiation started.");
        var jay=GameObject.CreatePrimitive(PrimitiveType.Capsule); jay.name="Player_Jay"; jay.transform.position=new(0,1,0); jay.transform.SetParent(characters); Object.DestroyImmediate(jay.GetComponent<Collider>()); jay.AddComponent<CharacterController>(); jay.AddComponent<JayPlayerController>(); var cam=new GameObject("PlayerCamera"); cam.transform.SetParent(jay.transform); cam.transform.localPosition=new(0,.65f,0); cam.AddComponent<Camera>(); cam.AddComponent<AudioListener>();
        var hale=Capsule("Partner_Dr_Hale",characters,new(-9,1,8),"Leather"); var cust=Capsule("Customer",characters,new(8,1,13),"Fabric"); Interact(hale,"Talk to Dr. Hale","Jay and Dr. Hale are ready for today's work."); Interact(cust,"Negotiate","Choose Accept, Counter, or Reject.");
        var light=new GameObject("Sun_Directional"); var dl=light.AddComponent<Light>(); dl.type=LightType.Directional; dl.intensity=1.1f; dl.color=new Color(1f,.78f,.58f); light.transform.rotation=Quaternion.Euler(45,-30,0); var point=new GameObject("Lab_Light"); var pl=point.AddComponent<Light>(); pl.type=LightType.Point; pl.range=14; pl.intensity=8; pl.color=new Color(.55f,.75f,1); point.transform.position=new(-7,3,11);
        var gm=new GameObject("JayGameManager"); var manager=gm.AddComponent<JayGameManager>(); BuildUI(manager);
        string[] prefabNames={"LabTable","Chair","StorageShelf","Barrel","WoodenCrate","StorageContainer","LaboratoryEquipment","Door","StreetLight","CleaningStation"}; foreach(var n in prefabNames) { var src=GameObject.Find(n); if(src==null) src=GameObject.Find("LabTable"); string pp=Root+"/Prefabs/PF_"+n+".prefab"; if(!File.Exists(pp)) PrefabUtility.SaveAsPrefabAsset(src,pp); }
        EditorSceneManager.SaveScene(scene,path); EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true),new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity",false)}; AssetDatabase.SaveAssets(); Selection.activeObject=gm;
    }
    static GameObject Capsule(string name,Transform p,Vector3 pos,string mat) { var g=GameObject.CreatePrimitive(PrimitiveType.Capsule); g.name=name; g.transform.SetParent(p); g.transform.position=pos; g.GetComponent<Renderer>().sharedMaterial=M(mat); return g; }
    static void BuildUI(JayGameManager manager)
    { var c=new GameObject("HUD_Canvas"); var canvas=c.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; c.AddComponent<CanvasScaler>(); c.AddComponent<GraphicRaycaster>(); Text Make(string name,Vector2 pos,int size,Color col){var g=new GameObject(name);g.transform.SetParent(c.transform);var t=g.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.color=col;t.alignment=TextAnchor.MiddleLeft;var r=t.rectTransform;r.anchorMin=new(0,1);r.anchorMax=new(0,1);r.pivot=new(0,1);r.anchoredPosition=pos;r.sizeDelta=new(700,60);return t;} manager.titleText=Make("Title",new(24,-20),28,new(1,.75f,.3f)); manager.titleText.text="JAY A CAREER"; manager.dayText=Make("Day",new(24,-65),20,Color.white); manager.objectiveText=Make("Objective",new(24,-100),18,new(.75f,.9f,1)); manager.promptText=Make("Prompt",new(24,-Screen.height+80),22,Color.white); manager.messageText=Make("Message",new(24,-Screen.height+135),20,new(1,.85f,.45f)); manager.dialogueText=Make("Dialogue",new(24,-Screen.height+200),18,new(.8f,1,.8f)); }
}

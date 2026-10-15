using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor.Animations;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json.Linq;
using Object=UnityEngine.Object;

public static class RealismSceneAuthor
{
    const string Root="Assets/JayACareer";
    const string Models=Root+"/Models/Realism/";
    const string Materials=Root+"/Materials/Realism/";
    static Dictionary<string,Material> mats=new Dictionary<string,Material>();
    static List<object> inventory=new List<object>();
    static Transform env,lab,exterior,bedroom,delivery;
    static JayGameManager gm;
    static Material Material(string name,Color color,float metallic=0,float smooth=.35f)
    {
        if(mats.TryGetValue(name,out var cached))return cached;
        var path=Materials+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.color=color;mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",smooth);
        mats[name]=mat;return mat;
    }
    static void Textures()
    {
        Directory.CreateDirectory(Materials);
        foreach(var file in Directory.GetFiles(Models,"*.materials.json"))
        foreach(var row in JArray.Parse(File.ReadAllText(file)))
        {
            string name=(string)row["name"];
            var c=row["color"].ToObject<float[]>();
            var m=Material(name,new Color(c[0],c[1],c[2],c.Length>3?c[3]:1),(float)row["metallic"],1-(float)row["roughness"]);
            var maps=(JObject)row["textures"];
            if(maps["Base Color"]!=null)m.SetTexture("_BaseMap",LoadTexture(Root+"/Textures/Realism/"+(string)maps["Base Color"],false));
            if(maps["Normal"]!=null){m.SetTexture("_BumpMap",LoadTexture(Root+"/Textures/Realism/"+(string)maps["Normal"],true));m.EnableKeyword("_NORMALMAP");}
            string arm=(string)maps["Roughness"];
            if(arm!=null && arm.Contains("arm"))PackArm(m,Root+"/Textures/Realism/"+arm,true);
            if(name.Contains("Glass"))
            {
                m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;
            }
            if(name.ToLower().Contains("short") || name.ToLower().Contains("eyebrow"))
            {m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.4f);m.SetFloat("_Cull",0);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
            if(name=="ScreenGlow"){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.02f,.25f,.28f));}
            EditorUtility.SetDirty(m);
        }
        foreach(var asset in new[]{"asphalt_03","concrete_floor","sand_03","rusty_painted_metal"})
        {
            var dir=Root+"/Textures/PolyHaven/"+asset;
            var m=Material(asset,Color.white,asset.Contains("metal")?.6f:0,.25f);
            var diff=Directory.GetFiles(dir,"*_diff_1k.jpg").FirstOrDefault();
            var normal=Directory.GetFiles(dir,"*_nor_gl_1k.png").FirstOrDefault();
            var rough=Directory.GetFiles(dir,"*_rough_1k.*").FirstOrDefault();
            if(diff!=null)m.SetTexture("_BaseMap",LoadTexture(diff,false));
            if(normal!=null){m.SetTexture("_BumpMap",LoadTexture(normal,true));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.55f);}
            if(rough!=null)PackArm(m,rough,false);
            if(!asset.Contains("metal")){m.SetFloat("_SpecularHighlights",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.SetFloat("_BumpScale",.15f);m.SetFloat("_EnvironmentReflections",0);m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");}
            EditorUtility.SetDirty(m);
        }
        Material("Architecture",new Color(.55f,.57f,.52f),.15f,.3f);
        Material("DarkTrim",new Color(.08f,.11f,.12f),.6f,.4f);
        Material("RoadMarking",new Color(.7f,.62f,.33f),0,.15f);
        Material("DoorPaint",new Color(.13f,.25f,.25f),.45f,.35f);
        Material("RoofMetal",new Color(.22f,.27f,.27f),.7f,.4f);
        Material("Gravel",new Color(.38f,.32f,.24f),0,.1f);
        Material("WarmFabric",new Color(.36f,.18f,.12f),0,.1f);
        AssetDatabase.SaveAssets();
    }
    static Texture2D LoadTexture(string path,bool normal)
    {
        path=path.Replace('\\','/');
        if(!File.Exists(path))path=path.Replace("/Textures/Realism/","/Textures/Characters/");
        var imp=AssetImporter.GetAtPath(path) as TextureImporter;
        if(imp!=null){imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;imp.maxTextureSize=1024;imp.SaveAndReimport();}
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static void PackArm(Material mat,string path,bool packed)
    {
        var raw=new Texture2D(2,2,TextureFormat.RGBA32,false,true);raw.LoadImage(File.ReadAllBytes(path));
        var pixels=raw.GetPixels();
        for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(packed?pixels[i].b:mat.GetFloat("_Metallic"),0,0,1-(packed?pixels[i].g:pixels[i].r));
        var rgba=new Texture2D(raw.width,raw.height,TextureFormat.RGBA32,false,true);rgba.SetPixels(pixels);rgba.Apply();
        string dest=Root+"/Textures/Realism/"+mat.name+"_MetalSmooth.png";
        File.WriteAllBytes(dest,rgba.EncodeToPNG());Object.DestroyImmediate(rgba);Object.DestroyImmediate(raw);AssetDatabase.ImportAsset(dest);
        var imp=AssetImporter.GetAtPath(dest) as TextureImporter;imp.sRGBTexture=false;imp.maxTextureSize=1024;imp.SaveAndReimport();
        mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dest));mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Smoothness",1);
    }
    static GameObject Group(string name,Transform parent=null)
    {var go=new GameObject(name);if(parent)go.transform.SetParent(parent,false);return go;}
    static Bounds Bounds(GameObject go)
    {var rs=go.GetComponentsInChildren<Renderer>();var b=rs.Length>0?rs[0].bounds:new Bounds(go.transform.position,Vector3.one);foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static GameObject Model(string model,string name,Transform parent,Vector3 pos,float yaw=0,Vector3? size=null,bool collider=true,bool single=false)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Models+model+".fbx") ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/JayACareer/ModKit/"+model+".fbx");
        if(!source)throw new Exception("Missing model "+model);
        var root=Group(name,parent);var visual=(GameObject)Object.Instantiate(source,root.transform);visual.name="Visual";
        if(single)
        {
            var selected=visual.GetComponentsInChildren<MeshRenderer>().OrderByDescending(r=>r.bounds.size.sqrMagnitude).First();
            var part=(GameObject)Object.Instantiate(selected.gameObject,root.transform);part.name="Visual";
            Object.DestroyImmediate(visual);visual=part;
        }
        foreach(var r in visual.GetComponentsInChildren<Renderer>())
        {r.sharedMaterials=r.sharedMaterials.Select(m=>m && mats.TryGetValue(m.name,out var replacement)?replacement:m).ToArray();}
        visual.transform.localPosition=Vector3.zero;
        var b=Bounds(visual);
        if(size.HasValue)
        {
            var v=size.Value;visual.transform.localScale=Vector3.Scale(visual.transform.localScale,new Vector3(v.x/Mathf.Max(.001f,b.size.x),v.y/Mathf.Max(.001f,b.size.y),v.z/Mathf.Max(.001f,b.size.z)));
            b=Bounds(visual);
        }
        visual.transform.position-=new Vector3(b.center.x-root.transform.position.x,b.min.y-root.transform.position.y,b.center.z-root.transform.position.z);
        b=Bounds(visual);
        if(collider){var c=root.AddComponent<BoxCollider>();c.center=root.transform.InverseTransformPoint(b.center);c.size=b.size;}
        root.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));
        inventory.Add(new {name,model,x=pos.x,y=pos.y,z=pos.z,width=b.size.x,height=b.size.y,depth=b.size.z});
        return root;
    }
    static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,string material,bool collision=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mats[material];
        if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static Transform Slot(string name,Transform parent,Vector3 p){var g=Group(name,parent);g.transform.position=p;return g.transform;}
    static void Wall(string name,Vector3 p,Vector3 size,float yaw=0,bool window=false)
    {
        var go=Model(window?"SM_Wall_Window_4m":"SM_Wall_Straight_4m",name,lab,p,yaw,size,true);
        foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterial=mats["Architecture"];
    }
    static void Architecture()
    {
        var ground=Box("DesertGround",exterior,new Vector3(0,-.12f,10.5f),new Vector3(44,.24f,27),"sand_03");ground.AddComponent<JaySurface>().kind=JaySurface.Kind.Sand;
        ground.GetComponent<Renderer>().sharedMaterial.SetTextureScale("_BaseMap",new Vector2(14,9));
        var road=Box("AsphaltRoad",exterior,new Vector3(1,.015f,10.5f),new Vector3(5,.03f,27),"asphalt_03");road.AddComponent<JaySurface>().kind=JaySurface.Kind.Asphalt;
        road.GetComponent<Renderer>().sharedMaterial.SetTextureScale("_BaseMap",new Vector2(2,9));
        for(int side=-1;side<=1;side+=2)Box("GravelShoulder",exterior,new Vector3(1+side*2.85f,.015f,10.5f),new Vector3(.7f,.03f,27),"Gravel");
        for(int i=0;i<9;i++)Box("WornRoadDash",exterior,new Vector3(1,.038f,-1+i*3),new Vector3(.1f,.008f,1.35f),"RoadMarking",false);
        var floor=Box("DedicatedLabFloor",lab,new Vector3(-7,.04f,11),new Vector3(12,.08f,12),"concrete_floor");floor.AddComponent<JaySurface>().kind=JaySurface.Kind.Concrete;
        floor.GetComponent<Renderer>().sharedMaterial.SetTextureScale("_BaseMap",new Vector2(4,4));
        for(int i=0;i<3;i++)
        {
            Wall("NorthWallModule"+i,new Vector3(-11+i*4,.08f,17),new Vector3(4,3.2f,.22f));
            Wall("WestWindowModule"+i,new Vector3(-13,.08f,7+i*4),new Vector3(4,3.2f,.22f),90,true);
            Wall("EastWallModule"+i,new Vector3(-1,.08f,7+i*4),new Vector3(4,3.2f,.22f),90);
        }
        Wall("FrontWindowLeft",new Vector3(-10.8f,.08f,5),new Vector3(4.4f,3.2f,.22f),0,true);
        Wall("FrontWindowRight",new Vector3(-3.2f,.08f,5),new Vector3(4.4f,3.2f,.22f),0,true);
        Box("EntranceLintel",lab,new Vector3(-7,2.73f,5),new Vector3(3.2f,1.1f,.22f),"Architecture");
        Box("DoorJambL",lab,new Vector3(-8.12f,1.12f,5),new Vector3(.14f,2.16f,.24f),"DarkTrim");
        Box("DoorJambR",lab,new Vector3(-5.88f,1.12f,5),new Vector3(.14f,2.16f,.24f),"DarkTrim");
        var hinge=Group("LabEntranceHinge",lab);hinge.transform.position=new Vector3(-8.05f,.08f,4.96f);
        var panel=Model("SM_Door_Panel","DoorPanel",hinge.transform,new Vector3(-7.04f,.08f,4.96f),0,new Vector3(2,2.1f,.09f),true);
        foreach(var r in panel.GetComponentsInChildren<Renderer>())r.sharedMaterial=mats["DoorPaint"];
        var door=hinge.AddComponent<DoorInteractable>();door.doorAudio=hinge.AddComponent<AudioSource>();door.doorAudio.spatialBlend=1;
        door.doorSound=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Original/door.wav");
        for(int x=0;x<3;x++)for(int z=0;z<3;z++)
        {
            var roof=Model("SM_Roof_Flat_4m","RoofPanel",lab,new Vector3(-11+x*4,3.28f,7+z*4),0,new Vector3(4,.16f,4));
            foreach(var r in roof.GetComponentsInChildren<Renderer>())r.sharedMaterial=mats["RoofMetal"];
        }
        Box("ContinuousRoofBacking",lab,new Vector3(-7,3.35f,11),new Vector3(12,.08f,12),"RoofMetal");
        var access=Box("LabAccessPath",exterior,new Vector3(-6.5f,.025f,2.7f),new Vector3(10,.05f,2),"concrete_floor");access.AddComponent<JaySurface>().kind=JaySurface.Kind.Concrete;
        // Visible site boundaries, backed by continuous collision within the existing map limits.
        for(int side=-1;side<=1;side+=2)
        {
            Box("SiteBoundaryWall",exterior,new Vector3(side*21.8f,1.25f,10.5f),new Vector3(.25f,2.5f,27),"Architecture");
            for(int i=0;i<6;i++)Model("modular_chainlink_fence","NorthSouthFence",exterior,new Vector3(-18+i*7.2f,0,side<0?-2.8f:23.8f),0,new Vector3(7,2.2f,.12f),true,true);
        }
        Box("BedroomFloor",bedroom,new Vector3(10.5f,.06f,8.5f),new Vector3(7,.12f,6),"concrete_floor").AddComponent<JaySurface>().kind=JaySurface.Kind.Concrete;
        Box("BedroomBack",bedroom,new Vector3(10.5f,1.5f,11.5f),new Vector3(7,3,.18f),"Architecture");
        Box("BedroomEast",bedroom,new Vector3(14,1.5f,8.5f),new Vector3(.18f,3,6),"Architecture");
        Box("BedroomWest",bedroom,new Vector3(7,1.5f,9.5f),new Vector3(.18f,3,4),"Architecture");
        Box("BedroomFront",bedroom,new Vector3(11.2f,1.5f,5.5f),new Vector3(5.6f,3,.18f),"Architecture");
        Box("BedroomHeader",bedroom,new Vector3(7.7f,2.65f,5.5f),new Vector3(1.4f,.7f,.18f),"Architecture");
        Box("BedroomRoof",bedroom,new Vector3(10.5f,3.05f,8.5f),new Vector3(7,.14f,6),"RoofMetal");
        Box("WarehouseFacade",delivery,new Vector3(17.3f,3,18),new Vector3(8,6,3),"Architecture");
        Box("RollerShutter",delivery,new Vector3(17.3f,1.8f,16.45f),new Vector3(3.6f,3.6f,.08f),"RoofMetal");
        for(int i=0;i<18;i++)Box("ShutterSeam",delivery,new Vector3(17.3f,.2f+i*.19f,16.39f),new Vector3(3.55f,.015f,.02f),"DarkTrim",false);
        Box("DeliveryApron",delivery,new Vector3(10.5f,.025f,14.5f),new Vector3(12,.05f,4),"concrete_floor").AddComponent<JaySurface>().kind=JaySurface.Kind.Concrete;
    }
    static void Props()
    {
        var prep=Model("LabBench","PreparationBench",lab,new Vector3(-10.5f,.08f,14.8f));prep.AddComponent<JayStation>().action=JayStation.Action.Prepare;
        gm.preparationSlot=Slot("PreparationPackageSlot",lab,new Vector3(-10.5f,1.18f,14.8f));
        Model("WallCabinet","PreparationWallCabinet",lab,new Vector3(-10.5f,1.85f,16.65f));
        Model("LabBench","InstrumentBench",lab,new Vector3(-4,.08f,14.8f));
        Model("Centrifuge","Centrifuge",lab,new Vector3(-4.6f,.98f,14.8f));
        Model("PrecisionBalance","PrecisionBalance",lab,new Vector3(-3.6f,.98f,14.8f));
        Model("LabTerminal","LabTerminal",lab,new Vector3(-4.2f,.98f,15));
        Model("FumeHood","VentilatedWorkstation",lab,new Vector3(-12,.08f,10.7f),90);
        Model("LabBench","GlasswareBench",lab,new Vector3(-9.7f,.08f,10.7f),90);
        Model("TestTubeRack","TestTubeRack",lab,new Vector3(-9.7f,.98f,10.7f),90);
        Model("GraduatedBeaker","GraduatedBeaker",lab,new Vector3(-9.7f,.98f,10),0,null,false);
        Model("ConicalFlask","ConicalFlask",lab,new Vector3(-9.7f,.98f,11.3f),0,null,false);
        for(int i=0;i<5;i++)Model("ReagentBottle","SealedReagentBottle",lab,new Vector3(-10.1f+i*.15f,.98f,11.6f),0,null,false);
        var sink=Model("SinkBench","CleaningSink",lab,new Vector3(-4,.08f,7.1f));sink.AddComponent<CleanableStation>();
        Model("WasteBin","CoveredWasteBin",lab,new Vector3(-2,.08f,7.1f));
        var shelf=Model("steel_frame_shelves_01","StorageShelves",lab,new Vector3(-2,.08f,11.5f),90,new Vector3(1.6f,2.1f,.65f));
        var returned=Model("ReturnTray","ReturnShelf",lab,new Vector3(-2.85f,.85f,11.5f),90);
        returned.AddComponent<JayPackageDock>().destination=JayDailyLoop.PackageLocation.Returned;
        gm.returnSlot=Slot("ReturnedPackageSlot",lab,new Vector3(-2.85f,1.40f,11.5f));
        Model("LabStool","LabStool",lab,new Vector3(-8,.08f,11));
        Model("SafetySign","LabSafetySign",lab,new Vector3(-5.4f,1.5f,5.15f),180);
        Model("CableTray","OverheadCableTray",lab,new Vector3(-11,2.8f,16.7f),0,null,false);
        var switchGo=Box("LabLightSwitch",lab,new Vector3(-5.55f,1.3f,5.15f),new Vector3(.12f,.18f,.06f),"SafetyOrange");switchGo.AddComponent<JayStation>().action=JayStation.Action.Lights;
        Model("SM_Vent_Wall","WallVent",lab,new Vector3(-1.18f,2.65f,12),90,new Vector3(.8f,.6f,.12f),false);
        for(int i=0;i<3;i++)Model("SM_Pipe_2m","ExhaustPipe",lab,new Vector3(-12.7f,2.9f,8+i*2),0,new Vector3(.12f,.12f,2),false);
        gm.workspaceClutter=Group("DailyWorkspaceClutter",lab);
        Model("WorkspaceStain","VisibleWorkspaceStain",gm.workspaceClutter.transform,new Vector3(-10.5f,.982f,14.8f),0,null,false);
        Model("GraduatedBeaker","UsedGlassware",gm.workspaceClutter.transform,new Vector3(-10,.98f,14.7f),0,null,false);
        var package=Model("cardboard_box_01","SealedDeliveryPackage",env,new Vector3(-10.5f,1.18f,14.8f),0,new Vector3(.5f,.4f,.36f));
        gm.deliveryPackage=package.AddComponent<LiftableObject>();gm.deliveryPackage.DisplayLabel="Sealed delivery package";
        var seal=Box("TamperSeal",package.transform,package.transform.position+new Vector3(0,.403f,0),new Vector3(.07f,.004f,.36f),"SafetyOrange",false);
        // Root pivots sit at base for authored props; the package uses a centred root for safe placement.
        foreach(Transform child in package.transform)child.localPosition-=Vector3.up*.2f;
        package.GetComponent<BoxCollider>().center-=Vector3.up*.2f;
        var counter=Model("LabBench","CustomerCounter",delivery,new Vector3(9,.05f,14));counter.AddComponent<JayPackageDock>().destination=JayDailyLoop.PackageLocation.Counter;
        gm.counterSlot=Slot("CustomerHandoffSlot",delivery,new Vector3(9,1.16f,14));
        var bed=Model("FinishedBed","BedroomBed",bedroom,new Vector3(12.7f,.12f,9.4f));bed.AddComponent<JayStation>().action=JayStation.Action.Sleep;
        Model("BedroomDesk","BedroomDesk",bedroom,new Vector3(9.4f,.12f,10.6f));
        Model("DeskLamp","BedroomLamp",bedroom,new Vector3(9.7f,.9f,10.6f));
        Model("LabStool","BedroomChair",bedroom,new Vector3(9.4f,.12f,9.6f)).AddComponent<MoveableObject>();
        for(int i=0;i<3;i++)Model("Barrel_02","IndustrialDrum",exterior,new Vector3(-15.2f+i*.8f,0,17.8f),0,new Vector3(.57f,.88f,.57f));
        var crate=Model("SM_Crate_Small","CarryableCrate",lab,new Vector3(-5,.08f,10),0,new Vector3(.55f,.5f,.55f));crate.AddComponent<LiftableObject>().DisplayLabel="Supply crate";
        var drum=Model("Barrel_02","CarryableEmptyDrum",lab,new Vector3(-11,.08f,7.5f),0,new Vector3(.4f,.6f,.4f));drum.AddComponent<LiftableObject>().DisplayLabel="Empty drum";
        var carton=Model("cardboard_box_01","SupplyCarton",lab,new Vector3(-2,1.5f,12),0,new Vector3(.4f,.3f,.35f));carton.AddComponent<LiftableObject>().DisplayLabel="Supply carton";
        Model("SM_Pallet","WoodenPallet",delivery,new Vector3(12,.05f,15),0,new Vector3(1.2f,.15f,1));
        Model("SM_Crate_Large","WarehouseCrate",delivery,new Vector3(12,.2f,15),0,new Vector3(.9f,.8f,.8f));
        Model("utility_box_02","ElectricalServiceBox",exterior,new Vector3(-13.4f,.4f,14),90,new Vector3(.8f,1.2f,.3f));
        Model("exterior_aircon_unit","LabAirConditioner",exterior,new Vector3(-13.4f,1.9f,9),90,new Vector3(.85f,.6f,.4f),true,true);
        Model("modular_electricity_poles","UtilityPole",exterior,new Vector3(4.2f,0,21),0,new Vector3(2,6,.4f),true,true);
        Model("street_lamp_01","StreetLightNorth",exterior,new Vector3(-2.8f,0,20),0,new Vector3(.7f,4,.7f));
        Model("street_lamp_01","StreetLightSouth",exterior,new Vector3(4.1f,0,1),180,new Vector3(.7f,4,.7f));
        Model("SafetySign","DeliverySafetySign",delivery,new Vector3(13.5f,1.4f,16.3f));
    }
    static void Lighting()
    {
        gm.lighting=Group("Lighting",env).AddComponent<JayLightingController>();
        var sun=Group("Sun",gm.lighting.transform).AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(52,-30,0);gm.lighting.sun=sun;RenderSettings.sun=sun;
        var lights=new List<Light>();
        for(int x=0;x<2;x++)for(int z=0;z<2;z++)
        {
            var fixture=Box("CeilingFixture",lab,new Vector3(-10+x*6,3.12f,8+z*6),new Vector3(1.1f,.08f,.25f),"LabelPaper",false);
            var light=Group("CoolLabLight",fixture.transform).AddComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=4.5f;light.color=new Color(.75f,.87f,1);light.transform.localPosition=new Vector3(0,-.2f,0);light.shadows=LightShadows.Soft;lights.Add(light);
        }
        gm.lighting.workspaceLights=lights.ToArray();
        var warm=Group("BedroomWarmLight",bedroom).AddComponent<Light>();warm.type=LightType.Point;warm.transform.position=new Vector3(10.5f,2.5f,8.5f);warm.range=6;warm.intensity=1.7f;warm.color=new Color(1,.72f,.44f);
        var flash=Group("Flashlight",gm.playerCamera.transform).AddComponent<Light>();flash.type=LightType.Spot;flash.range=13;flash.spotAngle=48;flash.intensity=3;flash.color=new Color(1,.92f,.78f);flash.shadows=LightShadows.Soft;flash.enabled=false;gm.lighting.flashlight=flash;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.38f,.48f,.58f);RenderSettings.ambientEquatorColor=new Color(.3f,.28f,.23f);RenderSettings.ambientGroundColor=new Color(.18f,.16f,.12f);
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.62f,.59f,.51f);RenderSettings.fogDensity=.005f;
        var probe=Group("LabReflectionProbe",lab).AddComponent<ReflectionProbe>();probe.transform.position=new Vector3(-7,1.7f,11);probe.size=new Vector3(12,3.2f,12);probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Realtime;probe.refreshMode=UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;probe.resolution=128;probe.boxProjection=true;
        QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
    }
    static void Player()
    {
        var go=Group("Player_Jay",env);go.tag="Player";go.transform.position=new Vector3(-5, .2f,2.8f);go.transform.rotation=Quaternion.Euler(0,-40,0);
        var cc=go.AddComponent<CharacterController>();cc.height=1.75f;cc.radius=.28f;cc.center=new Vector3(0,.875f,0);cc.stepOffset=.25f;
        gm.player=go.AddComponent<JayPlayerController>();gm.player.game=gm;
        var camera=Group("PlayerCamera",go.transform).AddComponent<Camera>();camera.transform.localPosition=new Vector3(0,1.63f,0);camera.nearClipPlane=.08f;camera.farClipPlane=110;camera.fieldOfView=65;camera.tag="MainCamera";camera.gameObject.AddComponent<AudioListener>();gm.playerCamera=camera;gm.player.outputCamera=camera;
        go.AddComponent<JayCameraRig>().Initialize(camera);
        gm.carryPoint=Slot("CarryPoint",camera.transform,Vector3.zero);gm.carryPoint.localPosition=new Vector3(.22f,-.25f,1.1f);
        gm.feedbackAudio=gm.gameObject.AddComponent<AudioSource>();gm.confirmSound=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Kenney/interface-sounds/Audio/confirmation_001.ogg");gm.placementSound=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Kenney/impact-sounds/Audio/impactWood_light_000.ogg");
        gm.player.footsteps=go.AddComponent<AudioSource>();gm.player.asphaltStep=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Original/asphalt.wav");gm.player.concreteStep=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Original/concrete.wav");gm.player.sandStep=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Original/sand.wav");
    }
    static void Characters()
    {
        foreach(var name in new[]{"Jay","DrHale","Customer"})
        {
            string path=Models+name+".fbx";
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;
            importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            var clips=importer.defaultClipAnimations;
            foreach(var clip in clips)clip.loopTime=!clip.name.Contains("Handoff");
            importer.clipAnimations=clips;importer.SaveAndReimport();
            var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if(!avatar || !avatar.isValid || !avatar.isHuman)throw new Exception("Invalid humanoid avatar: "+name);
        }
        string controllerPath=Root+"/Prefabs/Realism/Humanoid.controller";Directory.CreateDirectory(Root+"/Prefabs/Realism");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine=controller.layers[0].stateMachine;
        foreach(var state in machine.states)machine.RemoveState(state.state);
        controller.parameters=new AnimatorControllerParameter[0];controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("Talking",AnimatorControllerParameterType.Bool);controller.AddParameter("Handoff",AnimatorControllerParameterType.Trigger);
        var animations=AssetDatabase.LoadAllAssetsAtPath(Models+"Jay.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
        AnimatorState State(string name)
        {var s=machine.AddState(name);s.motion=animations.First(c=>c.name.EndsWith(name));return s;}
        var idle=State("Idle");var walk=State("Walk");var talk=State("Talk");var handoff=State("Handoff");machine.defaultState=idle;
        void Transition(AnimatorState from,AnimatorState to,AnimatorConditionMode mode,float threshold,string parameter)
        {var t=from.AddTransition(to);t.hasExitTime=false;t.duration=.18f;t.AddCondition(mode,threshold,parameter);}
        Transition(idle,walk,AnimatorConditionMode.Greater,.1f,"Speed");Transition(walk,idle,AnimatorConditionMode.Less,.1f,"Speed");
        Transition(idle,talk,AnimatorConditionMode.If,0,"Talking");Transition(talk,idle,AnimatorConditionMode.IfNot,0,"Talking");
        var any=machine.AddAnyStateTransition(handoff);any.hasExitTime=false;any.duration=.15f;any.AddCondition(AnimatorConditionMode.If,0,"Handoff");
        var back=handoff.AddTransition(idle);back.hasExitTime=true;back.exitTime=.9f;back.duration=.2f;
        GameObject Human(string model,string name,Transform parent,Vector3 p,float yaw,JayHumanAnimation.Role role,bool collision)
        {
            var actor=Model(model,name,parent,p,yaw,null,false);
            var animator=actor.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            var driver=actor.AddComponent<JayHumanAnimation>();driver.animator=animator;driver.game=gm;driver.role=role;
            if(collision){var col=actor.AddComponent<CapsuleCollider>();col.height=1.75f;col.radius=.28f;col.center=new Vector3(0,.875f,0);}
            return actor;
        }
        var jay=Human("Jay","JayBody",gm.player.transform,gm.player.transform.position,gm.player.transform.eulerAngles.y,JayHumanAnimation.Role.Jay,false);gm.player.bodyVisual=jay.transform;
        var hale=Human("DrHale","DrHale",lab,new Vector3(-6.5f,.08f,12.7f),180,JayHumanAnimation.Role.Hale,true);
        gm.hale=hale.AddComponent<TalkableCharacter>();gm.hale.CharacterName="Dr. Hale";gm.hale.headTarget=Slot("HeadTarget",hale.transform,hale.transform.position+Vector3.up*1.6f);
        var customer=Human("Customer","Customer",delivery,new Vector3(9,.05f,15.4f),180,JayHumanAnimation.Role.Customer,true);
        gm.customer=customer.AddComponent<TalkableCharacter>();gm.customer.CharacterName="Customer";gm.customer.headTarget=Slot("HeadTarget",customer.transform,customer.transform.position+Vector3.up*1.6f);
        gm.customerClothing=customer.GetComponentsInChildren<Renderer>().First(r=>r.name.Contains("CustomerClothing"));
        gm.customerVariantA=gm.customerClothing.sharedMaterial;
        string variantPath=Materials+"CustomerClothingVariantB.mat";
        gm.customerVariantB=AssetDatabase.LoadAssetAtPath<Material>(variantPath);
        if(!gm.customerVariantB){gm.customerVariantB=new Material(gm.customerVariantA);gm.customerVariantB.color=new Color(.55f,.65f,.85f);AssetDatabase.CreateAsset(gm.customerVariantB,variantPath);}
    }
    static void Polish()
    {
        var urp=UniversalRenderPipeline.asset;if(!urp)throw new Exception("URP is not active.");
        if(!urp.supportsHDR)urp.supportsHDR=true;
        var data=gm.playerCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=1;
        var volume=Group("GlobalPresentationVolume",env).AddComponent<Volume>();volume.isGlobal=true;
        string profilePath=Materials+"PresentationVolume.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
        if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}
        tone.mode.Override(TonemappingMode.ACES);
        if(!profile.TryGet<ColorAdjustments>(out var color)){color=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(color,profile);}
        color.postExposure.Override(.4f);color.contrast.Override(5);color.saturation.Override(-7);
        if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
        bloom.intensity.Override(.08f);bloom.threshold.Override(1.2f);
        volume.sharedProfile=profile;EditorUtility.SetDirty(profile);EditorUtility.SetDirty(urp);
        foreach(var info in new[]{new {name="FanAmbience",clip="fan",position=new Vector3(-12,2.5f,11),volume=.25f,spatial=1f},new {name="WindAmbience",clip="wind",position=Vector3.zero,volume=.15f,spatial=0f}})
        {
            var a=Group(info.name,env).AddComponent<AudioSource>();a.transform.position=info.position;a.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Original/"+info.clip+".wav");a.loop=true;a.playOnAwake=true;a.volume=info.volume;a.spatialBlend=info.spatial;a.minDistance=2;a.maxDistance=15;
        }
        for(int i=1;i<gm.lighting.workspaceLights.Length;i++)gm.lighting.workspaceLights[i].shadows=LightShadows.None;
    }
    static Text Text(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,int fontSize,string value,TextAnchor align=TextAnchor.UpperLeft)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var text=go.AddComponent<UnityEngine.UI.Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.color=new Color(.91f,.93f,.9f);text.text=value;text.alignment=align;text.raycastTarget=false;text.supportRichText=false;
        var r=text.rectTransform;r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;return text;
    }
    static GameObject Panel(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);go.AddComponent<UnityEngine.UI.Image>().color=color;var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;return go;
    }
    static void UI()
    {
        var canvas=Group("HUD_Canvas").AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;canvas.gameObject.AddComponent<GraphicRaycaster>();
        var strip=Panel(canvas.transform,"StatusStrip",new Vector2(0,1),new Vector2(28,-26),new Vector2(1864,106),new Color(.025f,.045f,.05f,.91f));
        gm.titleText=Text(strip.transform,"Title",new Vector2(0,1),new Vector2(22,-14),new Vector2(400,35),24,"JAY A CAREER");
        gm.objectiveText=Text(strip.transform,"Objective",new Vector2(0,1),new Vector2(22,-58),new Vector2(1500,38),20,"");
        gm.dayText=Text(strip.transform,"Day",new Vector2(1,1),new Vector2(-210,-16),new Vector2(160,34),23,"DAY 1",TextAnchor.UpperRight);
        gm.cashText=Text(strip.transform,"Cash",new Vector2(1,1),new Vector2(-22,-16),new Vector2(160,34),23,"$100",TextAnchor.UpperRight);
        var bottom=Panel(canvas.transform,"ContextPanel",Vector2.zero,new Vector2(28,28),new Vector2(1280,152),new Color(.025f,.045f,.05f,.88f));
        gm.promptText=Text(bottom.transform,"Prompt",Vector2.zero,new Vector2(20,70),new Vector2(1230,58),22,"");
        gm.messageText=Text(bottom.transform,"Message",Vector2.zero,new Vector2(20,14),new Vector2(1230,48),20,"");gm.messageText.color=new Color(.97f,.75f,.42f);
        gm.dialogueText=Text(canvas.transform,"Dialogue",new Vector2(.5f,0),new Vector2(0,210),new Vector2(1200,180),23,"",TextAnchor.MiddleCenter);
        var outline=gm.dialogueText.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,0,0,1);outline.effectDistance=new Vector2(2,-2);
        Text(canvas.transform,"Controls",new Vector2(1,0),new Vector2(-28,28),new Vector2(550,115),18,"WASD / Mouse  Move / Look\nO/Q  Interact / Close   L/R  Carry / Rotate\nT  Talk   C  Clean   V  View   F  Flashlight\nEsc  Pause",TextAnchor.LowerRight);
        Text(canvas.transform,"Reticle",new Vector2(.5f,.5f),Vector2.zero,new Vector2(20,20),20,"+",TextAnchor.MiddleCenter);
        gm.pausePanel=Panel(canvas.transform,"PausePanel",new Vector2(.5f,.5f),Vector2.zero,new Vector2(620,390),new Color(.025f,.045f,.05f,.98f));
        Text(gm.pausePanel.transform,"PausedTitle",new Vector2(.5f,1),new Vector2(0,-35),new Vector2(500,60),34,"PAUSED",TextAnchor.MiddleCenter);
        foreach(var resume in new[]{true,false})
        {
            var buttonGo=Panel(gm.pausePanel.transform,resume?"Resume":"Quit",new Vector2(.5f,.5f),new Vector2(0,resume?20:-85),new Vector2(360,72),new Color(.12f,.25f,.25f));
            var button=buttonGo.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=buttonGo.GetComponent<UnityEngine.UI.Image>();
            if(resume)UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,gm.Resume);else UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,gm.Quit);
            Text(buttonGo.transform,"Label",new Vector2(.5f,.5f),Vector2.zero,new Vector2(330,64),25,resume?"Resume":"Quit",TextAnchor.MiddleCenter);
        }
        gm.pausePanel.SetActive(false);
        var es=Group("EventSystem");es.AddComponent<EventSystem>();es.AddComponent<InputSystemUIInputModule>();
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before authoring.");
        AssetDatabase.Refresh();Textures();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        inventory.Clear();env=Group("AuthoredWorld").transform;lab=Group("Laboratory",env).transform;exterior=Group("Exterior",env).transform;bedroom=Group("Bedroom",env).transform;delivery=Group("CustomerArea",env).transform;
        gm=Group("JayGameManager").AddComponent<JayGameManager>();
        Architecture();Props();Player();Characters();Lighting();Polish();UI();
        // Saved prefabs separate stable interaction roots from replaceable model children.
        Directory.CreateDirectory(Root+"/Prefabs/Realism");
        foreach(var name in new[]{"PreparationBench","CustomerCounter","CleaningSink","ReturnShelf","BedroomBed","LabEntranceHinge","SealedDeliveryPackage","InstrumentBench","GlasswareBench","StorageShelves","DrHale","Customer","JayBody"})
        {
            var obj=GameObject.Find(name);if(obj)PrefabUtility.SaveAsPrefabAssetAndConnect(obj,Root+"/Prefabs/Realism/"+name+".prefab",InteractionMode.AutomatedAction);
        }
        string scene=Root+"/Scenes/JayACareer_Realism.unity";
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),scene);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scene,true)};
        PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.productName="Jay A Career";
        Directory.CreateDirectory("Deliverables/Evidence");File.WriteAllText("Deliverables/Evidence/scene-inventory.json",JArray.FromObject(inventory).ToString());
        AssetDatabase.SaveAssets();
        RealismReviewEntry.Prepare();
        return "Saved authored realism scene with "+inventory.Count+" model instances.";
    }
}


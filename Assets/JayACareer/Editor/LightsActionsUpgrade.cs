using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class LightsActionsUpgrade
{
    const string Root="Assets/JayACareer/";
    [MenuItem("Jay A Career/Apply Bedroom Switch and Daily Animations")]
    public static void Apply()
    {
        EditorSceneManager.OpenScene(Root+"Scenes/JayACareer_Realism.unity");
        var game=Object.FindFirstObjectByType<JayGameManager>();
        var bedroom=GameObject.Find("Bedroom").transform;
        game.lighting.bedroomLights=bedroom.GetComponentsInChildren<Light>(true);
        if(game.lighting.bedroomLights.Length==0)throw new Exception("Missing bedroom light.");
        if(game.lighting.roadLights.Length!=2 || game.lighting.roadLights.Any(l=>!l))throw new Exception("Both roadside lights must remain wired.");
        string lampPath=Root+"Materials/Realism/EveningLampBulbActive.mat";
        var lampMaterial=AssetDatabase.LoadAssetAtPath<Material>(lampPath);
        if(!lampMaterial)
        {
            lampMaterial=new Material(AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/EveningLampBulb.mat"));
            AssetDatabase.CreateAsset(lampMaterial,lampPath);
        }
        // A black authoring emission colour makes URP remove the emission keyword.
        // Keep the variant in the build; the daylight controller supplies black via
        // a property block in the morning and warm emission after dusk.
        lampMaterial.SetColor("_EmissionColor",new Color(1,.69f,.32f)*4);
        lampMaterial.EnableKeyword("_EMISSION");
        lampMaterial.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(lampMaterial);
        foreach(var bulb in game.lighting.roadLampBulbs)
        {
            var materials=bulb.sharedMaterials;
            for(int i=0;i<materials.Length;i++)if(materials[i] && materials[i].name.StartsWith("EveningLampBulb"))materials[i]=lampMaterial;
            bulb.sharedMaterials=materials;
        }

        var plate=GameObject.Find("BedroomLightSwitch");
        if(!plate)
        {
            plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name="BedroomLightSwitch";
            plate.transform.SetParent(bedroom,false);
        }
        // Beside the entrance on the inside face of the west bedroom wall.
        plate.transform.position=new Vector3(7.125f,1.35f,8.1f);
        plate.transform.rotation=Quaternion.Euler(0,-90,0);plate.transform.localScale=new Vector3(.24f,.34f,.045f);
        plate.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/LabelPaper.mat");
        var station=GetOrAdd<JayStation>(plate);station.action=JayStation.Action.BedroomLights;
        var rocker=plate.transform.Find("Rocker");
        if(!rocker)
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Rocker";rocker=cube.transform;
            rocker.SetParent(plate.transform,false);Object.DestroyImmediate(cube.GetComponent<Collider>());
        }
        rocker.localPosition=new Vector3(0,0,-.7f);rocker.localScale=new Vector3(.5f,.5f,.7f);
        rocker.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/DarkTrim.mat");
        game.lighting.bedroomSwitchRocker=rocker;game.lighting.bedroomLightsStartOn=true;
        rocker.localRotation=Quaternion.Euler(-15,0,0);
        var caption=bedroom.Find("BedroomLightCaption");
        if(!caption){caption=new GameObject("BedroomLightCaption").transform;caption.SetParent(bedroom,false);}
        caption.position=new Vector3(7.15f,1.64f,8.1f);caption.rotation=Quaternion.Euler(0,-90,0);caption.localScale=Vector3.one*.3f;
        var text=GetOrAdd<TextMesh>(caption.gameObject);
        text.text="BEDROOM LIGHT\n[O] ON / OFF";text.fontSize=40;text.characterSize=.035f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.15f,.14f,.12f);

        var actions=GetOrAdd<JayActionPresentation>(game.gameObject);
        game.actions=actions;actions.game=game;
        var preview=game.transform.Find("PreparationPreview");
        if(!preview)
        {
            var parcel=Object.Instantiate(game.deliveryPackage.gameObject,game.transform);
            parcel.name="PreparationPreview";preview=parcel.transform;
            foreach(var behaviour in parcel.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(behaviour);
            foreach(var collider in parcel.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            foreach(var rigidbody in parcel.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(rigidbody);
        }
        preview.SetPositionAndRotation(game.preparationSlot.position,game.preparationSlot.rotation);
        preview.gameObject.SetActive(false);actions.preparationPreview=preview.gameObject;
        actions.preparationPose=Anchor("PreparationActionPose",new Vector3(-10.5f,.08f,14.15f),Quaternion.identity,game.transform);
        actions.cleaningPose=Anchor("CleaningActionPose",new Vector3(-4,.08f,7.8f),Quaternion.Euler(0,180,0),game.transform);
        actions.sleepingPose=Anchor("SleepingActionPose",new Vector3(12.7f,.79f,10.2f),Quaternion.LookRotation(Vector3.up,Vector3.back),game.transform);
        actions.preparationShot=Shot("PreparationActionShot",new Vector3(-8.65f,1.9f,13.1f),new Vector3(-10.5f,1,14.2f),game.transform);
        actions.cleaningShot=Shot("CleaningActionShot",new Vector3(-6.1f,1.95f,9.2f),new Vector3(-4,1,7.75f),game.transform);
        actions.sleepingShot=Shot("SleepingActionShot",new Vector3(10.65f,2.35f,7.7f),new Vector3(12.7f,.85f,9.35f),game.transform);
        actions.preparationLeftHand=Anchor("PreparationLeftHand",new Vector3(-10.72f,1.25f,14.55f),Quaternion.identity,game.transform);
        actions.preparationRightHand=Anchor("PreparationRightHand",new Vector3(-10.28f,1.3f,14.55f),Quaternion.identity,game.transform);
        actions.cleaningLeftHand=Anchor("CleaningLeftHand",new Vector3(-3.75f,1.28f,7.4f),Quaternion.identity,game.transform);
        actions.cleaningRightHand=Anchor("CleaningRightHand",new Vector3(-4.15f,1.28f,7.4f),Quaternion.identity,game.transform);
        var animator=game.player.bodyVisual.GetComponentInChildren<Animator>(true);
        if(!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)throw new Exception("Jay needs a valid humanoid avatar.");
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        var cloth=hand.Find("CleaningCloth");
        if(!cloth)
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="CleaningCloth";cloth=cube.transform;
            cloth.SetParent(hand,false);Object.DestroyImmediate(cube.GetComponent<Collider>());
        }
        cloth.localPosition=new Vector3(0,.025f,.03f);cloth.localRotation=Quaternion.identity;
        var handScale=hand.lossyScale;cloth.localScale=new Vector3(.16f/handScale.x,.025f/handScale.y,.12f/handScale.z);
        var clothMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/CleaningCloth.mat");
        if(!clothMaterial){clothMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));clothMaterial.color=new Color(.2f,.62f,.75f);AssetDatabase.CreateAsset(clothMaterial,Root+"Materials/Realism/CleaningCloth.mat");}
        cloth.GetComponent<Renderer>().sharedMaterial=clothMaterial;cloth.gameObject.SetActive(false);actions.cleaningCloth=cloth.gameObject;
        string controllerPath=Root+"Prefabs/Realism/PlayerActions.controller";
        if(!File.Exists(controllerPath))AssetDatabase.CopyAsset(Root+"Prefabs/Realism/Humanoid.controller",controllerPath);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        var idle=controller.animationClips.First(c=>c.name.EndsWith("|Idle"));
        foreach(string action in new[]{"Prepare","Clean","Sleep"})
        {
            var clip=CreateClip(action,action=="Sleep"?5:3.2f,idle);
            var machine=controller.layers[0].stateMachine;
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==action)??machine.AddState(action);
            state.motion=clip;state.writeDefaultValues=true;
        }
        animator.runtimeAnimatorController=controller;
        EditorUtility.SetDirty(controller);EditorUtility.SetDirty(game);EditorUtility.SetDirty(game.lighting);EditorUtility.SetDirty(actions);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Deliverables/Evidence");
        File.WriteAllText("Deliverables/Evidence/lights-actions-authoring-20261018.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
            scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
            roadLights=game.lighting.roadLights.Length,bedroomLights=game.lighting.bedroomLights.Select(l=>l.name).ToArray(),
            bedroomSwitch=new[]{plate.transform.position.x,plate.transform.position.y,plate.transform.position.z},animations=new[]{"Prepare","Clean","Sleep"},
            animator=animator.name,avatar=animator.avatar.name,clips=controller.animationClips.Select(c=>new{c.name,c.length,c.humanMotion}).ToArray()
        },Newtonsoft.Json.Formatting.Indented));
        Debug.Log("JAY LIGHTS ACTIONS: authored bedroom switch and three humanoid action clips.");
    }
    static T GetOrAdd<T>(GameObject obj) where T:Component
    {
        var component=obj.GetComponent<T>();
        return component ? component : obj.AddComponent<T>();
    }
    static Transform Anchor(string name,Vector3 position,Quaternion rotation,Transform parent)
    {
        var anchor=parent.Find(name);if(!anchor){anchor=new GameObject(name).transform;anchor.SetParent(parent,false);}
        anchor.SetPositionAndRotation(position,rotation);return anchor;
    }
    static Transform Shot(string name,Vector3 position,Vector3 target,Transform parent)=>Anchor(name,position,Quaternion.LookRotation(target-position),parent);
    static AnimationClip CreateClip(string name,float duration,AnimationClip idle)
    {
        string folder=Root+"Animations";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(Root.TrimEnd('/'),"Animations");
        string path=folder+"/"+name+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
        clip.ClearCurves();clip.name=name;clip.frameRate=30;
        // Muscle-only clips default their body origin to ground level. Preserve the
        // imported humanoid's body translation and orientation so feet stay on the floor.
        foreach(string property in new[]{"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"})
        {
            var binding=EditorCurveBinding.FloatCurve("",typeof(Animator),property);
            var original=AnimationUtility.GetEditorCurve(idle,binding);
            if(original==null)throw new Exception("Idle animation has no body curve: "+property);
            AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,duration,original.Evaluate(0)));
        }
        // Humanoid muscle curves retarget to Jay's existing avatar without replacing the rig.
        foreach(string muscle in HumanTrait.MuscleName)
        {
            var baseline=AnimationUtility.GetEditorCurve(idle,EditorCurveBinding.FloatCurve("",typeof(Animator),muscle));
            Curve(clip,muscle,baseline!=null?baseline.Evaluate(0):0,0,duration);
        }
        Curve(clip,"Left Arm Down-Up",-.68f,.015f,duration);Curve(clip,"Right Arm Down-Up",-.68f,.015f,duration);
        Curve(clip,"Left Forearm Stretch",.5f,0,duration);Curve(clip,"Right Forearm Stretch",.5f,0,duration);
        if(name=="Sleep")
        {
            Curve(clip,"Left Arm Down-Up",-.75f,0,duration);Curve(clip,"Right Arm Down-Up",-.75f,0,duration);
            Curve(clip,"Left Forearm Stretch",.98f,0,duration);Curve(clip,"Right Forearm Stretch",.98f,0,duration);
            Curve(clip,"Left Arm Front-Back",.12f,0,duration);Curve(clip,"Right Arm Front-Back",.12f,0,duration);
            Curve(clip,"Chest Front-Back",.04f,.015f,duration,1.2f);
            Curve(clip,"Head Nod Down-Up",-.07f,.015f,duration,1.2f);
        }
        else
        {
            Curve(clip,"Spine Front-Back",-.10f,.02f,duration);
            Curve(clip,"Chest Front-Back",-.08f,.015f,duration);
            Curve(clip,"Head Nod Down-Up",-.16f,.025f,duration);
            Curve(clip,"Left Arm Front-Back",-.4f,name=="Prepare"?.08f:.03f,duration);
            Curve(clip,"Right Arm Front-Back",-.4f,.10f,duration);
            Curve(clip,"Left Arm Down-Up",-.1f,.03f,duration);
            Curve(clip,"Right Arm Down-Up",name=="Clean"?0:-.1f,.04f,duration);
            Curve(clip,"Left Forearm Stretch",.9f,.06f,duration);
            Curve(clip,"Right Forearm Stretch",.9f,.07f,duration);
            Curve(clip,"Right Hand In-Out",0,.2f,duration);
            Curve(clip,"Spine Left-Right",0,name=="Clean"?.1f:.04f,duration);
        }
        var settings=AnimationUtility.GetAnimationClipSettings(idle);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
        EditorUtility.SetDirty(clip);return clip;
    }
    static void Curve(AnimationClip clip,string muscle,float center,float amplitude,float duration,float frequency=2)
    {
        if(!HumanTrait.MuscleName.Contains(muscle))throw new Exception("Unknown humanoid muscle: "+muscle);
        if(amplitude==0)
        {AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),muscle),AnimationCurve.Constant(0,duration,center));return;}
        var curve=new AnimationCurve();
        int count=Mathf.CeilToInt(duration*30);
        for(int i=0;i<=count;i++){float t=duration*i/count;curve.AddKey(t,center+amplitude*Mathf.Sin(t*Mathf.PI*2*frequency));}
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),muscle),curve);
    }
    public static void Build(){ResponseFileFullBuild.BuildTo("Windows Lights Actions 20261018","lights-actions-build-20261018.json");}
    public static void ApplyAndReview(){Apply();RealismReviewEntry.StartValidation();}
    public static void ApplyAndBuild(){Apply();Build();}
}

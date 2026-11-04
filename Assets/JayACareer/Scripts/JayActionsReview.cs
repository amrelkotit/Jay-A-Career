using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Explicit opt-in integration review in the built player. Never starts in normal play.
public class JayActionsReview : MonoBehaviour
{
    readonly List<string> checks=new List<string>();
    string directory;
    JayGameManager game;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        if(Environment.GetCommandLineArgs().Contains("-jay-actions-review"))
        {Application.runInBackground=true;new GameObject("OptInDailyActionsReview").AddComponent<JayActionsReview>();}
    }
    IEnumerator Start()
    {
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-jay-review-output");
        directory=index>=0?args[index+1]:Path.Combine(Application.persistentDataPath,"LightsActionsReview");
        Directory.CreateDirectory(directory);
        var sequence=Review();
        while(true)
        {
            object next;
            try{if(!sequence.MoveNext())break;next=sequence.Current;}
            catch(Exception error)
            {
                File.WriteAllText(Path.Combine(directory,"review-result.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=false,checks,error=error.ToString()},Newtonsoft.Json.Formatting.Indented));
                Debug.LogException(error);Finish(1);yield break;
            }
            yield return next;
        }
        File.WriteAllText(Path.Combine(directory,"review-result.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,checks,captures=Directory.GetFiles(directory,"*.png").Select(Path.GetFileName).ToArray()},Newtonsoft.Json.Formatting.Indented));
        Finish(0);
    }
    static void Finish(int code)
    {
#if UNITY_EDITOR
        if(Environment.GetCommandLineArgs().Contains("-jay-review-keep-editor"))
        {
            if(code==0)UnityEditor.EditorApplication.isPlaying=false;
            else UnityEditor.EditorApplication.isPaused=true;
            return;
        }
        UnityEditor.EditorApplication.Exit(code);
#else
        Application.Quit(code);
#endif
    }
    void Check(bool condition,string description){if(!condition)throw new Exception(description);checks.Add(description);}
    void TimeAt(float seconds)
    {
        typeof(JayLightingController).GetField("<CycleTime>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game.lighting,seconds);
        game.lighting.SendMessage("UpdateDaylight");
    }
    void Capture(string name)
    {
        var texture=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;var target=game.playerCamera.targetTexture;
        game.playerCamera.targetTexture=texture;game.playerCamera.Render();RenderTexture.active=texture;
        image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
        File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());
        game.playerCamera.targetTexture=target;RenderTexture.active=previous;Destroy(image);Destroy(texture);
    }
    void Position(Vector3 position,Vector3 target)
    {
        var player=game.player;var controller=player.GetComponent<CharacterController>();controller.enabled=false;
        player.transform.position=position;
        var direction=target-(position+Vector3.up*1.6f);player.transform.rotation=Quaternion.Euler(0,Quaternion.LookRotation(direction).eulerAngles.y,0);
        player.GetComponent<JayCameraRig>().LookPivot.localRotation=Quaternion.Euler(Quaternion.LookRotation(direction).eulerAngles.x,0,0);
        controller.enabled=true;
    }
    IEnumerator Review()
    {
        yield return null;yield return null;
        game=FindFirstObjectByType<JayGameManager>();game.player.enabled=false;
        game.player.bodyVisual.gameObject.SetActive(game.player.GetComponent<JayCameraRig>().IsThirdPerson);
        Check(game.actions && !game.IsPerformingAction,"Saved scene has action presentation and starts idle");
        var light=game.lighting;
        TimeAt(0);Check(light.roadLights.Length==2 && light.roadLights.All(l=>!l.enabled),"Both street lights are off in the morning");
        game.Prepare();game.CleanWorkspace();game.Sleep();
        Check(!game.IsPerformingAction && game.Loop.Current==JayDailyLoop.Stage.Briefing,"Actions respect the briefing prerequisite");
        TimeAt(light.DayCycleSeconds*.5f);Check(light.roadLights.All(l=>l.enabled && l.intensity>11),"Both street lights automatically illuminate at night");
        Check(light.roadLampBulbs.All(r=>r.sharedMaterials.Any(m=>m && m.IsKeywordEnabled("_EMISSION"))),"Street lamp glass and bulb materials retain the emission shader variant");
        var emission=new MaterialPropertyBlock();
        foreach(var bulb in light.roadLampBulbs){bulb.GetPropertyBlock(emission);Check(emission.GetColor("_EmissionColor").maxColorComponent>1,"Street lamp renderer receives visible night emission: "+bulb.name);}
        Position(new Vector3(2.5f,.1f,9),new Vector3(-2.8f,3.5f,20));
        yield return new WaitForSeconds(.7f);Capture("street-lights-night");
        var bedroom=FindObjectsByType<JayStation>(FindObjectsSortMode.None).Single(s=>s.action==JayStation.Action.BedroomLights);
        bool labState=light.WorkspaceLightsOn;
        Position(new Vector3(8.5f,.1f,8.1f),new Vector3(7.125f,1.35f,8.1f));
        yield return new WaitForSeconds(.7f);Capture("bedroom-light-on");
        Check(Physics.Raycast(new Vector3(8.5f,1.35f,8.1f),Vector3.left,out var hit,2) && hit.collider.GetComponent<JayStation>()==bedroom,"Bedroom switch collider is reachable from inside the room");
        bedroom.Interact(game);Check(!light.BedroomLightsOn && light.bedroomLights.All(l=>!l.enabled),"Bedroom switch turns its lights off");
        Check(light.WorkspaceLightsOn==labState && light.roadLights.All(l=>l.enabled),"Bedroom switch leaves laboratory and street lights independent");
        yield return null;Capture("bedroom-light-off");
        bedroom.Interact(game);Check(light.BedroomLightsOn && light.bedroomLights.All(l=>l.enabled),"Bedroom switch turns its lights back on");
        TimeAt(0);if(!light.WorkspaceLightsOn)light.ToggleWorkspaceLights();
        Check(game.Loop.Brief(),"Briefing unlocks preparation");
        Position(new Vector3(-10.5f,.08f,13.9f),new Vector3(-10.5f,1.2f,14.8f));
        var originalLocal=game.player.bodyVisual.localPosition;
        game.Prepare();Check(game.IsPerformingAction && game.Loop.Current==JayDailyLoop.Stage.Preparation && !game.deliveryPackage.gameObject.activeSelf && game.actions.preparationPreview.activeSelf,"Preparation shows a preview parcel while animating before creating the deliverable");
        game.Prepare();game.CleanWorkspace();game.Sleep();Check(game.Loop.PackageId==0,"Repeated and overlapping action requests cannot advance the day");
        yield return new WaitForSeconds(.8f);
        var animator=game.player.bodyVisual.GetComponentInChildren<Animator>();
        Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Prepare"),"Prepare humanoid animation is playing");
        var foot=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
        Check(animator.GetBoneTransform(HumanBodyBones.Hips).position.y>.6f && foot.y>0 && foot.y<.25f,"Preparation keeps Jay standing on the floor");
        var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var handPosition=hand.position;
        yield return new WaitForSeconds(.13f);Check(Vector3.Distance(handPosition,hand.position)>.003f,"Preparation visibly moves Jay's hand");
        Capture("preparing");
        game.TogglePause();float normalized=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        yield return new WaitForSecondsRealtime(.4f);
        Check(game.IsPerformingAction && Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).normalizedTime-normalized)<.03f && game.Loop.PackageId==0,"Pause freezes animation and defers action completion");game.Resume();
        while(game.IsPerformingAction)yield return null;
        Check(game.Loop.Current==JayDailyLoop.Stage.Delivery && game.Loop.PackageId==1 && game.deliveryPackage.gameObject.activeSelf && !game.actions.preparationPreview.activeSelf,"Preparation creates exactly one package after the animation");
        Check(!game.player.bodyVisual.gameObject.activeSelf && Vector3.Distance(originalLocal,game.player.bodyVisual.localPosition)<.001f,"Action restores first-person visibility and body transform");
        game.BeginCarry(game.deliveryPackage);Check(game.PlacePackageAt(JayDailyLoop.PackageLocation.Counter),"Prepared package can be delivered");
        game.TalkTo(game.customer);game.ResolveDialogue("accept");Check(game.Loop.Cash==150 && game.Loop.Current==JayDailyLoop.Stage.Cleaning,"Customer payment and cleaning prerequisite remain intact");
        Position(new Vector3(-4,.08f,8.05f),new Vector3(-4,1,7.1f));game.CleanWorkspace();
        Check(game.IsPerformingAction && game.Loop.Dirty && game.workspaceClutter.activeSelf,"Cleaning leaves the stain visible until the animation completes");
        yield return new WaitForSeconds(1);
        Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Clean") && game.actions.cleaningCloth.activeSelf,"Clean humanoid animation and handheld cloth are visible");
        Capture("cleaning");while(game.IsPerformingAction)yield return null;
        Check(!game.Loop.Dirty && !game.workspaceClutter.activeSelf && !game.actions.cleaningCloth.activeSelf && game.Loop.Current==JayDailyLoop.Stage.Sleep,"Cleaning removes the stain and cloth only after completing");
        game.player.GetComponent<JayCameraRig>().ToggleView(game);
        game.player.bodyVisual.gameObject.SetActive(true);
        Position(new Vector3(11.5f,.12f,8.7f),new Vector3(12.7f,.7f,9.4f));game.Sleep();
        Check(game.IsPerformingAction && game.Loop.Day==1,"Sleeping defers the day reset until after the animation");
        yield return new WaitForSeconds(1);
        Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Sleep") && Mathf.Abs(Vector3.Dot(game.player.bodyVisual.up,Vector3.up))<.1f && Vector3.Dot(game.player.bodyVisual.forward,Vector3.up)>.9f,"Sleep animation lies Jay on his back on the bed");
        Capture("sleeping");while(game.IsPerformingAction)yield return null;
        Check(game.Loop.Day==2 && game.Loop.Cash==150 && game.Loop.Current==JayDailyLoop.Stage.Briefing && light.CycleTime<1,"Sleep begins the next morning with cash preserved");
        Check(game.player.GetComponent<JayCameraRig>().IsThirdPerson && game.player.bodyVisual.gameObject.activeSelf,"Action restores third-person view and body visibility");
        Check(!game.deliveryPackage.gameObject.activeSelf && light.roadLights.All(l=>!l.enabled),"New morning clears the package and automatically switches off street lights");
        yield return new WaitForSeconds(.7f);Capture("morning-after-sleep");
    }
}

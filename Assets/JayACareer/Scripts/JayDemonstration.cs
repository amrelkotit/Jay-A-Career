using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>Opt-in reproducible evidence route. Never runs during ordinary player sessions.</summary>
public class JayDemonstration : MonoBehaviour
{
    [Serializable] public class Report {public bool completed;public string error;public int days;public int cash;public int frames;public int width;public int height;public float averageFps;public float percentile1Fps;public string directory;public string environment;public int recordedWidth;public int recordedHeight;public string device;public string gpu;public List<string> steps=new List<string>();}
    JayGameManager g;CharacterController cc;JayCameraRig rig;Animator jay;Report report=new Report();
    string output,outputRoot;bool recording;float captureTime;int frame;List<float> samples=new List<float>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Launch()
    {var args=Environment.GetCommandLineArgs();if(args.Contains("-jay-demo")||args.Contains("-jay-benchmark")){Application.runInBackground=true;new GameObject("OptInEvidenceRoute").AddComponent<JayDemonstration>();}}
    void Start(){StartCoroutine(Run());}
    IEnumerator Run()
    {
        yield return null;g=FindFirstObjectByType<JayGameManager>();cc=g.player.GetComponent<CharacterController>();rig=g.player.GetComponent<JayCameraRig>();
        g.player.enabled=false;jay=g.player.bodyVisual.GetComponentInChildren<Animator>();
        recording=Environment.GetCommandLineArgs().Contains("-jay-demo");
        outputRoot=Path.Combine(Application.dataPath,"..","Evidence",recording?"Demo":"Benchmark");output=Path.Combine(outputRoot,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(output);report.directory=Path.GetFullPath(output);
        Screen.SetResolution(1920,1080,FullScreenMode.Windowed);Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
        report.environment=Application.isEditor?"Unity Editor":"Windows player";report.recordedWidth=1920;report.recordedHeight=1080;report.device=SystemInfo.processorType;report.gpu=SystemInfo.graphicsDeviceName;
        Application.logMessageReceived+=Log;
        if(recording)StartCoroutine(Capture());
        yield return Wait(3,"Morning: a compact industrial site, one daily delivery.");
        var door=FindFirstObjectByType<DoorInteractable>();door.Open(g);
        for(int day=1;day<=2;day++)
        {
            yield return Walk(new Vector3(-7,.1f,3.6f));yield return Walk(new Vector3(-7,.1f,7));yield return Walk(new Vector3(-7,.1f,11.4f));
            LookAt(g.hale.headTarget.position);g.TalkTo(g.hale);yield return Wait(4,"T: Dr. Hale briefs Jay before preparation.");g.ContinueBriefing();
            yield return Walk(new Vector3(-10.5f,.1f,13.6f));LookAt(g.preparationSlot.position);g.Prepare();yield return Wait(3,"O: one sealed package; visible workspace clutter appears.");
            g.BeginCarry(g.deliveryPackage);g.deliveryPackage.RotateCarried();yield return Wait(2,"L carries one object; R rotates it in the hand.");
            rig.ToggleView(g);g.player.bodyVisual.gameObject.SetActive(true);
            yield return Walk(new Vector3(-7,.1f,13.6f));yield return Walk(new Vector3(-7,.1f,7));yield return Walk(new Vector3(-7,.1f,3.3f));
            yield return Walk(new Vector3(1,.1f,2.7f));yield return Walk(new Vector3(5.5f,.1f,3.4f));yield return Walk(new Vector3(5.5f,.1f,12.2f));yield return Walk(new Vector3(9,.1f,12.5f));
            rig.ToggleView(g);g.player.bodyVisual.gameObject.SetActive(false);LookAt(g.counterSlot.position);g.PlacePackageAt(JayDailyLoop.PackageLocation.Counter);
            yield return Wait(3,"L at the counter snaps the correct package into its handoff position.");g.TalkTo(g.customer);
            yield return Wait(5,day==1?"A accepts $50. C offers $75 with a disclosed 50% acceptance chance.":"Day 2 demonstrates rejection and the required unsold return.");
            g.ResolveDialogue(day==1?"accept":"reject");yield return Wait(3,g.Loop.Feedback);
            if(day==2)g.BeginCarry(g.deliveryPackage);
            yield return Walk(new Vector3(5.5f,.1f,12.2f));yield return Walk(new Vector3(5.5f,.1f,3.4f));yield return Walk(new Vector3(1,.1f,2.7f));yield return Walk(new Vector3(-7,.1f,3.3f));yield return Walk(new Vector3(-7,.1f,7));
            if(day==2){yield return Walk(new Vector3(-4,.1f,9.5f));yield return Walk(new Vector3(-3.8f,.1f,11.4f));LookAt(g.returnSlot.position);g.PlacePackageAt(JayDailyLoop.PackageLocation.Returned);yield return Wait(3,"The unsold package returns to the laboratory shelf before cleaning.");}
            yield return Walk(new Vector3(-4,.1f,8.3f));LookAt(new Vector3(-4,1,7.1f));
            g.lighting.ToggleWorkspaceLights();g.lighting.ToggleFlashlight();yield return Wait(3,"O switches the four laboratory lights; F toggles Jay's flashlight.");g.lighting.ToggleWorkspaceLights();g.lighting.ToggleFlashlight();
            g.CleanWorkspace();yield return Wait(3,"C cleans the visible clutter after the delivery outcome is settled.");
            yield return Walk(new Vector3(-7,.1f,7));yield return Walk(new Vector3(-7,.1f,3.3f));yield return Walk(new Vector3(1,.1f,2.7f));yield return Walk(new Vector3(7.7f,.1f,4.4f));yield return Walk(new Vector3(7.7f,.2f,6.4f));yield return Walk(new Vector3(10.7f,.2f,7.8f));
            LookAt(new Vector3(12.7f,1,9.4f));g.Sleep();yield return Wait(4,"O at the bed begins another day, resets morning and retains session cash.");
            if(day==1){yield return Walk(new Vector3(7.7f,.2f,6.4f));yield return Walk(new Vector3(7.7f,.1f,4.4f));yield return Walk(new Vector3(1,.1f,2.7f));}
        }
        report.completed=String.IsNullOrEmpty(report.error)&&g.Loop.Day==3&&g.Loop.Cash==150;
        report.days=g.Loop.Day;report.cash=g.Loop.Cash;report.frames=frame;report.width=Screen.width;report.height=Screen.height;
        if(samples.Count>0){report.averageFps=1/samples.Average();samples.Sort();report.percentile1Fps=1/samples[Mathf.Min(samples.Count-1,(int)(samples.Count*.99f))];}
        WriteReport();
        recording=false;yield return new WaitForSecondsRealtime(1);
#if UNITY_EDITOR
        Debug.Log("Evidence route complete: "+report.completed);
        if(Application.isBatchMode)UnityEditor.EditorApplication.Exit(report.completed?0:1);
#else
        Application.Quit();
#endif
    }
    void WriteReport(){File.WriteAllText(Path.Combine(output,"route-report.json"),JsonUtility.ToJson(report,true));File.WriteAllText(Path.Combine(outputRoot,"route-report.json"),JsonUtility.ToJson(report,true));}
    IEnumerator Wait(float seconds,string label){report.steps.Add(label);g.ShowMessage(label);float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end){g.SetPrompt("Scripted walkthrough • "+g.Loop.Current);yield return null;}}
    void LookAt(Vector3 target){Vector3 delta=target-rig.LookPivot.position;g.player.transform.rotation=Quaternion.Euler(0,Quaternion.LookRotation(delta).eulerAngles.y,0);rig.LookPivot.localRotation=Quaternion.Euler(Mathf.DeltaAngle(0,Quaternion.LookRotation(delta).eulerAngles.x),0,0);}
    IEnumerator Walk(Vector3 target)
    {
        rig.LookPivot.localRotation=Quaternion.identity;float began=Time.realtimeSinceStartup;jay.SetFloat("Speed",1);g.player.SetPresentationMovement(1);
        while(Vector2.Distance(new Vector2(g.player.transform.position.x,g.player.transform.position.z),new Vector2(target.x,target.z))>.22f)
        {
            if(Time.realtimeSinceStartup-began>20){report.error="Blocked route to "+target;WriteReport();throw new Exception(report.error);}
            Vector3 delta=target-g.player.transform.position;delta.y=0;g.player.transform.rotation=Quaternion.Slerp(g.player.transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*7);
            cc.Move((delta.normalized*3+Vector3.down*2)*Time.deltaTime);yield return null;
        }
        jay.SetFloat("Speed",0);g.player.SetPresentationMovement(0);
    }
    void Update(){if(Time.realtimeSinceStartup>10&&Time.unscaledDeltaTime>0)samples.Add(Time.unscaledDeltaTime);}
    IEnumerator Capture()
    {
        while(recording)
        {
            yield return null;
            if(Time.realtimeSinceStartup>=captureTime){captureTime=Time.realtimeSinceStartup+.1f;var texture=CaptureCamera();File.WriteAllBytes(Path.Combine(output,"frame-"+(frame++).ToString("D5")+".jpg"),texture.EncodeToJPG(85));Destroy(texture);}
        }
    }
    Texture2D CaptureCamera()
    {
        var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);
        var previous=g.playerCamera.targetTexture;var active=RenderTexture.active;
        g.playerCamera.targetTexture=rt;
        var canvas=g.titleText.GetComponentInParent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=g.playerCamera;canvas.planeDistance=.4f;canvas.GetComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;canvas.scaleFactor=1;if(frame==0)foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>(true))text.SetAllDirty();Canvas.ForceUpdateCanvases();
        g.playerCamera.Render();RenderTexture.active=rt;
        var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
        g.playerCamera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);return texture;
    }
    void Log(string text,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)report.error=text;}
}

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Unity.Cinemachine;

// Opt-in player visual review; inactive in normal game sessions.
public class JayLightingReview:MonoBehaviour
{
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Launch(){if(Environment.GetCommandLineArgs().Contains("-jay-lighting-review")){Application.runInBackground=true;new GameObject("OptInLightingReview").AddComponent<JayLightingReview>();}}
 IEnumerator Start()
 {
  yield return null;var g=FindFirstObjectByType<JayGameManager>();g.player.enabled=false;
  var brain=g.playerCamera.GetComponent<CinemachineBrain>();if(brain)brain.enabled=false;g.player.bodyVisual.gameObject.SetActive(false);g.deliveryPackage.gameObject.SetActive(true);
  Screen.SetResolution(1920,1080,FullScreenMode.Windowed);string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Evidence","LabelsLighting"));Directory.CreateDirectory(dir);
  var rt=new RenderTexture(1920,1080,24);var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
  void TimeAt(float seconds){typeof(JayLightingController).GetField("<CycleTime>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g.lighting,seconds);g.lighting.SendMessage("UpdateDaylight");}
  void View(Vector3 p,Vector3 target){g.playerCamera.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));}
  void Capture(string name){g.playerCamera.targetTexture=rt;g.playerCamera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(dir,name+".png"),pixels.EncodeToPNG());RenderTexture.active=null;g.playerCamera.targetTexture=null;}
  if(!g.lighting.WorkspaceLightsOn)g.lighting.ToggleWorkspaceLights();
  View(new Vector3(-10.5f,1.5f,13.1f),new Vector3(-10.5f,1.05f,14.8f));yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Capture("labels-preparation-package");
  g.lighting.ToggleWorkspaceLights();TimeAt(0);View(new Vector3(-7,1.7f,7.3f),new Vector3(-7,1.3f,14));yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Capture("daylight-lights-off");
  TimeAt(60);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Capture("night-lights-off");
  g.lighting.ToggleFlashlight();yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Capture("night-flashlight");g.lighting.ToggleFlashlight();
  g.lighting.ToggleWorkspaceLights();yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Capture("night-lab-switch-on");
  g.lighting.ToggleWorkspaceLights();TimeAt(0);View(new Vector3(-7,1.7f,8),new Vector3(-12.8f,1.6f,9));yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Capture("daylight-window");
  TimeAt(60);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Capture("night-window");
  File.WriteAllText(Path.Combine(dir,"review-completed.json"),"{\"environment\":\"Windows player\",\"completed\":true,\"captures\":7}");Destroy(rt);Destroy(pixels);Application.Quit();
 }
}

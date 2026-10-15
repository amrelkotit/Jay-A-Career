using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public class JayInputValidation:MonoBehaviour
{
 [Serializable]public class Result{public bool passed;public string error;public string inputSource="Synthetic Input System devices; headless world-input method invocation";public int checks;public List<string> assertions=new List<string>();public List<string> manualChecks=new List<string>();}
 JayGameManager g;Result result=new Result();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Launch(){if(Environment.GetCommandLineArgs().Contains("-jay-validation"))new GameObject("OptInInputValidation").AddComponent<JayInputValidation>();}
 IEnumerator Start()
 {
  yield return null;yield return null;
  var routine=Run();
  while(true)
  {
   bool next;try{next=routine.MoveNext();}catch(Exception e){result.error=e.ToString();break;}
   if(!next){result.passed=String.IsNullOrEmpty(result.error);break;}
   yield return routine.Current;
  }
  var folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Deliverables","Evidence"));Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"input-physics-checks.json"),JsonUtility.ToJson(result,true));Debug.Log("INPUT CHECKS "+JsonUtility.ToJson(result));
#if UNITY_EDITOR
  if(Application.isBatchMode)UnityEditor.EditorApplication.Exit(result.passed?0:1);
#else
  Application.Quit();
#endif
 }
 void Check(bool ok,string text){result.checks++;result.assertions.Add((ok?"PASS ":"FAIL ")+text);if(!ok)throw new Exception(text);}
 void ResetRules()
 {
  g.EndDialogue();typeof(JayGameManager).GetField("<Loop>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,new JayDailyLoop());
 }
 void PlayerUpdate(){bool world=!g.IsPaused&&!g.DialogueActive&&g.activePlacement==null;typeof(JayPlayerController).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g.player,null);if(Application.isBatchMode&&world&&Cursor.lockState!=CursorLockMode.Locked)typeof(JayPlayerController).GetMethod("UpdateWorldInput",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g.player,null);}
 void UpdateInput(){typeof(InputSystem).GetMethod("Update",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});}
 IEnumerator KeyPress(Key key,bool manager=false)
 {
  InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));UpdateInput();Debug.Log("INPUT TEST "+key+" pressed="+Keyboard.current[key].wasPressedThisFrame+" held="+Keyboard.current[key].isPressed+" dialogue="+g.DialogueActive);
  try{PlayerUpdate();if(manager)typeof(JayGameManager).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,null);}catch(Exception e){result.error=e.ToString();Debug.LogError(e);}
  InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());UpdateInput();yield return null;
 }
 void AimAt(Vector3 target)
 {
  Vector3 origin=g.player.transform.position+Vector3.up*1.63f;var look=Quaternion.LookRotation(target-origin);float pitch=Mathf.DeltaAngle(0,look.eulerAngles.x);
  g.player.transform.rotation=Quaternion.Euler(0,look.eulerAngles.y,0);g.player.GetComponent<JayCameraRig>().LookPivot.localRotation=Quaternion.Euler(pitch,0,0);
  typeof(JayPlayerController).GetField("pitch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g.player,pitch);g.playerCamera.transform.SetPositionAndRotation(origin,look);
 }
 void Move(Vector3 position)
 {var cc=g.player.GetComponent<CharacterController>();cc.enabled=false;g.player.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}
 void ReadyOffer()
 {ResetRules();g.TalkTo(g.hale);g.ContinueBriefing();g.Prepare();g.BeginCarry(g.deliveryPackage);g.PlacePackageAt(JayDailyLoop.PackageLocation.Counter);g.TalkTo(g.customer);}
 IEnumerator Run()
 {
  g=FindFirstObjectByType<JayGameManager>();g.player.enabled=false;Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;InputSystem.AddDevice<Keyboard>().MakeCurrent();if(Mouse.current==null)InputSystem.AddDevice<Mouse>();InputSystem.EnableDevice(Keyboard.current);
#if UNITY_EDITOR
  InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
  foreach(var button in Keyboard.current.allKeys){var warm=button.wasPressedThisFrame;}
  Check(Keyboard.current!=null,"Synthetic Input System keyboard available");
  var original=g.player.transform.position;
  g.TalkTo(g.hale);yield return KeyPress(Key.A);Check(g.player.transform.position==original&&g.DialogueActive,"A during Hale briefing does not move or accept");
  yield return KeyPress(Key.C);Check(g.Loop.Current==JayDailyLoop.Stage.Briefing,"C during Hale briefing cannot clean or counter");
  yield return KeyPress(Key.T);Check(g.Loop.Current==JayDailyLoop.Stage.Preparation&&!g.DialogueActive,"T continues Hale briefing");
  ReadyOffer();original=g.player.transform.position;yield return KeyPress(Key.A);
  Check(g.Loop.Cash==150&&!g.DialogueActive,"A settles a $50 offer once");Check(g.player.transform.position==original,"Accept key does not strafe");
  g.ResolveDialogue("accept");Check(g.Loop.Cash==150,"Repeated response cannot pay twice");
  ReadyOffer();yield return KeyPress(Key.C);Check(g.Loop.NegotiationResolved&&g.Loop.Dirty,"C settles a counter without cleaning");
  int cash=g.Loop.Cash;g.ResolveDialogue("counter");Check(g.Loop.Cash==cash,"Counter cannot be retried");
  ReadyOffer();yield return KeyPress(Key.R);Check(g.Loop.Current==JayDailyLoop.Stage.Return&&g.Loop.Cash==100,"R rejects and requires return");
  g.CleanWorkspace();Check(g.Loop.Dirty,"Premature cleaning preserves visible work");g.BeginCarry(g.deliveryPackage);g.PlacePackageAt(JayDailyLoop.PackageLocation.Returned);
  g.CleanWorkspace();g.Sleep();Check(g.Loop.Day==2&&g.Loop.Cash==100,"Returned rejection completes another day");
  Check(g.customerClothing.sharedMaterial==g.customerVariantB,"Even day uses clothing variant B");
  yield return KeyPress(Key.Escape);Check(g.IsPaused&&Time.timeScale==0&&g.pausePanel.activeSelf,"Esc opens pause and freezes time");
  yield return KeyPress(Key.C);Check(g.Loop.Current==JayDailyLoop.Stage.Briefing,"Paused keys cannot progress tasks");yield return KeyPress(Key.Escape);
  Check(!g.IsPaused&&Time.timeScale==1,"Esc resumes and restores time");
  if(Application.isBatchMode)result.manualChecks.Add("Cursor capture requires an interactive Game View or Windows player; headless Unity has no mouse-capture window.");else Check(Cursor.lockState==CursorLockMode.Locked,"Resume captures mouse");
  var chair=FindFirstObjectByType<MoveableObject>();var start=chair.transform.position;chair.Interact(g);chair.transform.position+=Vector3.left*.4f;yield return KeyPress(Key.Escape);
  Check(g.activePlacement==null&&!g.IsPaused&&Vector3.Distance(chair.transform.position,start)<.001f,"Esc cancels arrangement before pause");
  chair.Interact(g);yield return KeyPress(Key.Enter,true);Check(g.activePlacement==null&&chair.IsPlaced,"Enter confirms without a null placement reference");
  var door=FindFirstObjectByType<DoorInteractable>();door.Open(g);Move(new Vector3(-7,.1f,4.96f));door.Close(g);Check(door.IsOpen,"Door closure refuses occupied player volume");
  Move(new Vector3(-5,.2f,2.8f));door.Close(g);Check(!door.IsOpen,"Door closes after doorway is cleared");door.Open(g);
  yield return new WaitForSecondsRealtime(.6f);Move(new Vector3(-5,.1f,8.4f));AimAt(new Vector3(-5,.33f,10));
  var crate=FindObjectsByType<LiftableObject>(FindObjectsSortMode.None).First(x=>x.DisplayLabel=="Supply crate");
  yield return KeyPress(Key.L);Check(g.CarriedObject==crate&&crate.IsCarried,"L targets and picks up an ordinary prop");
  var rotation=crate.transform.rotation;yield return KeyPress(Key.R);Check(Quaternion.Angle(rotation,crate.transform.rotation)>40,"R rotates a carried prop");
  AimAt(new Vector3(-5,.08f,9.225f));yield return KeyPress(Key.L);
  Check(g.CarriedObject==null&&!crate.IsCarried&&crate.GetComponent<Collider>().enabled,"L places prop on a valid surface and restores collision");
  var col=crate.GetComponent<BoxCollider>();Check(col.bounds.min.y>.075f,"Placed prop does not clip the lab floor");
  ResetRules();g.TalkTo(g.hale);g.ContinueBriefing();g.Prepare();int id=g.Loop.PackageId;g.BeginCarry(g.deliveryPackage);
  AimAt(new Vector3(-5,.08f,9.225f));g.DropCarried();Check(g.CarriedObject==null,"Package can be placed on an ordinary surface");
  g.deliveryPackage.transform.position=Vector3.down*4;g.RecoverPackage();Check(g.Loop.PackageId==id&&g.Loop.Package==JayDailyLoop.PackageLocation.Bench,"Lost package recovery preserves identity");
  Check(Vector3.Distance(g.deliveryPackage.transform.position,g.preparationSlot.position)<.001f,"Recovered package returns to preparation slot");
  Move(new Vector3(9,.1f,12.5f));g.BeginCarry(g.deliveryPackage);AimAt(g.counterSlot.position);yield return KeyPress(Key.L);
  Check(g.Loop.Package==JayDailyLoop.PackageLocation.Counter&&g.CarriedObject==null,"L targeting the counter docks the daily package");
  AimAt(g.customer.headTarget.position);yield return KeyPress(Key.T);Check(g.CustomerDialogue,"T targets the customer after physical handoff");
  yield return KeyPress(Key.R);yield return new WaitForSecondsRealtime(.6f);g.BeginCarry(g.deliveryPackage);Move(new Vector3(-3.8f,.1f,11.4f));AimAt(new Vector3(-2.85f,1.1f,11.5f));yield return KeyPress(Key.L);
  Check(g.Loop.Package==JayDailyLoop.PackageLocation.Returned&&g.CarriedObject==null,"L targeting the return shelf docks an unsold package");
  var rig=g.player.GetComponent<JayCameraRig>();rig.ToggleView(g);PlayerUpdate();Check(rig.IsThirdPerson&&g.player.bodyVisual.gameObject.activeSelf,"Third-person view shows Jay");
  g.TalkTo(g.hale);Check(!g.player.bodyVisual.gameObject.activeSelf,"Dialogue hides Jay to prevent head obstruction");g.EndDialogue();Check(g.player.bodyVisual.gameObject.activeSelf,"Dialogue restores third-person body");
  rig.ToggleView(g);PlayerUpdate();Check(!g.player.bodyVisual.gameObject.activeSelf,"First-person view hides Jay");
  g.lighting.ToggleFlashlight();Check(g.lighting.flashlight.enabled,"Flashlight enables");g.lighting.ToggleFlashlight();Check(!g.lighting.flashlight.enabled,"Flashlight disables");
  bool labWasOn=g.lighting.WorkspaceLightsOn;if(!labWasOn)g.lighting.ToggleWorkspaceLights();g.lighting.ToggleWorkspaceLights();Check(g.lighting.workspaceLights.All(l=>!l.enabled),"All four lab fixtures switch off");if(labWasOn)g.lighting.ToggleWorkspaceLights();
  typeof(JayLightingController).GetField("<CycleTime>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g.lighting,60f);g.lighting.SendMessage("UpdateDaylight");float night=g.lighting.sun.intensity;
  g.lighting.BeginNextDay();Check(night<.2f&&g.lighting.sun.intensity>1.4f&&g.lighting.CycleTime==0,"Ambient cycle differs at midpoint and resets to morning");
  Check(FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None).Length==3,"Three saved cameras persist after interactions");
 }
}



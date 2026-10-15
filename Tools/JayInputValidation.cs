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
 [Serializable]public class Result{public bool passed;public string error;public int checks;public List<string> assertions=new List<string>();}
 JayGameManager g;Result result=new Result();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Launch(){if(Environment.GetCommandLineArgs().Contains("-jay-validation"))new GameObject("OptInInputValidation").AddComponent<JayInputValidation>();}
 IEnumerator Start()
 {
  yield return null;yield return null;
  try{Run();result.passed=true;}catch(Exception e){result.error=e.ToString();}
  Directory.CreateDirectory("Deliverables/Evidence");File.WriteAllText("Deliverables/Evidence/input-physics-checks.json",JsonUtility.ToJson(result,true));
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
 void KeyPress(Key key,bool manager=false)
 {
  InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));InputSystem.Update();
  g.player.SendMessage("Update");if(manager)g.SendMessage("Update");
  InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.Update();
 }
 void Move(Vector3 position)
 {var cc=g.player.GetComponent<CharacterController>();cc.enabled=false;g.player.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}
 void ReadyOffer()
 {ResetRules();g.TalkTo(g.hale);g.ContinueBriefing();g.Prepare();g.BeginCarry(g.deliveryPackage);g.PlacePackageAt(JayDailyLoop.PackageLocation.Counter);g.TalkTo(g.customer);}
 void Run()
 {
  g=FindFirstObjectByType<JayGameManager>();g.player.enabled=false;
  Check(Keyboard.current!=null,"Keyboard device available");
  var original=g.player.transform.position;
  g.TalkTo(g.hale);KeyPress(Key.A);Check(g.player.transform.position==original&&g.DialogueActive,"A during Hale briefing does not move or accept");
  KeyPress(Key.C);Check(g.Loop.Current==JayDailyLoop.Stage.Briefing,"C during Hale briefing cannot clean or counter");
  KeyPress(Key.T);Check(g.Loop.Current==JayDailyLoop.Stage.Preparation&&!g.DialogueActive,"T continues Hale briefing");
  ReadyOffer();original=g.player.transform.position;KeyPress(Key.A);
  Check(g.Loop.Cash==150&&!g.DialogueActive,"A settles a $50 offer once");Check(g.player.transform.position==original,"Accept key does not strafe");
  g.ResolveDialogue("accept");Check(g.Loop.Cash==150,"Repeated response cannot pay twice");
  ReadyOffer();KeyPress(Key.C);Check(g.Loop.NegotiationResolved&&g.Loop.Dirty,"C settles a counter without cleaning");
  int cash=g.Loop.Cash;g.ResolveDialogue("counter");Check(g.Loop.Cash==cash,"Counter cannot be retried");
  ReadyOffer();KeyPress(Key.R);Check(g.Loop.Current==JayDailyLoop.Stage.Return&&g.Loop.Cash==100,"R rejects and requires return");
  g.CleanWorkspace();Check(g.Loop.Dirty,"Premature cleaning preserves visible work");g.BeginCarry(g.deliveryPackage);g.PlacePackageAt(JayDailyLoop.PackageLocation.Returned);
  g.CleanWorkspace();g.Sleep();Check(g.Loop.Day==2&&g.Loop.Cash==100,"Returned rejection completes another day");
  Check(g.customerClothing.sharedMaterial==g.customerVariantB,"Even day uses clothing variant B");
  KeyPress(Key.Escape);Check(g.IsPaused&&Time.timeScale==0&&g.pausePanel.activeSelf,"Esc opens pause and freezes time");
  KeyPress(Key.C);Check(g.Loop.Current==JayDailyLoop.Stage.Briefing,"Paused keys cannot progress tasks");KeyPress(Key.Escape);
  Check(!g.IsPaused&&Time.timeScale==1&&Cursor.lockState==CursorLockMode.Locked,"Esc resumes and captures mouse");
  var chair=FindFirstObjectByType<MoveableObject>();var start=chair.transform.position;chair.Interact(g);chair.transform.position+=Vector3.left*.4f;KeyPress(Key.Escape);
  Check(g.activePlacement==null&&!g.IsPaused&&Vector3.Distance(chair.transform.position,start)<.001f,"Esc cancels arrangement before pause");
  chair.Interact(g);KeyPress(Key.Enter,true);Check(g.activePlacement==null&&chair.IsPlaced,"Enter confirms without a null placement reference");
  var door=FindFirstObjectByType<DoorInteractable>();door.Open(g);Move(new Vector3(-7,.1f,4.96f));door.Close(g);Check(door.IsOpen,"Door closure refuses occupied player volume");
  Move(new Vector3(-5,.2f,2.8f));door.Close(g);Check(!door.IsOpen,"Door closes after doorway is cleared");door.Open(g);
  Move(new Vector3(-5,.1f,8.4f));g.playerCamera.transform.SetPositionAndRotation(new Vector3(-5,1.73f,8.4f),Quaternion.LookRotation(new Vector3(0,-1.4f,1.6f)));
  var crate=FindObjectsByType<LiftableObject>(FindObjectsSortMode.None).First(x=>x.DisplayLabel=="Supply crate");
  KeyPress(Key.L);Check(g.CarriedObject==crate&&crate.IsCarried,"L targets and picks up an ordinary prop");
  var rotation=crate.transform.rotation;KeyPress(Key.R);Check(Quaternion.Angle(rotation,crate.transform.rotation)>40,"R rotates a carried prop");
  g.playerCamera.transform.rotation=Quaternion.LookRotation(new Vector3(0,-1,.5f));KeyPress(Key.L);
  Check(g.CarriedObject==null&&!crate.IsCarried&&crate.GetComponent<Collider>().enabled,"L places prop on a valid surface and restores collision");
  var col=crate.GetComponent<BoxCollider>();Check(col.bounds.min.y>.075f,"Placed prop does not clip the lab floor");
  ResetRules();g.TalkTo(g.hale);g.ContinueBriefing();g.Prepare();int id=g.Loop.PackageId;g.BeginCarry(g.deliveryPackage);
  g.playerCamera.transform.rotation=Quaternion.LookRotation(new Vector3(0,-1,.5f));g.DropCarried();Check(g.CarriedObject==null,"Package can be placed on an ordinary surface");
  g.deliveryPackage.transform.position=Vector3.down*4;g.RecoverPackage();Check(g.Loop.PackageId==id&&g.Loop.Package==JayDailyLoop.PackageLocation.Bench,"Lost package recovery preserves identity");
  Check(Vector3.Distance(g.deliveryPackage.transform.position,g.preparationSlot.position)<.001f,"Recovered package returns to preparation slot");
  var rig=g.player.GetComponent<JayCameraRig>();rig.ToggleView(g);g.player.SendMessage("Update");Check(rig.IsThirdPerson&&g.player.bodyVisual.gameObject.activeSelf,"Third-person view shows Jay");
  g.TalkTo(g.hale);Check(!g.player.bodyVisual.gameObject.activeSelf,"Dialogue hides Jay to prevent head obstruction");g.EndDialogue();Check(g.player.bodyVisual.gameObject.activeSelf,"Dialogue restores third-person body");
  rig.ToggleView(g);g.player.SendMessage("Update");Check(!g.player.bodyVisual.gameObject.activeSelf,"First-person view hides Jay");
  g.lighting.ToggleFlashlight();Check(g.lighting.flashlight.enabled,"Flashlight enables");g.lighting.ToggleFlashlight();Check(!g.lighting.flashlight.enabled,"Flashlight disables");
  g.lighting.ToggleWorkspaceLights();Check(g.lighting.workspaceLights.All(l=>!l.enabled),"All four lab fixtures switch off");g.lighting.ToggleWorkspaceLights();
  typeof(JayLightingController).GetField("<CycleTime>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g.lighting,60f);g.lighting.SendMessage("UpdateDaylight");float night=g.lighting.sun.intensity;
  g.lighting.BeginNextDay();Check(night<.2f&&g.lighting.sun.intensity>1.4f&&g.lighting.CycleTime==0,"Ambient cycle differs at midpoint and resets to morning");
  Check(FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None).Length==3,"Three saved cameras persist after interactions");
 }
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class SceneChecks
{
    public static string Main()
    {
        var g=UnityEngine.Object.FindFirstObjectByType<JayGameManager>();
        if(!EditorApplication.isPlaying)throw new Exception("Requires Play mode");
        int checks=0;var rows=new List<object>();
        void Check(bool ok,string name){checks++;rows.Add(new {name,passed=ok});if(!ok)throw new Exception(name);}
        var backing=typeof(JayGameManager).GetField("<Loop>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
        foreach(var outcome in new[]{"accept","counter-success","counter-refusal","reject"})
        {
            backing.SetValue(g,new JayDailyLoop());g.EndDialogue();
            g.Sleep();Check(g.Loop.Day==1,"Early sleep blocked: "+outcome);
            g.CleanWorkspace();Check(g.Loop.Current==JayDailyLoop.Stage.Briefing,"Early clean blocked");
            g.TalkTo(g.customer);Check(!g.DialogueActive,"No-package negotiation blocked");
            g.TalkTo(g.hale);Check(g.DialogueActive&&!g.CustomerDialogue,"Hale briefing owns dialogue");g.ContinueBriefing();
            g.Prepare();int id=g.Loop.PackageId;g.Prepare();Check(g.Loop.PackageId==id,"Exactly one package");
            Check(g.workspaceClutter.activeSelf&&g.deliveryPackage.gameObject.activeSelf,"Authored clutter and package activated");
            g.BeginCarry(g.deliveryPackage);Check(g.CarriedObject==g.deliveryPackage,"Package carry reference");
            Check(g.PlacePackageAt(JayDailyLoop.PackageLocation.Counter),"Handoff dock placement");
            Check(Vector3.Distance(g.deliveryPackage.transform.position,g.counterSlot.position)<.01f,"Handoff exact position");
            if(outcome.StartsWith("counter"))
            {
                int seed=0;for(;seed<100;seed++){UnityEngine.Random.InitState(seed);bool success=UnityEngine.Random.value<.5f;if(success==(outcome=="counter-success"))break;}
                UnityEngine.Random.InitState(seed);
            }
            g.TalkTo(g.customer);g.ResolveDialogue(outcome.StartsWith("counter")?"counter":outcome);
            int expected=outcome=="accept"?150:outcome=="counter-success"?175:100;
            Check(g.Loop.Cash==expected,"Correct settlement: "+outcome);g.ResolveDialogue("accept");
            Check(g.Loop.Cash==expected,"Duplicate payment blocked");
            if(g.Loop.Current==JayDailyLoop.Stage.Return){g.CleanWorkspace();Check(g.Loop.Dirty,"Return required before cleaning");g.BeginCarry(g.deliveryPackage);Check(g.PlacePackageAt(JayDailyLoop.PackageLocation.Returned),"Unsold return dock");}
            g.CleanWorkspace();Check(!g.workspaceClutter.activeSelf&&!g.Loop.Dirty,"Visible cleanup");g.Sleep();
            Check(g.Loop.Day==2&&g.Loop.Cash==expected&&g.Loop.Current==JayDailyLoop.Stage.Briefing,"Day reset keeps cash");
        }
        g.TogglePause();Check(g.IsPaused&&Time.timeScale==0&&g.pausePanel.activeSelf,"Pause panel freezes time");g.Resume();Check(!g.IsPaused&&Time.timeScale==1,"Resume restores time");
        bool flash=g.lighting.flashlight.enabled;g.lighting.ToggleFlashlight();Check(g.lighting.flashlight.enabled!=flash,"Flashlight toggle");g.lighting.ToggleFlashlight();
        bool lab=g.lighting.workspaceLights[0].enabled;g.lighting.ToggleWorkspaceLights();Check(g.lighting.workspaceLights.All(l=>l.enabled!=lab),"Grouped lab switch");g.lighting.ToggleWorkspaceLights();
        Check(UnityEngine.Object.FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None).Length==3,"Three saved Cinemachine cameras");
        Directory.CreateDirectory("Deliverables/Evidence");File.WriteAllText("Deliverables/Evidence/scene-checks.json",JsonConvert.SerializeObject(new{checks,rows},Formatting.Indented));
        return checks+" scene integration checks passed";
    }
}


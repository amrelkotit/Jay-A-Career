using System;
using System.Collections;
using UnityEngine;

/// <summary>Plays authored humanoid clips before committing a daily-loop action.</summary>
public class JayActionPresentation : MonoBehaviour
{
    public enum Kind { Prepare, Clean, Sleep }
    public JayGameManager game;
    public Transform preparationPose, cleaningPose, sleepingPose;
    public Transform preparationShot, cleaningShot, sleepingShot;
    public Transform preparationLeftHand, preparationRightHand, cleaningLeftHand, cleaningRightHand;
    public GameObject cleaningCloth, preparationPreview;
    public float preparationSeconds=3.2f, cleaningSeconds=3.2f, sleepingSeconds=5;
    public bool IsBusy {get;private set;}
    public Kind Current {get;private set;}
    Transform body;
    Animator animator;
    Vector3 originalPosition;
    Quaternion originalRotation;
    bool originalVisibility;
    float actionSeconds, actionDuration;

    public void Begin(Kind kind, Action complete)
    {
        if(IsBusy || !game || game.IsPaused || game.DialogueActive)return;
        body=game.player.bodyVisual;
        animator=body ? body.GetComponentInChildren<Animator>(true) : null;
        Transform pose=kind==Kind.Prepare?preparationPose:kind==Kind.Clean?cleaningPose:sleepingPose;
        Transform shot=kind==Kind.Prepare?preparationShot:kind==Kind.Clean?cleaningShot:sleepingShot;
        if(!animator || !animator.runtimeAnimatorController || !pose || !shot)
        {game.ShowMessage("This action is missing its animation setup.");return;}
        StartCoroutine(Perform(kind,complete));
    }
    IEnumerator Perform(Kind kind, Action complete)
    {
        IsBusy=true;Current=kind;
        body=game.player.bodyVisual;
        originalPosition=body.localPosition;originalRotation=body.localRotation;
        originalVisibility=body.gameObject.activeSelf;
        body.gameObject.SetActive(true);
        animator.SetFloat("Speed",0);animator.SetBool("Talking",false);animator.ResetTrigger("Handoff");
        string state=kind==Kind.Prepare?"Prepare":kind==Kind.Clean?"Clean":"Sleep";
        animator.CrossFadeInFixedTime(state,.22f);
        game.player.SetPresentationMovement(0);
        game.player.GetComponent<JayCameraRig>().BeginActionView(kind==Kind.Prepare?preparationShot:kind==Kind.Clean?cleaningShot:sleepingShot);
        game.ShowMessage(kind==Kind.Prepare?"Preparing and sealing the package...":kind==Kind.Clean?"Wiping down the workspace...":"Resting until morning...");
        if(cleaningCloth)cleaningCloth.SetActive(kind==Kind.Clean);
        if(preparationPreview)preparationPreview.SetActive(kind==Kind.Prepare);
        float duration=kind==Kind.Prepare?preparationSeconds:kind==Kind.Clean?cleaningSeconds:sleepingSeconds;
        float elapsed=0;actionSeconds=0;actionDuration=duration;
        Vector3 start=body.position;Quaternion rotation=body.rotation;
        Transform pose=kind==Kind.Prepare?preparationPose:kind==Kind.Clean?cleaningPose:sleepingPose;
        while(elapsed<duration)
        {
            elapsed+=Time.deltaTime;
            actionSeconds=elapsed;
            // Ease into the station and return to the safe player position before control resumes.
            float weight=Mathf.SmoothStep(0,1,Mathf.Min(elapsed/.5f,(duration-elapsed)/.5f));
            body.position=Vector3.Lerp(start,pose.position,weight);
            body.rotation=Quaternion.Slerp(rotation,pose.rotation,weight);
            yield return null;
        }
        RestorePresentation();
        IsBusy=false;
        complete?.Invoke();
    }
    public bool ApplyHandIK(Animator actor)
    {
        if(!IsBusy || Current==Kind.Sleep)return false;
        var left=Current==Kind.Prepare?preparationLeftHand:cleaningLeftHand;
        var right=Current==Kind.Prepare?preparationRightHand:cleaningRightHand;
        if(!left || !right)return false;
        float weight=Mathf.SmoothStep(0,1,Mathf.Min(actionSeconds/.4f,(actionDuration-actionSeconds)/.4f));
        float wave=Mathf.Sin(actionSeconds*Mathf.PI*4);
        SetHand(actor,AvatarIKGoal.LeftHand,left.position+Vector3.up*(Current==Kind.Prepare?.025f*wave:0),weight);
        SetHand(actor,AvatarIKGoal.RightHand,right.position+(Current==Kind.Clean?Vector3.right*.14f*wave:Vector3.up*.05f*wave),weight);
        return true;
    }
    static void SetHand(Animator actor,AvatarIKGoal goal,Vector3 position,float weight)
    {
        actor.SetIKPositionWeight(goal,weight);
        actor.SetIKPosition(goal,position);
        actor.SetIKRotationWeight(goal,0);
    }
    void RestorePresentation()
    {
        if(cleaningCloth)cleaningCloth.SetActive(false);
        if(preparationPreview)preparationPreview.SetActive(false);
        if(animator)animator.CrossFadeInFixedTime("Idle",.15f);
        if(body){body.localPosition=originalPosition;body.localRotation=originalRotation;body.gameObject.SetActive(originalVisibility);}
        if(game && game.player)game.player.GetComponent<JayCameraRig>().EndActionView();
    }
    void OnDisable(){if(IsBusy){StopAllCoroutines();RestorePresentation();IsBusy=false;}}
}

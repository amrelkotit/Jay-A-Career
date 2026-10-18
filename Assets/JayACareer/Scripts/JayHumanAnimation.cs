using UnityEngine;
public class JayHumanAnimation : MonoBehaviour
{
    public enum Role { Jay, Hale, Customer }
    public Role role;
    public JayGameManager game;
    public Animator animator;
    int lastHandoffDay;
    void OnAnimatorIK(int layer)
    {
        if(role!=Role.Jay||!animator||!game)return;
        if(game.IsPerformingAction && game.actions.ApplyHandIK(animator))return;
        bool holding=game.CarriedObject&&game.player.GetComponent<JayCameraRig>().IsThirdPerson;
        foreach(var goal in new[]{AvatarIKGoal.LeftHand,AvatarIKGoal.RightHand})
        {
            animator.SetIKPositionWeight(goal,holding?.85f:0);
            if(holding)animator.SetIKPosition(goal,game.carryPoint.position+game.player.transform.right*(goal==AvatarIKGoal.LeftHand?-.2f:.2f)-Vector3.up*.05f);
        }
    }
    void Update()
    {
        if (!game || !animator || !animator.runtimeAnimatorController) return;
        if(role==Role.Jay && game.IsPerformingAction)return;
        animator.SetFloat("Speed",role==Role.Jay ? game.player.MovementAmount : 0);
        animator.SetBool("Talking",game.DialogueActive && (role==Role.Hale && !game.CustomerDialogue || role==Role.Customer && game.CustomerDialogue));
        if (game.Loop.NegotiationResolved && lastHandoffDay!=game.Loop.Day)
        { lastHandoffDay=game.Loop.Day; if(role!=Role.Hale) animator.SetTrigger("Handoff"); }
    }
}

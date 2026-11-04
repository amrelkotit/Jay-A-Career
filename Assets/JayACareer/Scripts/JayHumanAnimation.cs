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
        bool talking;
        if(DialogueManager.IsOpen)
        {
            // Story dialogue: only the character whose line is on screen animates.
            var speaker=DialogueManager.Instance.CurrentSpeaker;
            var self=role==Role.Hale?game.hale:role==Role.Customer?game.customer:null;
            talking=speaker&&self&&self.profile==speaker;
        }
        else talking=game.DialogueActive && (role==Role.Hale && !game.CustomerDialogue || role==Role.Customer && game.CustomerDialogue);
        animator.SetBool("Talking",talking);
        if (game.Loop.NegotiationResolved && lastHandoffDay!=game.Loop.Day)
        { lastHandoffDay=game.Loop.Day; if(role!=Role.Hale) animator.SetTrigger("Handoff"); }
    }
}

using UnityEngine;
public class ContextualInteractable : MonoBehaviour
{
    public virtual string DisplayName => gameObject.name;
    public virtual string PromptText => "[O] Interact";
    public virtual void Interact(JayGameManager game) { if (game != null) game.ShowMessage("This object has no action yet."); }
}

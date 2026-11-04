using UnityEngine;

/// <summary>
/// Who is speaking: display name, portrait and name colour. One asset per character
/// (Jay, Dr. Hale, Leo, Luka, Paul) so the dialogue box always presents them consistently.
/// </summary>
[CreateAssetMenu(menuName = "Jay A Career/Character Profile", fileName = "Character")]
public class CharacterProfile : ScriptableObject
{
    public string displayName = "Character";
    public Sprite portrait;
    public Color nameColor = new Color(.95f, .78f, .45f);
    [TextArea] public string shortBio;
}

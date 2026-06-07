using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dialogue Speaker Profile", fileName = "New Dialogue Speaker Profile")]
public class DialogueSpeakerProfile : ScriptableObject
{
    public string speakerId;
    public string displayName;
    public Sprite portrait;
}

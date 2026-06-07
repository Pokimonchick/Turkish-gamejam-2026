using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dialogue Data", fileName = "New Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string dialogueId;
    public List<DialogueLine> lines = new List<DialogueLine>();
}

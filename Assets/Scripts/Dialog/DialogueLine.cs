using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public DialogueSpeakerProfile speakerProfile;
    public string speakerName;
    [TextArea] public string text;
    public Sprite portrait;
}

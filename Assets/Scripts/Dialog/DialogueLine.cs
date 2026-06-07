using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea] public string text;
    public Sprite portrait;
}

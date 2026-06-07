using System.Collections;
using UnityEngine;

public class DialogueAutoStart : MonoBehaviour
{
    public DialogueData dialogueData;
    public float delaySeconds = 0.25f;
    public bool playOnStart = true;
    public bool onlyIfNoDialogueActive = true;

    private void Start()
    {
        if (playOnStart)
        {
            StartCoroutine(StartDialogueRoutine());
        }
    }

    private IEnumerator StartDialogueRoutine()
    {
        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        if (dialogueData == null)
        {
            yield break;
        }

        if (onlyIfNoDialogueActive && DialogueManager.IsDialogueActive)
        {
            yield break;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning($"DialogueAutoStart on {name} cannot find DialogueManager.Instance.", this);
            yield break;
        }

        DialogueManager.Instance.StartDialogue(dialogueData);
    }
}

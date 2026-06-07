using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PrologueCutsceneController : MonoBehaviour
{
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private string nextSceneName = "Village_Level_01_Greybox";
    [SerializeField, Min(0f)] private float startDelaySeconds = 0.25f;
    [SerializeField] private bool playOnStart = true;

    private bool hasStartedDialogue;
    private bool transitionRequested;
    private Coroutine startRoutine;

    private void Start()
    {
        if (playOnStart)
        {
            startRoutine = StartCoroutine(StartPrologueRoutine());
        }
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.DialogueEnded -= HandleDialogueEnded;
        }
    }

    public void StartPrologue()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
        }

        startRoutine = StartCoroutine(StartPrologueRoutine());
    }

    private IEnumerator StartPrologueRoutine()
    {
        if (startDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(startDelaySeconds);
        }

        while (DialogueManager.Instance == null)
        {
            yield return null;
        }

        if (dialogueData == null)
        {
            Debug.LogWarning("PrologueCutsceneController has no DialogueData assigned.", this);
            yield break;
        }

        DialogueManager.Instance.DialogueEnded -= HandleDialogueEnded;
        DialogueManager.Instance.DialogueEnded += HandleDialogueEnded;
        DialogueManager.Instance.StartDialogue(dialogueData);
        hasStartedDialogue = true;
        startRoutine = null;
    }

    private void HandleDialogueEnded()
    {
        if (!hasStartedDialogue || transitionRequested)
        {
            return;
        }

        transitionRequested = true;

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("PrologueCutsceneController nextSceneName is empty.", this);
            return;
        }

        SceneTransition.Load(nextSceneName);
    }
}

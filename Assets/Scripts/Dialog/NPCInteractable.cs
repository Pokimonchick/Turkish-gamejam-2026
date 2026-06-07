using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class NPCInteractable : MonoBehaviour
{
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private Transform player;
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactionHint;
    public bool useGlobalInteractionPrompt = true;
    public string interactionPromptText = "E \u2014 \u0432\u0437\u0430\u0438\u043c\u043e\u0434\u0435\u0439\u0441\u0442\u0432\u0438\u0435";

    private bool isPlayerInRange;

    private void Start()
    {
        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        SetHintVisible(false);
    }

    private void Update()
    {
        if (player == null)
        {
            SetHintVisible(false);
            return;
        }

        isPlayerInRange = Vector3.Distance(transform.position, player.position) <= interactionDistance;
        var dialogueActive = DialogueManager.IsDialogueActive;
        var dialogueEndedThisFrame = DialogueManager.LastEndedFrame == Time.frameCount;
        SetHintVisible(isPlayerInRange && !dialogueActive && !dialogueEndedThisFrame);

        if (!isPlayerInRange || dialogueActive || dialogueEndedThisFrame)
        {
            return;
        }

        if (!IsInteractPressed())
        {
            return;
        }

        if (dialogueData == null)
        {
            Debug.LogWarning($"NPCInteractable on {name} has no DialogueData assigned.", this);
            return;
        }

        if (dialogueData.lines == null || dialogueData.lines.Count == 0)
        {
            Debug.LogWarning($"NPCInteractable on {name} has empty DialogueData.", this);
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning($"NPCInteractable on {name} cannot find DialogueManager.Instance.", this);
            return;
        }

        DialogueManager.Instance.StartDialogue(dialogueData);
    }

    private void OnDisable()
    {
        if (InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.Hide(this);
        }

        SetHintVisible(false);
    }

    private void SetHintVisible(bool isVisible)
    {
        InteractionPromptUI prompt = InteractionPromptUI.Instance;

        if (useGlobalInteractionPrompt && prompt != null)
        {
            if (interactionHint != null)
            {
                interactionHint.SetActive(false);
            }

            if (isVisible)
            {
                prompt.Show(interactionPromptText, this);
            }
            else
            {
                prompt.Hide(this);
            }

            return;
        }

        if (interactionHint != null)
        {
            interactionHint.SetActive(isVisible);
        }
    }

    private bool IsInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return false;
        }

        switch (interactKey)
        {
            case KeyCode.E:
                return keyboard.eKey.wasPressedThisFrame;

            case KeyCode.Space:
                return keyboard.spaceKey.wasPressedThisFrame;

            case KeyCode.Return:
                return keyboard.enterKey.wasPressedThisFrame
                    || keyboard.numpadEnterKey.wasPressedThisFrame;

            default:
                Debug.LogWarning(
                    $"NPCInteractable on {name} uses unsupported Input System key {interactKey}.",
                    this);
                return false;
        }
#else
        return Input.GetKeyDown(interactKey);
#endif
    }
}

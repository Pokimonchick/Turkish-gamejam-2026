using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private Transform player;
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactionHint;

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

        if (!isPlayerInRange
            || dialogueActive
            || dialogueEndedThisFrame
            || dialogueData == null
            || DialogueManager.Instance == null)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            DialogueManager.Instance.StartDialogue(dialogueData);
        }
    }

    private void OnDisable()
    {
        SetHintVisible(false);
    }

    private void SetHintVisible(bool isVisible)
    {
        if (interactionHint != null)
        {
            interactionHint.SetActive(isVisible);
        }
    }
}

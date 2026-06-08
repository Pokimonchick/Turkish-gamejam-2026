using UnityEngine;

public class ProximityInteractionPrompt : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float activationDistance = 2f;
    [SerializeField] private string promptText = "Соедини два узла сгиба с помощью ЛКМ";
    [SerializeField] private bool hideWhileDialogueActive = true;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        HidePrompt();
    }

    private void Update()
    {
        if (player == null || InteractionPromptUI.Instance == null)
        {
            HidePrompt();
            return;
        }

        if (hideWhileDialogueActive && DialogueManager.IsDialogueActive)
        {
            HidePrompt();
            return;
        }

        bool isPlayerInRange = Vector3.Distance(transform.position, player.position) <= activationDistance;

        if (isPlayerInRange)
        {
            InteractionPromptUI.Instance.Show(promptText, this);
        }
        else
        {
            HidePrompt();
        }
    }

    private void OnDisable()
    {
        HidePrompt();
    }

    private void HidePrompt()
    {
        if (InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.Hide(this);
        }
    }
}

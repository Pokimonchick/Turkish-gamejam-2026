using System.Collections;
using UnityEngine;

[RequireComponent(typeof(NPCInteractable))]
public class DialogueFadeOutOnCompletion : MonoBehaviour
{
    public GameObject targetRoot;
    public float fadeDuration = 0.55f;
    public bool disableObjectAfterFade = true;
    public SpriteRenderer[] spriteRenderers;
    public Collider2D[] collidersToDisable;
    public Behaviour[] behavioursToDisable;
    public GameObject[] enableAfterFade;
    public GameObject[] disableAfterFade;

    private NPCInteractable interactable;
    private bool dialogueStartedHere;
    private bool hasCompleted;
    private bool isFading;

    private void Awake()
    {
        interactable = GetComponent<NPCInteractable>();

        if (targetRoot == null)
        {
            targetRoot = gameObject;
        }

        CacheMissingReferences();
    }

    private void OnEnable()
    {
        if (interactable == null)
        {
            interactable = GetComponent<NPCInteractable>();
        }

        if (interactable != null)
        {
            interactable.DialogueStartedByThisNpc += HandleDialogueStarted;
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.DialogueStartedByThisNpc -= HandleDialogueStarted;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.DialogueEnded -= HandleDialogueEnded;
        }
    }

    private void HandleDialogueStarted()
    {
        if (hasCompleted || isFading)
        {
            return;
        }

        dialogueStartedHere = true;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.DialogueEnded -= HandleDialogueEnded;
            DialogueManager.Instance.DialogueEnded += HandleDialogueEnded;
        }
    }

    private void HandleDialogueEnded()
    {
        if (!dialogueStartedHere || hasCompleted || isFading)
        {
            return;
        }

        dialogueStartedHere = false;
        hasCompleted = true;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.DialogueEnded -= HandleDialogueEnded;
        }

        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        isFading = true;
        CacheMissingReferences();
        SetInteractionEnabled(false);

        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;
        Color[] startColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            startColors[i] = spriteRenderers[i] == null ? Color.white : spriteRenderers[i].color;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / duration));
            ApplyAlpha(startColors, alpha);
            yield return null;
        }

        ApplyAlpha(startColors, 0f);
        SetObjectsActive(enableAfterFade, true);
        SetObjectsActive(disableAfterFade, false);

        if (disableObjectAfterFade && targetRoot != null)
        {
            targetRoot.SetActive(false);
        }

        isFading = false;
    }

    private void SetInteractionEnabled(bool isEnabled)
    {
        for (int i = 0; i < collidersToDisable.Length; i++)
        {
            if (collidersToDisable[i] != null)
            {
                collidersToDisable[i].enabled = isEnabled;
            }
        }

        for (int i = 0; i < behavioursToDisable.Length; i++)
        {
            if (behavioursToDisable[i] != null)
            {
                behavioursToDisable[i].enabled = isEnabled;
            }
        }
    }

    private void ApplyAlpha(Color[] startColors, float alpha)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = spriteRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            Color color = i < startColors.Length ? startColors[i] : renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool isActive)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(isActive);
            }
        }
    }

    private void CacheMissingReferences()
    {
        if (targetRoot == null)
        {
            targetRoot = gameObject;
        }

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = targetRoot.GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (collidersToDisable == null || collidersToDisable.Length == 0)
        {
            collidersToDisable = targetRoot.GetComponentsInChildren<Collider2D>(true);
        }

        if (behavioursToDisable == null || behavioursToDisable.Length == 0)
        {
            behavioursToDisable = new Behaviour[] { interactable };
        }
    }
}

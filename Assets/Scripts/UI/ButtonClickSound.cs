using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ButtonClickSound : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip hoverSound;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(PlayClick);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        PlaySound(clickSound);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
        {
            return;
        }

        PlaySound(hoverSound);
    }

    private static void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        GameAudioManager.Instance.PlaySfx(clip);
    }
}

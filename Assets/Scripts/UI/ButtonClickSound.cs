using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ButtonClickSound : MonoBehaviour
{
    [SerializeField] private AudioClip clickSound;

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
        if (clickSound == null)
        {
            return;
        }

        GameAudioManager.Instance.PlaySfx(clickSound);
    }
}

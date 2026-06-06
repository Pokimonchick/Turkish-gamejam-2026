using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Game Audio/Categorized Audio Source")]
public sealed class CategorizedAudioSource : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Music is controlled by the Music slider. SFX is controlled by the SFX slider.")]
    [SerializeField] private AudioCategory category = AudioCategory.SFX;
    [Tooltip("Per-source volume before category volume is applied.")]
    [SerializeField, Range(0f, 1f)] private float sourceVolume = 1f;

    public AudioCategory Category => category;

    private void Awake()
    {
        CacheAudioSource();
    }

    private void OnEnable()
    {
        CacheAudioSource();
        GameAudioManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (GameAudioManager.HasInstance)
        {
            GameAudioManager.Instance.Unregister(this);
        }
    }

    public void ApplyCategoryVolume(float categoryVolume)
    {
        CacheAudioSource();

        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(sourceVolume) * Mathf.Clamp01(categoryVolume);
        }
    }

    private void CacheAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnValidate()
    {
        CacheAudioSource();
        sourceVolume = Mathf.Clamp01(sourceVolume);
    }
}

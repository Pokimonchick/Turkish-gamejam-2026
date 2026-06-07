using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Game Audio/Scene Music Player")]
public sealed class SceneMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    private void Awake()
    {
        CacheAudioSource();
        ApplySettings();
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        CacheAudioSource();
        ApplySettings();

        if (audioSource == null || audioSource.clip == null)
        {
            return;
        }

        _ = GameAudioManager.Instance;

        if (audioSource.clip.loadState == AudioDataLoadState.Unloaded)
        {
            audioSource.clip.LoadAudioData();
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void ApplySettings()
    {
        if (audioSource == null)
        {
            return;
        }

        if (musicClip != null)
        {
            audioSource.clip = musicClip;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = loop;
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
        ApplySettings();
    }
}

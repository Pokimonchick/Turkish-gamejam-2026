using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Game Audio/Persistent Music Stopper")]
public sealed class PersistentMusicStopper : MonoBehaviour
{
    private void Start()
    {
        if (GameAudioManager.HasInstance)
        {
            GameAudioManager.Instance.StopMusic();
        }
    }
}

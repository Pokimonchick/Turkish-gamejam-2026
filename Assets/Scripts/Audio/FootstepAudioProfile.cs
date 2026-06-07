using UnityEngine;

[CreateAssetMenu(menuName = "Game Audio/Footstep Audio Profile", fileName = "New Footstep Audio Profile")]
public sealed class FootstepAudioProfile : ScriptableObject
{
    [SerializeField] private AudioClip[] clips;

    public AudioClip[] Clips => clips;
}

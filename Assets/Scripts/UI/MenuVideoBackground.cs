using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
public sealed class MenuVideoBackground : MonoBehaviour
{
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool muteVideoAudio = true;

    private VideoPlayer videoPlayer;

    private void Awake()
    {
        ConfigureVideoPlayer();
    }

    private void OnEnable()
    {
        if (videoPlayer == null)
        {
            ConfigureVideoPlayer();
        }

        if (videoPlayer != null && videoClip != null)
        {
            PlayWhenPrepared();
        }
    }

    private void OnDisable()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -= HandleVideoPrepared;
        videoPlayer.Stop();
    }

    private void ConfigureVideoPlayer()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.isLooping = loop;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
        videoPlayer.targetCamera = targetCamera;
        videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
        videoPlayer.audioOutputMode = muteVideoAudio ? VideoAudioOutputMode.None : VideoAudioOutputMode.Direct;
    }

    private void PlayWhenPrepared()
    {
        videoPlayer.prepareCompleted -= HandleVideoPrepared;

        if (videoPlayer.isPrepared)
        {
            videoPlayer.Play();
            return;
        }

        videoPlayer.prepareCompleted += HandleVideoPrepared;
        videoPlayer.Prepare();
    }

    private void HandleVideoPrepared(VideoPlayer preparedPlayer)
    {
        preparedPlayer.prepareCompleted -= HandleVideoPrepared;
        preparedPlayer.Play();
    }
}

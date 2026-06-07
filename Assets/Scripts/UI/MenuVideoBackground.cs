using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public sealed class MenuVideoBackground : MonoBehaviour
{
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RawImage targetImage;
    [SerializeField] private RenderTexture targetTexture;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool muteVideoAudio = true;
    [SerializeField] private float stallRestartDelay = 0.8f;

    private VideoPlayer videoPlayer;
    private RenderTexture runtimeTexture;
    private AspectRatioFitter aspectRatioFitter;
    private double lastVideoTime = -1d;
    private float stalledTimer;

    private void Awake()
    {
        ConfigureVideoPlayer();
    }

    private void OnEnable()
    {
        ConfigureVideoPlayer();

        if (videoPlayer != null && videoClip != null)
        {
            PlayWhenPrepared();
        }
    }

    private void Update()
    {
        WatchForStalledPlayback();
    }

    private void OnDisable()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -= HandleVideoPrepared;
        videoPlayer.loopPointReached -= HandleLoopPointReached;
        videoPlayer.errorReceived -= HandleVideoError;
        videoPlayer.Stop();
    }

    private void OnDestroy()
    {
        if (runtimeTexture == null)
        {
            return;
        }

        runtimeTexture.Release();
        Destroy(runtimeTexture);
        runtimeTexture = null;
    }

    private void ConfigureVideoPlayer()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        EnsureTargetImage();
        EnsureTargetTexture();

        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = targetTexture != null ? targetTexture : runtimeTexture;
        videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
        videoPlayer.audioOutputMode = muteVideoAudio ? VideoAudioOutputMode.None : VideoAudioOutputMode.Direct;
        videoPlayer.timeReference = VideoTimeReference.Freerun;
        videoPlayer.playbackSpeed = 1f;

        videoPlayer.loopPointReached -= HandleLoopPointReached;
        videoPlayer.loopPointReached += HandleLoopPointReached;
        videoPlayer.errorReceived -= HandleVideoError;
        videoPlayer.errorReceived += HandleVideoError;

        if (targetImage != null)
        {
            targetImage.texture = videoPlayer.targetTexture;
            targetImage.raycastTarget = false;
        }

        UpdateAspectRatio();
    }

    private void PlayWhenPrepared()
    {
        videoPlayer.prepareCompleted -= HandleVideoPrepared;
        ResetStallState();

        if (videoPlayer.isPrepared)
        {
            RestartFromBeginning();
            return;
        }

        videoPlayer.prepareCompleted += HandleVideoPrepared;
        videoPlayer.Prepare();
    }

    private void HandleVideoPrepared(VideoPlayer preparedPlayer)
    {
        preparedPlayer.prepareCompleted -= HandleVideoPrepared;
        UpdateAspectRatio();
        RestartFromBeginning();
    }

    private void HandleLoopPointReached(VideoPlayer finishedPlayer)
    {
        if (!loop || !isActiveAndEnabled)
        {
            return;
        }

        RestartFromBeginning();
    }

    private void HandleVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning($"Menu video playback error: {message}", this);
    }

    private void RestartFromBeginning()
    {
        ResetStallState();
        videoPlayer.time = 0d;
        videoPlayer.frame = 0;
        videoPlayer.Play();
    }

    private void WatchForStalledPlayback()
    {
        if (videoPlayer == null || videoClip == null || !videoPlayer.isPrepared || !videoPlayer.isPlaying)
        {
            ResetStallState();
            return;
        }

        double currentTime = videoPlayer.time;
        bool reachedEnd = videoClip.length > 0d && currentTime >= videoClip.length - 0.1d;

        if (reachedEnd)
        {
            ResetStallState();
            return;
        }

        if (Mathf.Abs((float)(currentTime - lastVideoTime)) > 0.001f)
        {
            lastVideoTime = currentTime;
            stalledTimer = 0f;
            return;
        }

        stalledTimer += Time.unscaledDeltaTime;

        if (stalledTimer < stallRestartDelay)
        {
            return;
        }

        Debug.LogWarning("Menu video looked stalled, restarting playback.", this);
        RestartFromBeginning();
    }

    private void ResetStallState()
    {
        lastVideoTime = -1d;
        stalledTimer = 0f;
    }

    private void EnsureTargetImage()
    {
        if (targetImage != null)
        {
            aspectRatioFitter = targetImage.GetComponent<AspectRatioFitter>();
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject imageObject = new GameObject("Video Background");
        imageObject.transform.SetParent(canvas.transform, false);
        imageObject.transform.SetAsFirstSibling();

        RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        targetImage = imageObject.AddComponent<RawImage>();
        targetImage.color = Color.white;
        targetImage.raycastTarget = false;

        aspectRatioFitter = imageObject.AddComponent<AspectRatioFitter>();
        aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
    }

    private void EnsureTargetTexture()
    {
        if (targetTexture != null || runtimeTexture != null)
        {
            return;
        }

        int width = Mathf.Max(Screen.width, 1920);
        int height = Mathf.Max(Screen.height, 1080);
        runtimeTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "Runtime Menu Video RenderTexture",
            useMipMap = false,
            autoGenerateMips = false
        };
        runtimeTexture.Create();
    }

    private void UpdateAspectRatio()
    {
        if (aspectRatioFitter == null || videoClip == null || videoClip.height == 0)
        {
            return;
        }

        aspectRatioFitter.aspectRatio = (float)videoClip.width / videoClip.height;
    }
}

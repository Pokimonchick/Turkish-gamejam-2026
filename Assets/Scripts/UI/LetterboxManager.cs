using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class LetterboxManager : MonoBehaviour
{
    public static LetterboxManager Instance { get; private set; }

    [Header("Target Aspect Ratio")]
    [SerializeField] private float targetWidth = 16f;
    [SerializeField] private float targetHeight = 9f;

    [Header("Camera")]
    [SerializeField] private bool applyToAllCameras = true;
    [SerializeField] private Camera targetCamera;

    private Camera blackBarsCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstanceExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("AspectRatioManager");
        managerObject.AddComponent<LetterboxManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateBlackBarsCamera();
        SceneManager.sceneLoaded += OnSceneLoaded;

        ApplyLetterbox();
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (blackBarsCamera != null)
        {
            Destroy(blackBarsCamera.gameObject);
        }

        Instance = null;
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyLetterbox();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyLetterbox();
    }

    public void ApplyLetterbox()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        Rect cameraRect = CalculateCameraRect();

        if (applyToAllCameras)
        {
            foreach (Camera cam in Camera.allCameras)
            {
                if (cam == null || cam == blackBarsCamera)
                {
                    continue;
                }

                cam.rect = cameraRect;
            }
        }
        else
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;

            if (cam != null && cam != blackBarsCamera)
            {
                cam.rect = cameraRect;
            }
        }

        if (blackBarsCamera != null)
        {
            blackBarsCamera.rect = new Rect(0f, 0f, 1f, 1f);
            blackBarsCamera.depth = -1000f;
        }
    }

    private Rect CalculateCameraRect()
    {
        float safeTargetWidth = Mathf.Max(targetWidth, 0.01f);
        float safeTargetHeight = Mathf.Max(targetHeight, 0.01f);
        float safeScreenHeight = Mathf.Max(Screen.height, 1);

        float targetAspect = safeTargetWidth / safeTargetHeight;
        float screenAspect = Screen.width / safeScreenHeight;

        if (screenAspect > targetAspect)
        {
            float width = targetAspect / screenAspect;
            float x = (1f - width) / 2f;

            return new Rect(x, 0f, width, 1f);
        }

        float height = screenAspect / targetAspect;
        float y = (1f - height) / 2f;

        return new Rect(0f, y, 1f, height);
    }

    private void CreateBlackBarsCamera()
    {
        if (blackBarsCamera != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("Black Bars Camera");
        DontDestroyOnLoad(cameraObject);

        blackBarsCamera = cameraObject.AddComponent<Camera>();
        blackBarsCamera.clearFlags = CameraClearFlags.SolidColor;
        blackBarsCamera.backgroundColor = Color.black;
        blackBarsCamera.cullingMask = 0;
        blackBarsCamera.depth = -1000f;
        blackBarsCamera.rect = new Rect(0f, 0f, 1f, 1f);
        blackBarsCamera.useOcclusionCulling = false;
        blackBarsCamera.allowHDR = false;
        blackBarsCamera.allowMSAA = false;
    }
}

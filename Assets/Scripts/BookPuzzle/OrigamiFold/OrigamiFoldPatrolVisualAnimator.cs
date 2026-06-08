using UnityEngine;

[DisallowMultipleComponent]
public class OrigamiFoldPatrolVisualAnimator : MonoBehaviour
{
    public Transform visualRoot;
    public SpriteRenderer spriteRenderer;
    public bool spriteFacesRight = true;
    public bool flipByHorizontalMovement = true;
    public float directionThreshold = 0.0005f;
    public float rockTiltAmplitude = 3f;
    public float rockSpeed = 3.5f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 previousLocalPosition;
    private float facingDirection = 1f;
    private float time;
    private bool hasBasePose;

    private void Awake()
    {
        CacheReferences();
        CaptureBasePose();
        previousLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        CacheReferences();
        CaptureBasePose();
        previousLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        if (visualRoot == null)
        {
            return;
        }

        UpdateFacing();
        ApplyRocking();
        previousLocalPosition = transform.localPosition;
    }

    private void OnDisable()
    {
        if (visualRoot != null && hasBasePose)
        {
            visualRoot.localPosition = baseLocalPosition;
            visualRoot.localRotation = baseLocalRotation;
        }
    }

    public void CaptureBasePose()
    {
        CacheReferences();

        if (visualRoot == null)
        {
            return;
        }

        baseLocalPosition = visualRoot.localPosition;
        baseLocalRotation = visualRoot.localRotation;
        hasBasePose = true;
    }

    private void UpdateFacing()
    {
        if (!flipByHorizontalMovement || spriteRenderer == null)
        {
            return;
        }

        float deltaX = transform.localPosition.x - previousLocalPosition.x;

        if (Mathf.Abs(deltaX) > directionThreshold)
        {
            facingDirection = Mathf.Sign(deltaX);
        }

        spriteRenderer.flipX = spriteFacesRight
            ? facingDirection < 0f
            : facingDirection > 0f;
    }

    private void ApplyRocking()
    {
        if (!hasBasePose)
        {
            CaptureBasePose();
        }

        time += Time.deltaTime;
        float tilt = Mathf.Sin(time * rockSpeed) * rockTiltAmplitude;
        visualRoot.localPosition = baseLocalPosition;
        visualRoot.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, tilt);
    }

    private void CacheReferences()
    {
        if (visualRoot == null)
        {
            Transform visual = transform.Find("Visual");
            visualRoot = visual != null ? visual : transform;
        }

        if (spriteRenderer == null && visualRoot != null)
        {
            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void OnValidate()
    {
        directionThreshold = Mathf.Max(0f, directionThreshold);
        rockTiltAmplitude = Mathf.Max(0f, rockTiltAmplitude);
        rockSpeed = Mathf.Max(0f, rockSpeed);
        CacheReferences();
    }
}

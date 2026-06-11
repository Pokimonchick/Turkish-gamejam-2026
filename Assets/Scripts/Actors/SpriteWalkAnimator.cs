using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpriteWalkAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprite Frames")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkFrames = System.Array.Empty<Sprite>();
    [SerializeField, Min(0.01f)] private float walkFramesPerSecond = 8f;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Sizing")]
    [SerializeField] private bool matchFramesToIdleHeight = true;
    [SerializeField] private bool anchorFramesToIdleBounds = true;

    [Header("Facing")]
    [SerializeField] private bool flipByDirection = true;

    private bool isWalking;
    private float time;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;
    private bool hasInitialTransform;

    private void Awake()
    {
        CacheReferences();
        CaptureIdleSprite();
        CaptureInitialTransform();
        ApplyCurrentSprite();
    }

    private void OnEnable()
    {
        CacheReferences();
        CaptureIdleSprite();
        CaptureInitialTransform();
        ResetVisualTransform();
        ApplyCurrentSprite();
    }

    private void Update()
    {
        if (!isWalking)
        {
            return;
        }

        time += Time.deltaTime;
        ApplyCurrentSprite();
    }

    public void SetWalking(bool walking)
    {
        if (isWalking == walking)
        {
            return;
        }

        isWalking = walking;
        time = 0f;
        ResetVisualTransform();
        ApplyCurrentSprite();
    }

    public void SetFacing(float directionX)
    {
        if (!flipByDirection || spriteRenderer == null || Mathf.Abs(directionX) < 0.01f)
        {
            return;
        }

        spriteRenderer.flipX = spriteFacesRight ? directionX < 0f : directionX > 0f;
    }

    public void ResetVisual()
    {
        ResetVisualTransform();
        ApplyCurrentSprite();
    }

    private void CacheReferences()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void CaptureIdleSprite()
    {
        if (idleSprite == null && spriteRenderer != null)
        {
            idleSprite = spriteRenderer.sprite;
        }
    }

    private void CaptureInitialTransform()
    {
        if (hasInitialTransform || visualRoot == null)
        {
            return;
        }

        initialLocalPosition = visualRoot.localPosition;
        initialLocalRotation = visualRoot.localRotation;
        initialLocalScale = visualRoot.localScale;
        hasInitialTransform = true;
    }

    private void ResetVisualTransform()
    {
        if (visualRoot == null || !hasInitialTransform)
        {
            return;
        }

        visualRoot.localPosition = initialLocalPosition;
        visualRoot.localRotation = initialLocalRotation;
        visualRoot.localScale = initialLocalScale;
    }

    private void ApplyCurrentSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite targetSprite = GetCurrentSprite();

        if (targetSprite != null && spriteRenderer.sprite != targetSprite)
        {
            spriteRenderer.sprite = targetSprite;
        }

        ApplyCurrentSpriteTransform();
    }

    private Sprite GetCurrentSprite()
    {
        if (!isWalking || walkFrames == null || walkFrames.Length == 0)
        {
            return idleSprite;
        }

        int frameCount = GetUsableWalkFrameCount();

        if (frameCount == 0)
        {
            return idleSprite;
        }

        int frameIndex = Mathf.FloorToInt(time * walkFramesPerSecond) % frameCount;
        return GetUsableWalkFrame(frameIndex);
    }

    private int GetUsableWalkFrameCount()
    {
        int count = 0;

        for (int i = 0; i < walkFrames.Length; i++)
        {
            if (walkFrames[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private Sprite GetUsableWalkFrame(int usableIndex)
    {
        int currentUsableIndex = 0;

        for (int i = 0; i < walkFrames.Length; i++)
        {
            Sprite frame = walkFrames[i];

            if (frame == null)
            {
                continue;
            }

            if (currentUsableIndex == usableIndex)
            {
                return frame;
            }

            currentUsableIndex++;
        }

        return null;
    }

    private void ApplyCurrentSpriteTransform()
    {
        if (visualRoot == null || !hasInitialTransform)
        {
            return;
        }

        visualRoot.localRotation = initialLocalRotation;

        Sprite currentSprite = spriteRenderer == null ? null : spriteRenderer.sprite;

        if (idleSprite == null || currentSprite == null)
        {
            visualRoot.localPosition = initialLocalPosition;
            visualRoot.localScale = initialLocalScale;
            return;
        }

        Bounds idleBounds = idleSprite.bounds;
        Bounds currentBounds = currentSprite.bounds;
        Vector3 targetScale = initialLocalScale;

        if (matchFramesToIdleHeight)
        {
            float idleHeight = idleBounds.size.y;
            float currentHeight = currentBounds.size.y;

            if (idleHeight > 0f && currentHeight > 0f)
            {
                float scaleMultiplier = idleHeight / currentHeight;
                targetScale = new Vector3(
                    initialLocalScale.x * scaleMultiplier,
                    initialLocalScale.y * scaleMultiplier,
                    initialLocalScale.z);
            }
        }

        visualRoot.localScale = targetScale;

        if (!anchorFramesToIdleBounds)
        {
            visualRoot.localPosition = initialLocalPosition;
            return;
        }

        Vector3 targetPosition = initialLocalPosition;
        targetPosition.x += idleBounds.center.x * initialLocalScale.x - currentBounds.center.x * targetScale.x;
        targetPosition.y += idleBounds.min.y * initialLocalScale.y - currentBounds.min.y * targetScale.y;
        visualRoot.localPosition = targetPosition;
    }

    private void OnValidate()
    {
        walkFramesPerSecond = Mathf.Max(0.01f, walkFramesPerSecond);
        CacheReferences();
    }
}

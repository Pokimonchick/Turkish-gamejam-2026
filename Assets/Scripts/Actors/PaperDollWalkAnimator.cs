using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class PaperDollWalkAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprite Frames")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField, Min(0.01f)] private float walkFramesPerSecond = 8f;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Idle Rocking")]
    [FormerlySerializedAs("idleTiltAngle")]
    [SerializeField, Min(0f)] private float idleRockTiltAmplitude = 1.2f;
    [FormerlySerializedAs("idleSpeed")]
    [SerializeField, Min(0f)] private float idleRockSpeed = 1.2f;

    [Header("Walk Rocking")]
    [FormerlySerializedAs("walkTiltAngle")]
    [SerializeField, Min(0f)] private float walkRockTiltAmplitude = 5f;
    [FormerlySerializedAs("walkSpeed")]
    [SerializeField, Min(0f)] private float walkRockSpeed = 6f;
    [SerializeField] private float walkBobHeight = 0.04f;
    [SerializeField] private float walkSideOffset = 0.025f;

    [Header("Paper Doll Feel")]
    [SerializeField] private bool steppedMotion = true;
    [SerializeField, Range(0f, 1f)] private float stepiness = 1f;
    [SerializeField, Min(2)] private int stepsPerCycle = 6;
    [SerializeField] private bool snapSteppedPoses = true;
    [SerializeField, Min(0f)] private float returnSmoothness = 12f;

    [Header("Facing")]
    [SerializeField] private bool flipByDirection = true;

    private bool isWalking;
    private float time;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;
    private bool hasInitialTransform;
    private int currentFrameIndex = -1;

    private void Awake()
    {
        CacheReferences();
        CaptureIdleSprite();
        CaptureInitialTransform();
    }

    private void OnEnable()
    {
        CacheReferences();
        CaptureIdleSprite();
        CaptureInitialTransform();
        ApplyCurrentSprite();
    }

    private void Update()
    {
        if (visualRoot == null)
        {
            return;
        }

        time += Time.deltaTime;
        ApplyCurrentSprite();

        if (isWalking)
        {
            ApplyWalkAnimation();
        }
        else
        {
            ApplyIdleAnimation();
        }
    }

    public void SetWalking(bool walking)
    {
        if (isWalking == walking)
        {
            return;
        }

        isWalking = walking;
        time = 0f;
        currentFrameIndex = -1;
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
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localPosition = initialLocalPosition;
        visualRoot.localRotation = initialLocalRotation;
        visualRoot.localScale = initialLocalScale;
    }

    private void ApplyIdleAnimation()
    {
        float phase = time * idleRockSpeed;
        float wave = ShapeWave(Mathf.Sin(phase));
        float tilt = wave * idleRockTiltAmplitude;
        Quaternion targetRotation = initialLocalRotation * Quaternion.Euler(0f, 0f, tilt);

        ApplyVisualState(initialLocalPosition, targetRotation);
    }

    private void ApplyWalkAnimation()
    {
        float phase = time * walkRockSpeed;
        float wave = ShapeWave(Mathf.Sin(phase));
        float bob = ShapeWave(Mathf.Abs(Mathf.Sin(phase)));
        float side = ShapeWave(Mathf.Sin(phase + Mathf.PI * 0.25f));

        Vector3 targetPosition = initialLocalPosition;
        targetPosition.y += bob * walkBobHeight;
        targetPosition.x += side * walkSideOffset;

        float tilt = wave * walkRockTiltAmplitude;
        Quaternion targetRotation = initialLocalRotation * Quaternion.Euler(0f, 0f, tilt);

        ApplyVisualState(targetPosition, targetRotation);
    }

    private void ApplyVisualState(Vector3 targetPosition, Quaternion targetRotation)
    {
        if ((steppedMotion && snapSteppedPoses) || returnSmoothness <= 0f)
        {
            visualRoot.localPosition = targetPosition;
            visualRoot.localRotation = targetRotation;
            visualRoot.localScale = initialLocalScale;
            return;
        }

        float smooth = Mathf.Clamp01(Time.deltaTime * returnSmoothness);

        visualRoot.localPosition = Vector3.Lerp(
            visualRoot.localPosition,
            targetPosition,
            smooth);

        visualRoot.localRotation = Quaternion.Lerp(
            visualRoot.localRotation,
            targetRotation,
            smooth);

        visualRoot.localScale = initialLocalScale;
    }

    private float ShapeWave(float value)
    {
        if (!steppedMotion || stepiness <= 0f)
        {
            return value;
        }

        float stepped = Quantize(value, stepsPerCycle);
        return Mathf.Lerp(value, stepped, stepiness);
    }

    private static float Quantize(float value, int steps)
    {
        if (steps <= 1)
        {
            return value;
        }

        return Mathf.Round(value * steps) / steps;
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

    private void CaptureIdleSprite()
    {
        if (idleSprite == null && spriteRenderer != null)
        {
            idleSprite = spriteRenderer.sprite;
        }
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
    }

    private Sprite GetCurrentSprite()
    {
        if (!isWalking || walkFrames == null || walkFrames.Length == 0)
        {
            currentFrameIndex = -1;
            return idleSprite;
        }

        int frameCount = GetUsableWalkFrameCount();

        if (frameCount == 0)
        {
            currentFrameIndex = -1;
            return idleSprite;
        }

        int frameIndex = Mathf.FloorToInt(time * walkFramesPerSecond) % frameCount;

        if (frameIndex == currentFrameIndex)
        {
            return null;
        }

        currentFrameIndex = frameIndex;
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

    private void OnValidate()
    {
        walkFramesPerSecond = Mathf.Max(0.01f, walkFramesPerSecond);
        idleRockTiltAmplitude = Mathf.Max(0f, idleRockTiltAmplitude);
        idleRockSpeed = Mathf.Max(0f, idleRockSpeed);
        walkRockTiltAmplitude = Mathf.Max(0f, walkRockTiltAmplitude);
        walkRockSpeed = Mathf.Max(0f, walkRockSpeed);
        stepsPerCycle = Mathf.Max(2, stepsPerCycle);
        returnSmoothness = Mathf.Max(0f, returnSmoothness);
        CacheReferences();
    }
}

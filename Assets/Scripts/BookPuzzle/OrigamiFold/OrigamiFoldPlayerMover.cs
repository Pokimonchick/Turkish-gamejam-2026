using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class OrigamiFoldPlayerMover : MonoBehaviour
{
    private const string DefaultFootstepFolder = "Assets/Audio/Steps";
    private const string DefaultFootstepProfileResourcePath = "Audio/DefaultFootstepAudioProfile";

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float bodyRadius = 0.18f;
    public float sampleProbeRadius = 0.025f;
    public LayerMask walkableMask;
    public bool requireAllSamplesInsideWalkable = true;
    public bool debugDrawSamples = true;

    [Header("Footsteps")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private FootstepAudioProfile defaultFootstepProfile;
    [SerializeField, Min(0.02f)] private float minFootstepInterval = 0.28f;
    [SerializeField, Min(0.02f)] private float maxFootstepInterval = 0.42f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.8f;
    [SerializeField] private bool avoidRepeatingFootstepSound = true;

    private Rigidbody2D body;
    private Vector2 moveInput;
    private float footstepTimer;
    private int lastFootstepIndex = -1;
    private bool wasWalking;
    private PaperDollWalkAnimator paperAnimator;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        paperAnimator = GetComponentInChildren<PaperDollWalkAnimator>();
        ResetFootstepTimer();
    }

    private void Update()
    {
        if (OrigamiFoldDialogueGuard.IsDialogueActive())
        {
            moveInput = Vector2.zero;
            SetPaperAnimatorWalking(false);
            return;
        }

        moveInput = ReadMoveInput();

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        if (paperAnimator != null)
        {
            paperAnimator.SetFacing(moveInput.x);
        }
    }

    private void FixedUpdate()
    {
        if (OrigamiFoldDialogueGuard.IsDialogueActive())
        {
            moveInput = Vector2.zero;
            HandleFootsteps(false);
            SetPaperAnimatorWalking(false);
            return;
        }

        if (moveInput == Vector2.zero)
        {
            HandleFootsteps(false);
            SetPaperAnimatorWalking(false);
            return;
        }

        Vector2 currentPosition = body.position;
        Vector2 moveDelta = moveInput * moveSpeed * Time.fixedDeltaTime;
        Vector2 targetPosition = currentPosition + moveDelta;
        bool moved = false;

        if (CanOccupy(targetPosition))
        {
            body.MovePosition(targetPosition);
            moved = true;
            HandleFootsteps(moved);
            SetPaperAnimatorWalking(moved);
            return;
        }

        Vector2 slidePosition = currentPosition;
        bool canSlide = false;

        Vector2 xTarget = currentPosition + new Vector2(moveDelta.x, 0f);

        if (!Mathf.Approximately(moveDelta.x, 0f) && CanOccupy(xTarget))
        {
            slidePosition = xTarget;
            canSlide = true;
        }

        Vector2 yTarget = slidePosition + new Vector2(0f, moveDelta.y);

        if (!Mathf.Approximately(moveDelta.y, 0f) && CanOccupy(yTarget))
        {
            slidePosition = yTarget;
            canSlide = true;
        }

        if (canSlide)
        {
            body.MovePosition(slidePosition);
            moved = true;
        }

        HandleFootsteps(moved);
        SetPaperAnimatorWalking(moved);
    }

    public bool CanOccupy(Vector2 targetPosition)
    {
        Vector2[] samples = GetSamplePositions(targetPosition);
        bool hasValidSample = false;

        for (int i = 0; i < samples.Length; i++)
        {
            bool isValid = IsWalkableSample(samples[i]);

            if (isValid)
            {
                hasValidSample = true;
            }
            else if (requireAllSamplesInsideWalkable)
            {
                return false;
            }
        }

        return requireAllSamplesInsideWalkable || hasValidSample;
    }

    private bool IsWalkableSample(Vector2 sample)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            sample,
            sampleProbeRadius,
            walkableMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            OrigamiFoldWalkableArea area = hit.GetComponent<OrigamiFoldWalkableArea>();

            if (area == null)
            {
                area = hit.GetComponentInParent<OrigamiFoldWalkableArea>();
            }

            if (area != null && area.isWalkable && hit.OverlapPoint(sample))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2[] GetSamplePositions(Vector2 center)
    {
        float diagonalOffset = bodyRadius * 0.70710678f;

        return new[]
        {
            center,
            center + Vector2.right * bodyRadius,
            center + Vector2.left * bodyRadius,
            center + Vector2.up * bodyRadius,
            center + Vector2.down * bodyRadius,
            center + new Vector2(diagonalOffset, diagonalOffset),
            center + new Vector2(-diagonalOffset, diagonalOffset),
            center + new Vector2(diagonalOffset, -diagonalOffset),
            center + new Vector2(-diagonalOffset, -diagonalOffset)
        };
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return input;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            input.y += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            input.y -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            input.x += 1f;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            input.x -= 1f;
        }
#else
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            input.y += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            input.y -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            input.x += 1f;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            input.x -= 1f;
        }
#endif

        return input;
    }

    private void HandleFootsteps(bool isWalking)
    {
        if (!isWalking)
        {
            wasWalking = false;
            ResetFootstepTimer();
            return;
        }

        if (!wasWalking)
        {
            PlayFootstep();
            ResetFootstepTimer();
            wasWalking = true;
            return;
        }

        footstepTimer -= Time.fixedDeltaTime;

        if (footstepTimer > 0f)
        {
            return;
        }

        PlayFootstep();
        ResetFootstepTimer();
    }

    private void PlayFootstep()
    {
        AudioClip[] clips = GetFootstepSounds();

        if (clips == null || clips.Length == 0)
        {
            return;
        }

        int clipIndex = GetRandomFootstepIndex(clips);

        if (clipIndex < 0)
        {
            return;
        }

        AudioClip clip = clips[clipIndex];

        if (clip == null)
        {
            return;
        }

        lastFootstepIndex = clipIndex;
        GameAudioManager.Instance.PlaySfx(clip, footstepVolume);
    }

    private int GetRandomFootstepIndex(AudioClip[] clips)
    {
        if (clips.Length <= 1)
        {
            return clips[0] == null ? -1 : 0;
        }

        int clipIndex = Random.Range(0, clips.Length);

        if (!avoidRepeatingFootstepSound || clipIndex != lastFootstepIndex)
        {
            return clips[clipIndex] == null ? FindFirstUsableClip(clips) : clipIndex;
        }

        for (int i = 1; i < clips.Length; i++)
        {
            int nextIndex = (clipIndex + i) % clips.Length;

            if (nextIndex != lastFootstepIndex && clips[nextIndex] != null)
            {
                return nextIndex;
            }
        }

        return FindFirstUsableClip(clips);
    }

    private AudioClip[] GetFootstepSounds()
    {
        if (HasUsableFootstepClips(footstepSounds))
        {
            return footstepSounds;
        }

        if (defaultFootstepProfile == null)
        {
            defaultFootstepProfile = Resources.Load<FootstepAudioProfile>(DefaultFootstepProfileResourcePath);
        }

        return defaultFootstepProfile == null ? null : defaultFootstepProfile.Clips;
    }

    private static bool HasUsableFootstepClips(AudioClip[] clips)
    {
        if (clips == null)
        {
            return false;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private static int FindFirstUsableClip(AudioClip[] clips)
    {
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                return i;
            }
        }

        return -1;
    }

    private void ResetFootstepTimer()
    {
        float minInterval = Mathf.Max(0.02f, minFootstepInterval);
        float maxInterval = Mathf.Max(minInterval, maxFootstepInterval);
        footstepTimer = Random.Range(minInterval, maxInterval);
    }

    private void SetPaperAnimatorWalking(bool walking)
    {
        if (paperAnimator == null)
        {
            return;
        }

        paperAnimator.SetWalking(walking);
    }

    private void OnValidate()
    {
        minFootstepInterval = Mathf.Max(0.02f, minFootstepInterval);
        maxFootstepInterval = Mathf.Max(minFootstepInterval, maxFootstepInterval);

#if UNITY_EDITOR
        AutoFillDefaultFootsteps();
        AutoFillDefaultFootstepProfile();
#endif
    }

#if UNITY_EDITOR
    private void AutoFillDefaultFootstepProfile()
    {
        if (defaultFootstepProfile != null)
        {
            return;
        }

        defaultFootstepProfile = AssetDatabase.LoadAssetAtPath<FootstepAudioProfile>(
            "Assets/Resources/Audio/DefaultFootstepAudioProfile.asset");
    }

    private void AutoFillDefaultFootsteps()
    {
        if (footstepSounds != null && footstepSounds.Length > 0)
        {
            return;
        }

        string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { DefaultFootstepFolder });
        if (clipGuids == null || clipGuids.Length == 0)
        {
            return;
        }

        System.Array.Sort(clipGuids, CompareAssetGuidsByPath);
        footstepSounds = new AudioClip[clipGuids.Length];

        for (int i = 0; i < clipGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(clipGuids[i]);
            footstepSounds[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        }
    }

    private static int CompareAssetGuidsByPath(string leftGuid, string rightGuid)
    {
        return string.CompareOrdinal(
            AssetDatabase.GUIDToAssetPath(leftGuid),
            AssetDatabase.GUIDToAssetPath(rightGuid));
    }
#endif

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bodyRadius);

        if (!debugDrawSamples)
        {
            return;
        }

        Vector2[] samples = GetSamplePositions(transform.position);
        Gizmos.color = Color.cyan;

        for (int i = 0; i < samples.Length; i++)
        {
            Gizmos.DrawWireSphere(samples[i], sampleProbeRadius);
        }
    }
}

using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles golf cup detection, fast-shot rim rebounds,
/// ball sinking animation and course completion.
/// Attach to the HoleTrigger GameObject.
/// </summary>
public class HoleCompletionController : MonoBehaviour
{
    [Header("Cup Capture Parameters")]
    [SerializeField] private float maxEntrySpeed = 3.2f;
    [SerializeField] private float captureRadius = 0.24f;
    [SerializeField] private float rimSuctionForce = 0.12f;

    [Header("Fast Shot Rebound")]
    [SerializeField] private float reboundSpeedMultiplier = 0.65f;
    [SerializeField] private float reboundUpwardSpeed = 0.85f;

    [Header("Sinking Animation")]
    [SerializeField] private float sinkDuration = 0.65f;
    [SerializeField] private float cupFloorY = -0.30f;

    [Header("Starting Tee Position")]
    [SerializeField]
    private Vector3 startingTeePosition =
        new Vector3(0f, 0.1605f, -8f);

    [Header("Legacy UI")]
    [SerializeField] private bool showLegacyOnGUI = false;

    // Events
    public event Action<int> OnLevelCompleted;
    public event Action OnHoleReplayed;

    // Public state
    public bool IsCompleted => isCompleted;

    private bool isCompleted;
    private bool isSinking;
    private bool hasRimBouncedThisApproach;

    private int finalScore;

    private float lastResetTime = -10f;

    private const float INITIALIZATION_GRACE_PERIOD = 0.5f;

    private GolfBallController activeBallController;

    private void Awake()
    {
        UpdateCupCenter();
    }

    private void Start()
    {
        activeBallController =
            FindFirstObjectByType<GolfBallController>();
    }

    public void UpdateCupCenter()
    {
        // Cup position is read from this GameObject.
        // It remains accurate after course switching.
    }

    private Vector3 GetCupCenter()
    {
        return new Vector3(
            transform.position.x,
            0f,
            transform.position.z
        );
    }

    private void OnTriggerStay(Collider other)
    {
        if (isCompleted || isSinking)
            return;

        if (Time.time - lastResetTime <
            INITIALIZATION_GRACE_PERIOD)
            return;

        Rigidbody ballRb = other.attachedRigidbody;

        if (ballRb == null)
            return;

        GolfBallController ballCtrl =
            ballRb.GetComponent<GolfBallController>();

        if (ballCtrl == null)
            return;

        if (ballCtrl.StrokeCount < 1)
            return;

        if (ballCtrl.IsLocked || ballCtrl.IsAiming)
            return;

        Vector3 ballPos = ballRb.position;
        Vector3 cupCenter = GetCupCenter();

        Vector2 ballXZ = new Vector2(
            ballPos.x,
            ballPos.z
        );

        Vector2 cupXZ = new Vector2(
            cupCenter.x,
            cupCenter.z
        );

        float horizontalDistance =
            Vector2.Distance(ballXZ, cupXZ);

        Vector3 velocity = ballRb.linearVelocity;

        Vector3 horizontalVelocity = new Vector3(
            velocity.x,
            0f,
            velocity.z
        );

        float horizontalSpeed =
            horizontalVelocity.magnitude;

        // Do not process the same fast approach repeatedly.
        if (hasRimBouncedThisApproach)
            return;

        // FAST SHOT: rebound off the cup rim.
        bool nearCupLip =
            ballPos.y >= -0.15f &&
            ballPos.y <= 0.75f;

        if (nearCupLip &&
            horizontalDistance <= captureRadius * 1.5f &&
            horizontalSpeed > maxEntrySpeed)
        {
            PerformRimBounce(
                ballRb,
                ballCtrl,
                horizontalVelocity,
                ballPos,
                cupCenter,
                horizontalSpeed
            );

            return;
        }

        // SLOW SHOT: gentle inward cup pull.
        if (horizontalDistance < captureRadius * 1.5f &&
            horizontalSpeed < maxEntrySpeed)
        {
            Vector3 inwardDirection =
                cupCenter - ballPos;

            inwardDirection.y = -0.2f;

            if (inwardDirection.sqrMagnitude > 0.0001f)
            {
                ballRb.AddForce(
                    inwardDirection.normalized * rimSuctionForce,
                    ForceMode.Force
                );
            }
        }

        // Capture and sink a valid slow putt.
        if (horizontalDistance <= captureRadius &&
            horizontalSpeed <= maxEntrySpeed &&
            nearCupLip)
        {
            StartCoroutine(
                SinkBallRoutine(ballCtrl, ballRb)
            );
        }
    }

    private void PerformRimBounce(
        Rigidbody ballRb,
        GolfBallController ballCtrl,
        Vector3 horizontalVelocity,
        Vector3 ballPos,
        Vector3 cupCenter,
        float horizontalSpeed)
    {
        if (ballRb == null || ballRb.isKinematic)
            return;

        Vector3 outwardDirection =
            new Vector3(
                ballPos.x - cupCenter.x,
                0f,
                ballPos.z - cupCenter.z
            );

        if (outwardDirection.sqrMagnitude < 0.0001f)
        {
            outwardDirection = -horizontalVelocity;
        }

        outwardDirection.Normalize();

        Vector3 reflectedVelocity =
            Vector3.Reflect(
                horizontalVelocity,
                outwardDirection
            );

        if (Vector3.Dot(
            reflectedVelocity,
            outwardDirection) < 0.1f)
        {
            reflectedVelocity =
                outwardDirection * horizontalSpeed;
        }

        reflectedVelocity *= reboundSpeedMultiplier;

        ballRb.linearVelocity = new Vector3(
            reflectedVelocity.x,
            Mathf.Max(
                reboundUpwardSpeed,
                Mathf.Abs(ballRb.linearVelocity.y) * 0.35f
            ),
            reflectedVelocity.z
        );

        hasRimBouncedThisApproach = true;

        // Rigidbody rebound velocity is already applied above.
        // GolfBallController detects movement in its next FixedUpdate.

        Debug.Log(
            "[HoleCompletionController] Fast shot bounced off cup rim."
        );
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody ballRb = other.attachedRigidbody;

        if (ballRb != null &&
            ballRb.GetComponent<GolfBallController>() != null)
        {
            hasRimBouncedThisApproach = false;
        }
    }

    private IEnumerator SinkBallRoutine(
        GolfBallController ballCtrl,
        Rigidbody ballRb)
    {
        if (isCompleted || isSinking)
            yield break;

        isSinking = true;
        isCompleted = true;

        ballCtrl.SetLocked(true);

        if (!ballRb.isKinematic)
        {
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.isKinematic = true;
        }

        Vector3 startPosition = ballRb.position;

        Vector3 cupCenter = GetCupCenter();

        Vector3 targetPosition = new Vector3(
            cupCenter.x,
            cupFloorY,
            cupCenter.z
        );

        float elapsed = 0f;

        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / sinkDuration
            );

            // Smooth ease-in/ease-out.
            float smoothT = t * t * (3f - 2f * t);

            ballCtrl.transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    smoothT
                );

            yield return null;
        }

        ballCtrl.transform.position = targetPosition;

        ballRb.position = targetPosition;

        finalScore = ballCtrl.StrokeCount;

        isSinking = false;

        if (finalScore >= 1)
        {
            Debug.Log(
                $"[HoleCompletionController] Hole completed in {finalScore} strokes."
            );

            OnLevelCompleted?.Invoke(finalScore);
        }
        else
        {
            Debug.LogWarning(
                "[HoleCompletionController] Invalid zero-stroke completion blocked."
            );

            isCompleted = false;
        }
    }

    /// <summary>
    /// Reset completion state during course loading and replay.
    /// </summary>
    public void ResetState()
    {
        StopAllCoroutines();

        isCompleted = false;
        isSinking = false;
        hasRimBouncedThisApproach = false;

        finalScore = 0;

        lastResetTime = Time.time;

        UpdateCupCenter();
    }

    /// <summary>
    /// Replay the current course.
    /// </summary>
    public void ReplayHole()
    {
        ResetState();

        if (activeBallController == null)
        {
            activeBallController =
                FindFirstObjectByType<GolfBallController>();
        }

        if (activeBallController != null)
        {
            activeBallController.ResetPosition(
                startingTeePosition
            );

            activeBallController.ResetStrokes();

            activeBallController.SetLocked(false);
        }

        SmoothCameraFollow camFollow =
            FindFirstObjectByType<SmoothCameraFollow>();

        if (camFollow != null)
        {
            camFollow.ResetToInitialPosition();
        }

        OnHoleReplayed?.Invoke();

        Debug.Log(
            "[HoleCompletionController] Hole reset for replay."
        );
    }

    private void OnGUI()
    {
        if (!showLegacyOnGUI)
            return;

        if (!isCompleted || isSinking)
            return;

        float scale = Mathf.Max(
            1f,
            Screen.height / 720f
        );

        float boxWidth = 320f * scale;
        float boxHeight = 220f * scale;

        float centerX =
            (Screen.width - boxWidth) * 0.5f;

        float centerY =
            (Screen.height - boxHeight) * 0.5f;

        GUI.color = new Color(
            0.08f, 0.12f, 0.18f, 0.92f
        );

        GUI.Box(
            new Rect(
                centerX,
                centerY,
                boxWidth,
                boxHeight
            ),
            GUIContent.none
        );

        GUI.color = Color.white;

        GUIStyle titleStyle =
            new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(22f * scale),
                fontStyle = FontStyle.Bold
            };

        titleStyle.normal.textColor =
            new Color(1f, 0.85f, 0.2f);

        GUI.Label(
            new Rect(
                centerX,
                centerY + 20f * scale,
                boxWidth,
                35f * scale
            ),
            "HOLE COMPLETED!",
            titleStyle
        );

        GUIStyle scoreStyle =
            new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(18f * scale)
            };

        scoreStyle.normal.textColor = Color.white;

        string scoreText =
            finalScore == 1
                ? "HOLE IN ONE!"
                : $"STROKES: {finalScore}";

        GUI.Label(
            new Rect(
                centerX,
                centerY + 65f * scale,
                boxWidth,
                30f * scale
            ),
            scoreText,
            scoreStyle
        );

        GUIStyle buttonStyle =
            new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(16f * scale),
                fontStyle = FontStyle.Bold
            };

        float buttonWidth = 180f * scale;
        float buttonHeight = 44f * scale;

        float buttonX =
            centerX + (boxWidth - buttonWidth) * 0.5f;

        float buttonY =
            centerY + 130f * scale;

        if (GUI.Button(
            new Rect(
                buttonX,
                buttonY,
                buttonWidth,
                buttonHeight
            ),
            "REPLAY HOLE",
            buttonStyle))
        {
            ReplayHole();
        }
    }
}

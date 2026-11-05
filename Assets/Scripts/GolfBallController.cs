using UnityEngine;
using System;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Core physics-based golf ball controller.
/// Handles touchpad/mouse drag-to-aim shooting, camera orbit,
/// trajectory preview and grounded rolling resistance.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class GolfBallController : MonoBehaviour
{
    [Header("Impulse & Power Calibration")]
    [SerializeField] private float minImpulse = 0.035f;
    [SerializeField] private float maxImpulse = 0.58f;
    [SerializeField] private float maxDragScreenFraction = 0.25f;
    [SerializeField] private float minDragPixels = 15f;

    [Header("Touchpad Ball Selection")]
    [SerializeField] private float ballSelectionScreenRadius = 75f;

    [Header("Turf Rolling Resistance")]
    [SerializeField] private float rollingResistance = 0.08f;
    [SerializeField] private float stopSpeedThreshold = 0.045f;
    [SerializeField] private float stopConfirmDuration = 0.15f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayerMask = ~0;
    [SerializeField] private float groundCheckOffset = 0.06f;

    [Header("Visual Guides & Trail")]
    [SerializeField] private AimTrajectoryGuide trajectoryGuide;
    [SerializeField] private TrailRenderer shotTrail;

    [Header("External References")]
    [SerializeField] private SmoothCameraFollow cameraFollow;
    [SerializeField] private HoleCompletionController holeController;

    // Public state
    public int StrokeCount { get; private set; }
    public bool HasTakenFirstStroke => StrokeCount > 0;
    public bool IsAiming => isAiming;
    public bool IsBallMoving => isBallMoving;
    public bool IsGrounded => isGrounded;
    public bool IsLocked { get; private set; }
    public bool IsHovered { get; private set; }
    public float CurrentAimPowerRatio { get; private set; }

    // Events
    public event Action<int> OnStrokeTaken;
    public event Action OnBallStopped;
    public event Action OnAimStarted;
    public event Action<float> OnAimPowerChanged;
    public event Action OnAimEnded;
    public event Action<float> OnShotReleased;

    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private Camera mainCamera;

    private bool isAiming;
    private bool isBallMoving;
    private bool isGrounded = true;

    private float stopTimer;

    private Vector2 dragStartScreenPos;

    private Vector3 pendingShotImpulse = Vector3.zero;
    private bool hasPendingShot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        mainCamera = Camera.main;

        rb.mass = 0.0459f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (trajectoryGuide == null)
        {
            trajectoryGuide = GetComponentInChildren<AimTrajectoryGuide>();

            if (trajectoryGuide == null)
            {
                GameObject guideObj = new GameObject("AimTrajectoryGuide");
                guideObj.transform.SetParent(transform, false);

                trajectoryGuide =
                    guideObj.AddComponent<AimTrajectoryGuide>();
            }
        }

        if (shotTrail == null)
        {
            shotTrail = GetComponent<TrailRenderer>();

            if (shotTrail == null)
            {
                shotTrail = gameObject.AddComponent<TrailRenderer>();

                shotTrail.time = 0.35f;
                shotTrail.startWidth = 0.06f;
                shotTrail.endWidth = 0.005f;

                shotTrail.startColor =
                    new Color(1f, 1f, 1f, 0.45f);

                shotTrail.endColor =
                    new Color(1f, 1f, 1f, 0f);

                Shader trailShader =
                    Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default");

                if (trailShader != null)
                {
                    shotTrail.material = new Material(trailShader);
                }

                shotTrail.emitting = false;
            }
        }
    }

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<SmoothCameraFollow>();

        if (holeController == null)
            holeController =
                FindFirstObjectByType<HoleCompletionController>();

        if (trajectoryGuide != null)
            trajectoryGuide.SetSelectionRingVisible(true);
    }

    private void Update()
    {
        PollPointerInput();
    }

    private void FixedUpdate()
    {
        if (rb == null || rb.isKinematic || IsLocked)
            return;

        CheckGroundedState();
        ApplyRollingResistance();

        // Recover the ball if it falls below the course.
        if (transform.position.y < -0.6f &&
            (holeController == null || !holeController.IsCompleted))
        {
            Debug.LogWarning(
                "[GolfBallController] Ball fell below turf. Recovering."
            );

            if (CourseManager.Instance != null)
            {
                CourseManager.Instance.PositionBallAtTee(
                    CourseManager.Instance.ActiveCourseId
                );
            }
            else
            {
                ResetPosition(new Vector3(0f, 0.1605f, -8f));
            }

            return;
        }

#if DEVELOPMENT_BUILD
        if (Time.frameCount % 300 == 0)
        {
            RaycastHit hit;

            bool didHit = Physics.Raycast(
                transform.position,
                Vector3.down,
                out hit,
                50f,
                groundLayerMask
            );

            string hitInfo = didHit
                ? $"{hit.collider.name} (y={hit.point.y:F3})"
                : "NONE";

            Debug.Log(
                $"[BALL MONITOR] Pos: {transform.position:F3}, " +
                $"Velocity: {rb.linearVelocity:F3}, Ground: {hitInfo}"
            );
        }
#endif

        if (hasPendingShot)
        {
            rb.AddForce(
                pendingShotImpulse,
                ForceMode.Impulse
            );

            pendingShotImpulse = Vector3.zero;
            hasPendingShot = false;

            isBallMoving = true;
            stopTimer = 0f;

            if (shotTrail != null)
                shotTrail.emitting = true;

            if (trajectoryGuide != null)
                trajectoryGuide.SetSelectionRingVisible(false);
        }
    }

    #region Input & Aiming

    private void PollPointerInput()
    {
        if (isBallMoving || IsLocked)
        {
            if (isAiming)
                CancelAim();

            if (cameraFollow != null && cameraFollow.IsOrbiting)
                cameraFollow.EndOrbitDrag();

            IsHovered = false;
            return;
        }

        bool pointerDown = false;
        bool pointerUp = false;

        Vector2 pointerPos = Vector2.zero;

        GetPointerState(
            out pointerDown,
            out pointerUp,
            out pointerPos
        );

        CheckBallHover(pointerPos);

        if (pointerDown)
        {
            if (IsPointerOverUI())
                return;

            if (IsPointerOverGolfBall(pointerPos))
            {
                isAiming = true;

                dragStartScreenPos = pointerPos;
                CurrentAimPowerRatio = 0f;

                OnAimStarted?.Invoke();
            }
            else
            {
                if (cameraFollow != null)
                {
                    cameraFollow.StartOrbitDrag(pointerPos);
                }
            }
        }
        else if (isAiming)
        {
            if (pointerUp)
            {
                ReleaseShot(pointerPos);
            }
            else
            {
                UpdateAimVisuals(pointerPos);
            }
        }
        else if (cameraFollow != null && cameraFollow.IsOrbiting)
        {
            if (pointerUp)
            {
                cameraFollow.EndOrbitDrag();
            }
            else
            {
                cameraFollow.UpdateOrbitDrag(pointerPos);
            }
        }
    }

    private bool IsPointerOverGolfBall(Vector2 pointerPos)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(pointerPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider == sphereCollider ||
                hit.transform == transform)
            {
                return true;
            }
        }

        Vector3 ballScreenPos =
            mainCamera.WorldToScreenPoint(transform.position);

        if (ballScreenPos.z > 0f)
        {
            float dist = Vector2.Distance(
                pointerPos,
                new Vector2(ballScreenPos.x, ballScreenPos.y)
            );

            if (dist <= ballSelectionScreenRadius)
                return true;
        }

        return false;
    }

    private void CheckBallHover(Vector2 pointerPos)
    {
        if (isAiming || isBallMoving || IsLocked)
        {
            IsHovered = false;
            return;
        }

        IsHovered = IsPointerOverGolfBall(pointerPos);
    }

    private bool IsPointerOverUI()
    {
        var eventSystem =
            UnityEngine.EventSystems.EventSystem.current;

        if (eventSystem == null)
            return false;

#if ENABLE_INPUT_SYSTEM

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            int touchId =
                Touchscreen.current.primaryTouch.touchId.ReadValue();

            if (eventSystem.IsPointerOverGameObject(touchId))
                return true;
        }

        if (eventSystem.IsPointerOverGameObject())
            return true;

        var eventData =
            new UnityEngine.EventSystems.PointerEventData(eventSystem);

        if (Mouse.current != null)
        {
            eventData.position =
                Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null)
        {
            eventData.position =
                Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else
        {
            return false;
        }

        var results =
            new System.Collections.Generic.List<
                UnityEngine.EventSystems.RaycastResult
            >();

        eventSystem.RaycastAll(eventData, results);

        return results.Count > 0;

#else

        return eventSystem.IsPointerOverGameObject();

#endif
    }

    private void GetPointerState(
        out bool pointerDown,
        out bool pointerUp,
        out Vector2 pointerPos)
    {
        pointerDown = false;
        pointerUp = false;
        pointerPos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM

        if (Touchscreen.current != null &&
            (
                Touchscreen.current.primaryTouch.press.isPressed ||
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame ||
                Touchscreen.current.primaryTouch.press.wasReleasedThisFrame
            ))
        {
            var touch = Touchscreen.current.primaryTouch;

            pointerPos = touch.position.ReadValue();

            if (touch.press.wasPressedThisFrame)
                pointerDown = true;

            if (touch.press.wasReleasedThisFrame)
                pointerUp = true;

            return;
        }

        if (Mouse.current != null)
        {
            pointerPos = Mouse.current.position.ReadValue();

            if (Mouse.current.leftButton.wasPressedThisFrame)
                pointerDown = true;

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                pointerUp = true;

            return;
        }

#endif
    }

    private void UpdateAimVisuals(Vector2 currentPointerPos)
    {
        Vector2 dragDelta =
            dragStartScreenPos - currentPointerPos;

        float dragDistance = dragDelta.magnitude;

        if (dragDistance < minDragPixels)
        {
            CurrentAimPowerRatio = 0f;

            OnAimPowerChanged?.Invoke(0f);

            if (trajectoryGuide != null)
                trajectoryGuide.Hide();

            return;
        }

        float maxDragPixels =
            Screen.height * maxDragScreenFraction;

        float powerRatio =
            Mathf.Clamp01(dragDistance / maxDragPixels);

        CurrentAimPowerRatio = powerRatio;

        OnAimPowerChanged?.Invoke(powerRatio);

        Vector3 shootDirection =
            CalculateWorldShootDirection(dragDelta);

        if (trajectoryGuide != null &&
            shootDirection.sqrMagnitude > 0.001f)
        {
            trajectoryGuide.UpdateAim(
                transform.position,
                shootDirection,
                powerRatio
            );
        }
    }

    private void ReleaseShot(Vector2 releasePointerPos)
    {
        Vector2 dragDelta =
            dragStartScreenPos - releasePointerPos;

        float dragDistance = dragDelta.magnitude;

        isAiming = false;

        float releasedPower = CurrentAimPowerRatio;

        CurrentAimPowerRatio = 0f;

        OnAimEnded?.Invoke();

        if (trajectoryGuide != null)
            trajectoryGuide.Hide();

        if (dragDistance < minDragPixels)
            return;

        float maxDragPixels =
            Screen.height * maxDragScreenFraction;

        float powerRatio =
            Mathf.Clamp01(dragDistance / maxDragPixels);

        Vector3 shootDirection =
            CalculateWorldShootDirection(dragDelta);

        if (shootDirection.sqrMagnitude < 0.001f)
            return;

        float impulseMagnitude =
            Mathf.Lerp(minImpulse, maxImpulse, powerRatio);

        pendingShotImpulse =
            shootDirection * impulseMagnitude;

        hasPendingShot = true;

        StrokeCount++;

        OnStrokeTaken?.Invoke(StrokeCount);
        OnShotReleased?.Invoke(releasedPower);
    }

    private void CancelAim()
    {
        isAiming = false;
        CurrentAimPowerRatio = 0f;

        OnAimEnded?.Invoke();

        if (trajectoryGuide != null)
            trajectoryGuide.Hide();
    }

    private Vector3 CalculateWorldShootDirection(Vector2 dragDelta)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return Vector3.forward;

        Vector3 camForward =
            Vector3.ProjectOnPlane(
                mainCamera.transform.forward,
                Vector3.up
            ).normalized;

        Vector3 camRight =
            Vector3.ProjectOnPlane(
                mainCamera.transform.right,
                Vector3.up
            ).normalized;

        Vector3 worldDir =
            (camRight * dragDelta.x) +
            (camForward * dragDelta.y);

        return worldDir.normalized;
    }

    #endregion

    #region Physics & Rolling Resistance

    private void CheckGroundedState()
    {
        float radius = sphereCollider != null
            ? sphereCollider.radius * transform.lossyScale.x
            : 0.35f;

        float checkDist = radius + groundCheckOffset;

        isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            checkDist,
            groundLayerMask
        );
    }

    private void ApplyRollingResistance()
    {
        if (rb == null || rb.isKinematic)
            return;

        Vector3 currentVel = rb.linearVelocity;

        Vector3 horizVel = new Vector3(
            currentVel.x,
            0f,
            currentVel.z
        );

        float horizontalSpeed = horizVel.magnitude;

        if (horizontalSpeed > 0.001f)
        {
            isBallMoving = true;

            if (isGrounded)
            {
                float deceleration =
                    rollingResistance *
                    Physics.gravity.magnitude *
                    Time.fixedDeltaTime;

                float newSpeed =
                    Mathf.Max(0f, horizontalSpeed - deceleration);

                Vector3 newHorizVel =
                    horizVel.normalized * newSpeed;

                rb.linearVelocity = new Vector3(
                    newHorizVel.x,
                    currentVel.y,
                    newHorizVel.z
                );

                horizontalSpeed = newSpeed;
            }

            if (horizontalSpeed < stopSpeedThreshold &&
                Mathf.Abs(currentVel.y) < 0.05f)
            {
                stopTimer += Time.fixedDeltaTime;

                if (stopTimer >= stopConfirmDuration)
                {
                    LockToRest();
                }
            }
            else
            {
                stopTimer = 0f;
            }
        }
        else if (isBallMoving &&
                 Mathf.Abs(currentVel.y) < 0.05f)
        {
            LockToRest();
        }
    }

    private void LockToRest()
    {
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        isBallMoving = false;
        stopTimer = 0f;

        if (shotTrail != null)
            shotTrail.emitting = false;

        if (trajectoryGuide != null)
            trajectoryGuide.SetSelectionRingVisible(true);

        OnBallStopped?.Invoke();
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;

        if (locked && isAiming)
            CancelAim();

        if (locked &&
            cameraFollow != null &&
            cameraFollow.IsOrbiting)
        {
            cameraFollow.EndOrbitDrag();
        }
    }

    public void ResetPosition(Vector3 position)
    {
        if (rb != null)
        {
            if (rb.isKinematic)
                rb.isKinematic = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
        }

        transform.position = position;

        Physics.SyncTransforms();

        if (rb != null)
        {
            rb.isKinematic = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        pendingShotImpulse = Vector3.zero;
        hasPendingShot = false;

        isAiming = false;
        isBallMoving = false;
        stopTimer = 0f;

        LockToRest();

        IsLocked = false;
    }

    public void ResetStrokes()
    {
        StrokeCount = 0;

        OnStrokeTaken?.Invoke(0);
    }

    #endregion
}
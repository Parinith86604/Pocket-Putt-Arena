
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Smooth golf camera controller.
/// Supports ball tracking, laptop touchpad orbit,
/// two-finger scrolling, zoom buttons and camera reset.
/// </summary>
public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Target & Reference")]
    [SerializeField] private Transform target;
    [SerializeField] private GolfBallController ballController;
    [SerializeField] private HoleCompletionController holeController;

    [Header("Smooth Damping Settings")]
    [SerializeField] private float smoothTime = 0.28f;
    [SerializeField] private float maxSpeed = 35f;

    [Header("Framing & Hole Visibility")]
    [SerializeField] private bool clampNearHole = true;
    [SerializeField] private float maxCameraZ = 0.5f;

    [Header("Touchpad Orbit Settings")]
    [SerializeField] private float orbitSensitivityX = 0.18f;
    [SerializeField] private float orbitSensitivityY = 0.15f;
    [SerializeField] private float minPitch = 20f;
    [SerializeField] private float maxPitch = 65f;

    [Header("Touchpad Zoom Settings")]
    [SerializeField] private float scrollZoomSensitivity = 0.003f;
    [SerializeField] private float minZoomMultiplier = 0.50f;
    [SerializeField] private float maxZoomMultiplier = 1.60f;
    [SerializeField] private float zoomSmoothSpeed = 10f;

    [Header("Field of View Zoom Settings")]
    [SerializeField] private float defaultFOV = 55f;
    [SerializeField] private float minFOV = 35f;
    [SerializeField] private float maxFOV = 70f;

    // Orbit and zoom state
    private float defaultPitch = 40f;
    private float defaultYaw = 0f;

    private float currentPitch = 40f;
    private float currentYaw = 0f;

    private float targetPitch = 40f;
    private float targetYaw = 0f;

    private float baseDistance = 17.2f;

    private float targetZoomMultiplier = 1f;
    private float currentZoomMultiplier = 1f;
    private float targetFOV = 55f;

    private Vector3 initialBaseOffset;
    private Vector3 currentVelocity = Vector3.zero;

    private bool isDraggingOrbit = false;

    private Vector2 lastPointerPos;

    private Camera cam;

    public bool IsOrbiting => isDraggingOrbit;

    // ---------------------------------------------------------
    // INITIALIZATION
    // ---------------------------------------------------------

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (cam != null)
        {
            defaultFOV = cam.fieldOfView;
            targetFOV = defaultFOV;
        }

        currentPitch = targetPitch = defaultPitch;
        currentYaw = targetYaw = defaultYaw;
    }

    private void Start()
    {
        FindReferences();
        CalculateBaseOffset();
    }

    private void FindReferences()
    {
        if (target == null)
        {
            if (ballController == null)
            {
                ballController =
                    FindFirstObjectByType<GolfBallController>();
            }

            if (ballController != null)
                target = ballController.transform;
        }

        if (holeController == null)
        {
            holeController =
                FindFirstObjectByType<HoleCompletionController>();
        }

        if (cam == null)
            cam = GetComponent<Camera>();
    }

    private void CalculateBaseOffset()
    {
        if (target != null)
        {
            Vector3 offset = transform.position - target.position;

            baseDistance = offset.magnitude;
            initialBaseOffset = offset;
        }
        else
        {
            initialBaseOffset =
                new Vector3(0f, 14.65f, -9f);

            baseDistance = initialBaseOffset.magnitude;
        }
    }

    // ---------------------------------------------------------
    // UPDATE
    // ---------------------------------------------------------

    private void Update()
    {
        PollScrollZoom();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindReferences();

            if (target == null)
                return;
        }

        // Smooth zoom interpolation
        currentZoomMultiplier = Mathf.Lerp(
            currentZoomMultiplier,
            targetZoomMultiplier,
            Time.deltaTime * zoomSmoothSpeed
        );

        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView,
                targetFOV,
                Time.deltaTime * zoomSmoothSpeed
            );
        }

        // Smooth orbit interpolation
        currentPitch = Mathf.Lerp(
            currentPitch,
            targetPitch,
            Time.deltaTime * 14f
        );

        currentYaw = Mathf.Lerp(
            currentYaw,
            targetYaw,
            Time.deltaTime * 14f
        );

        Quaternion orbitRotation =
            Quaternion.Euler(
                currentPitch,
                currentYaw,
                0f
            );

        Vector3 orbitDirection =
            orbitRotation * Vector3.back;

        float currentDistance =
            baseDistance * currentZoomMultiplier;

        Vector3 followTargetPos = target.position;

        if (followTargetPos.y < -0.5f)
            followTargetPos.y = 0.1605f;

        Vector3 targetCamPos =
            followTargetPos +
            orbitDirection * currentDistance;

        targetCamPos.y =
            Mathf.Max(targetCamPos.y, 1.5f);

        // Keep hole visible when facing forward.
        if (clampNearHole &&
            Mathf.Abs(
                Mathf.DeltaAngle(currentYaw, defaultYaw)
            ) < 45f)
        {
            targetCamPos.z =
                Mathf.Min(targetCamPos.z, maxCameraZ);
        }

        bool isCompleted =
            holeController != null &&
            holeController.IsCompleted;

        bool isAiming =
            ballController != null &&
            ballController.IsAiming;

        if (isAiming)
            return;

        if (isCompleted)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetCamPos,
                ref currentVelocity,
                0.40f,
                maxSpeed,
                Time.deltaTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                orbitRotation,
                Time.deltaTime * 5f
            );

            return;
        }

        float effectiveSmoothTime =
            isDraggingOrbit ? 0.08f : smoothTime;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetCamPos,
            ref currentVelocity,
            effectiveSmoothTime,
            maxSpeed,
            Time.deltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            orbitRotation,
            Time.deltaTime * 12f
        );
    }

    // ---------------------------------------------------------
    // TOUCHPAD SCROLL ZOOM
    // ---------------------------------------------------------

    private void PollScrollZoom()
    {
#if ENABLE_INPUT_SYSTEM

        if (Mouse.current == null)
            return;

        Vector2 scrollDelta =
            Mouse.current.scroll.ReadValue();

        if (Mathf.Abs(scrollDelta.y) < 0.01f)
            return;

        // Apply Inspector-configured scroll sensitivity.
        // The clamp prevents excessive zoom from mouse-wheel
        // events while allowing continuous touchpad scrolling.
        float adjustedScroll =
            Mathf.Clamp(scrollDelta.y, -40f, 40f)
            * scrollZoomSensitivity;

        // Positive scroll zooms in.
        targetZoomMultiplier = Mathf.Clamp(
            targetZoomMultiplier - adjustedScroll,
            minZoomMultiplier,
            maxZoomMultiplier
        );

        // Couple field of view to camera distance.
        targetFOV = Mathf.Clamp(
            targetFOV - adjustedScroll * 30f,
            minFOV,
            maxFOV
        );

#endif
    }

    // ---------------------------------------------------------
    // CAMERA ORBIT
    // ---------------------------------------------------------

    public void StartOrbitDrag(Vector2 pointerPos)
    {
        if (ballController != null &&
            (
                ballController.IsAiming ||
                ballController.IsBallMoving
            ))
        {
            return;
        }

        if (holeController != null &&
            holeController.IsCompleted)
        {
            return;
        }

        isDraggingOrbit = true;
        lastPointerPos = pointerPos;
    }

    public void UpdateOrbitDrag(Vector2 currentPointerPos)
    {
        if (!isDraggingOrbit)
            return;

        Vector2 delta =
            currentPointerPos - lastPointerPos;

        lastPointerPos = currentPointerPos;

        targetYaw +=
            delta.x * orbitSensitivityX;

        targetPitch -=
            delta.y * orbitSensitivityY;

        targetPitch = Mathf.Clamp(
            targetPitch,
            minPitch,
            maxPitch
        );
    }

    public void EndOrbitDrag()
    {
        isDraggingOrbit = false;
    }

    // ---------------------------------------------------------
    // PUBLIC ZOOM CONTROLS
    // ---------------------------------------------------------

    public void ZoomIn()
    {
        targetZoomMultiplier = Mathf.Clamp(
            targetZoomMultiplier - 0.20f,
            minZoomMultiplier,
            maxZoomMultiplier
        );

        targetFOV = Mathf.Clamp(
            targetFOV - 6f,
            minFOV,
            maxFOV
        );

        Debug.Log(
            $"[SmoothCameraFollow] ZoomIn: " +
            $"Distance {targetZoomMultiplier:F2}, " +
            $"FOV {targetFOV:F1}"
        );
    }

    public void ZoomOut()
    {
        targetZoomMultiplier = Mathf.Clamp(
            targetZoomMultiplier + 0.20f,
            minZoomMultiplier,
            maxZoomMultiplier
        );

        targetFOV = Mathf.Clamp(
            targetFOV + 6f,
            minFOV,
            maxFOV
        );

        Debug.Log(
            $"[SmoothCameraFollow] ZoomOut: " +
            $"Distance {targetZoomMultiplier:F2}, " +
            $"FOV {targetFOV:F1}"
        );
    }

    // ---------------------------------------------------------
    // CAMERA SNAP & RESET
    // ---------------------------------------------------------

    public void SnapToTarget()
    {
        FindReferences();

        if (target == null)
            return;

        currentPitch = targetPitch = defaultPitch;
        currentYaw = targetYaw = defaultYaw;

        currentZoomMultiplier =
            targetZoomMultiplier = 1f;

        targetFOV = defaultFOV;

        if (cam != null)
            cam.fieldOfView = defaultFOV;

        Quaternion rotation =
            Quaternion.Euler(
                defaultPitch,
                defaultYaw,
                0f
            );

        Vector3 orbitDirection =
            rotation * Vector3.back;

        transform.position =
            target.position +
            orbitDirection * baseDistance;

        transform.rotation = rotation;

        currentVelocity = Vector3.zero;
        isDraggingOrbit = false;
    }

    public void ResetToInitialPosition()
    {
        targetYaw = defaultYaw;
        targetPitch = defaultPitch;

        targetZoomMultiplier = 1f;
        targetFOV = defaultFOV;

        currentVelocity = Vector3.zero;
        isDraggingOrbit = false;

        SnapToTarget();

        Debug.Log(
            "[SmoothCameraFollow] Camera reset successfully."
        );
    }

    // ---------------------------------------------------------
    // COURSE FRAMING
    // ---------------------------------------------------------

    public void ConfigureCourseFraming(
        Vector3 initialOffset,
        float pitch,
        float yaw,
        float maxZ)
    {
        defaultPitch = targetPitch = currentPitch = pitch;
        defaultYaw = targetYaw = currentYaw = yaw;

        maxCameraZ = maxZ;

        targetZoomMultiplier = 1f;
        currentZoomMultiplier = 1f;

        targetFOV = defaultFOV;

        currentVelocity = Vector3.zero;

        initialBaseOffset = initialOffset;
        baseDistance = initialOffset.magnitude;

        SnapToTarget();
    }
}

using UnityEngine;

/// <summary>
/// Orchestrates ten playable Pocket Putt Arena courses.
/// Handles course progression, environment generation, tee placement and replay.
/// </summary>
public class CourseManager : MonoBehaviour
{
    public static CourseManager Instance { get; private set; }

    [Header("Course Progression")]
    [SerializeField, Range(1, 10)] private int activeCourseId = 1;
    public int ActiveCourseId => activeCourseId;
    public int CourseCount => courseNames.Length;

    [Header("Scene References")]
    [SerializeField] private GolfBallController ballController;
    [SerializeField] private HoleCompletionController holeController;
    [SerializeField] private SmoothCameraFollow cameraFollow;
    [SerializeField] private GolfHUDController hudController;
    [SerializeField] private CourseEnvironmentBuilder envBuilder;
    [SerializeField] private OpponentBallManager oppManager;

    private readonly string[] courseNames =
    {
        "Evergreen Valley",
        "Copper Canyon",
        "Frostlight Ridge",
        "Jade Temple Garden",
        "Azure Coast",
        "Volcanic Ember Run",
        "Moonlit Crater",
        "Redwood Rapids",
        "Sky Harbor",
        "Crystal Caverns"
    };

    private readonly int[] coursePars =
    {
        2, 3, 3, 2, 3, 3, 3, 3, 3, 3
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        FindSceneReferences();
        EnsurePhysicalGroundColliders();
    }

    private void Start()
    {
        EnsurePhysicalGroundColliders();
        EnsureVisualSystems();
        LoadCourse(activeCourseId);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void EnsureVisualSystems()
    {
        FindSceneReferences();

        if (envBuilder == null)
        {
            envBuilder = GetComponent<CourseEnvironmentBuilder>();

            if (envBuilder == null)
                envBuilder = FindFirstObjectByType<CourseEnvironmentBuilder>();

            if (envBuilder == null)
                envBuilder = gameObject.AddComponent<CourseEnvironmentBuilder>();
        }

        if (oppManager == null)
        {
            oppManager = GetComponent<OpponentBallManager>();

            if (oppManager == null)
                oppManager = gameObject.AddComponent<OpponentBallManager>();
        }

        if (oppManager != null)
            oppManager.SpawnOpponents();

        if (holeController != null &&
            holeController.GetComponent<HoleCelebrationEffect>() == null)
        {
            holeController.gameObject.AddComponent<HoleCelebrationEffect>();
        }
    }

    private void FindSceneReferences()
    {
        if (ballController == null)
            ballController = FindFirstObjectByType<GolfBallController>();

        if (holeController == null)
            holeController = FindFirstObjectByType<HoleCompletionController>();

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<SmoothCameraFollow>();

        if (hudController == null)
            hudController = FindFirstObjectByType<GolfHUDController>();

        if (envBuilder == null)
        {
            envBuilder = GetComponent<CourseEnvironmentBuilder>();

            if (envBuilder == null)
                envBuilder = FindFirstObjectByType<CourseEnvironmentBuilder>();
        }

        if (oppManager == null)
            oppManager = GetComponent<OpponentBallManager>();
    }

    /// <summary>
    /// Loads a course by its 1-based ID, from 1 through 10.
    /// </summary>
    public void LoadCourse(int courseId)
    {
        activeCourseId = Mathf.Clamp(courseId, 1, courseNames.Length);
        int index = activeCourseId - 1;

        FindSceneReferences();
        EnsurePhysicalGroundColliders();

        // Reset hole state before moving the ball or changing the course.
        Vector3 holePosition = new Vector3(0f, 0f, 8f);

        if (holeController != null)
        {
            holeController.ResetState();
            holeController.transform.position = holePosition;
            holeController.UpdateCupCenter();

            HoleCelebrationEffect celebration =
                holeController.GetComponent<HoleCelebrationEffect>();

            if (celebration != null)
                celebration.HideCelebration();
        }

        // Clear the previous completion UI and stroke display.
        if (hudController != null)
        {
            hudController.HideCompletionPanel();
            hudController.UpdateStrokeText(0);
        }

        // Build the requested thematic environment (1-10).
        if (envBuilder != null)
        {
            envBuilder.ApplyTheme(activeCourseId);
        }

        // Respawn at the tee after the course geometry is generated.
        PositionBallAtTee(activeCourseId);

        // Refresh displayed course title and par.
        if (hudController != null)
        {
            hudController.UpdateCourseHeader(
                courseNames[index],
                coursePars[index]
            );
        }

        // Rebuild opponent visuals for the currently selected course.
        if (oppManager != null)
            oppManager.SpawnOpponents();

        Debug.Log(
            $"[CourseManager] Loaded Course {activeCourseId}/10: " +
            $"{courseNames[index]} (Par {coursePars[index]}, Medium)"
        );
    }

    /// <summary>
    /// Places the ball at the tee and restores physics and strokes.
    /// </summary>
    public void PositionBallAtTee(int courseId)
    {
        EnsurePhysicalGroundColliders();

        Vector3 teePosition =
            new Vector3(0f, 0.1605f, -8f);

        RaycastHit hit;

        if (Physics.Raycast(
            new Vector3(teePosition.x, 5f, teePosition.z),
            Vector3.down,
            out hit,
            15f))
        {
            float ballRadius = 0.1575f;

            if (ballController != null)
            {
                SphereCollider sphere =
                    ballController.GetComponent<SphereCollider>();

                if (sphere != null)
                {
                    ballRadius =
                        sphere.radius *
                        ballController.transform.lossyScale.x;
                }
            }

            teePosition.y =
                hit.point.y + ballRadius + 0.003f;
        }

        if (ballController != null)
        {
            ballController.ResetPosition(teePosition);
            ballController.ResetStrokes();
            ballController.SetLocked(false);
        }

        if (cameraFollow != null)
        {
            cameraFollow.SnapToTarget();
        }
    }

    /// <summary>
    /// Ensures the base putting surface and course perimeter walls
    /// have enabled, solid colliders.
    /// </summary>
    public void EnsurePhysicalGroundColliders()
    {
        GameObject groundObject =
            GameObject.Find("Course_Ground");

        if (groundObject != null)
        {
            BoxCollider box =
                groundObject.GetComponent<BoxCollider>();

            if (box == null)
                box = groundObject.AddComponent<BoxCollider>();

            box.enabled = true;
            box.isTrigger = false;
            box.center = new Vector3(0f, -1f, 0f);
            box.size = new Vector3(10f, 2f, 10f);

            PhysicsMaterial turfMaterial =
                Resources.Load<PhysicsMaterial>("PM_GolfGreen");

            if (turfMaterial != null)
                box.sharedMaterial = turfMaterial;
        }

        string[] wallNames =
        {
            "Wall_Left",
            "Wall_Right",
            "Wall_Back",
            "Wall_Front"
        };

        foreach (string wallName in wallNames)
        {
            GameObject wall = GameObject.Find(wallName);

            if (wall == null)
                continue;

            Collider collider = wall.GetComponent<Collider>();

            if (collider != null)
            {
                collider.enabled = true;
                collider.isTrigger = false;
            }
        }

        Physics.SyncTransforms();
    }

    public void LoadNextCourse()
    {
        int nextCourse =
            (activeCourseId % courseNames.Length) + 1;

        LoadCourse(nextCourse);
    }

    public void LoadPreviousCourse()
    {
        int previousCourse =
            activeCourseId - 1;

        if (previousCourse < 1)
            previousCourse = courseNames.Length;

        LoadCourse(previousCourse);
    }

    public void ReplayCurrentCourse()
    {
        if (holeController != null)
        {
            holeController.ReplayHole();
        }
        else
        {
            LoadCourse(activeCourseId);
        }
    }

    public string GetCourseName(int courseId)
    {
        int index = Mathf.Clamp(
            courseId - 1,
            0,
            courseNames.Length - 1
        );

        return courseNames[index];
    }

    public int GetCoursePar(int courseId)
    {
        int index = Mathf.Clamp(
            courseId - 1,
            0,
            coursePars.Length - 1
        );

        return coursePars[index];
    }

    public string GetCourseDifficulty(int courseId)
    {
        return "Medium";
    }
}

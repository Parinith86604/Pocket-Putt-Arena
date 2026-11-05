using UnityEngine;

/// <summary>
/// Data for one Pocket Putt Arena course.
/// Course IDs 1-10 are supported.
/// Difficulty is Medium for the current ten-course set.
/// </summary>
[CreateAssetMenu(
    fileName = "CourseConfig",
    menuName = "PocketPuttArena/CourseConfig"
)]
public class CourseConfig : ScriptableObject
{
    [Header("Course Identity")]
    [Range(1, 10)]
    public int courseId = 1;

    public string courseName = "Evergreen Valley";

    public string difficulty = "Medium";

    [Min(1)]
    public int parScore = 3;

    [Header("Course Coordinates")]
    public Vector3 teePosition =
        new Vector3(0f, 0.1605f, -8f);

    public Vector3 holePosition =
        new Vector3(0f, 0f, 8f);

    public Vector3 flagPosition =
        new Vector3(0f, 1f, 8f);

    [Header("Camera Configuration")]
    public Vector3 cameraInitialOffset =
        new Vector3(0f, 14.65f, -9f);

    [Range(20f, 65f)]
    public float cameraPitch = 40f;

    public float cameraYaw = 0f;

    [Range(35f, 70f)]
    public float defaultFOV = 55f;

    public float maxCameraZ = 0.5f;

    [Header("Surface Settings")]
    [Min(0f)]
    public float rollingResistance = 0.08f;

    [Min(0.1f)]
    public float maxEntrySpeed = 3.2f;

    [Min(0.05f)]
    public float captureRadius = 0.24f;

    public static string GetDefaultCourseName(int id)
    {
        switch (id)
        {
            case 1: return "Evergreen Valley";
            case 2: return "Copper Canyon";
            case 3: return "Frostlight Ridge";
            case 4: return "Jade Temple Garden";
            case 5: return "Azure Coast";
            case 6: return "Volcanic Ember Run";
            case 7: return "Moonlit Crater";
            case 8: return "Redwood Rapids";
            case 9: return "Sky Harbor";
            case 10: return "Crystal Caverns";
            default: return "Unknown Course";
        }
    }

    public static int GetDefaultPar(int id)
    {
        switch (id)
        {
            case 1: return 2;
            case 2: return 3;
            case 3: return 3;
            case 4: return 2;
            case 5: return 3;
            default: return 3;
        }
    }
}

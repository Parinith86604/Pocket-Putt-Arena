
using UnityEngine;
using UnityEditor;

public static class SetCourseDifficultyTiers
{
    [MenuItem("Pocket Putt Arena/Assign Difficulty Tiers")]
    public static void AssignDifficultyTiers()
    {
        string[] guids =
            AssetDatabase.FindAssets("t:CourseConfig");

        bool[] found = new bool[11];

        int mediumCount = 0;
        int difficultCount = 0;
        int moreDifficultCount = 0;
        int invalidCount = 0;

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            CourseConfig config =
                AssetDatabase.LoadAssetAtPath<CourseConfig>(path);

            if (config == null)
                continue;

            int id = config.courseId;

            if (id < 1 || id > 10)
            {
                invalidCount++;
                Debug.LogWarning(
                    $"Invalid course ID {id}: {path}"
                );
                continue;
            }

            string difficulty;

            if (id <= 3)
            {
                difficulty = "Medium";
                mediumCount++;
            }
            else if (id <= 6)
            {
                difficulty = "Difficult";
                difficultCount++;
            }
            else
            {
                difficulty = "More Difficult";
                moreDifficultCount++;
            }

            config.difficulty = difficulty;

            EditorUtility.SetDirty(config);
            found[id] = true;

            Debug.Log(
                $"Course {id:00}: {config.courseName} - {difficulty}"
            );
        }

        AssetDatabase.SaveAssets();

        int missingCount = 0;

        for (int id = 1; id <= 10; id++)
        {
            if (!found[id])
            {
                missingCount++;

                Debug.LogWarning(
                    $"Missing configuration for Course {id:00}."
                );
            }
        }

        string result =
            "Difficulty assignment completed.\n\n" +
            $"Medium: {mediumCount}/3\n" +
            $"Difficult: {difficultCount}/3\n" +
            $"More Difficult: {moreDifficultCount}/4\n\n" +
            $"Missing courses: {missingCount}\n" +
            $"Invalid configurations: {invalidCount}";

        Debug.Log("[Pocket Putt Arena] " + result);

        EditorUtility.DisplayDialog(
            "Pocket Putt Arena - Difficulty Setup",
            result,
            "OK"
        );
    }
}

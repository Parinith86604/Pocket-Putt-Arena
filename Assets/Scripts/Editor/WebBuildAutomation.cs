using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.IO;

[InitializeOnLoad]
public static class WebBuildAutomation
{
    private static bool isBuilding = false;
    private static readonly string CommandFilePath = "D:/Unity/Projects/PocketPuttArena/Temp/build_command.txt";
    private static readonly string ResultFilePath = "D:/Unity/Projects/PocketPuttArena/Temp/build_result.txt";

    static WebBuildAutomation()
    {
        EditorApplication.update += CheckBuildTrigger;
    }

    [InitializeOnLoadMethod]
    private static void OnInit()
    {
        EditorApplication.update -= CheckBuildTrigger;
        EditorApplication.update += CheckBuildTrigger;
    }

    private static void CheckBuildTrigger()
    {
        if (isBuilding) return;

        if (File.Exists(CommandFilePath))
        {
            string command = "";
            try
            {
                command = File.ReadAllText(CommandFilePath).Trim().ToLowerInvariant();
                File.Delete(CommandFilePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebBuildAutomation] Could not read command file: {ex.Message}");
                return;
            }

            isBuilding = true;
            try
            {
                if (command == "development")
                {
                    BuildDevelopment();
                }
                else if (command == "release")
                {
                    BuildRelease();
                }
                else if (command == "all")
                {
                    BuildDevelopment();
                    BuildRelease();
                }
                else
                {
                    Debug.LogWarning($"[WebBuildAutomation] Unknown build command: {command}");
                }
            }
            finally
            {
                isBuilding = false;
            }
        }
    }

    [MenuItem("PocketPutt/Build/Web Development")]
    public static void BuildDevelopment()
    {
        ExecuteBuild(
            isDevelopment: true,
            outputPath: "D:/Unity/Projects/PocketPuttArena/Builds/Web_Development",
            compression: WebGLCompressionFormat.Disabled,
            reportName: "Development"
        );
    }

    [MenuItem("PocketPutt/Build/Web Release")]
    public static void BuildRelease()
    {
        ExecuteBuild(
            isDevelopment: false,
            outputPath: "D:/Unity/Projects/PocketPuttArena/Builds/Web_Release",
            compression: WebGLCompressionFormat.Brotli,
            reportName: "Release"
        );
    }

    public static void BuildWebDevelopmentBatch()
    {
        BuildDevelopment();
    }

    public static void BuildWebReleaseBatch()
    {
        BuildRelease();
    }

    public static void BuildAllBatch()
    {
        BuildDevelopment();
        BuildRelease();
    }

    private static void ExecuteBuild(bool isDevelopment, string outputPath, WebGLCompressionFormat compression, string reportName)
    {
        DateTime startTime = DateTime.Now;
        Debug.Log($"[WebBuildAutomation] Starting {reportName} WebGL build to: {outputPath} at {startTime:HH:mm:ss}");

        // Ensure target directory exists
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // Configure Player Settings for WebGL
        PlayerSettings.WebGL.template = "PROJECT:Responsive";
        PlayerSettings.WebGL.compressionFormat = compression;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.exceptionSupport = isDevelopment ? WebGLExceptionSupport.FullWithoutStacktrace : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.emscriptenArgs = "-O1";
        PlayerSettings.runInBackground = true;

        EditorUserBuildSettings.development = isDevelopment;
        EditorUserBuildSettings.connectProfiler = false;

        // Scenes to include (Course 01 only)
        string[] scenes = new string[] { "Assets/Scenes/GolfCourse01.unity" };

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = isDevelopment ? BuildOptions.Development : BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;
        DateTime endTime = DateTime.Now;
        TimeSpan duration = endTime - startTime;

        string reportText = GenerateReport(reportName, summary, duration, outputPath, compression, isDevelopment);

        // Write report files
        string reportFile = Path.Combine(outputPath, "build_report.txt");
        File.WriteAllText(reportFile, reportText);
        File.WriteAllText(ResultFilePath, reportText);

        Debug.Log($"[WebBuildAutomation] {reportName} build completed with result: {summary.result} in {duration.TotalSeconds:F1}s.");
    }

    private static string GenerateReport(string reportName, BuildSummary summary, TimeSpan duration, string outputPath, WebGLCompressionFormat compression, bool isDev)
    {
        using (StringWriter sw = new StringWriter())
        {
            sw.WriteLine($"=== POCKET PUTT ARENA: {reportName.ToUpper()} WEBGL BUILD REPORT ===");
            sw.WriteLine($"Build Result: {summary.result}");
            sw.WriteLine($"Target: WebGL");
            sw.WriteLine($"Development Mode: {isDev}");
            sw.WriteLine($"Compression Format: {compression}");
            sw.WriteLine($"Decompression Fallback: Enabled");
            sw.WriteLine($"Template: PROJECT:Responsive");
            sw.WriteLine($"Output Path: {outputPath}");
            sw.WriteLine($"Total Duration: {duration.TotalMinutes:F1} min ({duration.TotalSeconds:F1} sec)");
            sw.WriteLine($"Total Build Size: {FormatBytes((long)summary.totalSize)}");
            sw.WriteLine($"Warnings: {summary.totalWarnings}");
            sw.WriteLine($"Errors: {summary.totalErrors}");
            sw.WriteLine();

            sw.WriteLine("--- OUTPUT FILES & SIZES ---");
            if (Directory.Exists(outputPath))
            {
                var files = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories);
                Array.Sort(files);
                foreach (var file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    string relPath = file.Substring(outputPath.Length).TrimStart('/', '\\');
                    sw.WriteLine($"  {relPath,-45} {FormatBytes(fi.Length),12}");
                }
            }
            sw.WriteLine();
            sw.WriteLine("=== END OF REPORT ===");
            return sw.ToString();
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{(bytes / 1024f):F2} KB";
        return $"{(bytes / (1024f * 1024f)):F2} MB";
    }
}

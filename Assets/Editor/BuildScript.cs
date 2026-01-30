using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildScript
{
    public static void BuildCyber()
    {
        string[] scenes = { "Assets/Scenes/Main.unity" };
        string buildPath = Path.Combine(Directory.GetCurrentDirectory(), "ReflectiveHighway_WetRoads.apk");
        
        Debug.Log(">>> STARTING BUILD TO: " + buildPath);
        
        BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.Android, BuildOptions.None);
        
        if (File.Exists(buildPath)) {
            Debug.Log(">>> BUILD SUCCESS: " + buildPath);
        } else {
            Debug.Log(">>> BUILD FINISHED BUT FILE MISSING: " + buildPath);
        }
    }
}
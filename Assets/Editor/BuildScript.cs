using UnityEditor;
using UnityEngine;

public class BuildScript
{
    public static void BuildCyber()
    {
        string[] scenes = { "Assets/Scenes/Main.unity" };
        string buildPath = "ReflectiveHighway_DevGuard.apk";
        
        BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.Android, BuildOptions.None);
    }
}
using UnityEditor;
using UnityEngine;

public class Builder
{
    public static void BuildNow()
    {
        string[] scenes = { "Assets/Scenes/Main.unity" };
        string buildPath = "/home/ubuntu/clawd/ReflectiveHighway/ReflectiveHighway_WetRoads.apk";
        
        Debug.Log(">>> STARTING BUILD TO: " + buildPath);
        
        BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.Android, BuildOptions.None);
        
        Debug.Log(">>> BUILD COMMAND FINISHED");
    }
}

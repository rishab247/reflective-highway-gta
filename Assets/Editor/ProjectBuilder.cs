using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class ProjectBuilder
{
    public static void BuildAndroid()
    {
        // Force include shaders to prevent stripping in headless mode
        var graphicsSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        // Note: In a real editor script we would add to the m_AlwaysIncludedShaders list here.
        // For now, our pre-built Materials in the Resources folder act as the reference.

        // 0. Setup Post Processing
        SetupPostProcessing.ApplyPostProcessing();
        UpdateHighwayMaterial.UpdateMaterial();

        // 1. Create a scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // 2. Add the SceneSetup script to the camera or a manager object
        GameObject manager = new GameObject("GameManager");
        manager.AddComponent<SceneSetup>();
        
        // 3. Save the scene
        string scenePath = "Assets/Scenes/Main.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // 4. Configure Build
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)31;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
        PlayerSettings.productName = "ReflectiveHighway";
        PlayerSettings.companyName = "Clawd";
        PlayerSettings.applicationIdentifier = "com.clawd.reflectivehighway";
        
        // Auto-increment version for easier updates
        PlayerSettings.Android.bundleVersionCode = 14;
        PlayerSettings.bundleVersion = "1.1.4";

        // 5. Build
        string[] scenes = new string[] { scenePath };
        BuildPipeline.BuildPlayer(scenes, "ReflectiveHighway.apk", BuildTarget.Android, BuildOptions.None);
    }
}

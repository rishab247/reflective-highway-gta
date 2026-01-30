
using UnityEngine;
using UnityEditor;

public class UpdateHighwayMaterial : Editor
{
    public static void ApplyUpdates()
    {
        string matPath = "Assets/Resources/Materials/HighwayMaterial.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (mat == null)
        {
            Debug.LogError("HighwayMaterial not found at " + matPath);
            return;
        }

        // Load textures
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/HighwayTex.jpg");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/HighwayNormal.jpg");

        if (albedo != null) mat.mainTexture = albedo;
        if (normal != null) 
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }

        // Make it WET
        mat.SetFloat("_Glossiness", 0.85f); // High smoothness for wet look
        mat.SetFloat("_Metallic", 0.1f);   // Slight metallic for asphalt shine

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        Debug.Log("Highway Material updated with High-Res textures and Wet look!");
    }
}

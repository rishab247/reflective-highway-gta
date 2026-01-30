using UnityEngine;
using UnityEditor;

public class UpdateHighwayMaterial : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Update Highway Material")]
    public static void UpdateMaterial()
    {
        string matPath = "Assets/Resources/Materials/HighwayMaterial.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (mat == null)
        {
            Debug.LogError("Material not found! Creating new one.");
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            // FORCE Standard shader to ensure it's included in build
            mat.shader = Shader.Find("Standard");
        }

        // Load Maps
        Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/HighwayNormal.png");
        Texture2D maskMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/HighwayMask.png");
        
        if (normalMap)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normalMap));
            if (importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            mat.SetTexture("_BumpMap", normalMap);
            mat.EnableKeyword("_NORMALMAP");
            Debug.Log("Applied Normal Map.");
        }

        if (maskMap)
        {
            // Set as Mask Map (Metallic/Smoothness)
            mat.SetTexture("_MetallicGlossMap", maskMap); // Standard shader slot
            mat.SetFloat("_Smoothness", 1.0f); // Map controls value
            mat.EnableKeyword("_METALLICGLOSSMAP");
            Debug.Log("Applied Mask Map.");
        }
        
        // Settings for "Wet" look
        mat.SetFloat("_Glossiness", 0.9f); // High base smoothness
        mat.SetFloat("_Metallic", 0.0f);   // Asphalt isn't metal

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
    }
    
    [MenuItem("Tools/Update Building Windows")]
    public static void UpdateBuildings()
    {
         // Find materials named "BuildingMaterial" or similar if they exist
         // or specific ones used in scene. 
         // For this task, we'll try to find a material or just log instructions.
         Debug.Log("Building update requires specific material targeting. Please assign 'BuildingSmoothness.png' to the Metallic/Smoothness slot of your building materials manually if not using a shared one.");
    }
#endif
}

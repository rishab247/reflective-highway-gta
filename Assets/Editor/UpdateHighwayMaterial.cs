using UnityEngine;
using UnityEditor;

public class UpdateHighwayMaterial : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Update Highway Material")]
    public static void UpdateMaterial()
    {
        // 1. Road Material
        string matPath = "Assets/Resources/Materials/HighwayMaterial.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (mat == null)
        {
            Debug.Log("Creating new Road Material...");
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = Shader.Find("Standard");
        }

        Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/HighwayNormal.png");
        Texture2D maskMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/HighwayMask.png");
        
        if (normalMap) {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normalMap));
            if (importer.textureType != TextureImporterType.NormalMap) {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            mat.SetTexture("_BumpMap", normalMap);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (maskMap) {
            mat.SetTexture("_MetallicGlossMap", maskMap); 
            mat.SetFloat("_Smoothness", 1.0f); 
            mat.EnableKeyword("_METALLICGLOSSMAP");
        }
        
        mat.SetFloat("_Glossiness", 0.9f);
        mat.SetFloat("_Metallic", 0.0f);
        EditorUtility.SetDirty(mat);

        // 2. Building Material
        string buildMatPath = "Assets/Resources/Materials/BuildingMaterial.mat";
        Material buildMat = AssetDatabase.LoadAssetAtPath<Material>(buildMatPath);
        if (buildMat == null)
        {
            buildMat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(buildMat, buildMatPath);
        }
        else
        {
            buildMat.shader = Shader.Find("Standard");
        }

        Texture2D buildTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/BuildingTex_V2.png");
        if (buildTex != null)
        {
            buildMat.mainTexture = buildTex;
            // Make windows reflective
            buildMat.SetFloat("_Glossiness", 0.9f); 
            buildMat.SetFloat("_Metallic", 0.3f); // Steel/Glass look
        }
        EditorUtility.SetDirty(buildMat);

        // 3. Ground/Sidewalk Material
        string groundMatPath = "Assets/Resources/Materials/GroundMaterial.mat";
        Material groundMat = AssetDatabase.LoadAssetAtPath<Material>(groundMatPath);
        if (groundMat == null)
        {
            groundMat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(groundMat, groundMatPath);
        }
        else
        {
            groundMat.shader = Shader.Find("Standard");
        }

        Texture2D groundTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/GroundTex.png");
        if (groundTex != null)
        {
            groundMat.mainTexture = groundTex;
            groundMat.SetFloat("_Glossiness", 0.2f); // Matte concrete
            groundMat.SetFloat("_Metallic", 0.0f);
        }
        EditorUtility.SetDirty(groundMat);

        AssetDatabase.SaveAssets();
        Debug.Log("All Materials Updated.");
    }
    
    [MenuItem("Tools/Update Building Windows")]
    public static void UpdateBuildings()
    {
         Debug.Log("Building update handled in main function.");
    }
#endif
}

using UnityEngine;
using UnityEditor;

public class ImportConfig : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Configure Assets")]
    public static void Configure()
    {
        ConfigureSprite("Assets/Resources/Textures/UI_SteeringWheel.png");
        ConfigureSprite("Assets/Resources/Textures/UI_GasPedal.png");
        ConfigureSprite("Assets/Resources/Textures/UI_BrakePedal.png");
        
        ConfigureTexture("Assets/Resources/Textures/RealBuilding.png");
        
        AssetDatabase.Refresh();
    }

    static void ConfigureSprite(string path)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            Debug.Log("Configured Sprite: " + path);
        }
    }

    static void ConfigureTexture(string path)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
            Debug.Log("Configured Texture: " + path);
        }
    }
#endif
}

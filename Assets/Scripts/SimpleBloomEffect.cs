using UnityEngine;

public class SimpleBloomEffect : MonoBehaviour
{
    public Material bloomMat;

    void Start()
    {
        Shader s = Shader.Find("Hidden/SimpleBloom");
        if (s != null)
        {
            bloomMat = new Material(s);
            bloomMat.SetFloat("_Threshold", 0.7f);
            bloomMat.SetFloat("_Intensity", 1.2f);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (bloomMat != null)
        {
            Graphics.Blit(source, destination, bloomMat);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}

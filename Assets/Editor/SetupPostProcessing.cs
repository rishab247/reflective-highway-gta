
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.PostProcessing;

public class SetupPostProcessing : Editor
{
    [MenuItem("Highway/Setup Post-Processing")]
    public static void ApplyPostProcessing()
    {
        // Find the Main Camera
        GameObject cameraGameObject = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraGameObject == null)
        {
            Debug.LogError("Main Camera not found. Make sure your camera is tagged 'MainCamera'.");
            return;
        }

        // Add PostProcessLayer to the camera
        PostProcessLayer layer = cameraGameObject.GetComponent<PostProcessLayer>();
        if (layer == null)
        {
            layer = cameraGameObject.AddComponent<PostProcessLayer>();
        }
        layer.volumeLayer = LayerMask.GetMask("PostProcessing");
        layer.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;

        // Create a global PostProcessVolume
        GameObject postProcessVolumeGameObject = GameObject.Find("PostProcessVolume");
        if (postProcessVolumeGameObject == null)
        {
            postProcessVolumeGameObject = new GameObject("PostProcessVolume");
            postProcessVolumeGameObject.layer = LayerMask.NameToLayer("PostProcessing");
        }

        PostProcessVolume volume = postProcessVolumeGameObject.GetComponent<PostProcessVolume>();
        if (volume == null)
        {
            volume = postProcessVolumeGameObject.AddComponent<PostProcessVolume>();
        }
        volume.isGlobal = true;

        // Create or load the PostProcessProfile
        PostProcessProfile profile = volume.sharedProfile;
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, "Assets/Settings/PostProcessProfile.asset");
            volume.sharedProfile = profile;
        }

        // --- Configure Effects ---

        // 1. Bloom
        Bloom bloom;
        if (!profile.TryGetSettings<Bloom>(out bloom))
        {
            bloom = profile.AddSettings<Bloom>();
        }
        bloom.enabled.Override(true);
        bloom.intensity.Override(15f); // Strong bloom for glowing lights
        bloom.threshold.Override(1.2f);
        bloom.softKnee.Override(0.7f);
        bloom.diffusion.Override(7f);

        // 2. Motion Blur
        MotionBlur motionBlur;
        if (!profile.TryGetSettings<MotionBlur>(out motionBlur))
        {
            motionBlur = profile.AddSettings<MotionBlur>();
        }
        motionBlur.enabled.Override(true);
        motionBlur.shutterAngle.Override(180f); // Cinematic motion blur
        motionBlur.sampleCount.Override(12);

        // 3. Color Grading
        ColorGrading colorGrading;
        if (!profile.TryGetSettings<ColorGrading>(out colorGrading))
        {
            colorGrading = profile.AddSettings<ColorGrading>();
        }
        colorGrading.enabled.Override(true);
        colorGrading.tonemapper.Override(Tonemapper.ACES); // ACES is the industry standard for cinematic color
        colorGrading.temperature.Override(15f); // Shift towards cool blues
        colorGrading.tint.Override(5f);
        colorGrading.postExposure.Override(0.5f);
        colorGrading.contrast.Override(20f);
        colorGrading.saturation.Override(-10f); // Slightly desaturate for a grittier feel

        // 4. Vignette
        Vignette vignette;
        if (!profile.TryGetSettings<Vignette>(out vignette))
        {
            vignette = profile.AddSettings<Vignette>();
        }
        vignette.enabled.Override(true);
        vignette.intensity.Override(0.45f);
        vignette.smoothness.Override(0.4f);
        vignette.roundness.Override(1f);
        vignette.rounded.Override(true);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        Debug.Log("Post-processing setup complete!");
    }
}

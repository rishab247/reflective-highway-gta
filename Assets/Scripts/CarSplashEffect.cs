using UnityEngine;

public class CarSplashEffect : MonoBehaviour
{
    public ParticleSystem splashParticles;
    public float speedThreshold = 10f;
    public float emissionRate = 50f;
    
    private Rigidbody rb;
    private ParticleSystem.EmissionModule emission;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (splashParticles == null)
        {
            // Auto-create simple splash system if missing
            GameObject go = new GameObject("SplashParticles");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0, -0.5f, -1f); // Behind/under car
            go.transform.localRotation = Quaternion.Euler(-20, 180, 0); // Spray back and up
            
            splashParticles = go.AddComponent<ParticleSystem>();
            var main = splashParticles.main;
            main.startSpeed = 5f;
            main.startSize = 0.2f;
            main.startLifetime = 0.5f;
            main.startColor = new Color(0.8f, 0.9f, 1f, 0.4f); // Watery blue-white
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            emission = splashParticles.emission;
            emission.rateOverTime = 0; // Controlled by speed
            
            var shape = splashParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.5f;
            
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            
            // Try standard particle shader, fallback to whatever we can find
            Shader pShader = Shader.Find("Particles/Standard Unlit");
            if (pShader == null) pShader = Shader.Find("Mobile/Particles/Alpha Blended");
            if (pShader == null) pShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (pShader == null) pShader = Shader.Find("Mobile/Diffuse"); // Emergency fallback
            
            if (pShader != null) {
                renderer.material = new Material(pShader);
            }
        }
        else
        {
            emission = splashParticles.emission;
        }
    }

    void Update()
    {
        if (rb == null) return;
        
        float speed = rb.velocity.magnitude; // or carController.CurrentSpeed
        
        // Simple logic: If moving fast, emit water spray
        // In a real "puddle" system, we'd raycast down and check the texture/mask.
        // Here, we assume the whole road is "Wet" as per the visual style.
        
        if (speed > speedThreshold)
        {
            // Scale emission with speed
            float factor = (speed - speedThreshold) / 20f; // 0 to 1ish
            emission.rateOverTime = emissionRate * Mathf.Clamp01(factor);
        }
        else
        {
            emission.rateOverTime = 0f;
        }
    }
}

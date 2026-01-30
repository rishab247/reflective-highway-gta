using UnityEngine;

public class ProceduralEngineAudio : MonoBehaviour
{
    private AudioSource engineSource;
    private AudioSource roadSource;
    
    public float currentSpeed = 0f;
    public float maxSpeed = 100f;

    void Start()
    {
        // Engine Sound (Sawtooth wave synthesis approx)
        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.loop = true;
        engineSource.playOnAwake = false;
        engineSource.spatialBlend = 1.0f; // 3D sound
        engineSource.minDistance = 5f;
        
        // We need an AudioClip. Procedural usually uses OnAudioFilterRead, 
        // but creating a static clip of noise/hum is safer for WebGL/Android compatibility without complex threading.
        // Let's generate a 1-second clip of a low "purr".
        engineSource.clip = GenerateEngineTone();
        engineSource.Play();

        // Road Noise (White noise low pass)
        roadSource = gameObject.AddComponent<AudioSource>();
        roadSource.loop = true;
        roadSource.clip = GenerateWhiteNoise();
        roadSource.spatialBlend = 1.0f;
        roadSource.volume = 0f;
        roadSource.Play();
    }

    void Update()
    {
        // Pitch modulation based on speed
        float pitch = Mathf.Lerp(0.5f, 2.0f, currentSpeed / maxSpeed);
        engineSource.pitch = pitch;
        engineSource.volume = Mathf.Clamp01(0.2f + (currentSpeed / maxSpeed) * 0.5f);

        // Road noise volume based on speed
        roadSource.volume = Mathf.Clamp01((currentSpeed / maxSpeed) * 0.8f);
    }

    AudioClip GenerateEngineTone()
    {
        int sampleRate = 44100;
        int length = sampleRate; // 1 second
        float[] samples = new float[length];
        float frequency = 60f; // Low idle rumble

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            // Mix of Sawtooth (grit) and Sine (body)
            float saw = 2f * (t * frequency - Mathf.Floor(t * frequency + 0.5f));
            float sine = Mathf.Sin(2f * Mathf.PI * frequency * t);
            samples[i] = (saw * 0.4f + sine * 0.6f) * 0.5f;
        }

        AudioClip clip = AudioClip.Create("EngineTone", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip GenerateWhiteNoise()
    {
        int sampleRate = 44100;
        int length = sampleRate;
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            samples[i] = (Random.value * 2f - 1f) * 0.2f; // Low volume noise
        }

        AudioClip clip = AudioClip.Create("RoadNoise", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

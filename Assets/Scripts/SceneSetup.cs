using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SceneSetup : MonoBehaviour
{
    private string errorMessage = "None";
    private Transform mainCamera;
    private Transform playerSphere;
    
    // Endless Logic
    public float speed = 80f; // High speed driving
    private float currentZ = 0f;
    private float spawnZ = -100f; // Start generating from here
    private float segmentLength = 100f;
    private int initialSegments = 15;
    private List<GameObject> activeSegments = new List<GameObject>();
    private Transform environmentRoot;

    // Materials (Cached)
    private Material roadMat;
    private Material buildingMat;
    private Material windowMat;
    private Material lampMat;
    private Material lampEmissive;
    private Material railMat;
    private Texture2D windowTex;
    private Texture2D roadTex;

    void Awake()
    {
        Application.logMessageReceived += HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            errorMessage = logString + "\n" + stackTrace;
        }
    }

    void Start()
    {
        try {
            // Setup Environment Roots
            environmentRoot = new GameObject("Environment").transform;
            
            // visual setup
            SetupLighting();
            SetupPostProcessing();
            CreateSky();

            // Prepare Materials
            PrepareMaterials();
            
            // Player Setup
            CreateReflectiveSphere();
            SetupCamera();
            CreateReflectionProbe();

            // Initial Generation
            for (int i = 0; i < initialSegments; i++)
            {
                SpawnWorldSegment();
            }

        } catch (System.Exception e) {
            errorMessage = e.Message + "\n" + e.StackTrace;
            Debug.LogError(e);
        }
    }

    void PrepareMaterials()
    {
        // Road
        roadMat = Resources.Load<Material>("Materials/HighwayMaterial");
        roadTex = Resources.Load<Texture2D>("Textures/HighwayTex");
        if (roadMat == null) {
            roadMat = new Material(SafeShader("Standard"));
            roadMat.EnableKeyword("_NORMALMAP");
            roadMat.SetFloat("_Glossiness", 0.9f); // Wet
            roadMat.SetFloat("_Metallic", 0.0f);
            roadMat.SetColor("_Color", new Color(0.2f, 0.2f, 0.2f));
        }
        if (roadTex != null) {
            roadMat.mainTexture = roadTex;
            roadTex.wrapMode = TextureWrapMode.Repeat;
        }

        // Buildings
        windowTex = GenerateWindowTexture();
        buildingMat = new Material(SafeShader("Standard"));
        buildingMat.color = new Color(0.1f, 0.1f, 0.12f);
        buildingMat.SetFloat("_Glossiness", 0.8f);
        buildingMat.mainTexture = windowTex;
        
        windowMat = new Material(SafeShader("Standard")); // For extra glowing bits
        windowMat.EnableKeyword("_EMISSION");
        windowMat.SetColor("_EmissionColor", new Color(0.8f, 0.9f, 1f));

        // Lamps
        lampMat = new Material(SafeShader("Standard"));
        lampMat.color = new Color(0.2f, 0.2f, 0.2f);
        lampMat.SetFloat("_Glossiness", 0.5f);
        lampMat.SetFloat("_Metallic", 0.8f);

        lampEmissive = new Material(SafeShader("Standard"));
        lampEmissive.EnableKeyword("_EMISSION");
        lampEmissive.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.4f) * 2f);

        // Rails
        railMat = new Material(SafeShader("Standard"));
        railMat.color = Color.gray;
        railMat.SetFloat("_Glossiness", 0.7f);
        railMat.SetFloat("_Metallic", 0.5f);
    }

    void Update()
    {
        if (playerSphere == null) return;

        // 1. Move Player Forward
        float moveStep = speed * Time.deltaTime;
        playerSphere.Translate(Vector3.forward * moveStep);
        
        // 2. Camera Follow (Smooth)
        if (mainCamera != null) {
            Vector3 targetPos = playerSphere.position + new Vector3(0, 6, -18);
            mainCamera.position = Vector3.Lerp(mainCamera.position, targetPos, Time.deltaTime * 5f);
            mainCamera.LookAt(playerSphere.position + Vector3.up * 2f);
        }

        // 3. Endless Generation Logic
        // If player is getting close to the end of generated segments, spawn more
        // We want to keep 'initialSegments' amount ahead.
        // spawnZ is the Z coordinate of the NEXT segment to be spawned.
        // If (spawnZ - playerZ) < (segments * length) / 2 ... spawn
        
        if ((spawnZ - playerSphere.position.z) < (initialSegments * segmentLength))
        {
            SpawnWorldSegment();
            CleanupOldSegments();
        }
    }

    void SpawnWorldSegment()
    {
        // Container for this segment (easy cleanup)
        GameObject segmentRoot = new GameObject("Segment_" + spawnZ);
        segmentRoot.transform.SetParent(environmentRoot);
        activeSegments.Add(segmentRoot);

        // 1. Road
        CreateRoadChunk(segmentRoot.transform, spawnZ);
        
        // 2. Lamps (Left/Right)
        CreateLamp(new Vector3(-50f, 0, spawnZ), 90f, segmentRoot.transform);
        CreateLamp(new Vector3(50f, 0, spawnZ), -90f, segmentRoot.transform);

        // 3. Guard Rails
        CreateRailChunk(segmentRoot.transform, spawnZ);

        // 4. Buildings (Randomly sparse or dense)
        if (Random.value > 0.3f) CreateBuilding(new Vector3(-160f - Random.value * 50f, 0, spawnZ), segmentRoot.transform);
        if (Random.value > 0.3f) CreateBuilding(new Vector3(160f + Random.value * 50f, 0, spawnZ), segmentRoot.transform);

        // Advance Z
        spawnZ += segmentLength;
    }

    void CleanupOldSegments()
    {
        // If we have too many, remove the oldest (index 0)
        // Keep a buffer behind the player.
        // If the oldest segment Z is far behind player...
        
        if (activeSegments.Count > 0)
        {
            GameObject oldest = activeSegments[0];
            // Name format "Segment_100", but we can just check distance logic or list size
            // Lets just keep fixed list size for simplicity + buffer
            if (activeSegments.Count > initialSegments + 5) 
            {
                activeSegments.RemoveAt(0);
                Destroy(oldest);
            }
        }
    }

    // --- Object Creation Helpers ---

    void CreateRoadChunk(Transform parent, float zPos)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Plane);
        road.transform.SetParent(parent);
        road.transform.localScale = new Vector3(15, 1, 10); // 150m wide, 100m long
        road.transform.position = new Vector3(0, 0, zPos); // Centered on zPos? No, plane center is local 0.
        // To make segments tile perfectly:
        // Plane length 10 * 10 = 100. Center is at 0. Extends -50 to +50.
        // If zPos is 0, it covers -50 to 50. Next zPos 100 covers 50 to 150. Correct.
        
        Renderer r = road.GetComponent<Renderer>();
        r.material = roadMat;
        r.material.mainTextureScale = new Vector2(1, 5); // Tile texture
    }

    void CreateRailChunk(Transform parent, float zPos)
    {
        // Left
        GameObject l = GameObject.CreatePrimitive(PrimitiveType.Cube);
        l.transform.SetParent(parent);
        l.transform.position = new Vector3(-60f, 2f, zPos);
        l.transform.localScale = new Vector3(0.5f, 0.5f, segmentLength);
        l.GetComponent<Renderer>().material = railMat;

        // Right
        GameObject r = GameObject.CreatePrimitive(PrimitiveType.Cube);
        r.transform.SetParent(parent);
        r.transform.position = new Vector3(60f, 2f, zPos);
        r.transform.localScale = new Vector3(0.5f, 0.5f, segmentLength);
        r.GetComponent<Renderer>().material = railMat;
    }

    void CreateLamp(Vector3 pos, float rotY, Transform parent)
    {
        // Simple composite lamp
        GameObject lamp = new GameObject("Lamp");
        lamp.transform.SetParent(parent);
        lamp.transform.position = pos;
        lamp.transform.rotation = Quaternion.Euler(0, rotY, 0);

        // Pole
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(lamp.transform);
        pole.transform.localPosition = new Vector3(0, 7.5f, 0);
        pole.transform.localScale = new Vector3(0.8f, 7.5f, 0.8f);
        pole.GetComponent<Renderer>().material = lampMat;

        // Arm
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.transform.SetParent(lamp.transform);
        arm.transform.localPosition = new Vector3(3f, 14.5f, 0);
        arm.transform.localScale = new Vector3(6f, 0.5f, 0.5f);
        arm.GetComponent<Renderer>().material = lampMat;

        // Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(lamp.transform);
        head.transform.localPosition = new Vector3(5.5f, 14.2f, 0);
        head.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
        head.GetComponent<Renderer>().material = lampEmissive;

        // Light (Important! But expensive. Enable randomly or optimize?)
        // Lets keep it for every lamp for now, but reduce range/shadows for performance
        GameObject lightObj = new GameObject("Spot");
        lightObj.transform.SetParent(head.transform);
        lightObj.transform.localPosition = new Vector3(0, -0.5f, 0);
        lightObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Spot;
        l.range = 60f;
        l.spotAngle = 60f;
        l.intensity = 2.0f;
        l.color = new Color(1f, 0.85f, 0.6f);
        l.shadows = LightShadows.Hard; // Hard shadows are cheaper than soft
    }

    void CreateBuilding(Vector3 pos, Transform parent)
    {
        float height = 50f + Random.value * 200f;
        float width = 40f + Random.value * 40f;
        float depth = 40f + Random.value * 40f;

        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.transform.SetParent(parent);
        b.transform.position = pos + new Vector3(0, height/2, 0);
        b.transform.localScale = new Vector3(width, height, depth);
        
        Renderer r = b.GetComponent<Renderer>();
        r.material = buildingMat;
        // Tile texture to keep windows consistent size
        r.material.mainTextureScale = new Vector2(width/10f, height/20f);
        r.material.SetTexture("_EmissionMap", windowTex);
    }

    Texture2D GenerateWindowTexture()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        Color darkGlass = new Color(0.05f, 0.05f, 0.1f);
        Color litRoom = new Color(1.0f, 0.9f, 0.6f);

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                bool isFrame = (x % 32 < 4) || (y % 64 < 8);
                if (isFrame) colors[y*size + x] = Color.black;
                else {
                    float noise = Mathf.PerlinNoise(Mathf.Floor(x/32f)*0.5f, Mathf.Floor(y/64f)*0.5f);
                    colors[y*size + x] = (noise > 0.4f) ? litRoom : darkGlass;
                }
            }
        }
        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    void CreateReflectiveSphere()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Player";
        sphere.transform.localScale = new Vector3(6, 6, 6);
        sphere.transform.position = new Vector3(0, 3f, 0);
        playerSphere = sphere.transform;

        // Make it shiny
        Renderer r = sphere.GetComponent<Renderer>();
        Material mat = new Material(SafeShader("Standard"));
        mat.SetFloat("_Glossiness", 0.95f);
        mat.SetFloat("_Metallic", 1.0f);
        r.material = mat;
    }

    void CreateReflectionProbe()
    {
        GameObject probeObj = new GameObject("Probe");
        probeObj.transform.SetParent(playerSphere);
        probeObj.transform.localPosition = new Vector3(0, 2, 0);
        
        ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
        probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.size = new Vector3(300, 300, 300);
        probe.resolution = 64; // Low res for mobile performance
    }

    void SetupCamera()
    {
        GameObject camObj = GameObject.FindWithTag("MainCamera");
        if (camObj == null) {
            camObj = new GameObject("Main Camera");
            camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        mainCamera = camObj.transform;
        
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.farClipPlane = 2000f;
        cam.fieldOfView = 60f;
        cam.allowHDR = true;
    }

    void SetupLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.05f, 0.05f, 0.1f);
        RenderSettings.ambientGroundColor = Color.black;
        
        GameObject light = new GameObject("Moon");
        Light l = light.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 0.3f;
        l.color = new Color(0.2f, 0.3f, 0.5f);
        light.transform.rotation = Quaternion.Euler(45, -30, 0);
    }
    
    void SetupPostProcessing()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.002f;
        RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.02f);
    }

    void CreateSky()
    {
        Texture2D tex = Resources.Load<Texture2D>("Textures/SkyTex");
        if (tex == null) return;
        Shader s = Shader.Find("Skybox/Panoramic");
        if (s != null) {
            Material m = new Material(s);
            m.SetTexture("_MainTex", tex);
            RenderSettings.skybox = m;
        }
    }

    private Shader SafeShader(string name) {
        Shader s = Shader.Find(name);
        if (s == null) s = Shader.Find("Standard");
        return s;
    }
    
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.cyan;
        float distance = playerSphere != null ? playerSphere.position.z : 0;
        GUI.Label(new Rect(20, 20, 400, 40), "Distance: " + (int)distance + "m", style);
        
        if (errorMessage != "None") {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(20, 70, Screen.width, Screen.height), errorMessage, style);
        }
    }
}

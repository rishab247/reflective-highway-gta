using UnityEngine;
using System.Collections.Generic;

public class SceneSetup : MonoBehaviour
{
    private string errorMessage = "None";
    private Transform mainCamera;
    private Transform playerCar; // Changed from sphere
    private float currentSpeed = 0f;
    private float maxSpeed = 80f; // Target speed
    private float acceleration = 20f;
    private float steerSpeed = 30f;
    
    // Endless Logic
    private float spawnZ = -100f; 
    private float segmentLength = 100f;
    private int initialSegments = 20;
    private List<GameObject> activeSegments = new List<GameObject>();
    private Transform environmentRoot;

    // Materials
    private Material roadMat;
    private Material buildingMat;
    private Material windowMat;
    private Material carBodyMat;
    private Material carGlassMat;
    private Material tailLightMat;
    private Material headLightMat;
    private Material railMat;
    private Material lampMat;
    private Material lampEmissive;
    
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
            environmentRoot = new GameObject("Environment").transform;
            
            SetupLighting();
            CreateSky();
            PrepareMaterials(); // Generates High-Res Road
            
            CreatePlayerCar();
            SetupCamera(); // Adds Bloom
            
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
        // 1. HIGH QUALITY ROAD TEXTURE GENERATION
        roadTex = GenerateAsphaltTexture();
        roadMat = new Material(SafeShader("Standard"));
        roadMat.name = "RealAsphalt";
        roadMat.mainTexture = roadTex;
        roadMat.EnableKeyword("_NORMALMAP");
        // Create normal map from diffuse? Hard in runtime. We simulate depth via smoothness.
        roadMat.SetFloat("_Glossiness", 0.92f); // Wet look
        roadMat.SetFloat("_Metallic", 0.0f);
        roadMat.SetColor("_Color", new Color(0.15f, 0.15f, 0.15f)); // Dark Grey

        // Buildings
        windowTex = GenerateWindowTexture();
        buildingMat = new Material(SafeShader("Standard"));
        buildingMat.color = new Color(0.08f, 0.08f, 0.1f);
        buildingMat.SetFloat("_Glossiness", 0.8f);
        buildingMat.mainTexture = windowTex;
        
        windowMat = new Material(SafeShader("Standard")); 
        windowMat.EnableKeyword("_EMISSION");
        windowMat.SetColor("_EmissionColor", new Color(0.7f, 0.9f, 1f) * 1.5f); // Bright Bloom-ready windows

        // Car Materials
        carBodyMat = new Material(SafeShader("Standard"));
        carBodyMat.color = new Color(0.8f, 0.8f, 0.9f); // Silver Chrome
        carBodyMat.SetFloat("_Glossiness", 0.95f);
        carBodyMat.SetFloat("_Metallic", 1.0f);
        
        carGlassMat = new Material(SafeShader("Standard"));
        carGlassMat.color = new Color(0, 0, 0, 0.8f);
        carGlassMat.SetFloat("_Glossiness", 1.0f);
        carGlassMat.SetColor("_EmissionColor", new Color(0.1f, 0.1f, 0.15f)); // Slight reflection hint

        tailLightMat = new Material(SafeShader("Standard"));
        tailLightMat.EnableKeyword("_EMISSION");
        tailLightMat.SetColor("_EmissionColor", new Color(1f, 0, 0) * 3f); // SUPER BRIGHT RED

        headLightMat = new Material(SafeShader("Standard"));
        headLightMat.EnableKeyword("_EMISSION");
        headLightMat.SetColor("_EmissionColor", new Color(0.9f, 0.9f, 1f) * 3f); // SUPER BRIGHT WHITE

        // Lamps & Rails
        lampMat = new Material(SafeShader("Standard"));
        lampMat.color = Color.black;
        lampMat.SetFloat("_Glossiness", 0.6f);
        
        lampEmissive = new Material(SafeShader("Standard"));
        lampEmissive.EnableKeyword("_EMISSION");
        lampEmissive.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.2f) * 2f); // Sodium Vapor Orange

        railMat = new Material(SafeShader("Standard"));
        railMat.color = new Color(0.5f, 0.5f, 0.5f);
        railMat.SetFloat("_Glossiness", 0.7f);
        railMat.SetFloat("_Metallic", 0.6f);
    }

    void Update()
    {
        if (playerCar == null) return;

        // Acceleration
        currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.deltaTime * 0.5f);

        // Steering (Touch or Mouse)
        float steer = 0f;
        if (Input.GetMouseButton(0))
        {
            // Center is screen width / 2
            float normX = (Input.mousePosition.x / Screen.width) * 2f - 1f; // -1 to 1
            steer = normX * steerSpeed;
        }
        
        // Mobile Tilt Override
        if (Input.acceleration.x != 0) {
            steer = Input.acceleration.x * steerSpeed * 2f;
        }

        Vector3 moveVec = new Vector3(steer, 0, currentSpeed);
        playerCar.Translate(moveVec * Time.deltaTime);

        // Clamp X position to road (Road width ~30, so clamp -14 to 14)
        Vector3 pos = playerCar.position;
        pos.x = Mathf.Clamp(pos.x, -14f, 14f);
        playerCar.position = pos;

        // Camera Chase
        if (mainCamera != null) {
            Vector3 targetPos = playerCar.position + new Vector3(0, 3.5f, -8f); // Closer, lower chase
            // Add some "Speed Lag"
            targetPos.z -= (currentSpeed / maxSpeed) * 2f; 
            
            mainCamera.position = Vector3.Lerp(mainCamera.position, targetPos, Time.deltaTime * 8f);
            mainCamera.LookAt(playerCar.position + Vector3.up * 1.5f);
        }

        // Endless Gen
        if ((spawnZ - playerCar.position.z) < (initialSegments * segmentLength))
        {
            SpawnWorldSegment();
            CleanupOldSegments();
        }
    }

    // --- Texture Gen ---

    Texture2D GenerateAsphaltTexture()
    {
        int w = 512;
        int h = 512;
        Texture2D tex = new Texture2D(w, h);
        Color[] cols = new Color[w * h];
        Color baseAsphalt = new Color(0.15f, 0.15f, 0.15f);
        Color lineWhite = new Color(0.8f, 0.8f, 0.8f);
        Color lineYellow = new Color(0.8f, 0.7f, 0.1f);

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                // Noise for grain
                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.1f;
                Color c = baseAsphalt + new Color(noise, noise, noise);

                // Lane Lines
                // Center line (Double Yellow)
                float u = x / (float)w; // 0 to 1
                
                // Middle is 0.5.
                if (Mathf.Abs(u - 0.5f) < 0.01f) c = baseAsphalt; // Gap
                else if (Mathf.Abs(u - 0.48f) < 0.015f) c = lineYellow; // Left Yellow
                else if (Mathf.Abs(u - 0.52f) < 0.015f) c = lineYellow; // Right Yellow
                
                // Side lines (White dashed)
                // Left Lane divider ~ 0.25
                if (Mathf.Abs(u - 0.25f) < 0.02f) {
                    if ((y / 64) % 2 == 0) c = lineWhite; // Dash
                }
                // Right Lane divider ~ 0.75
                if (Mathf.Abs(u - 0.75f) < 0.02f) {
                    if ((y / 64) % 2 == 0) c = lineWhite; // Dash
                }

                cols[y*w + x] = c;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    Texture2D GenerateWindowTexture()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        Color darkGlass = new Color(0.02f, 0.02f, 0.05f);
        Color litRoom = new Color(1.0f, 0.95f, 0.8f);

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                bool isFrame = (x % 32 < 2) || (y % 64 < 4);
                if (isFrame) colors[y*size + x] = Color.black;
                else {
                    float noise = Mathf.PerlinNoise(Mathf.Floor(x/32f)*0.8f, Mathf.Floor(y/64f)*0.8f);
                    colors[y*size + x] = (noise > 0.5f) ? litRoom : darkGlass;
                }
            }
        }
        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    // --- Car Construction ---

    void CreatePlayerCar()
    {
        // Build a sleek Cyber-Sport Car
        playerCar = new GameObject("PlayerCar").transform;
        playerCar.position = new Vector3(0, 0.5f, 0);

        // Chassis
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(playerCar);
        body.transform.localPosition = new Vector3(0, 0.6f, 0);
        body.transform.localScale = new Vector3(2.2f, 0.8f, 5.0f);
        body.GetComponent<Renderer>().material = carBodyMat;

        // Cabin
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(playerCar);
        cabin.transform.localPosition = new Vector3(0, 1.2f, -0.5f);
        cabin.transform.localScale = new Vector3(1.8f, 0.7f, 2.5f);
        cabin.GetComponent<Renderer>().material = carGlassMat;

        // Wheels (4)
        CreateWheel(new Vector3(-1.1f, 0.4f, 1.5f));
        CreateWheel(new Vector3(1.1f, 0.4f, 1.5f));
        CreateWheel(new Vector3(-1.1f, 0.4f, -1.5f));
        CreateWheel(new Vector3(1.1f, 0.4f, -1.5f));

        // Lights
        CreateHeadlight(new Vector3(-0.8f, 0.8f, 2.5f));
        CreateHeadlight(new Vector3(0.8f, 0.8f, 2.5f));
        CreateTaillight(new Vector3(-0.8f, 0.8f, -2.5f));
        CreateTaillight(new Vector3(0.8f, 0.8f, -2.5f));
        
        // Underglow (Neon)
        GameObject glow = new GameObject("Underglow");
        glow.transform.SetParent(playerCar);
        glow.transform.localPosition = new Vector3(0, 0.1f, 0);
        Light l = glow.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 8f;
        l.intensity = 2f;
        l.color = Color.cyan;
    }

    void CreateWheel(Vector3 pos)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        w.transform.SetParent(playerCar);
        w.transform.localPosition = pos;
        w.transform.localRotation = Quaternion.Euler(0, 0, 90);
        w.transform.localScale = new Vector3(0.8f, 0.2f, 0.8f); // Flatten cylinder
        w.GetComponent<Renderer>().material = new Material(SafeShader("Standard")) { color = Color.black };
    }

    void CreateHeadlight(Vector3 pos)
    {
        // Mesh
        GameObject hl = GameObject.CreatePrimitive(PrimitiveType.Quad);
        hl.transform.SetParent(playerCar);
        hl.transform.localPosition = pos;
        hl.transform.localRotation = Quaternion.Euler(0, 0, 0); // Face forward? Quad faces -Z by default? No +Z.
        hl.transform.localScale = new Vector3(0.5f, 0.2f, 1f);
        hl.GetComponent<Renderer>().material = headLightMat;

        // Beam
        GameObject beam = new GameObject("Beam");
        beam.transform.SetParent(hl.transform);
        beam.transform.localPosition = Vector3.zero;
        Light l = beam.AddComponent<Light>();
        l.type = LightType.Spot;
        l.range = 100f;
        l.spotAngle = 45f;
        l.intensity = 5f;
        l.color = new Color(0.9f, 0.9f, 1f);
    }

    void CreateTaillight(Vector3 pos)
    {
        GameObject tl = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tl.transform.SetParent(playerCar);
        tl.transform.localPosition = pos;
        tl.transform.localRotation = Quaternion.Euler(0, 180, 0); // Face backward
        tl.transform.localScale = new Vector3(0.5f, 0.2f, 1f);
        tl.GetComponent<Renderer>().material = tailLightMat;

        // Glow
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(tl.transform);
        Light l = glow.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 2f;
        l.intensity = 2f;
        l.color = Color.red;
    }

    // --- World Gen ---

    void SpawnWorldSegment()
    {
        GameObject root = new GameObject("Segment_" + spawnZ);
        root.transform.SetParent(environmentRoot);
        activeSegments.Add(root);

        // Road
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Plane);
        road.transform.SetParent(root.transform);
        road.transform.localScale = new Vector3(3f, 1, 10f); // 30m wide, 100m long (Standard Highway)
        road.transform.position = new Vector3(0, 0, spawnZ);
        Renderer r = road.GetComponent<Renderer>();
        r.material = roadMat;
        r.material.mainTextureScale = new Vector2(1, 5); // Tiling

        // Lamps
        CreateLamp(new Vector3(-16f, 0, spawnZ), 90f, root.transform);
        CreateLamp(new Vector3(16f, 0, spawnZ), -90f, root.transform);
        
        // Rails
        CreateRail(new Vector3(-18f, 0, spawnZ), root.transform);
        CreateRail(new Vector3(18f, 0, spawnZ), root.transform);

        // Buildings
        if (Random.value > 0.2f) CreateBuilding(new Vector3(-40f - Random.value * 40f, 0, spawnZ), root.transform);
        if (Random.value > 0.2f) CreateBuilding(new Vector3(40f + Random.value * 40f, 0, spawnZ), root.transform);

        spawnZ += segmentLength;
    }
    
    void CreateRail(Vector3 pos, Transform parent)
    {
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.transform.SetParent(parent);
        rail.transform.position = pos + Vector3.up * 1f;
        rail.transform.localScale = new Vector3(0.5f, 1f, segmentLength);
        rail.GetComponent<Renderer>().material = railMat;
    }

    void CreateLamp(Vector3 pos, float rotY, Transform parent)
    {
        GameObject lamp = new GameObject("Lamp");
        lamp.transform.SetParent(parent);
        lamp.transform.position = pos;
        lamp.transform.rotation = Quaternion.Euler(0, rotY, 0);

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(lamp.transform);
        pole.transform.localPosition = new Vector3(0, 6f, 0);
        pole.transform.localScale = new Vector3(0.5f, 6f, 0.5f);
        pole.GetComponent<Renderer>().material = lampMat;

        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.transform.SetParent(lamp.transform);
        arm.transform.localPosition = new Vector3(2f, 11.5f, 0);
        arm.transform.localScale = new Vector3(4f, 0.3f, 0.3f);
        arm.GetComponent<Renderer>().material = lampMat;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(lamp.transform);
        head.transform.localPosition = new Vector3(3.8f, 11.3f, 0);
        head.GetComponent<Renderer>().material = lampEmissive;
    }

    void CreateBuilding(Vector3 pos, Transform parent)
    {
        float h = 40f + Random.value * 150f;
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.transform.SetParent(parent);
        b.transform.position = pos + new Vector3(0, h/2, 0);
        b.transform.localScale = new Vector3(30f, h, 30f);
        Renderer r = b.GetComponent<Renderer>();
        r.material = buildingMat;
        r.material.mainTextureScale = new Vector2(3, h/10f);
        r.material.SetTexture("_EmissionMap", windowTex);
    }
    
    void CleanupOldSegments()
    {
        if (activeSegments.Count > initialSegments + 4) {
            GameObject old = activeSegments[0];
            activeSegments.RemoveAt(0);
            Destroy(old);
        }
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
        cam.farClipPlane = 1500f;
        cam.allowHDR = true; // Essential for Bloom
        
        // Attach Bloom Script
        if (camObj.GetComponent<SimpleBloomEffect>() == null)
            camObj.AddComponent<SimpleBloomEffect>();
    }

    void SetupLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.02f, 0.02f, 0.1f);
        RenderSettings.ambientEquatorColor = new Color(0.05f, 0.05f, 0.1f);
        RenderSettings.ambientGroundColor = Color.black;
        RenderSettings.fog = true;
        RenderSettings.fogDensity = 0.003f;
        RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.02f);
    }

    void CreateSky()
    {
        Texture2D tex = Resources.Load<Texture2D>("Textures/SkyTex");
        if (tex != null) {
            Shader s = Shader.Find("Skybox/Panoramic");
            if (s != null) {
                Material m = new Material(s);
                m.SetTexture("_MainTex", tex);
                RenderSettings.skybox = m;
            }
        }
    }

    private Shader SafeShader(string name) {
        Shader s = Shader.Find(name);
        if (s == null) s = Shader.Find("Standard");
        return s;
    }
}

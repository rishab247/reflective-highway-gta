using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SceneSetup : MonoBehaviour
{
    private string errorMessage = "None";
    private Transform cameraOrbitRoot;
    private Transform mainCamera;
    private Transform playerSphere;
    private float orbitAngle = 0f;
    private Vector2 lastMousePos;

    void Awake()
    {
        Application.logMessageReceived += HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            errorMessage = logString + "\n" + stackTrace;
            try { GUIUtility.systemCopyBuffer = "ERROR: " + logString + "\nSTACK: " + stackTrace; } catch {}
        }
    }

    void Start()
    {
        try {
            SetupLighting(); // Basic lighting first
            SetupPostProcessing(); // Cinematic Overrides
            CreateReflectiveSphere();
            SetupCamera();
            CreateSky();
            // CreateCityscape(); // REMOVED: User wants real buildings
            CreateCityBuildings(); // NEW: Real 3D buildings
            CreateHighway();
            CreateStreetLamps(); // New: Procedural Lamps
            CreateGuardRails(); // New: Procedural Rails
        } catch (System.Exception e) {
            string fullErr = "CATCH: " + e.Message + "\n" + e.StackTrace;
            errorMessage = fullErr;
            Debug.LogError(e);
            try { GUIUtility.systemCopyBuffer = fullErr; } catch {}
        }
    }

    void Update()
    {
        HandleCameraOrbit();
    }

    void HandleCameraOrbit()
    {
        if (cameraOrbitRoot == null) return;

        // Simple touch/mouse orbit
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            float deltaX = Input.mousePosition.x - lastMousePos.x;
            orbitAngle += deltaX * 0.2f;
            lastMousePos = Input.mousePosition;
        }

        // Apply orbit
        cameraOrbitRoot.localRotation = Quaternion.Euler(0, orbitAngle, 0);
        
        // Keep camera pointing at sphere
        if (playerSphere != null && mainCamera != null)
        {
            mainCamera.LookAt(playerSphere.position + Vector3.up * 0.5f);
        }
    }

    void SetupCamera()
    {
        GameObject root = new GameObject("CameraOrbitRoot");
        cameraOrbitRoot = root.transform;
        if (playerSphere != null) cameraOrbitRoot.position = playerSphere.position;

        GameObject camObj = GameObject.FindWithTag("MainCamera");
        if (camObj == null) {
            camObj = new GameObject("Main Camera");
            camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        mainCamera = camObj.transform;
        mainCamera.SetParent(cameraOrbitRoot);
        mainCamera.localPosition = new Vector3(0, 5, -20);
        
        Camera cam = camObj.GetComponent<Camera>();
        // Set to Skybox mode to see the panoramic sky
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.farClipPlane = 10000f;
        cam.fieldOfView = 60f;
        cam.allowHDR = true; // Essential for cinematic feel
    }

    void SetupLighting()
    {
        GameObject light = new GameObject("Directional Light");
        Light l = light.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.0f; 
        l.color = new Color(1.0f, 0.95f, 0.8f); 
        light.transform.rotation = Quaternion.Euler(15, -160, 0); 
    }

    void SetupPostProcessing()
    {
        // Simulate Cinematic Look without PostProcessing Stack
        
        // 1. Atmosphere / Fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0015f;
        RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.05f); // Deep Night Blue

        // 2. Ambient Light (Moonlight feel)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.1f);

        // 3. Adjust Main Light to look like Moon
        GameObject dirLight = GameObject.Find("Directional Light");
        if (dirLight) {
            Light l = dirLight.GetComponent<Light>();
            l.color = new Color(0.7f, 0.8f, 1.0f); // Cool blue-white
            l.intensity = 0.4f; // Dimmer, let streetlights dominate
            l.shadows = LightShadows.Soft;
        }
    }

    private Shader SafeShader(string name) {
        Shader s = Shader.Find(name);
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Diffuse");
        if (s == null) s = Shader.Find("Unlit/Color");
        if (s == null) s = Shader.Find("Sprites/Default");
        return s;
    }

    void CreateSky()
    {
        Texture2D tex = Resources.Load<Texture2D>("Textures/SkyTex");
        if (tex == null) return;

        // Try to use a Skybox material with Panoramic shader
        Shader skyShader = Shader.Find("Skybox/Panoramic");
        if (skyShader == null) skyShader = SafeShader("Unlit/Texture");

        Material skyMat = new Material(skyShader);
        if (skyShader.name.Contains("Panoramic")) {
            skyMat.SetTexture("_MainTex", tex);
            skyMat.SetFloat("_Exposure", 1.0f);
            skyMat.SetFloat("_Rotation", 0f);
            RenderSettings.skybox = skyMat;
        } else {
            // Fallback skysphere if shader missing
            GameObject sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sky.name = "SkySphere";
            sky.transform.position = Vector3.zero;
            sky.transform.localScale = new Vector3(-8000, -8000, -8000);
            Destroy(sky.GetComponent<SphereCollider>());
            Renderer r = sky.GetComponent<Renderer>();
            r.material = skyMat;
            r.material.mainTexture = tex;
        }
        
        // Ensure lighting is updated
        DynamicGI.UpdateEnvironment();
    }

    void CreateCityscape()
    {
        GameObject city = GameObject.CreatePrimitive(PrimitiveType.Quad);
        city.name = "Cityscape";
        city.transform.position = new Vector3(0, 100, 2500); 
        city.transform.localScale = new Vector3(5000, 800, 1);

        Renderer r = city.GetComponent<Renderer>();
        // Use transparent shader for outline
        Material mat = new Material(SafeShader("Unlit/Transparent"));
        
        Texture2D tex = Resources.Load<Texture2D>("Textures/CityOutline");
        if (tex != null) {
            mat.mainTexture = tex;
            mat.mainTextureScale = new Vector2(4, 1); 
        }
        r.material = mat;
    }

    void CreateCityBuildings()
    {
        GameObject cityRoot = new GameObject("CityBuildings");
        float startZ = -500f;
        float endZ = 3500f;
        float spacing = 80f;

        Material buildingMat = new Material(SafeShader("Standard"));
        buildingMat.color = new Color(0.1f, 0.1f, 0.12f);
        buildingMat.SetFloat("_Glossiness", 0.4f);
        buildingMat.SetFloat("_Metallic", 0.5f);

        Material windowMat = new Material(SafeShader("Standard"));
        windowMat.EnableKeyword("_EMISSION");
        windowMat.SetColor("_EmissionColor", new Color(0.8f, 0.8f, 1f) * 0.5f);

        System.Random rnd = new System.Random(42);

        for (float z = startZ; z < endZ; z += spacing)
        {
            // Left Side Buildings
            SpawnBuildingBlock(new Vector3(-150f - (float)rnd.NextDouble() * 50f, 0, z), cityRoot.transform, buildingMat, windowMat, rnd);
            // Right Side Buildings
            SpawnBuildingBlock(new Vector3(150f + (float)rnd.NextDouble() * 50f, 0, z), cityRoot.transform, buildingMat, windowMat, rnd);
        }
    }

    void SpawnBuildingBlock(Vector3 pos, Transform parent, Material baseMat, Material winMat, System.Random rnd)
    {
        float height = 50f + (float)rnd.NextDouble() * 150f;
        float width = 40f + (float)rnd.NextDouble() * 30f;
        float depth = 40f + (float)rnd.NextDouble() * 30f;

        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = "Building";
        b.transform.SetParent(parent);
        b.transform.position = pos + new Vector3(0, height / 2, 0);
        b.transform.localScale = new Vector3(width, height, depth);
        b.GetComponent<Renderer>().material = baseMat;

        // Add some random "window" highlights
        if (rnd.NextDouble() > 0.3) {
            GameObject windows = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windows.transform.SetParent(b.transform);
            windows.transform.localPosition = new Vector3(0.51f, 0, 0); // Slightly offset to face the road
            windows.transform.localScale = new Vector3(0.1f, 0.8f, 0.8f);
            windows.GetComponent<Renderer>().material = winMat;
            Destroy(windows.GetComponent<BoxCollider>());
        }
    }

    void CreateHighway()
    {
        // PHASE 1: Segmented Road System for better lighting and fog
        GameObject roadRoot = new GameObject("HighwaySegments");
        
        Material mat = Resources.Load<Material>("Materials/HighwayMaterial");
        Texture2D tex = Resources.Load<Texture2D>("Textures/HighwayTex");
        
        if (mat == null) {
            mat = new Material(SafeShader("Standard"));
        }
        
        // PBR Setup for "Real Asphalt"
        mat.EnableKeyword("_NORMALMAP");
        mat.EnableKeyword("_METALLICGLOSSMAP");
        mat.SetFloat("_Glossiness", 0.85f); // High smoothness for "wet" look
        mat.SetFloat("_Metallic", 0.0f);    // Asphalt is non-metal
        mat.SetColor("_Color", new Color(0.2f, 0.2f, 0.2f)); // Darker asphalt
        
        if (tex != null) {
            tex.anisoLevel = 16;
            tex.filterMode = FilterMode.Trilinear;
            mat.mainTexture = tex;
        }

        // Generate 40 segments (40 * 100m = 4000m total length)
        for (int i = 0; i < 40; i++)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Plane);
            segment.name = "RoadSegment_" + i;
            segment.transform.SetParent(roadRoot.transform);
            
            // Plane is 10x10. We want 150m wide, 100m long.
            // Scale X = 15. Scale Z = 10.
            segment.transform.localScale = new Vector3(15, 1, 10);
            
            // Position: Center is 0. Start at -500. i*100 offset.
            // Z = -500 + (i * 100) + 50 (half length offset)
            float zPos = -500f + (i * 100f) + 50f;
            segment.transform.position = new Vector3(0, 0, zPos);
            
            Renderer r = segment.GetComponent<Renderer>();
            r.material = mat;
            // Tiling: 1 across, 5 down per segment (for 100m)
            r.material.mainTextureScale = new Vector2(1, 5);
        }
    }

    void CreateReflectiveSphere()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "ReflectiveSphere";
        sphere.transform.localScale = new Vector3(6, 6, 6);
        sphere.transform.position = new Vector3(0, 3f, 0);
        playerSphere = sphere.transform;

        Renderer r = sphere.GetComponent<Renderer>();
        Material mat = Resources.Load<Material>("Materials/SphereMaterial");
        Texture2D tex = Resources.Load<Texture2D>("Textures/SphereTex");

        if (mat == null) {
            mat = new Material(SafeShader("Standard"));
            mat.SetFloat("_Glossiness", 0.95f);
            mat.SetFloat("_Metallic", 1.0f); // Chrome
        }
        
        if (tex != null) mat.mainTexture = tex;
        r.material = mat;
    }
    
    // --- Phase 3: Cinematic Overhaul Procedures ---

    void CreateStreetLamps()
    {
        GameObject lampRoot = new GameObject("StreetLamps");
        // Highway runs from approx Z = -500 to +3500 (Center 1500, Scale 400->4000 length)
        // Center is 1500. Extent is 2000. Start = 1500 - 2000 = -500. End = 3500.
        float startZ = -400f;
        float endZ = 3400f;
        float spacing = 120f; // Distance between lamps

        // Create a shared material for poles
        Material metalMat = new Material(SafeShader("Standard"));
        metalMat.color = new Color(0.2f, 0.2f, 0.2f);
        metalMat.SetFloat("_Glossiness", 0.5f);
        metalMat.SetFloat("_Metallic", 0.8f);

        for (float z = startZ; z < endZ; z += spacing)
        {
            // Left Lamp
            CreateSingleLamp(new Vector3(-50f, 0, z), 90f, lampRoot.transform, metalMat);
            // Right Lamp
            CreateSingleLamp(new Vector3(50f, 0, z), -90f, lampRoot.transform, metalMat);
        }
    }

    void CreateSingleLamp(Vector3 pos, float rotY, Transform parent, Material mat)
    {
        GameObject lamp = new GameObject("Lamp");
        lamp.transform.position = pos;
        lamp.transform.rotation = Quaternion.Euler(0, rotY, 0);
        lamp.transform.SetParent(parent);

        // Pole
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(lamp.transform);
        pole.transform.localPosition = new Vector3(0, 7.5f, 0); 
        pole.transform.localScale = new Vector3(0.8f, 7.5f, 0.8f);
        pole.GetComponent<Renderer>().material = mat;
        
        // Arm
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.transform.SetParent(lamp.transform);
        arm.transform.localPosition = new Vector3(3f, 14.5f, 0);
        arm.transform.localScale = new Vector3(6f, 0.5f, 0.5f);
        arm.GetComponent<Renderer>().material = mat;

        // Lamp Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(lamp.transform);
        head.transform.localPosition = new Vector3(5.5f, 14.2f, 0);
        head.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
        // Emissive material for head
        Material emissive = new Material(SafeShader("Standard"));
        emissive.EnableKeyword("_EMISSION");
        emissive.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.4f) * 2f);
        head.GetComponent<Renderer>().material = emissive;

        // Light Source
        GameObject lightObj = new GameObject("StreetLightSpot");
        lightObj.transform.SetParent(head.transform);
        lightObj.transform.localPosition = new Vector3(0, -0.5f, 0);
        lightObj.transform.localRotation = Quaternion.Euler(90, 0, 0); // Point down
        
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Spot;
        l.range = 80f;
        l.spotAngle = 70f;
        l.intensity = 3.0f;
        l.color = new Color(1f, 0.85f, 0.6f); // Warm sodium vapor
        l.shadows = LightShadows.Hard; // Dramatic shadows
    }

    void CreateGuardRails()
    {
        GameObject railRoot = new GameObject("GuardRails");
        float startZ = -500f;
        float endZ = 3500f;
        float length = endZ - startZ;
        Vector3 centerPos = new Vector3(0, 1.5f, startZ + length/2f);

        Material railMat = new Material(SafeShader("Standard"));
        railMat.color = Color.gray;
        railMat.SetFloat("_Glossiness", 0.7f);
        railMat.SetFloat("_Metallic", 0.5f);

        // We use stretched cubes for rails for performance instead of thousands of small posts
        
        // Left Rail Top
        GameObject leftTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftTop.name = "LeftRailTop";
        leftTop.transform.SetParent(railRoot.transform);
        leftTop.transform.position = new Vector3(-60f, 2f, centerPos.z);
        leftTop.transform.localScale = new Vector3(0.5f, 0.5f, length);
        leftTop.GetComponent<Renderer>().material = railMat;

        // Left Rail Posts (Visual only, spread out)
        // ... omitted for simplicity, the rail strip is enough for the look at speed

        // Right Rail Top
        GameObject rightTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightTop.name = "RightRailTop";
        rightTop.transform.SetParent(railRoot.transform);
        rightTop.transform.position = new Vector3(60f, 2f, centerPos.z);
        rightTop.transform.localScale = new Vector3(0.5f, 0.5f, length);
        rightTop.GetComponent<Renderer>().material = railMat;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(20, 20, Screen.width - 40, 40), "ReflectiveHighway v1.2.0 - Cinematic", style);
        
        if (errorMessage != "None") {
            style.normal.textColor = Color.red;
            style.wordWrap = true;
            GUI.Label(new Rect(20, 70, Screen.width - 40, Screen.height - 100), "Error: " + errorMessage, style);
        }
    }
}

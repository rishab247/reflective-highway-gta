using UnityEngine;
using System.Collections.Generic;

public class SceneSetup : MonoBehaviour
{
    private string errorMessage = "None";
    private Transform mainCamera;
    private Transform playerCar;
    
    // Gameplay Stats
    private float currentSpeed = 0f;
    private float maxSpeed = 80f; 
    private float steerSpeed = 30f;
    private float distanceTraveled = 0f;
    private bool isGameOver = false;

    // Endless Logic
    private float spawnZ = -100f; 
    private float segmentLength = 100f;
    private int initialSegments = 20;
    private List<GameObject> activeSegments = new List<GameObject>();
    private List<GameObject> activeTraffic = new List<GameObject>();
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
    private Material trafficMat; // Random colors
    
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
            PrepareMaterials();
            
            CreatePlayerCar();
            SetupCamera();
            
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
        roadTex = GenerateAsphaltTexture();
        roadMat = new Material(SafeShader("Standard"));
        roadMat.mainTexture = roadTex;
        roadMat.SetFloat("_Glossiness", 0.92f); 
        roadMat.SetFloat("_Metallic", 0.0f);
        roadMat.SetColor("_Color", new Color(0.15f, 0.15f, 0.15f));

        // Buildings
        windowTex = GenerateWindowTexture();
        buildingMat = new Material(SafeShader("Standard"));
        buildingMat.color = new Color(0.08f, 0.08f, 0.1f);
        buildingMat.SetFloat("_Glossiness", 0.8f);
        buildingMat.mainTexture = windowTex;
        
        windowMat = new Material(SafeShader("Standard")); 
        windowMat.EnableKeyword("_EMISSION");
        windowMat.SetColor("_EmissionColor", new Color(0.7f, 0.9f, 1f) * 1.5f);

        // Player Car (Silver)
        carBodyMat = new Material(SafeShader("Standard"));
        carBodyMat.color = new Color(0.8f, 0.8f, 0.9f); 
        carBodyMat.SetFloat("_Glossiness", 0.95f);
        carBodyMat.SetFloat("_Metallic", 1.0f);
        
        carGlassMat = new Material(SafeShader("Standard"));
        carGlassMat.color = new Color(0, 0, 0, 0.8f);
        carGlassMat.SetFloat("_Glossiness", 1.0f);

        tailLightMat = new Material(SafeShader("Standard"));
        tailLightMat.EnableKeyword("_EMISSION");
        tailLightMat.SetColor("_EmissionColor", new Color(1f, 0, 0) * 3f);

        headLightMat = new Material(SafeShader("Standard"));
        headLightMat.EnableKeyword("_EMISSION");
        headLightMat.SetColor("_EmissionColor", new Color(0.9f, 0.9f, 1f) * 3f);

        // Lamps & Rails
        lampMat = new Material(SafeShader("Standard"));
        lampMat.color = Color.black;
        lampMat.SetFloat("_Glossiness", 0.6f);
        
        lampEmissive = new Material(SafeShader("Standard"));
        lampEmissive.EnableKeyword("_EMISSION");
        lampEmissive.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.2f) * 2f);

        railMat = new Material(SafeShader("Standard"));
        railMat.color = new Color(0.5f, 0.5f, 0.5f);
        railMat.SetFloat("_Glossiness", 0.7f);
        railMat.SetFloat("_Metallic", 0.6f);
        
        // Traffic generic
        trafficMat = new Material(SafeShader("Standard"));
        trafficMat.SetFloat("_Glossiness", 0.8f);
    }

    void Update()
    {
        if (isGameOver) {
            if (Input.GetMouseButtonDown(0)) {
                // Restart logic (simple scene reload would be better, but lets just reset pos)
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
            return;
        }

        if (playerCar == null) return;

        // Acceleration
        currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.deltaTime * 0.5f);
        distanceTraveled += currentSpeed * Time.deltaTime;

        // Steering
        float steer = 0f;
        if (Input.GetMouseButton(0))
        {
            float normX = (Input.mousePosition.x / Screen.width) * 2f - 1f; 
            steer = normX * steerSpeed;
        }
        if (Input.acceleration.x != 0) steer = Input.acceleration.x * steerSpeed * 2f;

        Vector3 moveVec = new Vector3(steer, 0, currentSpeed);
        playerCar.Translate(moveVec * Time.deltaTime);

        // Clamp to Road
        Vector3 pos = playerCar.position;
        pos.x = Mathf.Clamp(pos.x, -14f, 14f);
        playerCar.position = pos;

        // Camera Chase
        if (mainCamera != null) {
            Vector3 targetPos = playerCar.position + new Vector3(0, 3.5f, -8f); 
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
        
        // Traffic Logic
        UpdateTraffic();
        CheckCollisions();
    }
    
    // --- Traffic System ---
    
    void SpawnTraffic(float zPos)
    {
        // Lanes at x = -7, 0, 7 approximately (Road width 30)
        // Let's use 3 lanes: -8, 0, 8
        float[] lanes = new float[] {-8f, 0f, 8f};
        
        // Chance to spawn per lane
        foreach(float laneX in lanes) {
            if (Random.value > 0.7f) { // 30% chance per lane per segment
                GameObject car = CreateTrafficCar(new Vector3(laneX, 0.5f, zPos + Random.Range(0f, 50f)));
                activeTraffic.Add(car);
            }
        }
    }
    
    GameObject CreateTrafficCar(Vector3 pos)
    {
        GameObject tCar = new GameObject("TrafficCar");
        tCar.transform.SetParent(environmentRoot);
        tCar.transform.position = pos;
        
        // Random Color
        Material mat = new Material(trafficMat);
        mat.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f); // Vibrant
        
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(tCar.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.transform.localScale = new Vector3(2f, 1f, 4.5f); // Boxy
        body.GetComponent<Renderer>().material = mat;
        
        // Taillights
        GameObject tl = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tl.transform.SetParent(tCar.transform);
        tl.transform.localPosition = new Vector3(0, 0.5f, -2.26f);
        tl.transform.localRotation = Quaternion.Euler(0, 180, 0);
        tl.transform.localScale = new Vector3(1.8f, 0.3f, 1f);
        tl.GetComponent<Renderer>().material = tailLightMat;
        
        return tCar;
    }
    
    void UpdateTraffic()
    {
        float trafficSpeed = 40f; // Slower than player (80)
        for (int i = activeTraffic.Count - 1; i >= 0; i--) {
            GameObject t = activeTraffic[i];
            if (t == null) { activeTraffic.RemoveAt(i); continue; }
            
            t.transform.Translate(Vector3.forward * trafficSpeed * Time.deltaTime);
            
            // Cleanup if behind
            if (t.transform.position.z < playerCar.position.z - 50f) {
                activeTraffic.RemoveAt(i);
                Destroy(t);
            }
        }
    }
    
    void CheckCollisions()
    {
        // Simple distance check bounding box
        Bounds playerBounds = new Bounds(playerCar.position, new Vector3(2.2f, 2f, 5f));
        
        foreach(GameObject t in activeTraffic) {
            if (t == null) continue;
            // Traffic bounds
            Bounds tBounds = new Bounds(t.transform.position + Vector3.up*0.5f, new Vector3(2f, 2f, 4.5f));
            
            if (playerBounds.Intersects(tBounds)) {
                GameOver();
            }
        }
    }
    
    void GameOver()
    {
        isGameOver = true;
        currentSpeed = 0f;
        // visual crash?
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
        road.transform.localScale = new Vector3(3f, 1, 10f); 
        road.transform.position = new Vector3(0, 0, spawnZ);
        Renderer r = road.GetComponent<Renderer>();
        r.material = roadMat;
        r.material.mainTextureScale = new Vector2(1, 5); 

        // Lamps
        CreateLamp(new Vector3(-16f, 0, spawnZ), 90f, root.transform);
        CreateLamp(new Vector3(16f, 0, spawnZ), -90f, root.transform);
        
        // Rails
        CreateRail(new Vector3(-18f, 0, spawnZ), root.transform);
        CreateRail(new Vector3(18f, 0, spawnZ), root.transform);

        // Buildings
        if (Random.value > 0.2f) CreateBuilding(new Vector3(-40f - Random.value * 40f, 0, spawnZ), root.transform);
        if (Random.value > 0.2f) CreateBuilding(new Vector3(40f + Random.value * 40f, 0, spawnZ), root.transform);
        
        // Traffic
        if (spawnZ > 100f) // Don't spawn traffic immediately at start
            SpawnTraffic(spawnZ);

        spawnZ += segmentLength;
    }
    
    // Helpers (Rail, Lamp, Building) omitted for brevity, reusing previous implementations...
    // Actually, I must include them or the file breaks. Re-adding concise versions.
    
    void CreateRail(Vector3 pos, Transform parent) {
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.transform.SetParent(parent);
        rail.transform.position = pos + Vector3.up * 1f;
        rail.transform.localScale = new Vector3(0.5f, 1f, segmentLength);
        rail.GetComponent<Renderer>().material = railMat;
    }

    void CreateLamp(Vector3 pos, float rotY, Transform parent) {
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

    void CreateBuilding(Vector3 pos, Transform parent) {
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
    
    void CleanupOldSegments() {
        if (activeSegments.Count > initialSegments + 4) {
            GameObject old = activeSegments[0];
            activeSegments.RemoveAt(0);
            Destroy(old);
        }
    }
    
    // --- Textures & Car Gen ---
    
    Texture2D GenerateAsphaltTexture() {
        int w = 256; Texture2D tex = new Texture2D(w, w);
        Color[] cols = new Color[w*w];
        for(int i=0; i<cols.Length; i++) cols[i] = new Color(0.15f,0.15f,0.15f);
        // Simplified procedural noise/lines
        for(int y=0; y<w; y++) {
            for(int x=0; x<w; x++) {
                float u = x/(float)w;
                if(Mathf.Abs(u-0.5f)<0.01f || Mathf.Abs(u-0.25f)<0.01f || Mathf.Abs(u-0.75f)<0.01f) {
                   if((y/32)%2==0) cols[y*w+x] = Color.white * 0.8f;
                }
            }
        }
        tex.SetPixels(cols); tex.Apply(); return tex;
    }
    
    Texture2D GenerateWindowTexture() {
        int w=128; Texture2D tex=new Texture2D(w,w);
        Color[] c=new Color[w*w];
        for(int i=0;i<c.Length;i++) c[i] = (Random.value>0.6f)? new Color(1,0.9f,0.6f) : new Color(0,0,0.1f);
        tex.SetPixels(c); tex.Apply(); return tex;
    }

    void CreatePlayerCar() {
        playerCar = new GameObject("PlayerCar").transform;
        playerCar.position = new Vector3(0, 0.5f, 0);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(playerCar);
        body.transform.localPosition = new Vector3(0, 0.6f, 0);
        body.transform.localScale = new Vector3(2.2f, 0.8f, 5.0f);
        body.GetComponent<Renderer>().material = carBodyMat;
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(playerCar);
        cabin.transform.localPosition = new Vector3(0, 1.2f, -0.5f);
        cabin.transform.localScale = new Vector3(1.8f, 0.7f, 2.5f);
        cabin.GetComponent<Renderer>().material = carGlassMat;
        
        CreateHeadlight(new Vector3(-0.8f, 0.8f, 2.5f));
        CreateHeadlight(new Vector3(0.8f, 0.8f, 2.5f));
        CreateTaillight(new Vector3(-0.8f, 0.8f, -2.5f));
        CreateTaillight(new Vector3(0.8f, 0.8f, -2.5f));
        
        // Underglow
        GameObject glow = new GameObject("Underglow");
        glow.transform.SetParent(playerCar);
        glow.transform.localPosition = new Vector3(0, 0.1f, 0);
        Light l = glow.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 8f; l.intensity = 2f; l.color = Color.cyan;
    }
    
    void CreateHeadlight(Vector3 pos) {
        GameObject hl = GameObject.CreatePrimitive(PrimitiveType.Quad);
        hl.transform.SetParent(playerCar); hl.transform.localPosition = pos;
        hl.transform.localScale = new Vector3(0.5f, 0.2f, 1f);
        hl.GetComponent<Renderer>().material = headLightMat;
    }
    void CreateTaillight(Vector3 pos) {
        GameObject tl = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tl.transform.SetParent(playerCar); tl.transform.localPosition = pos;
        tl.transform.localRotation = Quaternion.Euler(0,180,0);
        tl.transform.localScale = new Vector3(0.5f, 0.2f, 1f);
        tl.GetComponent<Renderer>().material = tailLightMat;
    }

    void SetupCamera() {
        GameObject camObj = GameObject.FindWithTag("MainCamera");
        if (camObj == null) { camObj = new GameObject("Main Camera"); camObj.AddComponent<Camera>(); camObj.tag = "MainCamera"; }
        mainCamera = camObj.transform;
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox; cam.farClipPlane = 1500f; cam.allowHDR = true;
        if (camObj.GetComponent<SimpleBloomEffect>() == null) camObj.AddComponent<SimpleBloomEffect>();
    }

    void SetupLighting() {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.02f, 0.02f, 0.1f);
        RenderSettings.ambientGroundColor = Color.black;
        RenderSettings.fog = true; RenderSettings.fogDensity = 0.003f;
    }

    void CreateSky() {
        Texture2D tex = Resources.Load<Texture2D>("Textures/SkyTex");
        if (tex != null) {
            Shader s = Shader.Find("Skybox/Panoramic");
            if (s != null) { Material m = new Material(s); m.SetTexture("_MainTex", tex); RenderSettings.skybox = m; }
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
        style.fontSize = 40;
        style.normal.textColor = Color.cyan;
        style.fontStyle = FontStyle.Bold;
        
        if (!isGameOver) {
            GUI.Label(new Rect(50, 50, 500, 100), "DISTANCE: " + (int)distanceTraveled + "m", style);
            GUI.Label(new Rect(50, 100, 500, 100), "SPEED: " + (int)(currentSpeed*2) + " km/h", style);
        } else {
            style.fontSize = 80;
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(Screen.width/2 - 200, Screen.height/2 - 100, 500, 200), "GAME OVER", style);
            style.fontSize = 40;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(Screen.width/2 - 150, Screen.height/2 + 20, 500, 100), "Tap to Restart", style);
        }
        
        if (errorMessage != "None") {
            style.fontSize = 20; style.normal.textColor = Color.red;
            GUI.Label(new Rect(20, Screen.height-200, Screen.width, 200), errorMessage, style);
        }
    }
}

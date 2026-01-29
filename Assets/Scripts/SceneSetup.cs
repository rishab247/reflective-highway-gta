using UnityEngine;
using System.Collections.Generic;

public class SceneSetup : MonoBehaviour
{
    private string errorMessage = "None";
    private Transform mainCamera;
    private Transform playerCar;
    private AudioSource engineAudio;
    private AudioSource sfxAudio;
    
    // Gameplay Stats
    private float currentSpeed = 0f;
    private float maxSpeed = 80f; 
    private float steerSpeed = 30f;
    private float distanceTraveled = 0f;
    private int score = 0;
    private bool isGameOver = false;
    private bool isNitro = false;
    private float nitroTimer = 0f;

    // Endless Logic
    private float spawnZ = -100f; 
    private float segmentLength = 100f;
    private int initialSegments = 20;
    private List<GameObject> activeSegments = new List<GameObject>();
    private List<GameObject> activeTraffic = new List<GameObject>();
    private List<GameObject> activeItems = new List<GameObject>();
    private Transform environmentRoot;

    // Control State
    private float steerInput = 0f;

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
    private Material trafficMat;
    private Material coinMat;
    private Material nitroMat;
    private Material tunnelMat;
    
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
            SetupAudio();
            
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
        
        trafficMat = new Material(SafeShader("Standard"));
        trafficMat.SetFloat("_Glossiness", 0.8f);
        
        // Powerups
        coinMat = new Material(SafeShader("Standard"));
        coinMat.color = Color.yellow;
        coinMat.EnableKeyword("_EMISSION");
        coinMat.SetColor("_EmissionColor", Color.yellow);
        
        nitroMat = new Material(SafeShader("Standard"));
        nitroMat.color = Color.cyan;
        nitroMat.EnableKeyword("_EMISSION");
        nitroMat.SetColor("_EmissionColor", Color.cyan * 2f);
        
        tunnelMat = new Material(SafeShader("Standard"));
        tunnelMat.color = new Color(0.2f, 0.2f, 0.2f);
    }

    void Update()
    {
        if (isGameOver) {
            if (Input.GetMouseButtonDown(0)) {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
            return;
        }

        if (playerCar == null) return;

        // Nitro Logic
        if (isNitro) {
            maxSpeed = 120f;
            nitroTimer -= Time.deltaTime;
            if (nitroTimer <= 0) {
                isNitro = false;
                maxSpeed = 80f;
            }
        }

        // Acceleration
        currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.deltaTime * 0.5f);
        distanceTraveled += currentSpeed * Time.deltaTime;
        
        // Audio Pitch
        if (engineAudio != null) {
            engineAudio.pitch = 0.5f + (currentSpeed / 120f) * 1.5f;
        }

        // Steering (Buttons override Touch)
        // If touching buttons, steerInput is set in OnGUI
        // If not, allow keyboard fallback
        if (steerInput == 0) {
            if (Input.GetKey(KeyCode.LeftArrow)) steerInput = -1;
            if (Input.GetKey(KeyCode.RightArrow)) steerInput = 1;
        }

        Vector3 moveVec = new Vector3(steerInput * steerSpeed, 0, currentSpeed);
        playerCar.Translate(moveVec * Time.deltaTime);
        
        // Tilt Effect
        playerCar.localRotation = Quaternion.Lerp(playerCar.localRotation, Quaternion.Euler(0, 0, -steerInput * 10f), Time.deltaTime * 5f);

        // Clamp to Road
        Vector3 pos = playerCar.position;
        pos.x = Mathf.Clamp(pos.x, -14f, 14f);
        playerCar.position = pos;

        // Camera Chase
        if (mainCamera != null) {
            Vector3 targetPos = playerCar.position + new Vector3(0, 3.5f, -8f); 
            // Nitro shake/lag
            if (isNitro) targetPos += Random.insideUnitSphere * 0.1f;
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
        
        UpdateTraffic();
        CheckCollisions();
        
        // Reset Input for next frame (UI buttons hold it)
        steerInput = 0;
    }
    
    // --- Traffic & Items ---
    
    void SpawnTraffic(float zPos)
    {
        float[] lanes = new float[] {-8f, 0f, 8f};
        foreach(float laneX in lanes) {
            // Traffic?
            if (Random.value > 0.7f) { 
                CreateTrafficCar(new Vector3(laneX, 0.5f, zPos + Random.Range(0f, 50f)));
            } 
            // Powerup? (If no traffic)
            else if (Random.value > 0.8f) {
                CreateItem(new Vector3(laneX, 1f, zPos + Random.Range(0f, 50f)), Random.value > 0.8f); // 20% nitro, 80% coin
            }
        }
    }
    
    void CreateItem(Vector3 pos, bool isNitroItem) {
        GameObject item = GameObject.CreatePrimitive(isNitroItem ? PrimitiveType.Cube : PrimitiveType.Sphere); // Cube=Nitro, Sphere=Coin
        item.name = isNitroItem ? "NITRO" : "COIN";
        item.transform.SetParent(environmentRoot);
        item.transform.position = pos;
        item.transform.localScale = Vector3.one * 1.5f;
        item.GetComponent<Renderer>().material = isNitroItem ? nitroMat : coinMat;
        Destroy(item.GetComponent<Collider>()); // Manual check
        activeItems.Add(item);
    }
    
    void CreateTrafficCar(Vector3 pos)
    {
        GameObject tCar = new GameObject("TrafficCar");
        tCar.transform.SetParent(environmentRoot);
        tCar.transform.position = pos;
        Material mat = new Material(trafficMat);
        mat.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(tCar.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.transform.localScale = new Vector3(2f, 1f, 4.5f);
        body.GetComponent<Renderer>().material = mat;
        GameObject tl = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tl.transform.SetParent(tCar.transform);
        tl.transform.localPosition = new Vector3(0, 0.5f, -2.26f);
        tl.transform.localRotation = Quaternion.Euler(0, 180, 0);
        tl.transform.localScale = new Vector3(1.8f, 0.3f, 1f);
        tl.GetComponent<Renderer>().material = tailLightMat;
        activeTraffic.Add(tCar);
    }
    
    void UpdateTraffic()
    {
        float trafficSpeed = 40f; 
        for (int i = activeTraffic.Count - 1; i >= 0; i--) {
            GameObject t = activeTraffic[i];
            if (t == null) { activeTraffic.RemoveAt(i); continue; }
            t.transform.Translate(Vector3.forward * trafficSpeed * Time.deltaTime);
            if (t.transform.position.z < playerCar.position.z - 50f) {
                activeTraffic.RemoveAt(i); Destroy(t);
            }
        }
        
        // Spin Items
        foreach(GameObject item in activeItems) {
            if(item != null) item.transform.Rotate(0, 90f * Time.deltaTime, 0);
        }
    }
    
    void CheckCollisions()
    {
        Bounds playerBounds = new Bounds(playerCar.position, new Vector3(2.2f, 2f, 5f));
        
        // Traffic
        foreach(GameObject t in activeTraffic) {
            if (t == null) continue;
            Bounds tBounds = new Bounds(t.transform.position + Vector3.up*0.5f, new Vector3(2f, 2f, 4.5f));
            if (playerBounds.Intersects(tBounds)) GameOver();
        }
        
        // Items
        for (int i = activeItems.Count - 1; i >= 0; i--) {
            GameObject item = activeItems[i];
            if (item == null) { activeItems.RemoveAt(i); continue; }
            if (Vector3.Distance(playerCar.position, item.transform.position) < 3f) {
                if (item.name == "NITRO") ActivateNitro();
                else CollectCoin();
                Destroy(item);
                activeItems.RemoveAt(i);
            }
        }
    }
    
    void ActivateNitro() {
        isNitro = true;
        nitroTimer = 3f;
        // Play sound?
        if (sfxAudio != null) { sfxAudio.pitch = 1.5f; sfxAudio.PlayOneShot(AudioClip.Create("Nitro", 100, 1, 44100, false)); } // Placeholder
    }
    
    void CollectCoin() {
        score += 100;
    }
    
    void GameOver()
    {
        isGameOver = true;
        currentSpeed = 0f;
    }

    // --- World Gen (Biomes) ---

    void SpawnWorldSegment()
    {
        // 20% chance to switch biome logic (simplified: random tunnel/bridge segments)
        bool isTunnel = (Random.value < 0.1f);
        bool isBridge = !isTunnel && (Random.value < 0.1f);

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

        if (isTunnel) CreateTunnel(root.transform, spawnZ);
        else if (isBridge) CreateBridge(root.transform, spawnZ);
        else {
            // Normal City
            CreateLamp(new Vector3(-16f, 0, spawnZ), 90f, root.transform);
            CreateLamp(new Vector3(16f, 0, spawnZ), -90f, root.transform);
            CreateRail(new Vector3(-18f, 0, spawnZ), root.transform);
            CreateRail(new Vector3(18f, 0, spawnZ), root.transform);
            if (Random.value > 0.2f) CreateBuilding(new Vector3(-40f - Random.value * 40f, 0, spawnZ), root.transform);
            if (Random.value > 0.2f) CreateBuilding(new Vector3(40f + Random.value * 40f, 0, spawnZ), root.transform);
        }
        
        if (spawnZ > 100f) SpawnTraffic(spawnZ);

        spawnZ += segmentLength;
    }
    
    void CreateTunnel(Transform parent, float zPos) {
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent);
        roof.transform.position = new Vector3(0, 10f, zPos);
        roof.transform.localScale = new Vector3(32f, 1f, 100f);
        roof.GetComponent<Renderer>().material = tunnelMat;
        
        // Walls
        GameObject wallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallL.transform.SetParent(parent);
        wallL.transform.position = new Vector3(-16f, 5f, zPos);
        wallL.transform.localScale = new Vector3(1f, 10f, 100f);
        wallL.GetComponent<Renderer>().material = tunnelMat;
        
        GameObject wallR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallR.transform.SetParent(parent);
        wallR.transform.position = new Vector3(16f, 5f, zPos);
        wallR.transform.localScale = new Vector3(1f, 10f, 100f);
        wallR.GetComponent<Renderer>().material = tunnelMat;
        
        // Ceiling Lights
        GameObject light = new GameObject("TunnelLight");
        light.transform.SetParent(parent);
        light.transform.position = new Vector3(0, 9f, zPos);
        Light l = light.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 30f;
        l.intensity = 2f;
        l.color = Color.yellow;
    }
    
    void CreateBridge(Transform parent, float zPos) {
        // Just rails, no buildings, maybe water below?
        CreateRail(new Vector3(-16f, 0, zPos), parent);
        CreateRail(new Vector3(16f, 0, zPos), parent);
        // Suspension cables? (Simplified: tall pillars)
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.transform.SetParent(parent);
        pillar.transform.position = new Vector3(-18f, 10f, zPos);
        pillar.transform.localScale = new Vector3(2f, 20f, 2f);
        pillar.GetComponent<Renderer>().material = railMat;
        GameObject pillar2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar2.transform.SetParent(parent);
        pillar2.transform.position = new Vector3(18f, 10f, zPos);
        pillar2.transform.localScale = new Vector3(2f, 20f, 2f);
        pillar2.GetComponent<Renderer>().material = railMat;
    }
    
    // Helpers
    void CreateRail(Vector3 pos, Transform parent) {
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.transform.SetParent(parent);
        rail.transform.position = pos + Vector3.up * 1f;
        rail.transform.localScale = new Vector3(0.5f, 1f, segmentLength);
        rail.GetComponent<Renderer>().material = railMat;
    }
    void CreateLamp(Vector3 pos, float rotY, Transform parent) {
        GameObject lamp = new GameObject("Lamp");
        lamp.transform.SetParent(parent); lamp.transform.position = pos; lamp.transform.rotation = Quaternion.Euler(0, rotY, 0);
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(lamp.transform); pole.transform.localPosition = new Vector3(0, 6f, 0); pole.transform.localScale = new Vector3(0.5f, 6f, 0.5f); pole.GetComponent<Renderer>().material = lampMat;
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.transform.SetParent(lamp.transform); arm.transform.localPosition = new Vector3(2f, 11.5f, 0); arm.transform.localScale = new Vector3(4f, 0.3f, 0.3f); arm.GetComponent<Renderer>().material = lampMat;
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(lamp.transform); head.transform.localPosition = new Vector3(3.8f, 11.3f, 0); head.GetComponent<Renderer>().material = lampEmissive;
    }
    void CreateBuilding(Vector3 pos, Transform parent) {
        float h = 40f + Random.value * 150f;
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.transform.SetParent(parent); b.transform.position = pos + new Vector3(0, h/2, 0); b.transform.localScale = new Vector3(30f, h, 30f);
        Renderer r = b.GetComponent<Renderer>(); r.material = buildingMat; r.material.mainTextureScale = new Vector2(3, h/10f); r.material.SetTexture("_EmissionMap", windowTex);
    }
    void CleanupOldSegments() {
        if (activeSegments.Count > initialSegments + 4) {
            GameObject old = activeSegments[0]; activeSegments.RemoveAt(0); Destroy(old);
        }
    }
    
    // Audio
    void SetupAudio() {
        engineAudio = gameObject.AddComponent<AudioSource>();
        engineAudio.loop = true;
        engineAudio.clip = AudioClip.Create("EngineHum", 44100, 1, 44100, false); 
        // Procedural Audio: simple buzz
        float[] data = new float[44100];
        for(int i=0; i<data.Length; i++) data[i] = Mathf.Sin(i * 0.05f) * 0.2f;
        engineAudio.clip.SetData(data, 0);
        engineAudio.Play();
        
        sfxAudio = gameObject.AddComponent<AudioSource>();
    }

    Texture2D GenerateAsphaltTexture() {
        int w = 256; Texture2D tex = new Texture2D(w, w);
        Color[] cols = new Color[w*w];
        for(int i=0; i<cols.Length; i++) cols[i] = new Color(0.15f,0.15f,0.15f);
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
        body.transform.SetParent(playerCar); body.transform.localPosition = new Vector3(0, 0.6f, 0); body.transform.localScale = new Vector3(2.2f, 0.8f, 5.0f); body.GetComponent<Renderer>().material = carBodyMat;
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(playerCar); cabin.transform.localPosition = new Vector3(0, 1.2f, -0.5f); cabin.transform.localScale = new Vector3(1.8f, 0.7f, 2.5f); cabin.GetComponent<Renderer>().material = carGlassMat;
        CreateHeadlight(new Vector3(-0.8f, 0.8f, 2.5f)); CreateHeadlight(new Vector3(0.8f, 0.8f, 2.5f)); CreateTaillight(new Vector3(-0.8f, 0.8f, -2.5f)); CreateTaillight(new Vector3(0.8f, 0.8f, -2.5f));
        GameObject glow = new GameObject("Underglow"); glow.transform.SetParent(playerCar); glow.transform.localPosition = new Vector3(0, 0.1f, 0); Light l = glow.AddComponent<Light>(); l.type = LightType.Point; l.range = 8f; l.intensity = 2f; l.color = Color.cyan;
    }
    void CreateHeadlight(Vector3 pos) {
        GameObject hl = GameObject.CreatePrimitive(PrimitiveType.Quad); hl.transform.SetParent(playerCar); hl.transform.localPosition = pos; hl.transform.localScale = new Vector3(0.5f, 0.2f, 1f); hl.GetComponent<Renderer>().material = headLightMat;
    }
    void CreateTaillight(Vector3 pos) {
        GameObject tl = GameObject.CreatePrimitive(PrimitiveType.Quad); tl.transform.SetParent(playerCar); tl.transform.localPosition = pos; tl.transform.localRotation = Quaternion.Euler(0,180,0); tl.transform.localScale = new Vector3(0.5f, 0.2f, 1f); tl.GetComponent<Renderer>().material = tailLightMat;
    }
    void SetupCamera() {
        GameObject camObj = GameObject.FindWithTag("MainCamera"); 
        if (camObj == null) { 
            camObj = new GameObject("Main Camera"); 
            camObj.AddComponent<Camera>(); 
            camObj.tag = "MainCamera"; 
        }
        // ESSENTIAL: Add AudioListener for sound!
        if (camObj.GetComponent<AudioListener>() == null) {
            camObj.AddComponent<AudioListener>();
        }

        mainCamera = camObj.transform; 
        Camera cam = camObj.GetComponent<Camera>(); 
        cam.clearFlags = CameraClearFlags.Skybox; 
        cam.farClipPlane = 1500f; 
        cam.allowHDR = true;
        if (camObj.GetComponent<SimpleBloomEffect>() == null) camObj.AddComponent<SimpleBloomEffect>();
    }
    
    // --- ON SCREEN CONTROLS ---
    
    void OnGUI()
    {
        // Scale UI to be visible on high-DPI screens
        // Base resolution reference: 1920x1080
        float scaleX = Screen.width / 1920f;
        float scaleY = Screen.height / 1080f;
        float scale = Mathf.Max(scaleX, scaleY); // Use largest scale to keep items readable
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

        GUIStyle style = new GUIStyle();
        style.fontSize = 60; // Bigger font
        style.normal.textColor = Color.cyan; 
        style.fontStyle = FontStyle.Bold;
        
        // Adjust coordinates for the scaled matrix
        // We act as if screen is 1920x1080 (approx)
        
        if (!isGameOver) {
            GUI.Label(new Rect(50, 50, 800, 100), "DIST: " + (int)distanceTraveled + "m", style);
            GUI.Label(new Rect(50, 150, 800, 100), "SCORE: " + score, style);
            if (isNitro) {
                style.normal.textColor = Color.yellow;
                GUI.Label(new Rect(1920/2 - 150, 300, 300, 150), "NITRO!", style);
            }
            
            // CONTROLS
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 120; // Huge arrows
            
            // Left Button
            // Bottom Left
            if (GUI.RepeatButton(new Rect(50, 1080 - 400, 300, 300), "<", btnStyle)) {
                steerInput = -1f;
            }
            // Right Button
            // Bottom Right (Screen width relative to scale is roughly 1920)
            // But we must use Screen.width / scale to be precise? 
            // Simplification: Use fixed large coordinates that usually fit landscape
            if (GUI.RepeatButton(new Rect(1920 - 350, 1080 - 400, 300, 300), ">", btnStyle)) {
                steerInput = 1f;
            }
            
        } else {
            style.fontSize = 150; style.normal.textColor = Color.red;
            GUI.Label(new Rect(1920/2 - 400, 1080/2 - 200, 800, 300), "GAME OVER", style);
            
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 80;
            if (GUI.Button(new Rect(1920/2 - 250, 1080/2 + 100, 500, 200), "RESTART", btnStyle)) {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }
        
        if (errorMessage != "None") {
            style.fontSize = 40; style.normal.textColor = Color.red;
            GUI.Label(new Rect(50, 1080-300, 1800, 300), errorMessage, style);
        }
    }

    void SetupLighting() {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(0.02f, 0.02f, 0.1f); RenderSettings.ambientGroundColor = Color.black; RenderSettings.fog = true; RenderSettings.fogDensity = 0.003f;
    }
    void CreateSky() {
        Texture2D tex = Resources.Load<Texture2D>("Textures/SkyTex");
        if (tex != null) { Shader s = Shader.Find("Skybox/Panoramic"); if (s != null) { Material m = new Material(s); m.SetTexture("_MainTex", tex); RenderSettings.skybox = m; } }
    }
    private Shader SafeShader(string name) {
        Shader s = Shader.Find(name); if (s == null) s = Shader.Find("Standard"); return s;
    }
}

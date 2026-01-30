using UnityEngine;
using System.Collections.Generic;

public class SceneSetup : MonoBehaviour
{
    private string errorMessage = "None";
    private string diagnostics = "Initializing...";
    private Transform mainCamera;
    private Transform playerCar;
    
    private float currentSpeed = 0f;
    private float distanceTraveled = 0f;
    private float spawnZ = -100f; 
    private float segmentLength = 100f;
    private int initialSegments = 20;
    private List<GameObject> activeSegments = new List<GameObject>();
    private Transform environmentRoot;

    private bool isGameStarted = false; // RESTORED MISSING VARIABLE

    private Material roadMat;
    private Material buildingMat;
    private Material carBodyMat;
    private Material carGlassMat;
    private Material lampMat;
    private Material lampEmissive;

    void Awake() { 
        Application.logMessageReceived += HandleLog; 
    }

    void HandleLog(string logString, string stackTrace, LogType type) {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert || logString.Contains("Material") || logString.Contains("Texture")) {
            errorMessage = string.Format("[{0}] {1}\n{2}", type, logString, stackTrace);
            GUIUtility.systemCopyBuffer = errorMessage;
        }
    }

    private TouchControl btnLeft, btnRight, btnGas, btnBrake;
    private ProceduralEngineAudio audioSys;

    void Start() {
        try {
            environmentRoot = new GameObject("Environment").transform;
            SetupCamera(); 
            PrepareMaterials();
            SetupLighting();
            CreatePlayerCar();
            SetupHUD(); // NEW: Create UI
            for (int i = 0; i < initialSegments; i++) SpawnWorldSegment();
            diagnostics = "Nominal";
        } catch (System.Exception e) {
            errorMessage = "START ERROR: " + e.Message + "\n" + e.StackTrace;
            GUIUtility.systemCopyBuffer = errorMessage;
        }
    }

    void SetupHUD() {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Event System
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // Controls
        btnLeft = CreateTouchButton(canvasObj, "Left", "Textures/UI_SteeringWheel", new Vector2(150, 150), new Vector2(0, 0), new Vector3(0, 0, 90));
        btnRight = CreateTouchButton(canvasObj, "Right", "Textures/UI_SteeringWheel", new Vector2(150, 150), new Vector2(350, 0), new Vector3(0, 0, -90));
        
        btnBrake = CreateTouchButton(canvasObj, "Brake", "Textures/UI_BrakePedal", new Vector2(120, 200), new Vector2(-300, 0), Vector3.zero, new Vector2(1, 0));
        btnGas = CreateTouchButton(canvasObj, "Gas", "Textures/UI_GasPedal", new Vector2(120, 250), new Vector2(-100, 0), Vector3.zero, new Vector2(1, 0));
    }

    TouchControl CreateTouchButton(GameObject parent, string name, string spritePath, Vector2 size, Vector2 offset, Vector3 rot, Vector2? anchorOverride = null) {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        Sprite s = Resources.Load<Sprite>(spritePath);
        if (s != null) img.sprite = s;
        else img.color = new Color(1,1,1,0.5f); // Fallback box
        
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        
        if (anchorOverride.HasValue) {
            rt.anchorMin = rt.anchorMax = anchorOverride.Value; // Custom corner
        } else {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0); // Bottom Left default
        }
        
        rt.anchoredPosition = offset + (anchorOverride.HasValue ? Vector2.zero : new Vector2(size.x/2, size.y/2));
        rt.localRotation = Quaternion.Euler(rot);
        
        return go.AddComponent<TouchControl>();
    }

    void CreatePlayerCar() {
        playerCar = new GameObject("PlayerCar").transform;
        playerCar.position = new Vector3(0, 0.5f, 0);
        playerCar.gameObject.AddComponent<CarSplashEffect>();
        audioSys = playerCar.gameObject.AddComponent<ProceduralEngineAudio>();

        // --- High Def Composite Car ---
        
        // Chassis (Main Body)
        GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chassis.transform.SetParent(playerCar);
        chassis.transform.localPosition = new Vector3(0, 0.4f, 0);
        chassis.transform.localScale = new Vector3(2.0f, 0.6f, 4.8f);
        chassis.GetComponent<Renderer>().material = carBodyMat;

        // Cabin (Top)
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(playerCar);
        cabin.transform.localPosition = new Vector3(0, 1.0f, -0.3f);
        cabin.transform.localScale = new Vector3(1.7f, 0.6f, 2.5f);
        cabin.GetComponent<Renderer>().material = carGlassMat; // Glass material

        // Roof (Top of cabin, body color)
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(playerCar);
        roof.transform.localPosition = new Vector3(0, 1.31f, -0.3f);
        roof.transform.localScale = new Vector3(1.72f, 0.05f, 2.55f);
        roof.GetComponent<Renderer>().material = carBodyMat;

        // Wheels
        CreateWheel(new Vector3(-1.1f, 0.4f, 1.5f));
        CreateWheel(new Vector3(1.1f, 0.4f, 1.5f));
        CreateWheel(new Vector3(-1.1f, 0.4f, -1.5f));
        CreateWheel(new Vector3(1.1f, 0.4f, -1.5f));
        
        // Headlights
        GameObject lightL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lightL.transform.SetParent(playerCar);
        lightL.transform.localPosition = new Vector3(-0.7f, 0.5f, 2.45f);
        lightL.transform.localScale = new Vector3(0.4f, 0.2f, 0.1f);
        lightL.GetComponent<Renderer>().material = lampEmissive;
        
        GameObject lightR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lightR.transform.SetParent(playerCar);
        lightR.transform.localPosition = new Vector3(0.7f, 0.5f, 2.45f);
        lightR.transform.localScale = new Vector3(0.4f, 0.2f, 0.1f);
        lightR.GetComponent<Renderer>().material = lampEmissive;
    }

    void CreateWheel(Vector3 pos) {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        w.transform.SetParent(playerCar);
        w.transform.localPosition = pos;
        w.transform.localRotation = Quaternion.Euler(0, 0, 90);
        w.transform.localScale = new Vector3(0.8f, 0.2f, 0.8f);
        w.GetComponent<Renderer>().material = lampMat; // Black rubber look
    }

    void Update() {
        if (!isGameStarted) { if (Input.GetMouseButtonDown(0)) isGameStarted = true; return; }
        if (playerCar == null) return;

        float fps = 1.0f / Time.unscaledDeltaTime;
        diagnostics = string.Format("FPS: {0:F0} | Segs: {1}", fps, activeSegments.Count);

        // --- Controls Logic ---
        float inputSteer = Input.GetAxis("Horizontal");
        if (btnLeft != null && btnLeft.isPressed) inputSteer = -1f;
        if (btnRight != null && btnRight.isPressed) inputSteer = 1f;

        float targetSpeed = 0f; // Friction stops car
        if (btnGas != null && btnGas.isPressed) targetSpeed = 80f; // Fast
        else if (btnBrake != null && btnBrake.isPressed) targetSpeed = 0f; // Stop
        else targetSpeed = 15f; // Idle roll

        // Smooth Physics-ish movement
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 0.5f);
        
        // Update Audio
        if (audioSys != null) {
            audioSys.currentSpeed = currentSpeed;
            audioSys.maxSpeed = 80f;
        }

        playerCar.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
        
        Vector3 targetLanePos = playerCar.position;
        targetLanePos.x = Mathf.Clamp(targetLanePos.x + (inputSteer * 20f * Time.deltaTime), -8, 8); 
        playerCar.position = targetLanePos;
        
        // Camera Lag
        Vector3 camTarget = playerCar.position + new Vector3(0, 4.0f, -9f);
        mainCamera.position = Vector3.Lerp(mainCamera.position, camTarget, Time.deltaTime * 5f);
        mainCamera.LookAt(playerCar.position + Vector3.up * 1.0f);

        if ((spawnZ - playerCar.position.z) < (initialSegments * segmentLength)) SpawnWorldSegment();
    }

    void OnGUI() {
        GUIStyle diagStyle = new GUIStyle();
        diagStyle.fontSize = 30; diagStyle.normal.textColor = Color.green; diagStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(20, 20, Screen.width, 60), ">>> DIAGNOSTICS: " + diagnostics, diagStyle);

        if (errorMessage != "None") { 
            GUIStyle errStyle = new GUIStyle();
            errStyle.fontSize = 22; errStyle.normal.textColor = Color.red; 
            GUI.Label(new Rect(20, Screen.height - 150, Screen.width - 40, 150), "AUTO-COPIED ERROR:\n" + errorMessage, errStyle); 
        }
        if (!isGameStarted) {
            if (GUI.Button(new Rect(Screen.width/2-150, Screen.height/2, 300, 100), "START SYSTEM")) isGameStarted = true;
        }
    }

    private Shader SafeShader(string name) {
        Shader s = Shader.Find(name); 
        if (s == null) s = Shader.Find("Mobile/Diffuse");
        if (s == null) s = Shader.Find("Standard"); 
        return s;
    }
}

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

    void Start() {
        try {
            environmentRoot = new GameObject("Environment").transform;
            SetupCamera(); 
            PrepareMaterials();
            SetupLighting();
            CreatePlayerCar();
            for (int i = 0; i < initialSegments; i++) SpawnWorldSegment();
            diagnostics = "Nominal";
        } catch (System.Exception e) {
            errorMessage = "START ERROR: " + e.Message + "\n" + e.StackTrace;
            GUIUtility.systemCopyBuffer = errorMessage;
        }
    }

    void SetupCamera() {
        GameObject camObj = GameObject.FindWithTag("MainCamera"); 
        if (camObj == null) { camObj = new GameObject("Main Camera"); camObj.AddComponent<Camera>(); camObj.tag = "MainCamera"; }
        mainCamera = camObj.transform; 
        Camera cam = camObj.GetComponent<Camera>(); 
        cam.clearFlags = CameraClearFlags.SolidColor; 
        cam.backgroundColor = new Color(0, 0, 0.05f); 
        cam.farClipPlane = 2000f; 
        cam.allowHDR = false; 
        cam.allowMSAA = false;
    }

    void SetupLighting() {
        GameObject sun = new GameObject("Sun");
        Light l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(0.7f, 0.8f, 1.0f);
        l.intensity = 1.0f; 
        sun.transform.rotation = Quaternion.Euler(50, -30, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.2f);
    }

    void PrepareMaterials() {
        Shader mobileDiffuse = Shader.Find("Mobile/Diffuse");
        if (mobileDiffuse == null) mobileDiffuse = Shader.Find("Diffuse");
        Shader unlitColor = Shader.Find("Unlit/Color");

        roadMat = CreateSecureMaterial(mobileDiffuse, "Textures/WetAsphalt_Albedo", "Road");
        buildingMat = CreateSecureMaterial(mobileDiffuse, "Textures/BuildingColor", "Building");
        buildingMat.color = new Color(0.6f, 0.6f, 0.7f);
        carBodyMat = new Material(mobileDiffuse);
        carBodyMat.color = new Color(0.5f, 0.5f, 0.6f); 
        carGlassMat = new Material(mobileDiffuse);
        carGlassMat.color = new Color(0.1f, 0.2f, 0.3f);
        lampMat = new Material(mobileDiffuse);
        lampMat.color = Color.black;
        lampEmissive = new Material(unlitColor);
        lampEmissive.color = new Color(1f, 0.4f, 1f); 
    }

    Material CreateSecureMaterial(Shader s, string texPath, string name) {
        Material m = new Material(s);
        Texture2D tex = Resources.Load<Texture2D>(texPath);
        if (tex != null) m.mainTexture = tex;
        else LogAndCopy("MISSING ASSET: " + name + " texture at " + texPath);
        return m;
    }

    void LogAndCopy(string msg) {
        errorMessage = msg;
        GUIUtility.systemCopyBuffer = msg;
        Debug.LogWarning(msg);
    }

    void Update() {
        if (!isGameStarted) { if (Input.GetMouseButtonDown(0)) isGameStarted = true; return; }
        if (playerCar == null) return;

        float fps = 1.0f / Time.unscaledDeltaTime;
        diagnostics = string.Format("FPS: {0:F0} | Segs: {1} | Mem: {2}MB", fps, activeSegments.Count, System.GC.GetTotalMemory(false) / 1024 / 1024);

        Vector3 targetLanePos = playerCar.position;
        targetLanePos.x = Mathf.Clamp(targetLanePos.x + (Input.GetAxis("Horizontal") * 10f * Time.deltaTime), -8, 8); 
        playerCar.position = targetLanePos;
        
        mainCamera.position = playerCar.position + new Vector3(0, 3.5f, -8f);
        mainCamera.LookAt(playerCar.position + Vector3.up * 1.5f);

        if ((spawnZ - playerCar.position.z) < (initialSegments * segmentLength)) SpawnWorldSegment();
    }

    void SpawnWorldSegment() {
        GameObject root = new GameObject("Segment_" + spawnZ);
        root.transform.SetParent(environmentRoot);
        activeSegments.Add(root);
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Plane);
        road.transform.SetParent(root.transform);
        road.transform.localScale = new Vector3(3f, 1, 10.1f);
        road.transform.position = new Vector3(0, 0, spawnZ);
        road.GetComponent<Renderer>().material = roadMat;
        CreateBuilding(new Vector3(-45, 0, spawnZ), root.transform);
        CreateBuilding(new Vector3(45, 0, spawnZ), root.transform);
        if (activeSegments.Count > 25) {
            GameObject old = activeSegments[0]; activeSegments.RemoveAt(0); Destroy(old);
        }
        spawnZ += segmentLength;
    }

    void CreateBuilding(Vector3 pos, Transform parent) {
        float h = 100f + Random.value * 200f;
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.transform.SetParent(parent);
        b.transform.position = pos + new Vector3(0, h/2, 0);
        b.transform.localScale = new Vector3(35f, h, 35f);
        b.GetComponent<Renderer>().material = buildingMat; 
    }

    void CreatePlayerCar() {
        playerCar = new GameObject("PlayerCar").transform;
        playerCar.position = new Vector3(0, 0.5f, 0);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(playerCar); body.transform.localScale = new Vector3(2.2f, 0.8f, 5.0f); body.GetComponent<Renderer>().material = carBodyMat;
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

using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Prefabs (drag multiple for variety)")]
    public GameObject[] obstaclePrefabs;
    public float[] obstacleWeights;
    public GameObject scrapPrefab;

    [Header("Auto-discovery (fallback)")]
    [Tooltip("If obstaclePrefabs array is empty, scan inactive children of this GameObject for Obstacle components.")]
    public bool autoDiscoverChildren = true;

    [Header("Optional refs (auto-found if empty)")]
    public ShipHealth ship;
    public Transform shipFlame;

    [Header("Camera")]
    public Color backgroundColor = new Color(0.025f, 0.03f, 0.06f, 1f);

    [Header("Visuals")]
    public bool spawnStarfield = true;
    public int starCount = 100;

    GameManager gm;
    DifficultyManager diff;
    ObstacleSpawner obstacleSpawner;
    ScrapSpawner scrapSpawner;
    CanvasHUD hud;
    RadarController radar;
    AudioManager audioMgr;

    void Awake()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[GameBootstrap] No Main Camera in scene.");
            return;
        }

        CleanupLeftoverTextMeshes(cam);

        cam.backgroundColor = backgroundColor;
        cam.clearFlags = CameraClearFlags.SolidColor;

        if (cam.GetComponent<CameraShake>() == null) cam.gameObject.AddComponent<CameraShake>();

        if (ship == null) ship = FindObjectOfType<ShipHealth>();
        if (ship == null)
        {
            Debug.LogError("[GameBootstrap] No Ship with ShipHealth in scene.");
            return;
        }

        DiscoverObstaclePrefabs();

        gm = FindObjectOfType<GameManager>();
        if (gm == null) gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.shipHealth = ship;

        obstacleSpawner = FindObjectOfType<ObstacleSpawner>();
        if (obstacleSpawner == null) obstacleSpawner = new GameObject("ObstacleSpawner").AddComponent<ObstacleSpawner>();
        obstacleSpawner.obstaclePrefabs = obstaclePrefabs;
        obstacleSpawner.spawnWeights = obstacleWeights;
        obstacleSpawner.obstaclePrefab = (obstaclePrefabs != null && obstaclePrefabs.Length > 0) ? obstaclePrefabs[0] : null;

        scrapSpawner = FindObjectOfType<ScrapSpawner>();
        if (scrapSpawner == null) scrapSpawner = new GameObject("ScrapSpawner").AddComponent<ScrapSpawner>();
        scrapSpawner.scrapPrefab = scrapPrefab;
        scrapSpawner.ship = ship.transform;

        diff = FindObjectOfType<DifficultyManager>();
        if (diff == null) diff = new GameObject("DifficultyManager").AddComponent<DifficultyManager>();
        diff.spawner = obstacleSpawner;
        diff.scrapSpawner = scrapSpawner;
        diff.shipHealth = ship;

        hud = FindObjectOfType<CanvasHUD>();
        if (hud == null)
        {
            GameObject canvasGO = new GameObject("CanvasHUD");
            hud = canvasGO.AddComponent<CanvasHUD>();
        }
        hud.shipHealth = ship;
        hud.gameManager = gm;
        hud.diffManager = diff;

        if (FindObjectOfType<ScreenEffects>() == null)
            new GameObject("ScreenEffects").AddComponent<ScreenEffects>();

        radar = FindObjectOfType<RadarController>();
        if (radar == null) radar = new GameObject("Radar").AddComponent<RadarController>();
        radar.ship = ship.transform;
        radar.diffManager = diff;
        float halfX = cam.orthographicSize * cam.aspect;
        float halfY = cam.orthographicSize;
        radar.radarOffset = new Vector3(halfX - radar.radarRadius - 0.4f, -halfY + radar.radarRadius + 0.7f, 1f);
        radar.detectionRange = cam.orthographicSize * 2.4f;
        radar.warningRange = cam.orthographicSize * 1.2f;

        audioMgr = FindObjectOfType<AudioManager>();
        if (audioMgr != null)
        {
            audioMgr.shipHealth = ship;
            audioMgr.gameManager = gm;
            audioMgr.diffManager = diff;
        }

        if (FindObjectOfType<ExplosionFX>() == null)
            new GameObject("ExplosionFX").AddComponent<ExplosionFX>().shipHealth = ship;

        if (FindObjectOfType<ShipDamageFlash>() == null)
            ship.gameObject.AddComponent<ShipDamageFlash>();

        if (spawnStarfield && FindObjectOfType<Starfield>() == null)
        {
            Starfield sf = new GameObject("Starfield").AddComponent<Starfield>();
            sf.starCount = starCount;
        }

        new GameObject("RestartListener").AddComponent<RestartListener>().gameManager = gm;

        Debug.Log("[GameBootstrap] Ready. Obstacle prefabs: " + (obstaclePrefabs != null ? obstaclePrefabs.Length : 0) + ", Scrap prefab: " + (scrapPrefab != null ? scrapPrefab.name : "MISSING"));
    }

    void CleanupLeftoverTextMeshes(Camera cam)
    {
        TextMesh[] meshes = cam.GetComponentsInChildren<TextMesh>(true);
        int destroyed = 0;
        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i] != null && meshes[i].gameObject != cam.gameObject)
            {
                Destroy(meshes[i].gameObject);
                destroyed++;
            }
        }

        GameObject staleEnd = GameObject.Find("EndScreen");
        if (staleEnd != null)
        {
            Destroy(staleEnd);
            destroyed++;
        }

        if (destroyed > 0)
            Debug.Log("[GameBootstrap] Cleaned up " + destroyed + " leftover end-screen objects.");
    }

    void DiscoverObstaclePrefabs()
    {
        bool needsDiscover = obstaclePrefabs == null || obstaclePrefabs.Length == 0;
        if (needsDiscover && autoDiscoverChildren && transform.childCount > 0)
        {
            System.Collections.Generic.List<GameObject> found = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.GetComponent<Obstacle>() != null)
                {
                    child.gameObject.SetActive(false);
                    found.Add(child.gameObject);
                }
            }
            if (found.Count > 0)
            {
                obstaclePrefabs = found.ToArray();
                Debug.Log("[GameBootstrap] Auto-discovered " + found.Count + " obstacle prefabs from children.");
            }
        }

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            Debug.LogWarning("[GameBootstrap] No obstacle prefabs assigned. Drag prefabs into Obstacle Prefabs array, or parent them as inactive children.");
        }
        else
        {
            string names = "";
            for (int i = 0; i < obstaclePrefabs.Length; i++)
                names += (i > 0 ? ", " : "") + (obstaclePrefabs[i] != null ? obstaclePrefabs[i].name : "null");
            Debug.Log("[GameBootstrap] Using " + obstaclePrefabs.Length + " obstacle prefab(s): " + names);
        }
    }

    void Start()
    {
        if (gm != null) gm.StartGame();
    }
}

public class RestartListener : MonoBehaviour
{
    public GameManager gameManager;

    void Update()
    {
        if (gameManager == null) return;
        if (gameManager.CurrentState == GameManager.State.Won || gameManager.CurrentState == GameManager.State.Lost)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.R))
                gameManager.StartGame();
        }
    }
}

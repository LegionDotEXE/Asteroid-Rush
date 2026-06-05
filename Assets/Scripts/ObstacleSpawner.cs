using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle prefabs (drag in 1+ variants)")]
    public GameObject[] obstaclePrefabs;   // assign asteroid / satellite / hulk prefabs
    [Tooltip("How common each prefab is, matched by slot to Obstacle Prefabs. " +
             "Higher = more common. Leave empty for equal odds. " +
             "Example: asteroid 6, satellite 1, dead_ship 1.")]
    public float[] spawnWeights;
    public GameObject obstaclePrefab;       // optional fallback if the array is empty
    public int poolSize = 30;

    [Header("Driven by DifficultyManager")]
    public float speedMin = 6f,  speedMax = 8f;
    public float intervalMin = 1.2f, intervalMax = 1.2f;
    [Range(0,1)] public float clusterChance = 0f;
    public int perWave = 1;

    [Header("Size variety (scale, damage)")]
    public float smallScale = 0.7f, smallDamage = 14f;
    public float medScale   = 1.0f, medDamage   = 20f;
    public float bigScale   = 1.5f, bigDamage   = 27f;

    readonly List<GameObject> pool = new List<GameObject>();
    float spawnTimer;
    float spawnY, halfX;

    void Start()
    {
        float camZ = Mathf.Abs(Camera.main.transform.position.z);
        halfX  = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, camZ)).x;
        spawnY = Camera.main.ViewportToWorldPoint(new Vector3(0, 1.15f, camZ)).y;

        // collect (prefab, weight) pairs, keeping inspector slot alignment
        List<GameObject> prefabs = new List<GameObject>();
        List<float> weights = new List<float>();
        if (obstaclePrefabs != null)
        {
            for (int j = 0; j < obstaclePrefabs.Length; j++)
            {
                if (obstaclePrefabs[j] == null) continue;
                prefabs.Add(obstaclePrefabs[j]);
                float w = (spawnWeights != null && j < spawnWeights.Length && spawnWeights[j] > 0f)
                          ? spawnWeights[j] : 1f;
                weights.Add(w);
            }
        }
        if (prefabs.Count == 0 && obstaclePrefab != null)
        {
            prefabs.Add(obstaclePrefab); weights.Add(1f);
        }

        // build the pool proportional to weights (so common prefabs fill more slots)
        float total = 0f; foreach (var w in weights) total += w;
        for (int i = 0; i < prefabs.Count && total > 0f; i++)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(poolSize * weights[i] / total));
            for (int c = 0; c < count; c++)
            {
                GameObject o = Instantiate(prefabs[i]);
                o.SetActive(false);
                pool.Add(o);
            }
        }

        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted += ResetField;
    }
    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= ResetField;
    }

    void ResetField()
    {
        foreach (var o in pool) o.SetActive(false);
        spawnTimer = 0.4f;
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameActive()) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        spawnTimer = Random.Range(intervalMin, intervalMax);
        bool cluster = Random.value < clusterChance;
        int count = perWave + (cluster ? 2 : 0);

        if (cluster)
        {
            float cx = Random.Range(-halfX, halfX);
            for (int i = 0; i < count; i++)
                Spawn(Mathf.Clamp(cx + Random.Range(-1.5f, 1.5f), -halfX, halfX));
        }
        else
        {
            for (int i = 0; i < count; i++) Spawn(Random.Range(-halfX, halfX));
        }
    }

    void Spawn(float x)
    {
        GameObject o = GetPooled();
        if (o == null) return;

        float scale, damage, roll = Random.value;
        if (roll < 0.5f)       { scale = smallScale; damage = smallDamage; }
        else if (roll < 0.85f) { scale = medScale;   damage = medDamage; }
        else                   { scale = bigScale;   damage = bigDamage; }

        o.transform.position = new Vector3(x, spawnY, 0f);
        o.transform.rotation = Quaternion.identity;
        o.SetActive(true);
        o.GetComponent<Obstacle>().Launch(Random.Range(speedMin, speedMax), damage, scale);
    }

    // grab a RANDOM inactive pooled object; pool composition makes weights work
    GameObject GetPooled()
    {
        int start = Random.Range(0, pool.Count);
        for (int k = 0; k < pool.Count; k++)
        {
            GameObject o = pool[(start + k) % pool.Count];
            if (!o.activeSelf) return o;
        }
        return null;
    }

    public void SetPhase(float spMin, float spMax, float intMin, float intMax,
                         float cluster, int perW)
    {
        speedMin = spMin; speedMax = spMax;
        intervalMin = intMin; intervalMax = intMax;
        clusterChance = cluster; perWave = perW;
    }
}
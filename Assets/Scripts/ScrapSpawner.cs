using System.Collections.Generic;
using UnityEngine;

public class ScrapSpawner : MonoBehaviour
{
    public GameObject scrapPrefab;   // must have a Scrap component
    public int poolSize = 10;
    public Transform ship;           // homing target

    [Header("Driven by DifficultyManager")]
    public float intervalMin = 1.4f, intervalMax = 2.2f;
    public float speedMin = 4f, speedMax = 6f;

    readonly List<GameObject> pool = new List<GameObject>();
    float timer, spawnY, halfX;

    void Start()
    {
        float camZ = Mathf.Abs(Camera.main.transform.position.z);
        halfX  = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, camZ)).x;
        spawnY = Camera.main.ViewportToWorldPoint(new Vector3(0, 1.15f, camZ)).y;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject s = Instantiate(scrapPrefab);
            s.SetActive(false);
            pool.Add(s);
        }
        timer = Random.Range(intervalMin, intervalMax);

        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted += ResetField;
    }
    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= ResetField;
    }

    void ResetField()
    {
        foreach (var s in pool) s.SetActive(false);
        timer = Random.Range(intervalMin, intervalMax);
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameActive()) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Random.Range(intervalMin, intervalMax);
        Spawn();
    }

    void Spawn()
    {
        GameObject s = null;
        foreach (var o in pool) if (!o.activeSelf) { s = o; break; }
        if (s == null) return;

        s.transform.position = new Vector3(Random.Range(-halfX, halfX), spawnY, 0f);
        s.SetActive(true);
        s.GetComponent<Scrap>().Launch(Random.Range(speedMin, speedMax), ship);
    }

    public void SetPhase(float intMin, float intMax)
    {
        intervalMin = intMin; intervalMax = intMax;
    }
}
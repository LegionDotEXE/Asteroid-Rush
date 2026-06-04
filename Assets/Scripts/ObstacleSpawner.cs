using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public int poolSize = 20;
    public float spawnInterval = 1.2f;
    public float obstacleSpeed = 6f;

    List<GameObject> pool = new List<GameObject>();
    float spawnTimer;
    float screenHalfY;
    float spawnX;

    void Start()
    {
        float camZ = Mathf.Abs(Camera.main.transform.position.z);
        screenHalfY = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, camZ)).y;
        spawnX = Camera.main.ViewportToWorldPoint(new Vector3(1.2f, 0, camZ)).x;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(obstaclePrefab);
            obj.SetActive(false);
            pool.Add(obj);
        }
    }

    void Update()
    {
        if (!GameManager.Instance.IsGameActive()) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnObstacle();
            spawnTimer = spawnInterval;
        }

        foreach (GameObject obj in pool)
        {
            if (!obj.activeSelf) continue;
            obj.transform.position += Vector3.left * obstacleSpeed * Time.deltaTime;

            if (obj.transform.position.x < -spawnX)
                obj.SetActive(false);
        }
    }

    void SpawnObstacle()
    {
        GameObject obj = GetPooled();
        if (obj == null) return;

        float y = Random.Range(-screenHalfY, screenHalfY);
        obj.transform.position = new Vector3(spawnX, y, 0f);
        obj.SetActive(true);
    }

    GameObject GetPooled()
    {
        foreach (GameObject obj in pool)
        {
            if (!obj.activeSelf) return obj;
        }
        return null;
    }

    public void SetSpeed(float speed) => obstacleSpeed = speed;
    public void SetSpawnInterval(float interval) => spawnInterval = interval;
}

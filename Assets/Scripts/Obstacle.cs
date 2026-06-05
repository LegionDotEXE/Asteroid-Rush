using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public static readonly List<Obstacle> Active = new List<Obstacle>();

    [Tooltip("Collision/visual radius of the prefab at scale 1.")]
    public float baseRadius = 0.5f;

    [HideInInspector] public float speed  = 6f;
    [HideInInspector] public float damage = 14f;
    [HideInInspector] public float radius = 0.5f;

    float rotationSpeed;
    float despawnY;

    void OnEnable()  { Active.Add(this); }
    void OnDisable() { Active.Remove(this); }

    public void Launch(float speed, float damage, float scale)
    {
        this.speed  = speed;
        this.damage = damage;
        transform.localScale = Vector3.one * scale;
        radius = baseRadius * scale;
        rotationSpeed = Random.Range(-60f, 60f);

        float camZ = Mathf.Abs(Camera.main.transform.position.z);
        despawnY = Camera.main.ViewportToWorldPoint(new Vector3(0, -0.2f, camZ)).y;
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive()) return;

        transform.position += Vector3.down * speed * Time.deltaTime;
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (transform.position.y < despawnY) gameObject.SetActive(false);
    }
}
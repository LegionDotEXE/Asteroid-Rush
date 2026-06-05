using System.Collections.Generic;
using UnityEngine;

public class Scrap : MonoBehaviour
{
    public static readonly List<Scrap> Active = new List<Scrap>();

    public float speed = 5f;
    public float homingStrength = 2.5f;
    public float radius = 0.4f;
    public float pulseSpeed = 4f, pulseAmount = 0.15f;

    Transform ship;
    Vector3 baseScale;
    float despawnY, pulseT;

    void Awake()     { baseScale = transform.localScale; }
    void OnEnable()  { Active.Add(this); pulseT = Random.value * 6f; }
    void OnDisable() { Active.Remove(this); }

    public void Launch(float speed, Transform shipTransform)
    {
        this.speed = speed;
        ship = shipTransform;
        float camZ = Mathf.Abs(Camera.main.transform.position.z);
        despawnY = Camera.main.ViewportToWorldPoint(new Vector3(0, -0.2f, camZ)).y;
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive()) return;

        Vector3 move = Vector3.down * speed;
        if (ship != null)
            move += (ship.position - transform.position).normalized * homingStrength;
        transform.position += move * Time.deltaTime;

        pulseT += Time.deltaTime * pulseSpeed;
        transform.localScale = baseScale * (1f + Mathf.Sin(pulseT) * pulseAmount);

        if (transform.position.y < despawnY) gameObject.SetActive(false);
    }

    public void Collect() => gameObject.SetActive(false);
}
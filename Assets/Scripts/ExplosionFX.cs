using System.Collections;
using UnityEngine;

public class ExplosionFX : MonoBehaviour
{
    public ShipHealth shipHealth;
    public int particleCount = 18;
    public float duration = 1.0f;
    public float maxRadius = 2.5f;
    public Color innerColor = new Color(1f, 0.85f, 0.4f, 1f);
    public Color outerColor = new Color(0.95f, 0.3f, 0.15f, 0.8f);

    static Sprite particleSprite;
    SpriteRenderer[] shipRenderers;

    void Start()
    {
        if (particleSprite == null) particleSprite = BuildSprite();
        if (shipHealth != null)
        {
            shipRenderers = shipHealth.GetComponentsInChildren<SpriteRenderer>(true);
            shipHealth.OnDied += OnDied;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStarted += OnGameStarted;
    }

    void OnDestroy()
    {
        if (shipHealth != null) shipHealth.OnDied -= OnDied;
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= OnGameStarted;
    }

    void OnGameStarted()
    {
        StopAllCoroutines();
        if (shipRenderers != null)
        {
            for (int i = 0; i < shipRenderers.Length; i++)
            {
                if (shipRenderers[i] != null) shipRenderers[i].enabled = true;
            }
        }
    }

    void OnDied()
    {
        Vector3 pos = shipHealth.transform.position;
        StartCoroutine(Burst(pos));

        if (shipRenderers != null)
        {
            for (int i = 0; i < shipRenderers.Length; i++)
            {
                if (shipRenderers[i] != null) shipRenderers[i].enabled = false;
            }
        }

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.5f, 0.35f);
    }

    IEnumerator Burst(Vector3 center)
    {
        Transform[] parts = new Transform[particleCount];
        Vector2[] dirs = new Vector2[particleCount];
        float[] speeds = new float[particleCount];
        SpriteRenderer[] srs = new SpriteRenderer[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            GameObject go = new GameObject("ExpParticle");
            go.transform.position = center;
            float a = (i / (float)particleCount) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            speeds[i] = Random.Range(2f, maxRadius / duration * 1.4f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = particleSprite;
            sr.color = Color.Lerp(innerColor, outerColor, Random.value);
            sr.sortingOrder = 20;
            go.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);

            parts[i] = go.transform;
            srs[i] = sr;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;
            for (int i = 0; i < particleCount; i++)
            {
                if (parts[i] == null) continue;
                parts[i].position += (Vector3)(dirs[i] * speeds[i] * Time.deltaTime);
                Color c = srs[i].color;
                c.a = Mathf.Clamp01(1f - n);
                srs[i].color = c;
                parts[i].localScale *= (1f + Time.deltaTime * 0.6f);
            }
            yield return null;
        }

        for (int i = 0; i < particleCount; i++)
            if (parts[i] != null) Destroy(parts[i].gameObject);
    }

    static Sprite BuildSprite()
    {
        int res = 24;
        Texture2D tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        float r = res * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x - r, dy = y - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = d <= r ? Mathf.Pow(1f - d / r, 1.4f) : 0f;
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }
}

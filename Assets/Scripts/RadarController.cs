using System.Collections.Generic;
using UnityEngine;

public class RadarController : MonoBehaviour
{
    [Header("Refs")]
    public Transform ship;
    public DifficultyManager diffManager;

    [Header("Position")]
    public Vector3 radarOffset = new Vector3(3.6f, -3.6f, 0f);
    public float radarRadius = 0.95f;

    [Header("Range")]
    public float detectionRange = 14f;
    public float warningRange = 7f;

    [Header("Blips")]
    public int maxBlips = 24;
    public float blipSize = 0.12f;
    public Color blipFar = new Color(1f, 0.85f, 0.15f, 0.85f);
    public Color blipNear = new Color(1f, 0.25f, 0.15f, 1f);
    public Color blipScrap = new Color(0.3f, 0.95f, 0.65f, 0.9f);

    [Header("Frame")]
    public Color frameColor = new Color(0.4f, 0.8f, 1f, 0.6f);
    public Color sweepColor = new Color(0.4f, 0.8f, 1f, 0.35f);
    public float sweepSpeed = 90f;

    [Header("Refresh")]
    public float baseRefreshRate = 20f;

    Camera cam;
    SpriteRenderer frame, bg, sweep;
    List<SpriteRenderer> blipPool = new List<SpriteRenderer>();
    Sprite circleSprite;

    float sweepAngle;
    float refreshTimer;
    float currentRefreshRate;

    void Start()
    {
        cam = Camera.main;
        currentRefreshRate = baseRefreshRate;
        circleSprite = BuildCircleSprite();

        bg = MakeQuad("Radar_BG", new Color(0.05f, 0.07f, 0.10f, 0.65f), radarRadius * 2f, -9);
        frame = MakeQuad("Radar_Frame", frameColor, radarRadius * 2f, -8);
        frame.transform.localScale = Vector3.one * radarRadius * 2f * 1.04f;
        bg.transform.localScale = Vector3.one * radarRadius * 2f * 0.98f;

        sweep = MakeQuad("Radar_Sweep", sweepColor, radarRadius * 0.6f, -7);
        sweep.transform.localScale = new Vector3(radarRadius * 1.8f, 0.04f, 1f);

        for (int i = 0; i < maxBlips; i++)
        {
            SpriteRenderer b = MakeQuad("Blip_" + i, blipFar, blipSize, -6);
            b.transform.localScale = Vector3.one * blipSize;
            b.gameObject.SetActive(false);
            blipPool.Add(b);
        }

        if (diffManager != null)
            diffManager.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDestroy()
    {
        if (diffManager != null)
            diffManager.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(int tier, DifficultyManager.Phase phase)
    {
        currentRefreshRate = baseRefreshRate / (1f + tier * 0.35f);
    }

    void Update()
    {
        sweepAngle += sweepSpeed * Time.deltaTime;
        if (sweepAngle > 360f) sweepAngle -= 360f;
        sweep.transform.localRotation = Quaternion.Euler(0f, 0f, sweepAngle);

        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f) return;
        refreshTimer = 1f / Mathf.Max(1f, currentRefreshRate);

        RebuildBlips();
    }

    void RebuildBlips()
    {
        foreach (var b in blipPool) b.gameObject.SetActive(false);

        if (ship == null) return;

        int idx = 0;

        for (int i = 0; i < Obstacle.Active.Count && idx < blipPool.Count; i++)
        {
            Obstacle o = Obstacle.Active[i];
            Vector3 toObj = o.transform.position - ship.position;
            float dist = toObj.magnitude;
            if (dist > detectionRange) continue;

            PlaceBlip(idx++, toObj, dist, false);
        }

        for (int i = 0; i < Scrap.Active.Count && idx < blipPool.Count; i++)
        {
            Scrap s = Scrap.Active[i];
            Vector3 toObj = s.transform.position - ship.position;
            float dist = toObj.magnitude;
            if (dist > detectionRange) continue;

            PlaceBlip(idx++, toObj, dist, true);
        }
    }

    void PlaceBlip(int idx, Vector3 toObj, float dist, bool isScrap)
    {
        SpriteRenderer b = blipPool[idx];
        b.gameObject.SetActive(true);

        float norm = Mathf.Clamp01(dist / detectionRange);
        Vector2 dir = new Vector2(toObj.x, toObj.y).normalized;
        Vector3 localPos = (Vector3)(dir * norm * radarRadius);

        b.transform.localPosition = (Vector3)radarOffset + localPos;

        if (isScrap)
        {
            b.color = blipScrap;
            b.transform.localScale = Vector3.one * blipSize * 0.85f;
        }
        else
        {
            float t = 1f - Mathf.Clamp01(dist / warningRange);
            b.color = Color.Lerp(blipFar, blipNear, t);
            float pulse = dist < warningRange ? (1f + Mathf.Sin(Time.time * 12f) * 0.25f) : 1f;
            b.transform.localScale = Vector3.one * blipSize * pulse;
        }
    }

    SpriteRenderer MakeQuad(string objName, Color color, float size, int order)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(cam.transform);
        go.transform.localPosition = radarOffset;
        go.transform.localScale = Vector3.one * size;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    Sprite BuildCircleSprite()
    {
        int res = 64;
        Texture2D tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        float r = res * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x - r, dy = y - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, d <= r ? Color.white : new Color(0, 0, 0, 0));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }
}

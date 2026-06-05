using UnityEngine;

public class Starfield : MonoBehaviour
{
    public int starCount = 80;
    public float minSpeed = 0.4f;
    public float maxSpeed = 2.2f;
    public float minSize = 0.025f;
    public float maxSize = 0.09f;
    public Color nearStarColor = new Color(1f, 1f, 1f, 1f);
    public Color farStarColor = new Color(0.6f, 0.7f, 1f, 0.4f);

    Camera cam;
    Transform[] stars;
    float[] speeds;
    float halfX, halfY;
    static Sprite circleSprite;

    void Start()
    {
        cam = Camera.main;
        halfY = cam.orthographicSize;
        halfX = halfY * cam.aspect;

        if (circleSprite == null) circleSprite = BuildCircleSprite();

        stars = new Transform[starCount];
        speeds = new float[starCount];

        for (int i = 0; i < starCount; i++)
        {
            GameObject go = new GameObject("Star_" + i);
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(
                Random.Range(-halfX, halfX),
                Random.Range(-halfY, halfY),
                10f
            );

            float depth = Random.value;
            float size = Mathf.Lerp(minSize, maxSize, depth);
            go.transform.localScale = Vector3.one * size;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circleSprite;
            sr.color = Color.Lerp(farStarColor, nearStarColor, depth);
            sr.sortingOrder = -50;

            stars[i] = go.transform;
            speeds[i] = Mathf.Lerp(minSpeed, maxSpeed, depth);
        }
    }

    void Update()
    {
        for (int i = 0; i < stars.Length; i++)
        {
            Vector3 p = stars[i].position;
            p.y -= speeds[i] * Time.deltaTime;
            if (p.y < -halfY)
            {
                p.y = halfY;
                p.x = Random.Range(-halfX, halfX);
            }
            stars[i].position = p;
        }
    }

    static Sprite BuildCircleSprite()
    {
        int res = 16;
        Texture2D tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        float r = res * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x - r, dy = y - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = d <= r ? Mathf.Clamp01(1f - d / r) : 0f;
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }
}

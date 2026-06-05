using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenEffects : MonoBehaviour
{
    public static ScreenEffects Instance { get; private set; }

    Image flashOverlay;
    float flashTimer;
    float flashDuration;
    Color flashColor;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        BuildFlashOverlay();
    }

    void Update()
    {
        if (flashTimer > 0f && flashOverlay != null)
        {
            flashTimer -= Time.deltaTime;
            float n = Mathf.Clamp01(flashTimer / flashDuration);
            Color c = flashColor;
            c.a = flashColor.a * n;
            flashOverlay.color = c;
            if (flashTimer <= 0f) flashOverlay.color = Color.clear;
        }
    }

    public void Flash(Color color, float duration)
    {
        flashColor = color;
        flashDuration = Mathf.Max(0.01f, duration);
        flashTimer = flashDuration;
        if (flashOverlay != null) flashOverlay.color = color;
    }

    public void Shake(float magnitude, float duration)
    {
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(duration, magnitude);
    }

    void BuildFlashOverlay()
    {
        Canvas existing = FindObjectOfType<Canvas>();
        Canvas canvas = existing;
        if (canvas == null)
        {
            GameObject cgo = new GameObject("FlashCanvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            cgo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        GameObject go = new GameObject("ScreenFlash");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        flashOverlay = go.AddComponent<Image>();
        flashOverlay.color = Color.clear;
        flashOverlay.raycastTarget = false;

        Canvas c = go.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 999;
        go.AddComponent<GraphicRaycaster>();
    }
}

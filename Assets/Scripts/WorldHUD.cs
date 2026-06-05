using System.Collections;
using UnityEngine;

public class WorldHUD : MonoBehaviour
{
    [Header("Refs")]
    public ShipHealth shipHealth;
    public GameManager gameManager;
    public DifficultyManager diffManager;

    [Header("Health Bar")]
    public float barWidth = 3.5f;
    public float barHeight = 0.18f;
    public Vector3 healthBarOffset = new Vector3(-3.8f, -4.2f, 0f);

    [Header("Timer Bar")]
    public Vector3 timerBarOffset = new Vector3(-3.8f, -4.55f, 0f);

    [Header("Colors")]
    public Color healthFull    = new Color(0.28f, 0.85f, 0.45f);
    public Color healthMid     = new Color(0.95f, 0.75f, 0.15f);
    public Color healthLow     = new Color(0.90f, 0.22f, 0.22f);
    public Color timerColor    = new Color(0.35f, 0.75f, 1.00f);
    public Color regenColor    = new Color(0.30f, 0.95f, 0.65f);
    public Color bgColor       = new Color(0.08f, 0.08f, 0.10f, 0.75f);

    [Header("Vignette")]
    public VignetteOverlay vignetteRenderer;
    public float vignetteFlashSpeed = 8f;

    [Header("Phase Label")]
    public TextMesh phaseLabel;

    SpriteRenderer hpBg, hpFill, hpRegen;
    SpriteRenderer timerBg, timerFill;
    Camera cam;

    float currentHullNorm = 1f;
    float vignetteAlpha = 0f;
    bool vignetteFlashing = false;

    void Start()
    {
        cam = Camera.main;

        hpBg     = MakeBar("HUD_HpBg",     bgColor,      barWidth,       barHeight,       healthBarOffset, -9);
        hpFill   = MakeBar("HUD_HpFill",   healthFull,   barWidth,       barHeight,       healthBarOffset, -8);
        hpRegen  = MakeBar("HUD_HpRegen",  regenColor,   0f,             barHeight * 0.5f, healthBarOffset + Vector3.up * (barHeight * 0.15f), -7);

        timerBg   = MakeBar("HUD_TimerBg",   bgColor,    barWidth, barHeight * 0.55f, timerBarOffset, -9);
        timerFill = MakeBar("HUD_TimerFill", timerColor, barWidth, barHeight * 0.55f, timerBarOffset, -8);

        if (vignetteRenderer != null)
        {
            //vignetteRenderer.color = new Color(1f, 0.1f, 0.1f, 0f);
            vignetteRenderer.SetAlpha(0f);

        }

        if (phaseLabel != null)
        {
            phaseLabel.text = "";
            phaseLabel.color = new Color(1f, 0.85f, 0.2f, 0f);
        }

        if (shipHealth != null)
        {
            shipHealth.OnHullChanged    += OnHullChanged;
            shipHealth.OnDamaged        += OnDamaged;
            shipHealth.OnScrapCollected += OnScrapCollected;
        }

        if (diffManager != null)
            diffManager.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDestroy()
    {
        if (shipHealth != null)
        {
            shipHealth.OnHullChanged    -= OnHullChanged;
            shipHealth.OnDamaged        -= OnDamaged;
            shipHealth.OnScrapCollected -= OnScrapCollected;
        }
        if (diffManager != null)
            diffManager.OnPhaseChanged -= OnPhaseChanged;
    }

    void Update()
    {
        UpdateHealthBar();
        UpdateTimerBar();
        UpdateVignette();
        UpdateRegenBar();
    }

    void UpdateHealthBar()
    {
        float t = currentHullNorm;
        SetBarFill(hpFill, t);

        Color c = t > 0.5f
            ? Color.Lerp(healthMid, healthFull, (t - 0.5f) * 2f)
            : Color.Lerp(healthLow, healthMid,  t * 2f);
        hpFill.color = c;
    }

    void UpdateTimerBar()
    {
        if (gameManager == null) return;
        float t = gameManager.GetTimeRemaining() / gameManager.gameDuration;
        SetBarFill(timerFill, Mathf.Clamp01(t));
    }

    void UpdateVignette()
    {
        if (vignetteRenderer == null) return;

        float targetAlpha = 0f;
        if (currentHullNorm < 0.5f)
        {
            targetAlpha = Mathf.Lerp(0f, 0.55f, 1f - currentHullNorm * 2f);
            if (currentHullNorm < 0.15f && vignetteFlashing)
                targetAlpha += Mathf.Abs(Mathf.Sin(Time.time * vignetteFlashSpeed)) * 0.35f;
        }

        vignetteAlpha = Mathf.Lerp(vignetteAlpha, targetAlpha, Time.deltaTime * 6f);
        //vignetteRenderer.color = new Color(1f, 0.08f, 0.08f, Mathf.Clamp01(vignetteAlpha));
        vignetteRenderer.SetColor(new Color(1f, 0.08f, 0.08f, Mathf.Clamp01(vignetteAlpha)));
    }

    void UpdateRegenBar()
    {
        if (shipHealth == null) return;
        float regenNorm = Mathf.Clamp01(shipHealth.RegenPool / shipHealth.healPerScrap);
        SetBarFill(hpRegen, regenNorm);

        Vector3 p = hpRegen.transform.position;
        p.x = healthBarOffset.x - barWidth * 0.5f + (barWidth * currentHullNorm) * 0.5f + (barWidth * regenNorm * 0.5f);
        hpRegen.transform.position = p;
    }

    void OnHullChanged(float norm)
    {
        currentHullNorm = norm;
        vignetteFlashing = norm < 0.15f;
    }

    void OnDamaged(float amount)
    {
        StartCoroutine(DamageFlash());
    }

    void OnScrapCollected()
    {
        StartCoroutine(ScrapPulse());
    }

    void OnPhaseChanged(int tier, DifficultyManager.Phase phase)
    {
        if (phaseLabel != null)
            StartCoroutine(ShowPhaseLabel(phase.warning));
    }

    IEnumerator DamageFlash()
    {
        if (vignetteRenderer == null) yield break;
        vignetteAlpha = 0.7f;
        yield return new WaitForSeconds(0.08f);
    }

    IEnumerator ScrapPulse()
    {
        Color orig = hpRegen.color;
        hpRegen.color = Color.white;
        yield return new WaitForSeconds(0.12f);
        hpRegen.color = orig;
    }

    IEnumerator ShowPhaseLabel(string text)
    {
        if (phaseLabel == null) yield break;
        phaseLabel.text = text;
        float t = 0f;
        while (t < 0.3f) { t += Time.deltaTime; phaseLabel.color = new Color(1f, 0.85f, 0.2f, t / 0.3f); yield return null; }
        yield return new WaitForSeconds(1.4f);
        t = 0f;
        while (t < 0.5f) { t += Time.deltaTime; phaseLabel.color = new Color(1f, 0.85f, 0.2f, 1f - t / 0.5f); yield return null; }
        phaseLabel.text = "";
    }

    void SetBarFill(SpriteRenderer sr, float fill)
    {
        fill = Mathf.Clamp01(fill);
        Vector3 s = sr.transform.localScale;
        s.x = barWidth * fill;
        sr.transform.localScale = s;

        Vector3 p = sr.transform.position;
        p.x = healthBarOffset.x - barWidth * 0.5f + (barWidth * fill * 0.5f);
        sr.transform.position = p;
    }

    SpriteRenderer MakeBar(string objName, Color color, float width, float height, Vector3 offset, int order)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(cam.transform);
        go.transform.localPosition = offset;
        go.transform.localScale = new Vector3(width, height, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWhiteSprite();
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}

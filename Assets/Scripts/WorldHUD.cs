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
    public float barHeight = 0.22f;
    public Vector3 healthBarOffset = new Vector3(-3.4f, -4.0f, 1f);

    [Header("Timer Bar")]
    public Vector3 timerBarOffset = new Vector3(-3.4f, -4.4f, 1f);

    [Header("Colors")]
    public Color healthFull    = new Color(0.28f, 0.85f, 0.45f);
    public Color healthMid     = new Color(0.95f, 0.75f, 0.15f);
    public Color healthLow     = new Color(0.90f, 0.22f, 0.22f);
    public Color timerColor    = new Color(0.35f, 0.75f, 1.00f);
    public Color regenColor    = new Color(0.30f, 0.95f, 0.65f);
    public Color bgColor       = new Color(0.05f, 0.05f, 0.08f, 0.8f);

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
    float damageFlashTimer = 0f;
    bool active = true;

    void Start()
    {
        cam = Camera.main;

        hpBg     = MakeBar("HUD_HpBg",     bgColor,    barWidth, barHeight, healthBarOffset, -9);
        hpFill   = MakeBar("HUD_HpFill",   healthFull, barWidth, barHeight, healthBarOffset, -8);
        hpRegen  = MakeBar("HUD_HpRegen",  regenColor, barWidth, barHeight * 0.6f, healthBarOffset, -7);
        hpRegen.color = new Color(regenColor.r, regenColor.g, regenColor.b, 0.55f);

        timerBg   = MakeBar("HUD_TimerBg",   bgColor,    barWidth, barHeight * 0.55f, timerBarOffset, -9);
        timerFill = MakeBar("HUD_TimerFill", timerColor, barWidth, barHeight * 0.55f, timerBarOffset, -8);

        if (vignetteRenderer != null) vignetteRenderer.SetAlpha(0f);

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

        if (gameManager != null)
        {
            gameManager.OnGameStarted += () => { active = true; };
            gameManager.OnGameWon     += () => { active = false; };
            gameManager.OnGameLost    += () => { active = false; };
        }
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
        UpdateRegenBar();
        UpdateTimerBar();
        UpdateVignette();
    }

    void UpdateHealthBar()
    {
        float t = currentHullNorm;
        SetBarFill(hpFill, healthBarOffset, t);

        Color c = t > 0.5f
            ? Color.Lerp(healthMid, healthFull, (t - 0.5f) * 2f)
            : Color.Lerp(healthLow, healthMid,  t * 2f);
        hpFill.color = c;
    }

    void UpdateRegenBar()
    {
        if (shipHealth == null) return;
        float regenNorm = Mathf.Clamp01(shipHealth.RegenPool / Mathf.Max(0.01f, shipHealth.maxHull * 0.5f));
        float startFrac = currentHullNorm;
        float endFrac = Mathf.Clamp01(startFrac + regenNorm);
        float width = endFrac - startFrac;

        Vector3 s = hpRegen.transform.localScale;
        s.x = barWidth * width;
        s.y = barHeight * 0.6f;
        hpRegen.transform.localScale = s;

        Vector3 p = healthBarOffset;
        p.x = healthBarOffset.x - barWidth * 0.5f + barWidth * (startFrac + width * 0.5f);
        hpRegen.transform.localPosition = p;
    }

    void UpdateTimerBar()
    {
        if (gameManager == null) return;
        float t = gameManager.GetTimeRemaining() / gameManager.gameDuration;
        SetBarFill(timerFill, timerBarOffset, Mathf.Clamp01(t));
    }

    void UpdateVignette()
    {
        if (vignetteRenderer == null) return;
        if (!active) return;

        float targetAlpha = 0f;
        if (currentHullNorm < 0.5f)
        {
            targetAlpha = Mathf.Lerp(0f, 0.55f, 1f - currentHullNorm * 2f);
            if (currentHullNorm < 0.18f && vignetteFlashing)
                targetAlpha += Mathf.Abs(Mathf.Sin(Time.time * vignetteFlashSpeed)) * 0.35f;
        }

        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;
            targetAlpha = Mathf.Max(targetAlpha, 0.7f * (damageFlashTimer / 0.12f));
        }

        vignetteAlpha = Mathf.Lerp(vignetteAlpha, targetAlpha, Time.deltaTime * 6f);
        vignetteRenderer.SetColor(new Color(1f, 0.08f, 0.08f, Mathf.Clamp01(vignetteAlpha)));
    }

    void OnHullChanged(float norm)
    {
        currentHullNorm = norm;
        vignetteFlashing = norm < 0.18f;
    }

    void OnDamaged(float amount)
    {
        damageFlashTimer = 0.12f;
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

    void SetBarFill(SpriteRenderer sr, Vector3 anchorOffset, float fill)
    {
        fill = Mathf.Clamp01(fill);
        Vector3 s = sr.transform.localScale;
        s.x = barWidth * fill;
        sr.transform.localScale = s;

        Vector3 p = anchorOffset;
        p.x = anchorOffset.x - barWidth * 0.5f + barWidth * fill * 0.5f;
        sr.transform.localPosition = p;
    }

    SpriteRenderer MakeBar(string objName, Color color, float width, float height, Vector3 offset, int order)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(cam.transform);
        go.transform.localPosition = offset;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(width, height, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWhiteSprite();
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    static Sprite cachedWhite;
    Sprite CreateWhiteSprite()
    {
        if (cachedWhite != null) return cachedWhite;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        cachedWhite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return cachedWhite;
    }
}

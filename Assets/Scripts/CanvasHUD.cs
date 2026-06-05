using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CanvasHUD : MonoBehaviour
{
    public static CanvasHUD Instance { get; private set; }

    public ShipHealth shipHealth;
    public GameManager gameManager;
    public DifficultyManager diffManager;

    Image hullFill, hullTrack;
    Image regenFill;
    Image timerFill;
    Text hullText, timerText, phaseText, streakText;
    RectTransform dmgParent;

    GameObject hullPanelGO, timerPanelGO, phaseGO, streakGO;
    GameObject winPanel, lossPanel;

    float hullTarget = 1f;
    float timerTarget = 1f;
    int scrapStreak = 0;

    Coroutine phaseRoutine;
    Coroutine streakRoutine;
    bool active = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        BuildUI();

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
            gameManager.OnGameStarted += OnGameStarted;
            gameManager.OnGameWon     += () => StartCoroutine(ShowPanel(winPanel, 0.9f));
            gameManager.OnGameLost    += () => StartCoroutine(ShowPanel(lossPanel, 0.7f));
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
        if (hullFill != null)
        {
            hullFill.fillAmount = Mathf.Lerp(hullFill.fillAmount, hullTarget, Time.deltaTime * 10f);
            float p = hullFill.fillAmount;
            Color baseGreen = new Color(0.28f, 0.85f, 0.45f);
            Color mid = new Color(0.95f, 0.75f, 0.15f);
            Color low = new Color(0.95f, 0.2f, 0.18f);
            if (p < 0.25f)
            {
                hullFill.color = Color.Lerp(low, new Color(1f, 0.55f, 0.4f),
                    (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f);
            }
            else if (p < 0.55f)
            {
                hullFill.color = Color.Lerp(low, mid, (p - 0.25f) / 0.30f);
            }
            else
            {
                hullFill.color = Color.Lerp(mid, baseGreen, (p - 0.55f) / 0.45f);
            }

            if (hullText != null && shipHealth != null)
                hullText.text = Mathf.CeilToInt(hullTarget * shipHealth.maxHull) + "  /  " + (int)shipHealth.maxHull;
        }

        if (regenFill != null && shipHealth != null)
        {
            float regenNorm = Mathf.Clamp01(shipHealth.RegenPool / Mathf.Max(0.01f, shipHealth.maxHull * 0.5f));
            float overlap = Mathf.Clamp01(hullFill.fillAmount + regenNorm);
            regenFill.fillAmount = Mathf.Lerp(regenFill.fillAmount, overlap, Time.deltaTime * 12f);
        }

        if (timerFill != null && gameManager != null)
        {
            timerTarget = gameManager.GetTimeRemaining() / gameManager.gameDuration;
            timerFill.fillAmount = Mathf.Lerp(timerFill.fillAmount, timerTarget, Time.deltaTime * 8f);

            float remain = gameManager.GetTimeRemaining();
            Color cool = new Color(0.35f, 0.75f, 1f);
            Color warm = new Color(1f, 0.55f, 0.15f);
            Color danger = new Color(1f, 0.18f, 0.15f);
            if (remain < 6f)
                timerFill.color = Color.Lerp(danger, new Color(1f, 0.85f, 0.4f),
                    (Mathf.Sin(Time.time * 9f) + 1f) * 0.5f);
            else if (remain < 13f)
                timerFill.color = Color.Lerp(warm, danger, 1f - (remain - 6f) / 7f);
            else
                timerFill.color = cool;

            if (timerText != null)
                timerText.text = Mathf.CeilToInt(remain) + "s";
        }
    }

    void OnGameStarted()
    {
        active = true;
        scrapStreak = 0;
        if (streakText != null) streakText.color = Color.clear;
        if (winPanel != null) winPanel.SetActive(false);
        if (lossPanel != null) lossPanel.SetActive(false);
        SetGameplayHUDVisible(true);
    }

    void SetGameplayHUDVisible(bool visible)
    {
        if (hullPanelGO != null) hullPanelGO.SetActive(visible);
        if (timerPanelGO != null) timerPanelGO.SetActive(visible);
        if (phaseGO != null) phaseGO.SetActive(visible);
        if (streakGO != null) streakGO.SetActive(visible);
        if (dmgParent != null) dmgParent.gameObject.SetActive(visible);
    }

    void OnHullChanged(float norm)
    {
        hullTarget = Mathf.Clamp01(norm);
    }

    void OnDamaged(float amount)
    {
        scrapStreak = 0;
        if (streakText != null) streakText.color = Color.clear;
        SpawnDamageNumber("-" + Mathf.CeilToInt(amount), shipHealth.transform.position + Vector3.up * 0.8f,
            new Color(1f, 0.2f, 0.15f), 36);
        if (ScreenEffects.Instance != null)
            ScreenEffects.Instance.Flash(new Color(0.85f, 0.1f, 0.1f, 0.4f), 0.18f);
    }

    void OnScrapCollected()
    {
        scrapStreak++;
        SpawnDamageNumber("+REPAIR", shipHealth.transform.position + Vector3.up * 0.9f,
            new Color(0.4f, 0.95f, 0.6f), 28);
        if (ScreenEffects.Instance != null)
            ScreenEffects.Instance.Flash(new Color(0.3f, 0.85f, 0.55f, 0.18f), 0.12f);
        if (scrapStreak >= 2 && streakText != null)
        {
            streakText.text = "SCRAP STREAK  x" + scrapStreak;
            streakText.color = scrapStreak >= 5
                ? new Color(1f, 0.85f, 0.2f)
                : new Color(0.5f, 0.95f, 0.7f);
            if (streakRoutine != null) StopCoroutine(streakRoutine);
            streakRoutine = StartCoroutine(StreakPulse());
        }
    }

    void OnPhaseChanged(int tier, DifficultyManager.Phase phase)
    {
        if (phaseRoutine != null) StopCoroutine(phaseRoutine);
        phaseRoutine = StartCoroutine(PhaseFlash(phase.warning, tier));
        if (ScreenEffects.Instance != null)
        {
            Color c = tier == 0 ? new Color(0.4f, 0.8f, 1f, 0.2f)
                    : tier == 1 ? new Color(1f, 0.65f, 0.2f, 0.28f)
                                : new Color(1f, 0.2f, 0.15f, 0.35f);
            ScreenEffects.Instance.Flash(c, 0.5f);
        }
    }

    IEnumerator PhaseFlash(string text, int tier)
    {
        if (phaseText == null) yield break;
        phaseText.text = text;
        Color target = tier == 0 ? new Color(0.55f, 0.85f, 1f)
                    : tier == 1 ? new Color(1f, 0.7f, 0.25f)
                                : new Color(1f, 0.3f, 0.25f);
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float a = t / 0.25f;
            phaseText.color = new Color(target.r, target.g, target.b, a);
            phaseText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, a);
            yield return null;
        }
        for (int i = 0; i < 3; i++)
        {
            phaseText.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            phaseText.color = target;
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(1.2f);
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float a = 1f - (t / 0.5f);
            phaseText.color = new Color(target.r, target.g, target.b, a);
            yield return null;
        }
        phaseText.text = "";
    }

    IEnumerator StreakPulse()
    {
        if (streakText == null) yield break;
        Vector3 baseScale = Vector3.one;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            streakText.rectTransform.localScale = baseScale * Mathf.Lerp(1.4f, 1f, t / 0.2f);
            yield return null;
        }
        streakText.rectTransform.localScale = baseScale;
    }

    public void SpawnDamageNumber(string text, Vector3 worldPos, Color color, int fontSize)
    {
        if (dmgParent == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 screen = cam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dmgParent, screen, null, out Vector2 local);

        GameObject go = new GameObject("Dmg");
        go.transform.SetParent(dmgParent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = local;
        rt.sizeDelta = new Vector2(160, 60);

        Text t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        go.AddComponent<Outline>().effectColor = Color.black;

        StartCoroutine(FloatNumber(rt, t));
    }

    IEnumerator FloatNumber(RectTransform rt, Text t)
    {
        Vector2 start = rt.anchoredPosition;
        float e = 0f;
        float xDrift = Random.Range(-12f, 12f);
        while (e < 0.95f)
        {
            e += Time.deltaTime;
            rt.anchoredPosition = start + new Vector2(xDrift * e, e * 70f);
            Color c = t.color;
            c.a = 1f - Mathf.Clamp01((e - 0.4f) / 0.5f);
            t.color = c;
            rt.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, Mathf.Clamp01(e / 0.2f));
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    IEnumerator ShowPanel(GameObject panel, float delay)
    {
        if (panel == null) yield break;
        active = false;
        yield return new WaitForSeconds(delay);
        SetGameplayHUDVisible(false);
        panel.SetActive(true);

        Image bg = panel.GetComponent<Image>();
        if (bg != null)
        {
            Color target = bg.color;
            bg.color = new Color(target.r, target.g, target.b, 0f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                bg.color = new Color(target.r, target.g, target.b, Mathf.Clamp01(t) * target.a);
                yield return null;
            }
        }

        foreach (Text tx in panel.GetComponentsInChildren<Text>(true))
        {
            Color tc = tx.color;
            tx.color = new Color(tc.r, tc.g, tc.b, 0f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                tx.color = new Color(tc.r, tc.g, tc.b, Mathf.Clamp01(t));
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void BuildUI()
    {
        Canvas cv = GetComponent<Canvas>();
        if (cv == null) cv = gameObject.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;

        CanvasScaler cs = gameObject.GetComponent<CanvasScaler>();
        if (cs == null) cs = gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        RectTransform root = GetComponent<RectTransform>();

        BuildHullBar(root);
        BuildTimerBar(root);
        BuildPhaseLabel(root);
        BuildStreakLabel(root);
        BuildDmgLayer(root);

        winPanel = MakePanel(root, "ESCAPED",
            new Color(0.4f, 0.95f, 0.55f), new Color(0.05f, 0.12f, 0.08f, 0.92f),
            "press  SPACE  to start");

        lossPanel = MakePanel(root, "ENGINE FAILURE",
            new Color(1f, 0.25f, 0.18f), new Color(0.12f, 0.04f, 0.04f, 0.92f),
            "press  SPACE  to start");
    }

    void BuildHullBar(RectTransform root)
    {
        GameObject panel = new GameObject("HullHP");
        panel.transform.SetParent(root, false);
        hullPanelGO = panel;
        RectTransform pRT = panel.AddComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = pRT.pivot = Vector2.zero;
        pRT.anchoredPosition = new Vector2(28f, 28f);
        pRT.sizeDelta = new Vector2(380f, 84f);
        panel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.93f);

        MakeChildText(panel.transform, "Label", "HULL", 19, FontStyle.Bold,
            new Color(0.88f, 0.86f, 0.78f),
            new Vector2(0f, 0.55f), new Vector2(0.55f, 1f),
            new Vector2(14f, 2f), new Vector2(0f, -2f),
            TextAnchor.MiddleLeft);

        hullText = MakeChildText(panel.transform, "HPNum", "100  /  100", 16, FontStyle.Normal,
            new Color(0.75f, 0.72f, 0.65f),
            new Vector2(0.45f, 0.55f), new Vector2(1f, 1f),
            new Vector2(0f, 2f), new Vector2(-14f, -2f),
            TextAnchor.MiddleRight);

        GameObject track = new GameObject("Track");
        track.transform.SetParent(panel.transform, false);
        RectTransform tRT = track.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0f);
        tRT.anchorMax = new Vector2(1f, 0.5f);
        tRT.offsetMin = new Vector2(10f, 8f);
        tRT.offsetMax = new Vector2(-10f, -2f);
        hullTrack = track.AddComponent<Image>();
        hullTrack.color = new Color(0.03f, 0.03f, 0.04f, 1f);

        GameObject rgo = new GameObject("Regen");
        rgo.transform.SetParent(track.transform, false);
        RectTransform rRT = rgo.AddComponent<RectTransform>();
        rRT.anchorMin = Vector2.zero;
        rRT.anchorMax = Vector2.one;
        rRT.offsetMin = new Vector2(2f, 2f);
        rRT.offsetMax = new Vector2(-2f, -2f);
        regenFill = rgo.AddComponent<Image>();
        regenFill.color = new Color(0.3f, 0.95f, 0.65f, 0.45f);
        regenFill.type = Image.Type.Filled;
        regenFill.fillMethod = Image.FillMethod.Horizontal;
        regenFill.fillAmount = 1f;

        GameObject fgo = new GameObject("Fill");
        fgo.transform.SetParent(track.transform, false);
        RectTransform fRT = fgo.AddComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;
        fRT.offsetMin = new Vector2(2f, 2f);
        fRT.offsetMax = new Vector2(-2f, -2f);
        hullFill = fgo.AddComponent<Image>();
        hullFill.color = new Color(0.28f, 0.85f, 0.45f);
        hullFill.type = Image.Type.Filled;
        hullFill.fillMethod = Image.FillMethod.Horizontal;
        hullFill.fillAmount = 1f;
    }

    void BuildTimerBar(RectTransform root)
    {
        GameObject panel = new GameObject("EscapeTimer");
        panel.transform.SetParent(root, false);
        timerPanelGO = panel;
        RectTransform pRT = panel.AddComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = pRT.pivot = new Vector2(0.5f, 1f);
        pRT.anchoredPosition = new Vector2(0f, -28f);
        pRT.sizeDelta = new Vector2(1000f, 82f);
        panel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.93f);

        MakeChildText(panel.transform, "Label", "ESCAPE", 22, FontStyle.Bold,
            new Color(0.88f, 0.86f, 0.78f),
            new Vector2(0f, 0.55f), new Vector2(0.5f, 1f),
            new Vector2(18f, 2f), new Vector2(0f, -2f),
            TextAnchor.MiddleLeft);

        timerText = MakeChildText(panel.transform, "TimerNum", "30s", 22, FontStyle.Bold,
            new Color(1f, 0.95f, 0.85f),
            new Vector2(0.5f, 0.55f), new Vector2(1f, 1f),
            new Vector2(0f, 2f), new Vector2(-18f, -2f),
            TextAnchor.MiddleRight);

        GameObject track = new GameObject("Track");
        track.transform.SetParent(panel.transform, false);
        RectTransform tRT = track.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0f);
        tRT.anchorMax = new Vector2(1f, 0.5f);
        tRT.offsetMin = new Vector2(12f, 8f);
        tRT.offsetMax = new Vector2(-12f, -2f);
        track.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.04f, 1f);

        GameObject fgo = new GameObject("Fill");
        fgo.transform.SetParent(track.transform, false);
        RectTransform fRT = fgo.AddComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;
        fRT.offsetMin = new Vector2(2f, 2f);
        fRT.offsetMax = new Vector2(-2f, -2f);
        timerFill = fgo.AddComponent<Image>();
        timerFill.color = new Color(0.35f, 0.75f, 1f);
        timerFill.type = Image.Type.Filled;
        timerFill.fillMethod = Image.FillMethod.Horizontal;
        timerFill.fillAmount = 1f;
    }

    void BuildPhaseLabel(RectTransform root)
    {
        GameObject go = new GameObject("PhaseLabel");
        go.transform.SetParent(root, false);
        phaseGO = go;
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 200f);
        rt.sizeDelta = new Vector2(900f, 90f);
        phaseText = go.AddComponent<Text>();
        phaseText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        phaseText.fontSize = 56;
        phaseText.fontStyle = FontStyle.Bold;
        phaseText.alignment = TextAnchor.MiddleCenter;
        phaseText.color = Color.clear;
        phaseText.raycastTarget = false;
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = Color.black;
        ol.effectDistance = new Vector2(2, -2);
    }

    void BuildStreakLabel(RectTransform root)
    {
        GameObject go = new GameObject("StreakText");
        go.transform.SetParent(root, false);
        streakGO = go;
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -128f);
        rt.sizeDelta = new Vector2(700f, 48f);
        streakText = go.AddComponent<Text>();
        streakText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        streakText.fontSize = 28;
        streakText.fontStyle = FontStyle.Bold;
        streakText.alignment = TextAnchor.MiddleCenter;
        streakText.color = Color.clear;
        streakText.raycastTarget = false;
        go.AddComponent<Outline>().effectColor = Color.black;
    }

    void BuildDmgLayer(RectTransform root)
    {
        GameObject go = new GameObject("DmgNumbers");
        go.transform.SetParent(root, false);
        dmgParent = go.AddComponent<RectTransform>();
        dmgParent.anchorMin = Vector2.zero;
        dmgParent.anchorMax = Vector2.one;
        dmgParent.offsetMin = dmgParent.offsetMax = Vector2.zero;
    }

    GameObject MakePanel(RectTransform root, string title, Color titleColor, Color bgColor, string sub)
    {
        GameObject p = new GameObject(title.Replace(" ", "") + "Panel");
        p.transform.SetParent(root, false);
        RectTransform rt = p.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        MakeCenteredText(p.transform, "Title", title, 96, FontStyle.Bold,
            titleColor, new Vector2(0f, 40f), new Vector2(1400f, 140f));
        MakeCenteredText(p.transform, "Sub", sub, 32, FontStyle.Normal,
            new Color(0.75f, 0.72f, 0.65f), new Vector2(0f, -50f), new Vector2(800f, 60f));

        p.SetActive(false);
        return p;
    }

    static Text MakeChildText(Transform parent, string name, string text,
        int fontSize, FontStyle style, Color color,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        TextAnchor alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        Text t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = alignment;
        t.raycastTarget = false;
        go.AddComponent<Outline>().effectColor = Color.black;
        return t;
    }

    static void MakeCenteredText(Transform parent, string name, string msg,
        int size, FontStyle style, Color color, Vector2 offset, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = sizeDelta;
        Text t = go.AddComponent<Text>();
        t.text = msg;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = Color.black;
        ol.effectDistance = new Vector2(2, -2);
    }
}

using System.Collections;
using UnityEngine;

public class EndScreen : MonoBehaviour
{
    [Header("Refs")]
    public GameManager gameManager;
    public ShipHealth shipHealth;

    [Header("Text")]
    public TextMesh titleText;
    public TextMesh subtitleText;
    public TextMesh promptText;

    [Header("Colors")]
    public Color winColor = new Color(0.4f, 0.95f, 0.55f);
    public Color lossColor = new Color(0.95f, 0.3f, 0.25f);
    public Color subColor = new Color(0.85f, 0.85f, 0.9f);
    public Color promptColor = new Color(0.6f, 0.85f, 1f);

    [Header("Backdrop")]
    public VignetteOverlay backdrop;
    public Color winBackdrop = new Color(0.05f, 0.15f, 0.08f, 0.75f);
    public Color lossBackdrop = new Color(0.15f, 0.05f, 0.05f, 0.85f);

    [Header("Engine sputter (win)")]
    public Transform shipFlame;

    bool showing = false;

    void Start()
    {
        if (titleText != null)    SetAlpha(titleText, 0f);
        if (subtitleText != null) SetAlpha(subtitleText, 0f);
        if (promptText != null)   SetAlpha(promptText, 0f);

        if (gameManager != null)
        {
            gameManager.OnGameWon += HandleWin;
            gameManager.OnGameLost += HandleLoss;
            gameManager.OnGameStarted += HandleRestart;
        }
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnGameWon -= HandleWin;
            gameManager.OnGameLost -= HandleLoss;
            gameManager.OnGameStarted -= HandleRestart;
        }
    }

    void Update()
    {
        if (!showing) return;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (gameManager != null) gameManager.StartGame();
        }
    }

    void HandleWin()
    {
        StopAllCoroutines();
        StartCoroutine(ShowSequence(true));
    }

    void HandleLoss()
    {
        StopAllCoroutines();
        StartCoroutine(ShowSequence(false));
    }

    void HandleRestart()
    {
        showing = false;
        if (titleText != null)    SetAlpha(titleText, 0f);
        if (subtitleText != null) SetAlpha(subtitleText, 0f);
        if (promptText != null)   SetAlpha(promptText, 0f);
        if (backdrop != null)     backdrop.SetAlpha(0f);
    }

    IEnumerator ShowSequence(bool win)
    {
        if (win && shipFlame != null)
            StartCoroutine(EngineSputter());

        yield return new WaitForSeconds(win ? 0.8f : 0.4f);

        if (titleText != null)
        {
            titleText.text = win ? "ESCAPED" : "ENGINE FAILURE";
            titleText.color = (win ? winColor : lossColor);
            yield return FadeIn(titleText, win ? winColor : lossColor, 0.6f);
        }

        if (subtitleText != null)
        {
            float hullPct = shipHealth != null ? shipHealth.HullPercent : 0f;
            float timeLeft = gameManager != null ? gameManager.GetTimeRemaining() : 0f;
            subtitleText.text = win
                ? "Hull " + Mathf.RoundToInt(hullPct) + "%   |   Cleared the belt"
                : "Hull collapsed at " + (30f - timeLeft).ToString("F1") + "s";
            yield return FadeIn(subtitleText, subColor, 0.4f);
        }

        yield return new WaitForSeconds(0.6f);

        if (promptText != null)
        {
            promptText.text = "press SPACE to retry";
            StartCoroutine(PromptPulse());
        }

        showing = true;

        if (backdrop != null)
            backdrop.SetColor(win ? winBackdrop : lossBackdrop);
    }

    IEnumerator EngineSputter()
    {
        if (shipFlame == null) yield break;
        Vector3 baseScale = shipFlame.localScale;
        SpriteRenderer sr = shipFlame.GetComponent<SpriteRenderer>();

        for (int i = 0; i < 6; i++)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            shipFlame.localScale = baseScale * Random.Range(0.4f, 1.1f);
            yield return new WaitForSeconds(0.08f);
        }
        if (sr != null) sr.enabled = false;
        shipFlame.localScale = baseScale;
    }

    IEnumerator FadeIn(TextMesh tm, Color target, float duration)
    {
        float t = 0f;
        Color c = target; c.a = 0f;
        tm.color = c;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / duration);
            tm.color = c;
            yield return null;
        }
        c.a = 1f;
        tm.color = c;
    }

    IEnumerator PromptPulse()
    {
        while (showing)
        {
            float a = 0.5f + Mathf.Sin(Time.time * 3f) * 0.4f;
            Color c = promptColor;
            c.a = Mathf.Clamp01(a);
            promptText.color = c;
            yield return null;
        }
    }

    void SetAlpha(TextMesh tm, float a)
    {
        Color c = tm.color;
        c.a = a;
        tm.color = c;
    }
}

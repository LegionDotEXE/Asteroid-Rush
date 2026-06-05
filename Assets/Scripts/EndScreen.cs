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
    public Color winBackdrop = new Color(0.05f, 0.15f, 0.08f, 0.75f);
    public Color lossBackdrop = new Color(0.15f, 0.05f, 0.05f, 0.85f);

    [Header("Engine sputter (win)")]
    public Transform shipFlame;

    bool showing = false;
    SpriteRenderer backdropSprite;
    Camera cam;

    void Start()
    {
        cam = Camera.main;
        backdropSprite = BuildBackdrop();
        backdropSprite.color = new Color(0, 0, 0, 0);

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
        StopAllCoroutines();
        showing = false;
        if (titleText != null)    SetAlpha(titleText, 0f);
        if (subtitleText != null) SetAlpha(subtitleText, 0f);
        if (promptText != null)   SetAlpha(promptText, 0f);
        if (backdropSprite != null) backdropSprite.color = new Color(0, 0, 0, 0);
    }

    IEnumerator ShowSequence(bool win)
    {
        if (win && shipFlame != null)
            StartCoroutine(EngineSputter());

        yield return StartCoroutine(FadeBackdrop(win ? winBackdrop : lossBackdrop, 0.6f));

        if (titleText != null)
        {
            titleText.text = win ? "ESCAPED" : "ENGINE FAILURE";
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

        yield return new WaitForSeconds(0.4f);

        showing = true;
        if (promptText != null)
        {
            promptText.text = "press SPACE to retry";
            StartCoroutine(PromptPulse());
        }
    }

    IEnumerator FadeBackdrop(Color target, float duration)
    {
        if (backdropSprite == null) yield break;
        Color start = backdropSprite.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            backdropSprite.color = Color.Lerp(start, target, t / duration);
            yield return null;
        }
        backdropSprite.color = target;
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
            if (promptText != null) promptText.color = c;
            yield return null;
        }
    }

    void SetAlpha(TextMesh tm, float a)
    {
        Color c = tm.color;
        c.a = a;
        tm.color = c;
    }

    SpriteRenderer BuildBackdrop()
    {
        GameObject go = new GameObject("EndBackdrop");
        go.transform.SetParent(cam.transform);
        go.transform.localPosition = new Vector3(0f, 0f, 1.5f);
        go.transform.localRotation = Quaternion.identity;

        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;
        go.transform.localScale = new Vector3(w * 1.2f, h * 1.2f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sr.sortingOrder = 50;
        return sr;
    }
}

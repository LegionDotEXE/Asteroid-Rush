using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Refs")]
    public ShipHealth shipHealth;
    public GameManager gameManager;
    public DifficultyManager diffManager;

    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource engineSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip bgmClip;
    public AudioClip engineLoopClip;
    public AudioClip hitClip;
    public AudioClip scrapClip;
    public AudioClip warningClip;
    public AudioClip phaseClip;
    public AudioClip winClip;
    public AudioClip loseClip;

    [Header("Mix")]
    [Range(0f, 1f)] public float bgmVolume = 0.4f;
    [Range(0f, 1f)] public float engineVolume = 0.55f;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;

    [Header("Engine response")]
    public float enginePitchFull = 1.0f;
    public float enginePitchDead = 0.55f;
    public float engineDistortBelow = 25f;

    float lastWarningTime = -10f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (bgmSource != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            if (bgmClip != null) bgmSource.Play();
        }

        if (engineSource != null)
        {
            engineSource.clip = engineLoopClip;
            engineSource.loop = true;
            engineSource.volume = engineVolume;
            engineSource.pitch = enginePitchFull;
            if (engineLoopClip != null) engineSource.Play();
        }

        if (shipHealth != null)
        {
            shipHealth.OnDamaged        += OnDamaged;
            shipHealth.OnScrapCollected += OnScrapCollected;
            shipHealth.OnDied           += OnDied;
        }

        if (gameManager != null)
        {
            gameManager.OnGameWon += OnWin;
            gameManager.OnGameStarted += OnRestart;
        }

        if (diffManager != null)
            diffManager.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDestroy()
    {
        if (shipHealth != null)
        {
            shipHealth.OnDamaged        -= OnDamaged;
            shipHealth.OnScrapCollected -= OnScrapCollected;
            shipHealth.OnDied           -= OnDied;
        }
        if (gameManager != null)
        {
            gameManager.OnGameWon -= OnWin;
            gameManager.OnGameStarted -= OnRestart;
        }
        if (diffManager != null)
            diffManager.OnPhaseChanged -= OnPhaseChanged;
    }

    void Update()
    {
        if (engineSource == null || shipHealth == null) return;

        float hullNorm = shipHealth.HullPercent / 100f;
        float targetPitch = Mathf.Lerp(enginePitchDead, enginePitchFull, hullNorm);

        bool failing = shipHealth.HullPercent < engineDistortBelow;
        if (failing)
            targetPitch += Mathf.Sin(Time.time * 14f) * 0.04f;

        engineSource.pitch = Mathf.Lerp(engineSource.pitch, targetPitch, Time.deltaTime * 3f);

        ProximityWarning();
    }

    void ProximityWarning()
    {
        if (warningClip == null || sfxSource == null) return;
        if (Time.time - lastWarningTime < 1.4f) return;
        if (shipHealth == null) return;

        Transform ship = shipHealth.transform;
        for (int i = 0; i < Obstacle.Active.Count; i++)
        {
            Obstacle o = Obstacle.Active[i];
            float dist = (o.transform.position - ship.position).magnitude;
            if (dist < 3.5f)
            {
                sfxSource.PlayOneShot(warningClip, sfxVolume * 0.7f);
                lastWarningTime = Time.time;
                return;
            }
        }
    }

    void OnDamaged(float amount)
    {
        if (hitClip != null && sfxSource != null)
            sfxSource.PlayOneShot(hitClip, sfxVolume);

        if (engineSource != null)
            StartCoroutine(EngineStutter());
    }

    void OnScrapCollected()
    {
        if (scrapClip != null && sfxSource != null)
            sfxSource.PlayOneShot(scrapClip, sfxVolume * 0.8f);
    }

    void OnPhaseChanged(int tier, DifficultyManager.Phase phase)
    {
        if (phaseClip != null && sfxSource != null)
            sfxSource.PlayOneShot(phaseClip, sfxVolume);
    }

    void OnWin()
    {
        if (winClip != null && sfxSource != null)
            sfxSource.PlayOneShot(winClip, sfxVolume);
        StartCoroutine(FadeBgm(0f, 1.5f));
        if (engineSource != null) StartCoroutine(EngineSputter());
    }

    void OnDied()
    {
        if (loseClip != null && sfxSource != null)
            sfxSource.PlayOneShot(loseClip, sfxVolume);
        StartCoroutine(FadeBgm(0f, 0.8f));
        if (engineSource != null) StartCoroutine(EngineCutOut());
    }

    void OnRestart()
    {
        StopAllCoroutines();
        if (bgmSource != null) { bgmSource.volume = bgmVolume; if (bgmClip != null && !bgmSource.isPlaying) bgmSource.Play(); }
        if (engineSource != null) { engineSource.volume = engineVolume; engineSource.pitch = enginePitchFull; if (engineLoopClip != null && !engineSource.isPlaying) engineSource.Play(); }
    }

    IEnumerator EngineStutter()
    {
        if (engineSource == null) yield break;
        float orig = engineSource.pitch;
        engineSource.pitch = orig * 0.7f;
        yield return new WaitForSeconds(0.12f);
        engineSource.pitch = orig;
    }

    IEnumerator EngineSputter()
    {
        if (engineSource == null) yield break;
        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            engineSource.pitch *= (1f - Time.deltaTime * 0.8f);
            engineSource.volume = Mathf.Lerp(engineVolume, 0f, t / 1.2f);
            yield return null;
        }
        engineSource.Stop();
    }

    IEnumerator EngineCutOut()
    {
        if (engineSource == null) yield break;
        engineSource.pitch *= 0.4f;
        yield return new WaitForSeconds(0.2f);
        engineSource.Stop();
    }

    IEnumerator FadeBgm(float target, float duration)
    {
        if (bgmSource == null) yield break;
        float start = bgmSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
    }
}

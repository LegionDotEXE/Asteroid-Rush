using System;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Serializable]
    public class Phase
    {
        public string name = "PHASE";
        public string warning = "";
        [Header("Obstacles")]
        public float speedMin = 6f, speedMax = 8f;
        public float intervalMin = 1.0f, intervalMax = 1.4f;
        [Range(0,1)] public float clusterChance = 0f;
        public int perWave = 1;
        [Header("Scrap")]
        public float scrapIntervalMin = 1.4f, scrapIntervalMax = 2.2f;
        [Header("Engine")]
        public float passiveDrain = 0.7f;   // hull/sec bleed
    }

    public ObstacleSpawner spawner;
    public ScrapSpawner     scrapSpawner;
    public ShipHealth       shipHealth;

    public Phase[] phases = new Phase[]
    {
        new Phase{ name="PHASE 1", warning="DEBRIS FIELD AHEAD",
                   speedMin=6,  speedMax=8,  intervalMin=0.9f,  intervalMax=1.3f,
                   clusterChance=0f,    perWave=1, scrapIntervalMin=1.4f, scrapIntervalMax=2.2f, passiveDrain=0.7f },
        new Phase{ name="PHASE 2", warning="DENSITY RISING",
                   speedMin=9,  speedMax=12, intervalMin=0.55f, intervalMax=0.85f,
                   clusterChance=0.35f, perWave=1, scrapIntervalMin=2.6f, scrapIntervalMax=3.8f, passiveDrain=1.1f },
        new Phase{ name="PHASE 3", warning="MAX VELOCITY \u2014 CRITICAL",
                   speedMin=13, speedMax=17, intervalMin=0.30f, intervalMax=0.48f,
                   clusterChance=0.65f, perWave=2, scrapIntervalMin=4.0f, scrapIntervalMax=6.0f, passiveDrain=1.5f },
    };
 
    public event Action<int, Phase> OnPhaseChanged;
 
    int currentTier = -1;

    void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted += ResetTier;
    }
    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= ResetTier;
    }
    void ResetTier() => currentTier = -1;

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameActive()) return;
 
        float elapsed = GameManager.Instance.GetElapsed();
        int tier = elapsed >= 20f ? 2 : (elapsed >= 10f ? 1 : 0);
        if (tier == currentTier) return;
 
        currentTier = tier;
        ApplyPhase(phases[Mathf.Clamp(tier, 0, phases.Length - 1)], tier);
    }

    void ApplyPhase(Phase p, int tier)
    {
        if (spawner != null)
            spawner.SetPhase(p.speedMin, p.speedMax, p.intervalMin, p.intervalMax,
                             p.clusterChance, p.perWave);
        if (scrapSpawner != null)
            scrapSpawner.SetPhase(p.scrapIntervalMin, p.scrapIntervalMax);
        if (shipHealth != null)
            shipHealth.passiveDrain = p.passiveDrain;
 
        OnPhaseChanged?.Invoke(tier, p);
    }
}

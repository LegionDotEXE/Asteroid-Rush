using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public ObstacleSpawner spawner;

    [Header("Tier 1 - 0 to 10s")]
    public float tier1Speed = 6f;
    public float tier1Interval = 1.2f;

    [Header("Tier 2 - 10 to 20s")]
    public float tier2Speed = 9f;
    public float tier2Interval = 0.8f;

    [Header("Tier 3 - 20 to 30s")]
    public float tier3Speed = 13f;
    public float tier3Interval = 0.45f;

    int currentTier = 0;

    void Update()
    {
        if (!GameManager.Instance.IsGameActive()) return;

        float elapsed = 30f - GameManager.Instance.GetTimeRemaining();

        if (elapsed >= 20f && currentTier != 3)
        {
            currentTier = 3;
            ApplyTier(tier3Speed, tier3Interval);
        }
        else if (elapsed >= 10f && currentTier != 2)
        {
            currentTier = 2;
            ApplyTier(tier2Speed, tier2Interval);
        }
        else if (elapsed < 10f && currentTier != 1)
        {
            currentTier = 1;
            ApplyTier(tier1Speed, tier1Interval);
        }
    }

    void ApplyTier(float speed, float interval)
    {
        spawner.SetSpeed(speed);
        spawner.SetSpawnInterval(interval);
        Debug.Log("Difficulty tier " + currentTier);
    }
}

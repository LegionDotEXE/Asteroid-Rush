using System.Collections;
using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    public float maxDurability = 100f;
    float currentDurability;

    public enum HealthTier { Full, Seventy5, Fifty, TwentyFive, Critical, Dead }
    HealthTier currentTier = HealthTier.Full;

    bool regenActive = false;
    Coroutine regenCoroutine;

    void Start()
    {
        currentDurability = maxDurability;
    }

    void Update()
    {
        UpdateTier();
    }

    void UpdateTier()
    {
        float pct = currentDurability / maxDurability * 100f;
        HealthTier newTier;

        if (pct <= 0f)         newTier = HealthTier.Dead;
        else if (pct <= 15f)   newTier = HealthTier.Critical;
        else if (pct <= 25f)   newTier = HealthTier.TwentyFive;
        else if (pct <= 50f)   newTier = HealthTier.Fifty;
        else if (pct <= 75f)   newTier = HealthTier.Seventy5;
        else                   newTier = HealthTier.Full;

        if (newTier != currentTier)
        {
            currentTier = newTier;
            OnTierChanged(currentTier);
        }
    }

    void OnTierChanged(HealthTier tier)
    {
        Debug.Log("Health tier: " + tier);

        if (tier == HealthTier.Critical)
            StartCoroutine(CriticalAutoHeal());

        if (tier == HealthTier.Dead)
            GameManager.Instance.TriggerLoss();
    }

    IEnumerator CriticalAutoHeal()
    {
        yield return new WaitForSeconds(2f);
        if (currentDurability / maxDurability * 100f <= 15f)
        {
            currentDurability = maxDurability * 0.25f;
            Debug.Log("Critical auto-heal to 25%");
        }
    }

    public void TakeDamage(float amt)
    {
        currentDurability = Mathf.Max(currentDurability - amt, 0f);
    }

    public void StartRegen(float totalHeal, float duration)
    {
        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);
        regenCoroutine = StartCoroutine(RegenOverTime(totalHeal, duration));
    }

    IEnumerator RegenOverTime(float totalHeal, float duration)
    {
        float elapsed = 0f;
        float healPerSec = totalHeal / duration;
        while (elapsed < duration)
        {
            currentDurability = Mathf.Min(currentDurability + healPerSec * Time.deltaTime, maxDurability);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public float GetDurabilityPct() => currentDurability / maxDurability;
    public HealthTier GetTier() => currentTier;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle"))
        {
            TakeDamage(20f);
            other.gameObject.SetActive(false);
        }
    }
}

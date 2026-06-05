using System;
using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    [Header("Hull")]
    public float maxHull = 100f;
 
    [Header("Scrap regen (heals over time)")]
    public float healPerScrap = 20f;   // each pickup adds this much to the pool
    public float regenRate    = 7f;    // hull/sec pulled from pool into the hull
 
    [Header("Hit response")]
    public float invulnTime = 0.85f;   // i-frames after taking a hit
 
    // Set by DifficultyManager every phase. The engine bleeding out.
    [HideInInspector] public float passiveDrain = 0f;
 
    public float CurrentHull { get; private set; }
    public float RegenPool   { get; private set; }
    public float HullPercent => maxHull <= 0f ? 0f : (CurrentHull / maxHull) * 100f;
    public bool  IsInvulnerable => invulnTimer > 0f;
 
    public event Action<float> OnHullChanged;   // normalized 0..1
    public event Action<float> OnDamaged;       // amount (for flash/shake)
    public event Action        OnScrapCollected;
    public event Action        OnDied;
 
    float invulnTimer;
    bool  dead;

    void Start() => ResetHull();
    
    public void ResetHull()
    {
        CurrentHull  = maxHull;
        RegenPool    = 0f;
        invulnTimer  = 0f;
        dead         = false;
        OnHullChanged?.Invoke(1f);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameActive()) return;
 
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;
 
        // failing-engine bleed (ignores i-frames; can kill you on its own)
        if (passiveDrain > 0f) ApplyChange(-passiveDrain * Time.deltaTime, false);
 
        // regen from collected scrap, metered out by regenRate
        if (RegenPool > 0f)
        {
            float heal = Mathf.Min(RegenPool, regenRate * Time.deltaTime);
            RegenPool -= heal;
            ApplyChange(heal, false);
        }
    }

    public bool TakeHit(float amount)
    {
        if (dead || invulnTimer > 0f) return false;
        invulnTimer = invulnTime;
        ApplyChange(-amount, true);
        return true;
    }
 
    public void AddScrap()
    {
        RegenPool += healPerScrap;
        OnScrapCollected?.Invoke();
    }
 
    void ApplyChange(float delta, bool wasHit)
    {
        if (dead) return;
        CurrentHull = Mathf.Clamp(CurrentHull + delta, 0f, maxHull);
        OnHullChanged?.Invoke(CurrentHull / maxHull);
        if (wasHit && delta < 0f) OnDamaged?.Invoke(-delta);
        if (CurrentHull <= 0f) Die();
    }
 
    void Die()
    {
        if (dead) return;
        dead = true;
        OnDied?.Invoke();
    }

}

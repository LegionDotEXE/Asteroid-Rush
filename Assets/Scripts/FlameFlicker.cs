using UnityEngine;

public class FlameFlicker : MonoBehaviour
{
    public ShipHealth shipHealth;
    public Transform  flame;            // engine-flame child (with a SpriteRenderer)
    public float flickerSpeed = 12f;
 
    SpriteRenderer flameRenderer;
    Vector3 originalScale;

    void Start()
    {
        if (flame == null) return;
        originalScale = flame.localScale;
        flameRenderer = flame.GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (flame == null) return;
 
        float hull = shipHealth != null ? shipHealth.HullPercent : 100f;
        bool failing = hull < 40f;
 
        float flick = 0.85f + Mathf.PerlinNoise(Time.time * flickerSpeed, 0f) * 0.3f;
 
        // sputter: when failing, randomly cut the flame out for a frame
        if (flameRenderer != null)
            flameRenderer.enabled = !failing || Random.value > 0.4f;
 
        float len = 0.5f + hull / 200f;   // shorter flame as hull falls
        flame.localScale = new Vector3(originalScale.x * flick,
                                       originalScale.y * len * flick,
                                       originalScale.z);
    
    }
}

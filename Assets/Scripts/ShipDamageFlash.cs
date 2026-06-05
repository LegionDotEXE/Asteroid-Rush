using System.Collections;
using UnityEngine;

public class ShipDamageFlash : MonoBehaviour
{
    public float flashDuration = 0.22f;
    public Color flashColor = new Color(1f, 0.25f, 0.2f, 1f);

    SpriteRenderer[] renderers;
    Color[] originalColors;
    ShipHealth health;
    Coroutine running;

    void Awake()
    {
        health = GetComponent<ShipHealth>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].color;
    }

    void OnEnable()
    {
        if (health != null) health.OnDamaged += OnDamaged;
    }

    void OnDisable()
    {
        if (health != null) health.OnDamaged -= OnDamaged;
    }

    void OnDamaged(float amount)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float n = 1f - t / flashDuration;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].color = Color.Lerp(originalColors[i], flashColor, n);
            }
            yield return null;
        }
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].color = originalColors[i];
        }
    }
}

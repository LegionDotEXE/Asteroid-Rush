using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }
 
    Vector3 basePos;
    Coroutine routine;
 
    void Awake()
    {
        Instance = this;
        basePos = transform.localPosition;
    }
 
    public void Shake(float duration, float magnitude)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(duration, magnitude));
    }
 
    IEnumerator Run(float duration, float magnitude)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float damper = 1f - (t / duration);              // ease out
            Vector2 off = Random.insideUnitCircle * magnitude * damper;
            transform.localPosition = basePos + (Vector3)off;
            yield return null;
        }
        transform.localPosition = basePos;
    }
}

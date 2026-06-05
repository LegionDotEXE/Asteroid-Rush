using UnityEngine;

[RequireComponent(typeof(ShipHealth))]
public class ShipController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;        // top steer speed at full hull (units/s)
    public float smoothing = 12f;       // higher = snappier response
    public float collisionRadius = 0.5f;
    public float facingAngle = 90f;     
    public float tiltAmount = 14f;      // bank angle when moving sideways

    [Header("Hull % -> control quality")]
    [Range(0,1)] public float mul75  = 0.82f;  // 50–75%: slightly sluggish
    [Range(0,1)] public float mul50  = 0.62f;  // 25–50%: slows moderately
    [Range(0,1)] public float mul25  = 0.45f;  // invert..25%: slows significantly
    [Range(0,1)] public float mulCrit= 0.34f;  // below invert threshold
    public float driftBelow  = 25f;     // hull % where un-commanded drift begins
    public float invertBelow = 12f;     // hull % where controls flicker-invert

    [Header("Drift / invert")]
    public float driftStrength = 4f;
    public float invertDuration = 0.8f;
    public Vector2 invertEvery = new Vector2(2.2f, 3.5f);

    [Header("Hit response")]
    public float knockback = 4f;

    ShipHealth health;
    Vector3 velocity, startPos;
    float halfX, halfY;

    Vector2 driftVel; float driftTimer;
    float invertTimer, invertCooldown;

    void Awake() => health = GetComponent<ShipHealth>();

    void Start()
    {
        startPos = transform.position;
        float camZ = Mathf.Abs(Camera.main.transform.position.z);
        halfX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, camZ)).x;
        halfY = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, camZ)).y;

        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted += ResetShip;
    }
    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= ResetShip;
    }

    void ResetShip()
    {
        transform.position = startPos;
        velocity = Vector3.zero;
        driftVel = Vector2.zero;
        driftTimer = invertTimer = invertCooldown = 0f;
        transform.rotation = Quaternion.Euler(0f, 0f, facingAngle);
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameActive()) return;

        float hull = health.HullPercent;
        float mul  = SteerMultiplier(hull);

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"),
                                    Input.GetAxisRaw("Vertical"), 0f);
        if (input.sqrMagnitude > 1f) input.Normalize();

        // critical: occasionally invert controls for a brief window
        if (hull < invertBelow)
        {
            invertCooldown -= Time.deltaTime;
            if (invertCooldown <= 0f)
            {
                invertCooldown = Random.Range(invertEvery.x, invertEvery.y);
                invertTimer = invertDuration;
            }
        }
        if (invertTimer > 0f) { invertTimer -= Time.deltaTime; input = -input; }

        Vector3 desired = input * moveSpeed * mul;

        // drift below 25%: random sway, worse as hull approaches 0
        if (hull < driftBelow)
        {
            driftTimer -= Time.deltaTime;
            if (driftTimer <= 0f)
            {
                driftTimer = Random.Range(1.0f, 1.8f);
                float sev = Mathf.Clamp01((driftBelow - hull) / driftBelow);
                driftVel = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f))
                           * driftStrength * sev;
            }
            desired += (Vector3)driftVel;
        }
        else driftVel = Vector2.zero;

        // smooth toward desired velocity (frame-rate independent)
        velocity = Vector3.Lerp(velocity, desired, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        transform.position += velocity * Time.deltaTime;

        // clamp to the screen
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -halfX, halfX);
        p.y = Mathf.Clamp(p.y, -halfY, halfY);
        transform.position = p;

        // face up, bank on horizontal motion
        float bank = -velocity.x / Mathf.Max(0.01f, moveSpeed) * tiltAmount;
        transform.rotation = Quaternion.Euler(0f, 0f, facingAngle + bank);

        CheckCollisions();
    }

    float SteerMultiplier(float hull)
    {
        if (hull > 75f) return 1f;
        if (hull > 50f) return mul75;
        if (hull > 25f) return mul50;
        if (hull > invertBelow) return mul25;
        return mulCrit;
    }

    void CheckCollisions()
    {
        for (int i = 0; i < Obstacle.Active.Count; i++)
        {
            Obstacle o = Obstacle.Active[i];
            float rr = o.radius + collisionRadius;
            if ((o.transform.position - transform.position).sqrMagnitude <= rr * rr)
            {
                if (health.TakeHit(o.damage))
                {
                    Vector3 dir = (transform.position - o.transform.position).normalized;
                    velocity += dir * knockback;
                    if (CameraShake.Instance != null)
                        CameraShake.Instance.Shake(0.25f, Mathf.Lerp(0.08f, 0.3f, o.damage / 30f));
                }
            }
        }

        for (int i = Scrap.Active.Count - 1; i >= 0; i--)
        {
            Scrap s = Scrap.Active[i];
            float rr = s.radius + collisionRadius;
            if ((s.transform.position - transform.position).sqrMagnitude <= rr * rr)
            {
                s.Collect();
                health.AddScrap();
            }
        }
    }
}
using UnityEngine;

public class PooledBullet : MonoBehaviour
{
    public float lifetime = 3f;

    private int spawnFrame;
    private int lastHitColliderId = -1;

    private Damage damageData;

    [HideInInspector] public bool canPierce = false;
    [HideInInspector] public PlayerUpgradeManager upgrades;
    [HideInInspector] public bool useDamageFalloff = false;


    private bool isAlive = false;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // bullet becomes active when taken from pool
        isAlive = true;
        lastHitColliderId = -1;
        spawnFrame = Time.frameCount;

        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), lifetime);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }


    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
        rb.linearVelocity = Vector3.zero;
    }

    public void Configure(Damage dmg, float newLifeTime, bool pierce)
    {
        damageData = dmg; // store dmg
        lifetime = newLifeTime; // bullet lifetime
        canPierce = pierce; // whether bullet can pierce

        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), lifetime);
    }

    public void Configure(Damage dmg, float newLifeTime) // overload (no piercing)
    {
        Configure(dmg, newLifeTime, false);
    }

    private void HandleHit(Collider other)
    {
        if (!isAlive) return;

        // prevent multiple hits
        int id = other.GetInstanceID();
        if (id == lastHitColliderId) return;
        lastHitColliderId = id;

        // check if object has mechHealth
        MechHealth health = other.GetComponentInParent<MechHealth>();

        if (health != null)
        {
            // apply damage
            health.TakeDamage(damageData);

            // if cant pierce, return
            if (!canPierce)
                ReturnToPool();
        }
        else
        {
            ReturnToPool();
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void ReturnToPool()
    {
        if (!isAlive) return;
        isAlive = false;

        rb.linearVelocity = Vector3.zero;

        BulletPool.Instance?.Return(gameObject);
    }
}

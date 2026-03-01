using UnityEngine;

public class EnemyPooledBullet : MonoBehaviour
{
    public float lifetime = 3f;

    public float baseDamage = 10f; // base dmg at wave 1
    public float damageAmount; // actual damage

    private bool isAlive = false;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // activate bullet when taken from pool
        isAlive = true;

        float mult = 1f;
        if (WaveManager.Instance != null)
            mult = WaveManager.Instance.GetEnemyDamageMultiplier();

        damageAmount = baseDamage * mult;

        // return to pool after lifetime
        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), lifetime);

        rb.linearVelocity = Vector3.zero;
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
        rb.linearVelocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isAlive) return;

        // only damage player
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            MechHealth health = collision.gameObject.GetComponentInParent<MechHealth>();
            if (health != null)
            {
                Damage dmg = new Damage(damageAmount, gameObject);
                health.TakeDamage(dmg);
            }
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        isAlive = false;
        EnemyBulletPool.Instance?.Return(gameObject);
    }
}

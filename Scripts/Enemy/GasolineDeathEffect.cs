using System.Collections;
using UnityEngine;

public class GasolineDeathEffect : MonoBehaviour
{
    // explosion config
    public float explosionRadius = 3f;
    public float explosionDamage = 20f;
    public LayerMask enemyLayer;
    public GameObject explosionVfxPrefab;
    public float explosionVfxLifetime = 2f;

    private MechHealth health;
    private PlayerUpgradeManager upgrades;

    private void Awake()
    {
        health = GetComponent<MechHealth>();
        upgrades = FindFirstObjectByType<PlayerUpgradeManager>();

        health.OnDeath += OnEnemyDeath; // subscribe to this enemy's death event
    }

    private void OnDestroy()
    {
        health.OnDeath -= OnEnemyDeath; // unsubscribe from death event
    }

    private void OnEnemyDeath()
    {
        upgrades.OnEnemyKilled(health); // notify vampire core for healing

        upgrades.OnEnemyKilledHermes(); // notify hermes upgrade for speed boost

        if (upgrades == null || !upgrades.hasGasoline) // dont work if dont have the upgrade
            return;

        Vector3 pos = transform.position;

        Collider[] hits = Physics.OverlapSphere(pos, explosionRadius, enemyLayer); // detect nearby enemies within radius

        foreach (var col in hits)
        {
            MechHealth mh = col.GetComponentInParent<MechHealth>();

            if (mh == null || mh == health) // skip self
                continue;

            // explosion damage
            mh.TakeDamage(new Damage(explosionDamage, gameObject)); // apply explosion damage

        }

        // vfx
        GameObject vfx = Instantiate(explosionVfxPrefab, pos, Quaternion.identity);
        Destroy(vfx, explosionVfxLifetime);
    }
}

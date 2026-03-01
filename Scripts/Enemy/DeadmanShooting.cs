using System.Collections;
using UnityEngine;

public class DeadmanShooting : MonoBehaviour
{
    public Transform firePoint;

    // burst config
    public int shotsPerBurst = 3;
    public float shotInterval = 0.25f;
    public float burstCooldown = 2f;

    // bullet config
    public float bulletSpeed = 20f;
    public float bulletDamage = 30f;
    public float bulletLifetime = 2f;

    private BaseRangedAI ai;
    private float nextBurstTime;

    private void Awake()
    {
        ai = GetComponent<BaseRangedAI>();
    }

    private void Update()
    {
        if (!ai.CanShoot()) return; // only shoot in valid range

        if (Time.time >= nextBurstTime)
        {
            StartCoroutine(FireBurst()); // start burst
            nextBurstTime = Time.time + burstCooldown; // set next available burst time
        }
    }

    private IEnumerator FireBurst()
    {
        for (int i = 0; i < shotsPerBurst; i++) // fire multiple shots with delay
        {
            FireOneShot();
            yield return new WaitForSeconds(shotInterval); // wait before firing next
        }
    }

    private void FireOneShot()
    {
        GameObject bullet = EnemyBulletPool.Instance.Get();

        Vector3 dir = (ai.target.position - firePoint.position).normalized; // get direction toward player

        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = Quaternion.LookRotation(dir);

        // configure bullet
        EnemyPooledBullet epb = bullet.GetComponent<EnemyPooledBullet>();
        epb.damageAmount = bulletDamage;
        epb.lifetime = bulletLifetime;

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.linearVelocity = dir * bulletSpeed; // apply velocity to bullet
    }

}

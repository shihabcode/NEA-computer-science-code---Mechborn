using UnityEngine;

public class WalkingTurretShooter : MonoBehaviour
{
    // bullet config
    public Transform firePoint;
    public float bulletSpeed = 18f;
    public float fireRate = 1f;

    private BaseRangedAI ai;
    private float nextFire;

    private void Awake()
    {
        ai = GetComponent<BaseRangedAI>();
    }

    private void Update()
    {
        if (ai == null || !ai.CanShoot()) return; // ensure enemy is within valid shooting range
        if (Time.time < nextFire) return; // enforce fire rate cooldown

        FireAtPlayer();
        nextFire = Time.time + fireRate; // set next allowed fire rate
    }

    private void FireAtPlayer()
    {
        GameObject bullet = EnemyBulletPool.Instance.Get();
        if (bullet == null) return;

        Vector3 dir = (ai.target.position - firePoint.position).normalized; // calculate direction toward player

        // position and rotate bullet
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = Quaternion.LookRotation(dir);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.linearVelocity = dir * bulletSpeed; // apply velocity to bullet
    }
}

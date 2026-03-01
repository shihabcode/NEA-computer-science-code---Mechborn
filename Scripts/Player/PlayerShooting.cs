using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerShooting : MonoBehaviour
{
    // shooting config
    public GameObject bulletPrefab;
    public Transform firePointLeft;
    public Transform firePointRight;
    public float bulletSpeed = 20f;
    public float fireRate = 0.15f;
    public bool canPierce = false;
    public PlayerUpgradeManager upgrades;

    // spread and range
    [Range(0f, 30f)]
    public float spreadAngle = 5f;
    public float bulletRange = 3f;

    // offensive stats
    public float baseDamage = 10f;
    public float critChance = 0.1f;
    public float critMultiplier = 2f;

    // audio
    public AudioSource sfxSource;
    public AudioClip projectileShotClip;

    [Range(0f, 1f)] public float shotVolume = 1f;
    public float shotPitchRandom = 0.05f;


    private float nextFireTime = 0f;
    private bool fireLeft = true;
    private Camera mainCam;

    public WaveManager waveManager;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        // if in shop dont shoot
        if (waveManager.inShop)
            return;

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime) // automatic fire when holding left mouse button
        {
            Transform firePoint = fireLeft ? firePointLeft : firePointRight;
            fireLeft = !fireLeft; // alternate between fire points

            // firing sfx
            PlayShotSfx(projectileShotClip);
            Shoot(firePoint);

            nextFireTime = Time.time + fireRate; // next allowed fire time
        }
    }
    void PlayShotSfx(AudioClip clip)
    {
        // plays the sfx
        if (clip == null || sfxSource == null) return;

        // randomises pitch
        float basePitch = 1f;
        float jitter = Random.Range(-shotPitchRandom, shotPitchRandom);
        sfxSource.pitch = basePitch + jitter;

        sfxSource.PlayOneShot(clip, shotVolume);
    }

    // bullet mode

    void Shoot(Transform firePoint)
    {
        GameObject bullet = BulletPool.Instance.Get();
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = firePoint.rotation;

        // spread calc
        float half = spreadAngle * 0.5f; // spread applied as random yaw (around y axis)
        float angleOffset = Random.Range(-half, half);
        Quaternion spreadRot = Quaternion.AngleAxis(angleOffset, Vector3.up);
        Vector3 finalDir = spreadRot * firePoint.forward; // final direction is firepoint forward rotated by spread
        bullet.transform.rotation = Quaternion.LookRotation(finalDir);

        // bullet stats
        PooledBullet pooled = bullet.GetComponent<PooledBullet>();

        Damage dmg = CalculateShotDamage();
        bool pierceThisShot = canPierce;

        pooled.upgrades = upgrades; // give access to upgrades
        pooled.Configure(dmg, bulletRange, pierceThisShot);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.linearVelocity = finalDir * bulletSpeed; // apply velocity
    }

   
    // damage calc

    private Damage CalculateShotDamage()
    {
        bool isCrit = Random.value < critChance;
        float dmg = baseDamage;

        if (isCrit)
            dmg *= critMultiplier;

        return new Damage(dmg, gameObject, isCrit);
    }
}

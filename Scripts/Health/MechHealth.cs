using System;
using UnityEngine;
using System.Collections;

public class MechHealth : MonoBehaviour
{
    // Core health
    public float maxCoreHealth = 200f;
    [NonSerialized] public float currentCoreHealth;

    // Shield behaviour
    public float maxShield = 100f;
    public float shieldRechargeRate = 10f;
    public float shieldRechargeDelay = 5f;
    [NonSerialized] public float currentShield;

    // Armour
    [Range(0f, 0.9f)]
    public float armourPercent = 0.2f;

    // Overcharged shield behaviour
    [HideInInspector] public bool overchargedShieldActive = false;
    [Range(0f, 1f)] public float shieldSeepPercent = 0f;

    // Invulnerability
    public float invulnerabilityAfterHit = 0.08f;

    public bool IsDead => currentCoreHealth <= 0f;

    // Events
    public event Action<float, float> OnHealthChanged;
    public event Action<Damage> OnDamaged;
    public event Action OnDeath;

    private float _lastDamageTime = -999f;
    private float _lastHitTime = -999f;
    private int _lastHitFrame = -999;

    // Audio
    public AudioSource hitAudio;
    public AudioClip hitClip;
    public AudioClip critHitClip;

    public float hitVolume = 0.7f;
    public float pitchRandom = 0.05f;

    public AudioClip deathClip;
    public float deathVolume = 1f;

    private void Awake()
    {
        currentCoreHealth = maxCoreHealth;
        currentShield = maxShield;
    }

    private void Update()
    {
        HandleShieldRecharge();
    }

    public float GetCurrentHealth() => currentCoreHealth;

    public void TakeDamage(Damage damage)
    {
        if (currentCoreHealth <= 0f) return; // already died

        
        if (dashInvulnerability) return; // dash i-frames ignore dmg

        if (invulnerabilityAfterHit > 0f) // invulnerability window
        {
       
            if (Time.frameCount != _lastHitFrame)
            {
                if (Time.time < _lastHitTime + invulnerabilityAfterHit) return;
            }
        }

        _lastHitFrame = Time.frameCount;
        _lastHitTime = Time.time;
        _lastDamageTime = Time.time;

        float incoming = damage.amount;
        float remaining = incoming;

        // overcharged seep
        float bypassToHealth = 0f;
        if (overchargedShieldActive && currentShield > 0f && shieldSeepPercent > 0f)
        {
            bypassToHealth = remaining * shieldSeepPercent;
            remaining -= bypassToHealth;
        }

        // shield absorbs first
        if (currentShield > 0f)
        {
            float absorbed = Mathf.Min(currentShield, remaining);
            currentShield -= absorbed;
            remaining -= absorbed;
        }

        // bypass dmg goes to health
        remaining += bypassToHealth;

        // armour reduction
        if (remaining > 0f)
        {
            remaining *= (1f - armourPercent);
            currentCoreHealth -= remaining;
            currentCoreHealth = Mathf.Max(0f, currentCoreHealth);
        }

        // damage numbers
        if (remaining > 0f && DamageNumberManager.Instance != null)
        {
            Vector3 pos = transform.position + Vector3.up * 1.0f;
            DamageNumberManager.Instance.SpawnDamageNumber(pos, remaining, damage.isCrit);
        }

        PlayHitSfx(damage.isCrit);

        OnDamaged?.Invoke(damage);
        OnHealthChanged?.Invoke(currentCoreHealth, currentShield);

        if (currentCoreHealth <= 0f)
            Die();
    }

    // Hit SFX
    private void PlayHitSfx(bool isCrit)
    {
        AudioClip clip = isCrit ? critHitClip : hitClip;

        float pitch = 1f + UnityEngine.Random.Range(-pitchRandom, pitchRandom);

        if (currentCoreHealth - 0.0001f <= 0f || hitAudio == null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, hitVolume);
            return;
        }

        hitAudio.pitch = pitch;
        hitAudio.PlayOneShot(clip, hitVolume);
    }


    // dash invulnerability

    private bool dashInvulnerability = false; // true while dash i-frames are active

    public void EnableInvulnerability(float duration)
    {
        StartCoroutine(DashInvulnerabilityRoutine(duration));
    }

    private IEnumerator DashInvulnerabilityRoutine(float duration)
    {
        // toggles invulnerability for set time
        dashInvulnerability = true;
        yield return new WaitForSeconds(duration);
        dashInvulnerability = false;
    }

    // healing
    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentCoreHealth = Mathf.Min(maxCoreHealth, currentCoreHealth + amount);
        OnHealthChanged?.Invoke(currentCoreHealth, currentShield);
    }

    public void ForceHealthChangedEvent()
    {
        // force UI to refresh
        OnHealthChanged?.Invoke(currentCoreHealth, currentShield);
    }

    private void HandleShieldRecharge()
    {
        // no recharge needed if shield already full
        if (currentShield >= maxShield) return;
        if (!overchargedShieldActive && Time.time < _lastDamageTime + shieldRechargeDelay) // if not using overcharged shield, wait until delay after last dmg passed
            return;

        // recharge shield over time
        currentShield += shieldRechargeRate * Time.deltaTime;
        currentShield = Mathf.Min(currentShield, maxShield);
        OnHealthChanged?.Invoke(currentCoreHealth, currentShield); // notify UI
    }

    private void Die()
    {
        OnDeath?.Invoke();

        if (GetComponent<EnemyTag>() != null)
        {
            ScrapManager.Instance.AddScrapFromKill(); // if enemy, add scrap
        }

        if (deathClip != null)
            AudioSource.PlayClipAtPoint(deathClip, transform.position, deathVolume); // play death clip

        gameObject.SetActive(false); // disable
    }


}

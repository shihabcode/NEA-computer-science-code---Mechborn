using UnityEngine;

public class MechaMineAI : EnemyAI
{
    // explosion config
    public float triggerDistance = 3f;
    public float explosionRadius = 4f;
    public float explosionDamage = 40f;
    public float armTime = 0.7f;
    public GameObject explosionEffect;
    public float explosionVfxLifetime = 2f;

    private Vector3 armVelocity;

    private bool hasExploded = false;
    private float armTimer = 0f;
    private MechHealth health;

    // states
    private enum MineState { Chasing, Arming, Exploded }
    private MineState state = MineState.Chasing;

    protected override void Start()
    {
        base.Start();

        health = GetComponent<MechHealth>();
        if (health != null)
        {
            health.OnDeath += OnMineDeath; // subscribe to death event
        }
    }

    private void OnEnable()
    {
        // reset state variables
        hasExploded = false;
        state = MineState.Chasing;
        armTimer = 0f;
        armVelocity = Vector3.zero;
        rb.isKinematic = false;
    }

    protected override void FixedUpdate()
    {
        if (target == null || hasExploded) return;

        // check trigger distance
        if (state == MineState.Chasing)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= triggerDistance)
            {
                state = MineState.Arming;
                armTimer = armTime;

                armVelocity = rb.linearVelocity; // store current velocity and disable rb physics
                rb.isKinematic = true;
            }
        }

        // state machine

        switch (state)
        {
            case MineState.Chasing:
                base.MoveEnemy();
                break;

            case MineState.Arming:
                ArmAndBrake();
                break;

            case MineState.Exploded:
                if (rb != null) rb.linearVelocity = Vector3.zero;
                break;
        }

        RotateEnemy(); // always rotate to player while active
    }

    private void ArmAndBrake()
    {
        armTimer -= Time.fixedDeltaTime;
        
        // manually decelerate
        if (armTimer > 0f && armVelocity.sqrMagnitude > 0.0001f)
        {
            float t = Time.fixedDeltaTime;

            float decel = armVelocity.magnitude / Mathf.Max(armTime, 0.01f); // linear decel over armTime

            Vector3 dir = armVelocity.normalized;
            float newSpeed = Mathf.Max(0f, armVelocity.magnitude - decel * t);

            Vector3 avgVel = dir * ((armVelocity.magnitude + newSpeed) * 0.5f); // move using avg velocity
            transform.position += avgVel * t;

            armVelocity = dir * newSpeed;
        }

        if (armTimer <= 0f) // when timer ends, explode
        {
            Explode(doAoE: true);
        }
    }

    private void OnMineDeath()
    {
        if (hasExploded) return;
        Explode(doAoE: false); // explode visually but dont deal damage when mine is killed before arming is complete
    }

    private void Explode(bool doAoE)
    {
        if (hasExploded) return;
        hasExploded = true;
        state = MineState.Exploded;

        // spawn vfx
        GameObject vfx = Instantiate(explosionEffect, transform.position, Quaternion.identity);
        Destroy(vfx, explosionVfxLifetime);

        rb.isKinematic = true;


        if (doAoE && target != null) // apply aoe damage
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= explosionRadius)
            {
                MechHealth mh = target.GetComponentInParent<MechHealth>();
                if (mh != null)
                    mh.TakeDamage(new Damage(explosionDamage, gameObject));
            }
        }

        // force self destruction
        float killAmount = health.currentCoreHealth + health.currentShield + 1f;
        health.TakeDamage(new Damage(killAmount, gameObject));

    }
}

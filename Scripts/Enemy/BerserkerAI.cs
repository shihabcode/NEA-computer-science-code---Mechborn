using UnityEngine;

public class BerserkerAI : EnemyAI
{
    // dash config
    public float dashCooldown = 3f;
    public float dashMinDistance = 4f;
    public float dashMaxDistance = 12f;
    public float dashSpeedMultiplier = 0.5f;
    public float dashDuration = 0.5f;
    public float dashWindupTime = 0.2f;
    public float dashContactMultiplier = 2f;

    // states control
    private enum State {  Chasing, Windup, Dashing }
    private State state = State.Chasing;
    private float stateTimer = 0f;
    private float nextDashTime = 0f;
    private Vector3 dashDirection;


    // state machine
    protected override void MoveEnemy()
    {
        switch (state)
        {
            case State.Chasing:
                DoChase();
                TryStartDash();
                break;

            case State.Windup:
                DoWindup();
                break;

            case State.Dashing:
                DoDash();
                break;
        }
    }

    private void DoChase()
    {
        // predict player future position
        Vector3 targetPos = GetPredictedTargetPosition();
        Vector3 toTarget = targetPos - transform.position;

        float dist = toTarget.magnitude;
        if (dist <= 0.001f) // if extremely close, stop moving
        {
            ApplyAcceleration(Vector3.zero);
            return;
        }

        if (dist <= personalStopDistance) // stop if inside persoanl stopping distance
        {
            ApplyAcceleration(-Vector3.zero);
            return;
        }

        // move toward target position
        Vector3 dir = toTarget.normalized;
        Vector3 desiredVel = dir * maxSpeed;

        ApplyAcceleration(desiredVel);
    }

    private void TryStartDash()
    {
        if (Time.time < nextDashTime) return; // prevent dash if on cooldown
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        if (dist < dashMinDistance || dist > dashMaxDistance) // only dash if within allowed range
            return;

        // enter windup state
        state = State.Windup;
        stateTimer = dashWindupTime;
        rb.linearVelocity = Vector3.zero; // stop movement during windup
    }

    private void DoWindup()
    {
        stateTimer -= Time.fixedDeltaTime;
        rb.linearVelocity = Vector3.zero; // remain still while charging dash

        if (stateTimer <= 0f)
        {
            // lock direction
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f)
                toTarget = transform.forward; // fallback if too close

            dashDirection = toTarget.normalized;

            // enter dash state
            state = State.Dashing;
            stateTimer = dashDuration;

            rb.linearVelocity = dashDirection * maxSpeed * dashSpeedMultiplier; // apply dash velocity

            nextDashTime = Time.time + dashCooldown; // set next dash cooldown
        }
    }
    private void DoDash()
    {
        // enter dash state
        stateTimer -= Time.fixedDeltaTime;

        rb.linearVelocity = dashDirection * maxSpeed * dashSpeedMultiplier; // maintain constant vel

        if (stateTimer <= 0f)
        {
            state = State.Chasing; // return to chasing when dash ends
        }
    }

    // contact damage

    protected override void OnCollisionStay(Collision collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        if (Time.time < lastHitTime + contactCooldown) return;
        lastHitTime = Time.time;

        MechHealth mh = collision.collider.GetComponentInParent<MechHealth>();
        if (mh == null) return;

        float dmg = contactDamage;
        if (state == State.Dashing)
            dmg *= dashContactMultiplier;

        mh.TakeDamage(new Damage(dmg, gameObject));
    }
}

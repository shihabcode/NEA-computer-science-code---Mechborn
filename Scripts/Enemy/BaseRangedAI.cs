using UnityEngine;

public class BaseRangedAI : MonoBehaviour
{
    public Transform target;

    // movement
    public float maxSpeed = 5f;
    public float acceleration = 8f;
    public float deceleration = 10f;
    public float predictionTime = 0.3f;

    // distance bands
    public float preferredDistance = 10f;   
    public float distanceTolerance = 2f;    
    public float minShootDistance = 4f;     

    // contact damage
    public float contactDamage = 10f;
    public float contactCooldown = 0.5f;

    private Rigidbody rb;
    private Rigidbody targetRb;
    private float lastHitTime = -999f;

    public Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>(); // cache rb for reference
        rb.freezeRotation = true; // prevent physics from rotating the enemy
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player"); // automatically find player
            if (p != null) target = p.transform;
        }

        if (target != null)
            targetRb = target.GetComponent<Rigidbody>(); // cache rb for movement prediction
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        MoveEnemy();
        RotateTowardsTarget();
    }

    private void MoveEnemy()
    {
        if (target == null) return;

        // predicts position of player
        Vector3 targetPos = target.position;
        targetPos += targetRb.linearVelocity * predictionTime;

        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;

        float dist = toTarget.magnitude;
        if (dist < 0.001f) return;

        Vector3 dir = toTarget.normalized;

        // inner and outer boundaries
        float inner = preferredDistance - distanceTolerance;
        float outer = preferredDistance + distanceTolerance;

        Vector3 desiredVel = Vector3.zero;

        if (dist > outer)
        {
            // move closer
            desiredVel = dir * maxSpeed;
        }
        else if (dist < inner)
        {
            // move away
            desiredVel = -dir * maxSpeed;
        }
        else
        {
            // inside band
            desiredVel = Vector3.zero;
        }

        ApplyAcceleration(desiredVel);
    }

    private void ApplyAcceleration(Vector3 targetVelocity)
    {
        Vector3 currentVel = rb.linearVelocity;
        Vector3 speedDiff = targetVelocity - currentVel; // difference between current and desired vel

        // use deceleration when stopping, acceleration when moving
        float accelRate = (targetVelocity.sqrMagnitude < 0.0001f)
            ? deceleration
            : acceleration;

        Vector3 force = speedDiff * accelRate;
        rb.AddForce(force, ForceMode.Force); // apply force

        // animate
        animator.SetFloat("Speed", rb.linearVelocity.magnitude);
    }

    private void RotateTowardsTarget()
    {
        // smoothly rotate enemy to face player
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                10f * Time.fixedDeltaTime
            );
        }
    }

    public bool CanShoot()
    {
        // returns true if enemy is in correct distance to shoot
        if (target == null) return false;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        // must be within band and not too close
        return dist <= preferredDistance + distanceTolerance &&
               dist >= minShootDistance;
    }

    // contact dmg

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            if (Time.time >= lastHitTime + contactCooldown)
            {
                lastHitTime = Time.time;

                MechHealth mh = collision.collider.GetComponentInParent<MechHealth>();
                if (mh != null)
                {
                    mh.TakeDamage(new Damage(contactDamage, gameObject)); // apply damage instance
                }
            }
        }
    }
}

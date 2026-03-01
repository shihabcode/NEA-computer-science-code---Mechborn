using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform target;

    // movement config
    public float maxSpeed = 6f;
    public float acceleration = 10f;
    public float deceleration = 12f;
    public float predictionTime = 0.4f;

    // control and grouping config
    public float stopDistance = 0f;
    public float stopDistanceJitter = 0.5f;
    protected float personalStopDistance;

    // damage config
    public float contactDamage = 10f;
    public float contactCooldown = 0.5f;

    protected Rigidbody rb;
    protected Rigidbody targetRb;
    protected float lastHitTime = -999f;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.freezeRotation = true; // prevent physics from rotating enemy

        personalStopDistance = stopDistance + Random.Range(-stopDistanceJitter, stopDistanceJitter); // prevent overlapping using randomised stopping distance
        if (personalStopDistance < 0f) personalStopDistance = 0f; // prevent negative stopping distances
    }

    protected virtual void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player"); // find player
            if (p != null) target = p.transform;
        }

        if (target != null)
            targetRb = target.GetComponent<Rigidbody>();
    }

    protected virtual void FixedUpdate()
    {
        if (target == null) return;

        MoveEnemy();
        RotateEnemy();
    }

    protected virtual void MoveEnemy()
    {
        Vector3 targetPos = GetPredictedTargetPosition(); // get target position

        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f; // ignore vertical difference
        float centreDist = toTarget.magnitude;
        if (centreDist <= 0.001f) return;

        CapsuleCollider myCol = GetComponent<CapsuleCollider>();
        CapsuleCollider targetCol = target.GetComponent<CapsuleCollider>();

        float myRadius = (myCol != null) ? myCol.radius : 0.5f;
        float targetRadius = (targetCol != null) ? targetCol.radius : 0.5f;

        float surfaceDist = centreDist - (myRadius + targetRadius); // distance between surface

        if (surfaceDist <= personalStopDistance) // stop if between personal stopping distance
        {
            ApplyAcceleration(Vector3.zero);
            return;
        }

        Vector3 dir = toTarget.normalized;
        Vector3 desiredVel = dir * maxSpeed; 

        ApplyAcceleration(desiredVel); // move toward player
    }

    protected virtual void RotateEnemy()
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            // smoothly rotate to player
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                10f * Time.fixedDeltaTime
            );
        }
    }

    protected Vector3 GetPredictedTargetPosition() // predict where the player will be
    {
        Vector3 targetPos = target.position;
        if (targetRb != null)
        {
            targetPos += targetRb.linearVelocity * predictionTime;
        }
        return targetPos;
    }

    protected void ApplyAcceleration(Vector3 targetVelocity)
    {
        Vector3 currentVel = rb.linearVelocity;
        Vector3 speedDiff = targetVelocity - currentVel;

        float accelRate = (targetVelocity.sqrMagnitude < 0.0001f) // decelerate when stopping, accelerate when moving
            ? deceleration
            : acceleration;

        Vector3 force = speedDiff * accelRate;
        rb.AddForce(force, ForceMode.Force);
    }

    // contact damage

    protected virtual void OnCollisionStay(Collision collision)
    {
        if (!collision.collider.CompareTag("Player")) return; // only damage player

        if (Time.time < lastHitTime + contactCooldown) return; // enforce cooldown
        lastHitTime = Time.time;

        MechHealth mh = collision.collider.GetComponentInParent<MechHealth>();
        if (mh == null) return;

        mh.TakeDamage(new Damage(contactDamage, gameObject));
    }
}

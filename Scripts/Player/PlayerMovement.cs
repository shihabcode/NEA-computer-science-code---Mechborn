using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float maxSpeed;
    public float acceleration;
    public float deceleration;
    public LayerMask groundLayer;

    // base values
    [HideInInspector] public float baseMaxSpeed;
    [HideInInspector] public float baseAcceleration;
    [HideInInspector] public float baseDeceleration;

    // leaning config
    public Transform visualRoot;
    public float maxForwardLean = 10f;
    public float maxSideLean = 8f;
    public float leanLerpSpeed = 10f;

    // rotate config
    private Vector3 smoothedDirection;
    public float rotationLag = 0.15f;

    // dashing config
    public float dashDistance = 6f;
    public float dashTime = 0.15f;
    public float dashCooldown = 0.8f;
    public float dashSpeedMultiplier = 1.0f;
    private bool isDashing = false;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;

    private int playerLayer;
    private int enemyLayer;

    public float dashMaxSpeedMultiplier = 2.5f;
    public AnimationCurve dashSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float dashLerpStrength = 0.6f;

    public float hermesSpeedMultiplier = 1f;

    // energy config
    public float maxEnergy = 100f;
    public float dashEnergyCost = 25f;
    public float boostEnergyDrainPerSecond = 20f;
    public float energyRegenRate = 15f;
    public float energyRegenDelay = 1.5f;
    public float boostSpeedMultiplier = 1.8f;
    public float boostAccelerationMultiplier = 2f;
    public float boostWindowDuration = 0.7f;
    public float energyCostMultiplier = 1f;

    public EnergyBarUI energyBarUI;

    private float currentEnergy;
    private float lastEnergyUseTime = -999f;
    private bool isBoosting = false;
    private float boostWindowTimer = 0f;

    private Vector2 moveInput;
    private Rigidbody rb;

    private WaveManager waveManager;
    public MechHealth mechHealth;

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void Awake()
    {
        // store original movement values for upgrades
        baseMaxSpeed = maxSpeed;
        baseAcceleration = acceleration;
        baseDeceleration = deceleration;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        waveManager = FindFirstObjectByType<WaveManager>();

        playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer("Enemy");

        // initialize energy
        currentEnergy = maxEnergy;
        UpdateEnergyUI();
    }

    private void Update()
    {
        if (waveManager.inShop) // freeze controls inside shop phase
            return;

        dashCooldownTimer -= Time.deltaTime; // dash cooldown

        // dash countdown window
        if (boostWindowTimer > 0f)
            boostWindowTimer -= Time.deltaTime;

        // dash input check (must not already be dashing, cooldown allows it and enough energy available)
        if (Input.GetKeyDown(KeyCode.LeftShift) &&
            !isDashing &&
            dashCooldownTimer <= 0f &&
            currentEnergy >= dashEnergyCost * energyCostMultiplier
)
        {
            StartCoroutine(DashRoutine());
        }

        // boost after dash
        bool canBoostNow =
            !isDashing &&
            boostWindowTimer > 0f &&
            Input.GetKey(KeyCode.LeftShift) &&
            moveInput.sqrMagnitude > 0.01f &&
            currentEnergy > 0f;

        isBoosting = canBoostNow;

        HandleEnergy();

        rotatePlayer();
        UpdateLean();
    }

    void FixedUpdate()
    {
        if (waveManager.inShop) // freeze movement
            return;

        if (!isDashing) // do not apply normal movement forces during dash
            movePlayer();
    }

    // movement scripts
    public void movePlayer()
    {
        // movement multipliers
        float speedMult = hermesSpeedMultiplier;
        float accelMult = 1f;

        // boost increases speed and accel
        if (isBoosting && currentEnergy > 0.5f)
        {
            speedMult *= boostSpeedMultiplier;
            accelMult *= boostAccelerationMultiplier;
        }

        float effectiveMaxSpeed = maxSpeed * speedMult; 

        Vector3 targetVelocity = new Vector3(moveInput.x, 0f, moveInput.y) * effectiveMaxSpeed; // desired vel
        Vector3 speedDif = targetVelocity - rb.linearVelocity; // vel difference
        float accelOrDecel = (moveInput.magnitude == 0 ? deceleration : acceleration) * accelMult; // if no input, use deceleration to slow down faster
        Vector3 movement = speedDif * accelOrDecel;

        rb.AddForce(movement, ForceMode.Force); // apply force
    }

    public void rotatePlayer()
    {
        // convert mouse position into world space
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.transform.position.y;

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        Vector3 direction = worldPosition - transform.position; // direction from player to mouse point
        direction.y = 0;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        // smooth aim direction to reduce jitter and rotation lag
        smoothedDirection = Vector3.Lerp(
            smoothedDirection == Vector3.zero ? direction : smoothedDirection,
            direction,
            Time.deltaTime / rotationLag
        );

        // apply rotation
        Quaternion targetRotation = Quaternion.LookRotation(smoothedDirection);
        transform.rotation = targetRotation;
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;

        // spend dash energy
        float dashCost = dashEnergyCost * energyCostMultiplier;
        currentEnergy = Mathf.Max(0f, currentEnergy - dashCost);
        lastEnergyUseTime = Time.time;
        UpdateEnergyUI();

        // determine dash direction (if moving, in movement direction, otherwise dash forward)
        Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y);
        dashDirection = moveDir.sqrMagnitude > 0.1f
            ? moveDir.normalized
            : transform.forward;

        float distanceSpeed = dashDistance / dashTime; // base speed needed to cover distance in dashTime
        float currentTopSpeed = maxSpeed * hermesSpeedMultiplier;

        float baseDashSpeed = distanceSpeed + (currentTopSpeed * 0.8f);

        float peakMultiplier = dashMaxSpeedMultiplier;

        mechHealth.EnableInvulnerability(dashTime); // dash invulnerability

        // phase through enemies
        Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        float elapsed = 0f;
        while (elapsed < dashTime)
        {
            float t = elapsed / dashTime; // normalise time
            float curve = dashSpeedCurve.Evaluate(t); // use animation curve

            float currentSpeed = baseDashSpeed * Mathf.Lerp(1f, peakMultiplier, curve); // interpolate towards peak speed

            // snap hard at start then blend
            Vector3 targetVel = dashDirection * currentSpeed;
            float snap = (t < 0.08f) ? 1f : dashLerpStrength;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, snap);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // disable phasing
        Physics.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        isDashing = false;
        boostWindowTimer = boostWindowDuration;
    }



    private void HandleEnergy()
    {
        // drain while boosting
        if (isBoosting && currentEnergy > 0f)
        {
            float drain = boostEnergyDrainPerSecond * energyCostMultiplier * Time.deltaTime;
            currentEnergy = Mathf.Max(0f, currentEnergy - drain);
            lastEnergyUseTime = Time.time;

            if (currentEnergy <= 0f)
            {
                isBoosting = false;
                boostWindowTimer = 0f;
            }

            UpdateEnergyUI();
        }

        // regen when not using
        if (!isDashing && !isBoosting && currentEnergy < maxEnergy)
        {
            if (Time.time >= lastEnergyUseTime + energyRegenDelay)
            {
                currentEnergy += energyRegenRate * Time.deltaTime;
                currentEnergy = Mathf.Min(maxEnergy, currentEnergy);
                UpdateEnergyUI();
            }
        }
    }

    private void UpdateEnergyUI()
    {
        energyBarUI.SetEnergy(currentEnergy, maxEnergy);
    }

    public void ClampEnergyToMax()
    {
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        UpdateEnergyUI();
    }

    private void UpdateLean()
    {
        if (visualRoot == null) return;

        // effective max speed used to normalise strength
        float effectiveMaxSpeed = Mathf.Max(0.01f, maxSpeed * hermesSpeedMultiplier);

        // convert vel into local space
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float forwardNorm = Mathf.Clamp(localVel.z / effectiveMaxSpeed, -1f, 1f);
        float sideNorm = Mathf.Clamp(localVel.x / effectiveMaxSpeed, -1f, 1f);

        // pitch forward when moving forward. roll opposite when strafing
        float targetPitch = forwardNorm * maxForwardLean;
        float targetRoll = -sideNorm * maxSideLean;

        // interpolate visual root
        Quaternion targetRot = Quaternion.Euler(targetPitch, 0f, targetRoll);
        visualRoot.localRotation = Quaternion.Slerp(
            visualRoot.localRotation,
            targetRot,
            leanLerpSpeed * Time.deltaTime
        );
    }
}

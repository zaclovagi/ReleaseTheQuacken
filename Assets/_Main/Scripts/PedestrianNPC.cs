using UnityEngine;

/// <summary>
/// Pedestrian NPC that wanders using raycasts — no NavMesh required.
/// Casts forward for walls and downward for ledges, turning away when blocked.
/// Panics and flees when the duck gets close, ragdolls on direct impact.
/// </summary>
public class PedestrianNPC : MonoBehaviour
{
    public enum State { Wandering, Panicking, Fleeing, Ragdoll, Attacking }

    [Header("Movement")]
    public float normalSpeed = 1.4f;
    public float fleeSpeed = 6f;
    public float turnSpeed = 180f;

    [Header("Raycasts")]
    public float wallCheckDistance = 0.8f;
    public float ledgeCheckDistance = 0.6f;
    public float rayOriginHeight = 0.6f;

    [Header("Wander")]
    [Tooltip("How often the NPC picks a new random wander direction (seconds).")]
    public float wanderChangeInterval = 3f;
    [Tooltip("Max random yaw added each wander interval.")]
    public float wanderTurnRange = 90f;

    [Header("Detection")]
    [Tooltip("How far the NPC can hear a quack and start panicking.")]
    public float quackHearRadius = 8f;
    public float fleeRadius = 14f;

    [Header("Attack")]
    [Tooltip("Distance at which the NPC begins chasing and attacking the player.")]
    public float attackRange = 5f;
    [Tooltip("Distance at which the NPC deals damage to the player.")]
    public float attackContactRadius = 1.5f;
    [Tooltip("Score points removed from the player per hit.")]
    public int attackDamageScore = 10;
    [Tooltip("Seconds between hits.")]
    public float attackCooldown = 1f;
    public float attackChaseSpeed = 3f;

    [Header("Panic")]
    [Tooltip("Animator bool parameter name for panicking state.")]
    public string panicAnimBool = "isPanicking";

    [Header("Attack Animator")]
    [Tooltip("Animator trigger parameter name fired when the NPC hits the player.")]
    public string attackAnimTrigger = "Attack";

    private Rigidbody rb;
    private Animator animator;
    private Transform player;
    private Player playerScript;
    private State state = State.Wandering;

    private float currentSpeed;
    private float wanderTimer;
    private float panicFreezeTimer;
    private float attackCooldownTimer;
    private const float PanicFreezeDuration = 0.3f;

    void OnEnable()
    {
        Player.OnQuack += OnQuackHeard;
    }

    void OnDisable()
    {
        Player.OnQuack -= OnQuackHeard;
    }

    void OnQuackHeard(Vector3 quackPos, float radius)
    {
        if (state == State.Ragdoll || state == State.Fleeing || state == State.Panicking) return;
        if (Vector3.Distance(transform.position, quackPos) <= radius)
            EnterPanic();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        player = playerGO?.transform;
        playerScript = playerGO?.GetComponent<Player>();
        currentSpeed = normalSpeed;
        wanderTimer = Random.Range(0f, wanderChangeInterval);
    }

    void Update()
    {
        if (state == State.Ragdoll) return;

        float distToPlayer = player != null
            ? Vector3.Distance(transform.position, player.position)
            : float.MaxValue;
        switch (state)
        {
            case State.Wandering:
                if (distToPlayer <= attackRange)
                    EnterAttack();
                else
                    HandleWander();
                break;

            case State.Panicking:
                panicFreezeTimer += Time.deltaTime;
                if (panicFreezeTimer >= PanicFreezeDuration)
                    EnterFlee();
                break;

            case State.Fleeing:
                if (distToPlayer > fleeRadius)
                    ExitFlee();
                else
                    HandleFlee();
                break;

            case State.Attacking:
                HandleAttack(distToPlayer);
                break;
        }
    }

    void FixedUpdate()
    {
        if (state == State.Ragdoll || state == State.Panicking) return;
        MoveForward();
    }

    // -------------------------------------------------------------------------
    // State transitions
    // -------------------------------------------------------------------------

    void EnterPanic()
    {
        state = State.Panicking;
        panicFreezeTimer = 0f;
        currentSpeed = 0f;
        if (animator != null && !string.IsNullOrEmpty(panicAnimBool))
            animator.SetBool(panicAnimBool, true);
    }

    void EnterFlee()
    {
        state = State.Fleeing;
        currentSpeed = fleeSpeed;
        FaceAwayFromPlayer();
    }

    void ExitFlee()
    {
        state = State.Wandering;
        currentSpeed = normalSpeed;
        wanderTimer = 0f;
        if (animator != null && !string.IsNullOrEmpty(panicAnimBool))
            animator.SetBool(panicAnimBool, false);
    }

    void EnterAttack()
    {
        state = State.Attacking;
        currentSpeed = attackChaseSpeed;
        attackCooldownTimer = attackCooldown; // ready to hit immediately on contact
    }

    void HandleAttack(float distToPlayer)
    {
        // Give up if player moved far enough away
        if (distToPlayer > attackRange * 1.5f)
        {
            state = State.Wandering;
            currentSpeed = normalSpeed;
            wanderTimer = 0f;
            return;
        }

        // Face and chase player
        if (player != null)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }

        if (IsBlocked())
            TurnAwayFromObstacle();

        // Deal damage when close enough
        attackCooldownTimer += Time.deltaTime;
        if (distToPlayer <= attackContactRadius && attackCooldownTimer >= attackCooldown)
        {
            attackCooldownTimer = 0f;
            if (animator != null && !string.IsNullOrEmpty(attackAnimTrigger))
                animator.SetTrigger(attackAnimTrigger);
            playerScript?.TakeDamage(attackDamageScore);
        }
    }

    // -------------------------------------------------------------------------
    // Behaviour handlers
    // -------------------------------------------------------------------------

    void HandleWander()
    {
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderChangeInterval)
        {
            wanderTimer = 0f;
            transform.Rotate(0f, Random.Range(-wanderTurnRange, wanderTurnRange), 0f);
        }

        if (IsBlocked())
            TurnAwayFromObstacle();
    }

    void HandleFlee()
    {
        if (IsBlocked())
            TurnAwayFromObstacle();
        else
            FaceAwayFromPlayer();
    }

    void FaceAwayFromPlayer()
    {
        if (player == null) return;
        Vector3 fleeDir = transform.position - player.position;
        fleeDir.y = 0f;
        if (fleeDir.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(fleeDir.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    bool IsBlocked()
    {
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        if (Physics.Raycast(origin, transform.forward, wallCheckDistance))
            return true;

        Vector3 ledgeCheckOrigin = transform.position + transform.forward * ledgeCheckDistance + Vector3.up * rayOriginHeight;
        if (!Physics.Raycast(ledgeCheckOrigin, Vector3.down, rayOriginHeight + 0.5f))
            return true;

        return false;
    }

    void TurnAwayFromObstacle()
    {
        float[] turnOptions = { 45f, -45f, 90f, -90f, 180f };
        foreach (float angle in turnOptions)
        {
            Quaternion testRot = transform.rotation * Quaternion.Euler(0f, angle, 0f);
            Vector3 testDir = testRot * Vector3.forward;
            Vector3 origin = transform.position + Vector3.up * rayOriginHeight;

            bool wallAhead = Physics.Raycast(origin, testDir, wallCheckDistance);
            Vector3 ledgeOrigin = transform.position + testDir * ledgeCheckDistance + Vector3.up * rayOriginHeight;
            bool ledgeAhead = !Physics.Raycast(ledgeOrigin, Vector3.down, rayOriginHeight + 0.5f);

            if (!wallAhead && !ledgeAhead)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, testRot, turnSpeed * Time.deltaTime + Mathf.Abs(angle));
                return;
            }
        }
        transform.Rotate(0f, turnSpeed * Time.deltaTime, 0f);
    }

    void MoveForward()
    {
        if (rb != null)
        {
            Vector3 vel = transform.forward * currentSpeed;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
        }
        else
        {
            transform.position += transform.forward * currentSpeed * Time.fixedDeltaTime;
        }
    }

    // -------------------------------------------------------------------------
    // Ragdoll on impact
    // -------------------------------------------------------------------------

    void OnCollisionEnter(Collision collision)
    {
        if (state == State.Ragdoll) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        if (collision.gameObject.GetComponent<Player>() == null) return;
        TriggerRagdoll(collision.relativeVelocity);
    }

    public void TriggerRagdoll(Vector3 force)
    {
        state = State.Ragdoll;
        currentSpeed = 0f;

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
            rb.isKinematic = false;
            rb.AddForce(force * 3f, ForceMode.Impulse);
        }

        Invoke(nameof(RecoverFromRagdoll), 4f);
    }

    void RecoverFromRagdoll()
    {
        if (rb != null)
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        state = State.Wandering;
        currentSpeed = normalSpeed;
        wanderTimer = 0f;
        if (animator != null && !string.IsNullOrEmpty(panicAnimBool))
            animator.SetBool(panicAnimBool, false);
    }

    // -------------------------------------------------------------------------
    // Editor helpers
    // -------------------------------------------------------------------------

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, quackHearRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fleeRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f); // orange
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        Gizmos.color = Color.white;
        Gizmos.DrawRay(origin, transform.forward * wallCheckDistance);

        Vector3 ledgeOrigin = transform.position + transform.forward * ledgeCheckDistance + Vector3.up * rayOriginHeight;
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(ledgeOrigin, Vector3.down * (rayOriginHeight + 0.5f));
    }
}

using System.Collections;
using UnityEngine;

/// <summary>
/// Controla o comportamento do inimigo Yamabushi.
/// Estados: Idle → Patrol → Chase → Attack → Hurt → Dead
/// 
/// Setup no Inspector:
///   - Adicionar Animator com os parâmetros abaixo
///   - Adicionar Rigidbody2D (Freeze Rotation Z)
///   - Adicionar Collider2D
///   - Preencher os campos serializados
/// 
/// Parâmetros do Animator necessários:
///   - IsWalking   (Bool)
///   - IsRunning   (Bool)
///   - Attack1     (Trigger)
///   - Attack2     (Trigger)
///   - Attack3     (Trigger)
///   - Hurt        (Trigger)
///   - Dead        (Trigger)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class YamabushiController : MonoBehaviour
{
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");
    private static readonly int Attack3Hash = Animator.StringToHash("Attack3");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int IdleStateHash = Animator.StringToHash("Idle");
    private static readonly int RunStateHash = Animator.StringToHash("Run");
    private static readonly int JumpStateHash = Animator.StringToHash("Jump");
    private static readonly int Attack1StateHash = Animator.StringToHash("Attack1");
    private static readonly int Attack2StateHash = Animator.StringToHash("Attack2");
    private static readonly int Attack3StateHash = Animator.StringToHash("Attack3");

    // ─── Stats ───────────────────────────────────────────────────────────────
    [Header("Stats")]
    [SerializeField] private int maxHealth = 120;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private int attackDamage = 15;

    // ─── Detecção ────────────────────────────────────────────────────────────
    [Header("Detecção")]
    [SerializeField] private float activationHeightOffset = 0.9f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackVerticalTolerance = 1.1f;
    [SerializeField] private float preferredAttackDistance = 1.25f;
    [SerializeField] private float chasePredictionTime = 0.18f;
    [SerializeField] private LayerMask playerLayer;

    // ─── Patrol ──────────────────────────────────────────────────────────────
    [Header("Patrol")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;
    [SerializeField] private float patrolWaitTime = 1.5f;

    // ─── Attack ──────────────────────────────────────────────────────────────
    [Header("Attack")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private float attackHitDelay = 0.2f;
    [SerializeField] private float attackRecoveryTime = 0.55f;
    [SerializeField] private float attackKnockbackForce = 12f;
    [SerializeField] private float attackKnockbackUpForce = 4f;

    [Header("Voo")]
    [SerializeField] private bool followPlayerHeight = true;
    [SerializeField] private float verticalFollowSpeed = 3f;
    [SerializeField] private float verticalFollowDeadZone = 0.45f;
    [SerializeField] private float flightActivationHeight = 1.1f;

    [Header("Chao")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.15f;

    // ─── Knockback ───────────────────────────────────────────────────────────
    [Header("Knockback ao ser atingido")]
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float hurtDuration = 0.25f;
    [SerializeField] private float hurtCooldown = 0.35f;

    // ─── Componentes ─────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;

    // ─── Estado interno ──────────────────────────────────────────────────────
    private enum State { Idle, Patrol, Chase, Attack, Hurt, Dead }
    private State currentState = State.Idle;

    private int currentHealth;
    private Transform player;
    private Rigidbody2D playerRb;
    private Vector2 lastKnownPlayerPosition;
    private float attackTimer;
    private bool hasAwakened;
    private bool isPatrolWaiting;
    private Transform currentPatrolTarget;
    private bool isKnockedBack;
    private bool isUsingJumpAnimation;
    private bool isFlying;
    private float originalGravityScale;
    private float nextHurtTime;
    private Coroutine attackRoutine;
    private Coroutine hurtRoutine;

    // ─── Inicialização ───────────────────────────────────────────────────────
    private void Awake()
    {
        rb         = GetComponent<Rigidbody2D>();
        animator   = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        originalGravityScale = rb.gravityScale;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        attackTimer   = 0f;

        // Detecta o player na cena (tag "Player")
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRb = playerObj.GetComponent<Rigidbody2D>();
            lastKnownPlayerPosition = player.position;
            IgnorePlayerBodyCollision(playerObj);
        }

        // Começa patrulhando se tiver pontos configurados
        if (patrolPointA != null && patrolPointB != null)
        {
            currentPatrolTarget = patrolPointA;
            ChangeState(State.Patrol);
        }
        else
        {
            ChangeState(State.Idle);
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        attackDamage = Mathf.Max(0, attackDamage);
        activationHeightOffset = Mathf.Max(0f, activationHeightOffset);
        attackRange = Mathf.Max(0f, attackRange);
        attackVerticalTolerance = Mathf.Max(0f, attackVerticalTolerance);
        preferredAttackDistance = Mathf.Max(0f, preferredAttackDistance);
        chasePredictionTime = Mathf.Max(0f, chasePredictionTime);
        patrolWaitTime = Mathf.Max(0f, patrolWaitTime);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        attackHitDelay = Mathf.Max(0f, attackHitDelay);
        attackRecoveryTime = Mathf.Max(0f, attackRecoveryTime);
        attackKnockbackForce = Mathf.Max(0f, attackKnockbackForce);
        attackKnockbackUpForce = Mathf.Max(0f, attackKnockbackUpForce);
        verticalFollowSpeed = Mathf.Max(0f, verticalFollowSpeed);
        verticalFollowDeadZone = Mathf.Max(0f, verticalFollowDeadZone);
        flightActivationHeight = Mathf.Max(verticalFollowDeadZone, flightActivationHeight);
        groundCheckRadius = Mathf.Max(0.01f, groundCheckRadius);
        knockbackForce = Mathf.Max(0f, knockbackForce);
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        hurtDuration = Mathf.Max(0f, hurtDuration);
        hurtCooldown = Mathf.Max(0f, hurtCooldown);
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
            hurtRoutine = null;
        }

        if (rb != null)
            rb.gravityScale = originalGravityScale;
    }

    // ─── Loop principal ──────────────────────────────────────────────────────
    private void Update()
    {
        if (currentState == State.Dead || currentState == State.Hurt)
            return;

        ResolvePlayer();
        RefreshPlayerMemory();
        attackTimer -= Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:
            case State.Patrol:
                if (ShouldStartChase())
                    ChangeState(State.Chase);
                else if (currentState == State.Patrol)
                    HandlePatrol();
                break;

            case State.Chase:
                if (ShouldStopChase())
                    ChangeState(patrolPointA != null ? State.Patrol : State.Idle);
                else if (IsPlayerInAttackWindow() && attackTimer <= 0f)
                    ChangeState(State.Attack);
                else
                    ChasePlayer();
                break;

            case State.Attack:
                // A saída do estado é controlada pela animação via AnimationEvent ou Coroutine
                break;
        }
    }

    // ─── Patrol ──────────────────────────────────────────────────────────────
    private void HandlePatrol()
    {
        if (isPatrolWaiting) return;

        Vector2 direction = (currentPatrolTarget.position - transform.position);
        float dist = direction.magnitude;

        if (dist < 0.2f)
        {
            StartCoroutine(PatrolWait());
            return;
        }

        MoveInDirection(direction.normalized);
    }

    private IEnumerator PatrolWait()
    {
        isPatrolWaiting = true;
        SetMovementAnimation(false, false);
        yield return new WaitForSeconds(patrolWaitTime);

        currentPatrolTarget = currentPatrolTarget == patrolPointA ? patrolPointB : patrolPointA;
        isPatrolWaiting = false;
    }

    // ─── Chase ───────────────────────────────────────────────────────────────
    private void ChasePlayer()
    {
        if (player == null) return;

        Vector2 chaseTarget = GetPredictedPlayerPosition();
        float horizontalDelta = chaseTarget.x - transform.position.x;
        if (Mathf.Abs(horizontalDelta) <= preferredAttackDistance)
        {
            float verticalVelocity = GetChaseVerticalVelocity();
            rb.linearVelocity = new Vector2(0f, verticalVelocity);
            SetMovementAnimation(false, isFlying);
            FacePlayer();
            return;
        }

        Vector2 direction = new Vector2(Mathf.Sign(horizontalDelta), 0f);
        MoveInDirection(direction);
    }

    private void MoveInDirection(Vector2 direction)
    {
        if (isKnockedBack) return;

        float verticalVelocity = GetChaseVerticalVelocity();

        rb.linearVelocity = new Vector2(direction.x * moveSpeed, verticalVelocity);
        SetMovementAnimation(currentState == State.Patrol, currentState == State.Chase);
        FlipSprite(direction.x);
    }

    private float GetChaseVerticalVelocity()
    {
        if (!followPlayerHeight || currentState != State.Chase || player == null)
        {
            SetGroundedMovementMode();
            return rb.linearVelocity.y;
        }

        float verticalDelta = player.position.y - transform.position.y;
        bool shouldFly = verticalDelta >= flightActivationHeight
            || (isFlying && Mathf.Abs(verticalDelta) > verticalFollowDeadZone && !IsGrounded());

        if (!shouldFly)
        {
            SetGroundedMovementMode();
            return rb.linearVelocity.y;
        }

        isFlying = true;
        rb.gravityScale = 0f;
        SetAirFollowAnimation(true);
        return Mathf.Sign(verticalDelta) * verticalFollowSpeed;
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            return;
        }

        player = playerObj.transform;
        playerRb = playerObj.GetComponent<Rigidbody2D>();
        lastKnownPlayerPosition = player.position;
        IgnorePlayerBodyCollision(playerObj);
    }

    private void RefreshPlayerMemory()
    {
        if (player == null)
        {
            return;
        }

        if (!hasAwakened && !HasPlayerReachedActivationHeight())
        {
            return;
        }

        lastKnownPlayerPosition = player.position;
    }

    private bool ShouldStartChase()
    {
        if (hasAwakened)
        {
            return player != null;
        }

        hasAwakened = HasPlayerReachedActivationHeight();
        return hasAwakened;
    }

    private bool ShouldStopChase()
    {
        return player == null;
    }

    private bool HasPlayerReachedActivationHeight()
    {
        if (player == null)
        {
            return false;
        }

        return player.position.y >= transform.position.y - activationHeightOffset;
    }

    private bool IsPlayerInAttackWindow()
    {
        if (player == null)
        {
            return false;
        }

        Vector2 delta = player.position - transform.position;
        return Mathf.Abs(delta.x) <= attackRange && Mathf.Abs(delta.y) <= attackVerticalTolerance;
    }

    private Vector2 GetPredictedPlayerPosition()
    {
        Vector2 target = player != null ? (Vector2)player.position : lastKnownPlayerPosition;
        if (playerRb != null)
        {
            target += playerRb.linearVelocity * chasePredictionTime;
        }

        return target;
    }

    // ─── Attack ──────────────────────────────────────────────────────────────
    private void PerformAttack()
    {
        SetGroundedMovementMode();
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        SetMovementAnimation(false, false);

        // Escolhe um dos 3 ataques aleatoriamente
        int attackIndex = Random.Range(1, 4);
        FacePlayer();
        ResetCombatTriggers();
        animator.SetTrigger(GetAttackHash(attackIndex));
        animator.Play(GetAttackStateHash(attackIndex), 0, 0f);
        attackTimer = attackCooldown;

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        if (attackHitDelay > 0f)
            yield return new WaitForSeconds(attackHitDelay);

        if (currentState == State.Attack)
            OnAttackHit();

        if (attackRecoveryTime > 0f)
            yield return new WaitForSeconds(attackRecoveryTime);

        if (currentState != State.Dead && currentState != State.Hurt)
        {
            attackRoutine = null;
            ChangeState(State.Chase);
        }
    }

    /// <summary>
    /// Chamado via Animation Event no frame de hit de cada animação de ataque.
    /// </summary>
    public void OnAttackHit()
    {
        if (player == null) return;
        float dist = Vector2.Distance(attackOrigin != null ? attackOrigin.position : transform.position,
                                      player.position);
        if (dist <= attackRange * 1.2f)
        {
            PlayerHurt playerHurt = player.GetComponent<PlayerHurt>();
            if (playerHurt == null)
                playerHurt = player.GetComponentInParent<PlayerHurt>();

            if (playerHurt != null)
            {
                Vector2 knockbackDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
                playerHurt.TakeHit(knockbackDirection, attackKnockbackForce, attackKnockbackUpForce);
                return;
            }

            player.SendMessage("TakeHit", SendMessageOptions.DontRequireReceiver);
        }
    }

    // ─── Receber dano ────────────────────────────────────────────────────────
    public void TakeDamage(int damage, Vector2 knockbackDirection)
    {
        if (currentState == State.Dead) return;

        currentHealth -= Mathf.Max(1, damage);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (Time.time < nextHurtTime)
        {
            return;
        }

        nextHurtTime = Time.time + hurtCooldown;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
        }

        hurtRoutine = StartCoroutine(HurtRoutine(knockbackDirection));
    }

    public void TakeDamage(int damage)
    {
        Vector2 knockbackDirection = player != null
            ? ((Vector2)transform.position - (Vector2)player.position).normalized
            : Vector2.right;

        TakeDamage(damage, knockbackDirection);
    }

    private IEnumerator HurtRoutine(Vector2 knockbackDirection)
    {
        ChangeState(State.Hurt);
        SetGroundedMovementMode();
        ResetCombatTriggers();
        animator.SetTrigger(HurtHash);

        // Knockback
        isKnockedBack = true;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);
        isKnockedBack = false;

        // Espera animação de hurt terminar
        yield return new WaitForSeconds(hurtDuration);

        if (currentState != State.Dead)
        {
            RecoverFromHurt();
        }

        hurtRoutine = null;
    }

    // ─── Morte ───────────────────────────────────────────────────────────────
    private void Die()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
            hurtRoutine = null;
        }

        ChangeState(State.Dead);
        SetGroundedMovementMode();
        ResetCombatTriggers();
        animator.SetTrigger(DeadHash);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Desativa colisores
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        StartCoroutine(DestroyAfterDeath());
    }

    private IEnumerator DestroyAfterDeath()
    {
        // Duração aproximada da animação de morte (Dead tem 6 frames a ~12fps ≈ 0.5s)
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    // ─── Utilitários ─────────────────────────────────────────────────────────
    private void ChangeState(State newState)
    {
        currentState = newState;

        if (newState == State.Attack)
            PerformAttack();

        if (newState == State.Idle || newState == State.Dead || newState == State.Hurt || newState == State.Attack)
            SetMovementAnimation(false, false);
    }

    private void SetMovementAnimation(bool walking, bool running)
    {
        animator.SetBool(IsWalkingHash, walking);
        animator.SetBool(IsRunningHash, running);

        if (!running)
            isUsingJumpAnimation = false;

        if (!walking && !running)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void SetAirFollowAnimation(bool shouldUseJump)
    {
        if (shouldUseJump)
        {
            if (!isUsingJumpAnimation)
            {
                animator.CrossFade(JumpStateHash, 0.08f, 0);
                isUsingJumpAnimation = true;
            }

            return;
        }

        if (isUsingJumpAnimation)
        {
            animator.CrossFade(RunStateHash, 0.08f, 0);
            isUsingJumpAnimation = false;
        }
    }

    private void SetGroundedMovementMode()
    {
        isFlying = false;
        rb.gravityScale = originalGravityScale;

        if (isUsingJumpAnimation)
        {
            animator.CrossFade(RunStateHash, 0.08f, 0);
            isUsingJumpAnimation = false;
        }
    }

    private bool IsGrounded()
    {
        if (groundLayer.value == 0)
        {
            return false;
        }

        return Physics2D.OverlapCircle(GetGroundCheckPosition(), groundCheckRadius, groundLayer) != null;
    }

    private Vector2 GetGroundCheckPosition()
    {
        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;
            return new Vector2(bounds.center.x, bounds.min.y);
        }

        return transform.position;
    }

    private void ResetCombatTriggers()
    {
        animator.ResetTrigger(Attack1Hash);
        animator.ResetTrigger(Attack2Hash);
        animator.ResetTrigger(Attack3Hash);
        animator.ResetTrigger(HurtHash);
        animator.ResetTrigger(DeadHash);
    }

    private void RecoverFromHurt()
    {
        ResetCombatTriggers();

        if (player != null)
        {
            ChangeState(State.Chase);
            SetMovementAnimation(false, true);
            animator.CrossFade(RunStateHash, 0.08f, 0);
            return;
        }

        ChangeState(State.Idle);
        SetMovementAnimation(false, false);
        animator.CrossFade(IdleStateHash, 0.08f, 0);
    }

    private int GetAttackHash(int attackIndex)
    {
        switch (attackIndex)
        {
            case 1:
                return Attack1Hash;
            case 2:
                return Attack2Hash;
            default:
                return Attack3Hash;
        }
    }

    private int GetAttackStateHash(int attackIndex)
    {
        switch (attackIndex)
        {
            case 1:
                return Attack1StateHash;
            case 2:
                return Attack2StateHash;
            default:
                return Attack3StateHash;
        }
    }

    private void FacePlayer()
    {
        if (player == null)
        {
            return;
        }

        FlipSprite(player.position.x - transform.position.x);
    }

    private void IgnorePlayerBodyCollision(GameObject playerObject)
    {
        if (bodyCollider == null || playerObject == null)
        {
            return;
        }

        Collider2D[] playerColliders = playerObject.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider == null || playerCollider.isTrigger)
            {
                continue;
            }

            Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);
        }
    }

    private void FlipSprite(float directionX)
    {
        if (directionX < 0f)
            spriteRenderer.flipX = true;
        else if (directionX > 0f)
            spriteRenderer.flipX = false;
    }

    // ─── Gizmos de debug ─────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 2f, transform.position.y - activationHeightOffset, transform.position.z),
            new Vector3(transform.position.x + 2f, transform.position.y - activationHeightOffset, transform.position.z));

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

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
///   - IsMoving    (Bool)
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
    // ─── Stats ───────────────────────────────────────────────────────────────
    [Header("Stats")]
    [SerializeField] private int maxHealth = 80;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int attackDamage = 15;

    // ─── Detecção ────────────────────────────────────────────────────────────
    [Header("Detecção")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float attackRange = 1.2f;
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

    // ─── Knockback ───────────────────────────────────────────────────────────
    [Header("Knockback ao ser atingido")]
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float knockbackDuration = 0.2f;

    // ─── Componentes ─────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // ─── Estado interno ──────────────────────────────────────────────────────
    private enum State { Idle, Patrol, Chase, Attack, Hurt, Dead }
    private State currentState = State.Idle;

    private int currentHealth;
    private Transform player;
    private float attackTimer;
    private bool isPatrolWaiting;
    private Transform currentPatrolTarget;
    private bool isKnockedBack;

    // ─── Inicialização ───────────────────────────────────────────────────────
    private void Awake()
    {
        rb         = GetComponent<Rigidbody2D>();
        animator   = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        attackTimer   = 0f;

        // Detecta o player na cena (tag "Player")
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

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

    // ─── Loop principal ──────────────────────────────────────────────────────
    private void Update()
    {
        if (currentState == State.Dead || currentState == State.Hurt)
            return;

        attackTimer -= Time.deltaTime;

        float distToPlayer = player != null
            ? Vector2.Distance(transform.position, player.position)
            : float.MaxValue;

        switch (currentState)
        {
            case State.Idle:
            case State.Patrol:
                if (distToPlayer <= detectionRange)
                    ChangeState(State.Chase);
                else if (currentState == State.Patrol)
                    HandlePatrol();
                break;

            case State.Chase:
                if (distToPlayer > detectionRange)
                    ChangeState(patrolPointA != null ? State.Patrol : State.Idle);
                else if (distToPlayer <= attackRange && attackTimer <= 0f)
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
        SetMoving(false);
        yield return new WaitForSeconds(patrolWaitTime);

        currentPatrolTarget = currentPatrolTarget == patrolPointA ? patrolPointB : patrolPointA;
        isPatrolWaiting = false;
    }

    // ─── Chase ───────────────────────────────────────────────────────────────
    private void ChasePlayer()
    {
        if (player == null) return;
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        MoveInDirection(direction);
    }

    private void MoveInDirection(Vector2 direction)
    {
        if (isKnockedBack) return;

        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        SetMoving(true);
        FlipSprite(direction.x);
    }

    // ─── Attack ──────────────────────────────────────────────────────────────
    private void PerformAttack()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        SetMoving(false);

        // Escolhe um dos 3 ataques aleatoriamente
        int attackIndex = Random.Range(1, 4);
        animator.SetTrigger("Attack" + attackIndex);
        attackTimer = attackCooldown;

        StartCoroutine(ReturnToChaseAfterAttack());
    }

    private IEnumerator ReturnToChaseAfterAttack()
    {
        // Espera a animação de ataque terminar (ajuste conforme duração real)
        yield return new WaitForSeconds(1.2f);

        if (currentState != State.Dead && currentState != State.Hurt)
            ChangeState(State.Chase);
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
            // Envia dano ao player sem depender de um tipo específico.
            // O script do player precisa ter um método: void TakeDamage(int damage)
            player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    // ─── Receber dano ────────────────────────────────────────────────────────
    public void TakeDamage(int damage, Vector2 knockbackDirection)
    {
        if (currentState == State.Dead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(HurtRoutine(knockbackDirection));
    }

    private IEnumerator HurtRoutine(Vector2 knockbackDirection)
    {
        ChangeState(State.Hurt);
        animator.SetTrigger("Hurt");

        // Knockback
        isKnockedBack = true;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);
        isKnockedBack = false;

        // Espera animação de hurt terminar
        yield return new WaitForSeconds(0.5f);

        if (currentState != State.Dead)
            ChangeState(player != null ? State.Chase : State.Idle);
    }

    // ─── Morte ───────────────────────────────────────────────────────────────
    private void Die()
    {
        ChangeState(State.Dead);
        animator.SetTrigger("Dead");
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

        if (newState == State.Idle || newState == State.Dead || newState == State.Hurt)
            SetMoving(false);
    }

    private void SetMoving(bool moving)
    {
        animator.SetBool("IsMoving", moving);
        if (!moving)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
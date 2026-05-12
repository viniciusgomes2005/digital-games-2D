using System.Collections;
using UnityEngine;

/*
 * Configuracao do prefab Frog:
 * - Adicione um Rigidbody2D com Freeze Rotation Z.
 * - Adicione um BoxCollider2D ou CapsuleCollider2D.
 * - Crie child objects chamados WallCheck e EdgeCheck para a patrulha.
 * - Configure groundLayer para a layer Ground.
 * - Crie um parametro bool chamado IsMoving no Animator.
 * - Crie um parametro trigger chamado Spit no Animator.
 * - Crie um parametro trigger chamado Hurt no Animator.
 * - Crie um child AttackHitbox com BoxCollider2D trigger e deixe-o desativado.
 */
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class FrogEnemy : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform edgeCheck;
    [SerializeField] private float checkRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool startsMovingRight = true;

    [Header("Attack")]
    [SerializeField] private Transform player;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float hitboxStartDelay = 0.25f;
    [SerializeField] private float hitboxActiveTime = 0.25f;
    [SerializeField] private GameObject attackHitbox;

    [Header("Hurt")]
    [SerializeField] private int hitsToDie = 2;
    [SerializeField] private float hurtDuration = 0.4f;
    [SerializeField] private float hurtCooldown = 0.25f;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int SpitHash = Animator.StringToHash("Spit");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    private Rigidbody2D rb;
    private Animator animator;
    private EnemyAttackHitbox enemyAttackHitbox;
    private bool movingRight;
    private bool isAttacking;
    private bool isHurt;
    private bool isDead;
    private int hitsTaken;
    private float nextHurtTime;
    private float initialScaleX;
    private Coroutine attackRoutine;
    private Coroutine hurtRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        initialScaleX = Mathf.Abs(transform.localScale.x);
        if (initialScaleX <= Mathf.Epsilon)
        {
            initialScaleX = 1f;
        }

        movingRight = startsMovingRight;
        UpdateFacing();
        ResolvePlayer();
        ResolveAttackHitbox();
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        checkRadius = Mathf.Max(0.01f, checkRadius);
        attackRange = Mathf.Max(0f, attackRange);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        hitboxStartDelay = Mathf.Max(0f, hitboxStartDelay);
        hitboxActiveTime = Mathf.Max(0f, hitboxActiveTime);
        hitsToDie = Mathf.Max(1, hitsToDie);
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

        SetAttackHitboxActive(false);
        isAttacking = false;
        isHurt = false;
    }

    private void Update()
    {
        if (isAttacking || isHurt || isDead)
        {
            return;
        }

        ResolvePlayer();

        if (IsPlayerInAttackRange())
        {
            attackRoutine = StartCoroutine(AttackRoutine());
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (isAttacking || isHurt || isDead)
        {
            StopHorizontalMovement();
            SetMovingAnimation(false);
            return;
        }

        if (ShouldTurnAround())
        {
            TurnAround();
        }

        float horizontalVelocity = (movingRight ? 1f : -1f) * moveSpeed;
        Vector2 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector2(horizontalVelocity, currentVelocity.y);

        if (animator != null)
        {
            animator.SetBool(IsMovingHash, Mathf.Abs(horizontalVelocity) > 0.01f);
        }
    }

    public void TakeHit()
    {
        TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        if (isDead || Time.time < nextHurtTime)
        {
            return;
        }

        int damage = Mathf.Max(1, amount);
        nextHurtTime = Time.time + hurtCooldown;
        hitsTaken += damage;

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

        isAttacking = false;
        isHurt = true;

        StopHorizontalMovement();
        SetMovingAnimation(false);
        SetAttackHitboxActive(false);

        if (animator != null)
        {
            animator.ResetTrigger(SpitHash);
            animator.ResetTrigger(HurtHash);
            animator.SetTrigger(HurtHash);
        }

        if (hitsTaken >= hitsToDie)
        {
            isDead = true;
            hurtRoutine = StartCoroutine(DeathRoutine());
            return;
        }

        hurtRoutine = StartCoroutine(HurtRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        StopHorizontalMovement();
        SetMovingAnimation(false);
        FacePlayer();

        if (animator != null)
        {
            animator.ResetTrigger(SpitHash);
            animator.SetTrigger(SpitHash);
        }

        if (hitboxStartDelay > 0f)
        {
            yield return new WaitForSeconds(hitboxStartDelay);
        }

        if (isHurt)
        {
            isAttacking = false;
            attackRoutine = null;
            yield break;
        }

        ResetAttackHitbox();
        SetAttackHitboxActive(true);

        if (hitboxActiveTime > 0f)
        {
            yield return new WaitForSeconds(hitboxActiveTime);
        }

        SetAttackHitboxActive(false);

        if (attackCooldown > 0f)
        {
            yield return new WaitForSeconds(attackCooldown);
        }

        isAttacking = false;
        attackRoutine = null;
    }

    private IEnumerator HurtRoutine()
    {
        if (hurtDuration > 0f)
        {
            yield return new WaitForSeconds(hurtDuration);
        }

        isHurt = false;
        hurtRoutine = null;
    }

    private IEnumerator DeathRoutine()
    {
        if (hurtDuration > 0f)
        {
            yield return new WaitForSeconds(hurtDuration);
        }

        SetAttackHitboxActive(false);
        StopHorizontalMovement();
        SetMovingAnimation(false);
        gameObject.SetActive(false);
    }

    private bool ShouldTurnAround()
    {
        bool wallAhead = wallCheck != null && IsTouchingGround(wallCheck.position);
        bool groundAhead = edgeCheck == null || IsTouchingGround(edgeCheck.position);

        return wallAhead || !groundAhead;
    }

    private bool IsTouchingGround(Vector2 position)
    {
        return Physics2D.OverlapCircle(position, checkRadius, groundLayer) != null;
    }

    private bool IsPlayerInAttackRange()
    {
        if (player == null)
        {
            return false;
        }

        return Vector2.Distance(transform.position, player.position) <= attackRange;
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void ResolveAttackHitbox()
    {
        if (attackHitbox == null)
        {
            Transform hitboxTransform = transform.Find("AttackHitbox");
            if (hitboxTransform != null)
            {
                attackHitbox = hitboxTransform.gameObject;
            }
        }

        if (attackHitbox == null)
        {
            return;
        }

        enemyAttackHitbox = attackHitbox.GetComponent<EnemyAttackHitbox>();
        if (enemyAttackHitbox == null)
        {
            enemyAttackHitbox = attackHitbox.AddComponent<EnemyAttackHitbox>();
        }

        SetAttackHitboxActive(false);
    }

    private void ResetAttackHitbox()
    {
        if (enemyAttackHitbox != null)
        {
            enemyAttackHitbox.ResetHitbox();
        }
    }

    private void SetAttackHitboxActive(bool active)
    {
        if (attackHitbox != null && attackHitbox.activeSelf != active)
        {
            attackHitbox.SetActive(active);
        }
    }

    private void StopHorizontalMovement()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector2(0f, currentVelocity.y);
    }

    private void SetMovingAnimation(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool(IsMovingHash, isMoving);
        }
    }

    private void FacePlayer()
    {
        if (player == null)
        {
            return;
        }

        float directionToPlayer = player.position.x - transform.position.x;
        if (Mathf.Abs(directionToPlayer) <= Mathf.Epsilon)
        {
            return;
        }

        movingRight = directionToPlayer > 0f;
        UpdateFacing();
    }

    private void TurnAround()
    {
        movingRight = !movingRight;
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        Vector3 scale = transform.localScale;
        scale.x = initialScaleX * (movingRight ? 1f : -1f);
        transform.localScale = scale;
    }

    private void OnDrawGizmos()
    {
        DrawCheckGizmo(wallCheck, Color.red);
        DrawCheckGizmo(edgeCheck, Color.yellow);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void DrawCheckGizmo(Transform checkTransform, Color color)
    {
        if (checkTransform == null)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawWireSphere(checkTransform.position, checkRadius);
    }
}

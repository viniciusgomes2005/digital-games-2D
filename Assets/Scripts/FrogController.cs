using UnityEngine;

/*
 * Configuracao do prefab Frog:
 * - Adicione um Rigidbody2D com Freeze Rotation Z.
 * - Adicione um BoxCollider2D ou CapsuleCollider2D.
 * - Crie child objects chamados WallCheck e EdgeCheck.
 * - Configure groundLayer para a layer Ground.
 * - Crie um parametro bool chamado IsMoving no Animator.
 */
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class FrogController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform edgeCheck;
    [SerializeField] private float checkRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool startsMovingRight = true;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool movingRight;
    private bool initialFlipX;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            initialFlipX = spriteRenderer.flipX;
        }

        movingRight = startsMovingRight;
        UpdateFacing();
    }

    private void OnValidate()
    {
        checkRadius = Mathf.Max(0.01f, checkRadius);
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        // Checa antes de mover para virar antes de bater ou cair.
        if (ShouldTurnAround())
        {
            TurnAround();
        }

        float horizontalVelocity = (movingRight ? 1f : -1f) * moveSpeed;
        Vector2 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector2(horizontalVelocity, currentVelocity.y);

        // A animacao Jump e usada como animacao de movimento.
        if (animator != null)
        {
            animator.SetBool(IsMovingHash, Mathf.Abs(horizontalVelocity) > 0.01f);
        }
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

    private void TurnAround()
    {
        movingRight = !movingRight;
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        // Flip do sprite mantem o inimigo de pe, sem rotacionar o transform.
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.flipX = initialFlipX ^ !movingRight;
    }

    private void OnDrawGizmos()
    {
        DrawCheckGizmo(wallCheck, Color.red);
        DrawCheckGizmo(edgeCheck, Color.yellow);
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

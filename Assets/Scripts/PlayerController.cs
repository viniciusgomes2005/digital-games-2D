using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float doubleJumpForce = 16f;
    [SerializeField] private float jumpAttackDiveSpeed = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int DoubleJumpHash = Animator.StringToHash("DoubleJump");

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D playerCollider;
    private float horizontalInput;
    private bool isGrounded;
    private bool jumpRequested;
    private bool doubleJumpRequested;
    private bool jumpAttackDiveRequested;
    private bool canDoubleJump;
    private Vector2 knockbackVelocity;
    private float knockbackTimer;

    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;
    public bool IsAirborne => !isGrounded || Mathf.Abs(VerticalVelocity) > 0.1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        jumpForce = Mathf.Max(0f, jumpForce);
        doubleJumpForce = Mathf.Max(0f, doubleJumpForce);
        jumpAttackDiveSpeed = Mathf.Max(0f, jumpAttackDiveSpeed);
        groundCheckRadius = Mathf.Max(0.01f, groundCheckRadius);
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        isGrounded = CheckGrounded();

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
        }

        if (isGrounded)
        {
            canDoubleJump = true;
            jumpAttackDiveRequested = false;
        }

        if (Input.GetKeyDown(jumpKey))
        {
            if (isGrounded)
            {
                jumpRequested = true;
            }
            else if (canDoubleJump)
            {
                doubleJumpRequested = true;
                canDoubleJump = false;

                if (animator != null)
                {
                    animator.ResetTrigger(DoubleJumpHash);
                    animator.SetTrigger(DoubleJumpHash);
                }
            }
        }

        bool isMoving = Mathf.Abs(horizontalInput) > 0.01f;
        if (animator != null)
        {
            animator.SetBool(IsMovingHash, isMoving);
            animator.SetBool(IsGroundedHash, isGrounded);

            if (rb != null)
            {
                animator.SetFloat(VerticalVelocityHash, rb.linearVelocity.y);
            }
        }

        if (horizontalInput != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * -Mathf.Sign(horizontalInput);
            transform.localScale = scale;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 currentVelocity = rb.linearVelocity;
        if (knockbackTimer > 0f)
        {
            rb.linearVelocity = new Vector2(knockbackVelocity.x, Mathf.Max(currentVelocity.y, knockbackVelocity.y));
            jumpRequested = false;
            doubleJumpRequested = false;
            jumpAttackDiveRequested = false;
            isGrounded = CheckGrounded();
            return;
        }

        float verticalVelocity = currentVelocity.y;
        if (jumpRequested)
        {
            verticalVelocity = jumpForce;
        }
        else if (doubleJumpRequested)
        {
            verticalVelocity = doubleJumpForce;
        }
        else if (jumpAttackDiveRequested)
        {
            verticalVelocity = -jumpAttackDiveSpeed;
        }

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, verticalVelocity);
        jumpRequested = false;
        doubleJumpRequested = false;
        jumpAttackDiveRequested = false;
        isGrounded = CheckGrounded();
    }

    public void StartJumpAttackDive()
    {
        if (!IsAirborne)
        {
            return;
        }

        jumpAttackDiveRequested = true;
    }

    public void ApplyKnockback(Vector2 direction, float horizontalForce, float verticalForce, float duration)
    {
        if (horizontalForce <= 0f || duration <= 0f)
        {
            return;
        }

        float horizontalDirection = Mathf.Abs(direction.x) > 0.01f ? Mathf.Sign(direction.x) : 1f;
        knockbackVelocity = new Vector2(horizontalDirection * horizontalForce, verticalForce);
        knockbackTimer = duration;
    }

    private bool CheckGrounded()
    {
        Vector2 checkPosition = GetGroundCheckPosition();
        return Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer) != null;
    }

    private Vector2 GetGroundCheckPosition()
    {
        if (groundCheck != null)
        {
            return groundCheck.position;
        }

        if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            return new Vector2(bounds.center.x, bounds.min.y);
        }

        return transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GetGroundCheckPosition(), groundCheckRadius);
    }
}

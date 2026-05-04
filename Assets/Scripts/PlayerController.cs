using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private Rigidbody2D rb;
    private Animator animator;
    private float horizontalInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        bool isMoving = Mathf.Abs(horizontalInput) > 0.01f;
        if (animator != null)
        {
            animator.SetBool(IsMovingHash, isMoving);
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
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, currentVelocity.y);
    }
}

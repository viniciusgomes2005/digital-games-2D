using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHurt : MonoBehaviour
{
    [SerializeField] private float hurtCooldown = 0.5f;
    [SerializeField] private float defaultKnockbackDuration = 0.35f;
    [SerializeField] private bool defeatAfterHits = false;
    [SerializeField] private int hitsUntilDefeat = 3;
    [SerializeField] private bool defeatOnFall = false;
    [SerializeField] private float defeatFallY = -8f;

    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    private Animator animator;
    private Rigidbody2D rb;
    private PlayerController playerController;
    private float nextHurtTime;
    private int hitsTaken;
    private bool defeatTriggered;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
    }

    private void OnValidate()
    {
        hurtCooldown = Mathf.Max(0f, hurtCooldown);
        defaultKnockbackDuration = Mathf.Max(0f, defaultKnockbackDuration);
        hitsUntilDefeat = Mathf.Max(1, hitsUntilDefeat);
    }

    private void Update()
    {
        if (defeatTriggered || !defeatOnFall)
        {
            return;
        }

        if (transform.position.y <= defeatFallY)
        {
            TriggerDefeat();
        }
    }

    public void TakeHit()
    {
        TakeHit(Vector2.zero, 0f, 0f);
    }

    public void TakeHit(Vector2 direction, float horizontalForce, float verticalForce)
    {
        if (Time.time < nextHurtTime)
        {
            return;
        }

        nextHurtTime = Time.time + hurtCooldown;
        hitsTaken++;

        ApplyKnockback(direction, horizontalForce, verticalForce, defaultKnockbackDuration);

        if (animator != null)
        {
            animator.SetTrigger(HurtHash);
        }

        if (defeatAfterHits && hitsTaken >= hitsUntilDefeat)
        {
            TriggerDefeat();
        }
    }

    private void ApplyKnockback(Vector2 direction, float horizontalForce, float verticalForce, float duration)
    {
        if (horizontalForce <= 0f)
        {
            return;
        }

        float horizontalDirection = Mathf.Abs(direction.x) > 0.01f ? Mathf.Sign(direction.x) : 1f;

        if (playerController != null)
        {
            playerController.ApplyKnockback(direction, horizontalForce, verticalForce, duration);
            return;
        }

        if (rb == null)
        {
            return;
        }

        Vector2 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector2(horizontalDirection * horizontalForce, Mathf.Max(currentVelocity.y, verticalForce));
    }

    private void TriggerDefeat()
    {
        if (defeatTriggered)
        {
            return;
        }

        defeatTriggered = true;
        SceneManager.LoadScene("Derrota");
    }
}

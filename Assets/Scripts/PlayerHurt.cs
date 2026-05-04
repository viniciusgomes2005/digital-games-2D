using UnityEngine;

public class PlayerHurt : MonoBehaviour
{
    [SerializeField] private float hurtCooldown = 0.5f;

    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    private Animator animator;
    private float nextHurtTime;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnValidate()
    {
        hurtCooldown = Mathf.Max(0f, hurtCooldown);
    }

    public void TakeHit()
    {
        if (Time.time < nextHurtTime)
        {
            return;
        }

        nextHurtTime = Time.time + hurtCooldown;

        if (animator != null)
        {
            animator.SetTrigger(HurtHash);
        }
    }
}

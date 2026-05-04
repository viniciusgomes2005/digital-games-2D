using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyAttackHitbox : MonoBehaviour
{
    private const string PlayerTag = "Player";

    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    public void ResetHitbox()
    {
        hitTargets.Clear();
    }

    private void Awake()
    {
        Collider2D hitboxCollider = GetComponent<Collider2D>();
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
        }
    }

    private void OnDisable()
    {
        ResetHitbox();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || hitTargets.Contains(other))
        {
            return;
        }

        PlayerHurt playerHurt = other.GetComponent<PlayerHurt>();
        if (playerHurt == null)
        {
            playerHurt = other.GetComponentInParent<PlayerHurt>();
        }

        bool isPlayer = playerHurt != null || IsPlayerCollider(other);
        if (!isPlayer)
        {
            return;
        }

        Animator playerAnimator = null;
        if (playerHurt == null)
        {
            playerAnimator = other.GetComponent<Animator>();
            if (playerAnimator == null)
            {
                playerAnimator = other.GetComponentInParent<Animator>();
            }
        }

        hitTargets.Add(other);

        if (playerHurt != null)
        {
            playerHurt.TakeHit();
            return;
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(HurtHash);
        }
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        Transform current = other.transform;
        while (current != null)
        {
            if (current.CompareTag(PlayerTag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}

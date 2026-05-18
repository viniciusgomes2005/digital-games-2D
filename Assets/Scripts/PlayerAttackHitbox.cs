using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private readonly HashSet<FrogEnemy> hitTargets = new HashSet<FrogEnemy>();
    private readonly HashSet<YamabushiController> hitYamabushiTargets = new HashSet<YamabushiController>();

    public void SetDamage(int damage)
    {
        this.damage = Mathf.Max(0, damage);
    }

    public void ResetHitbox()
    {
        hitTargets.Clear();
        hitYamabushiTargets.Clear();
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
        if (other == null)
        {
            return;
        }

        TryHitYamabushi(other);

        FrogEnemy frogEnemy = other.GetComponent<FrogEnemy>();
        if (frogEnemy == null)
        {
            frogEnemy = other.GetComponentInParent<FrogEnemy>();
        }

        if (frogEnemy == null || hitTargets.Contains(frogEnemy))
        {
            return;
        }

        hitTargets.Add(frogEnemy);
        frogEnemy.TakeDamage(damage);
        return;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHitYamabushi(other);
    }

    private void TryHitYamabushi(Collider2D other)
    {
        YamabushiController yamabushi = other.GetComponent<YamabushiController>();
        if (yamabushi == null)
        {
            yamabushi = other.GetComponentInParent<YamabushiController>();
        }

        if (yamabushi == null || hitYamabushiTargets.Contains(yamabushi))
        {
            return;
        }

        Vector2 knockbackDirection = (yamabushi.transform.position - transform.root.position).normalized;
        if (knockbackDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            knockbackDirection = Vector2.right;
        }

        hitYamabushiTargets.Add(yamabushi);
        yamabushi.TakeDamage(damage, knockbackDirection);
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
    }
}

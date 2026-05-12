using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private readonly HashSet<FrogEnemy> hitTargets = new HashSet<FrogEnemy>();

    public void SetDamage(int damage)
    {
        this.damage = Mathf.Max(0, damage);
    }

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
        if (other == null)
        {
            return;
        }

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
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
    }
}

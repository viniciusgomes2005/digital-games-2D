using System.Collections;
using UnityEngine;

/*
 * Configuracao do Animator para combo de 3 ataques:
 * - Crie os estados Idle, Walk, Attack1, Attack2 e Attack3.
 * - Crie os parametros Trigger Attack, Attack2 e Attack3.
 * - Use Player_Attack1.anim no estado Attack1.
 * - Use Player_Attack2.anim no estado Attack2.
 * - Use Player_Attack3.anim no estado Attack3.
 * - Desligue Loop Time em Player_Attack1.anim, Player_Attack2.anim e Player_Attack3.anim.
 * - Prefira transicoes explicitas Idle -> Attack1 e Walk -> Attack1 com Trigger Attack.
 * - Evite Any State -> Attack1 para nao reiniciar o combo durante Attack2 ou Attack3.
 * - Desligue Has Exit Time nas transicoes Idle -> Attack1 e Walk -> Attack1.
 * - Crie Attack1 -> Attack2 com Trigger Attack2.
 * - Crie Attack2 -> Attack3 com Trigger Attack3.
 * - Ligue Has Exit Time em Attack1 -> Attack2 e Attack2 -> Attack3.
 * - Configure Exit Time dessas transicoes perto de 0.65.
 * - Crie Attack1 -> Idle com Has Exit Time ligado, Exit Time 1 e Transition Duration 0.
 * - Crie Attack2 -> Idle com Has Exit Time ligado, Exit Time 1 e Transition Duration 0.
 * - Crie Attack3 -> Idle com Has Exit Time ligado, Exit Time 1 e Transition Duration 0.
 * - Configure Transition Duration como 0 em todas as transicoes de ataque.
 *
 * As animacoes de ataque devem alterar apenas frames/propriedades visuais.
 * Nao anime Collider2D, Rigidbody2D ou Transform para preservar a fisica.
 */
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private KeyCode attackKey = KeyCode.J;
    [SerializeField] private float comboWindowStart = 0.4f;
    [SerializeField] private float comboWindowEnd = 0.9f;
    [SerializeField] private float hitboxStartDelay = 0.12f;
    [SerializeField] private float hitboxActiveTime = 0.18f;
    [SerializeField] private GameObject attackHitbox;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");
    private static readonly int Attack3Hash = Animator.StringToHash("Attack3");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    private Animator animator;
    private PlayerAttackHitbox playerAttackHitbox;
    private bool attack2Requested;
    private bool attack3Requested;
    private Coroutine attackHitboxRoutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ResolveAttackHitbox();
    }

    private void OnDisable()
    {
        StopAttackHitboxRoutine();
        SetAttackHitboxActive(false);
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        AttackState currentAttackState = GetCurrentAttackState();
        bool isTransitioningToAttack = IsAnimatorTransitioningToAttack();

        if (currentAttackState != AttackState.Attack1)
        {
            attack2Requested = false;
        }

        if (currentAttackState != AttackState.Attack2)
        {
            attack3Requested = false;
        }

        if (!Input.GetKeyDown(attackKey))
        {
            return;
        }

        if (currentAttackState == AttackState.Attack1)
        {
            TryRequestAttack2(stateInfo);
            return;
        }

        if (currentAttackState == AttackState.Attack2)
        {
            TryRequestAttack3(stateInfo);
            return;
        }

        if (currentAttackState == AttackState.Attack3)
        {
            // Attack3 e o fim do combo por enquanto.
            animator.ResetTrigger(AttackHash);
            return;
        }

        if (isTransitioningToAttack)
        {
            animator.ResetTrigger(AttackHash);
            return;
        }

        animator.SetTrigger(AttackHash);
        StartAttackHitboxWindow();
    }

    public void TriggerHurt()
    {
        if (animator == null)
        {
            return;
        }

        // Dispare este metodo quando o personagem receber hit.
        animator.SetTrigger(HurtHash);
    }

    private void TryRequestAttack2(AnimatorStateInfo attack1StateInfo)
    {
        if (attack2Requested)
        {
            return;
        }

        float attackProgress = attack1StateInfo.normalizedTime;
        bool isInsideComboWindow = attackProgress >= comboWindowStart && attackProgress <= comboWindowEnd;

        if (!isInsideComboWindow)
        {
            return;
        }

        attack2Requested = true;
        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(Attack2Hash);
        StartAttackHitboxWindow();
    }

    private void TryRequestAttack3(AnimatorStateInfo attack2StateInfo)
    {
        if (attack3Requested)
        {
            return;
        }

        float attackProgress = attack2StateInfo.normalizedTime;
        bool isInsideComboWindow = attackProgress >= comboWindowStart && attackProgress <= comboWindowEnd;

        if (!isInsideComboWindow)
        {
            return;
        }

        attack3Requested = true;
        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(Attack2Hash);
        animator.SetTrigger(Attack3Hash);
        StartAttackHitboxWindow();
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

        playerAttackHitbox = attackHitbox.GetComponent<PlayerAttackHitbox>();
        if (playerAttackHitbox == null)
        {
            playerAttackHitbox = attackHitbox.AddComponent<PlayerAttackHitbox>();
        }

        SetAttackHitboxActive(false);
    }

    private void StartAttackHitboxWindow()
    {
        if (attackHitbox == null)
        {
            return;
        }

        StopAttackHitboxRoutine();
        attackHitboxRoutine = StartCoroutine(AttackHitboxRoutine());
    }

    private IEnumerator AttackHitboxRoutine()
    {
        SetAttackHitboxActive(false);

        if (hitboxStartDelay > 0f)
        {
            yield return new WaitForSeconds(hitboxStartDelay);
        }

        ResetAttackHitbox();
        SetAttackHitboxActive(true);

        if (hitboxActiveTime > 0f)
        {
            yield return new WaitForSeconds(hitboxActiveTime);
        }

        SetAttackHitboxActive(false);
        attackHitboxRoutine = null;
    }

    private void StopAttackHitboxRoutine()
    {
        if (attackHitboxRoutine == null)
        {
            return;
        }

        StopCoroutine(attackHitboxRoutine);
        attackHitboxRoutine = null;
    }

    private void ResetAttackHitbox()
    {
        if (playerAttackHitbox != null)
        {
            playerAttackHitbox.ResetHitbox();
        }
    }

    private void SetAttackHitboxActive(bool active)
    {
        if (attackHitbox != null && attackHitbox.activeSelf != active)
        {
            attackHitbox.SetActive(active);
        }
    }

    private AttackState GetCurrentAttackState()
    {
        AnimatorStateInfo currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (currentStateInfo.IsName("Attack1"))
        {
            return AttackState.Attack1;
        }

        if (currentStateInfo.IsName("Attack2"))
        {
            return AttackState.Attack2;
        }

        if (currentStateInfo.IsName("Attack3"))
        {
            return AttackState.Attack3;
        }

        return AttackState.None;
    }

    private bool IsAnimatorTransitioningToAttack()
    {
        if (!animator.IsInTransition(0))
        {
            return false;
        }

        AnimatorStateInfo nextStateInfo = animator.GetNextAnimatorStateInfo(0);

        if (nextStateInfo.IsName("Attack1"))
        {
            return true;
        }

        if (nextStateInfo.IsName("Attack2"))
        {
            return true;
        }

        if (nextStateInfo.IsName("Attack3"))
        {
            return true;
        }

        return false;
    }

    private void OnValidate()
    {
        comboWindowStart = Mathf.Clamp01(comboWindowStart);
        comboWindowEnd = Mathf.Clamp01(comboWindowEnd);
        hitboxStartDelay = Mathf.Max(0f, hitboxStartDelay);
        hitboxActiveTime = Mathf.Max(0f, hitboxActiveTime);

        if (comboWindowEnd < comboWindowStart)
        {
            comboWindowEnd = comboWindowStart;
        }
    }

    private enum AttackState
    {
        None,
        Attack1,
        Attack2,
        Attack3
    }
}

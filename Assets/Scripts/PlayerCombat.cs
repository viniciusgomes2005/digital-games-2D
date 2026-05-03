using UnityEngine;

/*
 * Configuracao do Animator para combo simples:
 * - Crie os estados Idle, Walk, Attack1 e Attack2.
 * - Crie um parametro Trigger chamado Attack.
 * - Crie um parametro Trigger chamado Attack2.
 * - Use Player_Attack1.anim no estado Attack1.
 * - Use Player_Attack2.anim no estado Attack2.
 * - Crie Any State -> Attack1 com Trigger Attack.
 * - Desligue Has Exit Time em Any State -> Attack1.
 * - Configure Transition Duration como 0 em Any State -> Attack1.
 * - Crie Attack1 -> Attack2 com Trigger Attack2.
 * - Ligue Has Exit Time em Attack1 -> Attack2.
 * - Configure Exit Time de Attack1 -> Attack2 perto de 0.65.
 * - Configure Transition Duration como 0 em Attack1 -> Attack2.
 * - Crie Attack1 -> Idle com Has Exit Time ligado, Exit Time 1 e Transition Duration 0.
 * - Crie Attack2 -> Idle com Has Exit Time ligado, Exit Time 1 e Transition Duration 0.
 * - Desligue Loop Time em Player_Attack1.anim e Player_Attack2.anim.
 *
 * As animacoes de ataque devem alterar apenas frames/propriedades visuais.
 * Nao anime Collider2D, Rigidbody2D ou Transform para preservar a fisica.
 */
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private KeyCode attackKey = KeyCode.J;
    [SerializeField] private float comboWindowStart = 0.4f;
    [SerializeField] private float comboWindowEnd = 0.9f;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");

    private Animator animator;
    private bool attack2Requested;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isInAttack1 = IsCurrentOrNextState("Attack1");
        bool isInAttack2 = IsCurrentOrNextState("Attack2");

        if (!isInAttack1)
        {
            attack2Requested = false;
        }

        if (!Input.GetKeyDown(attackKey))
        {
            return;
        }

        if (isInAttack1)
        {
            TryRequestAttack2(stateInfo);
            return;
        }

        if (!isInAttack2)
        {
            animator.SetTrigger(AttackHash);
        }
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
    }

    private bool IsCurrentOrNextState(string stateName)
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
        {
            return true;
        }

        return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(stateName);
    }

    private void OnValidate()
    {
        comboWindowStart = Mathf.Clamp01(comboWindowStart);
        comboWindowEnd = Mathf.Clamp01(comboWindowEnd);

        if (comboWindowEnd < comboWindowStart)
        {
            comboWindowEnd = comboWindowStart;
        }
    }
}

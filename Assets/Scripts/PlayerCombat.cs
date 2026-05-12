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
    [Header("Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.J;

    [Header("Combo")]
    [SerializeField] private float comboWindowStart = 0.4f;
    [SerializeField] private float comboWindowEnd = 0.9f;
    [SerializeField, Range(0f, 1f)] private float unlockAttack2At = 0.2f;
    [SerializeField, Range(0f, 1f)] private float unlockAttack3At = 0.5f;
    [SerializeField] private int attack1Damage = 1;
    [SerializeField] private int attack2Damage = 2;
    [SerializeField] private int attack3Damage = 3;

    [Header("Hitbox")]
    [SerializeField] private float hitboxStartDelay = 0.12f;
    [SerializeField] private float hitboxActiveTime = 0.18f;
    [SerializeField] private float jumpAttackDiveDelay = 0.06f;
    [SerializeField] private GameObject attackHitbox;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerAttackHitbox playerAttackHitbox;
    [SerializeField] private YokaiEnergyBarController energyController;
    [SerializeField] private GameObject unlockMessageReceiver;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");
    private static readonly int Attack3Hash = Animator.StringToHash("Attack3");
    private static readonly int JumpAttackHash = Animator.StringToHash("JumpAttack");
    private static readonly int JumpAttackStateHash = Animator.StringToHash("JumpAttack");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    private PlayerController playerController;
    private bool attack2Requested;
    private bool attack3Requested;
    private bool airAttackUsed;
    private int previousMaxUnlockedComboStep = 1;
    private int currentAttackDamage = 1;
    private Coroutine attackHitboxRoutine;
    private Coroutine jumpAttackDiveRoutine;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        playerController = GetComponent<PlayerController>();
        ResolveEnergyController();
        ResolveAttackHitbox();
        previousMaxUnlockedComboStep = GetMaxUnlockedComboStep();
    }

    private void OnDisable()
    {
        StopAttackHitboxRoutine();
        StopJumpAttackDiveRoutine();
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
        bool isGrounded = playerController == null || playerController.IsGrounded;
        bool isAirborne = playerController != null && playerController.IsAirborne;

        CheckUnlockFeedback();

        if (isGrounded && !isAirborne)
        {
            airAttackUsed = false;
        }

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

        if (isAirborne)
        {
            TryRequestJumpAttack(currentAttackState, isTransitioningToAttack);
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
        StartAttackHitboxWindow(attack1Damage);
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
        if (GetMaxUnlockedComboStep() < 2)
        {
            animator.ResetTrigger(Attack2Hash);
            return;
        }

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
        StartAttackHitboxWindow(attack2Damage);
    }

    private void TryRequestAttack3(AnimatorStateInfo attack2StateInfo)
    {
        if (GetMaxUnlockedComboStep() < 3)
        {
            animator.ResetTrigger(Attack3Hash);
            return;
        }

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
        StartAttackHitboxWindow(attack3Damage);
    }

    private void TryRequestJumpAttack(AttackState currentAttackState, bool isTransitioningToAttack)
    {
        if (airAttackUsed || currentAttackState != AttackState.None || isTransitioningToAttack)
        {
            return;
        }

        airAttackUsed = true;
        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(Attack2Hash);
        animator.ResetTrigger(Attack3Hash);
        animator.ResetTrigger(JumpAttackHash);
        animator.SetTrigger(JumpAttackHash);
        animator.Play(JumpAttackStateHash, 0, 0f);

        StartJumpAttackDiveRoutine();
        StartAttackHitboxWindow(attack1Damage);
    }

    private int GetMaxUnlockedComboStep()
    {
        float energy = energyController != null ? energyController.NormalizedEnergy : 0f;

        if (energy >= unlockAttack3At)
        {
            return 3;
        }

        if (energy >= unlockAttack2At)
        {
            return 2;
        }

        return 1;
    }

    private void CheckUnlockFeedback()
    {
        int maxUnlockedComboStep = GetMaxUnlockedComboStep();
        if (maxUnlockedComboStep <= previousMaxUnlockedComboStep)
        {
            return;
        }

        if (maxUnlockedComboStep >= 2 && previousMaxUnlockedComboStep < 2)
        {
            ShowUnlockMessage("Combo II Unlocked");
        }

        if (maxUnlockedComboStep >= 3 && previousMaxUnlockedComboStep < 3)
        {
            ShowUnlockMessage("Combo III Unlocked");
        }

        previousMaxUnlockedComboStep = maxUnlockedComboStep;
    }

    private void ShowUnlockMessage(string message)
    {
        if (unlockMessageReceiver == null)
        {
            return;
        }

        unlockMessageReceiver.SendMessage("ShowUnlockMessage", message, SendMessageOptions.DontRequireReceiver);
    }

    private void ResolveEnergyController()
    {
        if (energyController != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        energyController = FindAnyObjectByType<YokaiEnergyBarController>();
#else
        energyController = FindObjectOfType<YokaiEnergyBarController>();
#endif
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

    private void StartAttackHitboxWindow(int damage)
    {
        if (attackHitbox == null)
        {
            return;
        }

        currentAttackDamage = Mathf.Max(0, damage);
        if (playerAttackHitbox != null)
        {
            playerAttackHitbox.SetDamage(currentAttackDamage);
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
        if (playerAttackHitbox != null)
        {
            playerAttackHitbox.SetDamage(currentAttackDamage);
        }

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

    private void StartJumpAttackDiveRoutine()
    {
        if (playerController == null)
        {
            return;
        }

        StopJumpAttackDiveRoutine();
        jumpAttackDiveRoutine = StartCoroutine(JumpAttackDiveRoutine());
    }

    private IEnumerator JumpAttackDiveRoutine()
    {
        if (jumpAttackDiveDelay > 0f)
        {
            yield return new WaitForSeconds(jumpAttackDiveDelay);
        }

        playerController.StartJumpAttackDive();
        jumpAttackDiveRoutine = null;
    }

    private void StopJumpAttackDiveRoutine()
    {
        if (jumpAttackDiveRoutine == null)
        {
            return;
        }

        StopCoroutine(jumpAttackDiveRoutine);
        jumpAttackDiveRoutine = null;
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

        if (currentStateInfo.IsName("3x_attack"))
        {
            return AttackState.Attack3;
        }

        if (currentStateInfo.IsName("JumpAttack"))
        {
            return AttackState.JumpAttack;
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

        if (nextStateInfo.IsName("3x_attack"))
        {
            return true;
        }

        if (nextStateInfo.IsName("JumpAttack"))
        {
            return true;
        }

        return false;
    }

    private void OnValidate()
    {
        comboWindowStart = Mathf.Clamp01(comboWindowStart);
        comboWindowEnd = Mathf.Clamp01(comboWindowEnd);
        unlockAttack2At = Mathf.Clamp01(unlockAttack2At);
        unlockAttack3At = Mathf.Clamp01(unlockAttack3At);
        attack1Damage = Mathf.Max(0, attack1Damage);
        attack2Damage = Mathf.Max(0, attack2Damage);
        attack3Damage = Mathf.Max(0, attack3Damage);
        hitboxStartDelay = Mathf.Max(0f, hitboxStartDelay);
        hitboxActiveTime = Mathf.Max(0f, hitboxActiveTime);
        jumpAttackDiveDelay = Mathf.Max(0f, jumpAttackDiveDelay);

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
        Attack3,
        JumpAttack
    }
}

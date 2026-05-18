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
    [SerializeField] private AudioSource damageAudioSource;
    [SerializeField] private AudioClip damageAudioClip;
    [SerializeField, Range(0f, 1f)] private float damageAudioVolume = 1f;
    [SerializeField] private YokaiEnergyBarController energyController;
    [SerializeField] private float damageEnergySeconds = 15f;

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
        if (damageAudioSource == null)
        {
            damageAudioSource = GetComponent<AudioSource>();
        }

        ResolveEnergyController();
    }

    private void OnValidate()
    {
        hurtCooldown = Mathf.Max(0f, hurtCooldown);
        defaultKnockbackDuration = Mathf.Max(0f, defaultKnockbackDuration);
        hitsUntilDefeat = Mathf.Max(1, hitsUntilDefeat);
        damageAudioVolume = Mathf.Clamp01(damageAudioVolume);
        damageEnergySeconds = Mathf.Max(0f, damageEnergySeconds);
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
        PlayDamageAudio();
        AddDamageEnergyTime();

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

    private void PlayDamageAudio()
    {
        if (damageAudioClip == null)
        {
            return;
        }

        if (damageAudioSource == null)
        {
            damageAudioSource = gameObject.AddComponent<AudioSource>();
        }

        damageAudioSource.spatialBlend = 0f;
        damageAudioSource.PlayOneShot(damageAudioClip, damageAudioVolume);
    }

    private void AddDamageEnergyTime()
    {
        if (damageEnergySeconds <= 0f)
        {
            return;
        }

        ResolveEnergyController();
        if (energyController != null)
        {
            energyController.AddSeconds(damageEnergySeconds);
        }
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

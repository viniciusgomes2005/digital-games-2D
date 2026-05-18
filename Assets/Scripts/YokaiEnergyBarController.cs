using UnityEngine;

public class YokaiEnergyBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Material energyBarMaterial;
    [SerializeField] private ParticleSystem energyParticleSystem;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;

    [Header("Energy")]
    [SerializeField] private bool autoStartOnPlay = true;
    [SerializeField] private bool syncWithMusic = true;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private bool loopMusic = false;
    [SerializeField] private float durationInSeconds = 60f;
    [SerializeField] private string fillPropertyName = "_Fill";
    [SerializeField, Range(0f, 1f)] private float normalizedEnergy;

    [Header("Particles")]
    [SerializeField] private float minEmissionRate = 0f;
    [SerializeField] private float maxEmissionRate = 60f;
    [SerializeField] private float minStartSpeed = 0.25f;
    [SerializeField] private float maxStartSpeed = 2.5f;
    [SerializeField] private float minStartSize = 0.08f;
    [SerializeField] private float maxStartSize = 0.18f;

    [Header("Optional Particle Color")]
    [SerializeField] private bool scaleParticleColor = false;
    [SerializeField] private Color minParticleColor = new Color(1f, 0.45f, 0.05f, 0.35f);
    [SerializeField] private Color maxParticleColor = new Color(1f, 0.9f, 0.2f, 1f);

    private bool isCharging;
    private float chargeTimer;

    public float NormalizedEnergy => normalizedEnergy;

    private void Awake()
    {
        ResolveMusicSource();
        ApplyMusicSettings();
    }

    private void Start()
    {
        if (autoStartOnPlay)
        {
            StartCharging();
            return;
        }

        ApplyEnergy();
    }

    private void Update()
    {
        if (!isCharging)
        {
            return;
        }

        chargeTimer += Time.deltaTime;
        SetEnergyFromTimer(GetChargeProgress());

        if (normalizedEnergy >= 1f)
        {
            isCharging = false;
        }
    }

    private void OnValidate()
    {
        durationInSeconds = Mathf.Max(0.01f, durationInSeconds);
        minEmissionRate = Mathf.Max(0f, minEmissionRate);
        maxEmissionRate = Mathf.Max(minEmissionRate, maxEmissionRate);
        minStartSpeed = Mathf.Max(0f, minStartSpeed);
        maxStartSpeed = Mathf.Max(minStartSpeed, maxStartSpeed);
        minStartSize = Mathf.Max(0f, minStartSize);
        maxStartSize = Mathf.Max(minStartSize, maxStartSize);
        normalizedEnergy = Mathf.Clamp01(normalizedEnergy);
    }

    public void SetEnergy(float value)
    {
        isCharging = false;
        chargeTimer = Mathf.Clamp01(value) * Mathf.Max(0.01f, durationInSeconds);
        SetEnergyFromTimer(value);
    }

    public void StartCharging()
    {
        ResetEnergy();
        isCharging = true;
        EnsureParticlesArePlaying();
        StartMusic();
    }

    public void ResetEnergy()
    {
        isCharging = false;
        chargeTimer = 0f;
        SetEnergyFromTimer(0f);

        if (energyParticleSystem != null)
        {
            energyParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void SetEnergyFromTimer(float value)
    {
        normalizedEnergy = Mathf.Clamp01(value);
        ApplyEnergy();
    }

    private float GetChargeProgress()
    {
        if (syncWithMusic && musicSource != null && musicSource.clip != null)
        {
            float clipLength = Mathf.Max(0.01f, musicSource.clip.length);
            return musicSource.time / clipLength;
        }

        float duration = Mathf.Max(0.01f, durationInSeconds);
        return chargeTimer / duration;
    }

    private void ApplyEnergy()
    {
        UpdateMaterialFill();
        UpdateParticles();
    }

    private void UpdateMaterialFill()
    {
        if (energyBarMaterial == null || string.IsNullOrWhiteSpace(fillPropertyName))
        {
            return;
        }

        energyBarMaterial.SetFloat(fillPropertyName, normalizedEnergy);
    }

    private void UpdateParticles()
    {
        if (energyParticleSystem == null)
        {
            return;
        }

        EnsureParticlesArePlaying();

        ParticleSystem.EmissionModule emission = energyParticleSystem.emission;
        emission.rateOverTime = Mathf.Lerp(minEmissionRate, maxEmissionRate, normalizedEnergy);

        ParticleSystem.MainModule main = energyParticleSystem.main;
        main.startSpeed = Mathf.Lerp(minStartSpeed, maxStartSpeed, normalizedEnergy);
        main.startSize = Mathf.Lerp(minStartSize, maxStartSize, normalizedEnergy);

        if (scaleParticleColor)
        {
            main.startColor = Color.Lerp(minParticleColor, maxParticleColor, normalizedEnergy);
        }
    }

    private void EnsureParticlesArePlaying()
    {
        if (energyParticleSystem != null && !energyParticleSystem.isPlaying)
        {
            energyParticleSystem.Play();
        }
    }

    private void ResolveMusicSource()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource == null && musicClip != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void ApplyMusicSettings()
    {
        if (musicSource == null)
        {
            return;
        }

        if (musicClip != null)
        {
            musicSource.clip = musicClip;
            durationInSeconds = Mathf.Max(0.01f, musicClip.length);
        }

        musicSource.spatialBlend = 0f;
        musicSource.loop = loopMusic;
    }

    private void StartMusic()
    {
        if (!playMusicOnStart || musicSource == null || musicSource.clip == null)
        {
            return;
        }

        musicSource.time = 0f;
        musicSource.Play();
    }
}

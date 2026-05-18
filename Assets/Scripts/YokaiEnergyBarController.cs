using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private bool loadDefeatWhenFull = true;
    [SerializeField] private string defeatSceneName = "Derrota";
    [SerializeField] private float musicFallbackDelay = 0.35f;
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
    private bool defeatTriggered;
    private float chargeTimer;
    private float lastMusicTime;
    private float musicStalledTimer;

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
            TriggerDefeatIfNeeded();
        }
    }

    private void OnValidate()
    {
        durationInSeconds = Mathf.Max(0.01f, durationInSeconds);
        musicFallbackDelay = Mathf.Max(0f, musicFallbackDelay);
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

    public void AddSeconds(float seconds)
    {
        if (seconds <= 0f)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, durationInSeconds);
        chargeTimer = Mathf.Clamp(chargeTimer + seconds, 0f, duration);
        SetEnergyFromTimer(chargeTimer / duration);
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
        defeatTriggered = false;
        chargeTimer = 0f;
        lastMusicTime = 0f;
        musicStalledTimer = 0f;
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

        if (normalizedEnergy >= 1f)
        {
            TriggerDefeatIfNeeded();
        }
    }

    private float GetChargeProgress()
    {
        if (syncWithMusic && musicSource != null && musicSource.clip != null)
        {
            if (musicSource.isPlaying && musicSource.time > lastMusicTime + 0.001f)
            {
                lastMusicTime = musicSource.time;
                musicStalledTimer = 0f;
            }
            else
            {
                musicStalledTimer += Time.deltaTime;
            }

            if (musicStalledTimer <= musicFallbackDelay)
            {
                float activeDuration = Mathf.Max(0.01f, durationInSeconds);
                float activeTimerProgress = chargeTimer / activeDuration;
                float activeMusicProgress = musicSource.time / activeDuration;
                return Mathf.Max(activeTimerProgress, activeMusicProgress);
            }

            if (!musicSource.isPlaying && playMusicOnStart)
            {
                StartMusic();
            }

            float timerProgress = chargeTimer / Mathf.Max(0.01f, durationInSeconds);
            float musicProgress = musicSource.time / Mathf.Max(0.01f, durationInSeconds);
            return Mathf.Max(timerProgress, musicProgress);
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
        lastMusicTime = 0f;
        musicStalledTimer = 0f;
        musicSource.Play();
    }

    private void TriggerDefeatIfNeeded()
    {
        if (!loadDefeatWhenFull || defeatTriggered || string.IsNullOrWhiteSpace(defeatSceneName))
        {
            return;
        }

        defeatTriggered = true;
        SceneManager.LoadScene(defeatSceneName);
    }
}

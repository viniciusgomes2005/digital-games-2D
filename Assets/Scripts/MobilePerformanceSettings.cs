using UnityEngine;

public class MobilePerformanceSettings : MonoBehaviour
{
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private int maxParticleCount = 120;
    [SerializeField] private bool forceMusicTo2D = true;

    private void Awake()
    {
        Application.targetFrameRate = targetFrameRate;

        if (forceMusicTo2D)
        {
            AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource == null || !audioSource.loop)
                {
                    continue;
                }

                audioSource.spatialBlend = 0f;
            }
        }

        ClampParticleSystems();
    }

    private void OnValidate()
    {
        targetFrameRate = Mathf.Max(30, targetFrameRate);
        maxParticleCount = Mathf.Max(16, maxParticleCount);
    }

    private void ClampParticleSystems()
    {
        ParticleSystem[] particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particleSystem.main;
            main.maxParticles = Mathf.Min(main.maxParticles, maxParticleCount);
        }
    }
}

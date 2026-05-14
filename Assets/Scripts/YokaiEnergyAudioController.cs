using UnityEngine;

public class YokaiEnergyAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private YokaiEnergyBarController energyController;
    [SerializeField] private AudioSource audioSource;

    [Header("Volume")]
    [SerializeField] private float minVolume = 0.02f;
    [SerializeField] private float maxVolume = 0.45f;

    [Header("Pitch")]
    [SerializeField] private float minPitch = 1f;
    [SerializeField] private float maxPitch = 1f;

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.spatialBlend = 0f;
        audioSource.loop = loop;
        audioSource.volume = minVolume;
        audioSource.pitch = minPitch;

        if (playOnStart && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void Update()
    {
        if (energyController == null || audioSource == null)
        {
            return;
        }

        float energy = Mathf.Clamp01(energyController.NormalizedEnergy);

        audioSource.volume = Mathf.Lerp(minVolume, maxVolume, energy);
        audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, energy);
    }
}

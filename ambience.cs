
using UnityEngine;

public class AmbienceSound : MonoBehaviour
{
    [Header("AMBIENCE SETTINGS")]
    [SerializeField] private AudioClip ambienceClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = ambienceClip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (ambienceClip != null)
            audioSource.Play();
    }

    private void Update()
    {
        audioSource.volume = volume;
    }
}

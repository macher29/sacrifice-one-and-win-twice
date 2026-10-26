
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Pressure Plate")]
    [SerializeField] private Transform plate;
    [SerializeField] private float pressDepth = 0.18f;
    [SerializeField] private float moveSpeed = 8f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip pressSound;
    [SerializeField] private AudioClip releaseSound;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private Vector3 originalPosition;
    private Vector3 pressedPosition;

    private AudioSource audioSource;
    private bool isPressed;
    public bool IsPressed => isPressed;

    private void Start()
    {
        if (plate == null)
        {
            Debug.LogError(
                "PressurePlate: Assign the visible plate in the Inspector!",
                this
            );

            enabled = false;
            return;
        }

        originalPosition = plate.localPosition;
        pressedPosition = originalPosition - Vector3.up * pressDepth;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    private void Update()
    {
        Vector3 targetPosition = isPressed
            ? pressedPosition
            : originalPosition;

        plate.localPosition = Vector3.Lerp(
            plate.localPosition,
            targetPosition,
            Time.deltaTime * moveSpeed
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPressed)
        {
            isPressed = true;

            if (pressSound != null)
            {
                audioSource.PlayOneShot(pressSound, volume);
            }

            Debug.Log("Pressure plate pressed!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isPressed)
        {
            isPressed = false;

            if (releaseSound != null)
            {
                audioSource.PlayOneShot(releaseSound, volume);
            }

            Debug.Log("Pressure plate released!");
        }
    }
}

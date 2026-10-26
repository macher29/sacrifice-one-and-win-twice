
using UnityEngine;

public class JailGateController : MonoBehaviour
{
    [Header("Player Detection Trigger")]
    [SerializeField] private Collider triggerBox;

    [Header("Gate Model")]
    [SerializeField] private Transform gate;
    [SerializeField] private Vector3 openDirection = Vector3.right;
    [SerializeField] private float openDistance = 2f;

    [Header("Movement Speed")]
    [SerializeField] private float openingSpeed = 0.25f;
    [SerializeField] private float closingSpeed = 0.15f;

    [Header("Gate Sounds")]
    [SerializeField] private AudioClip openingSound;
    [SerializeField] private AudioClip closingSound;
    [SerializeField, Range(0f, 1f)]
    private float soundVolume = 1f;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private AudioSource gateAudioSource;
    private bool playerInside;
    private bool wasPlayerInside;
    private bool forcedOpen;

    private void Awake()
    {
        if (gate == null || triggerBox == null)
        {
            Debug.LogError(
                "JailGateController: Assign both Gate and Trigger Box in the Inspector.",
                this
            );

            enabled = false;
            return;
        }

        closedPosition = gate.localPosition;

        Vector3 direction = openDirection.sqrMagnitude > 0.001f
            ? openDirection.normalized
            : Vector3.right;

        openPosition = closedPosition +
                       direction * Mathf.Max(0f, openDistance);

        gateAudioSource = GetComponent<AudioSource>();

        if (gateAudioSource == null)
            gateAudioSource = gameObject.AddComponent<AudioSource>();

        gateAudioSource.playOnAwake = false;
        gateAudioSource.loop = false;
        gateAudioSource.spatialBlend = 1f;

        playerInside = false;
        wasPlayerInside = false;
        forcedOpen = false;
    }

    private void Update()
    {
        if (gate == null || triggerBox == null)
            return;

        if (!forcedOpen)
        {
            playerInside = IsPlayerInsideTrigger();

            if (playerInside != wasPlayerInside)
            {
                PlayGateSound(
                    playerInside ? openingSound : closingSound
                );

                wasPlayerInside = playerInside;
            }
        }

        Vector3 targetPosition = (forcedOpen || playerInside)
            ? openPosition
            : closedPosition;

        float speed = (forcedOpen || playerInside)
            ? openingSpeed
            : closingSpeed;

        gate.localPosition = Vector3.MoveTowards(
            gate.localPosition,
            targetPosition,
            speed * Time.deltaTime
        );
    }

    private bool IsPlayerInsideTrigger()
    {
        if (!triggerBox.enabled ||
            !triggerBox.gameObject.activeInHierarchy)
        {
            return false;
        }

        Collider[] overlappingColliders = Physics.OverlapBox(
            triggerBox.bounds.center,
            triggerBox.bounds.extents,
            triggerBox.transform.rotation
        );

        foreach (Collider hit in overlappingColliders)
        {
            if (hit == triggerBox)
                continue;

            if (hit.CompareTag("Player"))
                return true;
        }

        return false;
    }

    private void PlayGateSound(AudioClip clip)
    {
        if (clip != null && gateAudioSource != null)
            gateAudioSource.PlayOneShot(clip, soundVolume);
    }

    public void OpenGatePermanently()
    {
        if (gate == null)
            return;

        if (!forcedOpen)
        {
            forcedOpen = true;
            PlayGateSound(openingSound);
        }

        Debug.Log("[JailGate] Gate forced open.", this);
    }

    public void ResetGate()
    {
        if (gate == null)
            return;

        forcedOpen = false;
        playerInside = false;
        wasPlayerInside = false;

        gate.localPosition = closedPosition;
    }
}

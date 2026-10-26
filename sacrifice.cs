
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SacrificeInteraction : MonoBehaviour
{
    [Header("INTERACTION")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool canSacrifice = true;

    [Header("SCREEN EFFECTS")]
    [SerializeField] private Image blackScreen;
    [SerializeField] private Image whiteScreen;
    [SerializeField] private float blackFlashDuration = 0.15f;
    [SerializeField] private float whiteScreenDuration = 2.5f;
    [SerializeField] private float whiteFadeInDuration = 0.15f;
    [SerializeField] private float whiteFadeOutDuration = 0.8f;

    [Header("CINEMATIC TEXT")]
    [SerializeField] private TMP_Text cinematicText;
    [SerializeField] private float textFadeInDuration = 1f;
    [SerializeField] private float textDisplayDuration = 2f;
    [SerializeField] private float textFadeOutDuration = 0.8f;

    [Header("SOUND EFFECTS")]
    [SerializeField] private AudioClip sacrificeSound;
    [SerializeField] private AudioClip flashbackSound;
    [SerializeField, Range(0f, 1f)]
    private float soundVolume = 1f;

    [Header("CLONE TRANSITION")]
    [SerializeField] private GameObject deadBodyModel;
    [SerializeField] private Transform cloneSpawnPoint;

    [Header("OBJECT TO DISABLE AFTER SACRIFICE")]
    [SerializeField] private GameObject objectToDisableAfterSacrifice;

    private AudioSource audioSource;
    private CharacterController characterController;
    private bool sequenceRunning;
    private bool inputWarningShown;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        characterController = GetComponent<CharacterController>();

        SetImageAlpha(blackScreen, 0f);
        SetImageAlpha(whiteScreen, 0f);
        SetTextAlpha(cinematicText, 0f);

        Debug.Log(
            "[Sacrifice] Awake: script is active on " + gameObject.name,
            this
        );
    }

    private void OnEnable()
    {
        Debug.Log("[Sacrifice] Script enabled.", this);
    }

    private void Update()
    {
        if (sequenceRunning || !canSacrifice)
            return;

        bool keyPressed = false;
        bool mouseClicked = false;

        // Old Input Manager.
        try
        {
            keyPressed = Input.GetKeyDown(interactionKey);
            mouseClicked = Input.GetMouseButtonDown(0);
        }
        catch (System.Exception exception)
        {
            if (!inputWarningShown)
            {
                Debug.LogWarning(
                    "[Sacrifice] Legacy input unavailable: " +
                    exception.Message,
                    this
                );

                inputWarningShown = true;
            }
        }

        // New Input System.
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            interactionKey == KeyCode.E &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            keyPressed = true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            mouseClicked = true;
        }
#endif

        if (keyPressed)
        {
            Debug.Log("[Sacrifice] E KEY PRESS REGISTERED.", this);
            StartSacrifice();
        }

        if (mouseClicked)
        {
            Debug.Log("[Sacrifice] LEFT MOUSE CLICK REGISTERED.", this);
        }
    }

    private void StartSacrifice()
    {
        if (sequenceRunning || !canSacrifice)
            return;

        Debug.Log("[Sacrifice] Starting sacrifice sequence.", this);
        StartCoroutine(PlaySacrificeSequence());
    }

    private IEnumerator PlaySacrificeSequence()
    {
        sequenceRunning = true;
        canSacrifice = false;

        Debug.Log("[Sacrifice] Step 1: Black flash.", this);

        if (sacrificeSound != null)
            audioSource.PlayOneShot(sacrificeSound, soundVolume);

        SetImageAlpha(blackScreen, 1f);

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, blackFlashDuration)
        );

        SetImageAlpha(blackScreen, 0f);

        Debug.Log("[Sacrifice] Step 2: White screen.", this);

        yield return StartCoroutine(
            FadeImage(whiteScreen, 0f, 1f, whiteFadeInDuration)
        );

        if (flashbackSound != null)
            audioSource.PlayOneShot(flashbackSound, soundVolume);

        Debug.Log("[Sacrifice] Step 3: Cinematic text.", this);

        yield return StartCoroutine(
            FadeText(cinematicText, 0f, 1f, textFadeInDuration)
        );

        float remainingTime = Mathf.Max(
            0f,
            whiteScreenDuration - textFadeInDuration
        );

        if (remainingTime > 0f)
            yield return new WaitForSecondsRealtime(remainingTime);

        yield return StartCoroutine(
            FadeText(cinematicText, 1f, 0f, textFadeOutDuration)
        );

        yield return StartCoroutine(
            FadeImage(whiteScreen, 1f, 0f, whiteFadeOutDuration)
        );

        // Reveal the original body on the pressure plate.
        if (deadBodyModel != null)
        {
            deadBodyModel.SetActive(true);

            Debug.Log(
                "[Sacrifice] Dead body revealed: " + deadBodyModel.name,
                this
            );
        }

        // Teleport the player to the clone spawn point.
        if (cloneSpawnPoint != null)
        {
            if (characterController != null)
                characterController.enabled = false;

            transform.SetPositionAndRotation(
                cloneSpawnPoint.position,
                cloneSpawnPoint.rotation
            );

            if (characterController != null)
                characterController.enabled = true;

            Debug.Log(
                "[Sacrifice] Player teleported to clone spawn point.",
                this
            );
        }
        else
        {
            Debug.LogWarning(
                "[Sacrifice] Clone Spawn Point is not assigned.",
                this
            );
        }

        // Disable the assigned jail door or other object.
        if (objectToDisableAfterSacrifice != null)
        {
            objectToDisableAfterSacrifice.SetActive(false);

            Debug.Log(
                "[Sacrifice] Disabled object: " +
                objectToDisableAfterSacrifice.name,
                this
            );
        }

        Debug.Log("[Sacrifice] Sequence completed.", this);

        sequenceRunning = false;
    }

    private IEnumerator FadeImage(
        Image target,
        float startAlpha,
        float endAlpha,
        float duration)
    {
        if (target == null)
        {
            Debug.LogError(
                "[Sacrifice] Black/white screen Image is NOT assigned.",
                this
            );

            yield break;
        }

        if (duration <= 0f)
        {
            SetImageAlpha(target, endAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            SetImageAlpha(
                target,
                Mathf.Lerp(
                    startAlpha,
                    endAlpha,
                    Mathf.Clamp01(elapsed / duration)
                )
            );

            yield return null;
        }

        SetImageAlpha(target, endAlpha);
    }

    private IEnumerator FadeText(
        TMP_Text target,
        float startAlpha,
        float endAlpha,
        float duration)
    {
        if (target == null)
        {
            Debug.LogError(
                "[Sacrifice] Cinematic Text is NOT assigned.",
                this
            );

            yield break;
        }

        if (duration <= 0f)
        {
            SetTextAlpha(target, endAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            SetTextAlpha(
                target,
                Mathf.Lerp(
                    startAlpha,
                    endAlpha,
                    Mathf.Clamp01(elapsed / duration)
                )
            );

            yield return null;
        }

        SetTextAlpha(target, endAlpha);
    }

    private void SetImageAlpha(Image target, float alpha)
    {
        if (target == null)
            return;

        Color color = target.color;
        color.a = alpha;
        target.color = color;
    }

    private void SetTextAlpha(TMP_Text target, float alpha)
    {
        if (target == null)
            return;

        Color color = target.color;
        color.a = alpha;
        target.color = color;
    }
}


using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4.2f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float crouchSpeed = 2.1f;
    [SerializeField] private float groundAcceleration = 22f;
    [SerializeField] private float airAcceleration = 6f;
    [SerializeField] private float jumpHeight = 1.35f;
    [SerializeField] private float gravity = -24f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 85f;
    [SerializeField] private bool invertY = false;

    [Header("Crouching")]
    [SerializeField] private float crouchHeight = 1.15f;
    [SerializeField] private float crouchTransitionSpeed = 12f;

    [Header("Camera Effects")]
    [SerializeField] private float walkBobFrequency = 8f;
    [SerializeField] private float walkBobAmount = 0.035f;
    [SerializeField] private float sprintBobMultiplier = 1.4f;
    [SerializeField] private float cameraLeanAngle = 2f;
    [SerializeField] private float cameraReturnSpeed = 10f;
    [SerializeField] private float walkFOV = 70f;
    [SerializeField] private float sprintFOV = 76f;
    [SerializeField] private float fovSmoothSpeed = 7f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeFrequency = 22f;

    [Header("Footsteps")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landingClip;
    [SerializeField] private AudioClip crouchClip;

    [SerializeField] private float walkStepInterval = 0.48f;
    [SerializeField] private float sprintStepInterval = 0.32f;
    [SerializeField] private float crouchStepInterval = 0.68f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.65f;

    [Header("Optional Sprint Breathing")]
    [SerializeField] private AudioClip sprintBreathingClip;
    [SerializeField, Range(0f, 1f)] private float breathingVolume = 0.25f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    // Movement state
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isCrouched;
    private bool isSprinting;
    private bool cursorLocked;

    // Camera state
    private float pitch;
    private float currentLean;
    private float cameraBobTime;
    private float nextFootstepTime;

    private Vector3 standingCameraPosition;
    private Vector3 currentBobOffset;
    private Quaternion cameraBaseRotation;

    // Crouching state
    private float standingHeight;
    private Vector3 standingCenter;

    // Camera shake state
    private float shakeStrength;
    private float shakeDuration;
    private float shakeTimeRemaining;

    // Audio
    private AudioSource footstepsSource;
    private AudioSource effectsSource;
    private AudioSource breathingSource;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        footstepsSource = SetupAudioSource(
            footstepsSource, "Player Footsteps", false);

        effectsSource = SetupAudioSource(
            effectsSource, "Player Sound Effects", false);

        breathingSource = SetupAudioSource(
            breathingSource, "Player Breathing", true);
    }

    private void Start()
    {
        // Find the camera automatically if it wasn't assigned.
        if (cameraRoot == null)
        {
            playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera != null)
                cameraRoot = playerCamera.transform;
        }

        if (cameraRoot == null)
        {
            Debug.LogError(
                "PlayerController needs a camera or camera pivot.",
                this);

            enabled = false;
            return;
        }

        if (playerCamera == null)
        {
            playerCamera = cameraRoot.GetComponent<Camera>();

            if (playerCamera == null)
                playerCamera = cameraRoot.GetComponentInChildren<Camera>();
        }

        // Save the original standing dimensions.
        standingHeight = controller.height;
        standingCenter = controller.center;

        crouchHeight = Mathf.Min(
            standingHeight,
            Mathf.Max(crouchHeight, controller.radius * 2f + 0.02f));

        standingCameraPosition = cameraRoot.localPosition;
        cameraBaseRotation = cameraRoot.localRotation;

        if (playerCamera != null)
            playerCamera.fieldOfView = walkFOV;

        isGrounded = controller.isGrounded;

        SetCursorLocked(lockCursorOnStart);
    }

    private void Update()
    {
        HandleCursor();
        HandleMouseLook();
        HandleCrouch();
        HandleMovement();
        HandleFootsteps();
        HandleSprintBreathing();
    }

    private void LateUpdate()
    {
        if (cameraRoot == null)
            return;

        float dt = Time.deltaTime;

        UpdateCameraBob(dt);
        UpdateCameraShake(dt);
        UpdateCameraLean(dt);
        UpdateCameraFOV(dt);
    }

    // -------------------------
    // MOUSE LOOK
    // -------------------------

    private void HandleMouseLook()
    {
        if (!cursorLocked)
            return;

        float mouseX =
            Input.GetAxis("Mouse X") * mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate the player horizontally.
        transform.Rotate(Vector3.up * mouseX);

        // Rotate the camera vertically.
        pitch += invertY ? mouseY : -mouseY;

        pitch = Mathf.Clamp(
            pitch, -maxLookAngle, maxLookAngle);
    }

    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }
        else if (!cursorLocked && Input.GetMouseButtonDown(0))
        {
            SetCursorLocked(true);
        }
    }

    private void SetCursorLocked(bool locked)
    {
        cursorLocked = locked;

        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }

    // -------------------------
    // MOVEMENT AND JUMPING
    // -------------------------

    private void HandleMovement()
    {
        float dt = Time.deltaTime;

        // WASD input.
        Vector3 input = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            Input.GetAxisRaw("Vertical"));

        input = Vector3.ClampMagnitude(input, 1f);

        moveInput = new Vector2(input.x, input.z);

        isSprinting =
            Input.GetKey(KeyCode.LeftShift) &&
            moveInput.sqrMagnitude > 0.01f &&
            !isCrouched;

        float targetSpeed;

        if (isCrouched)
            targetSpeed = crouchSpeed;
        else if (isSprinting)
            targetSpeed = sprintSpeed;
        else
            targetSpeed = walkSpeed;

        Vector3 desiredVelocity =
            transform.right * input.x +
            transform.forward * input.z;

        desiredVelocity = desiredVelocity.normalized * targetSpeed;

        bool groundedBeforeMove = controller.isGrounded;

        float acceleration = groundedBeforeMove
            ? groundAcceleration
            : airAcceleration;

        // Smooth acceleration instead of instant movement.
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            desiredVelocity,
            acceleration * dt);

        // Keep the controller attached to the ground.
        if (groundedBeforeMove && verticalVelocity < 0f)
            verticalVelocity = -2f;

        // SPACE to jump.
        if (groundedBeforeMove &&
            Input.GetKeyDown(KeyCode.Space) &&
            !isCrouched)
        {
            verticalVelocity = Mathf.Sqrt(
                jumpHeight * -2f * gravity);

            PlayEffect(jumpClip, 0.85f);

            TriggerCameraShake(0.025f, 0.08f);
        }

        // Apply gravity.
        verticalVelocity += gravity * dt;

        float fallSpeed = Mathf.Abs(verticalVelocity);

        Vector3 finalVelocity =
            horizontalVelocity +
            Vector3.up * verticalVelocity;

        controller.Move(finalVelocity * dt);

        isGrounded = controller.isGrounded;

        // Landing detection.
        if (!groundedBeforeMove && isGrounded)
        {
            if (fallSpeed > 1.5f)
            {
                PlayEffect(landingClip, 0.9f);

                float impact = Mathf.Clamp(
                    fallSpeed * 0.012f, 0.025f, 0.16f);

                TriggerCameraShake(impact, 0.18f);
            }

            verticalVelocity = -2f;
        }
    }

    // -------------------------
    // CROUCHING
    // -------------------------

    private void HandleCrouch()
    {
        bool wasCrouched = isCrouched;

        bool wantsCrouch =
            Input.GetKey(KeyCode.LeftControl);

        // Don't stand up if a ceiling is blocking the player.
        if (!wantsCrouch && isCrouched && !CanStandUp())
            wantsCrouch = true;

        float targetHeight = wantsCrouch
            ? crouchHeight
            : standingHeight;

        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            Time.deltaTime * crouchTransitionSpeed);

        // Keep the player's feet approximately in place.
        float heightDifference =
            standingHeight - controller.height;

        controller.center = standingCenter +
            Vector3.down * (heightDifference * 0.5f);

        isCrouched =
            wantsCrouch ||
            controller.height < standingHeight - 0.05f;

        if (isCrouched != wasCrouched)
        {
            PlayEffect(crouchClip, 0.7f);
        }
    }

    private bool CanStandUp()
    {
        float radius = controller.radius * 0.9f;

        Vector3 worldCenter =
            transform.TransformPoint(standingCenter);

        float halfSegment =
            Mathf.Max(0f, standingHeight * 0.5f - controller.radius);

        Vector3 bottom =
            worldCenter - Vector3.up * halfSegment;

        Vector3 top =
            worldCenter + Vector3.up * halfSegment;

        Collider[] overlaps = Physics.OverlapCapsule(
            bottom,
            top,
            radius,
            ~0,
            QueryTriggerInteraction.Ignore);

        foreach (Collider hit in overlaps)
        {
            // Ignore the player's own colliders.
            if (hit == controller ||
                hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    // -------------------------
    // FOOTSTEPS
    // -------------------------

    private void HandleFootsteps()
    {
        bool moving =
            isGrounded &&
            moveInput.sqrMagnitude > 0.04f;

        if (!moving)
        {
            nextFootstepTime = Time.time + 0.05f;
            return;
        }

        if (Time.time < nextFootstepTime)
            return;

        if (footstepClips == null ||
            footstepClips.Length == 0)
        {
            return;
        }

        AudioClip clip = footstepClips[
            Random.Range(0, footstepClips.Length)];

        float pitchMultiplier = isSprinting
            ? 1.05f
            : isCrouched
                ? 0.9f
                : 1f;

        footstepsSource.pitch =
            pitchMultiplier * Random.Range(0.96f, 1.04f);

        footstepsSource.PlayOneShot(
            clip, footstepVolume);

        float interval = isCrouched
            ? crouchStepInterval
            : isSprinting
                ? sprintStepInterval
                : walkStepInterval;

        nextFootstepTime = Time.time +
            interval * Random.Range(0.92f, 1.08f);
    }

    // -------------------------
    // SPRINT BREATHING
    // -------------------------

    private void HandleSprintBreathing()
    {
        if (sprintBreathingClip == null)
            return;

        if (isSprinting)
        {
            if (breathingSource.clip != sprintBreathingClip)
                breathingSource.clip = sprintBreathingClip;

            breathingSource.volume = breathingVolume;

            if (!breathingSource.isPlaying)
                breathingSource.Play();
        }
        else if (breathingSource.isPlaying)
        {
            breathingSource.Stop();
        }
    }

    private void PlayEffect(AudioClip clip, float volume)
    {
        if (clip != null && effectsSource != null)
        {
            effectsSource.PlayOneShot(clip, volume);
        }
    }

    private AudioSource SetupAudioSource(
        AudioSource source,
        string objectName,
        bool loop)
    {
        if (source == null)
        {
            GameObject audioObject = new GameObject(objectName);

            audioObject.transform.SetParent(transform, false);

            source = audioObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;

        return source;
    }

    // -------------------------
    // CAMERA BOB
    // -------------------------

    private void UpdateCameraBob(float dt)
    {
        Vector3 targetBob = Vector3.zero;

        bool shouldBob =
            isGrounded &&
            moveInput.sqrMagnitude > 0.04f;

        if (shouldBob)
        {
            float frequency = walkBobFrequency;

            if (isSprinting)
                frequency *= sprintBobMultiplier;
            else if (isCrouched)
                frequency *= 0.7f;

            cameraBobTime += dt * frequency;

            float amplitude = walkBobAmount;

            if (isSprinting)
                amplitude *= 1.35f;
            else if (isCrouched)
                amplitude *= 0.45f;

            targetBob = new Vector3(
                Mathf.Cos(cameraBobTime * 0.5f) *
                    amplitude * 0.5f,

                Mathf.Sin(cameraBobTime) * amplitude,

                0f);
        }

        currentBobOffset = Vector3.Lerp(
            currentBobOffset,
            targetBob,
            dt * 10f);

        float crouchOffset =
            (controller.height - standingHeight) * 0.5f;

        Vector3 cameraPosition =
            standingCameraPosition +
            Vector3.up * crouchOffset +
            currentBobOffset;

        cameraRoot.localPosition = cameraPosition;
    }

    // -------------------------
    // CAMERA SHAKE
    // -------------------------

    public void TriggerCameraShake(float strength, float duration)
    {
        if (strength <= 0f || duration <= 0f)
            return;

        shakeStrength = Mathf.Max(shakeStrength, strength);

        if (duration >= shakeTimeRemaining)
        {
            shakeDuration = duration;
            shakeTimeRemaining = duration;
        }
    }

    private void UpdateCameraShake(float dt)
    {
        if (shakeTimeRemaining <= 0f)
            return;

        shakeTimeRemaining -= dt;

        float envelope = shakeDuration > 0f
            ? Mathf.Clamp01(shakeTimeRemaining / shakeDuration)
            : 0f;

        float t = Time.time * shakeFrequency;

        // Perlin noise gives smoother shake than random jitter.
        Vector3 noise = new Vector3(
            Mathf.PerlinNoise(t, 0.17f) - 0.5f,
            Mathf.PerlinNoise(0.37f, t) - 0.5f,
            Mathf.PerlinNoise(t, t) - 0.5f
        ) * 2f;

        Vector3 shakeOffset =
            noise * shakeStrength * envelope;

        cameraRoot.localPosition += shakeOffset;

        if (shakeTimeRemaining <= 0f)
        {
            shakeStrength = 0f;
            shakeDuration = 0f;
        }
    }

    // -------------------------
    // CAMERA LEAN AND FOV
    // -------------------------

    private void UpdateCameraLean(float dt)
    {
        float targetLean = -moveInput.x * cameraLeanAngle;

        currentLean = Mathf.Lerp(
            currentLean,
            targetLean,
            dt * cameraReturnSpeed);

        cameraRoot.localRotation =
            cameraBaseRotation *
            Quaternion.Euler(pitch, 0f, currentLean);
    }

    private void UpdateCameraFOV(float dt)
    {
        if (playerCamera == null)
            return;

        float targetFOV = isSprinting
            ? sprintFOV
            : walkFOV;

        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            dt * fovSmoothSpeed);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (breathingSource != null &&
            breathingSource.isPlaying)
        {
            breathingSource.Stop();
        }
    }
}




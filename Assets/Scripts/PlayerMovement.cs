using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float mouseSensitivity = 3f;
    private float currentSpeed;

    [Header("Jump Settings")]
    public float jumpForce = 6f;
    private bool isGrounded;
    private bool jumpRequested = false;

    [Header("Crouch Settings")]
    public float walkHeight = 2f;
    public float crouchHeight = 1f;
    public float cameraWalkY = 0.6f;
    public float cameraCrouchY = 0.1f;
    public float crouchSmoothTime = 10f;
    private float targetCameraY;

    [Header("Speed Effect (FOV)")]
    public float normalFOV = 60f;
    public float sprintFOV = 75f;
    public float fovSmoothTime = 8f;
    private float targetFOV;

    [Header("Head Bobbing")]
    public bool isBobbingEnabled = true;
    public float bobFrequency = 12f;
    public float bobHorizontalAmount = 0.05f;
    public float bobVerticalAmount = 0.05f;
    private float bobTimer = 0f;
    private float defaultCameraX;

    [Header("Footstep Sounds")]
    public AudioSource audioSource;
    public AudioClip[] footstepSounds;
    public float baseStepInterval = 0.6f;
    private float stepTimer = 0f;

    [Header("References")]
    public Transform anatolyCamera;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Camera actualCamera;

    // Rotation tracking variables
    private float xRotation = 0f;
    private float yRotation = 0f; // NEW: Tracks horizontal look independently from Rigidbody

    // Movement input cache variables
    private float moveX;
    private float moveZ;
    private bool isMoving;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        actualCamera = anatolyCamera.GetComponent<Camera>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        currentSpeed = walkSpeed;
        targetCameraY = cameraWalkY;
        targetFOV = normalFOV;

        defaultCameraX = anatolyCamera.localPosition.x;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (anatolyCamera == null) return;

        // === 1. CAMERA ROTATION (300 FPS Look) ===
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate the camera object directly on both axes, leaving the root Rigidbody untouched
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        anatolyCamera.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

        // === 2. INPUT CACHING ===
        moveX = Input.GetAxis("Horizontal");
        moveZ = Input.GetAxis("Vertical");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpRequested = true;
        }

        // === 3. MOVEMENT STATES LOGIC ===
        if (Input.GetKey(KeyCode.LeftControl))
        {
            capsuleCollider.height = crouchHeight;
            capsuleCollider.center = new Vector3(0, -0.5f, 0);
            currentSpeed = crouchSpeed;
            targetCameraY = cameraCrouchY;
            targetFOV = normalFOV;
        }
        else if (Input.GetKey(KeyCode.LeftShift) && moveZ > 0)
        {
            capsuleCollider.height = walkHeight;
            capsuleCollider.center = Vector3.zero;
            currentSpeed = sprintSpeed;
            targetCameraY = cameraWalkY;
            targetFOV = sprintFOV;
        }
        else
        {
            capsuleCollider.height = walkHeight;
            capsuleCollider.center = Vector3.zero;
            currentSpeed = walkSpeed;
            targetCameraY = cameraWalkY;
            targetFOV = normalFOV;
        }

        // === 4. SMOOTH INTERPOLATION & HEAD BOBBING ===
        float currentCameraY = Mathf.Lerp(anatolyCamera.localPosition.y, targetCameraY, Time.deltaTime * crouchSmoothTime);

        if (isBobbingEnabled && isMoving)
        {
            float speedMultiplier = currentSpeed / walkSpeed;
            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;

            float newCameraX = defaultCameraX + Mathf.Cos(bobTimer / 2) * bobHorizontalAmount;
            float newCameraY = currentCameraY + Mathf.Sin(bobTimer) * bobVerticalAmount;

            // Apply position while preserving the Update rotation logic
            anatolyCamera.localPosition = new Vector3(newCameraX, newCameraY, anatolyCamera.localPosition.z);
        }
        else
        {
            bobTimer = 0f;
            float resetX = Mathf.Lerp(anatolyCamera.localPosition.x, defaultCameraX, Time.deltaTime * crouchSmoothTime);
            anatolyCamera.localPosition = new Vector3(resetX, currentCameraY, anatolyCamera.localPosition.z);
        }

        if (actualCamera != null)
        {
            actualCamera.fieldOfView = Mathf.Lerp(actualCamera.fieldOfView, targetFOV, Time.deltaTime * fovSmoothTime);
        }

        // === 5. FOOTSTEP AUDIO LOGIC ===
        if (isMoving)
        {
            float speedMultiplier = currentSpeed / walkSpeed;
            stepTimer += Time.deltaTime * speedMultiplier;

            if (stepTimer >= baseStepInterval)
            {
                PlayFootstepSound();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = baseStepInterval;
        }
    }

    // === 6. PHYSICS APPLICATOR (FixedUpdate) ===
    void FixedUpdate()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        isMoving = (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f) && isGrounded;

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            jumpRequested = false;
        }

        // NEW: Calculate direction based on camera vectors projected onto the ground plane
        Vector3 camForward = anatolyCamera.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = anatolyCamera.right;
        camRight.y = 0f;
        camRight.Normalize();

        // Moving relative to where Anatoly is looking
        Vector3 moveDirection = (camRight * moveX) + (camForward * moveZ);
        Vector3 velocity = moveDirection * currentSpeed;

        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    void PlayFootstepSound()
    {
        if (audioSource == null || footstepSounds.Length == 0) return;

        int randomIndex = Random.Range(0, footstepSounds.Length);
        audioSource.PlayOneShot(footstepSounds[randomIndex]);
    }
}
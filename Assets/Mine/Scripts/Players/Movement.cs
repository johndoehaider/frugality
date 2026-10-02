using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    #region Inspector

    [Header("Audio Clips")]
    [SerializeField] private AudioClip audioClipWalking;
    [SerializeField] private AudioClip audioClipRunning;

    [Header("Speed")]
    [SerializeField] private float speedWalking = 3f;
    [SerializeField] private float speedRunning = 5f;

    [Header("Stamina")]
    [SerializeField] private float stamina = 100f;
    [SerializeField] private float staminaDecayTime = 10f;
    [SerializeField] private float staminaRegenTime = 10f;
    [SerializeField] private float staminaExhaustionCooldown = 1f;

    [Header("Jumping")]
    [SerializeField] private float jumpStrength = 5f;
    [SerializeField] private float upwardGravityMultiplier = 1.5f;
    [SerializeField] private float fallingGravityMultiplier = 2.5f;
    private float groundedVerticalVelocity = -2f;

    [Header("Air Movement")]
    [Min(0f)]
    [SerializeField] private float airControlAcceleration = 20f;
    [Range(0f, 1.5f)]
    [SerializeField] private float jumpMomentumMultiplier = 1f; 
    [Min(0f)]
    [SerializeField] private float minimumAirControlSpeed = 1f;

    [Header("Jump Recovery")]
    [Range(0f, 1f)]
    [SerializeField] private float landingSpeedMultiplier = 0.75f;
    [Min(0f)]
    [SerializeField] private float landingSlowdownDuration = 0.3f;
    [Min(0f)]
    [SerializeField] private float reJumpDelay = 0.1f;

    [Header("References")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private Animator characterAnimator;

    #endregion

    #region Runtime State

    private CharacterController characterController;
    private Character playerCharacter;

    private bool grounded;
    private bool wasGrounded;
    private bool jumped;
    private bool landing;

    private bool staminaExhausted;
    private float maxStamina;
    private float staminaExhaustedUntil;

    private float verticalVelocity;
    private float airborneSpeedLimit;
    private float landingRecoveryTimeRemaining;
    private float nextJumpAllowedTime;

    private Vector3 groundedHorizontalVelocity;
    private Vector3 airborneHorizontalVelocity;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerCharacter = GetComponent<Character>();

        maxStamina = stamina;
    }

    private void Start()
    {
        footstepAudioSource.clip = audioClipWalking;
        footstepAudioSource.loop = true;

        grounded = characterController.isGrounded;
        wasGrounded = grounded;
    }

    private void Update()
    {
        UpdateGroundedStateBeforeMovement();
        UpdateLandingRecovery();
        UpdateVerticalVelocity();
        MoveCharacter();
        UpdateGroundedStateAfterMovement();
        UpdateLandingState();
        PlayFootstepSounds();
        UpdateStamina();
    }

    #endregion

    #region Movement

    private void MoveCharacter()
    {
        Vector2 input = playerCharacter.GetInputMovement();
        Vector3 localDirection = new Vector3(input.x, 0f, input.y);

        if (localDirection.sqrMagnitude > 1f)
        {
            localDirection.Normalize();
        }

        Vector3 worldDirection = transform.TransformDirection(localDirection);
        Vector3 horizontalVelocity;

        if (grounded)
        {
            float movementSpeed = GetGroundMovementSpeed() * GetLandingRecoverySpeedMultiplier();

            horizontalVelocity = worldDirection * movementSpeed;
            groundedHorizontalVelocity = horizontalVelocity;
            airborneHorizontalVelocity = horizontalVelocity;
            airborneSpeedLimit = horizontalVelocity.magnitude;
        }
        else
        {
            UpdateAirborneHorizontalVelocity(worldDirection);
            horizontalVelocity = airborneHorizontalVelocity;
        }

        Vector3 motion = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        CollisionFlags collisionFlags = characterController.Move(motion * Time.deltaTime);

        if ((collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }
    }

    private void UpdateStamina()
    {
        if (playerCharacter.IsRunning())
        {
            stamina -= Time.deltaTime * staminaDecayTime;

            if (stamina <= 0f)
            {
                stamina = 0f;
                staminaExhausted = true;
                staminaExhaustedUntil = Time.time + staminaExhaustionCooldown;
            }

            return;
        }

        if (stamina < maxStamina)
        {
            stamina += Time.deltaTime * staminaRegenTime;
            stamina = Mathf.Min(stamina, maxStamina);
        }

        if (staminaExhausted && Time.time >= staminaExhaustedUntil && stamina > 0f)
        {
            staminaExhausted = false;
        }
    }

    private float GetGroundMovementSpeed()
    {
        return playerCharacter.IsRunning() ? speedRunning : speedWalking;
    }

    #endregion

    #region Air Movement

    private void UpdateAirborneHorizontalVelocity(Vector3 desiredWorldDirection)
    {
        if (desiredWorldDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 targetAirVelocity = desiredWorldDirection.normalized * airborneSpeedLimit;
        airborneHorizontalVelocity = Vector3.MoveTowards(airborneHorizontalVelocity, targetAirVelocity, airControlAcceleration * Time.deltaTime);
    }

    private void CaptureTakeoffMomentum()
    {
        airborneHorizontalVelocity = groundedHorizontalVelocity * jumpMomentumMultiplier;
        airborneSpeedLimit = Mathf.Max(airborneHorizontalVelocity.magnitude, minimumAirControlSpeed);
    }

    #endregion

    #region Grounding And Gravity

    private void UpdateGroundedStateBeforeMovement()
    {
        wasGrounded = grounded;

        if (verticalVelocity > 0f)
        {
            grounded = false;
            return;
        }

        grounded = characterController.isGrounded;
    }

    private void UpdateVerticalVelocity()
    {
        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedVerticalVelocity;
            return;
        }

        float gravityMultiplier = verticalVelocity > 0f ? upwardGravityMultiplier : fallingGravityMultiplier;
        verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
    }

    private void UpdateGroundedStateAfterMovement()
    {
        grounded = characterController.isGrounded;
    }

    private void UpdateLandingState()
    {
        if (!jumped || wasGrounded || !grounded)
        {
            return;
        }

        landing = true;

        characterAnimator.SetTrigger("Land");
        jumped = false;
        landingRecoveryTimeRemaining = landingSlowdownDuration;
        nextJumpAllowedTime = Time.time + reJumpDelay;
    }

    public void AnimationEndedLanding()
    {
        landing = false;
    }

    public bool IsLanding()
    {
        return landing;
    }

    #endregion

    #region Jumping

    public void Jump()
    {
        if (!grounded || Time.time < nextJumpAllowedTime)
        {
            return;
        }

        CaptureTakeoffMomentum();

        verticalVelocity = jumpStrength;
        grounded = false;
        jumped = true;

        characterAnimator.SetTrigger("Jump");
    }

    #endregion

    #region Jump Recovery

    private void UpdateLandingRecovery()
    {
        if (landingRecoveryTimeRemaining <= 0f)
        {
            return;
        }

        landingRecoveryTimeRemaining = Mathf.Max(0f, landingRecoveryTimeRemaining - Time.deltaTime);
    }

    private float GetLandingRecoverySpeedMultiplier()
    {
        if (landingSlowdownDuration <= 0f || landingRecoveryTimeRemaining <= 0f)
        {
            return 1f;
        }

        float recoveryProgress = 1f - landingRecoveryTimeRemaining / landingSlowdownDuration;
        return Mathf.Lerp(landingSpeedMultiplier, 1f, recoveryProgress);
    }

    #endregion

    #region Audio

    private void PlayFootstepSounds()
    {
        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;

        if (grounded && horizontalVelocity.sqrMagnitude > 0.1f)
        {
            AudioClip desiredClip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;

            if (footstepAudioSource.clip != desiredClip)
            {
                footstepAudioSource.clip = desiredClip;
                footstepAudioSource.Play();
            }
            else if (!footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Play();
            }
        }
        else if (footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Pause();
        }
    }

    #endregion

    #region Perk Modifiers

    public void ApplyStaminUp(float walkSpeed, float runSpeed, float stam)
    {
        speedWalking = walkSpeed;
        speedRunning = runSpeed;
        maxStamina = stam;

        stamina = maxStamina;
        staminaExhausted = false;
    }

    #endregion

    #region Getters

    public bool IsGrounded()
    {
        return grounded;
    }

    public bool HasStamina()
    {
        return !staminaExhausted && stamina > 0f;
    }

    #endregion
}

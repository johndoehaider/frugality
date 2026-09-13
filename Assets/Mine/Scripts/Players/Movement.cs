using UnityEngine;

// RequireComponent makes sure this GameObject always has the physics components Movement needs.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class Movement : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip audioClipWalking;
    [SerializeField] private AudioClip audioClipRunning;

    [Header("Speeds")]
    [SerializeField] private float speedWalking = 3f;
    [SerializeField] private float speedRunning = 5f;
    [SerializeField] private float stamina = 100f;

    [Header("Jumping")]
    [SerializeField] private float jumpStrength = 5f;
    [SerializeField] private float upwardGravityMultiplier = 1.5f;
    [SerializeField] private float fallingGravityMultiplier = 2.5f;

    [Header("References")]
    [SerializeField] private AudioSource footstepAudioSource;

    private Rigidbody rigidBody;
    private CapsuleCollider capsule;
    private Character playerCharacter;

    private bool grounded;

    // Reuses one array for ground checks so Unity does not create a new one every physics frame.
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    // Gets the components Movement needs and sets up the footstep AudioSource.
    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        playerCharacter = GetComponent<Character>();

        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;

        footstepAudioSource.clip = audioClipWalking;
        footstepAudioSource.loop = true;
    }

    // While the player is touching something, get the size of the capsule, sphere-cast downward under the player, 
    // check every collider it hits, ignore empty results and the player's own collider, 
    // and if anything valid is underneath us, set grounded = true
    private void OnCollisionStay()
    {
        float radius = capsule.bounds.extents.x - 0.01f;

        int hitCount = Physics.SphereCastNonAlloc(capsule.bounds.center, radius, Vector3.down, groundHits, 
        capsule.bounds.extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);

        grounded = false;

        for (int i = 0; i < hitCount; i++)
        {
            if (groundHits[i].collider != null && groundHits[i].collider != capsule)
            {
                if (rigidBody.linearVelocity.y <= 0.1f)
                {
                    grounded = true;
                    break;
                }
            }
        }
    }

    // Moves the player using Rigidbody physics.
    private void FixedUpdate()
    {
        MoveCharacter();
        ApplyPlayerGravity();
    }

    // Handles footsteps every normal frame.
    private void Update()
    {
        PlayFootstepSounds();
    }

    // Converts input into world-space movement and applies walking or running speed.
    private void MoveCharacter()
    {
        Vector2 input = playerCharacter.GetInputMovement();
        Vector3 movement = new Vector3(input.x, 0f, input.y);

        if (playerCharacter.IsRunning())
        {
            movement *= speedRunning;
        }
        else
        {
            movement *= speedWalking;
        }

        movement = transform.TransformDirection(movement);

        Vector3 currentVelocity = rigidBody.linearVelocity;
        rigidBody.linearVelocity = new Vector3(movement.x, currentVelocity.y, movement.z);
    }

    public void ApplyStaminUp(float walkSpeed,float runSpeed, float stam)
    {
        speedWalking = walkSpeed;
        speedRunning = runSpeed;
        stamina = stam;
    }

    // Plays walking or running footsteps while the player is moving on the ground.
    private void PlayFootstepSounds()
    {
        Vector3 horizontalVelocity = new Vector3(rigidBody.linearVelocity.x, 0f, rigidBody.linearVelocity.z);

        if (grounded && horizontalVelocity.sqrMagnitude > 0.1f)
        {
            AudioClip desiredClip;

            if (playerCharacter.IsRunning())
            {
                desiredClip = audioClipRunning;
            }
            else
            {
                desiredClip = audioClipWalking;
            }

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
        else
        {
            if (footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Pause();
            }
        }
    }

    public void Jump()
    {
        if (grounded)
        {
            rigidBody.AddForce(Vector3.up * jumpStrength, ForceMode.VelocityChange);
            grounded = false;
        }
    }

    private void ApplyPlayerGravity()
    {
        if (rigidBody.linearVelocity.y > 0f)
        {
            rigidBody.AddForce(Physics.gravity * (upwardGravityMultiplier - 1f), ForceMode.Acceleration);
        }
        else if (rigidBody.linearVelocity.y < 0f)
        {
            rigidBody.AddForce(Physics.gravity * (fallingGravityMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    public bool IsGrounded()
    {
        return grounded;
    }
}

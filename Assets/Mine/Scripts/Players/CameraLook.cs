using UnityEngine;

public class CameraLook : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector2 sensitivity = new Vector2(1f, 1f);
    [SerializeField] private Vector2 yClamp = new Vector2(-60f, 60f);
    [SerializeField] private bool smooth;
    [SerializeField] private float interpolationSpeed = 25f;

    private Character playerCharacter;
    private Rigidbody playerRigidbody;

    private Quaternion targetCharacterRotation;
    private Quaternion targetCameraRotation;

    // Gets the Character and Rigidbody that this camera belongs to.
    private void Awake()
    {
        playerCharacter = GetComponentInParent<Character>();

        if (playerCharacter != null)
        {
            playerRigidbody = playerCharacter.GetComponent<Rigidbody>();
        }
    }

    // Saves the starting player and camera rotations.
    private void Start()
    {
        if (playerCharacter != null)
        {
            targetCharacterRotation = playerCharacter.transform.localRotation;
            targetCameraRotation = transform.localRotation;
        }
    }

    // Reads look input and rotates the player left/right and the camera up/down.
    private void LateUpdate()
    {
        if (playerCharacter != null && playerRigidbody != null)
        {
            Vector2 input;

            if (playerCharacter.IsCursorLocked())
            {
                input = playerCharacter.GetInputLook();
            }
            else
            {
                input = Vector2.zero;
            }

            input *= sensitivity;

            Quaternion horizontalRotation = Quaternion.Euler(0f, input.x, 0f);
            Quaternion verticalRotation = Quaternion.Euler(-input.y, 0f, 0f);

            targetCharacterRotation *= horizontalRotation;
            targetCameraRotation *= verticalRotation;
            targetCameraRotation = ClampPitch(targetCameraRotation);

            if (smooth)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetCameraRotation, Time.deltaTime * interpolationSpeed);

                Quaternion smoothCharacterRotation = Quaternion.Slerp(playerRigidbody.rotation, targetCharacterRotation, Time.deltaTime * interpolationSpeed);
                playerRigidbody.MoveRotation(smoothCharacterRotation);
            }
            else
            {
                transform.localRotation = targetCameraRotation;
                playerRigidbody.MoveRotation(targetCharacterRotation);
            }
        }
    }

    // Limits how far the player can look up and down.
    private Quaternion ClampPitch(Quaternion rotation)
    {
        rotation.x /= rotation.w;
        rotation.y /= rotation.w;
        rotation.z /= rotation.w;
        rotation.w = 1f;

        float pitch = 2f * Mathf.Rad2Deg * Mathf.Atan(rotation.x);
        pitch = Mathf.Clamp(pitch, yClamp.x, yClamp.y);

        rotation.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * pitch);

        return rotation;
    }
}

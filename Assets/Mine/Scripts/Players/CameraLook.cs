using UnityEngine;

public class CameraLook : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector2 sensitivity = new Vector2(1f, 1f);
    [SerializeField] private Vector2 yClamp = new Vector2(-89f, 89f);
    [SerializeField] private bool smooth;
    [SerializeField] private float interpolationSpeed = 35f;

    private Character playerCharacter;
    private Rigidbody playerRigidbody;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        playerCharacter = GetComponentInParent<Character>();

        if (playerCharacter != null)
        {
            playerRigidbody = playerCharacter.GetComponent<Rigidbody>();
        }
    }

    private void Start()
    {
        if (playerCharacter != null)
        {
            yaw = playerCharacter.transform.eulerAngles.y;

            float startingPitch = transform.localEulerAngles.x;

            if (startingPitch > 180f)
            {
                startingPitch -= 360f;
            }

            pitch = startingPitch;
        }
    }

    private void LateUpdate()
    {
        if (playerCharacter == null || playerRigidbody == null)
        {
            return;
        }

        Vector2 input = Vector2.zero;

        if (playerCharacter.IsCursorLocked())
        {
            input = playerCharacter.GetInputLook();
        }

        input *= sensitivity;

        yaw += input.x;
        pitch -= input.y;
        pitch = Mathf.Clamp(pitch, yClamp.x, yClamp.y);

        Quaternion targetCharacterRotation = Quaternion.Euler(0f, yaw, 0f);
        Quaternion targetCameraRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (smooth)
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetCameraRotation,
                Time.deltaTime * interpolationSpeed
            );

            Quaternion smoothCharacterRotation = Quaternion.Slerp(
                playerRigidbody.rotation,
                targetCharacterRotation,
                Time.deltaTime * interpolationSpeed
            );

            playerRigidbody.MoveRotation(smoothCharacterRotation);
        }
        else
        {
            transform.localRotation = targetCameraRotation;
            playerRigidbody.MoveRotation(targetCharacterRotation);
        }
    }
}
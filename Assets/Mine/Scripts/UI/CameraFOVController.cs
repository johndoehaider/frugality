using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFOVController : MonoBehaviour
{
    #region Inspector

    [Header("World FOV")]
    [Range(60f, 120f)]
    [SerializeField] private float worldFOV = 105f;

    [Header("ADS")]
    [Min(1f)]
    [SerializeField] private float adsMagnification = 1.2f;

    [Min(0f)]
    [SerializeField] private float adsTransitionSpeed = 100f;

    #endregion

    #region Cached References

    private Camera worldCamera;
    private Character playerCharacter;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        worldCamera = GetComponent<Camera>();
        playerCharacter = GetComponentInParent<Character>();
    }

    private void Start()
    {
        SetHorizontalFOV(worldFOV);
    }

    private void LateUpdate()
    {
        UpdateFOV();
    }

    #endregion

    #region FOV

    private void UpdateFOV()
    {
        float targetHorizontalFOV = worldFOV;

        if (playerCharacter.IsAiming())
        {
            targetHorizontalFOV = worldFOV / adsMagnification;
        }

        float currentHorizontalFOV = Camera.VerticalToHorizontalFieldOfView(worldCamera.fieldOfView, worldCamera.aspect);
        float newHorizontalFOV = Mathf.MoveTowards(currentHorizontalFOV, targetHorizontalFOV, adsTransitionSpeed * Time.deltaTime);

        SetHorizontalFOV(newHorizontalFOV);
    }

    private void SetHorizontalFOV(float horizontalFOV)
    {
        worldCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(horizontalFOV, worldCamera.aspect);
    }

    #endregion
}
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRadius = 0.25f;
    [SerializeField] private float interactionDistance = 2f;

    private Character playerCharacter;
    private Camera playerCamera;
    private IInteractable proximityInteractable;
    private bool inProximity;
    private bool holdingInteract = false;

    // Gets the player's Character component and uses Character's world camera for interaction checks.
    private void Awake()
    {
        playerCharacter = GetComponent<Character>();

        if (playerCharacter != null)
            playerCamera = playerCharacter.GetCameraWorld();
    }

    // Called by Unity's Player Input component when the Interact action is performed.
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started) 
        {
            holdingInteract = true;
        }

        if (context.canceled) 
        {
            holdingInteract = false;
        }
        if (context.performed)
        {
            TryInteract();
        }
    }

    // SphereCasts forward from the player's camera and interacts with the first IInteractable hit.
    public void TryInteract()
    {
        if (playerCamera != null)
        {
            RaycastHit hit;
            if (!inProximity)
            {
                if (Physics.SphereCast(playerCamera.transform.position, interactionRadius,
                    playerCamera.transform.forward, out hit, interactionDistance))
                {
                    IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

                    if (interactable != null)
                    {
                        interactable.Interact(this);
                    }
                }
            }

            else
            {
                if (proximityInteractable != null && proximityInteractable.UsesContinuousInteract())
                {
                    StartCoroutine(ContinuousInteract());
                }

                else if (proximityInteractable != null)
                {
                    proximityInteractable.Interact(this);

                }
            }
        }
    }

    private IEnumerator ContinuousInteract()
    {
        while (holdingInteract && proximityInteractable != null)
        {
            proximityInteractable.Interact(this);
            yield return null;
        }
    }

    public void InProximity(bool boolean)
    {
        inProximity = boolean;
    }

    public void InProximityInteractable(IInteractable interactable)
    {
        proximityInteractable = interactable;
    }

    public bool GetProximity()
    {
        return inProximity;
    }

    public IInteractable GetProximityInteractable()
    {
        return proximityInteractable;
    }
}

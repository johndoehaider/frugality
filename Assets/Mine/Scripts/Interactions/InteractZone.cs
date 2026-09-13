using UnityEngine;

public class InteractZone : MonoBehaviour
{
    private PlayerInteraction playerInteraction;
    private IInteractable interactable;

    void Awake()
    {
        interactable = GetComponentInParent<IInteractable>();
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerInteraction interaction = other.GetComponentInParent<PlayerInteraction>();

        if (interaction != null)
        {
            playerInteraction = interaction;
            interaction.InProximity(true);
            interaction.InProximityInteractable(interactable);
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerInteraction interaction = other.GetComponentInParent<PlayerInteraction>();

        if (interaction != null && interaction == playerInteraction)
        {
            playerInteraction = null;
            interaction.InProximity(false);
            interaction.InProximityInteractable(null);
        }
    }
}

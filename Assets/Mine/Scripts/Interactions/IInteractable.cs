public interface IInteractable
{
    void Interact(PlayerInteraction playerInteraction);
    string GetInteractionText(PlayerInteraction playerInteraction);
    bool UsesContinuousInteract();

}
using UnityEngine;

public class BuyableDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private int price;

    void Start()
    {
        
    }

    public void Interact(PlayerInteraction playerInteraction)
    {
        PlayerPoints playerPoints = playerInteraction.GetComponent<PlayerPoints>();

        if (playerPoints.points >= price)
        {
            playerPoints.RemovePoints(price);
            OpenDoor();
        }
    }

    public string GetInteractionText(PlayerInteraction playerInteraction)
    {
        return "Hold E to open Door [Cost: " + price + "]";
    }

    void OpenDoor()
    {
        Destroy(gameObject);
    }

    public bool UsesContinuousInteract()
    {
        return false;
    }
}

using UnityEngine;

public class BuyableDoor : MonoBehaviour, IInteractable
{
    private PlayerPoints playerPoints;
    public int price;

    void Start()
    {
        playerPoints = FindFirstObjectByType<PlayerPoints>();
    }

    public void Interact()
    {
        if (playerPoints.points >= price)
        {
            playerPoints.RemovePoints(price);
            OpenDoor();
        }
    }

    public string GetInteractionText()
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

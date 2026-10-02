using UnityEngine;

public class BuyableDoor : MonoBehaviour, IInteractable
{

    #region Inspector
    [Header("Value")]
    [Min(0)]
    [SerializeField] private int price;

    [Header("Zone Connections")]
    [SerializeField] private ZombieZone zoneA;
    [SerializeField] private ZombieZone zoneB;

    private bool isOpen;

    #endregion
    #region Interaction

    public void Interact(PlayerInteraction playerInteraction)
    {
        if (isOpen)
        {
            return;
        }
        
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

    public bool UsesContinuousInteract()
    {
        return false;
    }

    #endregion
    #region Door State

    private void OpenDoor()
    {
        isOpen = true;
        zoneA.ConnectTo(zoneB);
        gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        if (zoneA != null && zoneA == zoneB)
        {
            Debug.LogError($"BuyableDoor '{name}' cannot connect a ZombieZone to itself.", this);
        }
    }
}

#endregion

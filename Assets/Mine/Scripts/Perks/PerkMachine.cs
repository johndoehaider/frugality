using UnityEngine;

public class PerkMachine : MonoBehaviour, IInteractable
{
    [SerializeField] private PerkData perk;
    [SerializeField] private PowerManager powerManager;

    private PlayerPoints playerPoints;
    private PlayerPerks playerPerks;

    private void Start()
    {
        playerPoints = FindFirstObjectByType<PlayerPoints>();
        playerPerks = FindFirstObjectByType<PlayerPerks>();
    }

    public void Interact()
    {
        if (!powerManager.IsPowerOn())
        {
            return;
        }

        if (playerPerks.HasPerk(perk.perkType))
        {
            return;
        }

        if (playerPoints.GetPoints() >= perk.cost)
        {
            playerPoints.RemovePoints(perk.cost);
            playerPerks.AddPerk(perk);
        }
    }

    public string GetInteractionText()
    {
        if (!powerManager.IsPowerOn())
        {
            return "You must turn on the power first!";
        }

        if (playerPerks.HasPerk(perk.perkType))
        {
            return "";
        }

        return "Hold E for " + perk.perkName + " [Cost:  " + perk.cost + "]";
    }

    public bool UsesContinuousInteract()
    {
        return false;
    }
}
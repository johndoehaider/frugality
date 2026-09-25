using UnityEngine;

public class WallWeapon : MonoBehaviour, IInteractable
{

    [SerializeField] private int weaponPrice;
    [SerializeField] private int ammoPrice;
    [SerializeField] private string weaponName;
    [SerializeField] private Weapon weaponPrefab;

    public void Interact(PlayerInteraction playerInteraction)
    {
        Character currentPlayer = playerInteraction.GetComponent<Character>();
        PlayerPoints playerPoints = playerInteraction.GetComponent<PlayerPoints>();
        Inventory inventory = playerInteraction.GetComponentInChildren<Inventory>();

        if (currentPlayer == null || playerPoints == null || inventory == null)
        {
            return;
        }

        Weapon ownedWeapon = inventory.CheckWeaponOwnership(weaponName);
        if (ownedWeapon != null)
        {
            if (playerPoints.HasEnoughPoints(ammoPrice) && ownedWeapon.GetAmmunitionReserved() < ownedWeapon.GetAmmunitionReservedMax())
            {
                playerPoints.RemovePoints(ammoPrice);
                ownedWeapon.MaxAmmo();
            }
        }
        else
        {
            if (playerPoints.HasEnoughPoints(weaponPrice))
            {
                if (currentPlayer.AcquireWeapon(weaponPrefab))
                {
                    playerPoints.RemovePoints(weaponPrice);
                }
            }
        }
    }

    public string GetInteractionText(PlayerInteraction playerInteraction)
    {
        Inventory inventory = playerInteraction.GetComponentInChildren<Inventory>();

        if (inventory == null)
        {
            return "";
        }

        Weapon ownedWeapon = inventory.CheckWeaponOwnership(weaponName);
        if (ownedWeapon != null)
        {
            if (ownedWeapon.GetAmmunitionReserved() >= ownedWeapon.GetAmmunitionReservedMax())
            {
                return weaponName + " Ammo [FULL]";
            }

            return "Hold E to buy " + weaponName + " Ammo [Cost: " + ammoPrice + "]";
        }

        return "Hold E to buy " + weaponName + " [Cost: " + weaponPrice + "]";
    }

    public bool UsesContinuousInteract()
    {
        return false;
    }

}

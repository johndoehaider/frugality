using UnityEngine;
using System.Collections.Generic;

public class PlayerPerks : MonoBehaviour
{
    private List<PerkType> ownedPerks = new List<PerkType>();
    private HUDManager hudManager;

    private PlayerHealth playerHealth;
    private Inventory playerInventory;
    private Character playerCharacter;
    private Weapon equippedWeapon;
    private Movement playerMovement;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerCharacter = GetComponent<Character>();
        playerInventory = GetComponent<Inventory>();
        equippedWeapon = playerCharacter.GetEquippedWeapon();
        playerMovement = GetComponent<Movement>();

        hudManager = FindFirstObjectByType<HUDManager>();
    }

    public void AddPerk(PerkData perk)
    {
        if (HasPerk(perk.perkType))
        {
            return;
        }

        ownedPerks.Add(perk.perkType);
        ApplyPerkEffect(perk.perkType);
        hudManager.UpdatePerkIcon(perk.icon);
    }

    public bool HasPerk(PerkType perkType)
    {
        return ownedPerks.Contains(perkType);
    }

    private void ApplyPerkEffect(PerkType perkType)
    {
        if (perkType == PerkType.JuggerNog)
        {
            playerHealth.ApplyJuggernog(250f);
        }

        if (perkType == PerkType.StaminUp)
        {
            playerMovement.ApplyStaminUp(4.5f, 7.0f, 100000f);
        }

        if (perkType == PerkType.QuickRevive)
        {
        }

        if (perkType == PerkType.DoubleTap)
        {
            playerCharacter.ApplyDoubleTap(2.0f, 1.3f);
        }

        if (perkType == PerkType.SpeedCola)
        {
            playerCharacter.ApplySpeedCola(2f);
        }
    }

    public void RemoveAllPerks()
    {
        if (HasPerk(PerkType.SpeedCola))
        {
            playerCharacter.ApplySpeedCola(1f);
        }

        if (HasPerk(PerkType.JuggerNog))
        {
            playerHealth.ApplyJuggernog(100f); 
        }

        if (HasPerk(PerkType.StaminUp))
        {
            playerMovement.ApplyStaminUp(3f,5f, 100f);
        }

        if (HasPerk(PerkType.DoubleTap))
        {
            playerCharacter.ApplyDoubleTap(1f, 1f);
        }

        ownedPerks.Clear();
    }
}
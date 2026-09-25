using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    private List<Weapon> currentWeapons = new List<Weapon>();
    private Weapon equippedWeapon;

    private int equippedIndex = -1;
    private int weaponCapacity = 2;

    // Finds every Weapon under this Inventory, disables them, and equips the starting weapon.
    public void Init(int equippedAtStart = 0)
    {
        Weapon[] startingWeapons = GetComponentsInChildren<Weapon>(true);

        foreach (Weapon weapon in startingWeapons)
        {
            currentWeapons.Add(weapon);
            weapon.gameObject.SetActive(false);        
        }

        Equip(equippedAtStart);
    }

    // Disables the currently equipped weapon and enables the requested weapon.
    public Weapon Equip(int newWeaponIndex)
    {
        if (currentWeapons != null && currentWeapons.Count > 0)
        {
            if (newWeaponIndex >= 0 && newWeaponIndex < currentWeapons.Count)
            {
                if (equippedWeapon != currentWeapons[newWeaponIndex])
                {
                    if (equippedWeapon != null)
                    {
                        equippedWeapon.gameObject.SetActive(false);
                    }

                    equippedIndex = newWeaponIndex;
                    equippedWeapon = currentWeapons[equippedIndex];
                    equippedWeapon.gameObject.SetActive(true);
                }
            }
        }

        return equippedWeapon;
    }

    // Add this weapon if I have an available weapon slot and tell me its index.
    public int AcquireWeapon(Weapon weaponPrefab)
    {
        if (currentWeapons.Count >= weaponCapacity)
        {
            return -1;
        }

        Weapon newWeapon = Instantiate(weaponPrefab, transform);

        currentWeapons.Add(newWeapon);

        return currentWeapons.Count - 1;
    }

    // Replace whatever currently occupies my equipped slot.
    public int ReplaceEquippedWeapon(Weapon weaponPrefab)
    {
        Destroy(equippedWeapon.gameObject);
        equippedWeapon = null;

        Weapon newWeapon = Instantiate(weaponPrefab, transform);

        currentWeapons[equippedIndex] = newWeapon;

        return equippedIndex;
    }

    public Weapon CheckWeaponOwnership(string weaponName)
    {
        foreach (Weapon weapon in currentWeapons)
        {
            if (weapon.GetWeaponName() == weaponName)
            {
                return weapon;
            }
        }
        
        return null;
    }

    
    public void MaxAmmo()
    {
        foreach (Weapon weapon in currentWeapons)
        {
            weapon.MaxAmmo();
        }

    }

    // Returns the previous weapon index and wraps back to the final weapon when needed.
    public int GetLastIndex()
    {
        int newIndex = equippedIndex - 1;

        if (newIndex < 0)
        {
            newIndex = currentWeapons.Count - 1;
        }

        return newIndex;
    }

    // Returns the next weapon index and wraps back to the first weapon when needed.
    public int GetNextIndex()
    {
        int newIndex = equippedIndex + 1;

        if (newIndex >= currentWeapons.Count)
        {
            newIndex = 0;
        }

        return newIndex;
    }

    // Returns the currently equipped Weapon.
    public Weapon GetEquipped()
    {
        return equippedWeapon;
    }

    // Returns the array index of the currently equipped Weapon.
    public int GetEquippedIndex()
    {
        return equippedIndex;
    }

    public bool HasWeaponSpace()
    {
        return currentWeapons.Count < weaponCapacity;
    }
}

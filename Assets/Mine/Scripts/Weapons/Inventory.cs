using UnityEngine;

public class Inventory : MonoBehaviour
{
    private Weapon[] weapons;
    private Weapon equippedWeapon;
    private int equippedIndex = -1;

    // Finds every Weapon under this Inventory, disables them, and equips the starting weapon.
    public void Init(int equippedAtStart = 1)
    {
        weapons = GetComponentsInChildren<Weapon>(true);

        foreach (Weapon weapon in weapons)
        {
            weapon.gameObject.SetActive(false);
        }

        Equip(equippedAtStart);
    }

    // Disables the currently equipped weapon and enables the requested weapon.
    public Weapon Equip(int newWeaponIndex)
    {
        if (weapons != null && weapons.Length > 0)
        {
            if (newWeaponIndex >= 0 && newWeaponIndex < weapons.Length)
            {
                if (equippedIndex != newWeaponIndex)
                {
                    if (equippedWeapon != null)
                    {
                        equippedWeapon.gameObject.SetActive(false);
                    }

                    equippedIndex = newWeaponIndex;
                    equippedWeapon = weapons[equippedIndex];
                    equippedWeapon.gameObject.SetActive(true);
                }
            }
        }

        return equippedWeapon;
    }

    // Returns the previous weapon index and wraps back to the final weapon when needed.
    public int GetLastIndex()
    {
        int newIndex = equippedIndex - 1;

        if (newIndex < 0)
        {
            newIndex = weapons.Length - 1;
        }

        return newIndex;
    }

    // Returns the next weapon index and wraps back to the first weapon when needed.
    public int GetNextIndex()
    {
        int newIndex = equippedIndex + 1;

        if (newIndex >= weapons.Length)
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
}

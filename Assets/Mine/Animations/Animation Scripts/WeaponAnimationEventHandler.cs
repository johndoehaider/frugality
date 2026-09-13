using UnityEngine;

public class WeaponAnimationEventHandler : MonoBehaviour
{
    private Weapon weapon;

    // Gets the Weapon component on the same GameObject.
    private void Awake()
    {
        weapon = GetComponent<Weapon>();
    }

    // Animation Events call methods at exact points inside an animation clip.
    // Called by the weapon animation when a casing should be ejected.
    private void OnEjectCasing()
    {
        if (weapon != null)
            weapon.EjectCasing();
    }
}

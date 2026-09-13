using UnityEngine;

public class Magazine : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxAmmoInClip = 10;

    [Header("UI")]
    [SerializeField] private Sprite sprite;

    // Returns the maximum amount of ammunition this magazine can hold.
    public int GetMaxAmmoInClip()
    {
        return maxAmmoInClip;
    }

    // Returns the sprite used to represent this magazine in the HUD.
    public Sprite GetSprite()
    {
        return sprite;
    }
}

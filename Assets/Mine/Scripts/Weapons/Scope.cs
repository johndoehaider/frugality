using UnityEngine;

public class Scope : MonoBehaviour
{
    [SerializeField] private Sprite sprite;

    // Returns the sprite this scope uses for HUD or scope-related UI.
    public Sprite GetSprite()
    {
        return sprite;
    }
}

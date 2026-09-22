using UnityEngine;

public class WallWeapon : MonoBehaviour, IInteractable
{

    [SerializeField] private int price;
    [SerializeField] private string weaponName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Interact(PlayerInteraction playerInteraction)
    {

    }

    public string GetInteractionText(PlayerInteraction playerInteraction)
    {
        return "Hold E to buy " + weaponName + " [Cost: " + price + "]";
    }

    public bool UsesContinuousInteract()
    {
        return false;
    }

}

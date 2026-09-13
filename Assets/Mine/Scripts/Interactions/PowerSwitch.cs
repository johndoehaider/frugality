using UnityEngine;

public class PowerSwitch : MonoBehaviour, IInteractable
{

    [SerializeField] private PowerManager powerManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Interact()
    {
        if (!powerManager.IsPowerOn())
        {
            powerManager.TurnOnPower();
        }
    }

    public string GetInteractionText()
    {
        if (!powerManager.IsPowerOn())
        {
            return "Hold E to Turn on the Power";
        }

        return "";
    }

    public bool UsesContinuousInteract()
    {
        return false;
    }
}

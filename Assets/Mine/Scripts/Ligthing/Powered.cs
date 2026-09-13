using UnityEngine;

public class PoweredLight : MonoBehaviour
{
    [SerializeField] private PowerManager powerManager;

    private Light lightComponent;

    private void Start()
    {
        lightComponent = GetComponent<Light>();
        lightComponent.enabled = false;

        powerManager.OnPowerTurnedOn += TurnOnLight;
    }

    private void TurnOnLight()
    {
        lightComponent.enabled = true;
    }

    private void OnDestroy()
    {
        powerManager.OnPowerTurnedOn -= TurnOnLight;
    }
}
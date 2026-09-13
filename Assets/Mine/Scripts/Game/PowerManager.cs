using UnityEngine;
using System;

public class PowerManager : MonoBehaviour
{
    public event Action OnPowerTurnedOn;
    
    private bool isPowerOn = false;

    public void TurnOnPower()
    {
        if (!isPowerOn)
        {
            isPowerOn = true;
            OnPowerTurnedOn?.Invoke();
        }
    }

    public bool IsPowerOn()
    {
        return isPowerOn;
    }

}

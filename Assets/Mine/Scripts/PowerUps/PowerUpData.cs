using UnityEngine;

public enum PowerUpType
{
    MaxAmmo,
    Nuke,
    InstaKill,
    DoublePoints,
    Carpenter
}

[CreateAssetMenu(fileName = "NewPowerUp", menuName = "Zombies/Power Up")]
public class PowerUpData : ScriptableObject
{
    public string powerUpName;
    public PowerUpType powerUpType;
    public Sprite icon;
}
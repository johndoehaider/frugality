using UnityEngine;

public enum PerkType
{
    JuggerNog,
    SpeedCola,
    DoubleTap,
    StaminUp,
    QuickRevive
}

[CreateAssetMenu(fileName = "NewPerk", menuName = "Frugality/Perk")]
public class PerkData : ScriptableObject
{
    public string perkName;
    public PerkType perkType;
    public int cost;
    public Sprite icon;
}
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyBehaviorData", menuName = "Frugality/Zombie Behavior Data")]
public class EnemyBehaviorData : ScriptableObject
{
    [Header("Targeting")]
    [Tooltip("Minimum time before a zombie considers switching to a different player.")]
    [SerializeField] private float minTargetRefreshTime = 1f;

    [Tooltip("Maximum time before a zombie considers switching to a different player.")]
    [SerializeField] private float maxTargetRefreshTime = 3f;

    [Header("Navigation")]
    [SerializeField] private float pathRefreshInterval = 0.2f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float angularSpeed = 120f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float stoppingDistance = 1.1f;


    public float GetMinTargetRefreshTime()
    {
        return minTargetRefreshTime;
    }

    public float GetMaxTargetRefreshTime()
    {
        return maxTargetRefreshTime;
    }

    public float GetPathRefreshInterval()
    {
        return pathRefreshInterval;
    }

    public float GetAcceleration()
    {
        return acceleration;
    }

    public float GetAngularSpeed()
    {
        return angularSpeed;
    }

    public float GetAttackRange()
    {
        return attackRange;
    }

    public float GetStoppingDistance()
    {
        return stoppingDistance;
    }
}
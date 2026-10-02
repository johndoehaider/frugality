using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "EnemyBehaviorData", menuName = "Frugality/Zombie Behavior Data")]
public class EnemyBehaviorData : ScriptableObject
{
    [Header("Targeting")]
    [Min(0f)]
    [SerializeField] private float minTargetRefreshTime = 1f;
    [Min(0f)]
    [SerializeField] private float maxTargetRefreshTime = 3f;

    [Header("Navigation")]
    [Min(0.01f)]
    [SerializeField] private float pathRefreshInterval = 0.2f;
    [Min(0f)]
    [SerializeField] private float acceleration = 8f;
    [Min(0f)]
    [SerializeField] private float angularSpeed = 120f;
    [SerializeField] private ObstacleAvoidanceType defaultAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

    [Header("Combat")]
    [Min(0f)]
    [SerializeField] private float attackRange = 1.5f;
    [Min(0f)]
    [SerializeField] private float stoppingDistance = 1.1f;

    [Header("Barrier Entry")]
    [Min(0f)]
    [SerializeField] private float entryStagingDistance = 1.5f;
    [Min(0f)]
    [SerializeField] private float entryApproachDistance = 0.15f;
    [Range(0f, 180f)]
    [SerializeField] private float entryAlignmentAngle = 5f;
    [FormerlySerializedAs("entryAlignmentSpeed")]
    [Min(0f)]
    [SerializeField] private float entryTurnSpeed = 100f;
    [SerializeField] private ObstacleAvoidanceType entryAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
    [Min(0f)]
    [SerializeField] private float minEntrySlotRetryTime = 0.4f;
    [Min(0f)]
    [SerializeField] private float maxEntrySlotRetryTime = 0.6f;

    public float GetMinTargetRefreshTime() => minTargetRefreshTime;
    public float GetMaxTargetRefreshTime() => maxTargetRefreshTime;
    public float GetPathRefreshInterval() => pathRefreshInterval;
    public float GetAcceleration() => acceleration;
    public float GetAngularSpeed() => angularSpeed;
    public ObstacleAvoidanceType GetDefaultAvoidanceType() => defaultAvoidanceType;
    public float GetAttackRange() => attackRange;
    public float GetStoppingDistance() => stoppingDistance;
    public float GetEntryStagingDistance() => entryStagingDistance;
    public float GetEntryApproachDistance() => entryApproachDistance;
    public float GetEntryAlignmentAngle() => entryAlignmentAngle;
    public float GetEntryTurnSpeed() => entryTurnSpeed;
    public ObstacleAvoidanceType GetEntryAvoidanceType() => entryAvoidanceType;
    public float GetMinEntrySlotRetryTime() => minEntrySlotRetryTime;
    public float GetMaxEntrySlotRetryTime() => maxEntrySlotRetryTime;

    private void OnValidate()
    {
        if (maxTargetRefreshTime < minTargetRefreshTime)
        {
            maxTargetRefreshTime = minTargetRefreshTime;
        }

        if (maxEntrySlotRetryTime < minEntrySlotRetryTime)
        {
            maxEntrySlotRetryTime = minEntrySlotRetryTime;
        }
    }
}

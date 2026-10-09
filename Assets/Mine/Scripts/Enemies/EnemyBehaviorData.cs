using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "EnemyBehaviorData", menuName = "Frugality/Zombie Behavior Data")]
public class EnemyBehaviorData : ScriptableObject
{
    #region Targeting

    [HideInInspector]
    [SerializeField] private float minTargetRefreshTime = 1f;

    [HideInInspector]
    [SerializeField] private float maxTargetRefreshTime = 3f;

    #endregion

    #region Movement

    [Header("Movement")]
    [HideInInspector]
    [SerializeField] private float pathRefreshInterval = 0.2f;

    [Min(0f)]
    [SerializeField] private float acceleration = 8f;

    [Min(0f)]
    [SerializeField] private float angularSpeed = 120f;

    [SerializeField] private ObstacleAvoidanceType defaultAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

    #endregion

    #region Chase Weave

    [Header("Chase Weave")]
    [FormerlySerializedAs("chaseDeviationActivationDistance")]
    [Min(0f)]
    [SerializeField] private float chaseWeaveStartDistance = 6f;

    [FormerlySerializedAs("chaseDeviationRadius")]
    [Min(0f)]
    [SerializeField] private float chaseWeaveWidth = 0.65f;

    [Min(0.25f)]
    [SerializeField] private float chaseWeaveLookahead = 3f;

    [Min(0.25f)]
    [SerializeField] private float chaseWeaveRefreshTime = 2f;

    #endregion

    #region Combat

    [Header("Combat")]
    [Min(0f)]
    [SerializeField] private float attackRange = 1.5f;

    [Min(0f)]
    [SerializeField] private float stoppingDistance = 1.1f;

    #endregion

    #region Barrier Entry

    [Header("Barrier")]
    [HideInInspector]
    [SerializeField] private float entryStagingDistance = 1.5f;

    [HideInInspector]
    [SerializeField] private float entryApproachDistance = 0.15f;

    [FormerlySerializedAs("entryAlignmentSpeed")]
    [HideInInspector]
    [SerializeField] private float entryAlignmentAngle = 5f;

    [HideInInspector]
    [SerializeField] private float entryTurnSpeed = 100f;

    [SerializeField] private ObstacleAvoidanceType entryAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

    [HideInInspector]
    [SerializeField] private float minEntrySlotRetryTime = 0.4f;

    [HideInInspector]
    [SerializeField] private float maxEntrySlotRetryTime = 0.6f;

    #endregion

    #region Getters

    public float GetMinTargetRefreshTime() => minTargetRefreshTime;
    public float GetMaxTargetRefreshTime() => maxTargetRefreshTime;

    public float GetPathRefreshInterval() => pathRefreshInterval;
    public float GetAcceleration() => acceleration;
    public float GetAngularSpeed() => angularSpeed;
    public ObstacleAvoidanceType GetDefaultAvoidanceType() => defaultAvoidanceType;

    public float GetChaseWeaveStartDistance() => chaseWeaveStartDistance;
    public float GetChaseWeaveWidth() => chaseWeaveWidth;
    public float GetChaseWeaveLookahead() => chaseWeaveLookahead;
    public float GetChaseWeaveRefreshTime() => chaseWeaveRefreshTime;

    public float GetAttackRange() => attackRange;
    public float GetStoppingDistance() => stoppingDistance;

    public float GetEntryStagingDistance() => entryStagingDistance;
    public float GetEntryApproachDistance() => entryApproachDistance;
    public float GetEntryAlignmentAngle() => entryAlignmentAngle;
    public float GetEntryTurnSpeed() => entryTurnSpeed;
    public ObstacleAvoidanceType GetEntryAvoidanceType() => entryAvoidanceType;
    public float GetMinEntrySlotRetryTime() => minEntrySlotRetryTime;
    public float GetMaxEntrySlotRetryTime() => maxEntrySlotRetryTime;

    #endregion

    #region Validation

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

    #endregion
}
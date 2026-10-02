using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNavigation : MonoBehaviour
{
    #region Inspector

    [SerializeField] private EnemyBehaviorData behaviorData;

    #endregion

    #region Runtime State

    private NavMeshAgent agent;
    private float nextPathRefreshTime;
    private bool isMovementPaused;
    private bool isManualTraversal;
    private bool isShutdown;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.acceleration = behaviorData.GetAcceleration();
        agent.stoppingDistance = behaviorData.GetStoppingDistance();
        agent.obstacleAvoidanceType = behaviorData.GetDefaultAvoidanceType();
        agent.updateRotation = false;
    }

    #endregion

    #region NavMesh Movement

    public void MoveTowards(Vector3 destination, float turnSpeed)
    {
        if (isShutdown || isMovementPaused || isManualTraversal)
        {
            return;
        }

        if (Time.time >= nextPathRefreshTime)
        {
            agent.SetDestination(destination);
            nextPathRefreshTime =
                Time.time + behaviorData.GetPathRefreshInterval();
        }

        RotateTowardsMovement(turnSpeed);
    }

    public bool AlignTowardsPosition(
        Vector3 position,
        float angleTolerance,
        float turnSpeed)
    {
        if (isShutdown)
        {
            return false;
        }

        if (!TryGetFlatDirection(position, out Vector3 direction))
        {
            return true;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            Mathf.Max(0f, turnSpeed) * Time.deltaTime
        );

        return Quaternion.Angle(
            transform.rotation,
            targetRotation
        ) <= Mathf.Max(0f, angleTolerance);
    }

    public void FacePosition(Vector3 position)
    {
        if (isShutdown ||
            !TryGetFlatDirection(position, out Vector3 direction))
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction);
    }

    public void ClearPath()
    {
        if (isShutdown || isManualTraversal)
        {
            return;
        }

        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        nextPathRefreshTime = 0f;
    }

    public bool IsWithinHorizontalDistance(
        Vector3 position,
        float distance)
    {
        Vector3 difference = position - transform.position;
        difference.y = 0f;

        return difference.sqrMagnitude <= distance * distance;
    }

    #endregion

    #region Manual Traversal

    public void BeginManualTraversal()
    {
        if (isShutdown || isManualTraversal)
        {
            return;
        }

        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        agent.isStopped = true;
        agent.updatePosition = false;

        isMovementPaused = true;
        isManualTraversal = true;
    }

    public bool MoveManuallyTowards(
        Vector3 destination,
        float speed,
        float turnSpeed)
    {
        if (isShutdown || !isManualTraversal)
        {
            return false;
        }

        if (TryGetFlatDirection(destination, out Vector3 direction))
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                Mathf.Max(0f, turnSpeed) * Time.deltaTime
            );
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            Mathf.Max(0f, speed) * Time.deltaTime
        );

        return Vector3.SqrMagnitude(
            destination - transform.position
        ) <= 0.0001f;
    }

    public bool CompleteManualTraversal()
    {
        if (isShutdown || !isManualTraversal)
        {
            return false;
        }

        if (!agent.Warp(transform.position))
        {
            return false;
        }

        agent.updatePosition = true;
        agent.isStopped = false;

        isManualTraversal = false;
        isMovementPaused = false;
        nextPathRefreshTime = 0f;

        return true;
    }

    public void CancelManualTraversal()
    {
        if (!isManualTraversal)
        {
            return;
        }

        isManualTraversal = false;
    }

    #endregion

    #region Navigation Settings

    public void SetMovementSpeed(float speed)
    {
        if (!isShutdown)
        {
            agent.speed = speed;
        }
    }

    public void SetStoppingDistance(float distance)
    {
        if (!isShutdown)
        {
            agent.stoppingDistance = Mathf.Max(0f, distance);
        }
    }

    public void UseDefaultStoppingDistance()
    {
        SetStoppingDistance(behaviorData.GetStoppingDistance());
    }

    public void UseDefaultAvoidance()
    {
        if (!isShutdown)
        {
            agent.obstacleAvoidanceType =
                behaviorData.GetDefaultAvoidanceType();
        }
    }

    public void UseEntryAvoidance()
    {
        if (!isShutdown)
        {
            agent.obstacleAvoidanceType =
                behaviorData.GetEntryAvoidanceType();
        }
    }

    #endregion

    #region Movement Control

    public void PauseMovement()
    {
        if (isShutdown || isMovementPaused || isManualTraversal)
        {
            return;
        }

        isMovementPaused = true;
        agent.isStopped = true;
    }

    public void ResumeMovement()
    {
        if (isShutdown || !isMovementPaused || isManualTraversal)
        {
            return;
        }

        isMovementPaused = false;
        agent.isStopped = false;
    }

    public void Shutdown()
    {
        if (isShutdown)
        {
            return;
        }

        isShutdown = true;
        isMovementPaused = true;
        isManualTraversal = false;

        if (agent.enabled)
        {
            agent.isStopped = true;

            if (agent.hasPath)
            {
                agent.ResetPath();
            }

            agent.enabled = false;
        }
    }

    #endregion

    #region Rotation Helpers

    private void RotateTowardsMovement(float turnSpeed)
    {
        Vector3 direction = agent.desiredVelocity;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            Mathf.Max(0f, turnSpeed) * Time.deltaTime
        );
    }

    private bool TryGetFlatDirection(
        Vector3 position,
        out Vector3 direction)
    {
        direction = position - transform.position;
        direction.y = 0f;

        return direction.sqrMagnitude >= 0.0001f;
    }

    #endregion
}

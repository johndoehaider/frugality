using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNavigation : MonoBehaviour
{
    #region Constants

    private const int ChasePathCornerBufferSize = 64;
    private const float WeaveRefreshVariation = 0.15f;
    private const float MinimumWeaveSampleRadius = 0.05f;
    private const float MaximumWeaveSampleRadius = 0.2f;
    private const float MinimumUsefulLookahead = 0.5f;
    private const float MinimumGoalDirectionDot = 0.5f;
    private const float ObstacleFacingReleaseDelay = 0.25f;
    private const float MinimumPathFacingDistance = 0.1f;

    #endregion
    #region Runtime State

    private NavMeshAgent agent;
    private NavMeshPath chasePath;
    private readonly Vector3[] chasePathCorners = new Vector3[ChasePathCornerBufferSize];

    private float nextPathRefreshTime;
    private float nextWeaveRefreshTime;
    private float nextObstacleFacingCheckTime;
    private float obstacleFacingReleaseTime;

    private bool isMovementPaused;
    private bool isManualTraversal;
    private bool isShutdown;
    private bool hasWeaveGoal;
    private bool isRoutingAroundObstacle;

    private Vector3 weaveGoal;
    private Vector3 weaveForwardDirection;
    private Vector3 pathFacingDirection;

    #endregion
    #region Dependencies

    private EnemyBehaviorData behaviorData;

    #endregion
    #region Initialization

    public void Initialize(EnemyBehaviorData data)
    {
        behaviorData = data;
        agent = GetComponent<NavMeshAgent>();
        chasePath = new NavMeshPath();

        agent.acceleration = behaviorData.GetAcceleration();
        agent.angularSpeed = behaviorData.GetAngularSpeed();
        agent.stoppingDistance = behaviorData.GetStoppingDistance();
        agent.obstacleAvoidanceType = behaviorData.GetDefaultAvoidanceType();
        agent.updateRotation = true;
    }

    #endregion
    #region NavMesh Movement

    public void MoveTowards(Vector3 destination, float turnSpeed)
    {
        if (isShutdown || isMovementPaused || isManualTraversal)
        {
            return;
        }

        ClearChaseWeave();
        ClearChaseFacing();

        agent.updateRotation = true;
        agent.angularSpeed = Mathf.Max(0f, turnSpeed);

        if (Time.time >= nextPathRefreshTime)
        {
            agent.SetDestination(destination);
            nextPathRefreshTime = Time.time + behaviorData.GetPathRefreshInterval();
        }
    }

    public void MoveTowardsFacingPosition(Vector3 destination, float turnSpeed)
    {
        if (isShutdown || isMovementPaused || isManualTraversal)
        {
            return;
        }

        agent.updateRotation = false;

        Vector3 navigationDestination = GetChaseDestination(destination);

        if (Time.time >= nextPathRefreshTime)
        {
            agent.SetDestination(navigationDestination);
            nextPathRefreshTime = Time.time + behaviorData.GetPathRefreshInterval();
        }

        UpdateChaseRotation(destination, turnSpeed);
    }

    private void UpdateChaseRotation(Vector3 playerPosition, float turnSpeed)
    {
        UpdateObstacleFacingState(playerPosition);

        Vector3 direction;

        if (isRoutingAroundObstacle && pathFacingDirection.sqrMagnitude >= MinimumPathFacingDistance * MinimumPathFacingDistance)
        {
            direction = pathFacingDirection;
        }
        else if (!TryGetFlatDirection(playerPosition, out direction))
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Mathf.Max(0f, turnSpeed) * Time.deltaTime);
    }

    private void UpdateObstacleFacingState(Vector3 playerPosition)
    {
        if (Time.time < nextObstacleFacingCheckTime)
        {
            return;
        }

        nextObstacleFacingCheckTime = Time.time + behaviorData.GetPathRefreshInterval();

        bool directRouteBlocked = NavMesh.Raycast(agent.nextPosition, playerPosition, out _, agent.areaMask);

        if (directRouteBlocked)
        {
            isRoutingAroundObstacle = true;
            obstacleFacingReleaseTime = Time.time + ObstacleFacingReleaseDelay;
            UpdatePathFacingDirection();
            return;
        }

        if (isRoutingAroundObstacle && Time.time < obstacleFacingReleaseTime)
        {
            UpdatePathFacingDirection();
            return;
        }

        isRoutingAroundObstacle = false;
        pathFacingDirection = Vector3.zero;
    }

    private void UpdatePathFacingDirection()
    {
        if (agent.pathPending || !agent.hasPath)
        {
            return;
        }

        Vector3 direction = agent.steeringTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < MinimumPathFacingDistance * MinimumPathFacingDistance)
        {
            return;
        }

        pathFacingDirection = direction.normalized;
    }

    private void ClearChaseFacing()
    {
        isRoutingAroundObstacle = false;
        pathFacingDirection = Vector3.zero;
        nextObstacleFacingCheckTime = 0f;
        obstacleFacingReleaseTime = 0f;
    }

    private Vector3 GetChaseDestination(Vector3 playerPosition)
    {
        float startDistance = behaviorData.GetChaseWeaveStartDistance();
        float sqrDistanceToPlayer = GetFlatSqrDistance(transform.position, playerPosition);

        if (sqrDistanceToPlayer <= startDistance * startDistance)
        {
            ClearChaseWeave();
            return playerPosition;
        }

        if (hasWeaveGoal)
        {
            float goalTolerance = Mathf.Max(agent.radius, 0.25f);

            bool reachedGoal = GetFlatSqrDistance(transform.position, weaveGoal) <= goalTolerance * goalTolerance;
            bool passedGoal = HasPassedWeaveGoal();
            bool staleGoal = !IsWeaveGoalUsefulForPlayer(playerPosition);

            if (reachedGoal || passedGoal || staleGoal)
            {
                hasWeaveGoal = false;
                nextWeaveRefreshTime = 0f;
            }
        }

        if (Time.time >= nextWeaveRefreshTime)
        {
            TryCreateWeaveGoal(playerPosition);
        }

        return hasWeaveGoal ? weaveGoal : playerPosition;
    }

    private void TryCreateWeaveGoal(Vector3 playerPosition)
    {
        ScheduleNextWeaveRefresh();
        hasWeaveGoal = false;

        float weaveWidth = behaviorData.GetChaseWeaveWidth();

        if (weaveWidth <= 0f)
        {
            return;
        }

        chasePath.ClearCorners();

        if (!agent.CalculatePath(playerPosition, chasePath) || chasePath.status != NavMeshPathStatus.PathComplete)
        {
            return;
        }

        int cornerCount = chasePath.GetCornersNonAlloc(chasePathCorners);

        if (cornerCount < 2)
        {
            return;
        }

        float distanceToPlayer = Mathf.Sqrt(GetFlatSqrDistance(transform.position, playerPosition));
        float maximumSafeLookahead = distanceToPlayer - agent.stoppingDistance - 0.5f;
        float lookahead = Mathf.Min(behaviorData.GetChaseWeaveLookahead(), maximumSafeLookahead);

        if (lookahead < MinimumUsefulLookahead)
        {
            return;
        }

        if (!TryGetPointAlongPath(chasePathCorners, cornerCount, lookahead, out Vector3 pathPoint, out Vector3 pathDirection))
        {
            return;
        }

        Vector3 forwardDirection = pathPoint - transform.position;
        forwardDirection.y = 0f;

        if (forwardDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        forwardDirection.Normalize();

        Vector3 lateralDirection = Vector3.Cross(Vector3.up, pathDirection).normalized;
        float lateralOffset = Random.Range(-weaveWidth, weaveWidth);
        Vector3 candidate = pathPoint + lateralDirection * lateralOffset;

        float sampleRadius = Mathf.Clamp(weaveWidth * 0.25f, MinimumWeaveSampleRadius, MaximumWeaveSampleRadius);

        if (!NavMesh.SamplePosition(candidate, out NavMeshHit sampledHit, sampleRadius, agent.areaMask))
        {
            return;
        }

        Vector3 sampledGoal = sampledHit.position;

        Vector3 toSampledGoal = sampledGoal - transform.position;
        toSampledGoal.y = 0f;

        if (Vector3.Dot(toSampledGoal, forwardDirection) <= 0f)
        {
            return;
        }

        Vector3 sampledOffsetFromPath = sampledGoal - pathPoint;
        sampledOffsetFromPath.y = 0f;

        float sampledLateralDistance = Mathf.Abs(Vector3.Dot(sampledOffsetFromPath, lateralDirection));

        if (sampledLateralDistance > weaveWidth)
        {
            return;
        }

        if (NavMesh.Raycast(pathPoint, sampledGoal, out _, agent.areaMask))
        {
            return;
        }

        weaveGoal = sampledGoal;
        weaveForwardDirection = forwardDirection;
        hasWeaveGoal = true;
    }

    private bool TryGetPointAlongPath(Vector3[] corners, int cornerCount, float distance, out Vector3 point, out Vector3 pathDirection)
    {
        float remainingDistance = distance;

        for (int i = 1; i < cornerCount; i++)
        {
            Vector3 segmentStart = corners[i - 1];
            Vector3 segmentEnd = corners[i];
            Vector3 segment = segmentEnd - segmentStart;

            segment.y = 0f;

            float segmentLength = segment.magnitude;

            if (segmentLength < 0.0001f)
            {
                continue;
            }

            if (remainingDistance <= segmentLength)
            {
                point = Vector3.Lerp(segmentStart, segmentEnd, remainingDistance / segmentLength);
                pathDirection = segment / segmentLength;
                return true;
            }

            remainingDistance -= segmentLength;
        }

        Vector3 finalSegment = corners[cornerCount - 1] - corners[cornerCount - 2];
        finalSegment.y = 0f;

        if (finalSegment.sqrMagnitude < 0.0001f)
        {
            point = default;
            pathDirection = default;
            return false;
        }

        point = corners[cornerCount - 1];
        pathDirection = finalSegment.normalized;

        return true;
    }

    private bool HasPassedWeaveGoal()
    {
        if (weaveForwardDirection.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        Vector3 toGoal = weaveGoal - transform.position;
        toGoal.y = 0f;

        return Vector3.Dot(toGoal, weaveForwardDirection) <= 0f;
    }

    private bool IsWeaveGoalUsefulForPlayer(Vector3 playerPosition)
    {
        Vector3 toGoal = weaveGoal - transform.position;
        Vector3 toPlayer = playerPosition - transform.position;

        toGoal.y = 0f;
        toPlayer.y = 0f;

        if (toGoal.sqrMagnitude < 0.0001f || toPlayer.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        return Vector3.Dot(toGoal.normalized, toPlayer.normalized) >= MinimumGoalDirectionDot;
    }

    private void ScheduleNextWeaveRefresh()
    {
        float refreshTime = behaviorData.GetChaseWeaveRefreshTime();
        float variation = refreshTime * WeaveRefreshVariation;

        float minimumRefreshTime = Mathf.Max(0.1f, refreshTime - variation);
        float maximumRefreshTime = refreshTime + variation;

        nextWeaveRefreshTime = Time.time + Random.Range(minimumRefreshTime, maximumRefreshTime);
    }

    private void ClearChaseWeave()
    {
        hasWeaveGoal = false;
        weaveGoal = Vector3.zero;
        weaveForwardDirection = Vector3.zero;
        nextWeaveRefreshTime = 0f;
    }

    public void AlignTowardsPositionImmediate(Vector3 position)
    {
        FacePosition(position);
    }

    public bool AlignTowardsPosition(Vector3 position, float angleTolerance, float turnSpeed)
    {
        if (isShutdown)
        {
            return false;
        }

        agent.updateRotation = false;

        if (!TryGetFlatDirection(position, out Vector3 direction))
        {
            return true;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Mathf.Max(0f, turnSpeed) * Time.deltaTime);

        return Quaternion.Angle(transform.rotation, targetRotation) <= Mathf.Max(0f, angleTolerance);
    }

    public void FacePosition(Vector3 position)
    {
        if (isShutdown || !TryGetFlatDirection(position, out Vector3 direction))
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

        ClearChaseWeave();
        ClearChaseFacing();
        nextPathRefreshTime = 0f;
    }

    public bool IsWithinHorizontalDistance(Vector3 position, float distance)
    {
        return GetFlatSqrDistance(transform.position, position) <= distance * distance;
    }

    #endregion

    #region Manual Traversal

    public void BeginManualTraversal()
    {
        if (isShutdown || isManualTraversal)
        {
            return;
        }

        ClearChaseWeave();
        ClearChaseFacing();

        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        isMovementPaused = true;
        isManualTraversal = true;
    }

    public bool MoveManuallyTowards(Vector3 destination, float speed, float turnSpeed)
    {
        if (isShutdown || !isManualTraversal)
        {
            return false;
        }

        if (TryGetFlatDirection(destination, out Vector3 direction))
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Mathf.Max(0f, turnSpeed) * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, destination, Mathf.Max(0f, speed) * Time.deltaTime);

        return Vector3.SqrMagnitude(destination - transform.position) <= 0.0001f;
    }

    public bool CompleteManualTraversal()
    {
        if (isShutdown || !isManualTraversal)
        {
            return false;
        }

        return RestoreAutomaticNavigation();
    }

    public void CancelManualTraversal()
    {
        if (isShutdown || !isManualTraversal)
        {
            return;
        }

        RestoreAutomaticNavigation();
    }

    private bool RestoreAutomaticNavigation()
    {
        if (!agent.Warp(transform.position))
        {
            return false;
        }

        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.angularSpeed = behaviorData.GetAngularSpeed();
        agent.isStopped = false;

        isManualTraversal = false;
        isMovementPaused = false;
        nextPathRefreshTime = 0f;

        return true;
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
            agent.obstacleAvoidanceType = behaviorData.GetDefaultAvoidanceType();
        }
    }

    public void UseEntryAvoidance()
    {
        if (!isShutdown)
        {
            agent.obstacleAvoidanceType = behaviorData.GetEntryAvoidanceType();
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

        ClearChaseWeave();
        ClearChaseFacing();

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
    #region Helpers

    private bool TryGetFlatDirection(Vector3 position, out Vector3 direction)
    {
        direction = position - transform.position;
        direction.y = 0f;

        return direction.sqrMagnitude >= 0.0001f;
    }

    private float GetFlatSqrDistance(Vector3 firstPosition, Vector3 secondPosition)
    {
        Vector3 difference = secondPosition - firstPosition;
        difference.y = 0f;

        return difference.sqrMagnitude;
    }

    #endregion
}
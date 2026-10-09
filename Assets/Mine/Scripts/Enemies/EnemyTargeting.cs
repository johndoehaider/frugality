using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyTargeting : MonoBehaviour
{
    private EnemyBehaviorData behaviorData;
    private GameManager gameManager;
    private Character currentTarget;
    private PlayerHealth currentTargetHealth;
    private NavMeshPath targetPath;
    
    private const int TargetPathCornerBufferSize = 64;
    private readonly Vector3[] targetPathCorners = new Vector3[TargetPathCornerBufferSize];

    private float nextTargetRefreshTime;
    private bool isPaused;
    private bool isShutdown;

    public void Initialize(EnemyBehaviorData data, GameManager manager)
    {
        behaviorData = data;
        gameManager = manager;
        targetPath = new NavMeshPath();
    }

    private void Start()
    {
        if (!isPaused)
        {
            RefreshTarget();
        }
    }

    private void Update()
    {
        if (isPaused || isShutdown)
        {
            return;
        }

        if (IsCurrentTargetValid())
        {
            if (Time.time >= nextTargetRefreshTime)
            {
                RefreshTarget();
            }

            return;
        }

        if (currentTarget != null || Time.time >= nextTargetRefreshTime)
        {
            RefreshTarget();
        }
    }

    public bool TryGetCurrentTarget(out Character target, out PlayerHealth targetHealth)
    {
        target = currentTarget;
        targetHealth = currentTargetHealth;

        return IsCurrentTargetValid();
    }

    public void PauseTargeting()
    {
        if (isShutdown || isPaused)
        {
            return;
        }

        isPaused = true;
        ClearTarget();
    }

    public void ResumeTargeting()
    {
        if (isShutdown || !isPaused)
        {
            return;
        }

        isPaused = false;
        RefreshTarget();
    }

    public void Shutdown()
    {
        if (isShutdown)
        {
            return;
        }

        isShutdown = true;
        isPaused = true;
        ClearTarget();
        enabled = false;
    }

    private void RefreshTarget()
    {
        FindBestTarget();

        nextTargetRefreshTime = Time.time + Random.Range(
            behaviorData.GetMinTargetRefreshTime(),
            behaviorData.GetMaxTargetRefreshTime()
        );
    }

    private bool IsCurrentTargetValid()
    {
        return currentTarget != null &&
               currentTargetHealth != null &&
               !currentTargetHealth.IsDead();
    }

    private void FindBestTarget()
    {
        IReadOnlyList<Character> players = gameManager.GetPlayers();

        Character bestTarget = null;
        PlayerHealth bestTargetHealth = null;
        float shortestPathDistance = Mathf.Infinity;

        for (int i = 0; i < players.Count; i++)
        {
            Character player = players[i];

            if (!TryGetValidPlayerHealth(player, out PlayerHealth playerHealth))
            {
                continue;
            }

            float pathDistance = CalculatePathDistance(player.transform.position);

            if (pathDistance >= shortestPathDistance)
            {
                continue;
            }

            shortestPathDistance = pathDistance;
            bestTarget = player;
            bestTargetHealth = playerHealth;
        }

        currentTarget = bestTarget;
        currentTargetHealth = bestTargetHealth;
    }

    private float CalculatePathDistance(Vector3 targetPosition)
    {
        targetPath.ClearCorners();

        if (!NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, targetPath) || targetPath.status != NavMeshPathStatus.PathComplete)
        {
            return Mathf.Infinity;
        }

        int cornerCount = targetPath.GetCornersNonAlloc(targetPathCorners);
        float distance = 0f;

        for (int i = 1; i < cornerCount; i++)
        {
            distance += Vector3.Distance(targetPathCorners[i - 1], targetPathCorners[i]);
        }

        return distance;
    }

    private bool TryGetValidPlayerHealth(Character player, out PlayerHealth playerHealth)
    {
        playerHealth = null;

        if (player == null)
        {
            return false;
        }

        playerHealth = player.GetComponent<PlayerHealth>();
        return playerHealth != null && !playerHealth.IsDead();
    }

    private void ClearTarget()
    {
        currentTarget = null;
        currentTargetHealth = null;
    }
}

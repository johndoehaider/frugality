using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyTargeting : MonoBehaviour
{
    [SerializeField] private EnemyBehaviorData behaviorData;
    private GameManager gameManager;

    private Character currentTarget;
    private PlayerHealth currentTargetHealth;
    private NavMeshPath targetPath;
    private float nextTargetRefreshTime;


    private void Awake()
    {
        targetPath = new NavMeshPath();
    }

    private void Start()
    {
        if (gameManager == null)
        {
            Debug.LogError(
                "EnemyTargeting was not given a GameManager.",
                this
            );

            enabled = false;
            return;
        }

        FindBestTarget();
        ScheduleNextTargetRefresh();
    }

    private void Update()
    {
        if (!IsCurrentTargetValid())
        {
            FindBestTarget();
            ScheduleNextTargetRefresh();
            return;
        }

        if (Time.time >= nextTargetRefreshTime)
        {
            FindBestTarget();
            ScheduleNextTargetRefresh();
        }
    }

    private bool IsCurrentTargetValid()
    {
        return currentTarget != null
            && currentTargetHealth != null
            && !currentTargetHealth.IsDead();
    }

    private void FindBestTarget()
    {
        if (gameManager == null)
        {
            ClearTarget();
            return;
        }

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

            float pathDistance =
                CalculatePathDistance(player.transform.position);

            if (pathDistance < shortestPathDistance)
            {
                shortestPathDistance = pathDistance;
                bestTarget = player;
                bestTargetHealth = playerHealth;
            }
        }

        currentTarget = bestTarget;
        currentTargetHealth = bestTargetHealth;
    }


    private float CalculatePathDistance(Vector3 targetPosition)
    {
        targetPath.ClearCorners();

        if (!NavMesh.CalculatePath(
            transform.position,
            targetPosition,
            NavMesh.AllAreas,
            targetPath))
        {
            return Mathf.Infinity;
        }

        if (targetPath.status != NavMeshPathStatus.PathComplete)
        {
            return Mathf.Infinity;
        }

        Vector3[] corners = targetPath.corners;

        float distance = 0f;

        for (int i = 1; i < corners.Length; i++)
        {
            distance += Vector3.Distance(
                corners[i - 1],
                corners[i]
            );
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


    private void ScheduleNextTargetRefresh()
    {
        if (behaviorData == null)
        {
            return;
        }

        nextTargetRefreshTime =
            Time.time +
            Random.Range(
                behaviorData.GetMinTargetRefreshTime(),
                behaviorData.GetMaxTargetRefreshTime()
            );
    }

    private void ClearTarget()
    {
        currentTarget = null;
        currentTargetHealth = null;
    }

    public void Shutdown()
    {
        ClearTarget();
        enabled = false;
    }


    public Character GetCurrentTarget()
    {
        return currentTarget;
    }

    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }
}
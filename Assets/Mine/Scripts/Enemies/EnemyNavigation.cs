using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNavigation : MonoBehaviour
{
    [Header("Behavior")]
    [SerializeField] private EnemyBehaviorData behaviorData;

    private NavMeshAgent agent;

    private float nextPathRefreshTime;

    private bool isMovementPaused;
    private bool isShutdown;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.acceleration = behaviorData.GetAcceleration();
        agent.stoppingDistance = behaviorData.GetStoppingDistance();

        // Navigation controls position only.
        // Zombie rotation is handled manually toward its target.
        agent.updateRotation = false;
    }


    public void MoveTowards(Vector3 destination)
    {
        if (isShutdown || isMovementPaused)
        {
            return;
        }

        RotateTowardsPosition(destination);

        if (Time.time >= nextPathRefreshTime)
        {
            agent.SetDestination(destination);

            nextPathRefreshTime =
                Time.time + behaviorData.GetPathRefreshInterval();
        }
    }

    private void RotateTowardsPosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            behaviorData.GetAngularSpeed() * Time.deltaTime
        );
    }


    public void SetMovementSpeed(float speed)
    {
        agent.speed = speed;
    }


    public void PauseMovement()
    {
        if (isShutdown)
        {
            return;
        }

        isMovementPaused = true;
        agent.isStopped = true;
    }


    public void ResumeMovement()
    {
        if (isShutdown)
        {
            return;
        }

        isMovementPaused = false;
        agent.isStopped = false;
    }


    public void FacePosition(Vector3 position)
    {
        if (isShutdown)
        {
            return;
        }

        Vector3 direction = position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }


    public void Shutdown()
    {
        if (isShutdown)
        {
            return;
        }

        isShutdown = true;
        isMovementPaused = true;

        agent.isStopped = true;
        agent.ResetPath();
        agent.enabled = false;
    }
    
    public bool IsWithinHorizontalDistance(Vector3 position, float distance)
    {
        Vector3 difference = position - transform.position;
        difference.y = 0f;

        return difference.sqrMagnitude <= distance * distance;
    }

    public void ClearPath()
    {
        if (isShutdown)
        {
            return;
        }

        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        nextPathRefreshTime = 0f;
    }
}
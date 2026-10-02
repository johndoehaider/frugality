using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyTargeting))]
[RequireComponent(typeof(EnemyNavigation))]
[RequireComponent(typeof(EnemyEntry))]
public class Enemy : MonoBehaviour
{
    #region AI State

    private enum EnemyAIState
    {
        Chasing,
        Attacking,
        Entering,
        Dead
    }

    #endregion

    #region Inspector

    [Header("Behavior")]
    [SerializeField] private EnemyBehaviorData behaviorData;

    [Header("Speeds")]
    [SerializeField] private float walkingSpeed = 1f;
    [SerializeField] private float runningSpeed = 3f;
    [SerializeField] private float sprintingSpeed = 6f;

    [Header("Health & Damage")]
    [SerializeField] private float damage = 60f;
    [SerializeField] private float health = 100f;

    #endregion

    #region Speed Distribution Constants

    private const int MoveSpeedRoundMultiplier = 8;
    private const int MoveSpeedRollRange = 35;
    private const int WalkSpeedThreshold = 35;
    private const int RunSpeedThreshold = 70;

    #endregion

    #region Runtime State

    private EnemyTargeting targeting;
    private EnemyNavigation navigation;
    private EnemyEntry entry;
    private Animator animator;
    private Collider rootCollider;

    private Character attackTarget;
    private PlayerHealth attackTargetHealth;

    private SpawnManager spawnManager;
    private PowerUpManager powerupManager;

    private EnemyAIState aiState = EnemyAIState.Chasing;
    private float currentSpeed;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        targeting = GetComponent<EnemyTargeting>();
        navigation = GetComponent<EnemyNavigation>();
        entry = GetComponent<EnemyEntry>();
        animator = GetComponentInChildren<Animator>();
        rootCollider = GetComponent<Collider>();

        entry.Initialize(this, navigation, behaviorData, animator);
    }

    private void Update()
    {
        switch (aiState)
        {
            case EnemyAIState.Chasing:
                UpdateChasing();
                break;

            case EnemyAIState.Entering:
                entry.Tick();
                break;

            case EnemyAIState.Attacking:
            case EnemyAIState.Dead:
                break;
        }
    }

    #endregion

    #region Chasing

    private void UpdateChasing()
    {
        if (!targeting.TryGetCurrentTarget(out Character target, out PlayerHealth targetHealth))
        {
            navigation.ClearPath();
            return;
        }

        if (navigation.IsWithinHorizontalDistance(
            target.transform.position,
            behaviorData.GetAttackRange()))
        {
            StartAttack(target, targetHealth);
            return;
        }

        navigation.MoveTowards(
            target.transform.position,
            behaviorData.GetAngularSpeed()
        );
    }

    private void BeginChasing()
    {
        aiState = EnemyAIState.Chasing;
        navigation.UseDefaultAvoidance();
        navigation.UseDefaultStoppingDistance();
        navigation.ResumeMovement();
        targeting.ResumeTargeting();
    }

    #endregion

    #region Combat

    private void StartAttack(Character target, PlayerHealth targetHealth)
    {
        attackTarget = target;
        attackTargetHealth = targetHealth;
        aiState = EnemyAIState.Attacking;

        navigation.PauseMovement();
        navigation.FacePosition(target.transform.position);
        animator.SetTrigger("Attack");
    }

    public void AttackHit()
    {
        if (aiState == EnemyAIState.Entering)
        {
            entry.AttackHit();
            return;
        }

        if (aiState != EnemyAIState.Attacking ||
            attackTarget == null ||
            attackTargetHealth == null ||
            attackTargetHealth.IsDead())
        {
            return;
        }

        if (navigation.IsWithinHorizontalDistance(
            attackTarget.transform.position,
            behaviorData.GetAttackRange()))
        {
            attackTargetHealth.LoseHealth(damage);
        }
    }

    public void AnimationEndedAttack()
    {
        if (aiState == EnemyAIState.Entering)
        {
            entry.AnimationEndedAttack();
            return;
        }

        if (aiState != EnemyAIState.Attacking)
        {
            return;
        }

        attackTarget = null;
        attackTargetHealth = null;

        BeginChasing();
    }

    #endregion

    #region Spawn Entry

    public void SetSpawnPoint(EnemySpawnPoint spawnPoint)
    {
        if (spawnPoint != null &&
            spawnPoint.GetEntryType() == EnemySpawnEntryType.Barrier)
        {
            if (!entry.Begin(spawnPoint.GetAssignedBarrier()))
            {
                Debug.LogError(
                    $"Enemy '{name}' could not begin barrier entry from spawn point '{spawnPoint.name}'.",
                    this
                );

                BeginChasing();
                return;
            }

            aiState = EnemyAIState.Entering;
            targeting.PauseTargeting();
            return;
        }

        BeginChasing();
    }

    public void CompleteEntry()
    {
        if (aiState != EnemyAIState.Entering)
        {
            return;
        }

        BeginChasing();
    }

    #endregion

    #region Health And Death

    public void LoseHealth(float amount, PlayerPoints playerPoints)
    {
        if (aiState == EnemyAIState.Dead)
        {
            return;
        }

        health -= amount;

        if (health > 0f)
        {
            playerPoints?.AddPoints(10);
            return;
        }

        playerPoints?.AddPoints(50);
        powerupManager?.TryDropPowerUp(transform.position);

        BeginDeath();
    }

    private void BeginDeath()
    {
        if (aiState == EnemyAIState.Dead)
        {
            return;
        }

        aiState = EnemyAIState.Dead;
        attackTarget = null;
        attackTargetHealth = null;

        entry.Cancel();
        targeting.Shutdown();
        navigation.Shutdown();

        spawnManager?.EnemyDied(this);
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        animator.SetTrigger("Die");
        currentSpeed = 0f;

        if (rootCollider != null)
        {
            rootCollider.enabled = false;
        }

        yield return new WaitForSeconds(20f);
        Destroy(gameObject);
    }

    #endregion

    #region Movement

    private void SetMovementAnimationRunner(bool isRunner)
    {
        animator.SetBool("IsWalker", !isRunner);
        animator.SetBool("IsRunner", isRunner);
    }

    public void Walkers()
    {
        currentSpeed = walkingSpeed;
        navigation.SetMovementSpeed(currentSpeed);
        SetMovementAnimationRunner(false);
    }

    public void Runners()
    {
        currentSpeed = runningSpeed;
        navigation.SetMovementSpeed(currentSpeed);
        SetMovementAnimationRunner(true);
    }

    public void Sprinters()
    {
        currentSpeed = sprintingSpeed;
        navigation.SetMovementSpeed(currentSpeed);
        SetMovementAnimationRunner(true);
    }

    public void DogSpeed()
    {
        currentSpeed = runningSpeed;
        navigation.SetMovementSpeed(currentSpeed);
    }

    #endregion

    #region Round Scaling

    public void SetHealthForRound(int round)
    {
        if (round <= 9)
        {
            health = 150f + (round - 1) * 100f;
        }
        else
        {
            health = 950f * Mathf.Pow(1.1f, round - 9);
        }
    }

    public void SetSpeedForRound(int round)
    {
        int moveSpeedValue =
            round <= 1
                ? 1
                : (round - 1) * MoveSpeedRoundMultiplier;

        int speedRoll = Random.Range(
            moveSpeedValue,
            moveSpeedValue + MoveSpeedRollRange
        );

        if (speedRoll <= WalkSpeedThreshold)
        {
            Walkers();
        }
        else if (speedRoll <= RunSpeedThreshold)
        {
            Runners();
        }
        else
        {
            Sprinters();
        }
    }

    #endregion

    #region Dependencies

    public void SetSpawnManager(SpawnManager manager)
    {
        spawnManager = manager;
    }

    public void SetPowerUpManager(PowerUpManager manager)
    {
        powerupManager = manager;
    }

    #endregion

    #region Powerups

    public void InstaKill()
    {
        health = 1f;
    }

    public void EndInstaKill()
    {
        if (aiState == EnemyAIState.Dead || spawnManager == null)
        {
            return;
        }

        SetHealthForRound(spawnManager.GetRoundNumber());
    }

    public void Nuke()
    {
        BeginDeath();
    }

    #endregion

    #region Getters

    public float GetEnemySpeed()
    {
        return currentSpeed;
    }

    public float GetEnemyHealth()
    {
        return health;
    }

    #endregion
}

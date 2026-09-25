using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyTargeting))]
[RequireComponent(typeof(EnemyNavigation))]
public class Enemy : MonoBehaviour
{
    [Header("Behavior")]
    [SerializeField] private EnemyBehaviorData behaviorData;

    [Header("Speeds")]
    [SerializeField] private float walkingSpeed = 1f;
    [SerializeField] private float runningSpeed = 3f;
    [SerializeField] private float sprintingSpeed = 6f;

    [Header("Health & Damage")]
    [SerializeField] private float damage = 60f;
    [SerializeField] private float health = 100f;

    private EnemyTargeting targeting;
    private EnemyNavigation navigation;
    private Animator animator;

    private Character attackTarget;
    private PlayerHealth attackTargetHealth;

    private SpawnManager spawnManager;
    private PowerUpManager powerupManager;

    private float[] speeds;
    private float currentSpeed;

    private bool isDead;
    private bool isAttacking;


    #region Unity Lifecycle

    private void Awake()
    {
        speeds = new float[]
        {
            walkingSpeed,
            runningSpeed,
            sprintingSpeed
        };

        animator = GetComponentInChildren<Animator>();
        targeting = GetComponent<EnemyTargeting>();
        navigation = GetComponent<EnemyNavigation>();
    }

    private void Update()
    {
        UpdateAI();
    }

    #endregion


    #region AI

    private void UpdateAI()
    {
        if (isDead || isAttacking)
        {
            return;
        }

        Character target = targeting.GetCurrentTarget();

        if (target == null)
        {
            navigation.ClearPath();
            return;
        }

        if (navigation.IsWithinHorizontalDistance(
            target.transform.position,
            behaviorData.GetAttackRange()))
        {
            StartAttack(target);
            return;
        }

        navigation.MoveTowards(target.transform.position);
    }


    private void StartAttack(Character target)
    {
        PlayerHealth targetHealth = target.GetComponent<PlayerHealth>();

        if (targetHealth == null || targetHealth.IsDead())
        {
            return;
        }

        attackTarget = target;
        attackTargetHealth = targetHealth;

        isAttacking = true;

        navigation.PauseMovement();
        navigation.FacePosition(attackTarget.transform.position);

        animator.SetTrigger("Attack");
    }


    public void AttackHit()
    {
        if (isDead || !isAttacking)
        {
            return;
        }

        if (attackTarget == null || attackTargetHealth == null)
        {
            return;
        }

        if (attackTargetHealth.IsDead())
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
        isAttacking = false;

        attackTarget = null;
        attackTargetHealth = null;

        navigation.ResumeMovement();
    }

    #endregion

    #region Death and Speed

    public void LoseHealth(float amount, PlayerPoints playerPoints)
    {
        if (isDead)
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
        if (isDead)
        {
            return;
        }

        isDead = true;
        isAttacking = false;

        attackTarget = null;
        attackTargetHealth = null;

        targeting.Shutdown();
        navigation.Shutdown();

        spawnManager?.EnemyDied();

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        animator.SetTrigger("Die");

        currentSpeed = 0f;
        GetComponent<Collider>().enabled = false;

        yield return new WaitForSeconds(20f);

        Destroy(gameObject);
    }

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

    public void RandomSpeedEarlyGame()
    {
        currentSpeed = speeds[Random.Range(0, 2)];
        
        if (currentSpeed == walkingSpeed)
        {
            Walkers();
        }
        else
        {
            Runners();
        }
    }

    public void RandomSpeedLateGame()
    {
        currentSpeed = speeds[Random.Range(1, 3)];

        if (currentSpeed == runningSpeed)
        {
            Runners();
        }
        else
        {
            Sprinters();
        }
    }

    public void DogSpeed()
    {
        currentSpeed = runningSpeed;
        navigation.SetMovementSpeed(currentSpeed);
    }

    public void SetSpawnManager(SpawnManager manager)
    {
        spawnManager = manager;
    }

    public void SetPowerUpManager(PowerUpManager manager)
    {
        powerupManager = manager;
    }

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
        if (round <= 3)
        {
            Walkers();
        }
        else if (round <= 10)
        {
            RandomSpeedEarlyGame();
        }
        else if (round <= 20)
        {
            Runners();
        }
        else
        {
            RandomSpeedLateGame();
        }
    }

    public float GetEnemySpeed()
    {
        return currentSpeed;
    }

    public float GetEnemyHealth()
    {
        return health;
    }

    public void InstaKill()
    {
        health = 1;
    }

    public void EndInstaKill()
    {
        if (!isDead)
        {
            SetHealthForRound(spawnManager.GetRoundNumber());
        }
    }

    public void Nuke()
    {
        if (!isDead)
        {
            BeginDeath();
        }
    }

    #endregion
}

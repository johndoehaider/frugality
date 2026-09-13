using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{

    [Header("Barrier Behavior")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float barrierAttackRange = 2f;

    [Header("Speeds")]
    [SerializeField] private float walkingSpeed = 1f;
    [SerializeField] private float runningSpeed = 3f;  
    [SerializeField] private float sprintingSpeed = 6f;

    [Header("Health & Attack")]
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private float damage = 33f;
    [SerializeField] private float health = 100f;

    private GameObject player;
    private PlayerHealth playerHealth;
    private PlayerPoints playerPoints;
    private Rigidbody enemyRb;
    private Animator animator;

    private float [] speeds;
    private float currentSpeed;

    private bool isDead = false;
    private float lastDamageTime = 0f;

    void Awake()
    {
        speeds = new float[] {walkingSpeed, runningSpeed, sprintingSpeed};
        animator = GetComponentInChildren<Animator>();    
    }
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerHealth = player.GetComponent<PlayerHealth>();
        playerPoints = player.GetComponent<PlayerPoints>();

        enemyRb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Move enemy towards player
        Vector3 moveDirection = (player.transform.position - transform.position).normalized;
        moveDirection.y = 0;
        enemyRb.MovePosition(enemyRb.position + moveDirection * currentSpeed * Time.deltaTime);
        if (!isDead)
        {
            enemyRb.rotation = Quaternion.LookRotation(moveDirection);
        }
    }

    void Update()
    {
        
    }

    void OnCollisionStay(Collision collision)
    {
        if (!isDead)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                if (Time.time - lastDamageTime >= attackCooldown)
                {
                    animator.SetTrigger("Attack");
                    playerHealth.LoseHealth(damage);
                    lastDamageTime = Time.time;
                }
            }
            else
            {
                Barrier barrier = collision.gameObject.GetComponentInParent<Barrier>();

                if (barrier != null && !barrier.IsBroken())
                {
                    if (Time.time - lastDamageTime >= attackCooldown)
                    {
                        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;

                        if (Physics.Raycast(transform.position, directionToPlayer, barrierAttackRange, playerLayer))
                        {
                            animator.SetTrigger("Attack");
                            playerHealth.LoseHealth(33f);
                        }
                        else
                        {
                            animator.SetTrigger("Attack");
                            barrier.RemoveBoard();
                        }

                        lastDamageTime = Time.time;
                    }
                }
            }
        }
    }

    public void LoseHealth (float amount)
    {
        if (!isDead)
        {
            health -= amount;
            if (health <= 0)
            {
                Debug.Log("Enemy dead!");
                playerPoints.AddPoints(50);
                isDead = true;
                StartCoroutine(DeathRoutine());
            }
            else
            {
                playerPoints.AddPoints(10);
            }
        }
    }

    IEnumerator DeathRoutine()
    {
        // start death anim, set speed to 0, then wait 2 seconds to destroy object
        animator.SetTrigger("Die");
        currentSpeed = 0f;
        GetComponent<Collider>().enabled = false;
        yield return new WaitForSeconds(5f);
        Destroy(gameObject);
    }

    void SetMovementAnimationRunner(bool isRunner)
    {
        animator.SetBool("IsWalker", !isRunner);
        animator.SetBool("IsRunner", isRunner);
    }

    public void Walkers()
    {
        currentSpeed = walkingSpeed;

        SetMovementAnimationRunner(false);
    }

    public void Runners()
    {
        currentSpeed = runningSpeed;

        SetMovementAnimationRunner(true);
    }

    public void Sprinters()
    {
        currentSpeed = sprintingSpeed;

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
        Runners();
    }

    public void DogSpeed()
    {
        currentSpeed = runningSpeed;
    }
}

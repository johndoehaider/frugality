using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healSpeed = 11f;
    [SerializeField] private float healDelay = 3f;

    private float currentHealth;
    private float lastDamageTime;
    private HUDManager hudManager;

    private bool isDead;

    private void Start()
    {
        currentHealth = maxHealth;

        hudManager = FindFirstObjectByType<HUDManager>();

        if (hudManager != null)
            hudManager.UpdateHealth(currentHealth);
    }

    private void Update()
    {
        HealPlayer();
    }

    // Removes health and resets the regeneration delay.
    public void LoseHealth(float amount)
    {
        currentHealth -= amount;
        lastDamageTime = Time.time;

        if (hudManager != null)
            hudManager.UpdateHealth(currentHealth);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            isDead = true;
            Debug.Log("Game Over!");
        }
    }

    // Regenerates health after the player has gone long enough without taking damage.
    private void HealPlayer()
    {
        if (!isDead)
        {
            if (currentHealth >= maxHealth)
                return;         

            if (Time.time - lastDamageTime < healDelay)
                return;

            currentHealth += healSpeed * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            if (hudManager != null)
                hudManager.UpdateHealth(currentHealth);
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public void ApplyJuggernog(float amount)
    {
        maxHealth = amount;
        currentHealth = maxHealth;

        if (hudManager != null)
            hudManager.UpdateHealth(currentHealth);
    }

    public bool IsDead()
    {
        return isDead;
    }
}
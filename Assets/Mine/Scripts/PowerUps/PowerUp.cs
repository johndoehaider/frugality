using UnityEngine;
using System.Collections;

public class PowerUp : MonoBehaviour
{
    [SerializeField] private PowerUpData powerUp;
    [SerializeField] private float lifeTime = 30f;
    [SerializeField] private float blinkStartTime = 20f;

    private Renderer[] renderers;

    private PowerUpManager powerUpManager;

    private void Start()
    {
        powerUpManager = FindFirstObjectByType<PowerUpManager>();

        renderers = GetComponentsInChildren<Renderer>();

        StartCoroutine(LifetimeRoutine());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Character playerCharacter = other.GetComponent<Character>();

        powerUpManager.ApplyPowerUpEffect(powerUp, playerCharacter);
        
        Destroy(gameObject);
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(blinkStartTime);

        float remainingTime = lifeTime - blinkStartTime;

        while (remainingTime > 0f)
        {
            SetVisible(false);

            float blinkInterval = Mathf.Lerp(0.05f, 0.4f, remainingTime / (lifeTime - blinkStartTime));

            yield return new WaitForSeconds(blinkInterval);

            SetVisible(true);

            yield return new WaitForSeconds(blinkInterval);

            remainingTime -= blinkInterval * 2f;
        }

        Destroy(gameObject);
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer powerUpRenderer in renderers)
        {
            powerUpRenderer.enabled = visible;
        }
    }
}
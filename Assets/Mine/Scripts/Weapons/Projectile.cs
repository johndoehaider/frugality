using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float destroyAfter = 10f;
    [SerializeField] private bool destroyOnImpact = true;
    [SerializeField] private float minDestroyTime = 0f;
    [SerializeField] private float maxDestroyTime = 0.2f;

    [Header("Impact Effects")]
    [SerializeField] private Transform[] bloodImpactPrefabs;
    [SerializeField] private Transform[] metalImpactPrefabs;
    [SerializeField] private Transform[] dirtImpactPrefabs;
    [SerializeField] private Transform[] concreteImpactPrefabs;

    // Starts a timer so the projectile cannot exist forever if it never hits anything.
    private void Start()
    {
        StartCoroutine(DestroyAfter());
    }

    // Handles what happens when the projectile collides with something.
    private void OnCollisionEnter(Collision collision)
    {
        // Ignore other projectiles.
        if (collision.gameObject.GetComponent<Projectile>() != null)
            return;

        //SpawnImpactEffect(collision);

        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(DestroyTimer());
        }
    }

    // // Chooses an impact effect based on the tag of the object that was hit.
    // private void SpawnImpactEffect(Collision collision)
    // {
    //     if (collision.contactCount == 0)
    //         return;

    //     Transform[] selectedPrefabs = null;

    //     if (collision.gameObject.CompareTag("Blood"))
    //         selectedPrefabs = bloodImpactPrefabs;
    //     else if (collision.gameObject.CompareTag("Metal"))
    //         selectedPrefabs = metalImpactPrefabs;
    //     else if (collision.gameObject.CompareTag("Dirt"))
    //         selectedPrefabs = dirtImpactPrefabs;
    //     else if (collision.gameObject.CompareTag("Concrete"))
    //         selectedPrefabs = concreteImpactPrefabs;

    //     if (selectedPrefabs == null || selectedPrefabs.Length == 0)
    //         return;

    //     Transform impactPrefab =
    //         selectedPrefabs[Random.Range(0, selectedPrefabs.Length)];

    //     if (impactPrefab == null)
    //         return;

    //     ContactPoint contact = collision.contacts[0];

    //     Instantiate(
    //         impactPrefab,
    //         contact.point,
    //         Quaternion.LookRotation(contact.normal)
    //     );
    // }

    // Waits a random amount of time after impact before destroying the projectile.
    private IEnumerator DestroyTimer()
    {
        float destroyDelay = Random.Range(minDestroyTime, maxDestroyTime);

        yield return new WaitForSeconds(destroyDelay);

        Destroy(gameObject);
    }

    // Destroys the projectile after a maximum lifetime even if it never hits anything.
    private IEnumerator DestroyAfter()
    {
        yield return new WaitForSeconds(destroyAfter);

        Destroy(gameObject);
    }
}

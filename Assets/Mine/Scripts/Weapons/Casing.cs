using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Casing : MonoBehaviour
{
    [Header("Ejection Force")]
    [SerializeField] private float minimumXForce = 25f;
    [SerializeField] private float maximumXForce = 40f;
    [SerializeField] private float minimumYForce = 10f;
    [SerializeField] private float maximumYForce = 20f;
    [SerializeField] private float minimumZForce = -12f;
    [SerializeField] private float maximumZForce = 12f;

    [Header("Rotation")]
    [SerializeField] private float minimumRotation = -360f;
    [SerializeField] private float maximumRotation = 360f;
    [SerializeField] private float spinSpeed = 2500f;

    [Header("Lifetime")]
    [SerializeField] private float despawnTime = 3f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] casingSounds;
    [SerializeField] private AudioSource audioSource;

    private Rigidbody rigidBody;

    // Caches the Rigidbody and gives the casing randomized launch force and torque.
    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();

        rigidBody.AddRelativeTorque(
            Random.Range(minimumRotation, maximumRotation),
            Random.Range(minimumRotation, maximumRotation),
            Random.Range(minimumRotation, maximumRotation),
            ForceMode.Impulse
        );

        rigidBody.AddRelativeForce(
            Random.Range(minimumXForce, maximumXForce),
            Random.Range(minimumYForce, maximumYForce),
            Random.Range(minimumZForce, maximumZForce),
            ForceMode.Impulse
        );
    }

    // Starts the casing sound and despawn timers.
    private void Start()
    {
        transform.rotation = Random.rotation;

        StartCoroutine(PlaySound());
        StartCoroutine(RemoveCasing());
    }

    // Keeps the casing visually spinning while it is alive.
    private void FixedUpdate()
    {
        transform.Rotate(Vector3.right, spinSpeed * Time.fixedDeltaTime);
        transform.Rotate(Vector3.down, spinSpeed * Time.fixedDeltaTime);
    }

    // Plays one random casing sound after a short randomized delay.
    private IEnumerator PlaySound()
    {
        if (audioSource == null || casingSounds == null || casingSounds.Length == 0)
            yield break;

        yield return new WaitForSeconds(Random.Range(0.25f, 0.85f));

        audioSource.clip = casingSounds[Random.Range(0, casingSounds.Length)];
        audioSource.Play();
    }

    // Destroys the casing after its configured lifetime.
    private IEnumerator RemoveCasing()
    {
        yield return new WaitForSeconds(despawnTime);
        Destroy(gameObject);
    }
}

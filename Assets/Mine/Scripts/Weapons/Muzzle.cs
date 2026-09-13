using System.Collections;
using UnityEngine;

public class Muzzle : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform socket;
    [SerializeField] private Sprite sprite;
    [SerializeField] private AudioClip audioClipFire;

    [Header("Particles")]
    [SerializeField] private GameObject prefabFlashParticles;
    [SerializeField] private int flashParticlesCount = 5;

    [Header("Flash Light")]
    [SerializeField] private GameObject prefabFlashLight;
    [SerializeField] private float flashLightDuration;
    [SerializeField] private Vector3 flashLightOffset;

    private ParticleSystem particles;
    private Light flashLight;

    // Creates the muzzle flash particle and light objects once so they can be reused every shot.
    private void Awake()
    {
        if (prefabFlashParticles != null && socket != null)
        {
            GameObject particleObject = Instantiate(prefabFlashParticles, socket);
            particleObject.transform.localPosition = Vector3.zero;
            particleObject.transform.localRotation = Quaternion.identity;

            particles = particleObject.GetComponent<ParticleSystem>();
        }

        if (prefabFlashLight != null && socket != null)
        {
            GameObject lightObject = Instantiate(prefabFlashLight, socket);
            lightObject.transform.localPosition = flashLightOffset;
            lightObject.transform.localRotation = Quaternion.identity;

            flashLight = lightObject.GetComponent<Light>();

            if (flashLight != null)
            {
                flashLight.enabled = false;
            }
        }
    }

    // Plays the muzzle flash particles and briefly turns on the muzzle flash light.
    public void Effect()
    {
        if (particles != null)
        {
            particles.Emit(flashParticlesCount);
        }

        if (flashLight != null)
        {
            flashLight.enabled = true;
            StartCoroutine(DisableLight());
        }
    }

    // Waits for the configured flash duration, then turns the light off again.
    private IEnumerator DisableLight()
    {
        yield return new WaitForSeconds(flashLightDuration);

        if (flashLight != null)
        {
            flashLight.enabled = false;
        }
    }

    public Transform GetSocket()
    {
        return socket;
    }

    public Sprite GetSprite()
    {
        return sprite;
    }

    public AudioClip GetAudioClipFire()
    {
        return audioClipFire;
    }

    public ParticleSystem GetParticlesFire()
    {
        return particles;
    }

    public int GetParticlesFireCount()
    {
        return flashParticlesCount;
    }

    public Light GetFlashLight()
    {
        return flashLight;
    }

    public float GetFlashLightDuration()
    {
        return flashLightDuration;
    }
}

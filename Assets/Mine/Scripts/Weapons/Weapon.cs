using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(WeaponAttachmentManager))]
public class Weapon : MonoBehaviour
{
    #region Inspector

    [Header("Weapon Info")]
    [SerializeField] private string weaponName;

    [Header("Firing")]
    [SerializeField] private bool automatic;
    [SerializeField] private float projectileImpulse = 400f;
    [SerializeField] private float roundsPerMinute = 200;
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float maximumDistance = 500f;
    [SerializeField] private float damage = 13f;

    [Header("Enemy Penetration")]
    [Min(1)]
    [SerializeField] private int maxEnemyHitsPerShot = 1;
    [Range(0f, 1f)]
    [SerializeField] private float enemyPenetrationDamageRetention = 1f;

    [Header("Animation")]
    [SerializeField] private Transform socketEjection;
    [SerializeField] private RuntimeAnimatorController handController;

    [Header("Prefabs")]
    [SerializeField] private GameObject casingPrefab;
    [SerializeField] private GameObject projectilePrefab;

    [Header("UI")]
    [SerializeField] private Sprite bodySprite;

    [Header("Audio")]
    [SerializeField] private AudioClip audioClipHolster;
    [SerializeField] private AudioClip audioClipUnholster;
    [SerializeField] private AudioClip audioClipReload;
    [SerializeField] private AudioClip audioClipReloadEmpty;
    [SerializeField] private AudioClip audioClipCantReload;
    [SerializeField] private AudioClip audioClipFireEmpty;

    [Header("Ammunition")]
    [SerializeField] private int maxReserveAmmo = 120;
    [SerializeField] private int startingReserveAmmo = 32;

    #endregion

    #region Runtime State

    private Animator animator;
    private WeaponAttachmentManager attachmentManager;

    private Magazine magazine;
    private Muzzle muzzle;

    private Character playerCharacter;
    private PlayerPoints playerPoints;
    private Transform playerCamera;

    private const int HitBufferSize = 64;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[HitBufferSize];
    private readonly HashSet<Enemy> enemiesHitThisShot = new HashSet<Enemy>();

    private int currentAmmoInClip;
    private int currentReserveAmmo;

    #endregion

    #region Unity Lifecycle

    // Gets the weapon's Animator, attachment manager, owning Character, and player camera.
    private void Awake()
    {
        animator = GetComponent<Animator>();
        attachmentManager = GetComponent<WeaponAttachmentManager>();
        playerCharacter = GetComponentInParent<Character>();
        playerPoints = GetComponentInParent<PlayerPoints>();

        if (playerCharacter != null)
        {
            Camera cameraWorld = playerCharacter.GetCameraWorld();

            if (cameraWorld != null)
            {
                playerCamera = cameraWorld.transform;
            }
        }
    }

    // Gets the equipped magazine and muzzle, then fills the magazine at startup.
    private void Start()
    {
        if (attachmentManager != null)
        {
            magazine = attachmentManager.GetEquippedMagazine();
            muzzle = attachmentManager.GetEquippedMuzzle();
        }

        if (magazine != null)
        {
            currentAmmoInClip = magazine.GetMaxAmmoInClip();
            currentReserveAmmo = startingReserveAmmo;
        }
    }

    #endregion

    #region Weapon Modifiers And Animation

    // Plays the correct weapon reload animation depending on whether the magazine still has ammo.
    public void ReloadAnimation()
    {
        if (HasAmmunition())
        {
            animator.Play("Reload", 0, 0f);
        }
        else
        {
            animator.Play("Reload Empty", 0, 0f);
        }
    }

    public void ApplySpeedCola(float speed)
    {
        animator.SetFloat("ReloadSpeed", speed);
    }

    public void ApplyDoubleTap(float dIncrease, float rIncrease)
    {
        damage *= dIncrease;
        roundsPerMinute *= rIncrease;
    }

    #endregion

    #region Firing

    // Fires one shot, resolves enemy penetration from the camera ray, and spawns the visual projectile from the muzzle.
    public void Fire(float spreadMultiplier = 1f)
    {
        if (muzzle != null && magazine != null && playerCamera != null && projectilePrefab != null)
        {
            Transform muzzleSocket = muzzle.GetSocket();

            if (muzzleSocket != null)
            {
                animator.Play("Fire", 0, 0f);

                currentAmmoInClip = Mathf.Clamp(currentAmmoInClip - 1, 0, magazine.GetMaxAmmoInClip());

                muzzle.Effect();

                Vector3 targetPoint = ResolveHitscan();

                Quaternion projectileRotation = Quaternion.LookRotation(targetPoint - muzzleSocket.position);

                GameObject projectile = Instantiate(projectilePrefab, muzzleSocket.position, projectileRotation);

                Rigidbody projectileRigidbody = projectile.GetComponent<Rigidbody>();

                if (projectileRigidbody != null)
                {
                    projectileRigidbody.linearVelocity = projectile.transform.forward * projectileImpulse;
                }
            }
        }
    }

    private Vector3 ResolveHitscan()
    {
        Vector3 rayOrigin = playerCamera.position;
        Vector3 rayDirection = playerCamera.forward;
        Vector3 targetPoint = rayOrigin + rayDirection * maximumDistance;

        enemiesHitThisShot.Clear();

        int hitCount = Physics.RaycastNonAlloc(rayOrigin, rayDirection, hitBuffer, maximumDistance, hitMask, QueryTriggerInteraction.Ignore);

        SortHitsByDistance(hitCount);

        int enemyHits = 0;
        float currentDamage = damage;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hitBuffer[i];
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy == null)
            {
                targetPoint = hit.point;
                break;
            }

            if (!enemiesHitThisShot.Add(enemy))
            {
                continue;
            }

            enemy.LoseHealth(currentDamage, playerPoints);
            enemyHits++;

            if (enemyHits >= maxEnemyHitsPerShot)
            {
                targetPoint = hit.point;
                break;
            }

            currentDamage *= enemyPenetrationDamageRetention;
        }

        return targetPoint;
    }

    private void SortHitsByDistance(int hitCount)
    {
        for (int i = 1; i < hitCount; i++)
        {
            RaycastHit hit = hitBuffer[i];
            int previousIndex = i - 1;

            while (previousIndex >= 0 && hitBuffer[previousIndex].distance > hit.distance)
            {
                hitBuffer[previousIndex + 1] = hitBuffer[previousIndex];
                previousIndex--;
            }

            hitBuffer[previousIndex + 1] = hit;
        }
    }

    #endregion

    #region Ammunition

    // Adds ammunition during a reload animation. Passing 0 fills the magazine completely.
    public void FillAmmunition()
    {
        if (magazine != null )
        {
            if (currentReserveAmmo > 0)
            {
                int ammoNeeded = magazine.GetMaxAmmoInClip() - currentAmmoInClip;

                if (ammoNeeded > 0)
                {
                    int ammoToAdd = Mathf.Min(ammoNeeded, currentReserveAmmo);

                    currentAmmoInClip += ammoToAdd;
                    currentReserveAmmo -= ammoToAdd;
                }
            }
        }
    }

    public void MaxAmmo()
    {
        currentReserveAmmo = maxReserveAmmo;
    }

    #endregion

    #region Effects

    // Spawns a casing at the weapon's ejection socket.
    public void EjectCasing()
    {
        if (casingPrefab != null && socketEjection != null)
        {
            Instantiate(casingPrefab, socketEjection.position, socketEjection.rotation);
        }
    }
    
    #endregion

    #region Getters

    public string GetWeaponName()
    {
        return weaponName;
    }

    // Returns the weapon Animator.
    public Animator GetAnimator()
    {
        return animator;
    }

    // Returns the weapon's HUD sprite.
    public Sprite GetSpriteBody()
    {
        return bodySprite;
    }

    // Returns the holster sound.
    public AudioClip GetAudioClipHolster()
    {
        return audioClipHolster;
    }

    // Returns the unholster sound.
    public AudioClip GetAudioClipUnholster()
    {
        return audioClipUnholster;
    }

    // Returns the normal reload sound.
    public AudioClip GetAudioClipReload()
    {
        return audioClipReload;
    }

    // Returns the empty reload sound.
    public AudioClip GetAudioClipReloadEmpty()
    {
        return audioClipReloadEmpty;
    }

    public AudioClip GetAudioClipCantReload()
    {
        return audioClipCantReload;
    }

    // Returns the empty-fire sound.
    public AudioClip GetAudioClipFireEmpty()
    {
        return audioClipFireEmpty;
    }

    // Returns the firing sound supplied by the equipped muzzle.
    public AudioClip GetAudioClipFire()
    {
        if (muzzle != null)
        {
            return muzzle.GetAudioClipFire();
        }
        else
        {
            return null;
        }
    }

    // Returns the amount of ammunition currently loaded.
    public int GetAmmunitionCurrent()
    {
        return currentAmmoInClip;
    }

    // Returns the amount of ammunition currently reserved.
    public int GetAmmunitionReserved()
    {
        return currentReserveAmmo;
    }   

    // Returns the max amount of reserved ammunition.
    public int GetAmmunitionReservedMax()
    {
        return maxReserveAmmo;
    }

    // Returns the maximum ammunition the equipped magazine can hold.
    public int GetMaxAmmoInClip()
    {
        if (magazine != null)
        {
            return magazine.GetMaxAmmoInClip();
        }
        else
        {
            return 0;
        }
    }

    // Returns whether holding fire should continuously shoot this weapon.
    public bool IsAutomatic()
    {
        return automatic;
    }

    // Returns this weapon's fire rate in rounds per minute.
    public float GetRateOfFire()
    {
        return roundsPerMinute;
    }

    // Returns whether the current magazine is completely full.
    public bool IsFull()
    {
        if (magazine != null && currentAmmoInClip == magazine.GetMaxAmmoInClip())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // Returns whether at least one round is currently loaded.
    public bool HasAmmunition()
    {
        if (currentAmmoInClip > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // Returns the character Animator Controller used while this weapon is equipped.
    public RuntimeAnimatorController GetAnimatorController()
    {
        return handController;
    }

    // Returns this weapon's attachment manager.
    public WeaponAttachmentManager GetAttachmentManager()
    {
        return attachmentManager;
    }

    #endregion
}

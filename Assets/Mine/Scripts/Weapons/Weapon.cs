using UnityEngine;

[RequireComponent(typeof(Animator), typeof(WeaponAttachmentManager))]
public class Weapon : MonoBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] private string weaponName;

    [Header("Firing")]
    [SerializeField] private bool automatic;
    [SerializeField] private float projectileImpulse = 400f;
    [SerializeField] private float roundsPerMinute = 200;
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float maximumDistance = 500f;
    [SerializeField] private float damage = 13f;

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

    private Animator animator;
    private WeaponAttachmentManager attachmentManager;

    private Magazine magazine;
    private Muzzle muzzle;

    private Character playerCharacter;
    private PlayerPoints playerPoints;
    private Transform playerCamera;

    private int currentAmmoInClip;
    private int currentReserveAmmo;

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

    // Fires one shot, damages what the camera is aiming at, and spawns the visual projectile from the muzzle.
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

                Vector3 targetPoint = playerCamera.position + playerCamera.forward * maximumDistance;

                if (Physics.Raycast(playerCamera.position, playerCamera.forward, out RaycastHit hit, maximumDistance, hitMask))
                {
                    targetPoint = hit.point;

                    Enemy enemy = hit.collider.GetComponent<Enemy>();

                    if (enemy != null)
                    {
                        enemy.LoseHealth(damage, playerPoints);
                    }
                }

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

    // Spawns a casing at the weapon's ejection socket.
    public void EjectCasing()
    {
        if (casingPrefab != null && socketEjection != null)
        {
            Instantiate(casingPrefab, socketEjection.position, socketEjection.rotation);
        }
    }
    
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
}

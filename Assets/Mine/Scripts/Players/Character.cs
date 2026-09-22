using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// RequireComponent makes Unity keep CharacterKinematics on the same GameObject as Character.
[RequireComponent(typeof(CharacterKinematics))]
public class Character : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private Inventory inventory;

    [Header("Camera")]
    [SerializeField] private Camera cameraWorld;

    [Header("Animation")]
    [SerializeField] private float dampTimeLocomotion = 0.15f;
    [SerializeField] private float dampTimeAiming = 0.3f;
    [SerializeField] private Animator characterAnimator;

    [Header("Audio")]
    [SerializeField] private AudioSource weaponAudioSource;
    [SerializeField] private AudioSource reloadAudioSource;

    [Header("Knife")]
    [SerializeField] private GameObject knifeObject;
    [SerializeField] private Transform realRightHandBone; 
    [SerializeField] private float knifeDamage = 150f;
    [SerializeField] private float knifeRange = 2f;
    [SerializeField] private float knifeRadius = 0.25f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Knife Audio")]
    [SerializeField] private AudioClip knifeWhoosh;
    [SerializeField] private AudioClip knifeSwing1;
    [SerializeField] private AudioClip knifeSwing2;
    [SerializeField] private AudioClip knifeKillSound;

    private bool aiming;
    private bool running;
    private bool holstered;
    private bool reloading;
    private bool inspecting;
    private bool holstering;
    private bool knifing;

    private bool aimButtonHeld;
    private bool runButtonHeld;
    private bool fireButtonHeld;

    private bool tutorialTextVisible;
    private bool cursorLocked;

    private float lastShotTime;

    private float reloadSpeed = 1f;
    private float damageMulti = 1f;
    private float rpmMulti = 1f;

    private int layerOverlay;
    private int layerHolster;
    private int layerActions;

    private Vector2 lookInput;
    private Vector2 movementInput;

    private CharacterKinematics characterKinematics;
    private PlayerPoints playerPoints;
    private Movement movement;
    private Weapon equippedWeapon;
    private Scope equippedWeaponScope;
    private Magazine equippedWeaponMagazine;
    private GameObject equippedWeaponClone;

    // Animator hashes let us refer to Animator parameters with an int instead of repeatedly using strings.
    private static readonly int HashAiming = Animator.StringToHash("Aiming");
    private static readonly int HashMovement = Animator.StringToHash("Movement");
    private static readonly int HashAim = Animator.StringToHash("Aim");
    private static readonly int HashRunning = Animator.StringToHash("Running");
    private static readonly int HashHolstered = Animator.StringToHash("Holstered");
    private static readonly int HashKnife = Animator.StringToHash("Knife");

    // Sets up the cursor, gets the IK component, initializes the inventory, and loads the starting weapon.
    private void Awake()
    {
        cursorLocked = true;
        UpdateCursorState();

        characterKinematics = GetComponent<CharacterKinematics>();
        playerPoints = GetComponent<PlayerPoints>();
        movement = GetComponent<Movement>();

        inventory.Init();
        RefreshWeaponSetup();
    }

    // Gets the Animator layer indexes once so we can use them later when playing animations.
    private void Start()
    {
        layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
        layerActions = characterAnimator.GetLayerIndex("Layer Actions");
        layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");
    }

    // Updates aiming, running, automatic fire, and Animator values every frame.
    private void Update()
    {
        UpdateAimingState();
        UpdateRunningState();
        HandleAutomaticFire();
        UpdateAnimator();
    }

    // Runs IK after the normal animation update so the hands can be corrected into their final weapon positions.
    private void LateUpdate()
    {
        if (!knifing && equippedWeapon != null && equippedWeaponScope != null)
        {
            characterKinematics.Compute();
        }
    }

    // Updates whether the player is currently allowed to aim.
    private void UpdateAimingState()
    {
        if (aimButtonHeld && CanAim())
        {
            aiming = true;
        }
        else
        {
            aiming = false;
        }
    }

    // Updates whether the player is currently allowed to run.
    private void UpdateRunningState()
    {
        if (runButtonHeld && CanRun())
        {
            running = true;
        }
        else
        {
            running = false;
        }
    }

    // Repeatedly fires an automatic weapon while the fire button is being held.
    private void HandleAutomaticFire()
    {
        if (fireButtonHeld && equippedWeapon != null)
        {
            if (CanPlayAnimationFire() && equippedWeapon.HasAmmunition() && equippedWeapon.IsAutomatic())
            {
                float timeBetweenShots = 60f / equippedWeapon.GetRateOfFire();

                if (Time.time - lastShotTime > timeBetweenShots)
                {
                    Fire();
                }
            }
        }
    }

    // Returns the world camera used by the player.
    public Camera GetCameraWorld()
    {
        return cameraWorld;
    }

    // Returns the player's inventory.
    public Inventory GetInventory()
    {
        return inventory;
    }

    // Returns the player's currently equipped weapon.
    public Weapon GetEquippedWeapon()
    {
        return equippedWeapon;
    }

    public float GetReloadSpeed()
    {
        return reloadSpeed;
    }

    public AudioSource GetWeaponAudioSource()
    {
        return weaponAudioSource;
    }

    public AudioSource GetReloadAudioSource()
    {
        return reloadAudioSource;
    }

    // Returns whether the crosshair should currently be visible.
    public bool IsCrosshairVisible()
    {
        if (!aiming && !holstered && !knifing)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // Returns whether the player is currently running.
    public bool IsRunning()
    {
        return running;
    }

    // Returns whether the player is currently aiming.
    public bool IsAiming()
    {
        return aiming;
    }

    // Returns whether the cursor is currently locked for gameplay.
    public bool IsCursorLocked()
    {
        return cursorLocked;
    }

    // Returns whether the tutorial UI should be shown.
    public bool IsTutorialTextVisible()
    {
        return tutorialTextVisible;
    }

    // Returns the latest movement input received from the Input System.
    public Vector2 GetInputMovement()
    {
        return movementInput;
    }

    // Returns the latest look input received from the Input System.
    public Vector2 GetInputLook()
    {
        return lookInput;
    }

    // Sends the player's current movement, aim, and running values to the Animator.
    private void UpdateAnimator()
    {
        float movementAmount = Mathf.Clamp01(Mathf.Abs(movementInput.x) + Mathf.Abs(movementInput.y));

        characterAnimator.SetFloat(HashMovement, movementAmount, dampTimeLocomotion, Time.deltaTime);

        float aimingValue;

        if (aiming)
        {
            aimingValue = 1f;
        }
        else
        {
            aimingValue = 0f;
        }

        characterAnimator.SetFloat(HashAiming, aimingValue, 0.25f * dampTimeAiming, Time.deltaTime);
        characterAnimator.SetBool(HashAim, aiming);
        characterAnimator.SetBool(HashRunning, running);
    }


    // Starts the weapon inspect animation and marks the character as inspecting.
    private void Inspect()
    {
        inspecting = true;
        characterAnimator.CrossFade("Inspect", 0f, layerActions, 0f);
    }

    // Fires the equipped weapon, records the shot time, and plays the character fire animation.
    private void Fire()
    {
        if (equippedWeapon != null)
        {
            lastShotTime = Time.time;
            equippedWeapon.Fire();
            characterAnimator.CrossFade("Fire", 0.05f, layerOverlay, 0f);
        }
    }

    private void Knife()
    {
        knifing = true;
        aiming = false;
        
        CancelReloadAnimation(); 

        // 1. Hide the real weapon so it doesn't float in your face
        if (equippedWeapon != null)
        {
            equippedWeapon.gameObject.SetActive(false);

            // 2. Clone the equipped weapon and glue it directly to the true right hand bone
            if (realRightHandBone != null)
            {
                equippedWeaponClone = Instantiate(equippedWeapon.gameObject, realRightHandBone);
                equippedWeaponClone.name = equippedWeapon.gameObject.name;

                equippedWeaponClone.SetActive(true);

                equippedWeaponClone.transform.localPosition = new Vector3(0.1517f, -0.0533f, -0.0232f);
                equippedWeaponClone.transform.localRotation = Quaternion.Euler(16.122f, 113.661f, 103.924f);
                equippedWeaponClone.transform.localScale = Vector3.one;
                
                SetLayerRecursively(equippedWeaponClone, 31); 
                if (equippedWeaponClone.TryGetComponent(out Weapon weaponScript)) Destroy(weaponScript);
                if (equippedWeaponClone.TryGetComponent(out WeaponAttachmentManager attachManager)) Destroy(attachManager);
                if (equippedWeaponClone.TryGetComponent(out WeaponAnimationEventHandler eventHandler)) Destroy(eventHandler);
                if (equippedWeaponClone.TryGetComponent(out Animator cloneAnimator)) Destroy(cloneAnimator);
            }
        }

        knifeObject.SetActive(true);
        characterAnimator.SetTrigger(HashKnife);
    }

    public void KnifeHit()
    {
        if (Physics.SphereCast(
            cameraWorld.transform.position, knifeRadius, cameraWorld.transform.forward,
            out RaycastHit hit, knifeRange, enemyLayer, QueryTriggerInteraction.Ignore))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                bool killingBlow = enemy.GetEnemyHealth() <= knifeDamage;
                enemy.LoseHealth(knifeDamage, playerPoints);

                if (killingBlow)
                {
                    weaponAudioSource.PlayOneShot(knifeKillSound);
                    playerPoints.AddPoints(80);
                }
                else
                {
                    AudioClip slashSound;

                    if (Random.Range(0, 2) == 0)
                    {
                        slashSound = knifeSwing1;
                    }
                    else
                    {
                        slashSound = knifeSwing2;
                    }

                    weaponAudioSource.PlayOneShot(slashSound);
                }

                return;
            }
        }
        weaponAudioSource.PlayOneShot(knifeWhoosh);
    }

    public void AnimationEndedKnife()
    {
        knifeObject.SetActive(false);

        if (equippedWeaponClone != null)
        {
            Destroy(equippedWeaponClone);
        }
        if (equippedWeapon != null)
        {
            equippedWeapon.gameObject.SetActive(true);
        }

        knifing = false;

        UpdateAimingState(); 
        UpdateAnimator();

    }

    // Chooses the correct reload animation, marks the player as reloading, and tells the weapon to reload.
    private void PlayReloadAnimation()
    {
        if (equippedWeapon != null)
        {
            string stateName;

            if (equippedWeapon.GetAmmunitionReserved() > 0 && !equippedWeapon.IsFull())
            {
                if (equippedWeapon.HasAmmunition())
                {
                    stateName = "Reload";
                }
                else
                {
                    stateName = "Reload Empty";
                }       

                characterAnimator.Play(stateName, layerActions, 0f);
                reloading = true;
                equippedWeapon.ReloadAnimation();
            }
            
            // Play can't reload sound effect
            else if (equippedWeapon.GetAmmunitionReserved() <= 0)
            {
                AudioClip cantReloadSound = equippedWeapon.GetAudioClipCantReload();

                if (cantReloadSound != null)
                {
                    weaponAudioSource.PlayOneShot(cantReloadSound);
                }
            }
        }
    }

    private void CancelReloadAnimation()
    {
        reloading = false;
        characterAnimator.Play("Default", layerActions, 0f);

        if (reloadAudioSource != null)
        {
            reloadAudioSource.Stop();
        }
    }

    public void ApplySpeedCola(float speed)
    {
        reloadSpeed = speed;
        characterAnimator.SetFloat("ReloadSpeed", speed);

        if (equippedWeapon != null)
        {
            equippedWeapon.ApplySpeedCola(reloadSpeed);
        }
    }

    public void ApplyDoubleTap(float dIncrease, float rIncrease)
    {
        damageMulti = dIncrease;
        rpmMulti = rIncrease;
        if (equippedWeapon != null)
        {
            equippedWeapon.ApplyDoubleTap(damageMulti, rpmMulti);
        }
    }

    // A coroutine lets the weapon switch wait until the holster animation finishes before equipping the new weapon.
    private IEnumerator Equip(int newWeaponIndex)
    {
        if (!holstered)
        {
            holstering = true;
            SetHolstered(true);

            // WaitUntil pauses this coroutine until holstering becomes false.
            yield return new WaitUntil(() => holstering == false);
        }

        SetHolstered(false);
        characterAnimator.Play("Unholster", layerHolster, 0f);

        inventory.Equip(newWeaponIndex);
        RefreshWeaponSetup();
    }

    // Updates Character's weapon-related references after the equipped weapon changes.
    private void RefreshWeaponSetup()
    {
        equippedWeapon = inventory.GetEquipped();
        equippedWeapon.ApplySpeedCola(reloadSpeed);
        equippedWeapon.ApplyDoubleTap(damageMulti, rpmMulti);

        if (equippedWeapon != null)
        {
            characterAnimator.runtimeAnimatorController = equippedWeapon.GetAnimatorController();

            WeaponAttachmentManager attachmentManager = equippedWeapon.GetAttachmentManager();

            if (attachmentManager != null)
            {
                equippedWeaponScope = attachmentManager.GetEquippedScope();
                equippedWeaponMagazine = attachmentManager.GetEquippedMagazine();
            }
        }
    }

    // Plays the empty-trigger animation and still applies the weapon's fire-rate delay.
    private void FireEmpty()
    {
        lastShotTime = Time.time;
        characterAnimator.CrossFade("Fire Empty", 0.05f, layerOverlay, 0f);
    }

    // Applies the current cursor lock state to Unity's cursor.
    private void UpdateCursorState()
    {
        if (cursorLocked)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // Changes the holstered state and sends the same value to the Animator.
    private void SetHolstered(bool value)
    {
        holstered = value;
        characterAnimator.SetBool(HashHolstered, holstered);
    }

    // Returns whether firing is allowed in the player's current state.
    private bool CanPlayAnimationFire()
    {
        if (holstered || holstering || reloading || inspecting || knifing)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    // Returns whether reloading is allowed in the player's current state.
    private bool CanPlayAnimationReload()
    {
        if (reloading || inspecting || knifing)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    // Returns whether holstering is allowed in the player's current state.
    private bool CanPlayAnimationHolster()
    {
        if (reloading || inspecting || knifing)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    // Returns whether changing weapons is allowed in the player's current state.
    private bool CanChangeWeapon()
    {
        if (holstering || reloading || inspecting || knifing)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    // Returns whether inspecting is allowed in the player's current state.
    private bool CanPlayAnimationInspect()
    {
        if (holstered || holstering || reloading || inspecting || knifing)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    // Returns whether aiming is allowed in the player's current state.
    private bool CanAim()
    {
        if (holstered || inspecting || reloading || holstering || knifing)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    // Returns whether running is allowed based on the player's current actions and movement direction.
    private bool CanRun()
    {
        bool canRun = true;

        if (inspecting || reloading || aiming || knifing)
        {
            canRun = false;
        }

        if (fireButtonHeld && equippedWeapon != null)
        {
            if (equippedWeapon.HasAmmunition())
            {
                canRun = false;
            }
        }

        if (!movement.IsGrounded())
        {
            canRun = false;
        }

        if (movementInput.y <= 0f)
        {
            canRun = false;
        }

        if (Mathf.Abs(Mathf.Abs(movementInput.x) - 1f) < 0.01f)
        {
            canRun = false;
        }

        return canRun;
    }

    private bool CanKnife()
    {
        if (knifing || holstered || holstering || inspecting)
        {
            return false;
        }

        return true;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        
        obj.layer = newLayer; // Changes the current object's layer
        
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer); // Changes the child's layer too
        }
    }

    // InputAction.CallbackContext tells us what phase of an Input System action just happened.
    // Handles pressing, releasing, and firing a semi-automatic weapon.
    public void OnTryFire(InputAction.CallbackContext context)
    {
        if (cursorLocked)
        {
            if (context.started)
            {
                fireButtonHeld = true;
            }
            else if (context.canceled)
            {
                fireButtonHeld = false;
            }
            else if (context.performed && CanPlayAnimationFire() && equippedWeapon != null)
            {
                if (!equippedWeapon.HasAmmunition())
                {
                    FireEmpty();
                }
                else if (!equippedWeapon.IsAutomatic())
                {
                    float timeBetweenShots = 60f / equippedWeapon.GetRateOfFire();

                    if (Time.time - lastShotTime > timeBetweenShots)
                    {
                        Fire();
                    }
                }
            }
        }
    }

    public void OnTryKnife(InputAction.CallbackContext context)
    {
        if (cursorLocked && context.performed)
        {
            if (CanKnife())
            {
                Knife();
            }
        }
    }

    // Starts a reload when the reload input is performed and the player is allowed to reload.
    public void OnTryPlayReload(InputAction.CallbackContext context)
    {
        if (cursorLocked && context.performed)
        {
            if (CanPlayAnimationReload())
            {
                PlayReloadAnimation();
            }
        }
    }

    public void OnTryJump(InputAction.CallbackContext context)
    {
        if (cursorLocked && context.performed)
        {
            movement.Jump();
        }
    }

    // Starts the inspect animation when the inspect input is performed and allowed.
    public void OnTryInspect(InputAction.CallbackContext context)
    {
        if (cursorLocked && context.performed)
        {
            if (CanPlayAnimationInspect())
            {
                Inspect();
            }
        }
    }

    // Tracks whether the aim button is currently being held.
    public void OnTryAiming(InputAction.CallbackContext context)
    {
        if (cursorLocked)
        {
            if (context.started)
            {
                aimButtonHeld = true;
            }
            else if (context.canceled)
            {
                aimButtonHeld = false;
            }
        }
    }

    // Toggles the weapon holster state when the holster input is performed.
    public void OnTryHolster(InputAction.CallbackContext context)
    {
        if (cursorLocked && context.performed)
        {
            if (CanPlayAnimationHolster())
            {
                SetHolstered(!holstered);
                holstering = true;
            }
        }
    }

    // Tracks whether the run button is currently being held.
    public void OnTryRun(InputAction.CallbackContext context)
    {
        if (cursorLocked)
        {
            if (context.started)
            {
                runButtonHeld = true;
            }
            else if (context.canceled)
            {
                runButtonHeld = false;
            }
        }
    }

    // Chooses the next or previous inventory slot and starts the weapon-switch coroutine.
    public void OnTryInventoryNext(InputAction.CallbackContext context)
    {
        if (cursorLocked && context.performed && inventory != null)
        {
            float scrollDirection = 1f;

            // This action can come from a button or a mouse wheel, so Vector2 input is checked separately.
            if (context.valueType.IsEquivalentTo(typeof(Vector2)))
            {
                scrollDirection = Mathf.Sign(context.ReadValue<Vector2>().y);
            }

            int nextIndex;

            if (scrollDirection > 0f)
            {
                nextIndex = inventory.GetNextIndex();
            }
            else
            {
                nextIndex = inventory.GetLastIndex();
            }

            int currentIndex = inventory.GetEquippedIndex();

            if (CanChangeWeapon() && currentIndex != nextIndex)
            {
                StartCoroutine(Equip(nextIndex));
            }
        }
    }

    // Locks or unlocks the cursor when the cursor input is performed.
    public void OnLockCursor(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            cursorLocked = !cursorLocked;
            UpdateCursorState();
        }
    }

    // Saves movement input so Movement.cs can use it.
    public void OnMove(InputAction.CallbackContext context)
    {
        if (cursorLocked)
        {
            movementInput = context.ReadValue<Vector2>();
        }
        else
        {
            movementInput = Vector2.zero;
        }
    }

    // Saves look input so CameraLook.cs can use it.
    public void OnLook(InputAction.CallbackContext context)
    {
        if (cursorLocked)
        {
            lookInput = context.ReadValue<Vector2>();
        }
        else
        {
            lookInput = Vector2.zero;
        }
    }

    // Shows or hides the tutorial UI while the tutorial input is held.
    public void OnUpdateTutorial(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            tutorialTextVisible = true;
        }
        else if (context.canceled)
        {
            tutorialTextVisible = false;
        }
    }

    // Called by an animation event to tell the equipped weapon to eject a casing.
    public void EjectCasing()
    {
        if (equippedWeapon != null)
        {
            equippedWeapon.EjectCasing();
        }
    }

    // Called by an animation event to transfer ammunition into the equipped weapon.
    public void FillAmmunition()
    {
        if (equippedWeapon != null)
        {
            equippedWeapon.FillAmmunition();
        }
    }

    public void MaxAmmo()
    {
        if (equippedWeapon != null)
        {
            equippedWeapon.MaxAmmo();
        }
    }

    // Called by an animation event to show or hide the equipped weapon's magazine object.
    public void SetActiveMagazine(int active)
    {
        if (equippedWeaponMagazine != null)
        {
            if (active != 0)
            {
                equippedWeaponMagazine.gameObject.SetActive(true);
            }
            else
            {
                equippedWeaponMagazine.gameObject.SetActive(false);
            }
        }
    }

    // Called by an animation event when the reload animation finishes.
    public void AnimationEndedReload()
    {
        reloading = false;
    }

    // Called by an animation event when the inspect animation finishes.
    public void AnimationEndedInspect()
    {
        inspecting = false;
    }

    // Called by an animation event when the holster animation finishes.
    public void AnimationEndedHolster()
    {
        holstering = false;
    }
}

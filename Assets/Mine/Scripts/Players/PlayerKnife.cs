using UnityEngine;

public class PlayerKnife : MonoBehaviour
{
    [Header("Knife")]
    [SerializeField] private GameObject knifeObject;
    [SerializeField] private Transform realRightHandBone;
    [SerializeField] private float knifeDamage = 150f;
    [SerializeField] private float knifeRange = 2f;
    [SerializeField] private float knifeRadius = 0.25f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Audio")]
    [SerializeField] private AudioClip knifeWhoosh;
    [SerializeField] private AudioClip knifeSwing1;
    [SerializeField] private AudioClip knifeSwing2;
    [SerializeField] private AudioClip knifeKillSound;

    private Character character;
    private PlayerPoints playerPoints;

    private GameObject equippedWeaponClone;

    private void Awake()
    {
        character = GetComponent<Character>();
        playerPoints = GetComponent<PlayerPoints>();
    }

    public void StartKnife(Weapon equippedWeapon)
    {
        if (equippedWeapon != null)
        {
            equippedWeapon.gameObject.SetActive(false);

            if (realRightHandBone != null)
            {
                equippedWeaponClone = Instantiate(equippedWeapon.gameObject, realRightHandBone);
                equippedWeaponClone.name = equippedWeapon.gameObject.name;

                equippedWeaponClone.SetActive(true);

                equippedWeaponClone.transform.localPosition = new Vector3(0.1517f, -0.0533f, -0.0232f);
                equippedWeaponClone.transform.localRotation = Quaternion.Euler(16.122f, 113.661f, 103.924f);
                equippedWeaponClone.transform.localScale = Vector3.one;

                SetLayerRecursively(equippedWeaponClone, 31);

                if (equippedWeaponClone.TryGetComponent(out Weapon weaponScript))
                {
                    Destroy(weaponScript);
                }

                if (equippedWeaponClone.TryGetComponent(out WeaponAttachmentManager attachmentManager))
                {
                    Destroy(attachmentManager);
                }

                if (equippedWeaponClone.TryGetComponent(out WeaponAnimationEventHandler eventHandler))
                {
                    Destroy(eventHandler);
                }

                if (equippedWeaponClone.TryGetComponent(out Animator cloneAnimator))
                {
                    Destroy(cloneAnimator);
                }
            }
        }

        knifeObject.SetActive(true);
    }

    public void KnifeHit(Camera cameraWorld)
    {
        if (Physics.SphereCast(
            cameraWorld.transform.position,
            knifeRadius,
            cameraWorld.transform.forward,
            out RaycastHit hit,
            knifeRange,
            enemyLayer,
            QueryTriggerInteraction.Ignore))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                bool killingBlow = enemy.GetEnemyHealth() <= knifeDamage;

                enemy.LoseHealth(knifeDamage, playerPoints);

                if (killingBlow)
                {
                    character.GetWeaponAudioSource().PlayOneShot(knifeKillSound);
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

                    character.GetWeaponAudioSource().PlayOneShot(slashSound);
                }

                return;
            }
        }

        character.GetWeaponAudioSource().PlayOneShot(knifeWhoosh);
    }

    public void EndKnife(Weapon equippedWeapon)
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
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null)
        {
            return;
        }

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
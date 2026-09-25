using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class HUDManager : MonoBehaviour
{

    private Dictionary<GameObject, Coroutine> powerUpBlinkCoroutines
    = new Dictionary<GameObject, Coroutine>();
    
    [Header("Perks")]
    [SerializeField] private Transform perkContainer;
    [SerializeField] private GameObject perkIconPrefab;

    [Header("PowerUps")]
    [SerializeField] private Transform powerUpContainerLower;
    [SerializeField] private Transform powerUpContainerUpper;
    [SerializeField] private GameObject powerUpUpperIconPrefab;
    [SerializeField] private GameObject powerUpLowerIconPrefab;

    [Header("Crosshair")]
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private float crosshairTransitionSpeed = 10f;

    [Header("Settings")]
    [SerializeField] private float promptDistance = 2f;

    public TextMeshProUGUI pointsText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI interactText;
    public TextMeshProUGUI roundsText;

    private GameObject player;
    private Character playerCharacter;
    private PlayerPoints playerPoints;
    private PlayerInteraction playerInteraction;
    private Weapon equippedWeapon;
    private Camera mainCamera;
    private Vector3 targetScale;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerCharacter = player.GetComponent<Character>();
        mainCamera = player.GetComponentInChildren<Camera>();
        playerInteraction = player.GetComponent<PlayerInteraction>();
        playerPoints = player.GetComponent<PlayerPoints>();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        equippedWeapon = playerCharacter.GetEquippedWeapon();

        if (equippedWeapon != null)
        {
            UpdateAmmo(equippedWeapon.GetAmmunitionCurrent(), equippedWeapon.GetAmmunitionReserved());
        }

        UpdateCrosshair();
        UpdateInteractText();
    }

    public void UpdatePoints(int amount)
    {
        pointsText.text = "" + amount;
    }

    public void UpdateHealth(float amount)
    {
        healthText.text = "Health: " + Mathf.Floor(amount);
    }

    public void UpdateAmmo(int current, int reserve)
    {
        if (current < 10)
        {
            ammoText.text = "0" + current + "/" + reserve;
        }
        
        if (reserve < 10)
        {
            ammoText.text = "" + current + "/0" + reserve;
        }

        if (current < 10 && reserve < 10)
        {
            ammoText.text = "0" + current + "/0" + reserve;
        }

        if (current >= 10 && reserve >= 10)
        {
            ammoText.text = "" + current + "/" + reserve;
        }
    }

    public void UpdateRoundsText(int round)
    {
        roundsText.text = round.ToString();
    }

    public void UpdateInteractText()
    {
        RaycastHit hit;
        if (!playerInteraction.GetProximity())
        {
            if (Physics.SphereCast(mainCamera.transform.position, 0.25f, mainCamera.transform.forward, out hit, promptDistance))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    interactText.text = interactable.GetInteractionText(playerInteraction);
                }
                else
                {
                    interactText.text = "";
                }
            }
            else
            {
                interactText.text = "";
            }
        }
        else
        {
            interactText.text = playerInteraction.GetProximityInteractable().GetInteractionText(playerInteraction);
        }
    }

    public void UpdateCrosshair()
    {
        if (playerCharacter.IsCrosshairVisible())
        {
            targetScale = Vector3.one;
        }
        else
        {
            targetScale = Vector3.zero;
        }
        crosshair.localScale = 
        Vector3.Lerp(crosshair.localScale, targetScale, crosshairTransitionSpeed * Time.deltaTime);

        Color crosshairColor = crosshairImage.color;
        crosshairColor.a = Mathf.InverseLerp(0f, 0.5f, crosshair.localScale.x);

        crosshairImage.color = crosshairColor;
    }

    public void UpdatePerkIcon(Sprite icon)
    {
        GameObject newIcon = Instantiate(perkIconPrefab, perkContainer);

        Image image = newIcon.GetComponent<Image>();

        if (image != null)
        {
            image.sprite = icon;
        }
    }

    // All PowerUp Methods

    public GameObject CreatePowerUpIconLower(Sprite icon)
    {
        GameObject newIcon = Instantiate(powerUpLowerIconPrefab, powerUpContainerLower);

        Image image = newIcon.GetComponent<Image>();

        if (image != null)
        {
            image.sprite = icon;
        }

        return newIcon;
    }

    public void CreatePowerUpIconUpper(Sprite icon)
    {
        GameObject newIcon = Instantiate(powerUpUpperIconPrefab, powerUpContainerUpper);

        Image image = newIcon.GetComponent<Image>();

        if (image != null)
        {
            image.sprite = icon;
        }

        StartCoroutine(UpperPowerUpIconRoutine(newIcon));
    }

    public void RemovePowerUpIcon(GameObject icon)
    {
        if (icon == null)
        {
            return;
        }

        StopBlinkingPowerUpIcon(icon);
        Destroy(icon);
    }

    public void BlinkPowerUpIcon(GameObject icon)
    {
        if (powerUpBlinkCoroutines.ContainsKey(icon))
        {
            StopCoroutine(powerUpBlinkCoroutines[icon]);
        }

        powerUpBlinkCoroutines[icon] = StartCoroutine(BlinkPowerUpIconRoutine(icon));
    }

    public void StopBlinkingPowerUpIcon(GameObject icon)
    {
        if (icon == null)
        {
            return;
        }

        if (powerUpBlinkCoroutines.TryGetValue(icon, out Coroutine blinkCoroutine))
        {
            StopCoroutine(blinkCoroutine);
            powerUpBlinkCoroutines.Remove(icon);
        }

        Image image = icon.GetComponent<Image>();

        if (image != null)
        {
            image.enabled = true;
        }
    }
    
    private IEnumerator UpperPowerUpIconRoutine(GameObject icon)
    {
        CanvasGroup canvasGroup = icon.GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;

        // Fade in
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * 2f;
            yield return null;
        }

        canvasGroup.alpha = 1f;

        // Stay fully visible for 3 seconds
        yield return new WaitForSeconds(3f);

        // Fade out
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * 2f;
            yield return null;
        }

        Destroy(icon);
    }

    private IEnumerator BlinkPowerUpIconRoutine(GameObject icon)
    {
        Image image = icon.GetComponent<Image>();

        float remainingTime = 10f;

        while (remainingTime > 0f && icon != null)
        {
            image.enabled = false;

            float blinkInterval = Mathf.Lerp(0.05f, 0.4f, remainingTime / 10f);

            yield return new WaitForSeconds(blinkInterval);

            image.enabled = true;

            yield return new WaitForSeconds(blinkInterval);

            remainingTime -= blinkInterval * 2f;
        }
    }
}

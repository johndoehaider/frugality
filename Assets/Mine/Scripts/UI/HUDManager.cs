using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private Transform perkContainer;
    [SerializeField] private GameObject perkIconPrefab;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerCharacter = player.GetComponent<Character>();
        mainCamera = player.GetComponentInChildren<Camera>();
        playerInteraction = player.GetComponent<PlayerInteraction>();
        playerPoints = player.GetComponent<PlayerPoints>();
    }

    // Update is called once per frame
    void Update()
    {
        equippedWeapon = playerCharacter.GetEquippedWeapon();

        if (equippedWeapon != null)
        {
            UpdateAmmo(equippedWeapon.GetAmmunitionCurrent(), equippedWeapon.GetAmmunitionReserved());
        }

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

    public void UpdatePerkIcon(Sprite icon)
    {
        GameObject newIcon = Instantiate(perkIconPrefab, perkContainer);

        Image image = newIcon.GetComponent<Image>();

        if (image != null)
        {
            image.sprite = icon;
        }
    }

    public void UpdateInteractText()
    {
        RaycastHit hit;
        if (!playerInteraction.GetProximity())
        {
            if (Physics.SphereCast(mainCamera.transform.position, 0.25f, mainCamera.transform.forward, out hit, 2f))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    interactText.text = interactable.GetInteractionText();
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
            interactText.text = playerInteraction.GetProximityInteractable().GetInteractionText();
        }
    }
}

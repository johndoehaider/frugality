using UnityEngine;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    [Header("Power Up Prefabs")]
    [SerializeField] private GameObject maxAmmoPrefab;
    [SerializeField] private GameObject nukePrefab;
    [SerializeField] private GameObject instaKillPrefab;
    [SerializeField] private GameObject doublePointsPrefab;

    [Header("Drop Settings")]
    [SerializeField] private float startingDropThreshold = 2000f;
    [SerializeField] private float thresholdMultiplier = 1.14f;
    [SerializeField] private float randomDropChance = 0.02f;
    [SerializeField] private int maxDropsPerRound = 4;
    [SerializeField] private float yOffset = 1.2f;

    private List<GameObject> powerUpCycle = new List<GameObject>();
    private SpawnManager spawnManager;
    private HUDManager hudManager;
    private GameObject instaKillIcon;
    private GameObject doublePointsIcon;

    private float dropProgress;
    private float currentDropThreshold;

    private int cycleIndex;
    private int dropsThisRound;

    private void OnEnable()
    {
        PlayerPoints.OnPointsEarned += AddDropProgress;
        PlayerPoints.OnDoublePointsExpiring += BlinkDoublePointsIcon;
        SpawnManager.OnInstaKillExpiring += BlinkInstaKillIcon;
        PlayerPoints.OnDoublePointsEnded += RemoveDoublePointsIcon;
        SpawnManager.OnInstaKillEnded += RemoveInstaKillIcon;
    }

    private void OnDisable()
    {
        PlayerPoints.OnPointsEarned -= AddDropProgress;
        PlayerPoints.OnDoublePointsExpiring -= BlinkDoublePointsIcon;
        SpawnManager.OnInstaKillExpiring -= BlinkInstaKillIcon;
        PlayerPoints.OnDoublePointsEnded -= RemoveDoublePointsIcon;
        SpawnManager.OnInstaKillEnded -= RemoveInstaKillIcon;
    }

    private void Awake()
    {
        spawnManager = FindFirstObjectByType<SpawnManager>();
        hudManager = FindFirstObjectByType<HUDManager>();
    }

    private void Start()
    {
        currentDropThreshold = startingDropThreshold;

        CreateNewCycle();
    }

    private void AddDropProgress(PlayerPoints player, int amount)
    {
        dropProgress += amount;
    }

    private void CreateNewCycle()
    {
        powerUpCycle.Clear();

        powerUpCycle.Add(maxAmmoPrefab);
        powerUpCycle.Add(nukePrefab);
        powerUpCycle.Add(instaKillPrefab);
        powerUpCycle.Add(doublePointsPrefab);

        for (int i = powerUpCycle.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            GameObject temporaryPowerUp = powerUpCycle[i];
            powerUpCycle[i] = powerUpCycle[randomIndex];
            powerUpCycle[randomIndex] = temporaryPowerUp;
        }

        cycleIndex = 0;
    }

    public void TryDropPowerUp(Vector3 position)
    {
        if (dropsThisRound >= maxDropsPerRound)
        {
            return;
        }

        bool reachedThreshold = dropProgress >= currentDropThreshold;
        bool randomDrop = Random.value < randomDropChance;

        if (!reachedThreshold && !randomDrop)
        {
            return;
        }

        Vector3 spawnPos = new Vector3(position.x, yOffset, position.z);
        SpawnPowerUp(spawnPos);

        dropsThisRound++;

        if (reachedThreshold)
        {
            dropProgress -= currentDropThreshold;
            currentDropThreshold *= thresholdMultiplier;
        }
    }

    private void SpawnPowerUp(Vector3 position)
    {
        if (cycleIndex >= powerUpCycle.Count)
        {
            CreateNewCycle();
        }

        GameObject powerUpToSpawn = powerUpCycle[cycleIndex];

        Instantiate(powerUpToSpawn, position, Quaternion.identity);

        cycleIndex++;
    }

    public void ResetDropCountOfRound()
    {
        dropsThisRound = 0;
    }

    public void ApplyPowerUpEffect(PowerUpData powerUp, Character playerCharacter)
    {
        PlayerPoints playerPoints = playerCharacter.GetComponent<PlayerPoints>();

        if (powerUp.powerUpType == PowerUpType.MaxAmmo)
        {
            hudManager.CreatePowerUpIconUpper(powerUp.icon);
            playerCharacter.MaxAmmo();
        }

        if (powerUp.powerUpType == PowerUpType.InstaKill)
        {
            if (instaKillIcon == null)
            {
                instaKillIcon = hudManager.CreatePowerUpIconLower(powerUp.icon);
            }
            else
            {
                hudManager.StopBlinkingPowerUpIcon(instaKillIcon);
            }
            spawnManager.InstaKill();
        }

        if (powerUp.powerUpType == PowerUpType.DoublePoints)
        {
            if (doublePointsIcon == null)
            {
                doublePointsIcon = hudManager.CreatePowerUpIconLower(powerUp.icon);
            }
            else
            {
                hudManager.StopBlinkingPowerUpIcon(doublePointsIcon);
            }
            playerPoints.DoublePoints();
        }

        if (powerUp.powerUpType == PowerUpType.Nuke)
        {
            spawnManager.Nuke();
            playerPoints.AddPoints(400);
        }

        if (powerUp.powerUpType == PowerUpType.Carpenter)
        {
            
        }
        
    }

    private void RemoveInstaKillIcon()
    {
        hudManager.RemovePowerUpIcon(instaKillIcon);
        instaKillIcon = null;
    }

    private void RemoveDoublePointsIcon()
    {
        hudManager.RemovePowerUpIcon(doublePointsIcon);
        doublePointsIcon = null;
    }
    
    private void BlinkDoublePointsIcon()
    {
        hudManager.BlinkPowerUpIcon(doublePointsIcon);
    }

    private void BlinkInstaKillIcon()
    {
        hudManager.BlinkPowerUpIcon(instaKillIcon);
    }
}
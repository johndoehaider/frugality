using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class SpawnManager : MonoBehaviour
{
    public static event Action OnInstaKillExpiring;
    public static event Action OnInstaKillEnded;

    public GameObject [] enemyPrefabs;
    public GameObject bossPrefab;

    public GameObject [] spawnPoints;

    private HUDManager hudManager;
    private PowerUpManager powerupManager;

    private int roundNumber = 0;
    private int [] spawnAmounts = {6, 8, 13, 18};
    private int enemiesAlive;
    private int spawnAmount;
    private int maxSpawnAmount = 24;

    private bool waitingForNextRound = true;
    private bool isSpawning = true;
    private bool instaKillActive;
    private Coroutine instaKillCountdown;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        powerupManager = FindFirstObjectByType<PowerUpManager>();
        hudManager = FindFirstObjectByType<HUDManager>();

        Invoke ("SpawnEnemies", 5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (enemiesAlive == 0 && !waitingForNextRound && !isSpawning)
        {
            waitingForNextRound = true;
            Invoke ("SpawnEnemies", 5f);
        }
    }

    void SpawnEnemies()
    {
        waitingForNextRound = false;
        roundNumber++;

        hudManager.UpdateRoundsText(roundNumber);
        powerupManager.ResetDropCountOfRound();

        if (roundNumber < 5)
        {
            spawnAmount = spawnAmounts[roundNumber - 1];
        }
        else if (roundNumber < 10)
        {
            spawnAmount = 24 + roundNumber - 5;
        }
        else
        {
            spawnAmount = Mathf.FloorToInt(24 + 0.09f * roundNumber * roundNumber);
        }

        if (roundNumber % 5 != 0)
        {
            StartCoroutine(SpawnEnemyWaveNormal());
        }

        if (roundNumber % 5 == 0)
        {
            StartCoroutine(SpawnEnemyWaveBoss());
        }
    }

    IEnumerator SpawnEnemyWaveNormal()
    {
        isSpawning = true;

        int zombiesSpawned = 0;

        while (zombiesSpawned < spawnAmount)
        {
            if (enemiesAlive < maxSpawnAmount)
            {
                int randomIndex = UnityEngine.Random.Range(0, enemyPrefabs.Length);
                int randomSpawnPoint = UnityEngine.Random.Range(0, spawnPoints.Length);

                GameObject enemy = Instantiate(enemyPrefabs[randomIndex], spawnPoints[randomSpawnPoint].transform.position, enemyPrefabs[randomIndex].transform.rotation);

                Enemy enemyScript = enemy.GetComponent<Enemy>();
                enemyScript.SetSpawnManager(this);
                enemyScript.SetPowerUpManager(powerupManager);
                enemyScript.SetHealthForRound(roundNumber);
                enemyScript.SetSpeedForRound(roundNumber);

                if (instaKillActive)
                {
                    enemyScript.InstaKill();
                }

                enemiesAlive++;
                zombiesSpawned++;

                yield return new WaitForSeconds(2f);
            }
            else
            {
                yield return null;
            }
        }
        isSpawning = false;
    }

    IEnumerator SpawnEnemyWaveBoss()
    {
        spawnAmount = 1;
        isSpawning = true;
        for (int i = 0; i < spawnAmount * roundNumber; i++)
        {
            int randomSpawnPoint = UnityEngine.Random.Range(0, spawnPoints.Length);
            GameObject boss = Instantiate(bossPrefab, spawnPoints[randomSpawnPoint].transform.position, bossPrefab.transform.rotation);
            
            Enemy bossScript = boss.GetComponent<Enemy>();

            bossScript.SetSpawnManager(this);
            bossScript.DogSpeed();

            enemiesAlive++;

            yield return new WaitForSeconds(2f);
        }
        
        isSpawning = false;
    }

    public void EnemyDied()
    {
        enemiesAlive--;
    }

    public int GetRoundNumber()
    {
        return roundNumber;
    }

    public void InstaKill()
    {
        // Apply Insta-Kill to every enemy currently alive
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            enemy.InstaKill();
        }

        instaKillActive = true;

        // Reset the timer if Insta-Kill is picked up again
        if (instaKillCountdown != null)
        {
            StopCoroutine(instaKillCountdown);
        }

        instaKillCountdown = StartCoroutine(InstaKillTimer());
    }

    private IEnumerator InstaKillTimer()
    {
        yield return new WaitForSeconds(20f);
        OnInstaKillExpiring?.Invoke();
        yield return new WaitForSeconds(10f);

        instaKillActive = false;
        instaKillCountdown = null;

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            enemy.EndInstaKill();
        }

        OnInstaKillEnded?.Invoke();
    }

    public void Nuke()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in enemies)
        {
            enemy.Nuke();
        }
    }
}

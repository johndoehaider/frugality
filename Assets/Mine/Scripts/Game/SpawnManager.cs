using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SpawnManager : MonoBehaviour
{

    public GameObject [] enemyPrefabs;
    public GameObject bossPrefab;

    public GameObject [] spawnPoints;

    private HUDManager hudManager;

    private int roundNumber = 1;
    private int [] spawnAmounts = {6, 8, 13, 18};
    private int enemiesInScene;
    private int spawnAmount;
    private int maxSpawnAmount = 24;

    private bool waitingForNextRound = true;
    private bool isSpawning = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke ("SpawnEnemies", 5f);
        hudManager = FindFirstObjectByType<HUDManager>();
        hudManager.UpdateRoundsText(roundNumber);
    }

    // Update is called once per frame
    void Update()
    {
        enemiesInScene = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;

        if (enemiesInScene == 0 && !waitingForNextRound && !isSpawning)
        {
            waitingForNextRound = true;
            roundNumber++;
            hudManager.UpdateRoundsText(roundNumber);
            Invoke ("SpawnEnemies", 5f);
        }
    }

    IEnumerator SpawnEnemyWaveNormal()
    {
        isSpawning = true;

        int zombiesSpawned = 0;

        while (zombiesSpawned < spawnAmount)
        {
            if (enemiesInScene < maxSpawnAmount)
            {
                int randomIndex = Random.Range(0, enemyPrefabs.Length);
                int randomSpawnPoint = Random.Range(0, spawnPoints.Length);

                GameObject enemy = Instantiate(
                    enemyPrefabs[randomIndex],
                    spawnPoints[randomSpawnPoint].transform.position,
                    enemyPrefabs[randomIndex].transform.rotation
                );

                if (roundNumber <= 3)
                {
                    enemy.GetComponent<Enemy>().Walkers();
                }
                else if (roundNumber <= 10)
                {
                    enemy.GetComponent<Enemy>().RandomSpeedEarlyGame();
                }
                else if (roundNumber <= 20)
                {
                    enemy.GetComponent<Enemy>().Runners();
                }
                else
                {
                    enemy.GetComponent<Enemy>().RandomSpeedLateGame();
                }

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
            int randomSpawnPoint = Random.Range(0, spawnPoints.Length);
            GameObject boss = Instantiate(bossPrefab, 
            spawnPoints[randomSpawnPoint].transform.position, 
            bossPrefab.transform.rotation);
            
            boss.GetComponent<Enemy>().DogSpeed();
            yield return new WaitForSeconds(2f);
        }
        isSpawning = false;
    }

    void SpawnEnemies()
    {
        waitingForNextRound = false;

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
}

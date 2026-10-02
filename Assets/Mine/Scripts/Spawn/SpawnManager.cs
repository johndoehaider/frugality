using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    #region Events

    public static event Action OnInstaKillExpiring;
    public static event Action OnInstaKillEnded;

    #endregion

    #region Inspector

    [Header("Spawning")]
    [SerializeField] private ZombieZone[] zones;
    [SerializeField] private GameObject[] enemyPrefabs;
    [Min(1)]
    [SerializeField] private int maxConcurrentEnemies = 24;

    [Header("Round Population")]
    [Min(1)]
    [SerializeField] private int baseRoundPopulation = 24;
    [Min(0)]
    [SerializeField] private int playerPopulationContribution = 6;

    [Header("Round Pacing")]
    [Min(0f)]
    [SerializeField] private float initialRoundDelay = 5f;
    [Min(0f)]
    [SerializeField] private float betweenRoundDelay = 10f;
    [Min(0f)]
    [SerializeField] private float initialSpawnDelay = 2f;
    [Range(0.01f, 1f)]
    [SerializeField] private float spawnDelayMultiplier = 0.95f;
    [Min(0f)]
    [SerializeField] private float minimumSpawnDelay = 0.08f;

    [Header("Boss Rounds")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform[] bossSpawnPoints;

    #endregion

    #region Constants

    private static readonly float[] EarlyRoundPopulationMultipliers =
    {
        0.25f,
        0.30f,
        0.50f,
        0.70f,
        0.90f
    };

    private const float NoSpawnPointRetryDelay = 0.25f;
    private const float ConcurrentCapRetryDelay = 0.1f;
    private const float BossSpawnDelay = 2f;

    #endregion

    #region Runtime State

    private readonly HashSet<ZombieZone> activeSpawnZones = new HashSet<ZombieZone>();
    private readonly List<EnemySpawnPoint> eligibleSpawnPoints = new List<EnemySpawnPoint>();
    private readonly HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    private readonly List<Enemy> enemyBuffer = new List<Enemy>();

    private readonly WaitForSeconds spawnRetryWait = new WaitForSeconds(NoSpawnPointRetryDelay);
    private readonly WaitForSeconds concurrentCapRetryWait = new WaitForSeconds(ConcurrentCapRetryDelay);

    private HUDManager hudManager;
    private PowerUpManager powerupManager;
    private GameManager gameManager;

    private EnemySpawnPoint previousSpawnPoint;

    private int roundNumber;
    private bool isSpawning;
    private bool warnedAboutMissingSpawnPoint;

    private Coroutine nextRoundRoutine;
    private bool instaKillActive;
    private Coroutine instaKillCountdown;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        powerupManager = FindFirstObjectByType<PowerUpManager>();
        hudManager = FindFirstObjectByType<HUDManager>();

        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
        }

        nextRoundRoutine = StartCoroutine(NextRoundAfterDelay(initialRoundDelay));
    }

    #endregion

    #region Round Flow

    private void BeginRound()
    {
        roundNumber++;

        hudManager.UpdateRoundsText(roundNumber);
        powerupManager.ResetDropCountOfRound();

        if (roundNumber % 5 == 0)
        {
            if (!HasValidBossConfiguration())
            {
                Debug.LogError(
                    "SpawnManager reached a boss round without a valid Boss Prefab and Boss Spawn Point configuration.",
                    this
                );

                enabled = false;
                return;
            }

            StartCoroutine(SpawnBossWave());
            return;
        }

        int playerCount = gameManager.GetPlayers().Count;
        int spawnCount = GetSpawnCount(roundNumber, playerCount);

        StartCoroutine(SpawnNormalWave(spawnCount));
    }

    private void TryScheduleNextRound()
    {
        if (isSpawning || activeEnemies.Count > 0 || nextRoundRoutine != null)
        {
            return;
        }

        nextRoundRoutine = StartCoroutine(NextRoundAfterDelay(betweenRoundDelay));
    }

    private IEnumerator NextRoundAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        nextRoundRoutine = null;
        BeginRound();
    }

    #endregion

    #region Normal Spawning

    private IEnumerator SpawnNormalWave(int spawnCount)
    {
        isSpawning = true;

        int zombiesSpawned = 0;
        WaitForSeconds spawnDelayWait = new WaitForSeconds(GetSpawnDelay(roundNumber));

        while (zombiesSpawned < spawnCount)
        {
            if (activeEnemies.Count >= maxConcurrentEnemies)
            {
                yield return concurrentCapRetryWait;
                continue;
            }

            EnemySpawnPoint spawnPoint = GetRandomEligibleSpawnPoint();

            if (spawnPoint == null)
            {
                WarnMissingSpawnPointOnce();
                yield return spawnRetryWait;
                continue;
            }

            warnedAboutMissingSpawnPoint = false;

            GameObject prefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];

            Enemy enemy = SpawnEnemy(
                prefab,
                spawnPoint.transform.position,
                spawnPoint.transform.rotation
            );

            enemy.SetHealthForRound(roundNumber);
            enemy.SetSpeedForRound(roundNumber);
            enemy.SetSpawnPoint(spawnPoint);

            if (instaKillActive)
            {
                enemy.InstaKill();
            }

            zombiesSpawned++;
            yield return spawnDelayWait;
        }

        isSpawning = false;
        TryScheduleNextRound();
    }

    private Enemy SpawnEnemy(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject enemyObject = Instantiate(prefab, position, rotation);

        Enemy enemy = enemyObject.GetComponent<Enemy>();
        EnemyTargeting targeting = enemyObject.GetComponent<EnemyTargeting>();

        targeting.SetGameManager(gameManager);
        enemy.SetSpawnManager(this);
        enemy.SetPowerUpManager(powerupManager);

        activeEnemies.Add(enemy);

        return enemy;
    }

    private EnemySpawnPoint GetRandomEligibleSpawnPoint()
    {
        activeSpawnZones.Clear();
        eligibleSpawnPoints.Clear();

        for (int i = 0; i < zones.Length; i++)
        {
            ZombieZone zone = zones[i];

            if (zone == null || !zone.IsEnabled() || !zone.IsOccupied())
            {
                continue;
            }

            activeSpawnZones.Add(zone);

            foreach (ZombieZone connectedZone in zone.GetConnectedZones())
            {
                if (connectedZone != null && connectedZone.IsEnabled())
                {
                    activeSpawnZones.Add(connectedZone);
                }
            }
        }

        foreach (ZombieZone zone in activeSpawnZones)
        {
            zone.AddSpawnPointsTo(eligibleSpawnPoints);
        }

        if (eligibleSpawnPoints.Count == 0)
        {
            return null;
        }

        EnemySpawnPoint spawnPoint =
            eligibleSpawnPoints[UnityEngine.Random.Range(0, eligibleSpawnPoints.Count)];

        if (eligibleSpawnPoints.Count > 1 && spawnPoint == previousSpawnPoint)
        {
            spawnPoint =
                eligibleSpawnPoints[UnityEngine.Random.Range(0, eligibleSpawnPoints.Count)];
        }

        previousSpawnPoint = spawnPoint;

        return spawnPoint;
    }

    private float GetSpawnDelay(int round)
    {
        float delay =
            initialSpawnDelay *
            Mathf.Pow(spawnDelayMultiplier, Mathf.Max(0, round - 1));

        return Mathf.Max(minimumSpawnDelay, delay);
    }

    private void WarnMissingSpawnPointOnce()
    {
        if (warnedAboutMissingSpawnPoint)
        {
            return;
        }

        warnedAboutMissingSpawnPoint = true;

        Debug.LogWarning(
            "SpawnManager has no eligible enemy spawn points. Check zone occupancy, zone connections, and spawn-point configuration.",
            this
        );
    }

    #endregion

    #region Round Population

    private int GetSpawnCount(int round, int playerCount)
    {
        float populationMultiplier = Mathf.Max(1f, round / 5f);

        if (round >= 10)
        {
            populationMultiplier *= round * 0.15f;
        }

        int roundPopulation = baseRoundPopulation;

        if (playerCount <= 1)
        {
            roundPopulation += Mathf.FloorToInt(
                0.5f * playerPopulationContribution * populationMultiplier
            );
        }
        else
        {
            roundPopulation += Mathf.FloorToInt(
                (playerCount - 1) * playerPopulationContribution * populationMultiplier
            );
        }

        if (round <= EarlyRoundPopulationMultipliers.Length)
        {
            roundPopulation = Mathf.FloorToInt(
                roundPopulation * EarlyRoundPopulationMultipliers[round - 1]
            );
        }

        return roundPopulation;
    }

    #endregion

    #region Boss Spawning

    private IEnumerator SpawnBossWave()
    {
        isSpawning = true;

        int bossCount = roundNumber;
        WaitForSeconds bossSpawnWait = new WaitForSeconds(BossSpawnDelay);

        for (int i = 0; i < bossCount; i++)
        {
            while (activeEnemies.Count >= maxConcurrentEnemies)
            {
                yield return concurrentCapRetryWait;
            }

            Transform spawnPoint =
                bossSpawnPoints[UnityEngine.Random.Range(0, bossSpawnPoints.Length)];

            Enemy boss = SpawnEnemy(
                bossPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            boss.DogSpeed();

            yield return bossSpawnWait;
        }

        isSpawning = false;
        TryScheduleNextRound();
    }

    private bool HasValidBossConfiguration()
    {
        if (bossPrefab == null ||
            !bossPrefab.activeSelf ||
            bossSpawnPoints == null ||
            bossSpawnPoints.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < bossSpawnPoints.Length; i++)
        {
            if (bossSpawnPoints[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Enemy Tracking

    public void EnemyDied(Enemy enemy)
    {
        if (enemy == null || !activeEnemies.Remove(enemy))
        {
            return;
        }

        TryScheduleNextRound();
    }

    public int GetRoundNumber()
    {
        return roundNumber;
    }

    #endregion

    #region Powerups

    public void InstaKill()
    {
        foreach (Enemy enemy in activeEnemies)
        {
            enemy.InstaKill();
        }

        instaKillActive = true;

        if (instaKillCountdown != null)
        {
            StopCoroutine(instaKillCountdown);
        }

        instaKillCountdown = StartCoroutine(InstaKillTimer());
    }

    public void Nuke()
    {
        enemyBuffer.Clear();
        enemyBuffer.AddRange(activeEnemies);

        for (int i = 0; i < enemyBuffer.Count; i++)
        {
            Enemy enemy = enemyBuffer[i];

            if (enemy != null)
            {
                enemy.Nuke();
            }
        }
    }

    private IEnumerator InstaKillTimer()
    {
        yield return new WaitForSeconds(20f);
        OnInstaKillExpiring?.Invoke();

        yield return new WaitForSeconds(10f);

        instaKillActive = false;
        instaKillCountdown = null;

        foreach (Enemy enemy in activeEnemies)
        {
            enemy.EndInstaKill();
        }

        OnInstaKillEnded?.Invoke();
    }

    #endregion

    #region Validation

    private bool ValidateConfiguration()
    {
        bool isValid = true;

        if (gameManager == null)
        {
            Debug.LogError("SpawnManager could not find a GameManager.", this);
            isValid = false;
        }

        if (powerupManager == null)
        {
            Debug.LogError("SpawnManager could not find a PowerUpManager.", this);
            isValid = false;
        }

        if (hudManager == null)
        {
            Debug.LogError("SpawnManager could not find a HUDManager.", this);
            isValid = false;
        }

        if (zones == null || zones.Length == 0)
        {
            Debug.LogError("SpawnManager requires at least one ZombieZone.", this);
            isValid = false;
        }
        else
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] == null)
                {
                    Debug.LogError($"SpawnManager Zone element {i} is empty.", this);
                    isValid = false;
                }
            }
        }

        if (!ValidateEnemyPrefabs())
        {
            isValid = false;
        }

        return isValid;
    }

    private bool ValidateEnemyPrefabs()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("SpawnManager requires at least one enemy prefab.", this);
            return false;
        }

        bool isValid = true;

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            GameObject prefab = enemyPrefabs[i];

            if (prefab == null)
            {
                Debug.LogError($"SpawnManager Enemy Prefab element {i} is empty.", this);
                isValid = false;
                continue;
            }

            if (!prefab.activeSelf)
            {
                Debug.LogError($"Enemy prefab '{prefab.name}' must be active.", prefab);
                isValid = false;
            }

            if (prefab.GetComponent<Enemy>() == null ||
                prefab.GetComponent<EnemyTargeting>() == null)
            {
                Debug.LogError(
                    $"Enemy prefab '{prefab.name}' is missing required enemy components.",
                    prefab
                );

                isValid = false;
            }
        }

        return isValid;
    }

    #endregion
}

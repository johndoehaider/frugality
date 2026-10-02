using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZombieZone : MonoBehaviour
{
    [SerializeField] private bool startsEnabled;

    private bool isEnabled;

    private readonly List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();
    private readonly Dictionary<Character, HashSet<Collider>> playerColliders = new Dictionary<Character, HashSet<Collider>>();
    private readonly HashSet<ZombieZone> connectedZones = new HashSet<ZombieZone>();
    private readonly List<Character> stalePlayers = new List<Character>(4);
    private readonly List<Collider> staleColliders = new List<Collider>(8);

    private void Awake()
    {
        isEnabled = startsEnabled;

        Collider zoneCollider = GetComponent<Collider>();

        if (!zoneCollider.isTrigger)
        {
            Debug.LogError($"ZombieZone '{name}' requires a trigger Collider.", this);
        }

        EnemySpawnPoint[] childSpawnPoints = GetComponentsInChildren<EnemySpawnPoint>(true);

        for (int i = 0; i < childSpawnPoints.Length; i++)
        {
            EnemySpawnPoint spawnPoint = childSpawnPoints[i];

            if (!spawnPoint.IsValidConfiguration())
            {
                Debug.LogError($"ZombieZone '{name}' contains invalid spawn point '{spawnPoint.name}'.", spawnPoint);
                continue;
            }

            spawnPoints.Add(spawnPoint);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Character player = other.GetComponentInParent<Character>();

        if (player == null)
        {
            return;
        }

        if (!playerColliders.TryGetValue(player, out HashSet<Collider> colliders))
        {
            colliders = new HashSet<Collider>();
            playerColliders.Add(player, colliders);
        }

        colliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        Character player = other.GetComponentInParent<Character>();

        if (player == null || !playerColliders.TryGetValue(player, out HashSet<Collider> colliders))
        {
            return;
        }

        colliders.Remove(other);

        if (colliders.Count == 0)
        {
            playerColliders.Remove(player);
        }
    }

    public bool IsEnabled()
    {
        return isEnabled;
    }

    public bool IsOccupied()
    {
        RemoveInvalidOccupants();
        return playerColliders.Count > 0;
    }

    public void EnableZone()
    {
        isEnabled = true;
    }

    public void ConnectTo(ZombieZone other)
    {
        if (other == null || other == this)
        {
            return;
        }

        connectedZones.Add(other);
        other.connectedZones.Add(this);

        isEnabled = true;
        other.isEnabled = true;
    }

    public IEnumerable<ZombieZone> GetConnectedZones()
    {
        return connectedZones;
    }

    public void AddSpawnPointsTo(List<EnemySpawnPoint> destination)
    {
        destination.AddRange(spawnPoints);
    }

    private void RemoveInvalidOccupants()
    {
        stalePlayers.Clear();

        foreach (KeyValuePair<Character, HashSet<Collider>> pair in playerColliders)
        {
            Character player = pair.Key;
            HashSet<Collider> colliders = pair.Value;

            if (player == null)
            {
                stalePlayers.Add(player);
                continue;
            }

            staleColliders.Clear();

            foreach (Collider playerCollider in colliders)
            {
                if (playerCollider == null ||
                    !playerCollider.enabled ||
                    !playerCollider.gameObject.activeInHierarchy)
                {
                    staleColliders.Add(playerCollider);
                }
            }

            for (int i = 0; i < staleColliders.Count; i++)
            {
                colliders.Remove(staleColliders[i]);
            }

            if (colliders.Count == 0)
            {
                stalePlayers.Add(player);
            }
        }

        for (int i = 0; i < stalePlayers.Count; i++)
        {
            playerColliders.Remove(stalePlayers[i]);
        }
    }
}

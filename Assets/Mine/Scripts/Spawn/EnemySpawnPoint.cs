using UnityEngine;

public enum EnemySpawnEntryType
{
    Direct,
    Barrier
}

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private EnemySpawnEntryType entryType;
    [SerializeField] private Barrier assignedBarrier;

    public EnemySpawnEntryType GetEntryType()
    {
        return entryType;
    }

    public Barrier GetAssignedBarrier()
    {
        return assignedBarrier;
    }

    public bool IsValidConfiguration()
    {
        return entryType != EnemySpawnEntryType.Barrier || assignedBarrier != null;
    }
}

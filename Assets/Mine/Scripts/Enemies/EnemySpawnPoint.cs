using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Tooltip("Barrier this spawn enters through. Leave empty for an interior/riser spawn.")]
    [SerializeField] private Barrier assignedBarrier;

    public Barrier GetAssignedBarrier()
    {
        return assignedBarrier;
    }
}
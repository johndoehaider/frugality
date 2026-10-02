using UnityEngine;

public class Barrier : MonoBehaviour, IInteractable
{
    [System.Serializable]
    private sealed class ZombieEntrySlot
    {
        [SerializeField] private Transform approachPoint;
        [SerializeField] private Transform exitPoint;

        [System.NonSerialized] public Enemy Occupant;

        public Transform ApproachPoint => approachPoint;
        public Transform ExitPoint => exitPoint;
        public bool IsValid => approachPoint != null && exitPoint != null;
    }

    [Header("Boards")]
    [SerializeField] private GameObject[] boards;
    [SerializeField] private Collider zombieBlocker;
    [SerializeField] private float repairCooldown = 0.5f;

    [Header("Zombie Entry")]
    [SerializeField] private ZombieEntrySlot[] entrySlots;

    private const int PointLimit = 100;

    private int brokenBoards;
    private int pointsReceived;
    private float lastRepairTime;

    private void Awake()
    {
        ValidateConfiguration();
    }

    public void ZombieDamage()
    {
        if (IsBroken())
        {
            return;
        }

        boards[brokenBoards].SetActive(false);
        brokenBoards++;

        if (IsBroken())
        {
            zombieBlocker.enabled = false;
        }
    }

    public bool IsBroken()
    {
        return brokenBoards >= boards.Length;
    }

    public bool TryReserveClosestEntrySlot(Enemy enemy, out int slotIndex)
    {
        slotIndex = -1;
        float closestDistanceSquared = Mathf.Infinity;

        for (int i = 0; i < entrySlots.Length; i++)
        {
            ZombieEntrySlot slot = entrySlots[i];

            if (!slot.IsValid || slot.Occupant != null)
            {
                continue;
            }

            Vector3 difference = slot.ApproachPoint.position - enemy.transform.position;
            difference.y = 0f;

            float distanceSquared = difference.sqrMagnitude;

            if (distanceSquared >= closestDistanceSquared)
            {
                continue;
            }

            closestDistanceSquared = distanceSquared;
            slotIndex = i;
        }

        if (slotIndex < 0)
        {
            return false;
        }

        entrySlots[slotIndex].Occupant = enemy;
        return true;
    }

    public bool TryGetEntrySlot(int slotIndex, out Vector3 approachPosition, out Vector3 exitPosition)
    {
        approachPosition = Vector3.zero;
        exitPosition = Vector3.zero;

        if (!IsValidSlotIndex(slotIndex))
        {
            return false;
        }

        ZombieEntrySlot slot = entrySlots[slotIndex];
        approachPosition = slot.ApproachPoint.position;
        exitPosition = slot.ExitPoint.position;
        return true;
    }

    public void ReleaseEntrySlot(int slotIndex, Enemy enemy)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return;
        }

        ZombieEntrySlot slot = entrySlots[slotIndex];

        if (slot.Occupant == enemy)
        {
            slot.Occupant = null;
        }
    }

    public bool TryGetEntryApproachCenter(out Vector3 center)
    {
        center = Vector3.zero;
        int validSlotCount = 0;

        for (int i = 0; i < entrySlots.Length; i++)
        {
            ZombieEntrySlot slot = entrySlots[i];

            if (!slot.IsValid)
            {
                continue;
            }

            center += slot.ApproachPoint.position;
            validSlotCount++;
        }

        if (validSlotCount == 0)
        {
            return false;
        }

        center /= validSlotCount;
        return true;
    }

    public string GetInteractionText(PlayerInteraction playerInteraction)
    {
        return brokenBoards > 0 ? "Hold E to Rebuild Barrier" : "";
    }

    public void Interact(PlayerInteraction playerInteraction)
    {
        if (Time.time - lastRepairTime < repairCooldown)
        {
            return;
        }

        RebuildBoard(playerInteraction.GetComponent<PlayerPoints>());
        lastRepairTime = Time.time;
    }

    public bool UsesContinuousInteract()
    {
        return true;
    }

    private void RebuildBoard(PlayerPoints playerPoints)
    {
        if (brokenBoards <= 0)
        {
            return;
        }

        brokenBoards--;
        boards[brokenBoards].SetActive(true);
        zombieBlocker.enabled = true;

        if (playerPoints != null && pointsReceived < PointLimit)
        {
            playerPoints.AddPoints(10);
            pointsReceived += 10;
        }
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex < entrySlots.Length &&
               entrySlots[slotIndex].IsValid;
    }

    private void ValidateConfiguration()
    {
        if (boards == null || boards.Length == 0)
        {
            Debug.LogError($"Barrier '{name}' requires at least one board.", this);
        }
        else
        {
            for (int i = 0; i < boards.Length; i++)
            {
                if (boards[i] == null)
                {
                    Debug.LogError($"Barrier '{name}' has an empty board reference at index {i}.", this);
                }
            }
        }

        if (zombieBlocker == null)
        {
            Debug.LogError($"Barrier '{name}' requires a Zombie Blocker.", this);
        }

        if (entrySlots == null || entrySlots.Length == 0)
        {
            Debug.LogError($"Barrier '{name}' requires at least one zombie entry slot.", this);
            return;
        }

        for (int i = 0; i < entrySlots.Length; i++)
        {
            if (entrySlots[i] == null || !entrySlots[i].IsValid)
            {
                Debug.LogError($"Barrier '{name}' has an incomplete zombie entry slot at index {i}.", this);
            }
        }
    }
}

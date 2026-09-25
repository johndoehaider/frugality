using UnityEngine;

public class Barrier : MonoBehaviour, IInteractable
{

    [SerializeField] private GameObject[] boards;
    [SerializeField] private Collider zombieBlocker;
    [SerializeField] private float repairCooldown = 0.5f;

    private int brokenBoards = 0;
    private int pointLimit = 100;
    private int pointsRecieved = 0;
    private float lastRepairTime;


    public void ZombieDamage()
    {
        if (IsBroken())
        {
            return;
        }

        RemoveBoard();
    }

    public void RemoveBoard()
    {
        if (brokenBoards < boards.Length)
        {
            boards[brokenBoards].SetActive(false);
            brokenBoards++;
            if (IsBroken())
            {
                zombieBlocker.enabled = false;
            }
        }
    }

    public void RebuildBoard(PlayerPoints playerPoints)
    {
        if (brokenBoards > 0)
        {
            brokenBoards--;
            boards[brokenBoards].SetActive(true);
            zombieBlocker.enabled = true;

            if (pointsRecieved < pointLimit)
            {
                playerPoints.AddPoints(10);
                pointsRecieved += 10;
            }
        }   
    }

    public bool IsBroken()
    {
        if (brokenBoards == boards.Length)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public string GetInteractionText(PlayerInteraction playerInteraction)
    {
        if (brokenBoards > 0)
        {
            return "Hold E to Rebuild Barrier";
        }
        return "";
    }

    public void Interact(PlayerInteraction playerInteraction)
    {
        PlayerPoints playerPoints = playerInteraction.GetComponent<PlayerPoints>();

        if (Time.time - lastRepairTime >= repairCooldown)
        {
            RebuildBoard(playerPoints);
            lastRepairTime = Time.time;
        }
    }

    public bool UsesContinuousInteract()
    {
        return true;
    }
}

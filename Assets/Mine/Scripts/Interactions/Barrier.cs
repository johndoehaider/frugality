using UnityEngine;

public class Barrier : MonoBehaviour, IInteractable
{

    [SerializeField] private GameObject[] boards;
    [SerializeField] private Collider zombieBlocker;
    [SerializeField] private float repairCooldown = 0.5f;

    private PlayerPoints playerPoints;

    private int brokenBoards = 0;
    private int pointLimit = 100;
    private int pointsRecieved = 0;
    private float lastRepairTime;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerPoints = FindFirstObjectByType<PlayerPoints>();
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

    public void RebuildBoard()
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

    public string GetInteractionText()
    {
        if (brokenBoards > 0)
        {
            return "Hold E to Rebuild Barrier";
        }
        return "";
    }

    public void Interact()
    {
        if (Time.time - lastRepairTime >= repairCooldown)
        {
            RebuildBoard();
            lastRepairTime = Time.time;
        }
    }

    public bool UsesContinuousInteract()
    {
        return true;
    }
}

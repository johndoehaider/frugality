using UnityEngine;

public class PlayerPoints : MonoBehaviour
{
    [SerializeField] private int startingPoints = 500;

    private HUDManager hudManager;
    public int points;

    // Finds the HUD, sets the starting point total, and sends the initial value to the UI.
    private void Start()
    {
        points = startingPoints;
        hudManager = FindFirstObjectByType<HUDManager>();

        if (hudManager != null)
            hudManager.UpdatePoints(points);
    }

    // Returns the player's current point total.
    public int GetPoints()
    {
        return points;
    }

    // Adds points and updates the HUD.
    public void AddPoints(int amount)
    {
        points += amount;

        if (hudManager != null)
            hudManager.UpdatePoints(points);
    }

    // Removes points and updates the HUD.
    public void RemovePoints(int amount)
    {
        points -= amount;

        if (hudManager != null)
            hudManager.UpdatePoints(points);
    }

    // Returns whether the player currently has at least the requested amount of points.
    public bool HasEnoughPoints(int amount)
    {
        return points >= amount;
    }
}

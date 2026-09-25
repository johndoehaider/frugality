using UnityEngine;
using System;
using System.Collections;

public class PlayerPoints : MonoBehaviour
{
    [SerializeField] private int startingPoints = 500;

    public static event Action<PlayerPoints, int> OnPointsEarned;
    public static event Action OnDoublePointsExpiring;
    public static event Action OnDoublePointsEnded;

    private HUDManager hudManager;
    public int points;

    private bool doublePointsActive;
    private Coroutine doublePointsCountdown;

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
        if (doublePointsActive)
        {
            amount *= 2;
        }

        points += amount;
        OnPointsEarned?.Invoke(this, amount);

        if (hudManager != null)
        {
            hudManager.UpdatePoints(points);
        }
    }

    // Removes points and updates the HUD.
    public void RemovePoints(int amount)
    {
        points -= amount;

        if (hudManager != null)
        {
            hudManager.UpdatePoints(points);
        }
    }

    // Returns whether the player currently has at least the requested amount of points.
    public bool HasEnoughPoints(int amount)
    {
        return points >= amount;
    }

    public void DoublePoints()
    {
        doublePointsActive = true;

        if (doublePointsCountdown != null)
        {
            StopCoroutine(doublePointsCountdown);
        }

        doublePointsCountdown = StartCoroutine(DoublePointsTimer());
    }
    
    private IEnumerator DoublePointsTimer()
    {
        yield return new WaitForSeconds(20f);
        OnDoublePointsExpiring?.Invoke();
        yield return new WaitForSeconds(10f);
        
        doublePointsActive = false;
        doublePointsCountdown = null;

        OnDoublePointsEnded?.Invoke();
    }
}

using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private readonly List<Character> players = new List<Character>();

    public void RegisterPlayer(Character player)
    {
        if (player != null && !players.Contains(player))
        {
            players.Add(player);
        }
    }

    public void UnregisterPlayer(Character player)
    {
        players.Remove(player);
    }

    public IReadOnlyList<Character> GetPlayers()
    {
        return players;
    }
}
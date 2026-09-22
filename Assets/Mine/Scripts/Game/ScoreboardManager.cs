using UnityEngine;
using System.Collections.Generic;

public class ScoreboardManager : MonoBehaviour
{
    private Dictionary<PlayerPoints, int> playerScores = new Dictionary<PlayerPoints, int>();

    private void OnEnable()
    {
        PlayerPoints.OnPointsEarned += AddScore;
    }

    private void OnDisable()
    {
        PlayerPoints.OnPointsEarned -= AddScore;
    }

    private void AddScore(PlayerPoints player, int amount)
    {
        if (!playerScores.ContainsKey(player))
        {
            playerScores.Add(player, 0);
        }

        playerScores[player] += amount;
    }

    public int GetPlayerScore(PlayerPoints player)
    {
        if (playerScores.TryGetValue(player, out int score))
        {
            return score;
        }

        return 0;
    }

    public int GetTotalScore()
    {
        int totalScore = 0;

        foreach (int score in playerScores.Values)
        {
            totalScore += score;
        }

        return totalScore;
    }
}
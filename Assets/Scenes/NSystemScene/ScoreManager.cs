using Unity.Netcode;
using UnityEngine;
using System;

public class ScoreManager : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(
        10000,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public event Action OnGameOver;

    public void AddScore(int value)
    {
        if (!IsServer) return;

        score.Value += value;

        if (score.Value < 0)
            score.Value = 0;

        Debug.Log("Score: " + score.Value);

        if (score.Value <= 0 && !isGameOver.Value)
        {
            isGameOver.Value = true;
            Debug.Log("GAME OVER (ScoreManager)");
            OnGameOver?.Invoke();
        }
    }

    public int GetScore()
    {
        return score.Value;
    }
}
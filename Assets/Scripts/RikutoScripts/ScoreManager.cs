using Unity.Netcode;
using UnityEngine;
using System;

public class ScoreManager : NetworkBehaviour
{
    [SerializeField] int initilScore;
    public int InitialScore => initilScore;
    public NetworkVariable<int> score = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    [SerializeField] NetworkVariable<int> totalBonus = new NetworkVariable<int>(0);
    [SerializeField] NetworkVariable<int> lastClearBonus = new NetworkVariable<int>(0);
    bool isGameOver = false;
    public int TotalBonus => totalBonus.Value;
    public int LastClearBonus => lastClearBonus.Value;
    [Header("Publish Event")]
    [SerializeField] VoidEvent OnScoreReachZeroServerEvent;
    public void AddBonusServerOnly(int value)
    {
        if (!IsServer) return;
        //lastClearBonus.Value = value;
        totalBonus.Value += value;
        AddScoreServerOnly(value);
        //Debug.Log("Bonus Added: " + value + ", Total Score: " + score.Value);
    }
    public void AddScoreServerOnly(int value)
    {
        if (!IsServer) return;

        score.Value += value;

        if (score.Value < 0)
            score.Value = 0;

        //Debug.Log("Score: " + score.Value);

        if (score.Value <= 0 && !isGameOver)
        {
            isGameOver = true;
            Debug.Log("GAME OVER (ScoreManager)");
            OnScoreReachZeroServerEvent?.Invoke();
        }
    }

    public void SetScoreServerOnly()
    {
        score.Value = initilScore;
    }

    public int GetScore()
    {
        return score.Value;
    }

    public void ResetScore()
    {
        SetScoreServerOnly();
    }

    public void SetBonusServerOnly(int value)
    {
        if (!IsServer) return;
        lastClearBonus.Value = value;
    }
}
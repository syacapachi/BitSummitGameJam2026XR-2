using Unity.Netcode;
using UnityEngine;
using System;
using Syacapachi.Attribute;

public class ScoreManager : NetworkBehaviour
{
    [SerializeField] DifficultyDataBase database;
    [SerializeField] int debugScore = 10000;
    [SerializeField] int alertScore = 5000;
    public int InitialScore => debugScore;
    public NetworkVariable<int> score = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    [SerializeField] NetworkVariable<int> totalBonus = new NetworkVariable<int>(0);
    [SerializeField] NetworkVariable<int> lastClearBonus = new NetworkVariable<int>(0);
    
    public int TotalBonus => totalBonus.Value;
    public int LastClearBonus => lastClearBonus.Value;
    [Header("Publish Event")]
    [SerializeField] VoidEvent OnScoreReachZeroServerEvent;
    [SerializeField] BoolEvent aleartRpcEvent;

    bool isGameOver = false;
    bool isAlert = false;

    private void OnEnable()
    {
        score.OnValueChanged += OnScoreChanged;
    }
    private void OnDisable()
    {
        score.OnValueChanged -= OnScoreChanged;
    }
    private void OnScoreChanged(int oldScore, int newScore)
    {
        if(oldScore > alertScore && newScore <= alertScore)
        {
            aleartRpcEvent.Invoke(true);
        }
        else if(oldScore <= alertScore && newScore > alertScore)
        {
            aleartRpcEvent.Invoke(false);
        }
    }
    [OnInspectorButton(showOnlyInPlayMode = true)]
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
        score.Value = debugScore;
    }
    public void SetScoreByDifficultyServerOnly(Difficulty difficulty)
    {
        score.Value = database.GetSetting(difficulty).PlayerHP;
    }

    public int GetScore()
    {
        return score.Value;
    }

    public void ResetScore()
    {
        isGameOver = false;
        SetScoreServerOnly();
    }

    public void SetBonusServerOnly(int value)
    {
        if (!IsServer) return;
        lastClearBonus.Value = value;
    }
}
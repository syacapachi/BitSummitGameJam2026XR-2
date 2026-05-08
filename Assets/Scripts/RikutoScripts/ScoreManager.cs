using Syacapachi.Attribute;
using Unity.Netcode;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    [SerializeField] DifficultyDataBase database;
    [SerializeField] int alertScore = 5000;
    
    public NetworkVariable<int> score = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    [SerializeField] int totalBonusServerOnly = 0;
    [SerializeField] NetworkVariable<int> lastClearBonus = new NetworkVariable<int>(0);
    
    public int TotalBonusServerOnly => totalBonusServerOnly;
    public int LastClearBonus => lastClearBonus.Value;
    [Header("Publish Event")]
    [SerializeField] VoidEvent OnScoreReachZeroServerEvent;
    [SerializeField] BoolEvent aleartRpcEvent;

    int currentMaxScore = 10000;
    bool isGameOver = false;
    public int InitialScore => currentMaxScore;

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
        totalBonusServerOnly += value;
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
    public void SetScoreByDifficultyServerOnly(Difficulty difficulty)
    {
        int hp = database.GetSetting(difficulty).PlayerHP;
        score.Value = hp;
        currentMaxScore = hp;
    }

    public int GetScore()
    {
        return score.Value;
    }

    public void ResetScore()
    {
        isGameOver = false;
        totalBonusServerOnly = 0;
    }

    public void SetBonusServerOnly(int value)
    {
        if (!IsServer) return;
        lastClearBonus.Value = value;
    }
}
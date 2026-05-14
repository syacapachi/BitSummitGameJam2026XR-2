using Syacapachi.Attribute;
using Unity.Netcode;
using UnityEngine;

public class HPManager : NetworkBehaviour
{
    [SerializeField] DifficultyDataBase rpcDataBase;
    [SerializeField] int alertHP = 5000;
    
    public NetworkVariable<int> remainHP = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    int totalBonusHPServerOnly = 0;
    
    public int TotalBonusHPServerOnly => totalBonusHPServerOnly;
    [Header("Publish Event")]
    [SerializeField] VoidEvent OnScoreReachZeroServerEvent;
    [SerializeField] BoolEvent aleartRpcEvent;

    int currentMaxHP = 10000;
    bool isGameOver = false;
    public int InitialHP => currentMaxHP;

    private void OnEnable()
    {
        remainHP.OnValueChanged += OnHPChanged;
    }
    private void OnDisable()
    {
        remainHP.OnValueChanged -= OnHPChanged;
    }
    private void OnHPChanged(int oldHP, int newHP)
    {
        if(oldHP > alertHP && newHP <= alertHP)
        {
            aleartRpcEvent.Invoke(true);
        }
        else if(oldHP <= alertHP && newHP > alertHP)
        {
            aleartRpcEvent.Invoke(false);
        }
    }
    [OnInspectorButton(ShowOnlyInPlayMode = true)]
    public void AddBonusHPServerOnly(int value)
    {
        if (!IsServer) return;
        //lastClearBonus.Value = value;
        totalBonusHPServerOnly += value;
        AddHPServerOnly(value);
        //Debug.Log("Bonus Added: " + value + ", Total Score: " + score.Value);
    }
    public void AddHPServerOnly(int value)
    {
        if (!IsServer) return;

        if(remainHP.Value + value > currentMaxHP)
        {
            remainHP.Value = currentMaxHP;
        }
        else if(remainHP.Value + value < 0)
        {
            remainHP.Value = 0;
        }
        else
        {
            remainHP.Value += value;
        }

        if (remainHP.Value <= 0 && !isGameOver)
        {
            isGameOver = true;
            Debug.Log("GAME OVER (HPManager)", gameObject);
            OnScoreReachZeroServerEvent.Invoke();
        }
    }
    public void SetHPByDifficultyServerOnly()
    {
        int hp = rpcDataBase.CurrentSetting.PlayerHP;
        Debug.Log($"rpcDataBase diff = {rpcDataBase.CurrectDifficulty} ", gameObject);
        remainHP.Value = hp;
        currentMaxHP = hp;
    }

    public int GetHP()
    {
        return remainHP.Value;
    }

    public void ResetHP()
    {
        isGameOver = false;
        totalBonusHPServerOnly = 0;
    }
}
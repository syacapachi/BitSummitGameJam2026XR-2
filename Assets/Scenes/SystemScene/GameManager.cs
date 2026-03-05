using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public enum Phase
    {
        Tutorial,
        Phase1,
        Phase2,
        Phase3,
        Clear
    }
    public GameObject phaseBoard;
    public TextMeshProUGUI phaseText;

    public Phase phase;

    float timer;
    int lastSeconds;

    public EnemySpawner spawner; // Inspectorでセット

    public static GameManager Instance;

    private int score = 0;

    void Start()
    {
        phase = Phase.Tutorial;
        timer = 10f;

        lastSeconds = Mathf.CeilToInt(timer);

        Debug.Log("Start Phase : " + phase);
    }

    void Update()
    {
        if(timer > 0) timer -= Time.deltaTime;

        int currentSeconds = Mathf.CeilToInt(timer);

        //Debug.Log("debug : " + currentSeconds);
        if (currentSeconds != lastSeconds)
        {
            Debug.Log("残り時間 : " + currentSeconds);
            lastSeconds = currentSeconds;
        }

        if (timer <= 0 && phase != Phase.Clear)
        {
            NextPhase();
        }
    }

    void NextPhase()
    {
        if(phase != Phase.Tutorial) CheckPhaseBonus();
        if (phase == Phase.Tutorial)
        {
            phase = Phase.Phase1;
            timer = 30f;

            spawner.SpawnEnemies();  // フェーズ1開始で敵を出す
        }
        else if (phase == Phase.Phase1)
        {
            phase = Phase.Phase2;
            timer = 30f;

            spawner.SpawnEnemies();  // フェーズ2開始で敵を出す
        }
        else if (phase == Phase.Phase2)
        {
            phase = Phase.Phase3;
            timer = 30f;

            spawner.SpawnEnemies();  // フェーズ3開始で敵を出す
        }
        else if (phase == Phase.Phase3)
        {
            phase = Phase.Clear;
        }
        ShowPhaseText();
    }

    void Awake()
    {
        Instance = this;
    }

    public void AddScore(int value)
    {
        score += value;
        Debug.Log("Score: " + score);
    }

    void CheckPhaseBonus()
    {
        Debug.Log("hello");
        if (spawner.remain == 0)
        {
            Debug.Log("全撃破ボーナス +500");
            AddScore(500);
        }
    }

    public void EnemyKilled()
    {
        spawner.remain--;
    }

    void ShowPhaseText()
    {
        phaseText.text = phase.ToString();
        phaseBoard.SetActive(true);

        CancelInvoke(nameof(HidePhaseText));
        Invoke(nameof(HidePhaseText), 3f);
    }

    void HidePhaseText()
    {
        phaseBoard.SetActive(false);
    }
}

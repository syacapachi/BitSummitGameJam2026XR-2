using UnityEngine;

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
        timer -= Time.deltaTime;

        int currentSeconds = Mathf.CeilToInt(timer);

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
}

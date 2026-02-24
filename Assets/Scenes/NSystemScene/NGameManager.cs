using UnityEngine;

public class NGameManager : MonoBehaviour
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

    public NEnemySpawner spawner; // Inspector�ŃZ�b�g

    public static NGameManager Instance;

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
            Debug.Log("�c�莞�� : " + currentSeconds);
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

            spawner.SpawnEnemies();  // �t�F�[�Y1�J�n�œG���o��
        }
        else if (phase == Phase.Phase1)
        {
            phase = Phase.Phase2;
            timer = 30f;

            spawner.SpawnEnemies();  // �t�F�[�Y2�J�n�œG���o��
        }
        else if (phase == Phase.Phase2)
        {
            phase = Phase.Phase3;
            timer = 30f;

            spawner.SpawnEnemies();  // �t�F�[�Y3�J�n�œG���o��
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
            Debug.Log("�S���j�{�[�i�X +500");
            AddScore(500);
        }
    }

    public void EnemyKilled()
    {
        spawner.remain--;
    }
}

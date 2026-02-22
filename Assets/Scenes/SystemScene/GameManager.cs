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
            Debug.Log("Žc‚èŽžŠÔ : " + currentSeconds);
            lastSeconds = currentSeconds;
        }

        if (timer <= 0)
        {
            NextPhase();
        }
    }

    void NextPhase()
    {
        if (phase == Phase.Tutorial)
        {
            phase = Phase.Phase1;
            timer = 30f;
        }
        else if (phase == Phase.Phase1)
        {
            phase = Phase.Phase2;
            timer = 30f;
        }
        else if (phase == Phase.Phase2)
        {
            phase = Phase.Phase3;
            timer = 30f;
        }
        else if (phase == Phase.Phase3)
        {
            phase = Phase.Clear;
        }

        Debug.Log("Phase Changed : " + phase);
    }
}

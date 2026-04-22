using UnityEngine;
[CreateAssetMenu()]
public class PhaseCountDownSettingSO : ScriptableObject
{
    [SerializeField] int countdownStart;
    [SerializeField] float countdownBaseDuration;
    [SerializeField] float countdownLastDuration;

    public int CountdownStart => countdownStart;
    public float CountDownBaseDuration => countdownBaseDuration;
    public float CountdownLastDuration => countdownLastDuration;
}

using UnityEngine;

[CreateAssetMenu(fileName = "DifficultySetting", menuName = "Game/DifficultySetting")]
public class DifficultySetting : ScriptableObject
{
    [SerializeField] Difficulty difficulty;
    [SerializeField] int playerHP;
    [SerializeField] PhaseSO[] phaseSO;

    public Difficulty Difficulty => difficulty;
    public int PlayerHP => playerHP;
    public PhaseSO[] Phases => phaseSO;

}
public enum Difficulty
{
    Easy,
    Normal,
    Hard,
    Debug
}
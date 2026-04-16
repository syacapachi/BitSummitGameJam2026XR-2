using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour, ITutorialStart
{
    [SerializeField] Button button;
    [Header("SubScribeEvent")]
    [SerializeField] EnemyKilledEvent KilledEvent;
   

    void Start()
    {
        if (button != null)
            button.onClick.AddListener(OnTutorialStart);
    }
    private void OnEnable()
    {
        KilledEvent.Register(t => OnEnemyKilled(t.KilledEnemy));
    }
    private void OnDisable()
    {
        KilledEvent.Unregister(t => OnEnemyKilled(t.KilledEnemy));
    }
    private void OnEnemyKilled(IEnemy enemy)
    {
        Debug.Log("EnemyKilled!");
    }

    public void OnTutorialStart()
    {
        throw new System.NotImplementedException();
    }
}

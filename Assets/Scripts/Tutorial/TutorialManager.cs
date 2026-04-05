using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour,IEnemyBrokenReciever,ITutorialStart
{
    [SerializeField] Button button;

    void Start()
    {
        if(button != null)
            button.onClick.AddListener(OnTutorialStart);
    }
    public void OnEnemyKilled(IEnemy enemy)
    {
        Debug.Log("EnemyKilled!");
    }

    public void OnTutorialStart()
    {
        throw new System.NotImplementedException();
    }
}

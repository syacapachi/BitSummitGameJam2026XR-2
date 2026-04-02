using System.Collections.Generic;
using UnityEngine;

public class EnemyDeathReciver : MonoBehaviour,IEnemyBrokenReciever
{
    [SerializeField] NEnemySpawner spawner;
    [SerializeField] TutorialManager tutorialManager;
    void Start()
    {
        recievers.Add(spawner);
        recievers.Add(tutorialManager);
    }
    readonly List<IEnemyBrokenReciever> recievers = new ();
        public void EnemyKilled(IEnemy enemy)
    {
        foreach(var r in recievers)
        {
            r.EnemyKilled(enemy);
        } 
    }
}

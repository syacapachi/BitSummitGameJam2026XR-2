using System.Collections.Generic;
using UnityEngine;

public class EnemyDeathReciver : MonoBehaviour,IEnemyBrokenReciever
{
    [Header("Publish Event")]
    [SerializeField] EnemyKilledEvent KilledEvent; 
    public void OnEnemyKilled(IEnemy enemy)
    {
        KilledEvent.Invoke(new EnemyKilled() { KilledEnemy = enemy });
    }
}

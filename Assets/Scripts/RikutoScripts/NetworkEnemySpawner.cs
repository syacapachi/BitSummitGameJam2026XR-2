using Syacapachi.Attribute;
using Syacapachi.util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;


public class NetworkEnemySpawner : NetworkBehaviour,IEnemyBrokenReciever,ISpawnable,IKillable
{
    /// <summary>
    /// 待ってる敵が出てくるタイプ
    /// </summary>
    enum WaitSpawnType
    {
        /// <summary>
        /// 敵が分けるようになったらすぐに次の敵を出す
        /// </summary>
        WaitForNext,
        /// <summary>
        /// 全ての敵が倒されるまで次の敵を出さない
        /// </summary>
        WaitForAllEnemyDead,
        /// <summary>
        /// 一部の敵が倒されるまで次の敵を出さない
        /// </summary>
        WaitForSomeEnemyDead
    }
    /// <summary>
    /// 次のフェイズに行った時の条件
    /// </summary>
    enum NextPhaseType
    {
        /// <summary>
        /// 待ち行列を残す
        /// </summary>
        Remain,
        /// <summary>
        /// 待ち行列を削除する
        /// </summary>
        Delete
    }
    [Header("Setting")]
    [SerializeField] int maxEnemyCapacity = 10;
    [SerializeField] WaitSpawnType waitSpawnType = WaitSpawnType.WaitForNext;
    [SerializeField,EnableIfEnum(nameof(waitSpawnType),true, WaitSpawnType.WaitForSomeEnemyDead)] 
    int waitSpawnRemainCount = 5;
    [SerializeField] NextPhaseType nextPhaseType = NextPhaseType.Remain;
    //[SerializeField] EnemyDataBase enemyDataBase;
    [Header("Reference")]
    [SerializeField] NetworkObjectPool networkPool;
#if UNITY_EDITOR
    [SerializeField] Transform spawnPointParent;
#endif
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] Transform protectArea;
    [Header("PublishEvent")]
    [SerializeField] VoidEvent OnAllEnemyDeadRpcEvent;
    [Header("SubScribe Event")]
    [SerializeField] EnemyKilledEvent EnemyKilled;
    public int remain;
    public int RemainServerOnly => remain;
    private bool isSpawnFinished = false;
    private bool isAllDead = false;
    public bool IsSpawnFinishedServerOnly => isSpawnFinished;
    public bool IsAllDeadServerOnly => isAllDead;
    private readonly List<IEnemy> spawnedEnemies = new ();
    private readonly Queue<SpawnEvent> waitSpawnEnemyQueue = new();
    Coroutine waitForSpawn;

    public override void OnNetworkSpawn()
    {
        if(!IsServer) return;
        EnemyKilled.Register(EnemyKilledEventHandle);
    }
    public override void OnNetworkDespawn()
    {
        if(!IsServer) return;
        EnemyKilled.Unregister(EnemyKilledEventHandle);
    }
    private void EnemyKilledEventHandle(EnemyKilled killled)
    {
        OnEnemyKilled(killled.KilledEnemy);
    }
    public void SpawnFromEvent(List<SpawnEvent> events, bool useRandomSpawn)
    {
        if (!IsServer) return;
        if (!ManagerLocator.Instance.AllGameManager.IsGamePlaying) return;

        StopAllCoroutines();
        waitForSpawn = null;
        if (nextPhaseType == NextPhaseType.Delete)
        {
            waitSpawnEnemyQueue.Clear();
        }
        //remain = 0;
        // 追加（次のフェーズでリセット）　
        isAllDead = false;
        isSpawnFinished = false;

        StartCoroutine(SpawnRoutine(events));
        //とりあえず、入力されたリストをそれっぽくまぜまぜ
        if (useRandomSpawn)
        {
            List<SpawnEvent> randomSpawnEvent = new();
            foreach (SpawnEvent e in events)
            {
                int point = Random.Range(0, spawnPoints.Length);
                float time = Random.Range(0,e.SpawnTime);
                SpawnEvent randomEvent = new SpawnEvent(e.EnemyType,point,time);
                randomSpawnEvent.Add(randomEvent);
            }
            StartCoroutine(SpawnRoutine(randomSpawnEvent));
        }
    }
    public void StopSpaw()
    {
        if (!IsServer) return;
        StopAllCoroutines();
        waitForSpawn = null;
    }

    IEnumerator SpawnRoutine(List<SpawnEvent> spawnEvents)
    {
        float timer = 0f;

        // spawnTime順にソート（重要）
        //spawnEvents.Sort((a, b) => a.SpawnTime.CompareTo(b.SpawnTime));
        //Queueで高速に
        Queue<SpawnEvent> spawnQueue = new(spawnEvents.OrderBy(e => e.SpawnTime));

        int index = 0;
        isSpawnFinished = false; // 念のためリセット


        while (spawnQueue.Count > 0)
        {
            if (!ManagerLocator.Instance.AllGameManager.IsGamePlaying) yield break;
            timer += Time.deltaTime;

            // 今の時間で出すべき敵を全部出す
            while (spawnQueue.Count > 0 && spawnQueue.Peek().SpawnTime <= timer)
            {

                SpawnEvent e = spawnQueue.Dequeue();
                //SpawnEvent e = spawnEvents[index];

                if (e.EnemyType == null)
                {
                    //EnemySO が null のためスキップ
                    continue;
                }

                //通常召喚かつ規定数以上の場合待機行列に送る
                if (!e.ForceSpawn &&spawnedEnemies.Count >= maxEnemyCapacity)
                {
                    waitSpawnEnemyQueue.Enqueue(e);
                    waitForSpawn ??= StartCoroutine(WaitForSpawn());
                }
                else
                {
                    if (SpawnEnemy(e.EnemyType, e.SpawnPointIndex))
                    {
                        remain++;
                    }
                }

                index++;
            }
            //Debug.Log($"[SpawnRoutine] queue:{spawnQueue.Count} waitQueue:{waitSpawnEnemyQueue.Count}");

            yield return null;
        }
        Debug.Log("SpawnRoutine END");
        isSpawnFinished = true;
    }
    private IEnumerator WaitForSpawn()
    {
        while (waitSpawnEnemyQueue.Count > 0)
        {
            if (!ManagerLocator.Instance.AllGameManager.IsGamePlaying) yield break;
            switch(waitSpawnType)
            {
                case WaitSpawnType.WaitForNext:
                    while (spawnedEnemies.Count >= maxEnemyCapacity)
                    {
                        yield return null;
                    }
                    break;
                case WaitSpawnType.WaitForAllEnemyDead:
                    while (spawnedEnemies.Count > 0)
                    {
                        yield return null;
                    }
                    break;
                case WaitSpawnType.WaitForSomeEnemyDead:
                     while (spawnedEnemies.Count > waitSpawnRemainCount)
                    {
                        yield return null;
                    }
                    break;
            }
            if(waitSpawnEnemyQueue.Count > 0)
            {
                SpawnEvent e = waitSpawnEnemyQueue.Dequeue();
                if (e.EnemyType == null)
                {
                    continue;
                }
                if (SpawnEnemy(e.EnemyType, e.SpawnPointIndex))
                {
                    remain++;
                }
            }
        }
        waitForSpawn = null;
    }
    /*
    void SpawnEnemy(EnemySO enemyData, int[] usableIndex)
    {
        if (usableIndex.Length == 0)
        {
            Debug.LogWarning("No usable spawn points!");
            return;
        }
        int maxLength = Mathf.Min(usableIndex.Length,spawnPoints.Length);
        int randomArrayIndex = Random.Range(0, maxLength);
        int spawnIndex = usableIndex[randomArrayIndex];

        Transform point = spawnPoints[spawnIndex];

        Vector3 dir = protectArea.position - point.position;
        Quaternion rot = Quaternion.LookRotation(dir);

        GameObject obj = Instantiate(
            enemyData.prefab,
            point.position,
            rot
        );

        obj.GetComponent<NetworkObject>().Spawn();
    }
    */

    bool SpawnEnemy(EnemySO enemyData, int spawnIndex)
    {
        if (spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
        {
            Debug.LogWarning("Invalid spawn index!");
            return false;
        }

        Transform point = spawnPoints[spawnIndex];

        Vector3 dir = protectArea.position - point.position;
        Quaternion rot = Quaternion.LookRotation(dir);

        NetworkObject networkObject = networkPool.GetNetworkObject(
            enemyData.Prefab,
               point.position,
               rot);

        var enemy = networkObject.GetComponent<IEnemy>();
        enemy.InjectSetting(enemyData);
        spawnedEnemies.Add(enemy);
        networkObject.Spawn();
        //int id = enemyDataBase.GetIdFromEnemyData(enemyData);
        //enemy.InitEnemyRpc(id);
        return true;
    }

    public void OnEnemyKilled(IEnemy enemy)
    {
        if (!IsServer) return;
        remain--;
        Debug.Log($"[OnEnemyKilled] remain:{remain} spawnFinished:{isSpawnFinished} waitQueue:{waitSpawnEnemyQueue.Count}");
        if (remain == 0 && isSpawnFinished && waitSpawnEnemyQueue.Count == 0)
        {
            isAllDead = true;
            InvokeAllEnemyDeadRpc();
        }
        spawnedEnemies.Remove(enemy);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void InvokeAllEnemyDeadRpc()
    {
        OnAllEnemyDeadRpcEvent.Invoke();
    }

    public void KillAll()
    {
        waitSpawnEnemyQueue.Clear();    
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.NetworkObject.IsSpawned)
            {
                enemy.NetworkObject.Despawn(true);   
            }
        }
        spawnedEnemies.Clear();
        remain = 0;
        isSpawnFinished = false;
    }

    public void ResetSpawner()
    {
        StopAllCoroutines();

        KillAll();

        remain = 0;
        isSpawnFinished = false;
        isAllDead = false;
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (spawnPointParent == null) return; 
        var childs = spawnPointParent.GetComponentsInChildren<Transform>();
        if(childs != null)
        {
            spawnPoints = childs.Skip(1).ToArray();
        }
        
    }
#endif
}
[GenerateEvent(typeof(GameEventSOBase<>))]
public class EnemyKilled 
{
    public IEnemy KilledEnemy;
    public Vector3 positon;
}
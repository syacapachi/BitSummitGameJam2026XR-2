using Syacapachi.util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 敵を出現させるクラスです。出現した敵の参照を持ち、現在出現している数や出現待ちの敵を管理します。
/// </summary>
/// <remarks>
/// Responsibilities:
/// <br /> - 敵の出現
/// <br /> - 現在出現している敵の数
/// <br /> - 出現待ちの敵の数
/// <br /> - 出現中の敵を一斉に殺す
/// <br />
/// Requires:
/// <br /> - 出現ルール
///
/// <br />
/// Events:
/// <br /> - OnAllEnemyDeadRpcEvent:すべての敵がプレーヤーよってに死亡したら発行
/// </remarks>
public class NetworkEnemySpawner : NetworkBehaviour, IEnemyBrokenReciever, ISpawnable, IKillable
{
    [Header("Setting")]
    [SerializeField] EnemyDataBase enemyDataBase;
    [Header("Reference")]
    [SerializeField] NetworkObjectPool networkPool;
    [SerializeField] CheckPointManager checkPointManager;
    [SerializeField] GameStateManager gameStateManager;
    [SerializeField] Transform protectArea;
    [Header("PublishEvent")]
    [SerializeField] VoidEvent OnAllEnemyDeadRpcEvent;
    [Header("SubScribe Event")]
    [SerializeField] EnemyKilledEvent EnemyKilled;
    //出現可能地点と、最大出現数のうち、小さいほうが採用される。
    int waitSpawnRemainCount = 5;
    WaitSpawnType waitSpawnType = WaitSpawnType.WaitForNext;
    private RandomTable table;
    private int gameSeedServerOnly;
    private int maxEnemyCapacity = 10;
    private int remain;
    /// <summary>
    /// 残ってる敵　サーバーのみ
    /// </summary>
    public int RemainServerOnly => remain;
    private bool isSpawnFinished = false;
    private bool isAllDead = false;
    /// <summary>
    /// スポーンが終わったか、サーバーのみ
    /// </summary>
    public bool IsSpawnFinishedServerOnly => isSpawnFinished;
    /// <summary>
    /// 全敵が死んでいるか、サーバーのみ
    /// </summary>
    public bool IsAllDeadServerOnly => isAllDead;
    //出現している敵のリスト　サーバーのみ,　最大数はスポーンポイントの数
    private readonly List<IEnemy> spawnedEnemies = new(32);
    //出現待ちの敵のキュー　サーバーのみ,最大数はない
    private readonly Queue<SpawnEvent> waitSpawnEnemyQueue = new(32);
    /// <summary>
    /// スポーンイベントのキャッシュリスト。GCを防ぐ,とりあえず、32で初期化。必要に応じて増やす。
    /// </summary>
    readonly List<SpawnEvent> spawnEventCacheList = new(32);
    /// <summary>
    /// キャッシュリスト。GCを防ぐ,とりあえず、32で初期化。必要に応じて増やす。
    /// </summary>
    readonly List<SpawnEvent> randomSpawnEventCacheList = new(32);
    /// <summary>
    /// 出現可能な場所のキャッシュリスト。GCを防ぐ,とりあえず、32で初期化。必要に応じて増やす。
    /// </summary>
    readonly List<CheckPointManager.IndexToTransform> spawnablePosCacheList = new(32);
    Coroutine waitForSpawn;
    Coroutine spawnCorutine;
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        EnemyKilled.Register(EnemyKilledEventHandle);
    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        EnemyKilled.Unregister(EnemyKilledEventHandle);
    }
    private void EnemyKilledEventHandle(in EnemyKilled killled)
    {
        OnEnemyKilled(killled.KilledEnemy);
    }
    /// <summary>
    /// シード値を設定します。サーバーしか使わないので、サーバーのみです。
    /// </summary>
    /// <param name="gameSeed">
    /// シード値
    /// </param>
    public void SetRandomSeedServerOnly(int gameSeed)
    {
        gameSeedServerOnly = gameSeed;
        table = new RandomTable(gameSeedServerOnly, 5000);
        //代替案
        //Unity.Mathematics.Random random;
    }
    /// <summary>
    /// 敵を出現させるコルーチンを起動します。
    /// サーバーかつゲームがプレイ中でないと実行されません。
    /// </summary>
    /// <param name="setting">
    /// 出現設定
    /// </param>
    public void SpawnFromEvent(SpawnSetting setting)
    {
        if (!IsServer) return;
        if (!ManagerLocator.Instance.AllGameManager.IsGamePlaying) return;

        if (spawnCorutine != null)
        {
            StopCoroutine(spawnCorutine);
        }
        waitSpawnType = setting.WaitSpawnType;
        waitSpawnRemainCount = setting.WaitSpawnRemainCount;
        if (setting.NextPhaseType == NextPhaseType.Delete)
        {
            waitForSpawn = null;
            waitSpawnEnemyQueue.Clear();
        }
        //remain = 0;
        //次のフェーズでリセット　
        isAllDead = false;
        isSpawnFinished = false;
        spawnEventCacheList.Clear();
        spawnEventCacheList.AddRange(setting.CustomSpawnEvents);
        //前もってランダムイベントを作成。
        if (setting.UseRandomSpawn)
        {
            //キャッシュ
            var randomSetting = setting.RandomSpawnSettings;
            //初期化
            randomSetting.ResetSpawnCount();
            //割り算は計算コストがあるので、先に計算。
            float invPhaseTime = 1f / setting.PhaseTime;
            float time = randomSetting.GetSpawnDuration(0f);
            //初期値があると、軽い
            randomSpawnEventCacheList.Clear();
            spawnablePosCacheList.Clear();
            while (time < setting.PhaseTime)
            {
                float progress = Mathf.Clamp01(time * invPhaseTime);

                EnemySpawnSetting spawnEnemy =
                    randomSetting.ChooseEnemyByWeight(
                        progress,
                        table.NextFloat());
                if (spawnEnemy != null)
                {
                    checkPointManager.GetSpawnPointByTag(spawnEnemy.SpawnPointTag, spawnablePosCacheList);
                    SpawnEvent spawnEvent = new SpawnEvent(
                            spawnEnemy.TargetEnemy,
                            spawnablePosCacheList[
                                table.RangeInt(0, spawnablePosCacheList.Count)
                            ].id,
                            time);
                    randomSpawnEventCacheList.Add(spawnEvent);

                    Debug.Log($"Create Random Spaen {spawnEvent}, Tag {spawnEnemy.SpawnPointTag}", gameObject);
                }
                else
                {
                    Debug.Log("EnemySpawnSetting is null", gameObject);
                }
                float duration = randomSetting.GetSpawnDuration(progress);

                //無限ループ防止
                if (duration <= 0f)
                {
                    break;
                }

                time += duration;
            }
            spawnEventCacheList.AddRange(randomSpawnEventCacheList);
        }
        //最大を更新
        maxEnemyCapacity = Math.Min(setting.MaxSpawn, checkPointManager.SpawnPoints.Length);
        spawnCorutine = StartCoroutine(SpawnRoutine(spawnEventCacheList));
    }
    public void StopSpawn()
    {
        if (!IsServer) return;
        StopAllCoroutines();
        waitForSpawn = null;
    }
    /// <summary>
    /// 敵を出現させるコルーチンです。spawnEventsの内容に従って敵を出現させます。
    /// </summary>
    /// <param name="spawnEvents">出現させる敵のイベントリストか配列</param>
    /// <returns></returns>
    /// <remarks>IReadOnlyListにすると、配列でも良くなる。</remarks>
    IEnumerator SpawnRoutine(IReadOnlyList<SpawnEvent> spawnEvents)
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
            if (!gameStateManager.IsGamePlaying) yield break;
            timer += Time.deltaTime;

            // 今の時間で出すべき敵を全部出す
            while (spawnQueue.Count > 0 && spawnQueue.Peek().SpawnTime <= timer)
            {
                SpawnEvent e = spawnQueue.Dequeue();

                if (e.EnemyType == null)
                {
                    //EnemySO が null のためスキップ
                    continue;
                }

                //通常召喚かつ規定数以上の場合待機行列に送る
                if (!e.ForceSpawn && spawnedEnemies.Count >= maxEnemyCapacity)
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
            yield return null;
        }
        isSpawnFinished = true;
    }
    private IEnumerator WaitForSpawn()
    {
        while (waitSpawnEnemyQueue.Count > 0)
        {
            if (!gameStateManager.IsGamePlaying) yield break;
            switch (waitSpawnType)
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
            if (waitSpawnEnemyQueue.Count > 0)
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
        if (checkPointManager.IsUsingPoint(spawnIndex))
        {
            spawnIndex = checkPointManager.GetEnablePoint();
        }
        if (spawnIndex < 0 || spawnIndex >= checkPointManager.SpawnPoints.Length)
        {
            Debug.LogWarning("Invalid spawn index!", gameObject);
            return false;
        }
        checkPointManager.TrySetUsePoint(spawnIndex, true);
        var point = checkPointManager.SpawnPoints[spawnIndex].transform;

        Vector3 dir = protectArea.position - point.position;
        Quaternion rot = Quaternion.LookRotation(dir);

        NetworkObject networkObject = networkPool.GetNetworkObject(
            enemyData.Prefab,
               point.position,
               rot);

        var enemy = networkObject.GetComponent<IEnemy>();
        enemy.InjectSetting(enemyDataBase.GetIdFromEnemyData(enemyData), spawnIndex);
        spawnedEnemies.Add(enemy);
        networkObject.Spawn();
        return true;
    }

    public void OnEnemyKilled(IEnemy enemy)
    {
        if (!IsServer) return;
        if (gameStateManager.CurrentGameState != GameState.Playing) return;
        remain--;
        Debug.Log($"[OnEnemyKilledServerEvent] remainServerOnly:{remain} spawnFinished:{isSpawnFinished} waitQueue:{waitSpawnEnemyQueue.Count}", gameObject);
        if (remain == 0 && isSpawnFinished && waitSpawnEnemyQueue.Count == 0)
        {
            isAllDead = true;
            InvokeAllEnemyDeadRpc();
        }
        spawnedEnemies.Remove(enemy);
        checkPointManager.TrySetUsePoint(enemy.CurrentPointIndexServerOnly, false);
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
                checkPointManager.TrySetUsePoint(enemy.CurrentPointIndexServerOnly, false);
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
}
//[GenerateEvent(typeof(GameEventSOBase<>))]
public readonly struct EnemyKilled 
{
    public readonly IEnemy KilledEnemy;
    public readonly Vector3 Positon;
    public EnemyKilled(Vector3 pos, IEnemy killedEnemy)
    {
        this.Positon = pos;
        this.KilledEnemy = killedEnemy;
    }
}
/// <summary>
/// 待ってる敵が出てくるタイプ
/// </summary>
public enum WaitSpawnType
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
/// 次のフェイズに行った時の待ち敵行列の条件
/// </summary>
public enum NextPhaseType
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
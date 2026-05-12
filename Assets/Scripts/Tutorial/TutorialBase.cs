using System;
/// <summary>
/// 共通の変数とかを使ったり、コンストラクタを使うなら抽象クラスを使うとよい
/// </summary>
public abstract class TutorialBase
{
    protected readonly Action onComplete;
    protected readonly TutorialSpawner spawner;

    public TutorialBase(TutorialSpawner spawner, Action onComplete)
    {
        this.spawner = spawner;
        this.onComplete = onComplete;
    }

    protected void HandleAllDead()
    {
        onComplete?.Invoke();
    }
    /// <summary>
    /// チュートリアルのフェイズ開始時に呼ばれる
    /// 必ず実装する
    /// </summary>
    public abstract void OnStart();
    /// <summary>
    /// チュートリアルのフェイズ終了時に呼ばれる
    /// </summary>
    public abstract void OnEnd();

    public virtual void OnTargetDestroyed(ulong playerId) { }
    public virtual void OnAttackBlocked(ulong playerId) { }
    public virtual void OnEnemyKilled(EnemyKilled e) { }

    public virtual void OnMarkerPlaced(ulong playerId) { }
}
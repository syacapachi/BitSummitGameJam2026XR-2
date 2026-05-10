using Syacapachi.Attribute;
using System;
using UnityEngine;
//[GenerateEvent(typeof(GameEventSOBase<>),isArray:true)]
[Flags]
public enum PlayerJob
{
    /// <summary>
    ///両方見えない。両方あたる。
    /// </summary>
    Nothing = 0,
    /// <summary>
    /// 悪魔だけ見えない。悪魔だけ当たる。
    /// </summary>
    Demon = 1,
    /// <summary>
    /// おばけだけ見えない。おばけだけ当たる。
    /// </summary>
    Ghost = 1 << 1,
    /// チュートリアル用のジョブ
    /// 全員見えるし、全員当たる
    /// </summary>
    Tutorial = 1<<2
}
[Serializable]
public struct PlayerLayerSettings : IJobSetting
{
    /// <summary>
    /// 設定対象の職業
    /// </summary>
    [SerializeField,SingleFlagOnly]
    PlayerJob TargetJob;
    /// <summary>
    /// 攻撃が当たる職業。複数選択可能。
    /// </summary>
    [SerializeField]
    PlayerJob AttackableJob;
    /// <summary>
    /// Cameraのカリングマスク
    /// </summary>
    public LayerMask CullingMask;
    /// <summary>
    /// コライダーのレイヤー
    /// </summary>
    [SerializeField,Layer]
    int ColliderLayer ;


    public readonly PlayerJob SettingJob => TargetJob;

    public readonly int CollidersLayer => ColliderLayer;

    public readonly PlayerJob AttackableJobs => AttackableJob;

    public PlayerLayerSettings(LayerMask cullingMask, PlayerJob job,PlayerJob attackableJob)
    {
        CullingMask = cullingMask;
        TargetJob = job;
        AttackableJob = attackableJob;
        ColliderLayer = 0;
    }

    /// <summary>
    /// 最初に見つかったレイヤーを返す
    /// </summary>
    /// <returns></returns>
    public static int GetFirstLayer(LayerMask mask)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                return i;
            }
        }
        return -1; //見つからない場合は-1を返す
    }
    public readonly bool IsAttackableJob(PlayerJob targetJob)
    {
        return (AttackableJob & targetJob) != 0;
    }
    public readonly int GetNonVisibleLayerIndex()
    {
        return GetFirstLayer(~CullingMask);
    }
    public readonly bool IsVisibleLayer(int targetLayer)
    {
        Debug.Log($"Checking visibility for ownerLayer {targetLayer} in CullingMask {CullingMask.value}");
        return (CullingMask.value & (1 << targetLayer)) != 0;
    }
    public readonly bool IsVisibleLayer(LayerMask targetLayerMask)
    {
        return (CullingMask.value & targetLayerMask.value) != 0;
    }
    public override readonly string ToString()
    {
        return $"Job: {TargetJob}, AttackableJob: {AttackableJob}, CullingMask: {CullingMask.value}, CollidersLayer: {ColliderLayer}";
    }
}
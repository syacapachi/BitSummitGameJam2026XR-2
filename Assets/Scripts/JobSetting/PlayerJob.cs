using Syacapachi.Attribute;
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// 両方見える。両方当たらない
    /// </summary>
    Both = Demon | Ghost,
    /// <summary>
    /// チュートリアル用のジョブ
    /// 全員見えるし、全員当たる
    /// </summary>
    Tutorial = 1<<2
}
[Serializable]
public struct PlayerLayerSettings
{
    /// <summary>
    /// 設定対象の職業
    /// </summary>
    [SingleFlagOnly]
    public PlayerJob TargetJob;
    /// <summary>
    /// 攻撃が当たる職業。複数選択可能。
    /// </summary>
    public PlayerJob AttackableJob;
    /// <summary>
    /// Colliderのレイヤー 1つのレイヤーのみを指定してください。
    /// 複数指定した場合、最初のレイヤーが使用されます。
    /// Layerを指定する際は、Layerを参照してください。
    /// </summary>
    [SingleFlagOnly]
    public LayerMask TargetColliderLayer;
    /// <summary>
    /// Cameraのカリングマスク
    /// </summary>
    public LayerMask CullingMask;
    /// <summary>
    /// コライダーのレイヤー
    /// </summary>
    [HideInInspector]
    public int Layer ;

    public PlayerLayerSettings(int layer, LayerMask playerLayer, PlayerJob job,PlayerJob attackableJob)
    {
        TargetColliderLayer = layer;
        CullingMask = playerLayer;
        TargetJob = job;
        AttackableJob = attackableJob;
        Layer = 0;
        //最初に見つかったレイヤーを使用する
        for (int i = 0; i < 32; i++)
        {
            if((TargetColliderLayer.value & (1 << i)) != 0)
            {
                Layer = i;
                break;
            }
        }
    }
    public void LayerUpdate()
    {
        Layer = 0;
        //最初に見つかったレイヤーを使用する
        for (int i = 0; i < 32; i++)
        {
            if ((TargetColliderLayer.value & (1 << i)) != 0)
            {
                Layer = i;
                break;
            }
        }
    }
    public readonly bool IsAttackableJob(PlayerJob targetJob)
    {
        return (AttackableJob & targetJob) != 0;
    }
    public readonly bool IsAttackableLayer(int targetLayer)
    {
        return (CullingMask.value & (1 << targetLayer)) == 0;
    }
    public readonly bool IsAttackableLayer(LayerMask targetLayerMask)
    {
        return (CullingMask.value & targetLayerMask.value) == 0;
    }
    public readonly bool IsVisibleLayer(int targetLayer)
    {
        Debug.Log($"Checking visibility for layer {targetLayer} in CullingMask {CullingMask.value}");
        return (CullingMask.value & (1 << targetLayer)) != 0;
    }
    public readonly bool IsVisibleLayer(LayerMask targetLayerMask)
    {
        return (CullingMask.value & targetLayerMask.value) != 0;
    }
    public override readonly string ToString()
    {
        return $"Job: {TargetJob}, AttackableJob: {AttackableJob}, ColliderLayerMask: {TargetColliderLayer.value}, CullingMask: {CullingMask.value}, Layer: {Layer}";
    }
}
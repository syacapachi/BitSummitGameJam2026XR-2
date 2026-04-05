using Syacapachi.Attribute;
using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "JobSettingSO", menuName = "ScriptableObjects/JobSettingSO", order = 0)]
public class JobSettingSO : ScriptableObject
{
    [SerializeField] List<PlayerLayerSettings> playerLayerSettingsList = new List<PlayerLayerSettings>();
    public IReadOnlyList<PlayerLayerSettings> PlayerLayerSettingsList => playerLayerSettingsList;
}
[GenerateEvent(typeof(GameEventSOBase<>))]
[Flags]
public enum PlayerJob
{
    Nothing = 0,//両方見えない。両方あたる。
    Human = 1,//人間だけ見える。おばけだけ当たる。
    Ghost = 1 << 1,//おばけだけ見える。人間だけ当たる。
    Both = Human | Ghost,//両方見える。両方当たらない。
}
[Serializable]
public struct PlayerLayerSettings
{
    /// <summary>
    /// 設定対象の職業
    /// </summary>
    [SingleFlagOnly]
    public PlayerJob Job;
    /// <summary>
    /// 攻撃が当たる職業。複数選択可能。
    /// </summary>
    [HideInInspector]
    public PlayerJob AttackableJob;
    /// <summary>
    /// Colliderのレイヤー 1つのレイヤーのみを指定してください。
    /// 複数指定した場合、最初のレイヤーが使用されます。
    /// Layerを指定する際は、Layerを参照してください。
    /// </summary>
    [SingleFlagOnly]
    public LayerMask ColliderLayerMask;
    /// <summary>
    /// Cameraのカリングマスク
    /// </summary>
    public LayerMask CullingMask;
    /// <summary>
    /// 攻撃が当たるレイヤー。複数指定可能。
    /// </summary>
    [HideInInspector]
    public LayerMask AttackableLayer;
    /// <summary>
    /// コライダーのレイヤー
    /// </summary>
    [HideInInspector]
    public int Layer ;

    public PlayerLayerSettings(int layer, LayerMask playerLayer,LayerMask attackableLayer, PlayerJob job,PlayerJob attackableJob)
    {
        ColliderLayerMask = layer;
        CullingMask = playerLayer;
        AttackableLayer = attackableLayer;
        Job = job;
        AttackableJob = attackableJob;
        Layer = 0;
        //最初に見つかったレイヤーを使用する
        for (int i = 0; i < 32; i++)
        {
            if((ColliderLayerMask.value & (1 << i)) != 0)
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
            if ((ColliderLayerMask.value & (1 << i)) != 0)
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
        return (AttackableLayer.value & (1 << targetLayer)) != 0;
    }
    public readonly bool IsAttackableLayer(LayerMask targetLayerMask)
    {
        return (AttackableLayer.value & targetLayerMask.value) != 0;
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
        return $"Job: {Job}, AttackableJob: {AttackableJob}, ColliderLayerMask: {ColliderLayerMask.value}, CullingMask: {CullingMask.value}, AttackAbleLayer: {AttackableLayer.value}, Layer: {Layer}";
    }
}
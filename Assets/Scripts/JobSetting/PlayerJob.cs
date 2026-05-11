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
public class PlayerLayerSettings : JobSettingBase
{
    /// <summary>
    /// Cameraのカリングマスク
    /// </summary>
    public LayerMask CullingMask;
    /// <summary>
    /// SkyBox
    /// </summary>
    public Material skyboxMeterial;
    public PlayerLayerSettings() { }

    public PlayerLayerSettings(PlayerJob tartgetJob, PlayerJob attackableJob, LayerMask cullingMask, int colliderLayer):base(tartgetJob,attackableJob,colliderLayer)
    {
        CullingMask = cullingMask;
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
    public int GetNonVisibleLayerIndex()
    {
        return GetFirstLayer(~CullingMask);
    }
    public bool IsVisibleLayer(int targetLayer)
    {
        Debug.Log($"Checking visibility for ownerLayer {targetLayer} in CullingMask {CullingMask.value}");
        return (CullingMask.value & (1 << targetLayer)) != 0;
    }
    public bool IsVisibleLayer(LayerMask targetLayerMask)
    {
        return (CullingMask.value & targetLayerMask.value) != 0;
    }
    public override string ToString()
    {
        return $"Job: {base.SettingJob}, AttackableJob: {AttackableJobs}, CullingMask: {CullingMask.value}, CollidersLayer: {CollidersLayer}";
    }
}
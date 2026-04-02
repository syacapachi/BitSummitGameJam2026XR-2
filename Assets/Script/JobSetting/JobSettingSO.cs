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
    [SingleFlagOnly]
    public PlayerJob Job;//プレイヤーの職業
    /// <summary>
    /// コライダーのレイヤー
    /// </summary>
    public readonly int Layer ;

    public PlayerLayerSettings(int layer, LayerMask playerLayer, PlayerJob job)
    {
        ColliderLayerMask = layer;
        CullingMask = playerLayer;
        Job = job;
        Layer = 0;
        //最初に見つかったレイヤーを使用する
        for (int i= 0; i < 32; i++)
        {
            if((ColliderLayerMask.value & (1 << i)) != 0)
            {
                Layer = i;
                break;
            }
        }
    }
    public override readonly string ToString()
    {
        return $"Layer: {ColliderLayerMask}, LayerMask: {CullingMask}, Job: {Job}";
    }
}
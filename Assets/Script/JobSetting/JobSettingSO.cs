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
    [SingleFlagOnly]
    public LayerMask ColliderLayer;//Colliderのレイヤー
    public LayerMask CullingMask;//Cameraのカリングマスク
    [SingleFlagOnly]
    public PlayerJob Job;//プレイヤーの職業

    public PlayerLayerSettings(int layer, LayerMask playerLayer, PlayerJob job)
    {
        ColliderLayer = layer;
        CullingMask = playerLayer;
        Job = job;
    }
    public override readonly string ToString()
    {
        return $"Layer: {ColliderLayer}, LayerMask: {CullingMask}, Job: {Job}";
    }
}
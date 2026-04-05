using Syacapachi.Attribute;
using System;
using System.Collections.Generic;
using UnityEngine;
public class PlayerManager : MonoBehaviour
{
    private readonly List<NetworkPlayerRoot> playerList = new();
    /// <summary>
    /// このデバイスでのオーナーへの参照
    /// </summary>
    [SerializeField] LocalPlayerRoot localRoot;
    [Header("Subscribe Event")]
    [SerializeField] PlayerJobEvent jobEvent;
    public NetworkPlayerRoot NetworkOwnerPlayer { get;private set; }
    public LocalPlayerRoot LocalPlayerRoot => localRoot;
    public IReadOnlyList<NetworkPlayerRoot> AllPlayers => playerList;

    public event Action<PlayerJob> OnOwnerJobChanged;
    [Header("Owner Setting")]
    [Tooltip("ホスト、クライアント設定前のカメラ")]
    [SerializeField] bool IsJobOverride;
    [SerializeField,EnableIf(nameof(IsJobOverride))]
    PlayerJob JobOverride;
    public void ResistPlayer(NetworkPlayerRoot playerRoot)
    {
        playerList.Add(playerRoot);
    }
    public void UnResistPlayer(NetworkPlayerRoot playerRoot)
    {
        playerList.Remove(playerRoot);
    }
    public void ResistOwner(NetworkPlayerRoot playerRoot)
    {
        NetworkOwnerPlayer = playerRoot;
        Debug.Log("Resist owner");
        jobEvent.Register(OnJobChanged);
        if (IsJobOverride)
        {
            LocalPlayerRoot.Propaty.Job = JobOverride;
        }
    }
    public void UnResistOwner(NetworkPlayerRoot playerRoot)
    {
        jobEvent.Unregister(OnJobChanged);
        NetworkOwnerPlayer = null;
    }
    private void OnJobChanged(PlayerJob job)
    {
        OnOwnerJobChanged?.Invoke(job);
    }
}

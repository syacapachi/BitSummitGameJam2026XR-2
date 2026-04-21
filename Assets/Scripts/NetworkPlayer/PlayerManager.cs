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
    [SerializeField] PlayerJobEvent jobChangeEvent;
    public NetworkPlayerRoot NetworkOwnerPlayer { get;private set; }
    public LocalPlayerRoot LocalPlayerRoot => localRoot;
    public IReadOnlyList<NetworkPlayerRoot> AllPlayers => playerList;

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
        if (IsJobOverride)
        {
            //playerRoot.Propaty.Job = JobOverride;
        }
    }
    public void UnResistOwner(NetworkPlayerRoot playerRoot)
    {
        NetworkOwnerPlayer = null;
    }
}

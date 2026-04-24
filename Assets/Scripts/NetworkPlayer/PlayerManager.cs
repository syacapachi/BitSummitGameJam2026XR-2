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
    public NetworkPlayerRoot NetworkOwnerPlayer { get;private set; }
    public LocalPlayerRoot LocalPlayerRoot => localRoot;
    public IReadOnlyList<NetworkPlayerRoot> AllPlayers => playerList;

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
    }
    public void UnResistOwner(NetworkPlayerRoot playerRoot)
    {
        if (NetworkOwnerPlayer == playerRoot)
        {
            NetworkOwnerPlayer = null;
        }
    }
}

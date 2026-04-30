using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public class PlayerManager : MonoBehaviour
{
    private readonly List<NetworkPlayerRoot> playerList = new();
    private readonly Dictionary<ulong,bool> playerReadyDict = new();
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
        if (!playerReadyDict.ContainsKey(playerRoot.OwnerClientId))
        {
            playerReadyDict[playerRoot.OwnerClientId] = true;
        }
        
    }
    public void UnResistPlayer(NetworkPlayerRoot playerRoot)
    {
        playerList.Remove(playerRoot);
        playerReadyDict[playerRoot.OwnerClientId] = false;
    }
    public void ResistOwner(NetworkPlayerRoot playerRoot)
    {
        NetworkOwnerPlayer = playerRoot;
    }
    public void UnResistOwner(NetworkPlayerRoot playerRoot)
    {
        if (NetworkOwnerPlayer == playerRoot)
        {
            NetworkOwnerPlayer = null;
        }
    }
    public bool IsAllClientReady()
    {
        foreach(var client in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if(!playerReadyDict.TryGetValue(client,out var ready) || !ready)
            {
                return false;
            }
        }
        return true;
    }
}

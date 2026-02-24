using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using NUnit.Framework;
using System.Collections.Generic;
public class PlayerManager : MonoBehaviour
{
    private readonly List<PlayerRoot> playerList = new();
    public PlayerRoot OwnerPlayer {  get; private set; }

    public void ResistPlayer(PlayerRoot playerRoot)
    {
        playerList.Add(playerRoot);
    }
    public void UnResistPlayer(PlayerRoot playerRoot)
    {
        playerList.Remove(playerRoot);
    }
    public void ResistOwner(PlayerRoot playerRoot)
    {
        OwnerPlayer = playerRoot;
    }
}

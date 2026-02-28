using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using NUnit.Framework;
using System.Collections.Generic;
using System;
public class PlayerManager : MonoBehaviour
{
    private readonly List<PlayerRoot> playerList = new();
    public PlayerRoot OwnerPlayer {  get; private set; }

    public event Action<PlayerPropaty.PlayerJob> OnOwnerJobChanged;
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
        Debug.Log("Resist owner");
        OwnerPlayer.propaty.OnJobChanged += OnJobChanged;
    }
    public void UnResistOwner(PlayerRoot playerRoot)
    {
        OwnerPlayer.propaty.OnJobChanged -= OnJobChanged;
        OwnerPlayer = null;
    }
    private void OnJobChanged(PlayerPropaty.PlayerJob job)
    {
        OnOwnerJobChanged?.Invoke(job);
    }
}

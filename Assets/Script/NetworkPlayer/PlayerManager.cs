using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using Syacapachi.Attribute;
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

    public event Action<PlayerJob> OnOwnerJobChanged;
    [Header("Owner Setting")]
    [Tooltip("ホスト、クライアント設定前のカメラ")]
    [SerializeField] Camera mainCamera;
    [SerializeField] Canvas worldCanvas;
    [SerializeField] bool IsJobOverride;
    [SerializeField,EnableIf(nameof(IsJobOverride))]
    PlayerJob JobOverride;
    public Camera PlayerCamera => mainCamera;
    public Canvas WorldCanvas => worldCanvas;
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
        localRoot.Propaty.OnLocalJobChanged += OnJobChanged;
        mainCamera.enabled = false;
        worldCanvas.worldCamera = LocalPlayerRoot.CameraSetting.currentActiveCamera;
        if (IsJobOverride)
        {
            LocalPlayerRoot.Propaty.Job = JobOverride;
        }
    }
    public void UnResistOwner(NetworkPlayerRoot playerRoot)
    {
        localRoot.Propaty.OnLocalJobChanged -= OnJobChanged;
        NetworkOwnerPlayer = null;
        worldCanvas.worldCamera = null;
        mainCamera.enabled = true;
    }
    private void OnJobChanged(PlayerJob job)
    {
        OnOwnerJobChanged?.Invoke(job);
    }
}

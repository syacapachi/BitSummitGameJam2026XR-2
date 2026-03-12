using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using NUnit.Framework;
using System.Collections.Generic;
using System;
public class PlayerManager : MonoBehaviour
{
    private readonly List<PlayerRoot> playerList = new();
    /// <summary>
    /// このデバイスでのオーナーへの参照
    /// </summary>
    public PlayerRoot LocalOwnerPlayer {  get; private set; }

    public event Action<PlayerPropaty.PlayerJob> OnOwnerJobChanged;
    [Header("Owner Setting")]
    [Tooltip("ホスト、クライアント設定前のカメラ")]
    [SerializeField] Camera mainCamera;
    [SerializeField] Canvas worldCanvas;
    public Camera PlayerCamera => mainCamera;
    public Canvas WorldCanvas => worldCanvas;
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
        LocalOwnerPlayer = playerRoot;
        Debug.Log("Resist owner");
        LocalOwnerPlayer.propaty.OnJobChanged += OnJobChanged;
        mainCamera.enabled = false;
        worldCanvas.worldCamera = LocalOwnerPlayer.cameraSetting.localCamera;
    }
    public void UnResistOwner(PlayerRoot playerRoot)
    {
        LocalOwnerPlayer.propaty.OnJobChanged -= OnJobChanged;
        LocalOwnerPlayer = null;
        worldCanvas.worldCamera = null;
        mainCamera.enabled = true;
    }
    private void OnJobChanged(PlayerPropaty.PlayerJob job)
    {
        OnOwnerJobChanged?.Invoke(job);
    }
}

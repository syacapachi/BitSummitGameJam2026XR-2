using System;
using Unity.Netcode;
using UnityEngine;

public class CanvasCamera : NetworkBehaviour
{
    [SerializeField] Canvas uicanvas;
    public override void OnNetworkSpawn()
    {
        if(IsOwner)
            ManagerLocator.Instance.PlayerManager.OwnerPlayer.cameraSetting.OnCameraChanged += OnCameraChangedCallback;
    }
    /// <summary>
    /// イベント解除は一足早く行う
    /// </summary>
    public override void OnNetworkPreDespawn()
    {
        if (!IsOwner) return;
        CameraSetting setting = ManagerLocator.Instance.PlayerManager.OwnerPlayer?.cameraSetting;
        if (setting != null)
        {
            setting.OnCameraChanged -= OnCameraChangedCallback;
        }
    }
    private void OnCameraChangedCallback(Camera camera)
    {
        uicanvas.worldCamera = camera;
    }
}

using UnityEngine;
using Unity.Netcode;
using System;
[Obsolete("このクラスは、カメラのCullingMaskを変更する方針に変えたことで非推奨です")]
public class LookStateChange : NetworkBehaviour
{
    [SerializeField] Renderer meshRenderer;
    [SerializeField] PlayerJob lookableJob;
    [SerializeField] Canvas hpCanvas; // ←HPバーのCanvasをここにアサイン

    public override void OnNetworkSpawn()
    {
        PlayerManager playerManager = ManagerLocator.Instance.AllPlayerManager;
        playerManager.OnOwnerJobChanged += OnJobChangedHandle;

        if (playerManager.NetworkOwnerPlayer != null)
        {
            OnJobChangedHandle(playerManager.LocalPlayerRoot.Propaty.Job);
        }
    }

    public override void OnNetworkDespawn()
    {
        ManagerLocator.Instance.AllPlayerManager.OnOwnerJobChanged -= OnJobChangedHandle;
    }

    private void OnJobChangedHandle(PlayerJob job)
    {
        bool isVisible = (job & lookableJob) != 0;

        // 敵の表示
        meshRenderer.enabled = isVisible;

        // HPバーの表示も連動
        if (hpCanvas != null)
        {
            hpCanvas.enabled = isVisible;
        }
    }
}
using UnityEngine;
using Unity.Netcode;

public class LookStateChange : NetworkBehaviour
{
    [SerializeField] Renderer meshRenderer;
    [SerializeField] PlayerPropaty.PlayerJob lookableJob;
    [SerializeField] Canvas hpCanvas; // ←HPバーのCanvasをここにアサイン

    public override void OnNetworkSpawn()
    {
        PlayerManager playerManager = ManagerLocator.Instance.PlayerManager;
        playerManager.OnOwnerJobChanged += OnJobChangedHandle;

        if (playerManager.OwnerPlayer != null)
        {
            OnJobChangedHandle(playerManager.OwnerPlayer.propaty.Job);
        }
    }

    public override void OnNetworkDespawn()
    {
        ManagerLocator.Instance.PlayerManager.OnOwnerJobChanged -= OnJobChangedHandle;
    }

    private void OnJobChangedHandle(PlayerPropaty.PlayerJob job)
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
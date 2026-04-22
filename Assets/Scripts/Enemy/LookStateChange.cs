using UnityEngine;
using Unity.Netcode;
using System;
[Obsolete("このクラスは、カメラのCullingMaskを変更する方針に変えたことで非推奨です")]
public class LookStateChange : NetworkBehaviour
{
    [SerializeField] Renderer meshRenderer;
    [SerializeField] PlayerJob lookableJob;
    [SerializeField] Canvas hpCanvas; // ←HPバーのCanvasをここにアサイン
    [Header("SubscribeEvent")]
    [SerializeField] PlayerJobEvent jobChanged;
    public override void OnNetworkSpawn()
    {
        PlayerJob currentJob = jobChanged.CurrentValue;
        if(currentJob != default)
            OnJobChangedHandle(currentJob);
    }
    private void OnEnable()
    {
        jobChanged.Register(OnJobChangedHandle);
    }
    private void OnDisable()
    {
        if(jobChanged == null)
        {
            Debug.Log($"{gameObject.name}/{gameObject.transform.parent.name}");
        }
        jobChanged.Unregister(OnJobChangedHandle);
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
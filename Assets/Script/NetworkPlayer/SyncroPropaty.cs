using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.LowLevel;
using static PlayerPropaty;

public class SyncroPropaty : NetworkBehaviour
{
    [SerializeField] GameObject avatorCollider;
    private readonly NetworkVariable<int> PlayerLayer = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
        );
    private PlayerJob job = PlayerJob.Both;
    public PlayerJob Job => job;
    public override void OnNetworkSpawn()
    {
        ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot.Propaty.OnJobChanged += OnJobChangeHandle;
        PlayerLayer.OnValueChanged += OnValueChanged;
    }

    public override void OnNetworkDespawn()
    {
        ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot.Propaty.OnJobChanged -= OnJobChangeHandle;
        PlayerLayer.OnValueChanged -= OnValueChanged;
    }
    private void OnJobChangeHandle(PlayerJob newJob)
    {
        job = newJob;
        string layerName = jobToLayerMaskDic[newJob];
        PlayerLayer.Value = LayerMask.NameToLayer(layerName);
    }
    private void OnValueChanged(int previousValue, int newValue)
    {
        if (previousValue != newValue)
        {
            avatorCollider.layer = newValue;
        }
    }
}

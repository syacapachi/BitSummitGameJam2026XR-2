using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SyncroPropaty : NetworkBehaviour
{
    [SerializeField] GameObject avatorCollider;
    private readonly NetworkVariable<int> PlayerLayer = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
        );

    [field: SerializeField]
    public PlayerJob Job { get; private set; } = PlayerJob.Both;
    private IReadOnlyDictionary<PlayerJob, PlayerLayerSettings> jobToLayerMaskDic = new Dictionary<PlayerJob, PlayerLayerSettings>();


    private void Start()
    {
        var jobManager = ManagerLocator.Instance.JobManager;
        if (jobManager == null)
        {
            Debug.LogError("PlayerJobManager not found in the scene.");
            return;
        }
        jobToLayerMaskDic = jobManager.JobLayerMaskDic;
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot.Propaty.OnJobChanged += OnJobChangeHandle;
        }
        PlayerLayer.OnValueChanged += OnValueChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot.Propaty.OnJobChanged -= OnJobChangeHandle;
        }
        PlayerLayer.OnValueChanged -= OnValueChanged;
    }
    private void OnJobChangeHandle(PlayerJob newJob)
    {
        Job = newJob;
        PlayerLayerSettings setting = jobToLayerMaskDic[newJob];
        PlayerLayer.Value = setting.Layer;
    }
    private void OnValueChanged(int previousValue, int newValue)
    {
        if (previousValue != newValue)
        {
            avatorCollider.layer = newValue;
        }
    }
}

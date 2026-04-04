using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SyncroPropaty : NetworkBehaviour
{
    [SerializeField] GameObject avatorCollider;
    private int PlayerLayer = 0;
    [SerializeField] NetworkVariable<PlayerJob> syncroJob = new(
        PlayerJob.Both,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    public PlayerJob Job => syncroJob.Value;
    private IReadOnlyDictionary<PlayerJob, PlayerLayerSettings> jobToLayerMaskDic;


    private void OnEnable()
    {
        if(jobToLayerMaskDic == null)
        {
            ResistJobDic();
        }
        syncroJob.OnValueChanged += OnJobChanged;
    }
    private void OnDisable()
    {
        syncroJob.OnValueChanged -= OnJobChanged;
    }
    private void ResistJobDic()
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
            PlayerPropaty playerPropaty = ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot.Propaty;
            playerPropaty.OnLocalJobChanged += OnJobChangeHandle;
            syncroJob.Value = playerPropaty.Job;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot.Propaty.OnLocalJobChanged -= OnJobChangeHandle;
        }
    }
    private void OnJobChangeHandle(PlayerJob newJob)
    {
        syncroJob.Value = newJob;
        PlayerLayerSettings setting = jobToLayerMaskDic[newJob];
        PlayerLayer = setting.Layer;
    }
    private void OnJobChanged(PlayerJob previousJob, PlayerJob newJob)
    {
        if (previousJob != newJob)
        {
            PlayerLayerSettings setting = jobToLayerMaskDic[newJob];
            PlayerLayer = setting.Layer;
        }
    }
}

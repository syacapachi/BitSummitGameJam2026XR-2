using Syacapachi.Attribute;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SyncroPropaty : NetworkBehaviour
{
    [SerializeField] GameObject avatorCollider;
    [SerializeField] JobSetting setting;
    [SerializeField] PlayerJob playerjob = PlayerJob.Both;
    [SerializeField]
    NetworkVariable<PlayerJob> syncroJob = new(
        PlayerJob.Both,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    [Header("Subscribe Event")]
    [SerializeField] PlayerJobEvent jobChangeLocalEvent;
    [Header("Debug")]
    [SerializeField] bool IsDebugMode = false;
    [SerializeField,EnableIf(nameof(IsDebugMode))] VoidEvent switchJobEvent;

    private IReadOnlyDictionary<PlayerJob, PlayerLayerSettings> jobToLayerMaskDic;
    public PlayerJob Job
    {
        get => syncroJob.Value;
        set
        {
            if (playerjob != value)
            {
                playerjob = value;
                if (IsOwner)
                {
                    syncroJob.Value = playerjob;
                    jobChangeLocalEvent.Invoke(playerjob);
                }   
            }
        }
    }

    private void OnEnable()
    {
        if (jobToLayerMaskDic == null)
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
        jobToLayerMaskDic = setting.JobLayerMaskDic;
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (IsDebugMode)
            {
                switchJobEvent.Register(OnJobChangeHandle);
            }

            // ホストならHuman、クライアントならGhost
            PlayerJob initialJob = IsHost ? PlayerJob.Human : PlayerJob.Ghost;
            syncroJob.Value = initialJob;
            jobChangeLocalEvent.Invoke(initialJob);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && IsDebugMode)
        {
            switchJobEvent.Unregister(OnJobChangeHandle);
        }
    }
    private void OnJobChanged(PlayerJob previousJob, PlayerJob newJob)
    {
        if (previousJob != newJob)
        {
            PlayerLayerSettings setting = jobToLayerMaskDic[newJob];
            avatorCollider.layer = setting.Layer;
        }
    }
    private void OnJobChangeHandle()
    {
        Debug.Log("SwitchJob action performed! Current job: " + Job);
        Job = Job switch
        {
            PlayerJob.Nothing => PlayerJob.Human,
            PlayerJob.Human => PlayerJob.Ghost,
            PlayerJob.Ghost => PlayerJob.Both,
            PlayerJob.Both => PlayerJob.Nothing,
            _ => throw new System.NotImplementedException(),
        };
        Debug.Log("Job changed to: " + Job);
    }
}

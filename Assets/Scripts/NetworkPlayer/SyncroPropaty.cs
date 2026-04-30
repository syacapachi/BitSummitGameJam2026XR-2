using Syacapachi.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SyncroPropaty : NetworkBehaviour
{
    [Serializable]
    struct DefaultJobSetting
    {
        public bool IsHost;
        public PlayerJob Job;
    }
    [SerializeField] Collider[] avatorColliders;
    [SerializeField] JobSettingGenerator setting;
    [SerializeField] DefaultJobSetting[] defaultJobSettings = new DefaultJobSetting[]
    {
        new DefaultJobSetting { IsHost = true, Job = PlayerJob.Demon },
        new DefaultJobSetting { IsHost = false, Job = PlayerJob.Ghost },
    };
    [SerializeField]
    NetworkVariable<PlayerJob> syncroJob = new(
        PlayerJob.Both,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    [SerializeField, ReadOnly] PlayerJob currentjob = PlayerJob.Both;
    [Header("Subscribe Event")]
    [SerializeField] PlayerJobEvent jobChangeLocalEvent;
    [Header("Debug")]
    [SerializeField] bool IsDebugMode = false;
    [SerializeField,EnableIf(nameof(IsDebugMode))] VoidEvent switchJobEvent;
    public PlayerJob Job
    {
        get => syncroJob.Value;
        set
        {
            if (IsOwner)
            {
                jobChangeLocalEvent.Invoke(value);
                currentjob = value;
                if (syncroJob.Value != value)
                {
                    syncroJob.Value = value;
                }
            }
            
        }
    }

    private void OnEnable()
    {
        syncroJob.OnValueChanged += OnJobChanged;
    }
    private void OnDisable()
    {
        syncroJob.OnValueChanged -= OnJobChanged;
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (IsDebugMode)
            {
                switchJobEvent.Register(OnJobChangeHandle);
            }
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
            ApplyInitializeJob();
        }
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        ApplyInitializeJob();
    }
    private void ApplyInitializeJob()
    {
        // ホストならHuman、クライアントならGhost
        PlayerJob initialJob = IsHost ? defaultJobSettings.FirstOrDefault(s => s.IsHost).Job
                                      : defaultJobSettings.FirstOrDefault(s => !s.IsHost).Job;
        Job = initialJob;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            if (IsDebugMode)
            {
                switchJobEvent.Unregister(OnJobChangeHandle);
            }
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        }   
    }
    private void OnJobChanged(PlayerJob previousJob, PlayerJob newJob)
    {
        if (previousJob != newJob)
        {
            PlayerLayerSettings settings = setting.JobLayerMaskReadOnlyDic[newJob];
            foreach (var collider in avatorColliders)
            {
                collider.gameObject.layer = settings.Layer;
            }
        }
    }
    private void OnJobChangeHandle()
    {
        Job = Job switch
        {
            PlayerJob.Nothing => PlayerJob.Demon,
            PlayerJob.Demon => PlayerJob.Ghost,
            PlayerJob.Ghost => PlayerJob.Both,
            PlayerJob.Both => PlayerJob.Nothing,
            _ => throw new System.NotImplementedException(),
        };
        Debug.Log("Job changed to: " + Job, gameObject);
    }
#if UNITY_EDITOR
    private void Reset()
    {
        avatorColliders = GetComponentsInChildren<Collider>();
    }
#endif
}

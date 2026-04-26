using Syacapachi.Attribute;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SyncroPropaty : NetworkBehaviour
{
    [SerializeField] Collider[] avatorColliders;
    [SerializeField] JobSettingGenerator setting;
    [SerializeField,ReadOnly] PlayerJob playerjob = PlayerJob.Both;
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
    public PlayerJob Job
    {
        get => syncroJob.Value;
        set
        {
            if (IsOwner)
            {
                jobChangeLocalEvent.Invoke(value);
                playerjob = value;
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
        PlayerJob initialJob = IsHost ? PlayerJob.Demon : PlayerJob.Ghost;
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
        Debug.Log("SwitchJob action performed! Current job: " + Job);
        Job = Job switch
        {
            PlayerJob.Nothing => PlayerJob.Demon,
            PlayerJob.Demon => PlayerJob.Ghost,
            PlayerJob.Ghost => PlayerJob.Both,
            PlayerJob.Both => PlayerJob.Nothing,
            _ => throw new System.NotImplementedException(),
        };
        Debug.Log("Job changed to: " + Job);
    }
#if UNITY_EDITOR
    private void Reset()
    {
        avatorColliders = GetComponentsInChildren<Collider>();
    }
#endif
}

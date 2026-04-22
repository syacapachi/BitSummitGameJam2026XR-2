using Syacapachi.Attribute;
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
    public PlayerJob Job
    {
        get => syncroJob.Value;
        set
        {
            if (syncroJob.Value != value)
            {
                syncroJob.Value = value;
                playerjob = value;
                if (IsOwner)
                {   
                    jobChangeLocalEvent.Invoke(value);
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
                NetworkManager.SceneManager.OnLoadComplete += OnSceneLoaded;
            }
        }
    }
    private void OnSceneLoaded(ulong clientId,string SceneName,UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // ホストならHuman、クライアントならGhost
        PlayerJob initialJob = IsHost ? PlayerJob.Demon : PlayerJob.Ghost;
        Job = initialJob;
        jobChangeLocalEvent.Invoke(initialJob);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && IsDebugMode)
        {
            switchJobEvent.Unregister(OnJobChangeHandle);
            NetworkManager.SceneManager.OnLoadComplete -= OnSceneLoaded;
        }
    }
    private void OnJobChanged(PlayerJob previousJob, PlayerJob newJob)
    {
        if (previousJob != newJob)
        {
            PlayerLayerSettings settings = setting.JobLayerMaskDic[newJob];
            avatorCollider.layer = settings.Layer;
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
}

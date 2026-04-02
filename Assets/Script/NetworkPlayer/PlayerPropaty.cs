using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;

public class PlayerPropaty : MonoBehaviour
{
    [SerializeField] InputReciever inputReciever;
    [SerializeField] GameObject PlayerCollider;
    [SerializeField] Camera PlayerCamera;
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
        
        // 初期レイヤー設定
        if (jobToLayerMaskDic.TryGetValue(playerjob, out var initialSettings))
        {
            OnLayerChange(initialSettings);
        }
        else
        {
            Debug.LogError($"Initial PlayerJob {playerjob} not found in JobLayerMaskDic.");
        }
    }
    public event Action<PlayerJob> OnLocalJobChanged;
    [SerializeField] PlayerJob playerjob = PlayerJob.Both;
    
    public PlayerJob Job {
        get => playerjob; 
        set 
        {
            if (playerjob != value)
            {
                playerjob = value;
                PlayerLayerSettings settings = jobToLayerMaskDic[playerjob];
                
                OnLayerChange(settings);
                OnLocalJobChanged?.Invoke(playerjob);
            }
        }
    }
    

    /*
    NetworkVariable<PlayerJob> job =
    new NetworkVariable<PlayerJob>(
        PlayerJob.Nothing,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public PlayerJob Job
    {
        get => job.Value;
        set => job.Value = value;
    }
    */
    public bool CanSeeEnemy
    {
        get => playerjob != PlayerJob.Human;
    }
    [SerializeField] bool IsDebugMode = true;
    void OnEnable()
    {
        if (IsDebugMode)
        {
            inputReciever.OnSwirchJob += OnJobChangeHandle;
            OnLocalJobChanged?.Invoke(Job);
        }
    }
    void OnDisable()
    {
        if (IsDebugMode)
        {
            inputReciever.OnSwirchJob -= OnJobChangeHandle;
        }
    }
    /// <summary>
    /// 物理演算はサーバーで行われるため、クライアント側でレイヤーを変更しても意味がない。
    /// </summary>
    /// <param name="previousValue"></param>
    /// <param name="newValue"></param>
    private void OnLayerChange(PlayerLayerSettings newSetting)
    {
        PlayerCollider.layer = newSetting.Layer;
        // カメラのカリングマスクを更新
        PlayerCamera.cullingMask = newSetting.CullingMask;
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

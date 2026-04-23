using UnityEngine;
using System;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "JobSetting", menuName = "ScriptableObjects/JobSettingGenerator")]
public class JobSettingGenerator : ScriptableObject
{
    /// <summary>
    /// [SerializeField],publicはシーンをまたいでも値が残る。
    /// </summary>
    [SerializeField] List<PlayerLayerSettings> playerLayerSettingsList = new();
    /// <summary>
    /// シリアライズできないクラスは、参照が消える(そのシーンにこのScriptableObjectを使うScriptがない) -> 値が初期化される。
    /// </summary>
    private readonly Dictionary<PlayerJob, PlayerLayerSettings> JobToLayerMaskDic = new();
    public IReadOnlyDictionary<PlayerJob, PlayerLayerSettings> JobLayerMaskDic
    {
        get
        {
            //初期化を動的に
            InitDic();
            return JobToLayerMaskDic;
        }
    }
    private bool isInitialized = false;
    private void InitDic()
    {
        if (isInitialized) return;
        isInitialized = true;

        foreach (var settings in playerLayerSettingsList)
        {
            if (JobLayerMaskDic.ContainsKey(settings.TargetJob))
            {
                Debug.LogError($"Job {settings.TargetJob} is duplicated in JobSettingSO.");
                continue;
            }
            settings.LayerUpdate();
            JobToLayerMaskDic[settings.TargetJob] = settings;
        }
        var JobArray = System.Enum.GetValues(typeof(PlayerJob));
        // Enumに定義されているジョブがすべてJobSettingSOに定義されているか確認
        foreach (var job in JobArray)
        {
            // PlayerJobのビットフラグを考慮して、定義されていないジョブがあれば、ColliderLayerは0、CullingMaskはすべてのレイヤーの積集合に設定
            PlayerJob playerJob = (PlayerJob)job;
            if (!JobLayerMaskDic.ContainsKey(playerJob))
            {
                //とりあえず、ColliderLayerは0、CullingMaskはすべてのレイヤーの積集合に設定
                int colliderLayer = 0;
                LayerMask cullingMask = -1;
                PlayerJob attackableJob = PlayerJob.Nothing;
                LayerMask attackableLayer = 0;
                foreach (var mask in JobArray)
                {
                    if ((playerJob != (PlayerJob)mask) && ((playerJob & (PlayerJob)mask) != 0))
                    {
                        cullingMask &= JobLayerMaskDic[(PlayerJob)mask].CullingMask;
                        attackableJob |= JobLayerMaskDic[(PlayerJob)mask].AttackableJob;
                    }
                }
                JobToLayerMaskDic[playerJob] = new PlayerLayerSettings
                {
                    TargetJob = playerJob,
                    TargetColliderLayer = colliderLayer,
                    CullingMask = cullingMask,
                    AttackableJob = attackableJob,
                };
                Debug.LogWarning($"[{nameof(JobSettingGenerator)}]Job {playerJob} is not defined in JobSettingSO. ColliderLayer set to {colliderLayer}, CullingMask set to {cullingMask} (intersection of all defined jobs).");
            }
        }
    }
    private void OnEnable()
    {
        InitDic();
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        isInitialized = false;
    }
#endif
}

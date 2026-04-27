using UnityEngine;
using System;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "JobSetting", menuName = "ScriptableObjects/JobSettingGenerator")]
public class JobSettingGenerator : ScriptableObject
{
    /// <summary>
    /// [SerializeField],publicはシーンをまたいでも値が残る。
    /// </summary>
    [SerializeField] PlayerLayerSettings[] playerLayerSettingsList;
    /// <summary>
    /// シリアライズできないクラスは、参照が消える(そのシーンにこのScriptableObjectを使うScriptがない) -> 値が初期化される。
    /// </summary>
    private readonly Dictionary<PlayerJob, PlayerLayerSettings> JobToLayerMaskDic = new();
    public IReadOnlyDictionary<PlayerJob, PlayerLayerSettings> JobLayerMaskReadOnlyDic
    {
        get
        {
            //初期化を動的に
            InitDic();
            return JobToLayerMaskDic;
        }
    }
    private bool isInitialized = false;
    /// <summary>
    /// PlayerJobに対応するPlayerLayerSettingsを取得する。JobSettingSOに定義されていないジョブがあれば、ColliderLayerは0、CullingMaskはすべてのレイヤーの積集合に設定して返す。
    /// </summary>
    /// <param name="job"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    public bool TryGetPlayerLayerSettings(PlayerJob job, out PlayerLayerSettings settings)
    {
        InitDic();
        return JobToLayerMaskDic.TryGetValue(job, out settings);
    }
    private void InitDic()
    {
        if (isInitialized) return;
        isInitialized = true;

        foreach (var settings in playerLayerSettingsList)
        {
            if (JobLayerMaskReadOnlyDic.ContainsKey(settings.TargetJob))
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
            if (!JobLayerMaskReadOnlyDic.ContainsKey(playerJob))
            {
                //とりあえず、ColliderLayerは6(Avator)、CullingMaskはすべてのレイヤーの積集合に設定
                //攻撃可能なジョブは和集合で設定
                LayerMask colliderLayer = 1<<6;
                LayerMask cullingMask = -1;
                PlayerJob attackableJob = PlayerJob.Nothing;
                foreach (var mask in JobArray)
                {
                    if ((playerJob != (PlayerJob)mask) && ((playerJob & (PlayerJob)mask) != 0))
                    {
                        cullingMask &= JobLayerMaskReadOnlyDic[(PlayerJob)mask].CullingMask;
                        attackableJob |= JobLayerMaskReadOnlyDic[(PlayerJob)mask].AttackableJob;
                    }
                }
                var newSetting = new PlayerLayerSettings(colliderLayer, cullingMask, playerJob, attackableJob);
                newSetting.LayerUpdate();
                JobToLayerMaskDic[playerJob] = newSetting;
                Debug.LogWarning($"[{nameof(JobSettingGenerator)}]Job {playerJob} is not defined in JobSettingSO. ColliderLayer set to {colliderLayer}, CullingMask set to {cullingMask} (intersection of all defined jobs).");
            }
        }
    }
    /// <summary>
    /// ScriptableObjectのAwake()
    /// </summary>
    //private void OnEnable()
    //{
    //    InitDic();
    //}
#if UNITY_EDITOR
    private void OnValidate()
    {
        isInitialized = false;
    }
#endif
}

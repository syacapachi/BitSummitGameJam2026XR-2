using UnityEngine;
using System;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "JobSetting", menuName = "Scriptable Objects/JobSetting")]
public class JobSetting : ScriptableObject
{
    /// <summary>
    /// [SerializeField],publicはシーンをまたいでも値が残る。
    /// </summary>
    [SerializeField] JobSettingSO jobSettingSO;
    /// <summary>
    /// シリアライズできないクラスは、参照が消える(そのシーンにこのScriptableObjectを使うScriptがない) -> 値が初期化される。
    /// </summary>
    private readonly Dictionary<PlayerJob, PlayerLayerSettings> JobToLayerMaskDic = new();
    public IReadOnlyDictionary<PlayerJob, PlayerLayerSettings> JobLayerMaskDic => JobToLayerMaskDic;

    /// <summary>
    /// Scriptable のAwakeはこれ、シーン開始時に辞書を初期化
    /// </summary>
    void OnEnable()
    {
        foreach (var settings in jobSettingSO.PlayerLayerSettingsList)
        {
            if (JobLayerMaskDic.ContainsKey(settings.Job))
            {
                Debug.LogError($"Job {settings.Job} is duplicated in JobSettingSO.");
                continue;
            }
            settings.LayerUpdate();
            JobToLayerMaskDic[settings.Job] = settings;
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
                        attackableLayer |= JobLayerMaskDic[(PlayerJob)mask].AttackableLayer;
                    }
                }
                JobToLayerMaskDic[playerJob] = new PlayerLayerSettings
                {
                    Job = playerJob,
                    ColliderLayerMask = colliderLayer,
                    CullingMask = cullingMask,
                    AttackableJob = attackableJob,
                    AttackableLayer = attackableLayer,
                };
                Debug.LogWarning($"[JobManager]Job {playerJob} is not defined in JobSettingSO. ColliderLayer set to 0, CullingMask set to intersection of all defined jobs.");
            }
        }
    }
}

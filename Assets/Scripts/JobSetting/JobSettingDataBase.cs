using Syacapachi.Attribute;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JobSetting", menuName = "ScriptableObjects/JobSettingDataBase")]
public class JobSettingDataBase : ScriptableObject
{
    /// <summary>
    /// [SerializeField],publicはシーンをまたいでも値が残る。
    /// </summary>
    [SerializeReference,SerializeReferenceView] JobSettingBase[] playerLayerSettingsList;
    /// <summary>
    /// シリアライズできないクラスは、参照が消える(そのシーンにこのScriptableObjectを使うScriptがない) -> 値が初期化される。
    /// </summary>
    private readonly Dictionary<PlayerJob, JobSettingBase> JobToLayerMaskDic = new();
    public IReadOnlyDictionary<PlayerJob, JobSettingBase> JobLayerMaskReadOnlyDic
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
    /// PlayerJobに対応するEnemyJobSettingを取得する。JobSettingSOに定義されていないジョブがあれば、ColliderLayerは0、CullingMaskはすべてのレイヤーの積集合に設定して返す。
    /// </summary>
    /// <param name="job"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    public bool TryGetPlayerLayerSettings(PlayerJob job, out JobSettingBase settings)
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
            if (JobLayerMaskReadOnlyDic.ContainsKey(settings.SettingJob))
            {
                Debug.LogError($"Job {settings.SettingJob} is duplicated in JobSettingSO.");
                continue;
            }
            JobToLayerMaskDic[settings.SettingJob] = settings;
        }
        var JobArray = System.Enum.GetValues(typeof(PlayerJob));
        // Enumに定義されているジョブがすべてJobSettingSOに定義されているか確認
        foreach (var job in JobArray)
        {
            // PlayerJobのビットフラグを考慮して、定義されていないジョブがあれば、ColliderLayerは0、CullingMaskはすべてのレイヤーの積集合に設定
            PlayerJob playerJob = (PlayerJob)job;
            if (!JobLayerMaskReadOnlyDic.ContainsKey(playerJob))
            {
                //とりあえず、ColliderLayerは0(Default)
                //攻撃可能なジョブは和集合で設定
                int colliderLayer = 0;
                LayerMask cullingMask = -1;
                PlayerJob attackableJob = PlayerJob.Nothing;
                foreach (var mask in JobArray)
                {
                    if ((playerJob != (PlayerJob)mask) && ((playerJob & (PlayerJob)mask) != 0))
                    {
                        attackableJob |= JobLayerMaskReadOnlyDic[(PlayerJob)mask].AttackableJobs;
                    }
                }
                var newSetting = new JobSettingBase(playerJob, attackableJob, colliderLayer);
                JobToLayerMaskDic[playerJob] = newSetting;
                Debug.LogWarning($"[{nameof(JobSettingGenerator)}]Job {playerJob} is not defined in JobSettingSO. CollidersLayer set to {colliderLayer}, CullingMask set to {cullingMask} (intersection of all defined jobs).");
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

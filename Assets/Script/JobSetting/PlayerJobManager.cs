using System.Collections.Generic;
using UnityEngine;

public class PlayerJobManager : MonoBehaviour
{
    [SerializeField] JobSettingSO jobSettingSO;
    private Dictionary<PlayerJob, PlayerLayerSettings> JobToLayerMaskDic = new();
    public IReadOnlyDictionary<PlayerJob, PlayerLayerSettings> JobLayerMaskDic => JobToLayerMaskDic;
    void Awake()
    {
        JobToLayerMaskDic = new Dictionary<PlayerJob, PlayerLayerSettings>();
        foreach (var settings in jobSettingSO.PlayerLayerSettingsList)
        {
            if(JobLayerMaskDic.ContainsKey(settings.Job))
            {
                Debug.LogError($"Job {settings.Job} is duplicated in JobSettingSO.");
                continue;
            }
            JobToLayerMaskDic[settings.Job] = settings;
        }
        var JobArray = System.Enum.GetValues(typeof(PlayerJob));
        // Enumに定義されているジョブがすべてJobSettingSOに定義されているか確認
        foreach (var job in JobArray)
        {
            // PlayerJobのビットフラグを考慮して、定義されていないジョブがあれば、ColliderLayerは0、CullingMaskはすべてのレイヤーの積集合に設定
            PlayerJob playerJob = (PlayerJob)job;
            Debug.Log($"Checking job: {playerJob}");
            if (!JobLayerMaskDic.ContainsKey(playerJob))
            {
                //とりあえず、ColliderLayerは0、CullingMaskはすべてのレイヤーの積集合に設定
                int colliderLayer = 0;
                LayerMask cullingMask = -1;
                foreach (var mask in JobArray)
                {
                    if((playerJob != (PlayerJob)mask) && ((playerJob & (PlayerJob)mask) != 0))
                    {
                        cullingMask &= JobLayerMaskDic[(PlayerJob)mask].CullingMask;
                    }
                }
                JobToLayerMaskDic[playerJob] = new PlayerLayerSettings
                {
                    Job = playerJob,
                    ColliderLayerMask = colliderLayer,
                    CullingMask = cullingMask
                };
                Debug.LogWarning($"[JobManager]Job {playerJob} is not defined in JobSettingSO. ColliderLayer set to 0, CullingMask set to intersection of all defined jobs.");
            }
        }
        foreach (var kvp in JobLayerMaskDic)
        {
            Debug.Log($"Job: {kvp.Key}, ColliderLayer: {kvp.Value.ColliderLayerMask.value}, CullingMask: {kvp.Value.CullingMask.value}");
        }
    }
}

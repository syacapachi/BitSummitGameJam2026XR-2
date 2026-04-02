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
            JobToLayerMaskDic[settings.Job] = settings;
        }
    }
}

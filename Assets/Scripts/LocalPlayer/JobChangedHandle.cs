using UnityEngine;

public class JobChangedHandle : MonoBehaviour
{
    [SerializeField] GameObject PlayerCollider;
    [SerializeField] Camera PlayerCamera;
    [SerializeField] JobSettingGenerator setting;
    [Header("Subsucribe Event")]
    [SerializeField] PlayerJobEvent jobEvent;


    private void OnEnable()
    {
        jobEvent.Register(OnJobChanged);
    }
    private void OnDisable()
    {
        jobEvent.Unregister(OnJobChanged);
    }
    /// <summary>
    /// 物理演算はサーバーで行われるため、クライアント側でレイヤーを変更しても意味がない。
    /// </summary>
    /// <param name="previousValue"></param>
    /// <param name="newValue"></param>
    private void OnJobChanged(PlayerJob newJob)
    {
        if(setting.JobLayerMaskDic.TryGetValue(newJob, out var mask))
        {
            LayerChange(mask);
        }
        else
        {
            Debug.LogError($"[{nameof(JobChangedHandle)}]job{newJob} setting is null");
        }
    }
    private void LayerChange(PlayerLayerSettings newSetting)
    {
        PlayerCollider.layer = newSetting.Layer;
        // カメラのカリングマスクを更新
        PlayerCamera.cullingMask = newSetting.CullingMask;
    }
}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
[RequireComponent(typeof(LazyFollow))]
[RequireComponent(typeof(XRGrabInteractable))]
public class GrabableLazyFollowHelper : MonoBehaviour
{
    [SerializeField]
    LazyFollow m_LazyFollow;
    [SerializeField]
    XRGrabInteractable m_GrabInteractable;
    [SerializeField] Camera Camera;
    private void Awake()
    {
        m_LazyFollow ??= GetComponent<LazyFollow>();
        m_GrabInteractable ??= GetComponent<XRGrabInteractable>();
    }
    private void OnEnable()
    {
        m_GrabInteractable.selectEntered.AddListener(OnSelect);
        m_GrabInteractable.selectExited.AddListener(OnSelectExit);
    }
    private void OnDisable()
    {
        m_GrabInteractable.selectEntered.RemoveListener(OnSelect);
        m_GrabInteractable.selectExited.RemoveListener(OnSelectExit);
    }
    private void OnSelect(SelectEnterEventArgs args)
    {
        SetLazyFlowActive(false);
    }
    private void OnSelectExit(SelectExitEventArgs args)
    {
        SetTargetOffset();
        SetLazyFlowActive(true);
    }
    private void SetLazyFlowActive(bool active)
    {
        m_LazyFollow.enabled = active;
    }
    public void SetTargetOffset()
    {
        //カメラに対する相対座標を計算する。Vector3同士の引き算ではScaleが考慮されないので注意
        Vector3 point = Camera.transform.InverseTransformPoint(this.transform.position);
        if(point.z < 0f) point.z = -point.z;
        m_LazyFollow.targetOffset = point;
    }
    private void Reset()
    {
        m_LazyFollow = GetComponent<LazyFollow>();
        m_GrabInteractable = GetComponent<XRGrabInteractable>();
    }
}

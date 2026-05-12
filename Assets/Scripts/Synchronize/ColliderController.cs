using Syacapachi.Attribute;
using UnityEngine;
public class ColliderController : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] Collider[] m_Collider;
    [OnInspectorButton]
    void SetColliderTrigger(bool isTriger)
    {
        foreach (Collider collider in m_Collider)
        {
            collider.isTrigger = isTriger;
        }
    }
    [OnInspectorButton]
    void SetColliderEnable(bool enable)
    {
        foreach (Collider collider in m_Collider)
        {
            collider.enabled = enable;
        }
    }
    private void Reset()
    {
        m_Collider = GetComponentsInChildren<Collider>();
    }
#endif
}

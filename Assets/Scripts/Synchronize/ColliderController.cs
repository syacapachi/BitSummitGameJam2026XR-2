using Syacapachi.Attribute;
using UnityEngine;
public class ColliderController : MonoBehaviour
{
#if UNITY_EDITOR
    private Collider[] m_Collider;
    [OnInspectorButton]
    void SetColliderTrigger(bool isTriger)
    {
        foreach (Collider collider in m_Collider)
        {
            collider.isTrigger = isTriger;
        }
    }
    private void Reset()
    {
        m_Collider = GetComponentsInChildren<Collider>();
    }
#endif
}

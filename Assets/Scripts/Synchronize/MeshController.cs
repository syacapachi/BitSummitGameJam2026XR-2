using Syacapachi.Attribute;
using Unity.Netcode;
using UnityEngine;
public class MeshController : NetworkBehaviour
{
    [SerializeField, Layer] int ownerLayer = 1;
    [SerializeField, Layer] int noOwnerLayer = 1;
    [SerializeField] Renderer[] m_Renderer;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            foreach (var renderer in m_Renderer)
            {
                renderer.gameObject.layer = ownerLayer;
            }
        }
        else
        {
            foreach (var renderer in m_Renderer)
            {
                renderer.gameObject.layer = noOwnerLayer;
            }
        }
    }
#if UNITY_EDITOR
    private void Reset()
    {
        Find();
    }
    [OnInspectorButton]
    private void Find()
    {
        m_Renderer = GetComponentsInChildren<Renderer>();
    }
#endif
}
using Syacapachi.Attribute;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
public class MeshController : NetworkBehaviour
{
    [SerializeField, SingleFlagOnly] LayerMask layer; 
    [SerializeField] List<Renderer> m_Renderer = new();

    public override void OnNetworkSpawn()
    {
        int value = 0;
        for(int i = 0; i < 32; i++)
        {
            if((layer & 1 << i) != 0)
            {
                value = i;
                break;
            }
        }
        if (IsOwner)
        {
            foreach (var renderer in m_Renderer)
            {
                renderer.gameObject.layer = value;
            }
        }
    }
#if UNITY_EDITOR
    private void Reset()
    {
        FindAndApply();
    }
    [OnInspectorButton]
    private void FindAndApply()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        m_Renderer = renderers.ToList();
    }
#endif
}
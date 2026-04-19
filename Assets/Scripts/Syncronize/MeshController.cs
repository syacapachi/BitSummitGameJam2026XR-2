using Syacapachi.Attribute;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
public class MeshController : NetworkBehaviour
{
    [SerializeField] bool meshEnsble = false;
    [SerializeField] List<Renderer> m_Renderer = new();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            foreach (var renderer in m_Renderer)
            {
                renderer.enabled = meshEnsble;
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
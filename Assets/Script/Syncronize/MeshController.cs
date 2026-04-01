using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Netcode.Extensions;
public class MeshController : NetworkBehaviour
{
    [SerializeField] List<Renderer> m_Renderer = new();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            foreach (var renderer in m_Renderer)
            {
                renderer.enabled = false;
            }
        }
    }
    private void Reset()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        m_Renderer = renderers.ToList();
    }
}
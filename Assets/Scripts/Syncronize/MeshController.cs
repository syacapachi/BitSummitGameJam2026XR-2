using Syacapachi.Attribute;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
public class MeshController : NetworkBehaviour
{
    [SerializeField, SingleFlagOnly] LayerMask ownerLayer = 1;
    [SerializeField, SingleFlagOnly] LayerMask noOwnerLayer = 1;
    [SerializeField] Renderer[] m_Renderer;
    private int ownerLayerValue;
    private int noOwnerLayerValue;

    int GetSingleLayerValue(LayerMask layerMask)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask.value & (1 << i)) != 0)
            {
                return i;
            }
        }
        return -1; // 有効なレイヤーが見つからない場合
    }

    public override void OnNetworkSpawn()
    {
        ownerLayerValue = GetSingleLayerValue(ownerLayer);
        noOwnerLayerValue = GetSingleLayerValue(noOwnerLayer);
        if (IsOwner)
        {
            foreach (var renderer in m_Renderer)
            {
                renderer.gameObject.layer = ownerLayerValue;
            }
        }
        else
        {
            foreach (var renderer in m_Renderer)
            {
                renderer.gameObject.layer = noOwnerLayerValue;
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
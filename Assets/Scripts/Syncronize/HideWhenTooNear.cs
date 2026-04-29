using Syacapachi.Attribute;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideWhenTooNear : MonoBehaviour
{
    [Header("Playerを判定するためのCollider")]
    [SerializeField] Collider[] myColliders;
    [Header("Playerの頭のTransform")]
    [SerializeField] Camera playerCamera;
    [Header("Playerを判定するためのLayerMask")]
    [SerializeField] LayerMask playerLayer;
    [Header("Playerとの距離をどれくらいの頻度でチェックするか")]
    [SerializeField] float checkInterval = 0.1f;
    [Header("Playerがどれくらい近いときにRendererを消すか")]
    [SerializeField] float hideThreshold = 0.2f;
    private readonly HashSet<Collider> myColliderSet = new();
    private readonly Dictionary<Collider,Renderer[]> otherRendererCache = new();
    //OverlapSphereNonAllocのための配列
    readonly Collider[] hits = new Collider[10];
    private void Awake()
    {
        foreach (var col in myColliders)
        {
            myColliderSet.Add(col);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(PlayerCheckCoruinte());
    }
    //Update()でやると重いのでコルーチンで定期的にPlayerとの距離をチェックする
    IEnumerator PlayerCheckCoruinte()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            CheckPlayer();
            yield return wait;
        }
    }

    void CheckPlayer()
    { 
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 0.5f, hits, playerLayer);
        for (int i = 0; i < hitCount; i++)
        {
            var other = hits[i];
            if (myColliderSet.Contains(other)) continue;
            if(!other.TryGetComponent<PlayerCollider>(out var playerCollider)) continue;
            if (ManagerLocator.Instance.AllPlayerManager.NetworkOwnerPlayer.OwnerClientId == playerCollider.OwnerClientId) continue;
            float distance = Vector3.Distance(playerCamera.transform.position, other.transform.position);
            bool hide = distance > hideThreshold;
            SetRendererVisible(other, !hide);
        }
        
    }
    void SetRendererVisible(Collider other, bool visible)
    {
        if (!otherRendererCache.TryGetValue(other, out var renderers))
        {
            renderers = other.GetComponentsInChildren<Renderer>();
            otherRendererCache[other] = renderers;
        }
        foreach (var renderer in renderers)
        {
            renderer.enabled = visible;
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
        myColliders = GetComponentsInChildren<Collider>();
        playerCamera = Camera.main;
    }
#endif
}

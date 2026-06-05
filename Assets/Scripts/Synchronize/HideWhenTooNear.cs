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
    [Header("Playerとの距離をどれくらいの頻度でチェックするか(重い処理なので、0.5秒以上)")]
    [SerializeField] float checkInterval = 0.1f;
    [Header("Player判別距離")]
    [SerializeField] float checkDistance = 1.0f;
    [Header("Playerがどれくらい近いときにRendererを消すか")]
    [SerializeField] float hideThreshold = 0.8f;
    private readonly HashSet<Collider> myColliderSet = new();
    private readonly Dictionary<PlayerCollider, Renderer[]> otherRendererCache = new();
    //OverlapSphereNonAllocのための配列
    readonly Collider[] hits = new Collider[10];
    readonly HashSet<PlayerCollider> lastHitsColliders = new();
    private bool isActiveSearch = false;
    private void Awake()
    {
        foreach (var col in myColliders)
        {
            myColliderSet.Add(col);
        }
    }

    private void OnEnable()
    {
        isActiveSearch = true;
        StartCoroutine(PlayerCheckCoruinte());
    }
    private void OnDisable()
    {
        isActiveSearch = false;
    }
    //Update()でやると重いのでコルーチンで定期的にPlayerとの距離をチェックする
    IEnumerator PlayerCheckCoruinte()
    {
        while (isActiveSearch)
        {
            CheckPlayer();
            float timer = 0f;
            while(timer < checkInterval)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    void CheckPlayer()
    {
        //直前を消す
        foreach (var col in lastHitsColliders)
        {
            SetRendererVisible(col, true);
        }
        lastHitsColliders.Clear();
        var locator = ManagerLocator.Instance;
        if (locator == null || locator.AllPlayerManager == null || locator.AllPlayerManager.NetworkOwnerPlayer == null) return;
        
        int hitCount = Physics.OverlapSphereNonAlloc(playerCamera.transform.position, checkDistance, hits, playerLayer);
        for (int i = 0; i < hitCount; i++)
        {
            var other = hits[i];
            if (myColliderSet.Contains(other)) continue;
            if (!other.TryGetComponent<PlayerCollider>(out var playerCollider)) continue;
            if (locator.AllPlayerManager.NetworkOwnerPlayer.OwnerClientId == playerCollider.OwnerClientId) continue;
            lastHitsColliders.Add(playerCollider);
            float distance = Vector3.Distance(playerCamera.transform.position, other.transform.position);
            //Debug.Log($"found collider distance = {distance}", playerCollider);
            SetRendererVisible(playerCollider, !(distance < hideThreshold));
        }
    }
    void SetRendererVisible(PlayerCollider other, bool visible)
    {
        if (!otherRendererCache.TryGetValue(other, out var renderers))
        {
            otherRendererCache[other] = other.Renderers;
            renderers = other.Renderers;
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

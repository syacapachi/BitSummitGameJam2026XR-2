using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DamageIndicatorUI : MonoBehaviour
{
    [Header("Event")]
    [SerializeField]
    DamageIndicatorEvent damageIndicatorEvent;

    [Header("Reference")]
    [SerializeField]
    Transform playerHead;

    [SerializeField]
    RectTransform arcImage;

    [Header("Setting")]
    [SerializeField]
    float displayTime = 3f;

    Transform target;
    float remainTime;

    private void Awake()
    {
        arcImage.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        Debug.Log("DamageIndicatorUI Enable");
        damageIndicatorEvent.Register(OnDamage);
    }

    private void OnDisable()
    {
        damageIndicatorEvent.Unregister(OnDamage);
    }

    private void OnDamage(
        DamageIndicatorInfo info)
    {
        Debug.Log(
    $"OnDamage {info.EnemyNetworkObjectId}"
);
        if (!NetworkManager.Singleton
            .SpawnManager
            .SpawnedObjects
            .TryGetValue(
                info.EnemyNetworkObjectId,
                out var obj))
        {
            Debug.LogError(
    $"Enemy not found : {info.EnemyNetworkObjectId}"
);
            return;
        }

        target = obj.transform;
        remainTime = displayTime;

        arcImage.gameObject.SetActive(true);

        UpdateIndicatorDirection();
    }

    private void Update()
    {
        if (remainTime <= 0f)
        {
            HideIndicator();
            return;
        }

        remainTime -= Time.deltaTime;

        if (target == null)
        {
            HideIndicator();
            return;
        }

        UpdateIndicatorDirection();
    }

    private void UpdateIndicatorDirection()
    {
        Vector3 toEnemy =
            target.position -
            playerHead.position;

        // ‚‚³‚Í–³Ž‹
        toEnemy.y = 0f;

        if (toEnemy.sqrMagnitude < 0.0001f)
            return;

        float angle =
            Vector3.SignedAngle(
                playerHead.forward,
                toEnemy.normalized,
                Vector3.up);

        // ArcImage ‚Ì‰ŠúˆÊ’u‚ª^ã(Ü)
        arcImage.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                -angle
            );
    }

    private void HideIndicator()
    {
        target = null;
        remainTime = 0f;

        arcImage.gameObject.SetActive(false);

        arcImage.localRotation =
            Quaternion.identity;
    }

}

using UnityEngine;
using Unity.Netcode;
using System.Collections;
public class BombAction : NetworkBehaviour,IDamageSender
{
    [Header("Bomb Settings")]
    [Tooltip("爆発時間")]
    [SerializeField] float explosionDelay = 5f;
    [Tooltip("爆発の最大半径")]
    [SerializeField] float explosionRadiusValue = 5f;
    [Tooltip("爆発の持続時間")]
    [SerializeField] float explosionTime = 0.5f;
    [Tooltip("爆発のダメージ")]
    [SerializeField] int explosionDamage = 50;
    [SerializeField] SphereCollider  explosionCollider;
    [SerializeField] NetworkVariable<float> timer = new NetworkVariable<float>(
        5f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    [SerializeField] NetworkVariable<bool> isExploded = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    [SerializeField] NetworkVariable<float> explosionRadiusNetwork = new NetworkVariable<float>(
        0.5f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private Camera mainCamera;
    public GameObject GameObject => this.gameObject;

    public float Damage => explosionDamage;
    void Start()
    {
        mainCamera = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            timer.Value = explosionDelay;
        }
        // 爆発の当たり判定は爆発時に有効にするため、最初は無効にしておく
        explosionCollider.enabled = false;

        isExploded.OnValueChanged += OnBombExploded;
        explosionRadiusNetwork.OnValueChanged += OnColliderRadiusChanged;
        Debug.Log($"BombAction spawned on network. owner:{OwnerClientId},NetworkId = {NetworkObjectId}");
    }
    public override void OnNetworkDespawn()
    {
        isExploded.OnValueChanged -= OnBombExploded;
        explosionRadiusNetwork.OnValueChanged -= OnColliderRadiusChanged;
        Debug.Log($"BombAction despawned on network. owner:{OwnerClientId},NetworkId = {NetworkObjectId}");
    }
    private void Update()
    {
        if (IsServer)
        {
            if(isExploded.Value)
            {
                return;
            }
            timer.Value -= Time.deltaTime;
            if (timer.Value <= 0f)
            {
                StartCoroutine(ExplodeCoroutine());
            }
        }
    }
    
    private IEnumerator ExplodeCoroutine()
    {
        isExploded.Value = true;
        for(float explosionTimer = 0f; explosionTimer < explosionTime; explosionTimer+=Time.deltaTime)
        {
            explosionRadiusNetwork.Value = Mathf.Lerp(0f, explosionRadiusValue, explosionTimer / explosionTime);
            yield return null;
        }
        explosionCollider.enabled = false;
        NetworkObject.Despawn();
    }
    private void OnBombExploded(bool oldValue, bool newValue)
    {
        if(oldValue != newValue)
            explosionCollider.enabled = newValue;
    }
    private void OnColliderRadiusChanged(float oldValue, float newValue)
    {
        explosionCollider.radius = newValue;
    }
    private void OnCollisionEnter(Collision other)
    {
        if (IsServer && isExploded.Value)
        {
            // 爆発の当たり判定に入ったオブジェクトに対する処理をここに追加
            GameObject hitObject = other.gameObject;
            Debug.Log($"Object {hitObject} entered explosion radius!");
            if(hitObject.TryGetComponent<IDamageReciever>(out var damageReciever))
            {
                SendDamage(damageReciever, explosionDamage);
            }
        }
    }
    private void OnGUI()
    {
        Vector3 position = mainCamera.WorldToScreenPoint(transform.position);
        GUI.Label(new Rect(position.x, Screen.height - position.y, 100, 20), $"Timer: {timer.Value:F1}");
    }
    public void SendDamage(IDamageReciever reciever, float damage)
    {
        if(IsServer)
            reciever.TakeDamage(this,damage);
    }
}

using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;
/// <summary>
/// プレイヤークラスのルートコンポーネント。プレイヤーに関連するすべてのコンポーネントを管理するためのクラス。プレイヤーの入力、キャラクターコントロール、ヘルス、プロパティ、カメラ設定などを統括する役割を持つ。
/// </summary>
public class NetworkPlayerRoot : NetworkBehaviour
{
    /// <summary>
    /// オブジェクトの参照
    /// </summary>
    public Transform playerRoot;
    public PlayerHealth playerHealth;
    public SyncroPropaty propaty; 
    public PlayerItemControll itemControll;
    public PlayerStats stats; // プレイヤーの統計情報を管理するコンポーネント
    private bool isXREnabled = false;
    private PlayerManager playerManager;
    public bool IsXREnabled => isXREnabled;
    /// <summary>
    /// OnNetworkSpawn()の後
    /// </summary>
    protected override void OnNetworkPostSpawn()
    {
        ResistPlayerManager();
        NetworkManager.SceneManager.OnLoadComplete += OnSceneLoad;
        NetworkManager.SceneManager.OnUnloadComplete += OnUnloadComplete;
    }

    private void OnUnloadComplete(ulong clientId, string sceneName)
    {
        
    }

    private void OnSceneLoad(ulong clientId, string SceneName, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ResistPlayerManager();
    }
    private void ResistPlayerManager()
    {
        var Locator = ManagerLocator.Instance;
        if(Locator == null )
        {
            Debug.LogError("ManagerLocator is null");
            return;
        }
        playerManager = Locator.AllPlayerManager;
        if (playerManager == null)
        {
            Debug.LogError("Player Manager is null");
            return;
        }
        playerManager.ResistPlayer(this);
        if (IsOwner)
        {
            playerRoot.gameObject.name = $"You Player_{OwnerClientId}";
            playerManager.ResistOwner(this);
            isXREnabled = XRSettings.isDeviceActive;
        }
        else
        {
            //オーナーでないクライアントでは、PlayerInputを無効にする。これにより、他のプレイヤーの入力が誤って処理されるのを防ぐ。
            playerRoot.gameObject.name = $"Player_{OwnerClientId}";
        }
    }
    public override void OnNetworkDespawn()
    {
        playerManager.UnResistPlayer(this);
        if (IsOwner)
        {
            playerManager.UnResistOwner(this);
        }
        NetworkManager.SceneManager.OnLoadComplete -= OnSceneLoad;
        NetworkManager.SceneManager.OnUnloadComplete -= OnUnloadComplete;
    }
}

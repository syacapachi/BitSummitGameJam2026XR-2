using System.Collections;
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
    private PlayerManager playerManager;
    /// <summary>
    /// OnNetworkSpawn()の後
    /// </summary>
    protected override void OnNetworkPostSpawn()
    {
        StartCoroutine(ResistPlayerManager());
        NetworkManager.SceneManager.OnLoadComplete += OnSceneLoad;
        NetworkManager.SceneManager.OnUnloadComplete += OnUnloadComplete;
    }

    private void OnUnloadComplete(ulong clientId, string sceneName)
    {
        
    }

    private void OnSceneLoad(ulong clientId, string SceneName, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        StartCoroutine(ResistPlayerManager());
    }
    private IEnumerator ResistPlayerManager()
    {
        var Locator = ManagerLocator.Instance;
        while (Locator == null || Locator.AllPlayerManager == null)
        {
            yield return null;
        }
        playerManager = Locator.AllPlayerManager;
        playerManager.ResistPlayer(this);
        if (IsOwner)
        {
            playerRoot.gameObject.name = $"You Player_{OwnerClientId}";
            playerManager.ResistOwner(this);
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

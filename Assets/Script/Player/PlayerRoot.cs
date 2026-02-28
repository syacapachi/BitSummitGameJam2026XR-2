using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// プレイヤークラスのルートコンポーネント。プレイヤーに関連するすべてのコンポーネントを管理するためのクラス。プレイヤーの入力、キャラクターコントロール、ヘルス、プロパティ、カメラ設定などを統括する役割を持つ。
/// </summary>
public class PlayerRoot : NetworkBehaviour
{
    /// <summary>
    /// オブジェクトの参照
    /// </summary>
    public GameObject playerPrefab;
    public Transform playerRoot;
    public Canvas playerCanvas;
    public PlayerInput playerInput;
    public CharactorControll characterControll;
    public PlayerHealth playerHealth;
    public PlayerPropaty propaty;
    public CameraSetting cameraSetting;

    public override void OnNetworkSpawn()
    {
        PlayerManager player = ManagerLocator.Instance.PlayerManager;
        player.ResistPlayer(this);
        if (IsOwner)
        {
            playerPrefab.name = $"You Player_{OwnerClientId}";
            player.ResistOwner(this);
            //オーナーのクライアントでPlayerInputを有効にする。これにより、プレイヤーが入力を受け取れるようになる。
            playerInput.enabled = true;
            playerInput.ActivateInput();
        }
        else
        {
            //オーナーでないクライアントでは、PlayerInputを無効にする。これにより、他のプレイヤーの入力が誤って処理されるのを防ぐ。
            playerPrefab.name = $"Player_{OwnerClientId}";
            playerInput.enabled = false;
            playerCanvas.enabled = false;
        }
    }
    public override void OnNetworkDespawn()
    {
        PlayerManager player = ManagerLocator.Instance.PlayerManager;
        player.UnResistPlayer(this);
        if (IsOwner)
        {
            player.UnResistOwner(this);
            playerInput.DeactivateInput();
            playerInput.enabled = false;
        }
    }
}

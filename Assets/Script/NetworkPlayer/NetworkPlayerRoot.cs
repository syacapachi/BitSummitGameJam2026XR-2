using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public PlayerPropaty propaty;
    public PlayerItemControll itemControll;
    public PlayerStats stats; // プレイヤーの統計情報を管理するコンポーネント
    private bool isXREnabled = false;
    private PlayerManager playerManager;
    public bool IsXREnabled => isXREnabled;
    public override void OnNetworkSpawn()
    {
        playerManager = ManagerLocator.Instance.AllPlayerManager;
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
    }
}

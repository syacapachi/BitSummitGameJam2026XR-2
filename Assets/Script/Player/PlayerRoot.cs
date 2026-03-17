using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
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
            playerPrefab.name = $"You Player_{OwnerClientId}";
            playerManager.ResistOwner(this);
            isXREnabled = XRSettings.isDeviceActive;
            if (isXREnabled)
            {
                characterControll.enabled = false;
                cameraSetting.enabled = false;
            }
            SetActionEnable();


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
        playerManager.UnResistPlayer(this);
        if (IsOwner)
        {
            playerManager.UnResistOwner(this);
            SetActionDisable();
            playerInput.enabled = false;
        }
    }
    private void SetActionEnable()
    {
        playerInput.actions.Enable();
    }
    private void SetActionDisable() 
    {
        playerInput.actions.Disable();
    }
}

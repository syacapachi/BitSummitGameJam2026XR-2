using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
public class PlayerManager : NetworkBehaviour
{
    /// <summary>
    /// プレイヤーの管理を行うシングルトンインスタンス。これにより、ゲーム内のどこからでもプレイヤーの管理にアクセスできるようになる。
    /// </summary>
    public static PlayerManager Instance { get; private set; }
    public GameObject playerPrefab;
    public Transform playerRoot;
    public PlayerInput playerInput;
    public CharactorControll characterControll;
    public PlayerHealth playerHealth;
    public CameraSetting cameraSetting;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple instances of PlayerManager detected. This should not happen in a properly designed singleton pattern.");
            }
            Instance = this;
            playerPrefab.name = $"You Player_{OwnerClientId}";

            //オーナーのクライアントでPlayerInputを有効にする。これにより、プレイヤーが入力を受け取れるようになる。
            playerInput.enabled = true;
            playerInput.ActivateInput();
        }
        else
        {
            //オーナーでないクライアントでは、PlayerInputを無効にする。これにより、他のプレイヤーの入力が誤って処理されるのを防ぐ。
            playerPrefab.name = $"Player_{OwnerClientId}";
            playerInput.enabled = false;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner && Instance == this)
        {
            Instance = null;
            playerInput.DeactivateInput();
            playerInput.enabled = false;
        }
    }
}

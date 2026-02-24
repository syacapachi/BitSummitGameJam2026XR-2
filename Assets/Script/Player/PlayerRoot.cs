using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerRoot : NetworkBehaviour
{
    public GameObject playerPrefab;
    public Transform playerRoot;
    public PlayerInput playerInput;
    public CharactorControll characterControll;
    public PlayerHealth playerHealth;
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
        }
    }
    public override void OnNetworkDespawn()
    {
        ManagerLocator.Instance.PlayerManager.UnResistPlayer(this);
        if (IsOwner)
        {
            playerInput.DeactivateInput();
            playerInput.enabled = false;
        }
    }
}

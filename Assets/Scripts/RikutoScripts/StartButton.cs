using UnityEngine;
using Unity.Netcode;

public class StartButton : NetworkBehaviour
{
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject humanUI;
    [SerializeField] GameObject ghostUI;

    [SerializeField] GameStateEvent gameStateEvent;

    private void OnEnable()
    {
        gameStateEvent.Register(OnGameStateChange);
    }
    private void OnDisable()
    {
        gameStateEvent.Unregister(OnGameStateChange);
    }
    public void SelectHuman()
    {
        if(IsServer) return;
        ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot.Propaty.Job = PlayerJob.Human;
        humanUI.SetActive(false);
        Debug.Log("Human");
    }

    public void SelectGhost()
    {
        if (IsServer) return;
        ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot.Propaty.Job = PlayerJob.Ghost;
        ghostUI.SetActive(false);
        Debug.Log("Ghost");
    }
    private void OnGameStateChange(GameState state)
    {
        switch (state)
        {
            case GameState.GameClear:
                GameEndHandle();
                break;
            case GameState.GameOver:
                GameEndHandle(); break;
            default: break;
        }
    }
    public void SelectStartGame()
    {
        StartGameRpc();
    }
    private void GameEndHandle()
    {
        HideUIRpc(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!other.CompareTag("Bullet")) return;

        StartGameRpc();
    }

    [Rpc(SendTo.Server)]
    void StartGameRpc()
    {
        Debug.Log("[Start Game Rpc]");
        ManagerLocator.Instance.AllGameManager.StartGame();
        HideUIRpc(false);
    }

    [Rpc(SendTo.Everyone)]
    void HideUIRpc(bool enabled)
    {
        startUI.SetActive(enabled);
        humanUI.SetActive(enabled);
        ghostUI.SetActive(enabled);
    }
}
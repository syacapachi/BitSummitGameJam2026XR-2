using UnityEngine;
using Unity.Netcode;

public class StartButton : NetworkBehaviour
{
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject resetUI;

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
        Debug.Log("Human");
    }

    public void SelectGhost()
    {
        if (IsServer) return;
        ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot.Propaty.Job = PlayerJob.Ghost;
        Debug.Log("Ghost");
    }
    private void OnGameStateChange(GameState state)
    {
        switch (state)
        {
            case GameState.Initializing:
                OnGameInitialize(); break;

            case GameState.GameClear:
                GameEndHandle();break;
            case GameState.GameOver:
                GameEndHandle(); break;
            default: break;
        }
    }
    public void SelectStartGame()
    {
        StartGameRpc();
    }
    public void SelectResetGame()
    {
        ResetGameRpc();
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
        ManagerLocator.Instance.AllGameManager.StartGameServerOnly();
        startUI.SetActive(false);
    }
    [Rpc(SendTo.Server)]
    void ResetGameRpc()
    {
        Debug.Log("[Start Game Rpc]");
        ManagerLocator.Instance.AllGameManager.ResetGameServerOnly();
        resetUI.SetActive(false);
    }
    private void GameEndHandle()
    {
        resetUI.SetActive(true);
    }
    private void OnGameInitialize()
    {
        startUI.SetActive(true);
    }
}
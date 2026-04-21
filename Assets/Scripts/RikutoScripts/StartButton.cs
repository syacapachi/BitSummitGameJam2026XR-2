using UnityEngine;
using Unity.Netcode;

public class StartButton : NetworkBehaviour
{
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject resetUI;
    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateEvent;
    [Header("Publich Event")]
    [SerializeField] PlayerJobEvent playerJobEvent;

    /*
     シーン遷移でUIの状態をリセットするため、StartからOnNetworkSpawnに移動
    private void Start()
    {
        startUI.SetActive(false);
        resetUI.SetActive(false);
    }
    */
    public override void OnNetworkSpawn()
    {
        startUI.SetActive(true);
        resetUI.SetActive(false);
    }
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
        playerJobEvent.Invoke(PlayerJob.Human);
        Debug.Log("Human");
    }

    public void SelectGhost()
    {
        if (IsServer) return;
        playerJobEvent.Invoke(PlayerJob.Ghost);
        Debug.Log("Ghost");
    }
    private void OnGameStateChange(GameState state)
    {
        switch (state)
        {
            case GameState.Initializing:
                OnGameInitialize(); break;
            case GameState.Playing:
                startUI.SetActive(false); break;
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

    [Rpc(SendTo.Server)]
    void StartGameRpc()
    {
        Debug.Log("[Start Game Rpc]");
        ManagerLocator.Instance.AllGameManager.StartGameServerOnly();
        //クライアントで消えてない
        //startUI.SetActive(false);
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
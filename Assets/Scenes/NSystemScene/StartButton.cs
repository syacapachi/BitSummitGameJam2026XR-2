using UnityEngine;
using Unity.Netcode;

public class StartButton : NetworkBehaviour
{
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject humanUI;
    [SerializeField] GameObject ghostUI;

    private void Start()
    {
        ManagerLocator.Instance.AllGameManager.OnGameEndRpc += data => GameEndHandle();
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        ManagerLocator.Instance.AllGameManager.OnGameEndRpc -= data => GameEndHandle();
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
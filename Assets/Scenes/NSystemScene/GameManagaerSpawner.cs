using UnityEngine;
using Unity.Netcode;

public class GameManagerSpawner : NetworkBehaviour
{
    [SerializeField] GameObject gameManagerPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        Debug.Log("Spawner OnNetworkSpawn");
        if(ManagerLocator.Instance.AllGameManager != null)
        {
            GameObject obj = Instantiate(gameManagerPrefab);
            Debug.LogWarning("GameManager already exists. Spawner will not create another one.");
            Debug.Log("GameManager Instance : " + obj.name);

            var netObj = obj.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogError("NetworkObject missing!");
                return;
            }
            netObj.Spawn();

            Debug.Log("GameManager Spawned id=" + netObj.NetworkObjectId);
        }
    }
}
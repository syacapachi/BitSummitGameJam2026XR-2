using UnityEngine;
using Unity.Netcode;
public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] GameObject playerPrefab;
    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
    }
    private void OnClientConnectedCallback(ulong clientID)
    {
        if(OwnerClientId != clientID)
        {
            GameObject obj = Instantiate(playerPrefab);
            NetworkObject nobj = obj.GetComponent<NetworkObject>();
            nobj.SpawnAsPlayerObject(clientID);
        }
    }
}

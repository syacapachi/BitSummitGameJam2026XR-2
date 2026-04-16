using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class ServerAction : NetworkBehaviour
{
    [SerializeField] UnityTransport transport;
    // Start is called before the first frame update
    void Start()
    {

    }
    // Net上でオブジェクトがスポーンしたときに呼ばれる
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log("ServerAction spawned on server/host.");
        }
    }
    //ネット上でオブジェクトがデスポーンしたときに呼ばれる
    public override void OnNetworkDespawn()
    {
        NetworkManager instance = NetworkManager.Singleton;
        if (instance != null)
        {
            if (IsServer)
            {
                instance.OnClientConnectedCallback -= HandleClientConnected;
            }
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
    }
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 150, 25), IsServer ? "Running on Server/Host" : "Running on Client");
        GUI.Label(new Rect(10, 40, 300, 25), $"NetworkObjectId: {NetworkObjectId}");
        GUI.Label(new Rect(10, 70, 300, 25), $"NetworkBehaviourId: {NetworkBehaviourId}");
        GUI.Label(new Rect(10, 100, 300, 25), $"Owner: {OwnerClientId}");
        //GUI.Label(new Rect(10, 70, 300, 25), $"IPAddress: {transport.}");
    }

}

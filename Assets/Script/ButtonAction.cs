using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class Scripts : MonoBehaviour
{
    [SerializeField] Button ServerButton;
    [SerializeField] Button HostButton;
    [SerializeField] Button ClientButton;
    [SerializeField] Button ExitButton;
    private bool isNetworkStarted = false;
    private void Start()
    {
        ServerButton.onClick.AddListener(OnStartServer);
        HostButton.onClick.AddListener(OnStartHost);
        ClientButton.onClick.AddListener(OnStartClient);
        ExitButton.onClick.AddListener(OnExitNetwork);

        ServerButton.gameObject.SetActive(true);
        HostButton.gameObject.SetActive(true);
        ClientButton.gameObject.SetActive(true);
        ExitButton.gameObject.SetActive(false);
    }
    private void OnStartServer()
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("Server Started");
        OnNetworkStart();
    }
    private void OnStartHost()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("Host Started");
        OnNetworkStart();
    }
    private void OnStartClient()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client Started");
        OnNetworkStart();
    }
    private void OnExitNetwork()
    {
        if (isNetworkStarted)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("Server Stopped");
            }
            else if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("Host Stopped");
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("Client Stopped");
            }
            isNetworkStarted = false;
            ServerButton.gameObject.SetActive(true);
            HostButton.gameObject.SetActive(true);
            ClientButton.gameObject.SetActive(true);
            ExitButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Network is not started. Cannot exit network.");
        }
    }
    public void OnNetworkStart()
    {
        isNetworkStarted = true;
        ServerButton.gameObject.SetActive(false);
        HostButton.gameObject.SetActive(false);
        ClientButton.gameObject.SetActive(false);
        ExitButton.gameObject.SetActive(true);
    }
}

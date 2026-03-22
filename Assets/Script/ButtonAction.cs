using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Scripts : MonoBehaviour
{
    [SerializeField] Button ServerButton;
    [SerializeField] Button HostButton;
    [SerializeField] Button ClientButton;
    [SerializeField] Button ExitButton;
    [SerializeField] Button DiscoveryButton;
    TextMeshProUGUI discovertext;
    [SerializeField] Button StopDiscoveryButton;
    [SerializeField] GameObject connectionButtonPrefab;
    [SerializeField] Transform canvasTransfrom;
    readonly Queue<Button> connectionButtonUnActiveQueue = new();
    readonly Queue<Button> connectionButtonActiveQueue = new();
    [SerializeField] MyNetworkDiscovery m_Discovery;
    [SerializeField] UnityTransport transport;
    private bool isNetworkStarted = false;
    NetworkManager m_NetworkManager;

    readonly Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new Dictionary<IPAddress, DiscoveryResponseData>();
    public UnityEvent OnClientStart = new UnityEvent();

    public Vector2 DrawOffset = new Vector2(10, 210);

    void Awake()
    {
        m_NetworkManager = NetworkManager.Singleton;
        m_Discovery ??= m_NetworkManager.gameObject.GetComponent<MyNetworkDiscovery>();
        m_Discovery.OnServerFound.AddListener(OnServerFound);

    }

    private void Start()
    {
        ServerButton.onClick.AddListener(OnStartServer);
        HostButton.onClick.AddListener(OnStartHost);
        ClientButton.onClick.AddListener(OnStartClient);
        ExitButton.onClick.AddListener(OnExitNetwork);
        DiscoveryButton.onClick.AddListener(StartDiscover);
        discovertext = DiscoveryButton.GetComponentInChildren<TextMeshProUGUI>();

        StopDiscoveryButton.onClick.AddListener(StopDiscovery);

        ServerButton.gameObject.SetActive(true);
        HostButton.gameObject.SetActive(true);
        ClientButton.gameObject.SetActive(true);
        DiscoveryButton.gameObject.SetActive(true);

        ExitButton.gameObject.SetActive(false);
        StopDiscoveryButton.gameObject.SetActive(false);
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
            SetActiveButtons(true);


        }
        else
        {
            Debug.LogWarning("Network is not started. Cannot exit network.");
        }
    }
    public void OnNetworkStart()
    {
        isNetworkStarted = true;
        SetActiveButtons(false);   
    }
    private void SetActiveButtons(bool active)
    {
        ServerButton.gameObject.SetActive(active);
        HostButton.gameObject.SetActive(active);
        ClientButton.gameObject.SetActive(active);
        DiscoveryButton.gameObject.SetActive(active);

        ExitButton.gameObject.SetActive(!active);
    }
    private void StartDiscover()
    {
        if (m_Discovery.IsRunning)
        {
                RefreshList();
        }
        else
        {
            discovertext.text = "Refresh List";
            StopDiscoveryButton.gameObject.SetActive(true);
            m_Discovery.StartClient();
        }
        m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
    }
    private void StopDiscovery()
    {
        discovertext.text = "Discover";
        m_Discovery.StopDiscovery();
        RefreshList();
        StopDiscoveryButton.gameObject.SetActive(false);

    }
    private void RefreshList()
    {
        discoveredServers.Clear();
        while (connectionButtonActiveQueue.Count > 0)
        {
            var button = connectionButtonActiveQueue.Dequeue();
            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
            connectionButtonUnActiveQueue.Enqueue(button);
        }
    }
    private void OnServerFound(IPEndPoint sender, DiscoveryResponseData response)
    {
        discoveredServers[sender.Address] = response;
        Button button;
        if (connectionButtonUnActiveQueue.Count > 0)
        {
            button = connectionButtonUnActiveQueue.Dequeue();
        }
        else
        {
            var obj = GameObject.Instantiate(connectionButtonPrefab, canvasTransfrom);
            button = obj.GetComponent<Button>();
            connectionButtonActiveQueue.Enqueue(button);
        }
        button.GetComponentInChildren<TextMeshProUGUI>().text = $"{response.ServerName}[{sender}]";
        button.onClick.AddListener(() => ConnectedToServer(sender.ToString(), response.Port));
        button.gameObject.SetActive(true);
    }
    private void ConnectedToServer(string address,ushort port)
    {
        transport ??= (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
        transport.SetConnectionData(address, port);
        m_NetworkManager.StartClient();
        OnClientStart.Invoke();
        OnNetworkStart();
        StopDiscoveryButton.gameObject.SetActive(false);
    }
}

using System.Collections.Generic;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
    [SerializeField] NetworkManager m_NetworkManager;

    [Header("日英テキスト設定")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private string japaneseTitleText;
    [SerializeField] private string englishTitleText;
    [SerializeField] private string japaneseDescriptionText;
    [SerializeField] private string englishDescriptionText;

    [Header("ボタンテキスト設定")]
    [SerializeField] private TextMeshProUGUI hostButtonText;
    [SerializeField] private TextMeshProUGUI stopDiscoverButtonText;
    [SerializeField] private TextMeshProUGUI exitButtonText;

    [SerializeField] private string japaneseHostText;
    [SerializeField] private string englishHostText;
    [SerializeField] private string japaneseDiscoverText;
    [SerializeField] private string englishDiscoverText;
    [SerializeField] private string japaneseRefreshText;
    [SerializeField] private string englishRefreshText;
    [SerializeField] private string japaneseStopDiscoverText;
    [SerializeField] private string englishStopDiscoverText;
    [SerializeField] private string japaneseExitText;
    [SerializeField] private string englishExitText;

    readonly Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new();
    public UnityEvent OnClientStart = new UnityEvent();

    private string m_discoverText = string.Empty;
    private string m_refreshText = string.Empty;

    private void Reset()
    {
        m_NetworkManager ??= NetworkManager.Singleton;
        m_Discovery ??= m_NetworkManager.gameObject.GetComponent<MyNetworkDiscovery>();
        m_Discovery.OnServerFound.AddListener(OnServerFound);
    }

    private void Start()
    {
        m_NetworkManager ??= NetworkManager.Singleton;
        m_Discovery ??= m_NetworkManager.gameObject.GetComponent<MyNetworkDiscovery>();
        m_Discovery.OnServerFound.AddListener(OnServerFound);

        // ServerButton・ClientButtonは未使用のため非表示
        if (ServerButton != null)
            ServerButton.gameObject.SetActive(false);

        if (ClientButton != null)
            ClientButton.gameObject.SetActive(false);

        if (HostButton != null)
        {
            HostButton.onClick.AddListener(OnStartHost);
            HostButton.gameObject.SetActive(true);
        }
        if (ExitButton != null)
        {
            ExitButton.onClick.AddListener(OnExitNetwork);
            ExitButton.gameObject.SetActive(false);
        }
        if (DiscoveryButton != null)
        {
            DiscoveryButton.onClick.AddListener(StartDiscover);
            // discovertext を UpdateLanguageText より先に取得する
            discovertext = DiscoveryButton.GetComponentInChildren<TextMeshProUGUI>();
            DiscoveryButton.gameObject.SetActive(true);
        }
        if (StopDiscoveryButton != null)
        {
            StopDiscoveryButton.onClick.AddListener(StopDiscovery);
            StopDiscoveryButton.gameObject.SetActive(false);
        }

        // discovertext 取得後に UpdateLanguageText を呼ぶ
        UpdateLanguageText();
    }

    // =========================
    // 日英テキスト更新
    // =========================
    private void UpdateLanguageText()
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

        if (titleText != null)
            titleText.text = isJapanese ? japaneseTitleText : englishTitleText;

        if (descriptionText != null)
            descriptionText.text = isJapanese ? japaneseDescriptionText : englishDescriptionText;

        if (hostButtonText != null)
            hostButtonText.text = isJapanese ? japaneseHostText : englishHostText;

        // discovertext 取得後に実行されるため正しく反映される
        m_discoverText = isJapanese ? japaneseDiscoverText : englishDiscoverText;
        m_refreshText = isJapanese ? japaneseRefreshText : englishRefreshText;

        if (discovertext != null)
            discovertext.text = m_discoverText;

        if (stopDiscoverButtonText != null)
            stopDiscoverButtonText.text = isJapanese ? japaneseStopDiscoverText : englishStopDiscoverText;

        if (exitButtonText != null)
            exitButtonText.text = isJapanese ? japaneseExitText : englishExitText;
    }

    // =========================
    // SERVER / HOST / CLIENT
    // =========================
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

    // =========================
    // EXIT
    // =========================
    private void OnExitNetwork()
    {
        if (isNetworkStarted)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Network Stopped");
            isNetworkStarted = false;
            SetActiveButtons(true);
            StopDiscovery();
        }
        else
        {
            Debug.LogWarning("Network is not started. Cannot exit network.");
        }
    }

    // =========================
    // NETWORK START
    // =========================
    public void OnNetworkStart()
    {
        isNetworkStarted = true;
        SetActiveButtons(false);
    }

    private void SetActiveButtons(bool active)
    {
        // ServerButton・ClientButtonは常に非表示のため除外
        HostButton?.gameObject.SetActive(active);
        DiscoveryButton?.gameObject.SetActive(active);
        ExitButton?.gameObject.SetActive(!active);
    }

    // =========================
    // DISCOVERY
    // =========================
    private void StartDiscover()
    {
        if (m_Discovery.IsRunning)
        {
            // 探索中 → もう一度探す（リフレッシュ）
            RefreshList();
        }
        else
        {
            // 探索開始 → ボタンテキストを「もう一度探す」に変更
            discovertext.text = m_refreshText;
            StopDiscoveryButton.gameObject.SetActive(true);
            m_Discovery.StartClient();
        }
        m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
    }

    private void StopDiscovery()
    {
        // ボタンテキストを「部屋を探す」に戻す
        if (discovertext != null)
            discovertext.text = m_discoverText;

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
            var obj = Instantiate(connectionButtonPrefab, canvasTransfrom);
            button = obj.GetComponent<Button>();
            connectionButtonActiveQueue.Enqueue(button);
        }
        button.GetComponentInChildren<TextMeshProUGUI>().text = $"{response.ServerName}[{sender}]";
        button.onClick.AddListener(() => ConnectedToServer(sender.Address.ToString(), response.Port));
        button.gameObject.SetActive(true);
    }

    private void ConnectedToServer(string address, ushort port)
    {
        transport ??= (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
        transport.SetConnectionData(address, port);
        m_NetworkManager.StartClient();
        OnClientStart.Invoke();
        OnNetworkStart();
        StopDiscoveryButton.gameObject.SetActive(false);
    }
}

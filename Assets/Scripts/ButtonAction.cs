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
    [SerializeField] Button HostButton;
    [SerializeField] Button ExitButton;
    [SerializeField] Button DiscoveryButton;
    [SerializeField] Button StopDiscoveryButton;
    [SerializeField] GameObject connectionButtonPrefab;
    [SerializeField] Transform canvasTransfrom;

    readonly Queue<Button> connectionButtonUnActiveQueue = new();
    readonly Queue<Button> connectionButtonActiveQueue = new();

    [SerializeField] MyNetworkDiscovery m_Discovery;
    [SerializeField] UnityTransport transport;
    private bool isNetworkStarted = false;
    [SerializeField] NetworkManager m_NetworkManager;

    [SerializeField] private VoidEvent connectCanvasEvent;

    [Header("UI テキスト参照")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("ボタンテキスト参照")]
    [SerializeField] private TextMeshProUGUI hostButtonText;
    [SerializeField] private TextMeshProUGUI discoverButtonText;
    [SerializeField] private TextMeshProUGUI stopDiscoverButtonText;
    [SerializeField] private TextMeshProUGUI exitButtonText;

    [Header("日英テキスト設定")]
    [SerializeField] private string japaneseTitleText;
    [SerializeField] private string englishTitleText;
    [SerializeField] private string japaneseDescriptionText;
    [SerializeField] private string englishDescriptionText;

    [Header("ボタンテキスト設定")]
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

    // ★ 追加: 接続ボタンテキスト
    [SerializeField] private string japaneseRoomFoundText = "部屋を見つけた！";
    [SerializeField] private string englishRoomFoundText = "Room Found!";

    readonly Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new();
    public UnityEvent OnClientStart = new UnityEvent();

    private TextMeshProUGUI discovertext;
    private string discoverTextDefault;
    private string refreshText;

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

        if (DiscoveryButton != null)
            discovertext = DiscoveryButton.GetComponentInChildren<TextMeshProUGUI>();

        UpdateLanguageText();

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
            DiscoveryButton.gameObject.SetActive(true);
        }
        if (StopDiscoveryButton != null)
        {
            StopDiscoveryButton.onClick.AddListener(StopDiscovery);
            StopDiscoveryButton.gameObject.SetActive(false);
        }
    }

    private void UpdateLanguageText()
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

        if (titleText != null)
            titleText.text = isJapanese ? japaneseTitleText : englishTitleText;
        if (descriptionText != null)
            descriptionText.text = isJapanese ? japaneseDescriptionText : englishDescriptionText;
        if (hostButtonText != null)
            hostButtonText.text = isJapanese ? japaneseHostText : englishHostText;
        if (stopDiscoverButtonText != null)
            stopDiscoverButtonText.text = isJapanese ? japaneseStopDiscoverText : englishStopDiscoverText;
        if (exitButtonText != null)
            exitButtonText.text = isJapanese ? japaneseExitText : englishExitText;

        discoverTextDefault = isJapanese ? japaneseDiscoverText : englishDiscoverText;
        refreshText = isJapanese ? japaneseRefreshText : englishRefreshText;

        if (discovertext != null)
            discovertext.text = discoverTextDefault;
    }

    private void OnStartHost()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("Host Started");
        connectCanvasEvent?.Invoke();
        OnNetworkStart();
    }

    private void OnExitNetwork()
    {
        if (!isNetworkStarted) return;

        NetworkManager.Singleton.Shutdown();
        Debug.Log("Network Stopped");
        isNetworkStarted = false;
        StopDiscovery();
        SetActiveButtons(true);
    }

    public void OnNetworkStart()
    {
        isNetworkStarted = true;
        SetActiveButtons(false);
    }

    private void SetActiveButtons(bool active)
    {
        HostButton?.gameObject.SetActive(active);
        DiscoveryButton?.gameObject.SetActive(active);
        StopDiscoveryButton?.gameObject.SetActive(false);
        ExitButton?.gameObject.SetActive(!active);
    }

    private void StartDiscover()
    {
        if (m_Discovery.IsRunning)
        {
            RefreshList();
            m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
        }
        else
        {
            m_Discovery.StartClient();
            m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
        }

        if (discovertext != null)
            discovertext.text = refreshText;

        StopDiscoveryButton?.gameObject.SetActive(true);
    }

    private void StopDiscovery()
    {
        m_Discovery.StopDiscovery();
        RefreshList();

        if (discovertext != null)
            discovertext.text = discoverTextDefault;

        StopDiscoveryButton?.gameObject.SetActive(false);
        DiscoveryButton?.gameObject.SetActive(true);
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
        }

        // ★ テキストを日英対応に変更
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";
        button.GetComponentInChildren<TextMeshProUGUI>().text =
            isJapanese ? japaneseRoomFoundText : englishRoomFoundText;

        // ★ ボタンの色を赤に変更
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.6f, 0.05f, 0.05f, 1f);
        colors.highlightedColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        colors.pressedColor = new Color(0.4f, 0.02f, 0.02f, 1f);
        button.colors = colors;

        // ★ テキストの色を金色に変更
        button.GetComponentInChildren<TextMeshProUGUI>().color =
            new Color(1f, 0.78f, 0.2f, 1f);

        button.onClick.AddListener(() =>
            ConnectedToServer(sender.Address.ToString(), response.Port));
        button.gameObject.SetActive(true);
        connectionButtonActiveQueue.Enqueue(button);
    }

    private void ConnectedToServer(string address, ushort port)
    {
        transport ??= (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
        transport.SetConnectionData(address, port);
        m_NetworkManager.StartClient();
        OnClientStart.Invoke();
        OnNetworkStart();
        connectCanvasEvent?.Invoke();
        StopDiscoveryButton?.gameObject.SetActive(false);
    }
}

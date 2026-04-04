using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
public class MyNetworkDiscovery : MonoBehaviour 
{ 
    
    [Serializable]
    public class ServerFoundEvent : UnityEvent<IPEndPoint, DiscoveryResponseData>
    {
    };
    private enum MessageType : byte
    {
        BroadCast = 0,
        Response = 1,
    }

    UdpClient m_Client;
    [Header("ReadOnly Setting")]
    [SerializeField] List<string> LocalAddress = new();
    [SerializeField] List<string> BroadcastAddress = new ();
    [SerializeField] List<string> BroadcastMask = new();
    [Header("Discover Wifi Setting")]
    [SerializeField] WifiIPV4Info.PrivateIPv4Range WifiType = WifiIPV4Info.PrivateIPv4Range.Any;
    [SerializeField] ushort m_Port = 47777;

    // This is long because unity inspector does not like ulong.
    [SerializeField]
    long m_UniqueApplicationId;
    NetworkManager m_NetworkManager;

    [SerializeField]
    [Tooltip("If true NetworkDiscovery will make the server visible and answer to client broadcasts as soon as netcode starts running as server.")]
    bool m_StartWithServer = true;

    public string ServerName = "EnterName";

    public ServerFoundEvent OnServerFound;

    private bool m_HasStartedWithServer = false;

    /// <summary>
    /// Gets a value indicating whether the discovery is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets whether the discovery is in server mode.
    /// </summary>
    public bool IsServer { get; private set; }

    /// <summary>
    /// Gets whether the discovery is in client mode.
    /// </summary>
    public bool IsClient { get; private set; }

    public void OnApplicationQuit()
    {
        StopDiscovery();
    }

    void OnValidate()
    {
        if (m_UniqueApplicationId == 0)
        {
            var value1 = (long)Random.Range(int.MinValue, int.MaxValue);
            var value2 = (long)Random.Range(int.MinValue, int.MaxValue);
            m_UniqueApplicationId = value1 + (value2 << 32);
        }
    }
    public void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
    }

    public void Update()
    {
        if (m_StartWithServer && m_HasStartedWithServer == false && IsRunning == false)
        {
            if (m_NetworkManager.IsServer)
            {
                StartServer();
                m_HasStartedWithServer = true;
            }
        }
    }

    public void ClientBroadcast(DiscoveryBroadcastData broadCast)
    {
        if (!IsClient)
        {
            throw new InvalidOperationException("Cannot send client broadcast while not running in client mode. Call StartClient first.");
        }
        IReadOnlyList<WifiIPV4Info> ipAddressList = WifiIPV4Info.Create(WifiType);
        if(ipAddressList.Count == 0)
        {
            Debug.LogError("No valid wifi network interface found for broadcasting.");
            return;
        }
        LocalAddress.Clear();
        BroadcastAddress.Clear();
        BroadcastMask.Clear();
        foreach (var info in ipAddressList)
        {
            Debug.Log($"Found wifi network interface with IP: {info.IPAddress}, SubnetMask: {info.SubnetMask}, BroadcastAddress: {info.BroadcastAddress}");
            LocalAddress.Add(info.IPAddress.ToString());
            BroadcastMask.Add(info.SubnetMask.ToString());
            IPAddress broadcastAddress = info.BroadcastAddress;

            BroadcastAddress.Add(broadcastAddress.ToString());
            //IPAddress.Broadcast は環境依存でうまく動かないことがあるため手動で計算したブロードキャストアドレスを使う
            //IPEndPoint endPoint = new(IPAddress.Broadcast, m_Port);
            IPEndPoint endPoint = new IPEndPoint(broadcastAddress, m_Port);

            //ここ以下でUDPクライアントを作ってブロードキャストを送る。UDPクライアントはusingで囲んで、送信後に確実に破棄されるようにする。
            using FastBufferWriter writer = new FastBufferWriter(1024, Allocator.Temp, 1024 * 64);

            WriteHeader(writer, MessageType.BroadCast);

            writer.WriteNetworkSerializable(broadCast);
            var data = writer.ToArray();

            try
            {
                // This works because PooledBitStream.Get resets the position to 0 so the array segment will always start from 0.
                m_Client.SendAsync(data, data.Length, endPoint);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }        
    }

    /// <summary>
    /// Starts the discovery in server mode which will respond to client broadcasts searching for servers.
    /// </summary>
    public void StartServer()
    {
        StartDiscovery(true);
    }

    /// <summary>
    /// Starts the discovery in client mode. <see cref="ClientBroadcast"/> can be called to send out broadcasts to servers and the client will actively listen for responses.
    /// </summary>
    public void StartClient()
    {
        StartDiscovery(false);
    }

    public void StopDiscovery()
    {
        IsClient = false;
        IsServer = false;
        IsRunning = false;

        if (m_Client != null)
        {
            try
            {
                m_Client.Close();
            }
            catch (Exception)
            {
                // We don't care about socket exception here. Socket will always be closed after this.
            }

            m_Client = null;
        }
    }

    protected bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response)
    {
        response = new DiscoveryResponseData()
        {
            ServerName = ServerName,
            Port = ((UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
        };
        return true;
    }

    protected void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
    {
        OnServerFound.Invoke(sender, response);
    }

    void StartDiscovery(bool isServer)
    {
        StopDiscovery();

        IsServer = isServer;
        IsClient = !isServer;

        // If we are not a server we use the 0 port (let udp client assign a free port to us)
        var port = isServer ? m_Port : 0;

        m_Client = new UdpClient(port) { EnableBroadcast = true, MulticastLoopback = false };

        _ = ListenAsync(isServer ? ReceiveBroadcastAsync : new Func<Task>(ReceiveResponseAsync));

        IsRunning = true;
    }

    async Task ListenAsync(Func<Task> onReceiveTask)
    {
        while (true)
        {
            try
            {
                await onReceiveTask();
            }
            catch (ObjectDisposedException)
            {
                // socket has been closed
                break;
            }
            catch (Exception)
            {
            }
        }
    }

    async Task ReceiveResponseAsync()
    {
        UdpReceiveResult udpReceiveResult = await m_Client.ReceiveAsync();

        var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);
        using var reader = new FastBufferReader(segment, Allocator.Persistent);

        try
        {
            if (ReadAndCheckHeader(reader, MessageType.Response) == false)
            {
                return;
            }

            reader.ReadNetworkSerializable(out DiscoveryResponseData receivedResponse);
            ResponseReceived(udpReceiveResult.RemoteEndPoint, receivedResponse);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    async Task ReceiveBroadcastAsync()
    {
        UdpReceiveResult udpReceiveResult = await m_Client.ReceiveAsync();

        var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);
        using var reader = new FastBufferReader(segment, Allocator.Persistent);

        try
        {
            if (ReadAndCheckHeader(reader, MessageType.BroadCast) == false)
            {
                return;
            }

            reader.ReadNetworkSerializable(out DiscoveryBroadcastData receivedBroadcast);

            if (ProcessBroadcast(udpReceiveResult.RemoteEndPoint, receivedBroadcast, out DiscoveryResponseData response))
            {
                using var writer = new FastBufferWriter(1024, Allocator.Persistent, 1024 * 64);
                WriteHeader(writer, MessageType.Response);

                writer.WriteNetworkSerializable(response);
                var data = writer.ToArray();

                await m_Client.SendAsync(data, data.Length, udpReceiveResult.RemoteEndPoint);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void WriteHeader(FastBufferWriter writer, MessageType messageType)
    {
        // Serialize unique application id to make sure packet received is from same application.
        writer.WriteValueSafe(m_UniqueApplicationId);

        // Write a flag indicating whether this is a broadcast
        writer.WriteByteSafe((byte)messageType);
    }

    private bool ReadAndCheckHeader(FastBufferReader reader, MessageType expectedType)
    {
        reader.ReadValueSafe(out long receivedApplicationId);
        if (receivedApplicationId != m_UniqueApplicationId)
        {
            return false;
        }

        reader.ReadByteSafe(out byte messageType);
        if (messageType != (byte)expectedType)
        {
            return false;
        }

        return true;
    }
}
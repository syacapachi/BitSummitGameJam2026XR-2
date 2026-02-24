using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
public class ChatSystem : NetworkBehaviour
{
    public static ChatSystem Instance { get; private set; }
    /// サーバー側でチャットメッセージの履歴を管理するためのリスト
    private List<ChatMessage> messageHistory = new();
    [SerializeField] UI_Board UI_Board;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += SendHistory;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= SendHistory;
        }
    }
    private void SendHistory(ulong clientId)
    {
        foreach (var msg in messageHistory)
        {
            SendHistoryClientRpc(msg, clientId);
        }
    }

    [ClientRpc]
    private void SendHistoryClientRpc(ChatMessage msg, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        UI_Board.AddMessage(msg);
    }
    /// <summary>
    /// クライアントから投稿要求
    /// 送信は誰でも行えるようにする。
    /// 投稿に対してサーバー側で検閲処理を行い、問題なければ全クライアントにメッセージをブロードキャストします。
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="text"></param>
    [Rpc(SendTo.Server,InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitMessageRpc(FixedString128Bytes sender, FixedString512Bytes text)
    {
        string rawText = text.ToString();

        // 🔎 検閲処理
        if (!IsValidMessage(rawText))
            return;
        ChatMessage msg = new ChatMessage
        {
            Sender = sender,
            Text = text
        };

        messageHistory.Add(msg);

        BroadcastMessageRpc(msg);
    }

    private bool IsValidMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (text.Length > 200) return false;

        // NGワード例
        string[] banned = { "badword", "xxx" };

        foreach (var word in banned)
        {
            if (text.Contains(word))
                return false;
        }

        return true;
    }

    //ClientRpcは旧仕様で、現在はRpc()属性を使用してクライアント向けRPCを定義します。
    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void BroadcastMessageRpc(ChatMessage msg)
    {
        UI_Board.AddMessage(msg);
    }
}

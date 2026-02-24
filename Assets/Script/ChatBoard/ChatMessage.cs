
using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// INetworkSerializableを実装したチャットメッセージクラス
/// これでネットワーク上でチャットメッセージをシリアライズして送信できるようになります。
/// </summary>
public class ChatMessage : INetworkSerializable
{
    /// <summary>
    /// サイズを指定してFixedStringを使用することで、ネットワーク上でのシリアライズが効率的になります。
    /// </summary>
    public FixedString128Bytes Sender;
    public FixedString512Bytes Text;
    /// <summary>
    /// インターフェースの実装。BufferSerializerを使用して、SenderとTextをシリアライズします。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serializer"></param>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Sender);
        serializer.SerializeValue(ref Text);
    }

}

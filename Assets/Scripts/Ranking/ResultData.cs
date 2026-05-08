using Syacapachi.Attribute;
using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace Syacapachi.Data
{
    /// <summary>
    /// データを全員分集めたもの
    /// </summary>
    [Serializable]
    public class RankingListWrapper
    {
        public List<ResultData> Rankings = new();
    }

    [Serializable]
    [GenerateEvent(typeof(GameEventSOBase<>))]
    public class ResultData :INetworkSerializable
    {
        //共通情報
        public string Time;
        public int GameSeed;
        public bool IsGameOver;
        public float Cooperation;

        public PlayerResultData[] detail;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Time);
            serializer.SerializeValue(ref GameSeed);
            serializer.SerializeValue(ref IsGameOver);
            serializer.SerializeValue(ref Cooperation);
            serializer.SerializeValue(ref detail);
        }
    }
}

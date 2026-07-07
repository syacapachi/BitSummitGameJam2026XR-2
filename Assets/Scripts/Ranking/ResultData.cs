using Syacapachi.Attribute;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Syacapachi.Data
{
    /// <summary>
    /// データを全員分集めたもの
    /// </summary>
    [Serializable]
    public sealed class RankingListWrapper
    {
        public List<ResultData> Rankings = new();
    }

    [Serializable]
    [GenerateEvent(typeof(ReadOnlyGameEventBase<>))]
    public class ResultData : INetworkSerializable
    {
        //共通情報
        /// <summary>
        /// プレイした日時
        /// </summary>
        public string DateTime;
        /// <summary>
        /// 残りスコア
        /// </summary>
        public int RemainHP;
        /// <summary>
        /// フェイズボーナスの合計
        /// </summary>
        public int TotalBonusHP;
        /// <summary>
        /// シード値(ランダム性を入れる場合)
        /// </summary>
        public int GameSeed;
        /// <summary>
        /// 難易度
        /// </summary>
        public Difficulty Difficulty;
        /// <summary>
        /// ゲームオーバーかどうか
        /// </summary>
        public bool IsGameOver;
        /// <summary>
        /// 協力度
        /// </summary>
        public float Cooperation;

        /// <summary>
        /// プレーヤーごとの詳細
        /// </summary>
        public PlayerResultData[] detail;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref DateTime);
            serializer.SerializeValue(ref RemainHP);
            serializer.SerializeValue(ref TotalBonusHP);
            serializer.SerializeValue(ref GameSeed);
            serializer.SerializeValue(ref IsGameOver);
            serializer.SerializeValue(ref Cooperation);
            serializer.SerializeValue(ref Difficulty);
            serializer.SerializeValue(ref detail);//INetworkSerializableがあるとこれでいいっぽい。
        }
    }
}

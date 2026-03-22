using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yokoyama.Data
{
    /// <summary>
    /// データを全員分集めたもの
    /// </summary>
    [Serializable]
    public class RankingListWrapper
    {
        public List<RankingData> Rankings = new();
    }

    [Serializable]
    public class RankingData
    {
        //共通情報
        public string Time;
        public int GameSeed;
        public int TotalScore;
        public List<DetailData> detail = new();

        public void MakeDetailData(IReadOnlyDictionary<string, int> data)
        {
            detail.Clear();
            foreach (KeyValuePair<string, int> kvp in data)
            {
                detail.Add(new DetailData { TargetName = kvp.Key, BreakCount = kvp.Value });
            }
        }
    }
    /// <summary>
    /// 詳細情報
    /// </summary>
    [Serializable]
    public class DetailData
    {
        public string TargetName;
        public int BreakCount;
    }
}

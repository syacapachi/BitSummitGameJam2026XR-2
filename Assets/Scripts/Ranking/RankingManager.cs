using Syacapachi.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Syacapachi.Manager
{
    public class RankingManager : MonoBehaviour
    {
        [SerializeField] bool isSaveJson = true;
        [SerializeField] string FileName = "Ranking";
        [SerializeField] int rankingMaxCount = 10;
        [SerializeField] bool useDailyFile = false;
        [SerializeField] string DaylyFileName = "DailyRanking";
        [SerializeField] ResultDataEvent resultEvent;
        [SerializeField] RankingListWrapper rankingList = new();
        public RankingListWrapper RankingList => rankingList;
        private RankingListWrapper daylyRankingList = new();
        public RankingListWrapper DaylyRankingList => daylyRankingList;
        private string FilePath => Path.Combine(Application.streamingAssetsPath, FileName + ".json");
        private string DailyFilePath => Path.Combine(Application.streamingAssetsPath, DaylyFileName + ".json");

        public ResultData CurrentResult;
        public IReadOnlyList<ResultData> Results => rankingList.Rankings;
        private void Start()
        {
            rankingList = LoadJson(FilePath);
        }
        private void OnEnable()
        {
            resultEvent.Register(SaveJson);
        }
        private void OnDisable()
        {
            resultEvent.Unregister(SaveJson);
        }
        private void SaveJson(ResultData data)
        {
            CurrentResult = data;
#if UNITY_EDITOR
            Debug.Log($"[{nameof(RankingManager)}] {gameObject.name} Recived Data \n Detail = {JsonUtility.ToJson(data, true)}", gameObject);
#endif
            if (!isSaveJson) return;
            RankingListWrapper wrapper = LoadJson(FilePath);
            wrapper.Rankings.Add(data);
            SortJson(wrapper);
            ExportJson(wrapper, FilePath);
            
            //ランキングデータ更新
            rankingList = wrapper;
            if (useDailyFile)
            {
                RankingListWrapper dailyWrapper = LoadJson(DailyFilePath);
                dailyWrapper.Rankings.Add(data);
                if (dailyWrapper.Rankings.Count > rankingMaxCount)
                {
                    //ランキング数がmaxを超えた場合はソートしてmax数分だけ保存
                    dailyWrapper.Rankings = dailyWrapper.Rankings
                        .OrderByDescending(r => r.Cooperation)
                        .ThenBy(r => r.Time)
                        .Take(rankingMaxCount)
                        .ToList();
                }
                else
                {
                    //ランキング数がmaxに満たない場合はソートのみ}
                    SortJson(dailyWrapper);
                }
                ExportJson(dailyWrapper, DailyFilePath);
                //日別ランキングデータ更新
                daylyRankingList = dailyWrapper;
            }
        }
        private void ExportJson(RankingListWrapper wrapper, string filePath)
        {
            string jsonText = JsonUtility.ToJson(wrapper, true);
            //ファイルがない場合は作成。そして書き込む。
            File.WriteAllText(filePath, jsonText);
#if UNITY_EDITOR
            Debug.Log($"ExportedJson:\n{jsonText}at{filePath}", gameObject);
#endif
        }
        private RankingListWrapper LoadJson(string filepath)
        {
            if (!System.IO.File.Exists(filepath))
            {
                Debug.LogError("[Ranking Manager]No ranking file found", gameObject);
                return new RankingListWrapper();
            }

            string json = File.ReadAllText(filepath);
            return JsonUtility.FromJson<RankingListWrapper>(json);
        }
        private void SortJson(RankingListWrapper wrapper)
        {
            // スコアの降順にソート（高いほど上位）
            wrapper.Rankings = wrapper.Rankings
                .OrderByDescending(r => r.Cooperation)
                .ThenBy(r => r.Time) // 同点ならタイム順
                .Take(rankingMaxCount) //上位以外は消す。
                .ToList();
        }
    }
}

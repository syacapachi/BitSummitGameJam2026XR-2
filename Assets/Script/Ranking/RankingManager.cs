using System;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Syacapachi.Data;
namespace Syacapachi.Manager
{
    public class RankingManager : MonoBehaviour
    {
        [SerializeField] bool isSaveJson = true;
        [SerializeField] string FileName = "Ranking";
        [SerializeField] int rankingMaxCount = 10;
        [SerializeField] bool useDailyFile = false;
        [SerializeField] string DaylyFileName = "DailyRanking";
        [SerializeField] RankingListWrapper rankingList = new();
        public RankingListWrapper RankingList => rankingList;
        private RankingListWrapper daylyRankingList = new();
        public RankingListWrapper DaylyRankingList => daylyRankingList;
        private string FilePath => Path.Combine(Application.streamingAssetsPath, FileName + ".json");
        private string DailyFilePath => Path.Combine(Application.streamingAssetsPath, DaylyFileName + ".json");
        //public void SetEvent(GameManager game)
        //{
        //    game.OnGameEnd += OnGameEndHandle;
        //}
        private void Start()
        {
            rankingList = LoadJson(FilePath);
        }
        //private void OnDisable()
        //{
        //    var game = ManagerLocator.Instance.Game;
        //    if (game != null)
        //    {
        //        game.OnGameEnd -= OnGameEndHandle;
        //    }
        //}
        private void OnGameEndHandle(bool isgameComplete)
        {
            if (isgameComplete && isSaveJson)
            {
                SaveJson();
            }

        }

        private void SaveJson()
        {
            //var game = ManagerLocator.Instance.AllGameManager;
            //if (game == null)
            //{
            //    Debug.LogError("[Ranking Manager] Game Manager is MIssing");
            //    return;
            //}
            RankingData data = new RankingData
            {
                Time = DateTime.Now.ToString(),
                GameSeed = 100,
                TotalScore = 200
            };
            //data.MakeDetailData();
            if (isSaveJson)
            {
                RankingListWrapper wrapper = LoadJson(FilePath);
                wrapper.Rankings.Add(data);
                ExportJson(wrapper,FilePath);
                //ランキングデータ更新
                rankingList = wrapper;
            }
            if(useDailyFile)
            {
                RankingListWrapper dailyWrapper = LoadJson(DailyFilePath);
                dailyWrapper.Rankings.Add(data);
                if (dailyWrapper.Rankings.Count > rankingMaxCount)
                {
                    //ランキング数がmaxを超えた場合はソートしてmax数分だけ保存
                    dailyWrapper.Rankings = dailyWrapper.Rankings
                        .OrderByDescending(r => r.TotalScore)
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
        private void ExportJson(RankingListWrapper wrapper,string filePath)
        {
            string jsonText = JsonUtility.ToJson(wrapper, true);
            string writePath = filePath;
            File.WriteAllText(writePath, jsonText);
#if UNITY_EDITOR
            Debug.Log($"ExportedJson:\n{jsonText}at{writePath}");
#endif
        }
        private RankingListWrapper LoadJson(string filepath)
        {
            if (!System.IO.File.Exists(filepath))
            {
                Debug.LogError("[Ranking Manager]No ranking file found");
                return new RankingListWrapper();
            }

            string json = File.ReadAllText(filepath);
            return JsonUtility.FromJson<RankingListWrapper>(json);
        }
        private void SortJson(RankingListWrapper wrapper)
        {
            // スコアの降順にソート（高いほど上位）
            wrapper.Rankings = rankingList.Rankings
                .OrderByDescending(r => r.TotalScore)
                .ThenBy(r => r.Time) // 同点ならタイム順
                .ToList();
        }
        private void OnApplicationQuit()
        {
            if (isSaveJson)
            {
                SaveJson();
            }
        }
    }

}

#if UNITY_EDITOR
namespace Syacapachi.util
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public class AsyncReferenceFinder
    {
        //検索除外拡張子
        static readonly HashSet<string> SkipExtensions = new()
        {
        ".uss",
        ".uxml",
        ".shader",
        ".cginc",
        ".compute",
        ".hlsl",
        ".json",
        ".txt",
        ".xml",
        ".bytes"
       };
        private readonly Dictionary<UnityEngine.Object,List<UnityEngine.Object>> cache = new();
        private UnityEngine.Object target;
        private string[] guids;
        private int guidsIndex;
        private int objectIndex;
        private UnityEngine.GameObject[] sceneObjects;
        private readonly List<UnityEngine.Object> results = new();

        private Action<List<UnityEngine.Object>> onComplete;

        private string currentSearchName = "";
        public float Progress { 
            get 
            {
                int total = (guids?.Length ?? 0) + (sceneObjects?.Length ?? 0);
                if (total == 0) return 0;
                return (float)(guidsIndex + objectIndex) / total; 
            } 
        }
        public bool IsRunning { get; private set; }

        public void StartSearch(Object target, Action<List<Object>> onComplete, bool forceRefresh = false)
        {
            if (IsRunning)
            {
                EditorUtility.DisplayDialog("Search Progress", "Search Progress","ok");
                return;
            }
            if (!forceRefresh && cache.TryGetValue(target, out var cached))
            {
                onComplete?.Invoke(cached);
                return;
            }
            this.target = target;
            this.onComplete = onComplete;
            //アセットへのGUIDがもらえる(検索範囲は、プレハブと、ScriptableObject)
            guids = AssetDatabase.FindAssets("t:Prefab t:ScriptableObject");
            //現在のシーンからシーンないルートオブジェクトを取得
            Scene currentScene = SceneManager.GetActiveScene();
            sceneObjects = currentScene.GetRootGameObjects();
            guidsIndex = 0;
            objectIndex = 0;
            results.Clear();

            IsRunning = true;
            //Editorの更新と共に非同期的に実行
            EditorApplication.update += SearchReference;
        }

        private void SearchReference()
        {
            // 1フレームで処理する数（調整可能）
            int batch = 20;
            int i = 0;
            for (; i < batch && objectIndex < sceneObjects.Length; i++, objectIndex++)
            {
                FindInComponent(sceneObjects[objectIndex]);
                currentSearchName = sceneObjects[objectIndex].name;
            }

            for (; i < batch && guidsIndex < guids.Length; i++, guidsIndex++)
            {
                //GUIDをパスへ変換
                string path = AssetDatabase.GUIDToAssetPath(guids[guidsIndex]);

                //拡張子でスキップ
                if (SkipExtensions.Contains(Path.GetExtension(path)))
                    continue;
                // 型で安全確認
                Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (!IsSearchTarget(type))
                    continue;

                currentSearchName = path;
                if (path.EndsWith(".prefab"))
                {
                    //パスから実体を得る
                    var gameObj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    FindInComponent(gameObj);
                }
                else
                {
                    //パスから実体を得る
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (obj == null) continue;

                    //オブジェクトのプロパティを得るために変換(ほぼキャスト)
                    var so = new SerializedObject(obj);
                    var prop = so.GetIterator();
                    //参照を上から順に調べる。
                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference
                            && prop.objectReferenceValue == target)
                        {
                            results.Add(obj);
                            break;
                        }
                    }
                }    
            }
            // 進捗バー表示
            bool isCanceled = EditorUtility.DisplayCancelableProgressBar(
                $"Searching References about {target.name}",
                 currentSearchName,
                 Progress
            );
            // 完了
            if (guidsIndex >= guids.Length)
            {
                Complete();
            }
            //中止
            if (isCanceled)
            {
                StopSearch();
            }
        }
        private void FindInComponent(GameObject gameObj)
        {
            var components = gameObj.GetComponentsInChildren<Component>(true);
            foreach (var comp in components)
            {
                if (comp == null) continue;

                var so = new UnityEditor.SerializedObject(comp);
                var prop = so.GetIterator();

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == UnityEditor.SerializedPropertyType.ObjectReference &&
                        prop.objectReferenceValue == target)
                    {
                        results.Add(comp.gameObject);
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// GameObejct ScriptableObjectを埋め込めるか
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool IsSearchTarget(Type type)
        {
            return
                typeof(GameObject).IsAssignableFrom(type) || // Prefab
                typeof(ScriptableObject).IsAssignableFrom(type);
        }
        private void Complete()
        {
            EditorUtility.ClearProgressBar();
            EditorApplication.update -= SearchReference;
            //キャッシュ更新
            var distinct = results.Distinct().ToList();
            cache[target] = distinct;

            IsRunning = false;
            onComplete?.Invoke(distinct);
        }
        //とりあえず完了と同じに
        public void StopSearch()
        {
            if(!IsRunning) return;
            Complete();
            Debug.Log($"Search canceled: {target.name} ({Progress}%)");
        }
        public void ClearCache(UnityEngine.Object target = null)
        {
            if (target == null)
                cache.Clear();
            else
                cache.Remove(target);
        }
    }
}
#endif
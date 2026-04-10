#if UNITY_EDITOR
namespace Syacapachi.util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public class AsyncReferenceFinder
    {
        private readonly Dictionary<UnityEngine.Object,List<UnityEngine.Object>> cache = new();
        private UnityEngine.Object target;
        private string[] guids;
        private int guidsIndex;
        private int objectIndex;
        private UnityEngine.GameObject[] sceneObjects;
        private readonly List<UnityEngine.Object> results = new();

        private Action<List<UnityEngine.Object>> onComplete;

        public float Progress => (float)(guidsIndex + objectIndex) / (guids.Length + sceneObjects.Length);
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
            //アセットへのGUIDがもらえる(検索範囲は、プレハブと、ScriptableObjectとシーン)
            guids = AssetDatabase.FindAssets("t:Prefab t:ScriptableObject t:Scene");
            //現在のシーンからシーンないルートオブジェクトを取得
            Scene currentScene = SceneManager.GetActiveScene();
            sceneObjects = currentScene.GetRootGameObjects();
            guidsIndex = 0;
            objectIndex = 0;
            results.Clear();

            IsRunning = true;
            //Editorの更新と共に非同期的に実行
            EditorApplication.update += Update;
        }

        private void Update()
        {
            // 1フレームで処理する数（調整可能）
            int batch = 20;
            int i = 0;
            for(;i<batch && objectIndex < sceneObjects.Length; i++, objectIndex++)
            {
                var components = sceneObjects[objectIndex].GetComponentsInChildren<Component>(true);

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

            for (; i < batch && guidsIndex < guids.Length; i++, guidsIndex++)
            {
                //GUIDをパスへ変換
                string path = AssetDatabase.GUIDToAssetPath(guids[guidsIndex]);
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

            // 進捗バー表示
            EditorUtility.DisplayProgressBar(
                "Searching References",
                target.name,
                Progress
            );

            // 完了
            if (guidsIndex >= guids.Length)
            {
                EditorUtility.ClearProgressBar();
                EditorApplication.update -= Update;
                //キャッシュ更新
                var distinct = results.Distinct().ToList();
                cache[target] = distinct;

                IsRunning = false;
                onComplete?.Invoke(distinct);
            }
        }
        public void StopSearch()
        {
            if(!IsRunning) return;
            EditorUtility.ClearProgressBar();
            EditorApplication.update -= Update;

            IsRunning = false;
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
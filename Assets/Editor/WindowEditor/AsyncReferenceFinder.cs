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
        private Object target;
        private string[] guids;
        private int guidsIndex;
        private int objectIndex;
        private GameObject[] sceneObjects;
        //埋め込まれている
        private readonly List<Object> assginedResults = new();
        //埋め込められる
        private readonly List<Object> assignableResult = new();
        private Action<List<Object>, List<Object>> onComplete;

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

        public void StartSearchRefernce(Object target, Action<List<Object>, List<Object>> onComplete)
        {
            if (IsRunning)
            {
                EditorUtility.DisplayDialog("Search Progress", "Search Progress", "ok");
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
            assginedResults.Clear();

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

                    //オブジェクトのプロパティを得るために変換(ほぼキャスト),開放を忘れずに。
                    using var so = new SerializedObject(obj);
                    var prop = so.GetIterator();
                    //参照を上から順に調べる。
                    while (prop.NextVisible(true))
                    {
                        if (IsAssignableField(prop, target))
                        {
                            assignableResult.Add(obj);
                        }
                        if (prop.propertyType == SerializedPropertyType.ObjectReference
                            && prop.objectReferenceValue == target)
                        {
                            assginedResults.Add(obj);
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

                using var so = new SerializedObject(comp);
                var prop = so.GetIterator();

                //全ての参照ツリーを検索
                //Monobehaviour->InlineClass->...のように
                while (prop.NextVisible(true))
                {
                    if (IsAssignableField(prop, target))
                    {
                        assignableResult.Add(comp);
                    }
                    if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                        prop.objectReferenceValue == target)
                    {
                        assginedResults.Add(comp);
                        break;
                    }
                }
            }
        }
        private static Type GetFieldType(SerializedProperty prop)
        {
            //対象のオブジェクトを取得する
            var targetObject = prop.serializedObject.targetObject;
            //そのタイプを得る。
            var type = targetObject.GetType();

            //そのオブジェクトに記述されているpropと同じ名前のフィールドを取得する
            var field = type.GetField(prop.name,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            return field?.FieldType;
        }
        /// <summary>
        /// 検索中のSerializedPropのフィールドにtargetを入れられるか.
        /// </summary>
        /// <param name="prop">検索中のフィールド</param>
        /// <param name="target">アサインしたいオブジェクト</param>
        /// <returns></returns>
        static bool IsAssignableField(SerializedProperty prop, Object target)
        {
            //[System.Serializeable]か、
            //UnityEngine.Obejctを継承してないと、ScriptableObjectを埋め込めない
            if (prop.propertyType != SerializedPropertyType.ObjectReference)
                return false;

            // 既にアサインされてる場合（従来）
            if (prop.objectReferenceValue == target)
                return true;

            // nullなら型チェック
            if (prop.objectReferenceValue == null)
            {
                var fieldType = GetFieldType(prop);
                if (fieldType == null) return false;

                return fieldType.IsAssignableFrom(target.GetType());
            }

            return false;
        }
        /// <summary>
        /// GameObejct ScriptableObjectを埋め込めるか
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static bool IsSearchTarget(Type type)
        {
            return
                typeof(GameObject).IsAssignableFrom(type) || // Prefab
                typeof(ScriptableObject).IsAssignableFrom(type);
        }
        private void Complete()
        {
            EditorUtility.ClearProgressBar();
            EditorApplication.update -= SearchReference;
            //重複解除
            var distinct = assginedResults.Distinct().ToList();
            var distinct2 = assignableResult.Distinct().ToList();
            IsRunning = false;
            onComplete?.Invoke(distinct,distinct2);
        }
        //とりあえず完了と同じに
        public void StopSearch()
        {
            if(!IsRunning) return;
            Complete();
            Debug.Log($"Search canceled: {target.name} ({Progress}%)");
        }
    }
}
#endif
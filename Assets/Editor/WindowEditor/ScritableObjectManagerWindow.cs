#if UNITY_EDITOR
namespace Syacapachi.util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.TerrainTools;
    using UnityEngine;

    public class ScritableObjectManagerWindow : EditorWindow
    {

        static readonly GUIContent RefreshLabel = new GUIContent("Refresh", "Refresh");
        static readonly GUIContent FindLabel = new GUIContent("Find", "Find");
        static readonly GUIContent FindAssginableLabel = new GUIContent("Find Assginable", "Find Assginable");
        static readonly GUIContent ReFindLabel = new GUIContent("ReFind", "ReFind");
        static readonly GUIContent PingLabel = new GUIContent("Ping", "Ping");
        static readonly GUIContent SelectLabel = new GUIContent("Select", "Select");
        static readonly GUIContent SearchLabel = new GUIContent("Search", "Search");
        static readonly GUIContent SearchingLabel = new GUIContent("Searching...", "Searching");
        static readonly GUIContent ClearLabel = new GUIContent("Clear", "Clear");

        static readonly GUILayoutOption width50 = GUILayout.Width(50);
        static readonly GUILayoutOption width60 = GUILayout.Width(60);
        static readonly GUILayoutOption width150 = GUILayout.Width(150);

        //Type<ScriptableObjec>とそのScriptableObjectのインスタンスのリスト
        static readonly Dictionary<System.Type, List<ScriptableObject>> typeToIntanceCache = new();
        //あるScriptableObjectを参照しているObjectへの参照キャッシュ複数Windowで共有できるのでstatic
        static readonly Dictionary<ScriptableObject, List<UnityEngine.Object>> objectReferenceCache = new();
        static readonly Dictionary<Type, List<UnityEngine.Object>> assginCache = new();
        //折りたたみ状態
        static readonly Dictionary<System.Type, bool> typeFoldouts = new();
        static readonly Dictionary<ScriptableObject, bool> contentFoldouts = new();
        //検索状態
        readonly Dictionary<ScriptableObject, bool> isSearching = new();
        readonly List<ScriptableObject> cachedList = new();
        
        Vector2 scroll;
        string searchText = string.Empty;
        string[] tokens = null;
        bool isSearchClass = false;
        bool isSearchName = true;
        static ScritableObjectManagerWindow()
        {
        }

        //メニューにこの間数を呼ぶボタンを追加
        [MenuItem("Tools/Event Manager Advanced")]
        public static void Open()
        {
            //新しいWindowsを生成
            GetWindow<ScritableObjectManagerWindow>();
        }
        //エディター生成時にリフレッシュ
        void OnEnable()
        {
            Refresh();
        }
        //エディターの処理
        void OnGUI()
        {
            if (GUILayout.Button(RefreshLabel))
                Refresh();

            DrawToolbar();
            scroll = GUILayout.BeginScrollView(scroll);

            foreach (var group in typeToIntanceCache)
            {
                List<ScriptableObject> filtered = new List<ScriptableObject>();
                if (isSearchClass && MatchSearch(group.Key.Name))
                {
                    filtered = group.Value;
                }
                else if (isSearchName)
                {
                    // ▼ フィルタ済みリスト
                    foreach (var so in group.Value)
                    {
                        if (MatchSearch(so.name))
                        {
                            filtered.Add(so);
                        }
                    }
                }
                else
                {
                    filtered = group.Value;
                }
                if (filtered.Count == 0)
                    continue;
                DrawGroup(group.Key, filtered);
            }
            //Endしないと終わらない
            GUILayout.EndScrollView();
        }

        void DrawGroup(System.Type type, IReadOnlyList<ScriptableObject> list)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (!typeFoldouts.ContainsKey(type)) typeFoldouts[type] = false;
                typeFoldouts[type] = EditorGUILayout.Foldout(typeFoldouts[type], type.Name, true);
                if (GUILayout.Button(FindAssginableLabel, width150))
                {
                    Debug.Log("hey");
                }
            }
            // ▼ 展開時のみ結果表示
            if (!typeFoldouts[type]) return;

            foreach (var e in list)
            {
                if (!contentFoldouts.ContainsKey(e)) contentFoldouts[e] = true;
                if (!isSearching.ContainsKey(e)) isSearching[e] = false;
                using (new GUILayout.VerticalScope("box"))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        //折りたたみを更新
                        contentFoldouts[e] = EditorGUILayout.Foldout(contentFoldouts[e], e.name, true);

                        if (GUILayout.Button(FindLabel, width50))
                        {
                            StartSearch(e, false);
                        }

                        if (GUILayout.Button(ReFindLabel, width60))
                        {
                            StartSearch(e, true);
                        }
                        if (GUILayout.Button(PingLabel, width50))
                        {
                            EditorGUIUtility.PingObject(e);
                        }

                        if (GUILayout.Button(SelectLabel, width60))
                        {
                            Selection.activeObject = e;
                        }
                    }

                    // ▼ 検索中表示
                    if (isSearching[e])
                    {
                        GUILayout.Label(SearchingLabel);
                    }

                    // ▼ 展開時に結果表示
                    if (contentFoldouts[e] && objectReferenceCache.TryGetValue(e, out var cachedList))
                    {
                        GUILayout.Space(3);
                        foreach (var r in cachedList)
                        {
                            if (r == null) continue;
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label(r.name);

                                if (GUILayout.Button(PingLabel, width50))
                                    EditorGUIUtility.PingObject(r);

                                if (GUILayout.Button(SelectLabel, width60))
                                    Selection.activeObject = r;
                            }
                        }
                    }
                }
            }
        }
        void DrawToolbar()
        {
            //usingを使ったスコープなら、勝手にEndを呼んでくれる。
            using (new GUILayout.HorizontalScope("box"))
            {
                //GUILayoutOptionもキャッシュ化できる。
                GUILayout.Label(SearchLabel, width50);

                isSearchClass = GUILayout.Toggle(isSearchClass, "Include ClassName");
                isSearchName = GUILayout.Toggle(isSearchName, "Include FileName");
                string newSearch = GUILayout.TextField(searchText);

                // 入力変化検知（Repaint最適化）
                if (newSearch != searchText)
                {
                    searchText = newSearch;
                    //文字変化時に分割
                    tokens = searchText.Split(' ');
                    Repaint();
                }

                if (GUILayout.Button(ClearLabel, width60))
                {
                    searchText = "";
                    tokens = null;
                    GUI.FocusControl(null);
                }
            }
        }
        /// <summary>
        /// 検索テキストの文字が、引数の文字に含まれているか。 Spaceで分ける
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool MatchSearch(string name)
        {
            if (tokens == null) return true; 
            return tokens.All(t => name.Contains(t, StringComparison.OrdinalIgnoreCase));
        }
        void Refresh()
        {
            FindAllScriptableObjects(cachedList);

            //リスト部分だけ初期化
            foreach (var kvp in typeToIntanceCache)
            {
                kvp.Value.Clear();
            }
            foreach (var e in cachedList)
            {
                System.Type rootType = GetRootType(e.GetType());
                if (!typeToIntanceCache.TryGetValue(rootType, out var cachedList))
                {
                    cachedList = new List<ScriptableObject>();
                    typeToIntanceCache[rootType] = cachedList;
                }
                cachedList.Add(e);
            }
        }
        static System.Type GetRootType(System.Type t)
        {
            while (t.BaseType != null && t.BaseType != typeof(ScriptableObject) && !t.BaseType.IsGenericType)
                t = t.BaseType;

            return t;
        }

        /// <summary>
        /// ScriptableObjectを検索して返す。
        /// </summary>
        /// <param name="result">結果をいれるリスト</param>
        /// <returns>見つかった数 </returns>
        static int FindAllScriptableObjects(List<ScriptableObject> result)
        {
            result.Clear();
            //Assets内の検索返り値はGUID(string)
            foreach(string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
            {
                //ファイル参照に変換
                string path = AssetDatabase.GUIDToAssetPath(guid);
                //アセット読み込み
                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if(so != null)
                {
                    result.Add(so);
                }
            }
            return result.Count;
        }
        bool StartSearch(ScriptableObject target, bool force)
        {
            if (!force && objectReferenceCache.ContainsKey(target))
                return false;

            isSearching[target] = true;
            AsyncReferenceFinder finder = new();
            finder.StartSearchRefernce(target, (result1, result2) =>
            {
                objectReferenceCache[target] = result1;
                CreateWindow<AssginableReferenceEditorWindow>().Init(result2);
                isSearching[target] = false;
                AsyncRefrenceFinderGlobal.Unresister(finder);
                Repaint();
            });
            AsyncRefrenceFinderGlobal.Resister(finder);
            return true;
        }
    }
}
#endif
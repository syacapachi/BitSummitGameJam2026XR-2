#if UNITY_EDITOR
namespace Syacapachi.util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public class ScritableObjectManagerWindow : EditorWindow
    {
        
        //ScriptableObjectのリスト
        readonly Dictionary<System.Type, List<ScriptableObject>> groupedEvents = new();
        //参照キャッシュ
        readonly Dictionary<ScriptableObject, List<UnityEngine.Object>> objectCache = new();
        readonly Dictionary<Type, List<UnityEngine.Object>> assginCache = new();
        //折りたたみ状態
        readonly Dictionary<System.Type, bool> typeFoldouts = new();
        readonly Dictionary<ScriptableObject, bool> contentFoldouts = new();
        //検索状態
        readonly Dictionary<ScriptableObject, bool> isSearching = new();
        Vector2 scroll;
        string searchText = string.Empty;
        bool isSearchClass = false;
        bool isSearchName = true;
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
            if (GUILayout.Button("Refresh"))
                Refresh();

            DrawToolbar();
            scroll = GUILayout.BeginScrollView(scroll);

            foreach (var group in groupedEvents)
            {
                List<ScriptableObject> filtered = new();
                if (isSearchClass && MatchSearch(group.Key.Name))
                {
                    filtered = group.Value;
                }
                else if (isSearchName)
                {
                    // ▼ フィルタ済みリスト
                    filtered = group.Value.Where(e => MatchSearch(e.name)).ToList();   
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

        void DrawGroup(System.Type type, List<ScriptableObject> list)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (!typeFoldouts.ContainsKey(type)) typeFoldouts[type] = false;
                typeFoldouts[type] = EditorGUILayout.Foldout(typeFoldouts[type], type.Name, true);
                if (GUILayout.Button("Find Assginable", GUILayout.Width(150)))
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

                        if (GUILayout.Button("Find", GUILayout.Width(50)))
                        {
                            StartSearch(e, false);
                        }

                        if (GUILayout.Button("ReFind", GUILayout.Width(60)))
                        {
                            StartSearch(e, true);
                        }
                        if (GUILayout.Button("Ping", GUILayout.Width(50)))
                        {
                            EditorGUIUtility.PingObject(e);
                        }

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = e;
                        }  
                    }

                    // ▼ 検索中表示
                    if (isSearching[e])
                    {
                        GUILayout.Label("Searching...");
                    }

                    // ▼ 展開時に結果表示
                    if (contentFoldouts[e] && objectCache.ContainsKey(e))
                    {
                        GUILayout.Space(3);
                        foreach (var r in objectCache[e])
                        {
                            if(r == null) continue;
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label(r.name);

                                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                                    EditorGUIUtility.PingObject(r);

                                if (GUILayout.Button("Select", GUILayout.Width(60)))
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
                GUILayout.Label("Search:", GUILayout.Width(50));

                isSearchClass = GUILayout.Toggle(isSearchClass, "Include ClassName");
                isSearchName = GUILayout.Toggle(isSearchName, "Include FileName");
                string newSearch = GUILayout.TextField(searchText);

                // 入力変化検知（Repaint最適化）
                if (newSearch != searchText)
                {
                    searchText = newSearch;
                    Repaint();
                }

                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                {
                    searchText = "";
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
            if (string.IsNullOrEmpty(searchText)) return true;
            var tokens = searchText.Split(' ');
            return tokens.All(t => name.Contains(t, StringComparison.OrdinalIgnoreCase));
        }
        void Refresh()
        {
            var events = FindAllScriptableObjects();

            //リスト部分だけ初期化
            foreach(var kvp in groupedEvents)
            {
                kvp.Value.Clear();
            }
            foreach (var e in events)
            {
                System.Type rootType = GetRootType(e.GetType());
                if (!groupedEvents.ContainsKey(rootType))
                {
                    groupedEvents[rootType] = new List<ScriptableObject>();
                }
                groupedEvents[rootType].Add(e);
            }
        }
        System.Type GetRootType(System.Type t)
        {
            while (t.BaseType != null && t.BaseType != typeof(ScriptableObject) && !t.BaseType.IsGenericType)
                t = t.BaseType;

            return t;
        }

        /// <summary>
        /// ScriptabkleObjectを検索して返す。
        /// </summary>
        /// <returns></returns>
        List<ScriptableObject> FindAllScriptableObjects()
        {
            //Assets内の検索返り値はGUID(string)
            return AssetDatabase.FindAssets("t:ScriptableObject")
                //パスに変換
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                //参照取得
                .Select(p => AssetDatabase.LoadAssetAtPath<ScriptableObject>(p))
                //存在していれば
                .Where(o => o != null)
                .ToList();
        }
        void StartSearch(ScriptableObject target, bool force)
        {
            if (!force && objectCache.ContainsKey(target))
                return;

            isSearching[target] = true;
            AsyncReferenceFinder finder = new();
            finder.StartSearchRefernce(target, (result1,result2) =>
            {
                objectCache[target] = result1;
                CreateWindow<AssginableReferenceEditorWindow>().Init(result2);
                isSearching[target] = false;
                AsyncRefrenceFinderGlobal.Unresister(finder);
                Repaint();
            });
            AsyncRefrenceFinderGlobal.Resister(finder);
        }
    }
}
#endif
#if UNITY_EDITOR
namespace Syacapachi.util
{
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public class ScritableObjectManagerWindow : EditorWindow
    {
        AsyncReferenceFinder finder = new AsyncReferenceFinder();
        //ScriptableObjectのリスト
        readonly Dictionary<System.Type, List<ScriptableObject>> groupedEvents = new();
        //参照キャッシュ
        readonly Dictionary<UnityEngine.Object, List<Object>> cache = new();
        //折りたたみ状態
        readonly Dictionary<System.Type, bool> typeFoldouts = new();
        readonly Dictionary<UnityEngine.Object, bool> contentFoldouts = new();
        //検索状態
        readonly Dictionary<UnityEngine.Object, bool> isSearching = new();
        Vector2 scroll;
        string searchText = string.Empty;
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
            if (finder != null && finder.IsRunning)
            {
                Repaint(); // 進捗更新
            }
            if (GUILayout.Button("Refresh"))
                Refresh();

            DrawToolbar();

            scroll = GUILayout.BeginScrollView(scroll);

            foreach (var group in groupedEvents)
            {
                // ▼ フィルタ済みリスト
                var filtered = group.Value.Where(e => MatchSearch(e.name)).ToList();

                if (filtered.Count == 0)
                    continue;

                DrawGroup(group.Key, filtered);
            }

            GUILayout.EndScrollView();
        }

        void DrawGroup(System.Type type, List<ScriptableObject> list)
        {
            if (!typeFoldouts.ContainsKey(type)) typeFoldouts[type] = false;
            typeFoldouts[type] = EditorGUILayout.Foldout(typeFoldouts[type], type.Name, true, EditorStyles.boldLabel);
            // ▼ 展開時のみ結果表示
            if (!typeFoldouts[type]) return;

            foreach (var e in list)
            {
                if (!contentFoldouts.ContainsKey(e)) contentFoldouts[e] = true;
                if (!isSearching.ContainsKey(e)) isSearching[e] = false;
                GUILayout.BeginVertical("box");

                GUILayout.BeginHorizontal();

                //折りたたみを更新
                contentFoldouts[e] = EditorGUILayout.Foldout(contentFoldouts[e], e.name, true);

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                    EditorGUIUtility.PingObject(e);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                    Selection.activeObject = e;

                if (GUILayout.Button("Find", GUILayout.Width(50)))
                {
                    StartSearch(e, false);
                }

                if (GUILayout.Button("ReFind", GUILayout.Width(70)))
                {
                    StartSearch(e, true);
                }

                GUILayout.EndHorizontal();

                // ▼ 検索中表示
                if (isSearching[e])
                {
                    GUILayout.Label("Searching...");
                }

                // ▼ 展開時に結果表示
                if (contentFoldouts[e] && cache.ContainsKey(e))
                {
                    GUILayout.Space(3);
                    foreach (var r in cache[e])
                    {
                        GUILayout.BeginHorizontal();

                        GUILayout.Label(r.name);

                        if (GUILayout.Button("Ping", GUILayout.Width(50)))
                            EditorGUIUtility.PingObject(r);

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                            Selection.activeObject = r;

                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndVertical();
            }
        }
        void DrawRefrence(List<Object> refrenceObjects)
        {

        }
        void DrawToolbar()
        {
            GUILayout.BeginHorizontal("box");

            GUILayout.Label("Search:", GUILayout.Width(50));

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

            GUILayout.EndHorizontal();
        }
        bool MatchSearch(string name)
        {
            if (string.IsNullOrEmpty(searchText)) return true;

            return name.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
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
        void StartSearch(UnityEngine.Object target, bool force)
        {
            if (!force && cache.ContainsKey(target))
                return;

            isSearching[target] = true;

            finder.StartSearch(target, (result) =>
            {
                cache[target] = result;
                isSearching[target] = false;
                Repaint();
            });
        }

    }
}
#endif
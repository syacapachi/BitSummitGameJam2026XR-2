#if UNITY_EDITOR
namespace Syacapachi.util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unity.Collections;
    using UnityEditor;
    using UnityEngine;

    public class ScritableObjectManagerWindow : EditorWindow, ISerializationCallbackReceiver
    {
        [Serializable]
        public struct ListWrapper
        {
            public List<UnityEngine.Object> List;
            public ListWrapper(List<UnityEngine.Object> list)
            {
                this.List = list;
            }
        }
        [Serializable]
        public struct ListGUIDWrapper
        {
            public List<GUID> GuidList;
            public ListGUIDWrapper(List<GUID> list)
            {
                this.GuidList = list;
            }
            public ListGUIDWrapper(List<UnityEngine.Object> list)
            {
                //Allocator.Temp,このスコープ内でのみ有効なNativeArray,Disposeは自動
                NativeArray<int> instanceIDs = new NativeArray<int>(list.Count, Allocator.Temp);
                NativeArray<GUID> guidOuts = new NativeArray<GUID>(list.Count, Allocator.Temp);
                for (int i = 0; i < list.Count; i++)
                {
                    instanceIDs[i] = list[i].GetInstanceID();
                }
                //Unityが用意している高速な配列
                AssetDatabase.InstanceIDsToGUIDs(instanceIDs, guidOuts);
                GuidList = guidOuts.ToList();

                instanceIDs.Dispose();  
                guidOuts.Dispose();
            }
        }
        [Flags]
        enum SearchMode
        {
            None = 0,
            Class = 1 << 0,
            FileName = 1 << 1,
        }
        enum SearchLogic
        {
            None = 0,
            AND = 1,
            OR = 2
        }
        //GUIContentのキャッシュ
        //これは、メモリ確保でアクセス高速化
        static readonly GUIContent RefreshLabel = new GUIContent("Refresh", "Refresh");
        static readonly GUIContent FindLabel = new GUIContent("Find", "Find");
        static readonly GUIContent FindAssginableLabel = new GUIContent("Find Assginable", "Find Assginable");
        static readonly GUIContent ReFindLabel = new GUIContent("ReFind", "ReFind");
        static readonly GUIContent PingLabel = new GUIContent("Ping", "Ping");
        static readonly GUIContent SelectLabel = new GUIContent("Select", "Select");
        static readonly GUIContent SearchLabel = new GUIContent("Search", "Search");
        static readonly GUIContent SearchingLabel = new GUIContent("Searching...", "Searching");
        static readonly GUIContent ClearLabel = new GUIContent("Clear", "Clear");

        //staticやreadoblyは、アセンブリロード時(スクリプト編集後など)や、Play時に再生成される。
        //正しく言えば、Unityがシリアライズできるデータは、アセンブリロード前に一時退避しアセンブリロード後に再生成&代入される仕様。

        // Type<ScriptableObjec>とそのScriptableObjectのインスタンスのリスト
        //強参照があるとGCできないので、intとかstringにしたい。
        static readonly Dictionary<System.Type, List<ScriptableObject>> typeToIntanceCache = new();
        // 検索文字列、検索対象と、対応するScriptableObjectのリストのキャッシュ
        static readonly Dictionary<(Type,string[]), List<ScriptableObject>> searchInstanceCache = new();
        // あるScriptableObjectを参照しているObjectへの参照キャッシュ(KeyはインスタンスID) 複数Windowで共有できるのでstatic
        static readonly Dictionary<int, List<UnityEngine.Object>> objectReferenceCache = new();
        // あるScriptableObjectを参照できるObjectへの参照キャッシュ(KeyはインスタンスID) 複数Windowで共有できるのでstatic
        static readonly Dictionary<int, Dictionary<string,List<UnityEngine.Object>>> assginableCache = new();
        // 折りたたみ状態
        static readonly Dictionary<System.Type, bool> typeFoldouts = new();
        //(KeyはインスタンスID)
        static readonly Dictionary<int, bool> contentFoldouts = new();
        // 検索状態(KeyはインスタンスID) 
        static readonly Dictionary<int, bool> isSearching = new();
        static readonly List<ScriptableObject> cachedList = new();

        //ここ以下シリアライズ対象
        //検索結果を保存するために逃がすリスト
        //アセンブリロード時、Play時にこれはシリアライズされる。
        List<int> objectReferenceCacheKeysInstanceId = new();
        List<ListWrapper> objectReferenceCacheValues = new();

        List<int> assginableCacheKeysInstanceId = new();
        List<int> assginableCacheKeysCount = new();
        List<string> assginableCacheValuesKey = new();
        List<ListWrapper> assginableCacheValuesValues = new();

        Vector2 scroll;
        string searchText = string.Empty;
        string[] tokens = null;
        SearchMode searchMode = SearchMode.FileName;
        static ScritableObjectManagerWindow()
        {
        }
        //DictをListに変換
        public void OnBeforeSerialize()
        {
            objectReferenceCacheKeysInstanceId.Clear();
            objectReferenceCacheValues.Clear();
            assginableCacheKeysInstanceId.Clear();
            assginableCacheKeysCount.Clear();
            assginableCacheValuesKey.Clear();
            assginableCacheValuesValues.Clear();

            foreach (var kvp in objectReferenceCache)
            {
                objectReferenceCacheKeysInstanceId.Add(kvp.Key);
                objectReferenceCacheValues.Add(new ListWrapper(kvp.Value));
            }
            foreach (var kvp in assginableCache)
            {
                assginableCacheKeysInstanceId.Add(kvp.Key);
                assginableCacheKeysCount.Add(kvp.Value.Count);
                foreach (var kvp2 in kvp.Value)
                {
                    assginableCacheValuesKey.Add(kvp2.Key);
                    assginableCacheValuesValues.Add(new ListWrapper(kvp2.Value));
                }
            }
        }
        //ListをDictに変換
        public void OnAfterDeserialize()
        {
            objectReferenceCache.Clear();
            assginableCache.Clear();
            for (int i = 0; i < objectReferenceCacheKeysInstanceId.Count; i++)
            {
                objectReferenceCache[objectReferenceCacheKeysInstanceId[i]] = objectReferenceCacheValues[i].List;
            }
            for (int i = 0, access = 0; i < assginableCacheKeysInstanceId.Count; i++)
            {
                if (!assginableCache.TryGetValue(assginableCacheKeysInstanceId[i], out var inlineDict))
                {
                    inlineDict = new Dictionary<string, List<UnityEngine.Object>>();
                    assginableCache[assginableCacheKeysInstanceId[i]] = inlineDict;
                }
                inlineDict.Clear();
                for (int k = 0; k < assginableCacheKeysCount[i]; k++, access++)
                {
                    inlineDict[assginableCacheValuesKey[access]] = assginableCacheValuesValues[access].List;
                } 
            }
        }


        //メニューにこの間数を呼ぶボタンを追加
        [MenuItem("Tools/Event Manager Advanced")]
        public static void Open()
        {
            //新しいWindowsを生成,既存のものがある場合は、最後に生成されたものそれを表示(疑似シングルトン)
            GetWindow<ScritableObjectManagerWindow>();
        }
        //Window生成時にリフレッシュ
        void OnEnable()
        {
            Refresh();
        }
        //Window非表示にキャッシュ削除
        void OnDisable()
        {
            ClearCahce();
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
                List<ScriptableObject> filtered;
                // 何もない場合はそのままだす
                if (searchMode == SearchMode.None || tokens == null)
                {
                    filtered = group.Value;
                }
                // クラス検索モードで、 クラス名を含む場合そのままだす
                else if ((searchMode & SearchMode.Class) != 0 && MatchSearch(group.Key.Name, tokens.AsSpan()))
                {
                    filtered = group.Value;
                }
                // ファイル名検索モードの場合、キャッシュを活用、ない場合は個別に中身を検索。
                else if (!searchInstanceCache.TryGetValue((group.Key, tokens), out filtered))
                {
                    filtered = new List<ScriptableObject>();
                    // ▼ フィルタ済みリスト
                    foreach (var so in group.Value)
                    {
                        if (MatchSearch(so.name, tokens.AsSpan()))
                        {
                            filtered.Add(so);
                        }
                    }
                    searchInstanceCache[(group.Key, tokens)] = filtered;
                }
                
                if (filtered.Count == 0)
                    continue;
                DrawGroup(group.Key, filtered);
            }
            //Endしないと終わらない
            GUILayout.EndScrollView();
        }
        static void ClearCahce()
        {
            AsyncReferenceFinder.ClearCahce();
            typeToIntanceCache.Clear();
            searchInstanceCache.Clear();
            objectReferenceCache.Clear();
            assginableCache.Clear();
            isSearching.Clear();
            typeFoldouts.Clear();
            contentFoldouts.Clear();
        }
        static void Refresh()
        {
            searchInstanceCache.Clear();
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
        void DrawToolbar()
        {
            //usingを使ったスコープなら、勝手にEndを呼んでくれる。
            using (new GUILayout.HorizontalScope("box"))
            {
                //GUILayoutOptionもキャッシュ化できる。
                GUILayout.Label(SearchLabel, GUIContentCache.GetWidth(50));

                if(GUILayout.Toggle((searchMode & SearchMode.Class) != 0, "Include ClassName"))
                {
                    searchMode |= SearchMode.Class;
                }
                else
                {
                    searchMode &= ~SearchMode.Class;
                }

                if(GUILayout.Toggle((searchMode & SearchMode.FileName) != 0, "Include FileName")) 
                {
                    searchMode |= SearchMode.FileName;
                }
                else
                {
                    searchMode &= ~SearchMode.FileName;
                }
                
                string newSearch = GUILayout.TextField(searchText);

                // 入力変化検知（Repaint最適化）
                if (newSearch != searchText)
                {
                    searchText = newSearch;
                    //文字変化時に分割
                    tokens = searchText.Split(' ');
                }

                if (GUILayout.Button(ClearLabel, GUIContentCache.GetWidth(60)))
                {
                    searchText = "";
                    tokens = null;
                    GUI.FocusControl(null);
                }
            }
        }
        
        static void DrawGroup(System.Type type, IReadOnlyList<ScriptableObject> list)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (!typeFoldouts.ContainsKey(type)) typeFoldouts[type] = false;
                typeFoldouts[type] = EditorGUILayout.Foldout(typeFoldouts[type], type.Name, true);
                if (GUILayout.Button(FindAssginableLabel, GUIContentCache.GetWidth(150)))
                {
                    Debug.Log("hey");
                }
            }
            // ▼ 展開時のみ結果表示
            if (!typeFoldouts[type]) return;

            foreach (var so in list)
            {
                bool hasCache = objectReferenceCache.TryGetValue(so.GetInstanceID(), out var cachedList);

                using (new GUILayout.VerticalScope("box"))
                {
                    using (new GUILayout.HorizontalScope())
                    {    
                        if (hasCache)
                        {
                            if (!contentFoldouts.TryGetValue(so.GetInstanceID(), out bool foldout))
                            {
                                foldout = true;
                                contentFoldouts[so.GetInstanceID()] = foldout;
                            }
                            //折りたたみを更新
                            contentFoldouts[so.GetInstanceID()] = EditorGUILayout.Foldout(foldout, so.name, true);
                        }
                        else
                        {
                            EditorGUILayout.LabelField(so.name);
                        }

                        if (GUILayout.Button(FindLabel, GUIContentCache.GetWidth(50)))
                        {
                            StartSearch(so, false);
                        }

                        if (GUILayout.Button(ReFindLabel, GUIContentCache.GetWidth(60)))
                        {
                            StartSearch(so, true);
                        }
                        if (GUILayout.Button(PingLabel, GUIContentCache.GetWidth(50)))
                        {
                            EditorGUIUtility.PingObject(so);
                        }

                        if (GUILayout.Button(SelectLabel, GUIContentCache.GetWidth(60)))
                        {
                            Selection.activeObject = so;
                        }
                    }

                    if (!isSearching.TryGetValue(so.GetInstanceID(), out bool searching))
                    {
                        searching = false;
                        isSearching[so.GetInstanceID()] = searching;
                    }

                    // ▼ 検索中表示
                    if (searching)
                    {
                        GUILayout.Label(SearchingLabel);
                    }

                    // ▼ 展開時に結果表示
                    if (cachedList != null && contentFoldouts[so.GetInstanceID()])
                    {
                        GUILayout.Space(3);
                        foreach (var finded in cachedList)
                        {
                            if (finded == null) continue;
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label(finded.name);

                                if (GUILayout.Button(PingLabel, GUIContentCache.GetWidth(50)))
                                    EditorGUIUtility.PingObject(finded);

                                if (GUILayout.Button(SelectLabel, GUIContentCache.GetWidth(60)))
                                    Selection.activeObject = finded;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 検索対象のtokenをnameが持っているか
        /// </summary>
        /// <param name="tokens">検索対象の文字配列</param>
        /// <param name="name"></param>
        /// <returns></returns>
        static bool MatchSearch(string name, in Span<string> tokens, SearchLogic mode = SearchLogic.AND)
        {
            if (tokens == null) return true;
            foreach (string s in tokens)
            {
                if (name.Contains(s, StringComparison.OrdinalIgnoreCase))
                {
                    //含む場合早期リターン
                    if (mode == SearchLogic.OR)
                    {
                        return true;
                    }
                }
                else
                {
                    //含まない場合早期リターン
                    if(mode == SearchLogic.AND)
                    {
                        return false;
                    }
                }
            }
            //ここは、全て含む(AND) or 全て含まない(OR)
            return mode == SearchLogic.AND;
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
            foreach (GUID guid in AssetDatabase.FindAssetGUIDs("t:ScriptableObject"))
            {
                //ファイル参照に変換 なくていいむしろGUIDはファイルが移動しても大丈夫だから堅牢
                //string path = AssetDatabase.GUIDToAssetPath(guid);
                //アセット読み込み
                ScriptableObject so = AssetDatabase.LoadAssetByGUID<ScriptableObject>(guid);
                if (so != null)
                {
                    result.Add(so);
                }
            }
            return result.Count;
        }
        static bool StartSearch(ScriptableObject target, bool force)
        {
            if (!force && assginableCache.TryGetValue(target.GetInstanceID(), out var cache))
            {
                //最後に作られた既存のWindowを上書きして表示
                GetWindow<AssginableReferenceEditorWindow>(target.name).Init(cache);
                return false;
            }

            isSearching[target.GetInstanceID()] = true;
            AsyncReferenceFinder finder = new();
            finder.StartSearchRefernce(target, (assigned, assginable) =>
            {
                objectReferenceCache[target.GetInstanceID()] = assigned;
                assginableCache[target.GetInstanceID()] = assginable;
                CreateWindow<AssginableReferenceEditorWindow>(target.name).Init(assginable);
                isSearching[target.GetInstanceID()] = false;
                AsyncRefrenceFinderGlobal.Unresister(finder);
                //Repaint();
            });
            AsyncRefrenceFinderGlobal.Resister(finder);
            return true;
        }
    }
}
#endif
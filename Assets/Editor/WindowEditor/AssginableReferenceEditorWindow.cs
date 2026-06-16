namespace Syacapachi.Editor
{
    using Syacapachi.util;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using static Syacapachi.Editor.ScritableObjectManagerWindow;

    public class AssginableReferenceEditorWindow : EditorWindow
    {
        static readonly GUIContent PingLabel = new GUIContent("Ping", "Ping");
        static readonly GUIContent SelectLabel = new GUIContent("Select", "Select");
        static readonly Dictionary<string, bool> foldoutsCache = new();

        /// <summary>
        /// Unityがシリアライズできる非static,非readonlyなフィールドは、
        /// アセンブリロード時、Play時に再代入される(保存できる)。
        /// 描画しないので、[SerializeField]はいらない。
        /// </summary>
        private Vector2 scroll;
        //ScriptableObjectへの参照を残すとGCが回収できない。
        private List<ListWrapper<string>> refrenceList = new();

        public static void Open()
        {
            //これで、Windowのインスタンスを作成して表示(他のEditorWindowもいける)
            CreateWindow<AssginableReferenceEditorWindow>();
        }
        /// <summary>
        /// 初期化時に呼ばれる
        /// </summary>
        private void OnEnable()
        {
            scroll = Vector2.zero;
        }
        /// <summary>
        /// エディターが更新される場合
        /// </summary>
        private void OnGUI()
        {
            //スクロールできるフィールド
            using var scrollScope = new GUILayout.ScrollViewScope(scroll, "box");
            scroll = scrollScope.scrollPosition;
            DrawContent(refrenceList);
        }
        private static void DrawContent(IReadOnlyList<ListWrapper<string>> refrencesList)
        {
            //この関数中では、GUIContentのアイコンサイズが32x32になる
            using var iconSizeScope = new EditorGUIUtility.IconSizeScope(new Vector2(32, 32));
            foreach (var wrapper in refrencesList)
            {
                string type = wrapper.Key;
                if (!foldoutsCache.TryGetValue(type, out bool enable))
                {
                    enable = false;
                    foldoutsCache[type] = enable;
                }
                foldoutsCache[type] = EditorGUILayout.Foldout(enable, type, true);
                if (!foldoutsCache[type]) continue;
                foreach (var obj in wrapper.List)
                {
                    if (obj == null) continue;
                    using (new GUILayout.HorizontalScope())
                    {
                        if (!GUIContentCache.TryGetContent(obj.name, out var content))
                        {
                            //同じ参照が帰ってきたので、コピー
                            content = new GUIContent(EditorGUIUtility.ObjectContent(obj, obj.GetType()));
                            GUIContentCache.ResistContent(obj.name, content);
                        }
                        GUILayout.Label(content, EditorStyles.boldLabel);

                        //内部でRectを計算してGUI.Button()を呼ぶので、効率化する際に考える。
                        if (GUILayout.Button(PingLabel, GUIContentCache.GetWidth(50)))
                        {
                            EditorGUIUtility.PingObject(obj);
                        }

                        if (GUILayout.Button(SelectLabel, GUIContentCache.GetWidth(60)))
                        {
                            Selection.activeObject = obj;
                        }
                    }
                }
            }
        }
        public void Init(IReadOnlyDictionary<string, List<UnityEngine.Object>> refrences)
        {
            refrenceList.Clear();
            foreach (var kvp in refrences)
            {
                refrenceList.Add(new ListWrapper<string>(kvp.Key,kvp.Value));
            }
        }
    }
}
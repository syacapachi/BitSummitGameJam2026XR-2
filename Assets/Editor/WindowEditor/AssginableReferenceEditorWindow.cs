using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssginableReferenceEditorWindow : EditorWindow
{
    static readonly GUIContent PingLabel = new GUIContent("Ping", "Ping");
    static readonly GUIContent SelectLabel = new GUIContent("Select", "Select");

    /// <summary>
    /// Unityがシリアライズできる非static,非readonlyなフィールドは、
    /// アセンブリロード時、Play時に再代入される(保存できる)。
    /// 描画しないので、[SerializeField]はいらない。
    /// </summary>
    private Vector2 scroll;
    private List<UnityEngine.Object> refrencesList = new();
    //GUIに追加
    [MenuItem("Tools/tt")]
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
        DrawContent(refrencesList);
    }
    private static void DrawContent(IReadOnlyList<UnityEngine.Object> refrencesList)
    {
        foreach (var obj in refrencesList)
        {
            if (obj == null) continue;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(obj.name, EditorStyles.boldLabel);

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
    public void Init(IReadOnlyList<UnityEngine.Object> refrences)
    {
        refrencesList.Clear();
        foreach (var obj in refrences)
        {
            refrencesList.Add(obj);
        }
        Repaint();
    }
    public void Init(Span<UnityEngine.Object> refrences)
    {
        refrencesList.Clear();
        foreach (var obj in refrences)
        {
            refrencesList.Add(obj);
        }
        Repaint();
    }
}

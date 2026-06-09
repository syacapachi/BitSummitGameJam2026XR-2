using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssginableReferenceEditorWindow : EditorWindow
{
    static readonly GUIContent PingLabel = new GUIContent("Ping", "Ping");
    static readonly GUIContent SelectLabel = new GUIContent("Select", "Select");
    static readonly GUILayoutOption width50 = GUILayout.Width(50);
    static readonly GUILayoutOption width60 = GUILayout.Width(60);

    private Vector2 scroll;
    private readonly List<UnityEngine.Object> refrencesList = new();
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
        DrawContent();
    }
    private void DrawContent()
    {
        foreach (var obj in refrencesList)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(obj.name, EditorStyles.boldLabel);

                if (GUILayout.Button(PingLabel, width50))
                {
                    EditorGUIUtility.PingObject(obj);
                }

                if (GUILayout.Button(SelectLabel, width60))
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

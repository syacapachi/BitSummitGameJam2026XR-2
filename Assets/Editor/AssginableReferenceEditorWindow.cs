using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssginableReferenceEditorWindow : EditorWindow
{
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

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(obj);
                }

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = obj;
                }
            }
        }
    }
    public void Init(List<UnityEngine.Object> refrences)
    {
        refrencesList.Clear();
        refrencesList.AddRange(refrences);
        Repaint();
    }
    
}

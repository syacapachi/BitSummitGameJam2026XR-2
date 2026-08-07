using Syacapachi.Attribute;
using Syacapachi.util;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GenerateEventWindow : EditorWindow
{
    /// <summary>
    /// 作ることができるクラスのリスト。ScriptableObjectを継承したジェネリック型のクラス。
    /// </summary>
    private static readonly List<Type> generatableClasses = new();
    /// <summary>
    /// 選択しているスクリプトファイルのクラスのフルネームのリスト。名前空間を含む。
    /// </summary>
    private readonly List<string> fullNames = new();
    /// <summary>
    /// 選択しているスクリプトファイルのクラスのTypeのリスト。名前空間を含む。
    /// </summary>
    private readonly List<Type> types = new();
    /// <summary>
    /// 作ることができるクラスのインデックス。ジェネリック型のクラス。
    /// </summary>
    private int selectedBaseIndex = 0;
    /// <summary>
    /// 選択しているスクリプトファイルのクラスのインデックス。名前空間を含む。
    /// </summary>
    private int selectedIndex = 0;
    private string assemblyName;
    private MonoScript cachedScript;
    static GenerateEventWindow()
    {
        foreach(Type t in TypeCache.GetTypesDerivedFrom(typeof(ScriptableObject)))
        {
            if (t.IsGenericType)
            {
                generatableClasses.Add(t);
            }
        }
        generatableClasses.Sort((a, b) => string.Compare(a.Name, b.Name));
    }
    [MenuItem("Assets/GenerateWindow")]
    private static void Open()
    {
        GetWindow<GenerateEventWindow>("Generate Event");
    }

    void OnGUI()
    {
        var selectedObject = Selection.activeObject as MonoScript;
        if (selectedObject == null)
        {
            EditorGUILayout.LabelField($"選択中のスクリプトファイルがありません。");
            return;
        }

        // 選択中のスクリプトファイルが変更された場合のみ処理を行う
        if (cachedScript != selectedObject)
        {
            // 選択中のスクリプトファイルを取得
            cachedScript = selectedObject;
            // 選択中のスクリプトファイルのアセンブリ名を取得
            assemblyName = ", " + cachedScript.GetClass()?.Assembly.GetName().Name;
            CSharpTypeParser.GetFullNames(cachedScript.text, fullNames);

            // Typeを取得保存する、取得したTypeを追加する
            types.Clear();
            foreach (string fullName in fullNames)
            {
                // Typeを取得して表示する 入力は"型名, アセンブリ名"の形式で行う必要がある
                Type t = Type.GetType(fullName + assemblyName);
                if (t != null)
                {
                    types.Add(t);
                }
            }
        }
        EditorGUILayout.LabelField($"選択中のスクリプトファイル: {cachedScript.name}, クラス名: {cachedScript.GetClass()}");

        if (types.Count == 0)
        {
            EditorGUILayout.LabelField($"選択中のスクリプトファイルにクラスが見つかりませんでした。");
            return;
        }
        selectedIndex = EditorGUILayout.Popup("クラスを選択してください", selectedIndex, types.ConvertAll(t => t.Name).ToArray());
        selectedBaseIndex = EditorGUILayout.Popup("基本クラスを選択してください", selectedBaseIndex, generatableClasses.ConvertAll(t => t.Name).ToArray());
        if (GUILayout.Button("Generate Event"))
        {
            Type selectedType = types[selectedIndex];
            Type baseType = generatableClasses[selectedBaseIndex];
            GenerateEventAttribute attr = new GenerateEventAttribute(baseType);
            if (AutoEventGenerator.GenerateEvent(selectedType, attr))
            {
                EditorUtility.DisplayDialog("Generate Event", $"イベントクラスを生成しました: {selectedType.Name}", "OK");
                // 生成したスクリプトをアセットデータベースに反映する(内部データドメインリロードが行われる)
                AssetDatabase.Refresh();
                // ドメインリロードを要求する
                //EditorUtility.RequestScriptReload();
            }
            else
            {
                EditorUtility.DisplayDialog("Generate Event", $"イベントクラスの生成に失敗しました: {selectedType.Name}", "OK");
            }
        }
    }
}

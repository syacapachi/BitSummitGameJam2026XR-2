#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
/// <summary>
/// エディター上ではLogScopeのデバックレベルを調整するクラスです。
/// </summary>
public sealed class LogScopeSettingsWindow : EditorWindow
{
    private enum LogLevel
    {
        Level0,
        Level1,
        Level2,
        Level3,
    }

    private static readonly string[] LevelSymbols =
    {
        "LOGSCOPE_LEVEL0",
        "LOGSCOPE_LEVEL1",
        "LOGSCOPE_LEVEL2",
        "LOGSCOPE_LEVEL3",
    };

    private readonly HashSet<string> _defines = new();

    private string _cachedDefineString;

    private NamedBuildTarget buildTarget;

    private LogLevel level;
    private bool buildEnvTest;

    [MenuItem("Tools/LogScope/Settings")]
    private static void Open()
    {
        GetWindow<LogScopeSettingsWindow>("LogScope");
    }

    private void OnEnable()
    {
        // Project Settings -> Player -> Other Settings -> Scripting Define Symbolsを取得
        buildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        Load();
    }

    private void OnGUI()
    {
        GUILayout.Label("LogScope Settings", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        level = (LogLevel)EditorGUILayout.EnumPopup("Log Level", level);

        buildEnvTest = EditorGUILayout.Toggle(
            new GUIContent(
                "ビルド環境を再現",
                "（ビルド環境を再現）"
            ),
            buildEnvTest);

        if (EditorGUI.EndChangeCheck())
        {
            Save();
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
@"Level0 : 何も出力しない
Level1 : Error
Level2 : Error + Warning
Level3 : Error + Warning + Log",
            MessageType.Info);
    }
    /// <summary>
    /// 変更がある場合に
    /// キャッシュの更新を行います。
    /// </summary>
    /// <param name="defineString"></param>
    private void RefreshDefinesIfNeeded(string defineString)
    {
        if (string.IsNullOrEmpty(defineString)) 
            return;
        if (defineString == _cachedDefineString)
            return;
        _cachedDefineString = defineString;

        _defines.Clear();

        ReadOnlySpan<char> span = defineString.AsSpan();

        while (!span.IsEmpty)
        {
            int index = span.IndexOf(';');

            ReadOnlySpan<char> token;

            if (index < 0)
            {
                token = span;
                span = default;
            }
            else
            {
                token = span[..index];
                span = span[(index + 1)..];
            }

            if (!token.IsEmpty)
            {
                _defines.Add(token.ToString());
            }
        }
    }

    private void Load()
    {
        // 定義されているシンボルを取得
        string defineString =
            PlayerSettings.GetScriptingDefineSymbols(buildTarget);

        // 更新を確認
        RefreshDefinesIfNeeded(defineString);

        buildEnvTest = LogScopeEditorSettings.instance.isTestBuildEnv;

        level = LogLevel.Level0;

        for (int i = 0; i < LevelSymbols.Length; i++)
        {
            if (_defines.Contains(LevelSymbols[i]))
            {
                level = (LogLevel)i;
                break;
            }
        }
    }

    private void Save()
    {
        // Prefs更新
        LogScopeEditorSettings.instance.isTestBuildEnv = buildEnvTest;
        LogScopeEditorSettings.instance.logLevel = (int)level;

        // 更新を確認
        RefreshDefinesIfNeeded(
            PlayerSettings.GetScriptingDefineSymbols(buildTarget));

        // 変更後に再コンパイルが走るはずだが、走らない場合がある(非同期処理なので、遅れてるだけかも)。
        // そのとき他のDefineが検出できず、このまま通すと削除されるので、検出する。
        if (!string.IsNullOrEmpty(_cachedDefineString) &&  _defines.Count == 0)
        {
            // Whindowを消すと再コンパイルが入るのでこのメッセージ。
            Debug.LogError("Whindowを再読み込みしてください。");
            return;
        }


        foreach (string symbol in LevelSymbols)
            _defines.Remove(symbol);

         
        _defines.Add(LevelSymbols[(int)level]);

        

        string newDefineString =
            string.Join(";", _defines.OrderBy(x => x));

        if (newDefineString == _cachedDefineString)
            return;
            

        _cachedDefineString = newDefineString;

        PlayerSettings.SetScriptingDefineSymbols(
            buildTarget,
            newDefineString);

        // 再コンパイルをリクエスト
        AssetDatabase.Refresh();
    }
}

#endif
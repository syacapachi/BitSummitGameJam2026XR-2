#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// [OnInspectorButton]属性を持つメソッドを、Inspectorにボタンとして表示。
/// MonoBehaviour / ScriptableObject 両対応版。
/// ネストした ScriptableObjectも再帰的に描画。
/// </summary>
[CustomEditor(typeof(UnityEngine.Object), true)]
public class OnInspectorButtonEditor : Editor
{
    // メソッド名と引数のキャッシュ (パフォーマンス向上のため)
    private readonly Dictionary<string, object[]> methodParameters = new();
    // ScriptableObjectのFoldout状態のキャッシュ (複数インスペクターでの状態管理のため)
    private readonly Dictionary<UnityEngine.Object, bool> foldoutStates = new();
    // ネストしたScriptableObjectのEditorキャッシュ (パフォーマンス向上のため)
    private readonly Dictionary<UnityEngine.Object, Editor> _editorCache = new();
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        //base.OnInspectorGUI(); //これを呼ぶと、全てのフィールドが描画される。DrawDefaultInspector()と同様。
        //通常のインスペクター描画を行う。これを呼ばないと、通常のフィールドが表示されない。
        DrawDefaultInspector();

        //各インスペクターで呼ばれる。
        var targetType = target.GetType();

        //自分自身は描画しない(エラー回避)
        if (targetType == typeof(OnInspectorButtonEditor)) return;

        // メソッドを列挙
        var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<OnInspectorButtonAttribute>();
            if (attr == null)
                continue;
            // 実行中のみ表示
            if (attr.showOnlyInPlayMode && !Application.isPlaying)
                continue;

            DrawButtonForMethod(method, attr);
        }

        // ネストしたScriptableObjectを再帰的に描画
        DrawNestedScriptableObjects(target);
    }

    private void DrawButtonForMethod(MethodInfo method, OnInspectorButtonAttribute attr)
    {
        //ラベルがない場合は関数名で上書き
        string buttonLabel = string.IsNullOrEmpty(attr.label) ? method.Name : attr.label;
        //引数を取得
        var parameters = method.GetParameters();

        EditorGUILayout.Space(4);

        if (parameters.Length == 0)
        {
            if (GUILayout.Button(buttonLabel))
                InvokeMethod(method, null);
        }
        else
        {
            //初回は辞書に登録することで次回以降の検索の手間を省く
            if (!methodParameters.ContainsKey(method.Name))
                methodParameters[method.Name] = new object[parameters.Length];

            var values = methodParameters[method.Name];

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{method.Name} Parameters", EditorStyles.boldLabel);

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                values[i] = DrawFieldForType(param, values[i]);
            }

            if (GUILayout.Button(buttonLabel))
                InvokeMethod(method, values);

            EditorGUILayout.EndVertical();
        }
    }

    private void InvokeMethod(MethodInfo method, object[] values)
    {
        try
        {
            method.Invoke(target, values);
        }
        catch (Exception e)
        {
            Debug.LogError($"[OnInspectorButton] {method.Name} failed: {e}");
        }
    }

    private object DrawFieldForType(ParameterInfo param, object currentValue)
    {
        Type t = param.ParameterType;
        string name = ObjectNames.NicifyVariableName(param.Name);

        if (t == typeof(int))
            return EditorGUILayout.IntField(name, currentValue != null ? (int)currentValue : 0);
        if (t == typeof(float))
            return EditorGUILayout.FloatField(name, currentValue != null ? (float)currentValue : 0f);
        if (t == typeof(string))
            return EditorGUILayout.TextField(name, currentValue as string ?? "");
        if (t == typeof(bool))
            return EditorGUILayout.Toggle(name, currentValue != null && (bool)currentValue);
        if (t == typeof(Vector2))
            return EditorGUILayout.Vector2Field(name, currentValue != null ? (Vector2)currentValue : Vector2.zero);
        if (t == typeof(Vector3))
            return EditorGUILayout.Vector3Field(name, currentValue != null ? (Vector3)currentValue : Vector3.zero);
        if (t == typeof(Color))
            return EditorGUILayout.ColorField(name, currentValue != null ? (Color)currentValue : Color.white);

        // Enum
        if (t.IsEnum)
        {
            if (currentValue == null)
                currentValue = Enum.GetValues(t).GetValue(0);
            return EditorGUILayout.EnumPopup(name, (Enum)currentValue);
        }

        // UnityEngine.Object
        if (typeof(UnityEngine.Object).IsAssignableFrom(t))
            return EditorGUILayout.ObjectField(name, currentValue as UnityEngine.Object, t, true);

        // 配列またはList
        if (typeof(IList).IsAssignableFrom(t))
        {
            EditorGUILayout.LabelField($"{name} ({t.Name}) : List/Array not supported in editor parameters");
            return currentValue;
        }

        EditorGUILayout.LabelField($"{name} ({t.Name}) : not supported");
        return currentValue;
    }

    /// <summary>
    /// ScriptableObjectのネストされたフィールドを再帰的に描画
    /// </summary>
    private void DrawNestedScriptableObjects(UnityEngine.Object obj, int depth = 0, HashSet<UnityEngine.Object> visited = null)
    {
        if (obj == null || depth > 3) return;

        visited ??= new HashSet<UnityEngine.Object>();

        if(visited.Contains(obj)) return; // 循環参照回避
        visited.Add(obj);

        //SOに入っている[SerialiFiled],publicを取得(インスペクターで描画可能なやつ)
        var so = new SerializedObject(obj);
        so.Update();
        //[Serializable]の先頭
        var prop = so.GetIterator();

        bool enterChildren = true;

        //次に行けるかどうか
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false; //最初の1回だけは展開しておく
            //[SerializeReference]が有る場合は描画
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                DrawSOReference(prop, depth, visited);
            }
            if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
            {
                //配列の中身もチェックする
                for (int i = 0; i < prop.arraySize; i++)
                {
                    var elementProp = prop.GetArrayElementAtIndex(i);
                    if (elementProp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        DrawSOReference(elementProp, depth, visited, $"{prop.displayName}[{i}]");
                    }
                }
            }
        }
        //状態を保存しておくことで、複数インスペクターで同じSOを描画している場合でも、展開状態を共有できる。
        so.ApplyModifiedProperties();
    }
    private void DrawSOReference(SerializedProperty prop,int depth,HashSet<UnityEngine.Object> visited,string overrideLabel = null)
    {
        UnityEngine.Object refObj = prop.objectReferenceValue;
        if (refObj is not ScriptableObject nestedSO) return;

        if (!foldoutStates.ContainsKey(nestedSO))
        {
            foldoutStates[nestedSO] = false; // 初期状態は折りたたみ
        }
        string label = overrideLabel ?? prop.displayName;
        label = $"{label} ▶ {nestedSO.name} ({nestedSO.GetType().Name}";

        EditorGUILayout.Space(3);

        foldoutStates[nestedSO] = EditorGUILayout.Foldout(
            foldoutStates[nestedSO],
            label,
            true
        );
        if (!foldoutStates[nestedSO]) return;
        
        EditorGUI.indentLevel++;
        // -------------------------
        // Editorキャッシュ使用
        // -------------------------
        if (!_editorCache.TryGetValue(nestedSO, out var cachedEditor) || cachedEditor == null)
        {
            Editor.CreateCachedEditor(nestedSO, null, ref cachedEditor);
            _editorCache[nestedSO] = cachedEditor;
        }

        if (cachedEditor != null)
        {
            cachedEditor.OnInspectorGUI();
        }

        // 再帰
        DrawNestedScriptableObjects(nestedSO, depth + 1, visited);

        EditorGUI.indentLevel--;

    }
}
#endif

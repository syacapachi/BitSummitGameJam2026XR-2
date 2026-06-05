namespace Syacapachi.Editor
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public static class NestesObjectDrawLogic
    {
        // ScriptableObjectごとのFoldout状態のキャッシュ。ScriptableObjectはUnityEngine.Objectを継承しているので、インスタンスごとに状態を管理できる。
        // ScriptableObjectのFoldout状態のキャッシュ (複数インスペクターでの状態管理のため)
        private static readonly Dictionary<UnityEngine.Object, bool> foldoutStates = new();
        // ネストしたEditorキャッシュ (パフォーマンス向上のため)ここだけの別クラスにできる。
        private static readonly Dictionary<UnityEngine.Object, Editor> editorCache = new();

        internal static bool TryGetOrCreateEditorCache(UnityEngine.Object obj, out Editor editor)
        {
            editor = GetOrCreateEditorCache(obj);
            return editor != null;
        }

        internal static Editor GetOrCreateEditorCache(UnityEngine.Object obj)
        {
            if (!editorCache.TryGetValue(obj, out var editor))
            {
                Editor.CreateCachedEditor(obj, null, ref editor);
                editorCache[obj] = editor;
            }
            return editor;
        }

        internal static void DrawNestedScriptableObject(UnityEngine.Object obj)
        {
            DrawNestedScriptableObjectsRecrusiveInternal(obj);
        }
        /// <summary>
        /// ScriptableObjectのネストされたフィールドを再帰的に描画
        /// </summary>
        private static void DrawNestedScriptableObjectsRecrusiveInternal(UnityEngine.Object obj, int depth = 0, HashSet<UnityEngine.Object> visited = null, string overrideLabel = null)
        {
            if (obj == null || depth > 0) return;

            visited ??= new HashSet<UnityEngine.Object>();

            if (visited.Contains(obj)) return; // 循環参照回避
            visited.Add(obj);

            //SOに入っている[SerialiFiled],publicを取得(インスペクターで描画可能なやつ)
            //usingでこの関数を抜けたときにso.Dispose()が呼ばれて、安全に破棄できる。
            using var so = new SerializedObject(obj);

            //最新情報に更新
            so.Update();
            //[Serializable]の先頭(Unity6のどこか以降はDisposeがある、以前はない)
            var prop = so.GetIterator();

            bool enterChildren = true;

            //次に行けるかどうか(内部でポインターが変わっている)
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false; //最初の1回だけは展開しておく
                                       //UnityEngine.Objectの参照が有る場合は描画
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    DrawSOReference(prop, depth, visited, overrideLabel);
                }
                else if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                {
                    //配列の中身もチェックする
                    for (int i = 0; i < prop.arraySize; i++)
                    {
                        var elementProp = prop.GetArrayElementAtIndex(i);
                        if (elementProp.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            DrawSOReference(elementProp, depth, visited, overrideLabel + $"{prop.displayName}[{i}]");
                        }
                    }
                }
            }
            //状態を保存しておくことで、複数インスペクターで同じSOを描画している場合でも、状態を共有できる。
            so.ApplyModifiedProperties();
        }
        private static void DrawSOReference(SerializedProperty prop, int depth, HashSet<UnityEngine.Object> visited, string overrideLabel = null)
        {
            UnityEngine.Object refObj = prop.objectReferenceValue;
            if (refObj == null) return;
            if (refObj is not ScriptableObject nestedSO)
            {
                // ScriptableObjectじゃない場合は再帰
                DrawNestedScriptableObjectsRecrusiveInternal(refObj, depth + 1, visited, refObj.name);
                return;
            }
            //非永続オブジェクト(一時オブジェクト)を拒否
            if (!EditorUtility.IsPersistent(nestedSO))
                return;

            if (!foldoutStates.TryGetValue(nestedSO, out var enable))
            {
                enable = false;// 初期状態は折りたたみ
                foldoutStates[nestedSO] = enable;
            }
            string label = overrideLabel ?? prop.displayName;
            label = $"{label} ▶ {nestedSO.name} ({nestedSO.GetType().Name})";

            EditorGUILayout.Space(3);

            foldoutStates[nestedSO] = EditorGUILayout.Foldout(
                enable,
                label,
                true
            );
            if (!foldoutStates[nestedSO]) return;

            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            using (new EditorGUI.IndentLevelScope(1))
            {
                // -------------------------
                // Editorキャッシュ使用
                // -------------------------
                if (!editorCache.TryGetValue(nestedSO, out var cachedEditor) || cachedEditor == null)
                {
                    Editor.CreateCachedEditor(nestedSO, null, ref cachedEditor);
                    editorCache[nestedSO] = cachedEditor;
                }

                //エディター描画(この中でも呼ばれするので実質再帰)
                if (cachedEditor != null)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Space(2);
                        cachedEditor.OnInspectorGUI();
                        GUILayout.Space(2);
                    }
                }
            }
            EditorGUI.indentLevel = prevIndent;
        }
    }
}
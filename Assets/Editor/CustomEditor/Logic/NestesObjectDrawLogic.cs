namespace Syacapachi.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public static class NestesObjectDrawLogic
    {
        [Flags]
        public enum NestedScriptableObjectResult
        {
            None = 0,
            DrawedObject = 1 << 0,
        }

        // ScriptableObjectごとのFoldout状態のキャッシュ。ScriptableObjectはUnityEngine.Objectを継承しているので、インスタンスごとに状態を管理できる。
        // ScriptableObjectのFoldout状態のキャッシュ (複数インスペクターでの状態管理のため)
        // KeyはUnityEngine.Object.GetInstanceID()
        private static readonly Dictionary<int, bool> foldoutStates = new();
        // KeyはUnityEngine.Object.GetInstanceID()
        // ネストしたEditorキャッシュ (パフォーマンス向上のため)ここだけの別クラスにできる。
        private static readonly Dictionary<int, Editor> editorCache = new();

        public static bool TryGetOrCreateEditorCache(UnityEngine.Object obj, out Editor editor)
        {
            editor = GetOrCreateEditorCache(obj);
            return editor != null;
        }

        public static Editor GetOrCreateEditorCache(UnityEngine.Object obj)
        {
            int instanceId = obj.GetInstanceID();
            if (!editorCache.TryGetValue(instanceId, out var editor) || editor == null)
            {
                Editor.CreateCachedEditor(obj, null, ref editor);
                editorCache[instanceId] = editor;
            }
            return editor;
        }

        public static NestedScriptableObjectResult DrawNestedScriptableObject(UnityEngine.Object obj)
        {
            return DrawNestedScriptableObjectsRecrusiveInternal(obj);
        }
        /// <summary>
        /// ScriptableObjectのネストされたフィールドを再帰的に描画
        /// </summary>
        private static NestedScriptableObjectResult DrawNestedScriptableObjectsRecrusiveInternal(UnityEngine.Object obj, int depth = 0, HashSet<UnityEngine.Object> visited = null, string overrideLabel = null)
        {
            if (obj == null || depth > 0) return NestedScriptableObjectResult.None;

            visited ??= new HashSet<UnityEngine.Object>(2);

            if (visited.Contains(obj)) return NestedScriptableObjectResult.None; // 循環参照回避
            visited.Add(obj);

            NestedScriptableObjectResult result = NestedScriptableObjectResult.None;

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
                    result |= DrawSOReference(prop, depth, visited, overrideLabel);
                }
                else if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                {
                    //配列の中身もチェックする
                    for (int i = 0; i < prop.arraySize; i++)
                    {
                        var elementProp = prop.GetArrayElementAtIndex(i);
                        if (elementProp.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            result |= DrawSOReference(elementProp, depth, visited, overrideLabel + $"{prop.displayName}[{i}]");
                        }
                    }
                }
            }
            //状態を保存しておくことで、複数インスペクターで同じSOを描画している場合でも、状態を共有できる。
            so.ApplyModifiedProperties();
            return result;
        }
        private static NestedScriptableObjectResult DrawSOReference(SerializedProperty prop, int depth, HashSet<UnityEngine.Object> visited, string overrideLabel = null)
        {
            UnityEngine.Object refObj = prop.objectReferenceValue;
            if (refObj == null) return NestedScriptableObjectResult.None;
            if (refObj is not ScriptableObject nestedSO)
            {
                // ScriptableObjectじゃない場合は再帰
                return DrawNestedScriptableObjectsRecrusiveInternal(refObj, depth + 1, visited, refObj.name);
            }
            //非永続オブジェクト(一時オブジェクト)を拒否
            if (!EditorUtility.IsPersistent(nestedSO))
                return NestedScriptableObjectResult.None;

            NestedScriptableObjectResult result = NestedScriptableObjectResult.DrawedObject;

            int instanceId = nestedSO.GetInstanceID();
            if (!foldoutStates.TryGetValue(instanceId, out var enable))
            {
                enable = false;// 初期状態は折りたたみ
                foldoutStates[instanceId] = enable;
            }
            string label = overrideLabel ?? prop.displayName;
            label = $"{label} ▶ {nestedSO.name} ({nestedSO.GetType().Name})";

            EditorGUILayout.Space(3);

            foldoutStates[instanceId] = EditorGUILayout.Foldout(
                enable,
                label,
                true
            );
            if (!foldoutStates[instanceId]) return result;

            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            using (new EditorGUI.IndentLevelScope(1))
            {
                // -------------------------
                // Editorキャッシュ使用
                // -------------------------
                if (!editorCache.TryGetValue(instanceId, out var cachedEditor) || cachedEditor == null)
                {
                    Editor.CreateCachedEditor(nestedSO, null, ref cachedEditor);
                    editorCache[instanceId] = cachedEditor;
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
            return result;
        }
    }
}

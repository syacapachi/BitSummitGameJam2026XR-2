#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using Unity.Netcode;
    using Unity.Netcode.Editor;
    using UnityEditor;
    using UnityEngine;
    using Syacapachi.Attribute;
    /// <summary>
    /// [OnInspectorButton]属性を持つメソッドを、Inspectorにボタンとして表示。
    /// NetworkBehaviour 対応版。
    /// </summary>
    [CustomEditor(typeof(NetworkBehaviour), true)]
    public class NetCodeButtonEditor : NetcodeEditorBase<NetworkBehaviour>
    {
        private readonly Dictionary<Type, MethodInfo[]> methodCache = new();
        // メソッド名と引数のキャッシュ (パフォーマンス向上のため)
        private readonly Dictionary<MethodInfo, object[]> methodParameters = new();
        // Foldoutの状態のキャッシュ (複数インスペクターでの状態管理のため)
        private readonly Dictionary<object, bool> foldouts = new();
        // ScriptableObjectのFoldout状態のキャッシュ (複数インスペクターでの状態管理のため)
        private readonly Dictionary<UnityEngine.Object, bool> foldoutStates = new();
        // ネストしたEditorキャッシュ (パフォーマンス向上のため)
        private readonly Dictionary<UnityEngine.Object, Editor> editorCache = new();
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

            // キャッシュからメソッドを取得、なければリフレクションで取得してキャッシュに保存
            if (!methodCache.TryGetValue(targetType, out var methods))
            {
                // メソッドを列挙
                methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                methodCache[targetType] = methods;
            }

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
            //変更を保存
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawButtonForMethod(MethodInfo method, OnInspectorButtonAttribute attr)
        {
            //ラベルがない場合は関数名で上書き
            string buttonLabel = string.IsNullOrEmpty(attr.label) ? method.Name : attr.label;
            //引数を取得
            var parameters = method.GetParameters();

            EditorGUILayout.Space(5);

            if (parameters.Length == 0)
            {
                if (GUILayout.Button(buttonLabel))
                    InvokeMethod(method, null);

                return;
            }
            //初回は辞書に登録することで次回以降の検索の手間を省く
            if (!methodParameters.ContainsKey(method))
                methodParameters[method] = new object[parameters.Length];

            var values = methodParameters[method];

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{method.Name} Parameters", EditorStyles.boldLabel);

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                values[i] = DrawField(param.ParameterType, param.Name, values[i]);
            }

            if (GUILayout.Button(buttonLabel))
                InvokeMethod(method, values);

            EditorGUILayout.EndVertical();
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

        private object DrawField(Type t, string name, object value)
        {
            name = ObjectNames.NicifyVariableName(name);
            if (t == typeof(int))
                return EditorGUILayout.IntField(name, value != null ? (int)value : 0);
            if (t == typeof(float))
                return EditorGUILayout.FloatField(name, value != null ? (float)value : 0f);
            if (t == typeof(double))
                return EditorGUILayout.DoubleField(name, value != null ? (double)value : 0);
            if (t == typeof(long))
                return EditorGUILayout.LongField(name, value != null ? (long)value : 0);
            if (t == typeof(string))
                return EditorGUILayout.TextField(name, value as string ?? "");
            if (t == typeof(bool))
                return EditorGUILayout.Toggle(name, value != null && (bool)value);
            if (t == typeof(Vector2))
                return EditorGUILayout.Vector2Field(name, value != null ? (Vector2)value : Vector2.zero);
            if (t == typeof(Vector3))
                return EditorGUILayout.Vector3Field(name, value != null ? (Vector3)value : Vector3.zero);
            if (t == typeof(Vector4))
                return EditorGUILayout.Vector4Field(name, value != null ? (Vector4)value : Vector4.zero);
            if (t == typeof(Vector2Int))
                return EditorGUILayout.Vector2IntField(name, value != null ? (Vector2Int)value : Vector2Int.zero);
            if (t == typeof(Vector3Int))
                return EditorGUILayout.Vector3IntField(name, value != null ? (Vector3Int)value : Vector3Int.zero);
            if (t == typeof(Color))
                return EditorGUILayout.ColorField(name, value != null ? (Color)value : Color.white);
            if (t == typeof(Rect))
                return EditorGUILayout.RectField(name, value != null ? (Rect)value : new Rect());
            if (t == typeof(Bounds))
                return EditorGUILayout.BoundsField(name, value != null ? (Bounds)value : new Bounds());
            if (t == typeof(AnimationCurve))
                return EditorGUILayout.CurveField(name, value as AnimationCurve ?? new AnimationCurve());
            if (t == typeof(Gradient))
                return EditorGUILayout.GradientField(name, value as Gradient ?? new Gradient());
            // Enum
            if (t.IsEnum)
            {
                value ??= Enum.GetValues(t).GetValue(0);
                return EditorGUILayout.EnumPopup(name, (Enum)value);
            }

            // UnityEngine.Object
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
            {
                var obj = value as UnityEngine.Object;

                obj = EditorGUILayout.ObjectField(name, obj, t, true);

                if (obj is ScriptableObject so)
                    DrawScriptableObjectInline(so);

                return obj;
            }
            // 配列
            if (t.IsArray)
            {
                Type elementType = t.GetElementType();
                IList list = value as IList;
                return DrawList(name, elementType, list);
            }
            // List
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = t.GetGenericArguments()[0];
                IList list = value as IList;
                return DrawList(name, elementType, list);
            }
            // 辞書
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return DrawDictionary(name, t, value);
            }
            // ScriptableObjectをインラインで描画
            return DrawObject(name, t, value);
        }
        IList DrawList(string name, Type elementType, IList list)
        {
            list ??= (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

            bool fold = GetFoldout(list);

            fold = EditorGUILayout.Foldout(fold, $"{name} [{list.Count}]");
            SetFoldout(list, fold);

            if (!fold)
                return list;

            EditorGUI.indentLevel++;

            int size = EditorGUILayout.IntField("Size", list.Count);

            while (list.Count < size)
                list.Add(GetDefault(elementType));

            while (list.Count > size)
                list.RemoveAt(list.Count - 1);

            for (int i = 0; i < list.Count; i++)
            {
                list[i] = DrawField(elementType, $"Element {i}", list[i]);
            }

            EditorGUI.indentLevel--;

            return list;
        }

        object DrawDictionary(string name, Type dictType, object dictObj)
        {
            var args = dictType.GetGenericArguments();

            Type keyType = args[0];
            Type valueType = args[1];

            IDictionary dict = dictObj as IDictionary;

            dict ??= (IDictionary)Activator.CreateInstance(dictType);

            bool fold = GetFoldout(dict);

            fold = EditorGUILayout.Foldout(fold, $"{name} [{dict.Count}]");
            SetFoldout(dict, fold);

            if (!fold)
                return dict;

            EditorGUI.indentLevel++;

            List<object> keys = new();

            foreach (var k in dict.Keys)
                keys.Add(k);

            foreach (var key in keys)
            {
                EditorGUILayout.BeginHorizontal();

                object newKey = DrawField(keyType, "Key", key);
                object newValue = DrawField(valueType, "Value", dict[key]);

                if (!Equals(newKey, key))
                {
                    dict.Remove(key);
                    dict[newKey] = newValue;
                }
                else
                {
                    dict[key] = newValue;
                }

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    dict.Remove(key);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add"))
            {
                dict[GetDefault(keyType)] = GetDefault(valueType);
            }

            EditorGUI.indentLevel--;

            return dict;
        }
        object DrawObject(string name, Type type, object value)
        {
            if (value == null)
                value = Activator.CreateInstance(type);

            bool fold = GetFoldout(value);

            fold = EditorGUILayout.Foldout(fold, name);

            SetFoldout(value, fold);

            if (!fold)
                return value;

            EditorGUI.indentLevel++;

            var fields = type.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            foreach (var f in fields)
            {
                var fieldValue = f.GetValue(value);

                var newValue = DrawField(f.FieldType, f.Name, fieldValue);

                if (!Equals(fieldValue, newValue))
                    f.SetValue(value, newValue);
            }

            EditorGUI.indentLevel--;

            return value;
        }
        void DrawScriptableObjectInline(ScriptableObject so)
        {
            if (so == null)
                return;

            if (!editorCache.TryGetValue(so, out var editor))
            {
                editor = CreateEditor(so);
                editorCache[so] = editor;
            }

            EditorGUILayout.BeginVertical("box");

            editor.OnInspectorGUI();

            EditorGUILayout.EndVertical();
        }

        object GetDefault(Type t)
        {
            if (t.IsValueType)
                return Activator.CreateInstance(t);

            return null;
        }

        bool GetFoldout(object key)
        {
            if (!foldouts.TryGetValue(key, out bool value))
            {
                value = false;
                foldouts[key] = value;
            }

            return value;
        }

        void SetFoldout(object key, bool value)
        {
            foldouts[key] = value;
        }

        /// <summary>
        /// ScriptableObjectのネストされたフィールドを再帰的に描画
        /// </summary>
        private void DrawNestedScriptableObjects(UnityEngine.Object obj, int depth = 0, HashSet<UnityEngine.Object> visited = null)
        {
            if (obj == null || depth > 3) return;

            visited ??= new HashSet<UnityEngine.Object>();

            if (visited.Contains(obj)) return; // 循環参照回避
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
                //配列の場合は、要素がScriptableObjectの参照になっている可能性があるので、さらにチェックする
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
        private void DrawSOReference(SerializedProperty prop, int depth, HashSet<UnityEngine.Object> visited, string overrideLabel = null)
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
            if (!editorCache.TryGetValue(nestedSO, out var cachedEditor) || cachedEditor == null)
            {
                Editor.CreateCachedEditor(nestedSO, null, ref cachedEditor);
                editorCache[nestedSO] = cachedEditor;
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
}
#endif


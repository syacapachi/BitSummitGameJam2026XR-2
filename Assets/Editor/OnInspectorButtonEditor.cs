#if UNITY_EDITOR

namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// [OnInspectorButton]属性を持つメソッドを、Inspectorにボタンとして表示。
    /// UnityEngine.Objectを継承したクラスごとにインスタンスが生成される。
    /// MonoBehaviour / ScriptableObject 両対応版。
    /// ネストした ScriptableObjectも再帰的に描画。
    /// </summary>
    [CustomEditor(typeof(UnityEngine.Object), true)]
    public class OnInspectorButtonEditor : Editor
    {
        //関数と、そのAttributeをもつラッパー構造体
        readonly struct MethodCache
        {
            public readonly MethodInfo Method;
            public readonly OnInspectorButtonAttribute Attribute;
            public MethodCache(
                MethodInfo method,
                OnInspectorButtonAttribute attribute)
            {
                Method = method;
                Attribute = attribute;
            }
        }
        //全てのキャッシュを初期化して持っておく
        private static readonly MethodCache[] allMethods =
            //TypeCacheで高速化(さらに高速化するなら、for文で回そう)
            TypeCache.GetMethodsWithAttribute<OnInspectorButtonAttribute>()
            .Select(method =>
                new MethodCache(
                    method,
                    //method.GetCustomAttribute<OnInspectorButtonAttribute>()より軽い
                    (OnInspectorButtonAttribute)
                    Attribute.GetCustomAttribute(method, typeof(OnInspectorButtonAttribute)))
                )
            .ToArray();

        //staticは、アセンブリロード時(スクリプト編集後など)や、Play時に再生成される。
        //クラスとOnInspectorButtonをもつ関数のキャッシュ,このデータは静的なのでstaticにすることでパフォーマンス向上
        private static readonly Dictionary<Type, MethodCache[]> methodCaches = new();
        //非UnityEngine.Onbjectのフィールド変数情報のキャッシュ
        private static readonly Dictionary<Type, FieldInfo[]> fieldCache = new();
        //関数の引数情報のキャッシュ(具体的な値ではないのでstatic)
        private static readonly Dictionary<MethodInfo, ParameterInfo[]> parameterInfoCaches = new();
        // メソッドと引数のキャッシュ (パフォーマンス向上のため),インスペクターごとに値が違うので非static
        private readonly Dictionary<MethodInfo, object[]> methodParameters = new();
        // 値更新時に自動で発火する場合、有効かどうか
        private readonly Dictionary<MethodInfo, bool> valiedInvokeEnabled = new();
        // 抽象クラスやインターフェースと、それを実装/継承する具体的なクラスのキャッシュ (描画できない型を識別するため)
        private readonly Dictionary<string, Type> abstructToClass = new();
        // Foldoutの状態のキャッシュ (複数インスペクターでの状態管理のため)
        private readonly Dictionary<string, bool> foldouts = new();
        // パラメータのFoldoutの状態のキャッシュ (複数インスペクターでの状態管理のため)
        private readonly Dictionary<MethodInfo, bool> parametersfoldouts = new();
        // ScriptableObjectのFoldout状態のキャッシュ (複数インスペクターでの状態管理のため)
        private readonly Dictionary<UnityEngine.Object, bool> foldoutStates = new();
        // ネストしたEditorキャッシュ (パフォーマンス向上のため)
        private readonly Dictionary<UnityEngine.Object, Editor> editorCache = new();
        // このフレームで値が更新されたか
        private bool isValueChangedThisFrame = false;
        private void OnDisable()
        {
            //キャッシュの明示的消去
            foldouts.Clear();
        }
        public override void OnInspectorGUI()
        {
            //初期化必須(無限ループ防止)
            isValueChangedThisFrame = false;
            serializedObject.Update();
            //base.OnInspectorGUI(); //これを呼ぶと、全てのフィールドが描画される。DrawDefaultInspector()と同様。
            //通常のインスペクター描画を行う。これを呼ばないと、通常のフィールドが表示されない。
            DrawDefaultInspector();
            //インスペクター上に関数を呼び出すためのボタンを描画する。対象のオブジェクトの型をリフレクションで調べて、[OnInspectorButton]属性が付いているメソッドを探し、ボタンを表示する。
            DrawInspectorButtons(target);

            // ネストしたScriptableObjectを再帰的に描画
            DrawNestedScriptableObjects(target);
            //変更を保存
            serializedObject.ApplyModifiedProperties();
        }
        private void DrawInspectorButtons(object obj)
        {
            //各インスペクターで呼ばれる。
            var targetType = obj.GetType();

            //自分自身は描画しない(エラー回避)
            if (targetType == typeof(OnInspectorButtonEditor)) return;

            // キャッシュからメソッドを取得、なければリフレクションで取得してキャッシュに保存
            if (!methodCaches.TryGetValue(targetType, out var methods))
            {
                // メソッドを列挙(このクラスの中の関数をリフレクションで取得)
                //methods = targetType.GetMethods(
                //    BindingFlags.Instance
                //    | BindingFlags.Static
                //    | BindingFlags.Public
                //    | BindingFlags.NonPublic)
                //    .Where(m =>
                //        m.GetCustomAttribute(typeof(OnInspectorButtonAttribute)) != null)
                //    .Select(m =>
                //        new MethodCache()
                //        {
                //            Method = m,
                //            Attribute = m.GetCustomAttribute<OnInspectorButtonAttribute>()
                //        })
                //    .ToArray();

                methods = allMethods
                    .Where(cache =>
                        //描画クラスが、関数を定義したクラスの子か
                        cache.Method.DeclaringType.IsAssignableFrom(targetType)
                        &&
                        (
                            !cache.Attribute.HideWhenChildClass
                            //子クラスのみなら、一致してるか
                            || cache.Method.DeclaringType == targetType
                        )
                    )
                    .OrderBy(cache => cache.Attribute.Order)
                    .ToArray();

                methodCaches[targetType] = methods;
            }

            foreach (var method in methods)
            {
                // 実行中のみ表示
                if (method.Attribute.ShowOnlyInPlayMode && !Application.isPlaying)
                    continue;

                DrawButtonForMethod(obj, method.Method, method.Attribute);
            }
        }
        private void DrawButtonForMethod(object invokeTarget, MethodInfo method, OnInspectorButtonAttribute attr)
        {
            //ラベルがない場合は関数名で上書き
            string buttonLabel = string.IsNullOrEmpty(attr.Label) ? method.Name : attr.Label;

            //引数を取得して引数情報を初期化
            if (!parameterInfoCaches.TryGetValue(method, out var parameters))
            {
                parameters = method.GetParameters(); ;
                parameterInfoCaches[method] = parameters;
            }

            if (parameters.Length == 0)
            {
                if (GUILayout.Button(buttonLabel))
                    InvokeMethod(invokeTarget, method, null);

                return;
            }
            //初回は辞書に登録することで次回以降の検索の手間を省く
            if (!methodParameters.TryGetValue(method, out var values))
            {
                values = new object[parameters.Length];
                methodParameters[method] = values;
            }
            //パラメータを隠せるFoldout
            if (!parametersfoldouts.TryGetValue(method, out var enable))
            {
                enable = true;
                parametersfoldouts[method] = enable;
            }

            EditorGUILayout.BeginVertical("box");
            parametersfoldouts[method] = EditorGUILayout.Foldout(enable, $"{method.Name} Parameters", true);

            if (parametersfoldouts[method])
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < parameters.Length; i++)
                {
                    //変更を検知するエリア
                    EditorGUI.BeginChangeCheck();
                    var param = parameters[i];
                    values[i] = DrawField(param.ParameterType, param.Name, values[i], $"{target.GetInstanceID()}#{method.Name}#{param.MetadataToken}");
                    //変更を検知
                    if (EditorGUI.EndChangeCheck())
                    {
                        isValueChangedThisFrame = true;
                    }
                    //isValueChangedThisFrame |= GUI.changed;//GUI全体で値が変わったか

                }
                EditorGUI.indentLevel--;
            }
            if (GUILayout.Button(buttonLabel))
                InvokeMethod(invokeTarget, method, values);

            //自動発火機能がある場合
            if (attr.ValidateInvoke)
            {
                if (!valiedInvokeEnabled.TryGetValue(method, out var enableValidate))
                {
                    enableValidate = false;
                    valiedInvokeEnabled[method] = enableValidate;
                }
                valiedInvokeEnabled[method] = EditorGUILayout.ToggleLeft(
                    $"Auto Invoke{method.Name}",
                    enableValidate
                    );

                if (valiedInvokeEnabled[method] && isValueChangedThisFrame)
                {
                    InvokeMethod(invokeTarget, method, values);
                    //再発火防止
                    isValueChangedThisFrame = false;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static void InvokeMethod(object invokeTarget, MethodInfo method, object[] values)
        {
            try
            {
                method.Invoke(invokeTarget, values);
            }
            catch (Exception e)
            {
                string paramLog = LogUtility.BuildParameterLog(method, values);
                //エラースクリプトへのリンクを載せる。
                Debug.LogException(
                   new Exception(
                       $"[{nameof(OnInspectorButtonEditor)}] {method.DeclaringType.FullName}.{method.Name} \n({paramLog})",
                       e),
                   invokeTarget as UnityEngine.Object//非UnityEngine。Objectならnullになって何も出ない
                   );
            }
        }

        private object DrawField(Type t, string name, object currentValue, string path)
        {
            name = ObjectNames.NicifyVariableName(name);
            //Nullableな型は、nullを許容するためにNullable.GetUnderlyingTypeで元の型を取得して描画する。
            if (Nullable.GetUnderlyingType(t) is Type underlyingType)
            {
                currentValue ??= GetDefault(underlyingType);
                return DrawField(underlyingType, name, currentValue, path);
            }
            if (t == typeof(int))
                return EditorGUILayout.IntField(name, currentValue != null ? (int)currentValue : 0);
            if (t == typeof(byte))
                return (byte)EditorGUILayout.IntField(name, currentValue != null ? (byte)currentValue : 0);
            if (t == typeof(short))
                return (short)EditorGUILayout.IntField(name, currentValue != null ? (short)currentValue : 0);
            if (t == typeof(ushort))
                return (ushort)EditorGUILayout.IntField(name, currentValue != null ? (ushort)currentValue : 0);
            if (t == typeof(uint))
                return (uint)EditorGUILayout.IntField(name, currentValue != null ? (int)(uint)currentValue : 0);
            if (t == typeof(ulong))
                return (ulong)EditorGUILayout.LongField(name, currentValue != null ? (long)(ulong)currentValue : 0);
            if (t == typeof(sbyte))
                return (sbyte)EditorGUILayout.IntField(name, currentValue != null ? (sbyte)currentValue : 0);
            if (t == typeof(decimal))
                return (decimal)EditorGUILayout.FloatField(name, currentValue != null ? (float)(decimal)currentValue : 0f);
            if (t == typeof(float))
                return EditorGUILayout.FloatField(name, currentValue != null ? (float)currentValue : 0f);
            if (t == typeof(double))
                return EditorGUILayout.DoubleField(name, currentValue != null ? (double)currentValue : 0);
            if (t == typeof(long))
                return EditorGUILayout.LongField(name, currentValue != null ? (long)currentValue : 0);
            if (t == typeof(string))
                return EditorGUILayout.TextField(name, currentValue as string ?? "");
            if (t == typeof(char))
            {
                string str = EditorGUILayout.TextField(name, currentValue != null ? ((char)currentValue).ToString() : "");
                return string.IsNullOrEmpty(str) ? '\0' : str[0];
            }
            if (t == typeof(DateTime))
            {
                string str = EditorGUILayout.TextField(name, currentValue != null ? ((DateTime)currentValue).ToString("o") : DateTime.Now.ToString("o"));
                if (DateTime.TryParse(str, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
                    return result;
                return currentValue ?? DateTime.Now;
            }
            if (t == typeof(TimeSpan))
            {
                string str = EditorGUILayout.TextField(name, currentValue != null ? ((TimeSpan)currentValue).ToString() : TimeSpan.Zero.ToString());
                if (TimeSpan.TryParse(str, out var result))
                    return result;
                return currentValue ?? TimeSpan.Zero;
            }
            if (t == typeof(bool))
                return EditorGUILayout.Toggle(name, currentValue != null && (bool)currentValue);
            if (t == typeof(Vector2))
                return EditorGUILayout.Vector2Field(name, currentValue != null ? (Vector2)currentValue : Vector2.zero);
            if (t == typeof(Vector3))
                return EditorGUILayout.Vector3Field(name, currentValue != null ? (Vector3)currentValue : Vector3.zero);
            if (t == typeof(Vector4))
                return EditorGUILayout.Vector4Field(name, currentValue != null ? (Vector4)currentValue : Vector4.zero);
            if (t == typeof(Vector2Int))
                return EditorGUILayout.Vector2IntField(name, currentValue != null ? (Vector2Int)currentValue : Vector2Int.zero);
            if (t == typeof(Vector3Int))
                return EditorGUILayout.Vector3IntField(name, currentValue != null ? (Vector3Int)currentValue : Vector3Int.zero);
            if (t == typeof(Color))
                return EditorGUILayout.ColorField(name, currentValue != null ? (Color)currentValue : Color.white);
            if (t == typeof(Rect))
                return EditorGUILayout.RectField(name, currentValue != null ? (Rect)currentValue : new Rect());
            if (t == typeof(RectInt))
                return EditorGUILayout.RectIntField(name, currentValue != null ? (RectInt)currentValue : new RectInt());
            if (t == typeof(Bounds))
                return EditorGUILayout.BoundsField(name, currentValue != null ? (Bounds)currentValue : new Bounds());
            if (t == typeof(BoundsInt))
                return EditorGUILayout.BoundsIntField(name, currentValue != null ? (BoundsInt)currentValue : new BoundsInt());
            if (t == typeof(AnimationCurve))
                return EditorGUILayout.CurveField(name, currentValue as AnimationCurve ?? new AnimationCurve());
            if (t == typeof(Gradient))
                return EditorGUILayout.GradientField(name, currentValue as Gradient ?? new Gradient());
            if (t == typeof(LayerMask))
                return (LayerMask)EditorGUILayout.MaskField(name, currentValue != null ? ((LayerMask)currentValue).value : 0, UnityEditorInternal.InternalEditorUtility.layers);
            if (t == typeof(Quaternion))
                return Quaternion.Euler(EditorGUILayout.Vector3Field(name, ((Quaternion?)currentValue)?.eulerAngles ?? Vector3.zero));
            if (t == typeof(UnityEvent))
            {
                // UnityEventは専用のプロパティドローアーが必要なので、ここでは描画できないことを示すメッセージを表示する。
                EditorGUILayout.HelpBox($"UnityEvent type is not supported for field {name}.", MessageType.Error);
                return currentValue;
            }

            // Enum
            if (t.IsEnum)
            {
                currentValue ??= Enum.GetValues(t).GetValue(0);
                if (t.GetCustomAttribute<FlagsAttribute>() != null)
                {
                    // [Flags]属性がある場合はEnumFlagsFieldで描画
                    return EditorGUILayout.EnumFlagsField(name, (Enum)currentValue);
                }
                // 通常のEnumはEnumPopupで描画
                return EditorGUILayout.EnumPopup(name, (Enum)currentValue);
            }

            // UnityEngine.Object
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
            {
                var obj = currentValue as UnityEngine.Object;

                obj = EditorGUILayout.ObjectField(name, obj, t, true);

                if (obj is ScriptableObject so)
                    DrawScriptableObjectInline(so);

                return obj;
            }
            // 配列
            if (t.IsArray)
            {
                Type elementType = t.GetElementType();
                Array array = currentValue as Array;
                return DrawArray(name, elementType, array, path);
            }
            // List
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = t.GetGenericArguments()[0];
                IList list = currentValue as IList;
                return DrawList(elementType, name, list, path);
            }
            // 辞書
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return DrawDictionary(name, t, currentValue, path);
            }
            // 抽象クラスやインターフェースは直接描画できないので、実装/継承する具体的なクラスを選択して描画する。選択されていない場合は、選択ボタンを表示する。
            if (t.IsAbstract || t.IsInterface)
            {
                return DrawAbstructOrInterface(name, t, currentValue, path);
            }
            //リスト、辞書、抽象クラス/インターフェース以外のジェネリック型
            if (t.IsGenericType)
            {
                if (t.IsGenericTypeDefinition)
                {
                    //Generic<> やGeneric<,>など、ジェネリック型の定義自体の場合は、描画できないので、エラーメッセージを表示する
                    // ジェネリック型の定義自体は描画できないので、エラーメッセージを表示する
                    EditorGUILayout.HelpBox($"Generic type definition {t.Name} is not supported.", MessageType.Error);
                    return currentValue;
                }
                else if (t.ContainsGenericParameters)
                {
                    //Generic<T>やGeneric<T,U>など、ジェネリック型の引数に未指定の型パラメータが含まれている場合は描画できないので、エラーメッセージを表示する
                    // ジェネリック型の引数に未指定の型パラメータが含まれている場合も描画できないので、エラーメッセージを表示する
                    EditorGUILayout.HelpBox($"Generic type {t.Name}<{string.Join(", ", t.GetGenericArguments().Select(t => t.Name))}> contains unspecified type parameters and is not supported.", MessageType.Error);
                    return currentValue;
                }
                //上記以外のジェネリック型は、通常のクラスと同様に描画する
            }
            // ScriptableObjectをインラインで描画
            return DrawObject(name, t, currentValue, path);
        }
        IList DrawList(Type elementType, string name, IList list, string path)
        {
            // nullの場合は新しいリストを作成
            list ??= (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            // Foldoutの状態をリスト自体で管理することで、同じリストを複数のインスペクターで描画している場合でも、展開状態を共有できる。
            bool fold = GetFoldout(path);

            // Foldoutを描画して状態を更新
            fold = EditorGUILayout.Foldout(fold, $"{name} [{list.Count}]", true);
            SetFoldout(path, fold);

            if (!fold)
                return list;

            //展開されている場合は要素を描画
            EditorGUI.indentLevel++;

            int size = EditorGUILayout.IntField("Size", list.Count);

            while (list.Count < size)
                list.Add(GetDefault(elementType));

            while (list.Count > size)
                list.RemoveAt(list.Count - 1);

            for (int i = 0; i < list.Count; i++)
            {
                //要素を描画して更新
                list[i] = DrawField(elementType, $"{name} Element[{i}]", list[i], $"{path}#Element[{i}]");
            }
            if (GUILayout.Button("Add"))
            {
                list.Add(GetDefault(elementType));
            }

            EditorGUI.indentLevel--;

            return list;
        }
        Array DrawArray(string name, Type elementType, Array array, string path)
        {
            // 配列はサイズ変更のたびに新しい配列を作成して要素をコピーする必要があるため、Array.Resizeのような機能を自前で実装する。
            static Array ArrayResize(Array oldArray, int newSize, Type elementType)
            {
                var newArray = Array.CreateInstance(elementType, newSize);
                if (oldArray != null)
                {
                    for (int i = 0; i < Mathf.Min(oldArray.Length, newSize); i++)
                        newArray.SetValue(oldArray.GetValue(i), i);
                }
                return newArray;
            }

            array ??= Array.CreateInstance(elementType, 0);
            // nullの場合は新しいリストを作成
            bool fold = GetFoldout(path);
            fold = EditorGUILayout.Foldout(fold, $"{name} [{array.Length}]", true);
            SetFoldout(path, fold);


            if (!fold)
                return array;

            //展開されている場合は要素を描画
            EditorGUI.indentLevel++;


            int newSize = EditorGUILayout.IntField("Size", array.Length);

            if (array == null || newSize != array.Length)
            {
                array = ArrayResize(array, newSize, elementType);
            }

            for (int i = 0; i < array.Length; i++)
            {
                //要素を描画して更新
                array.SetValue(
                    DrawField(elementType, $"{name} Element[{i}]", array.GetValue(i), $"{path}#ArrayElement[{i}]"),
                    i);
            }
            if (GUILayout.Button("Add"))
            {
                array = ArrayResize(array, array.Length + 1, elementType);
            }

            EditorGUI.indentLevel--;

            return array;
        }

        IDictionary DrawDictionary(string name, Type dictType, object dictObj, string path)
        {
            var args = dictType.GetGenericArguments();

            Type keyType = args[0];
            Type valueType = args[1];

            IDictionary dict = dictObj as IDictionary;

            dict ??= (IDictionary)Activator.CreateInstance(dictType);

            bool fold = GetFoldout(path);

            fold = EditorGUILayout.Foldout(fold, $"{name} [{dict.Count}]", true);
            SetFoldout(path, fold);

            if (!fold)
                return dict;

            EditorGUI.indentLevel++;

            List<object> keys = new();

            foreach (var k in dict.Keys)
                keys.Add(k);

            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                EditorGUILayout.BeginVertical();

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    dict.Remove(key);
                    break;
                }

                object newKey = DrawField(keyType, $"{name} Key [{i}]", key, $"{path}#Key[{i}]");
                object newValue = DrawField(valueType, $"{name} Value [{i}]", dict[key], $"{path}#Value[{i}]");

                //キーが変更された場合は、古いキーを削除して新しいキーで追加。そうでない場合は値だけ更新。
                if (!Equals(newKey, key))
                {
                    dict.Remove(key);
                    dict[newKey] = newValue;
                }
                else
                {
                    dict[key] = newValue;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add"))
            {
                var key = GetDefault(keyType);
                if (key == null)
                {
                    EditorUtility.DisplayDialog("No Concrete Class Found", $"Cannot add entry with null pathkey for type {keyType.Name}.", "OK");
                    return dict;
                }
                dict[key] = GetDefault(valueType);
            }

            EditorGUI.indentLevel--;

            return dict;
        }

        object DrawObject(string name, Type type, object value, string path)
        {
            value ??= Activator.CreateInstance(type);

            bool fold = GetFoldout(path);

            fold = EditorGUILayout.Foldout(fold, name, true);

            SetFoldout(path, fold);

            if (!fold)
                return value;

            EditorGUI.indentLevel++;

            if (!fieldCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

                fieldCache[type] = fields;
            }
            //フィールドを列挙して描画

            foreach (var f in fields)
            {
                var fieldValue = f.GetValue(value);

                var newValue = DrawField(f.FieldType, f.Name, fieldValue, $"{path}#{f.Name}");
                if (!Equals(fieldValue, newValue))
                    f.SetValue(value, newValue);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"{type} Methods");
                //これで非UnityEngine.Objectの関数が呼べる
                DrawInspectorButtons(value);
            }

            EditorGUI.indentLevel--;

            return value;
        }
        /// <summary>
        /// 抽象クラスやインターフェースは直接描画できないので、実装/継承する具体的なクラスを選択して描画する。選択されていない場合は、選択ボタンを表示する。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        object DrawAbstructOrInterface(string name, Type type, object value, string path)
        {
            if (abstructToClass.TryGetValue(path, out var concreteType))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{type.Name} ▶ {concreteType.Name}", EditorStyles.boldLabel);
                    if (GUILayout.Button("Delete", GUILayout.Width(100)))
                    {
                        abstructToClass.Remove(path);
                        return null;
                    }
                }
                return DrawField(concreteType, name, value, $"{path}#{concreteType}");
            }
            if (GUILayout.Button($"Select Class ({type.Name})"))
            {
                ShowTypeMenu(type, path);
            }

            return value;
        }
        /// <summary>
        /// 具象クラスの選択メニューを表示する。選択されたクラスは、抽象クラスやインターフェースのキャッシュに保存される。次回以降は直接描画されるようになる。
        /// </summary>
        /// <param name="baseType"></param>
        private void ShowTypeMenu(Type baseType, string path)
        {
            var menu = new GenericMenu();
            //var types = AppDomain.CurrentDomain.GetAssemblies()
            //    .SelectMany(a => a.GetTypes())
            //    .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            //Unity内部のキャッシュで検索高速化
            var types = TypeCache.GetTypesDerivedFrom(baseType);
            if (types.Count == 0)
            {
                EditorUtility.DisplayDialog("No Concrete Class Found", $"No concrete class found that implements/inherits {baseType.Name}.", "OK");
                return;
            }
            foreach (var type in types)
            {
                menu.AddItem(new GUIContent(type.FullName), false, () =>
                {
                    abstructToClass[path] = type;
                });
            }
            menu.ShowAsContext();
        }
        void DrawScriptableObjectInline(ScriptableObject so)
        {
            if (so == null)
                return;

            if (!editorCache.TryGetValue(so, out var editor))
            {
                Editor.CreateCachedEditor(so, null, ref editor);
                editorCache[so] = editor;
            }

            EditorGUILayout.BeginVertical("box");

            editor.OnInspectorGUI();

            EditorGUILayout.EndVertical();
        }
        static object GetDefault(Type t)
        {
            if (t == null)
                return null;
            //UnityEngine.Objectは、作っちゃダメ
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
                return null;
            if (t.IsValueType)
                return Activator.CreateInstance(t);

            //生成できない型の場合はnullを返す
            try
            {
                return Activator.CreateInstance(t);
            }
            catch
            {
                return null;
            }
        }
        bool GetFoldout(string pathkey)
        {
            if (!foldouts.TryGetValue(pathkey, out bool value))
            {
                value = false;
                foldouts[pathkey] = value;
            }

            return value;
        }

        void SetFoldout(string pathkey, bool value)
        {
            foldouts[pathkey] = value;
        }

        /// <summary>
        /// ScriptableObjectのネストされたフィールドを再帰的に描画
        /// </summary>
        private void DrawNestedScriptableObjects(UnityEngine.Object obj, int depth = 0, HashSet<UnityEngine.Object> visited = null, string overrideLabel = null)
        {
            if (obj == null || depth > 0) return;

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
            //状態を保存しておくことで、複数インスペクターで同じSOを描画している場合でも、展開状態を共有できる。
            so.ApplyModifiedProperties();
        }
        private void DrawSOReference(SerializedProperty prop, int depth, HashSet<UnityEngine.Object> visited, string overrideLabel = null)
        {
            UnityEngine.Object refObj = prop.objectReferenceValue;
            if (refObj == null) return;
            if (refObj is not ScriptableObject nestedSO)
            {
                // ScriptableObjectじゃない場合は再帰
                DrawNestedScriptableObjects(refObj, depth + 1, visited, $"{refObj.name} ");
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

                //エディター描画(この中でも呼ばれするの実質再帰)
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
#endif
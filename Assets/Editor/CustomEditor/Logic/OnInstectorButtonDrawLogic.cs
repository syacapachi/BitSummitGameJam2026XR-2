namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Events;

    public static class OnInstectorButtonDrawLogic
    {
        //描画結果
        [Flags]
        public enum InspectorButtonResult
        {
            None = 0,
            Invoked = 1 << 0,
            ParameterChanged = 1 << 1,
            RequiresRepaint = 1 << 2,
            Exception = 1 << 3,
            DrawedButton = 1 << 4,
        }
        public enum CreateFailureReason
        {
            /// <summary>
            /// 理由なし->作れる。
            /// </summary>
            None = 0,
            NoDefaultConstructor = 1,
            PrivateDefaultConstructorOnly = 2,
        }
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
        readonly struct FieldCache
        {
            public readonly FieldInfo Field;
            public readonly ShowInspectorAttribute Attribute;
            public FieldCache(FieldInfo field, ShowInspectorAttribute attribute)
            {
                Field = field;
                Attribute = attribute;
            }
        }
        /// <summary>
        /// 描画対象のキーとなる構造体
        /// stringでキー比較だと、結合でGCがあるほかヒープに確保されるので効率が悪いので
        /// スタックにある+小さい構造体で比較。
        /// </summary>
        readonly struct DrawPathKey : IEquatable<DrawPathKey>
        {
            public readonly int rootId;
            public readonly int methodToken;
            public readonly int paramToken;
            public readonly int depth;
            public readonly int index;

            public DrawPathKey(int rootId, int methodToken, int paramToken, int depth, int index)
            {
                this.rootId = rootId;
                this.methodToken = methodToken;
                this.paramToken = paramToken;
                this.depth = depth;
                this.index = index;
            }

            public DrawPathKey Child(int token, int childIndex = 0)
            {
                int parentToken = CombineToken(paramToken, index);
                return new DrawPathKey(
                    rootId,
                    methodToken,
                    CombineToken(parentToken, token),
                    depth + 1,
                    childIndex);
            }

            public readonly bool Equals(DrawPathKey other)
            {
                return rootId == other.rootId
                    && methodToken == other.methodToken
                    && paramToken == other.paramToken
                    && depth == other.depth
                    && index == other.index;
            }

            public readonly override bool Equals(object obj)
            {
                return obj is DrawPathKey other && Equals(other);
            }

            public readonly override int GetHashCode()
            {
                //オーバーフローを無視する
                unchecked
                {
                    int hash = rootId;
                    hash = (hash * 397) ^ methodToken;
                    hash = (hash * 397) ^ paramToken;
                    hash = (hash * 397) ^ depth;
                    hash = (hash * 397) ^ index;
                    return hash;
                }
            }
        }
        readonly struct CreateFailure
        {
            public readonly CreateFailureReason Reason;
            public readonly string Message;

            public CreateFailure(CreateFailureReason reason, Type type)
            {
                Reason = reason;
                Message = 
                    reason == CreateFailureReason.None
                    ? string.Empty
                    : $"{type.Name} は自動生成できません By {reason}";
            }
        }
        /// <summary>
        /// validateInvokeEnabled,parametersFoldoutsについてはreadonlyじゃないので、そこで、内部効率化が行われていない場合がある。
        /// </summary>
        sealed class MethodDetails
        {
            /// <summary>
            /// パラメータの情報
            /// </summary>
            public readonly ParameterInfo[] ParameterInfos;
            /// <summary>
            /// パラメータのキャッシュ
            /// </summary>
            public readonly GUIContent ParameterLabel;
            /// <summary>
            /// 関数名前のキャッシュ
            /// </summary>
            public readonly GUIContent MethodLabel;
            /// <summary>
            /// 自動発火のラベル
            /// </summary>
            public readonly GUIContent AutoInvokeLabel;
            /// <summary>
            /// 動的発火機能が有効かどうか
            /// </summary>
            public bool validateInvokeEnabled = false;
            /// <summary>
            /// パラメータを描画するか
            /// </summary>
            public bool parametersFoldouts = true;

            public MethodDetails(ParameterInfo[] parameterInfos, string methodName, string methodLabel)
            {
                this.ParameterInfos = parameterInfos;
                //stringを保持 or GUIContentで保持
                ParameterLabel = new GUIContent($"{methodName} Parameters", methodName);
                MethodLabel = new GUIContent(methodLabel, methodName);
                AutoInvokeLabel = new GUIContent($"Auto Invoke {methodName}", methodName);
            }
        }
        /// <summary>
        /// GUIContentのキャッシュ
        /// </summary>
        private static readonly GUIContent Size = new GUIContent("Size","Size");
        private static readonly GUIContent Add = new GUIContent("Add", "Add");
        private static readonly GUIContent Delete = new GUIContent("Delete", "Delete");
        //全てのキャッシュを初期化して持っておく
        private static readonly MethodCache[] allMethods;
        private static readonly FieldCache[] allFields;
        private static readonly List<MethodCache> tmpCacheList = new();

        //static初期化
        static OnInstectorButtonDrawLogic()
        {
            //TypeCacheで高速化(さらに高速化するなら、for文で回そう)
            allMethods =
            TypeCache.GetMethodsWithAttribute<OnInspectorButtonAttribute>()
            //リフレクションで全てのメソッドを取得して、OnInspectorButtonAttributeが付いているものだけを抽出してキャッシュに保存
            .Select(method =>
                new MethodCache(
                    method,
                    //method.GetCustomAttribute<OnInspectorButtonAttribute>()より軽い
                    (OnInspectorButtonAttribute)
                    Attribute.GetCustomAttribute(method, typeof(OnInspectorButtonAttribute)))
                )
            .ToArray();

            allFields =
                TypeCache.GetFieldsWithAttribute<ShowInspectorAttribute>()
                .Select(field =>
                new FieldCache(
                    field,
                    field.GetCustomAttribute<ShowInspectorAttribute>())
                ).ToArray();
        }
        //staticやreadoblyは、アセンブリロード時(スクリプト編集後など)や、Play時に再生成される。
        //正しく言えば、Unityがシリアライズできるデータは、アセンブリロード前に一時退避しアセンブリロード後に再生成&代入される仕様。
        //もし、保存したい値がある場合は、非staticクラスでISerializationCallbackReceiverを継承すること。

        //DrawInspectorButtonsで使用する、クラスごとのOnInspectorButtonをもつ関数のキャッシュ。クラスごとにキャッシュすることで、同じクラスのオブジェクトを複数描画している場合でも、リフレクションのコストを削減できる。
        //クラスとOnInspectorButtonをもつ関数のキャッシュ,このデータは静的なのでstaticにすることでパフォーマンス向上
        private static readonly Dictionary<Type, MethodCache[]> methodCaches = new();
        //非UnityEngine.Onbjectのフィールド変数情報のキャッシュ
        private static readonly Dictionary<Type, FieldInfo[]> fieldCache = new();
        //自動生成できるかのキャッシュ
        private static readonly Dictionary<Type, CreateFailure> createFailureReasonCache = new();
        // メソッドと引数のキャッシュ (パフォーマンス向上のため),(InstanceID,MethodInfoの組みなので)static
        private static readonly Dictionary<(int, MethodInfo), object[]> methodParameters = new();
        // パラメータのFoldout,関数の引数情報,自動発火機能のキャッシュ
        private static readonly Dictionary<MethodInfo, MethodDetails> methodDetails = new();
        // 抽象クラスやインターフェースと、それを実装/継承する具体的なクラスのキャッシュ (描画できない型を識別するため)
        private static readonly Dictionary<DrawPathKey, Type> abstractToClass = new();
        // Foldoutの状態のキャッシュ pathは一意なのでstatic
        private static readonly Dictionary<DrawPathKey, bool> foldouts = new();
        // DrawPathKeyを文字列として使う必要がある場合のキャッシュ
        private static readonly Dictionary<DrawPathKey, string> pathStrings = new();

        private const int NullableToken = -1;
        private const int ListElementToken = -2;
        private const int ArrayElementToken = -3;
        private const int DictionaryKeyToken = -4;
        private const int DictionaryValueToken = -5;
        private const int ConcreteTypeToken = -6;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetRootId(object target)
        {
            return target is UnityEngine.Object unityObject
                ? unityObject.GetInstanceID()
                : target.GetHashCode();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CombineToken(int parentToken, int childToken)
        {
            unchecked
            {
                return (parentToken * 397) ^ childToken;
            }
        }

        private static string GetPathString(DrawPathKey pathKey)
        {
            if (!pathStrings.TryGetValue(pathKey, out string path))
            {
                path = $"{pathKey.rootId}#{pathKey.methodToken}#{pathKey.paramToken}#{pathKey.depth}#{pathKey.index}";
                pathStrings[pathKey] = path;
            }
            return path;
        }

        /// <summary>
        /// [OnInspectorButtonEditor]がある関数を描画します。
        /// </summary>
        /// <param name="obj"> 描画対象のインスタンス(関数の発火元) </param>
        /// <returns> 結果状態を表すenum </returns>
        public static InspectorButtonResult DrawInspectorButtons(object obj)
        {
            //各インスペクターで呼ばれる。
            var targetType = obj.GetType();

            //自分自身は描画しない(エラー回避)
            if (targetType == typeof(OnInspectorButtonEditor)) return InspectorButtonResult.None;

            // キャッシュからメソッドを取得、なければリフレクションで取得してキャッシュに保存
            if (!methodCaches.TryGetValue(targetType, out var methods))
            {
                tmpCacheList.Clear();
                foreach (var cache in allMethods)
                {
                    if(
                        //描画クラスが、関数を定義したクラスの子か
                        cache.Method.DeclaringType.IsAssignableFrom(targetType)
                        && 
                        (
                            !cache.Attribute.HideWhenChildClass
                            //子クラスのみなら、一致してるか
                            || cache.Method.DeclaringType == targetType
                        )
                    )
                    {
                        tmpCacheList.Add(cache);
                    }
                }
                //ここもforで作るとGC少ないちなみに、ToArrayが一番Alloc。
                methods = tmpCacheList.OrderBy(cache => cache.Attribute.Order).ToArray();

                methodCaches[targetType] = methods;
            }

            InspectorButtonResult result = InspectorButtonResult.None;
            foreach (var method in methods)
            {
                // 実行中のみ表示
                if (method.Attribute.ShowOnlyInPlayMode && !Application.isPlaying)
                    continue;

                result |= DrawButtonForMethod(obj, method.Method, method.Attribute);
            }
            return result;
        }
        private static InspectorButtonResult DrawButtonForMethod(object invokeTarget, MethodInfo method, OnInspectorButtonAttribute attr)
        {
            //ラベルがない場合は関数名で上書き

            if (!methodDetails.TryGetValue(method, out var details))
            {
                //関数名のラベルを初期化(毎回やるとGC)
                string buttonLabel = string.IsNullOrEmpty(attr.Label) ? method.Name : attr.Label;
                //引数を取得して引数情報を初期化
                ParameterInfo[] param = method.GetParameters(); ;
                details = new(param, method.Name, buttonLabel);
                methodDetails[method] = details;
            }


            if (details.ParameterInfos.Length == 0)
            {
                //stringだと、一時GUIContextが生成されるので、GUIContentをキャッシュ化
                if (GUILayout.Button(details.MethodLabel))
                {
                    return InspectorButtonResult.DrawedButton | InvokeMethod(invokeTarget, method, null);
                }

                return InspectorButtonResult.DrawedButton;
            }

            //初回は辞書に登録することで次回以降の検索の手間を省く
            if (!methodParameters.TryGetValue((invokeTarget.GetHashCode(), method), out var values))
            {
                values = new object[details.ParameterInfos.Length];
                methodParameters[(invokeTarget.GetHashCode(), method)] = values;
            }

            //この描画中に値が更新されたか
            bool isValueChangedThisFrame = false;
            InspectorButtonResult result = InspectorButtonResult.DrawedButton;
            using (new EditorGUILayout.VerticalScope("box"))
            {
                details.parametersFoldouts = EditorGUILayout.Foldout(details.parametersFoldouts, details.ParameterLabel, true);


                if (details.parametersFoldouts)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < details.ParameterInfos.Length; i++)
                    {
                        var param = details.ParameterInfos[i];
                        //変更を検知するエリア
                        EditorGUI.BeginChangeCheck();
                        //引数の値を描画して更新,
                        //パスは、対象のHashCode(),(UnityEngine.ObjectならインスタンスIDがくる)と
                        //関数IDと引数IDで一意になるようにする。これで、同じ関数を複数描画している場合でも、引数の値が混ざらないようにする。
                        //stringだと、結合でGCがあるほか、ヒープに確保されるので効率が悪い
                        DrawPathKey pathKey = new(
                            GetRootId(invokeTarget),
                            method.MetadataToken,
                            param.MetadataToken,
                            0,
                            i);
                        values[i] = DrawField(param.ParameterType, param.Name, values[i], pathKey);
                        //変更を検知
                        if (EditorGUI.EndChangeCheck())
                        {
                            isValueChangedThisFrame = true;
                        }
                        //isValueChangedThisFrame |= GUI.changed;//GUI全体で値が変わったか

                    }
                    EditorGUI.indentLevel--;
                }

                if (isValueChangedThisFrame)
                {
                    result |= InspectorButtonResult.ParameterChanged;
                }

                if (GUILayout.Button(details.MethodLabel))
                {
                    return InspectorButtonResult.DrawedButton | InvokeMethod(invokeTarget, method, values);
                }

                //自動発火機能がある場合
                if (attr.ValidateInvoke)
                {
                    details.validateInvokeEnabled = EditorGUILayout.ToggleLeft(
                        details.AutoInvokeLabel,
                        details.validateInvokeEnabled
                        );

                    if (details.validateInvokeEnabled && isValueChangedThisFrame)
                    {
                        return InspectorButtonResult.DrawedButton | InspectorButtonResult.ParameterChanged | InvokeMethod(invokeTarget, method, values);
                    }
                }
            }
            return result;
        }

        private static InspectorButtonResult InvokeMethod(object invokeTarget, MethodInfo method, object[] values)
        {
            try
            {
                method.Invoke(invokeTarget, values);
                return InspectorButtonResult.Invoked;
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
                return InspectorButtonResult.Exception;
            }
        }
        private static object DrawField(Type t, string name, object currentValue, DrawPathKey pathKey)
        {
            name = ObjectNames.NicifyVariableName(name);
            //Nullableな型は、nullを許容するためにNullable.GetUnderlyingTypeで元の型を取得して描画する。
            if (Nullable.GetUnderlyingType(t) is Type underlyingType)
            {
                currentValue ??= GetDefaultOrNull(underlyingType);
                return DrawField(underlyingType, name + "?", currentValue, pathKey.Child(NullableToken));
            }
            //この辺あとで、型と描画方法の対応表みたいなの作って整理するかも
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
                    DrawScriptableObjectInline(so, pathKey);

                return obj;
            }
            // 配列
            if (t.IsArray)
            {
                Type elementType = t.GetElementType();
                Array array = currentValue as Array;
                return DrawArray(elementType, name, array, pathKey);
            }
            // List
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = t.GetGenericArguments()[0];
                IList list = currentValue as IList;
                return DrawList(elementType, name, list, pathKey);
            }
            // 辞書
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return DrawDictionary(t, name, currentValue, pathKey);
            }
            // 抽象クラスやインターフェースは直接描画できないので、実装/継承する具体的なクラスを選択して描画する。選択されていない場合は、選択ボタンを表示する。
            if (t.IsAbstract || t.IsInterface)
            {
                return DrawAbstractOrInterface(t, name, currentValue, pathKey);
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
            return DrawObject(t, name, currentValue, pathKey);
        }
        static IList DrawList(Type elementType, string name, IList list, DrawPathKey pathKey)
        {
            // nullの場合は新しいリストを作成
            list ??= (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            // Foldoutの状態をリスト自体で管理することで、同じリストを複数のインスペクターで描画している場合でも、展開状態を共有できる。
            bool fold = GetFoldout(pathKey);

            // Foldoutを描画して状態を更新
            fold = EditorGUILayout.Foldout(fold, $"{name} [{list.Count}]", true);
            SetFoldout(pathKey, fold);

            if (!fold)
                return list;

            //展開されている場合は要素を描画
            EditorGUI.indentLevel++;

            int size = EditorGUILayout.IntField(Size, list.Count);

            while (list.Count < size)
                list.Add(GetDefaultOrNull(elementType));

            while (list.Count > size)
                list.RemoveAt(list.Count - 1);

            for (int i = 0; i < list.Count; i++)
            {
                //要素を描画して更新
                list[i] = DrawField(
                    elementType,
                    $"{name} Element[{i}]",
                    list[i],
                    pathKey.Child(ListElementToken, i));
            }
            if (GUILayout.Button(Add))
            {
                list.Add(GetDefaultOrNull(elementType));
            }

            EditorGUI.indentLevel--;

            return list;
        }
        static Array DrawArray(Type elementType, string name, Array array, DrawPathKey pathKey)
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
            bool fold = GetFoldout(pathKey);
            fold = EditorGUILayout.Foldout(fold, $"{name} [{array.Length}]", true);
            SetFoldout(pathKey, fold);


            if (!fold)
                return array;

            //展開されている場合は要素を描画
            EditorGUI.indentLevel++;


            int newSize = EditorGUILayout.IntField(Size, array.Length);

            if (array == null || newSize != array.Length)
            {
                array = ArrayResize(array, newSize, elementType);
            }

            for (int i = 0; i < array.Length; i++)
            {
                //要素を描画して更新
                array.SetValue(
                    DrawField(
                        elementType,
                        $"{name} Element[{i}]",
                        array.GetValue(i),
                        pathKey.Child(ArrayElementToken, i)),
                    i);
            }
            if (GUILayout.Button(Add))
            {
                array = ArrayResize(array, array.Length + 1, elementType);
            }

            EditorGUI.indentLevel--;

            return array;
        }

        static IDictionary DrawDictionary(Type dictType, string name, object dictObj, DrawPathKey pathKey)
        {
            var args = dictType.GetGenericArguments();

            Type keyType = args[0];
            Type valueType = args[1];

            IDictionary dict = dictObj as IDictionary;

            dict ??= (IDictionary)Activator.CreateInstance(dictType);

            bool fold = GetFoldout(pathKey);

            fold = EditorGUILayout.Foldout(fold, $"{name} [{dict.Count}]", true);
            SetFoldout(pathKey, fold);

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

                if (GUILayout.Button("-", GUIContentCache.GetWidth(20)))
                {
                    dict.Remove(key);
                    break;
                }

                object newKey = DrawField(
                    keyType,
                    $"{name} Key [{i}]",
                    key,
                    pathKey.Child(DictionaryKeyToken, i));
                object newValue = DrawField(
                    valueType,
                    $"{name} Value [{i}]",
                    dict[key],
                    pathKey.Child(DictionaryValueToken, i));

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

            if (GUILayout.Button(Add))
            {
                var key = GetDefaultOrNull(keyType);
                if (key == null)
                {
                    EditorUtility.DisplayDialog("No Concrete Class Found", $"Cannot add entry with null pathkey for type {keyType.Name}.", "OK");
                    return dict;
                }
                dict[key] = GetDefaultOrNull(valueType);
            }

            EditorGUI.indentLevel--;

            return dict;
        }

        static object DrawObject(Type type, string name, object value, DrawPathKey pathKey)
        {
            CreateFailure result = CanCreate(type);

            if(result.Reason != CreateFailureReason.None)
            {
                EditorGUILayout.HelpBox(result.Message, MessageType.Error);
                return value;
            }

            value ??= GetDefaultOrNull(type);

            //nullの場合はラベルを貼って
            if (value == null)
            {
                EditorGUILayout.HelpBox($"{type.Name} は自動生成できません UnKnown Error", MessageType.Error);
                return value;
            }

            bool fold = GetFoldout(pathKey);

            fold = EditorGUILayout.Foldout(fold, name, true);

            SetFoldout(pathKey, fold);

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

                var newValue = DrawField(
                    f.FieldType,
                    f.Name,
                    fieldValue,
                    pathKey.Child(f.MetadataToken));
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
        static CreateFailure CanCreate(Type type)
        {
            if (createFailureReasonCache.TryGetValue(type, out var cache))
            {
                return cache;
            }
            CreateFailure result;
            //イミュータブル系は、デフォルト値を持つのでOK
            if (type.IsValueType)
            {
                result = new CreateFailure(CreateFailureReason.None, type);
                createFailureReasonCache[type] = result;
                return result;
            }

            var publicCtor = type.GetConstructor(Type.EmptyTypes);

            //デフォルトコンストラクタ&publicでOK
            if (publicCtor != null)
            {
                result = new CreateFailure(CreateFailureReason.None, type);
                createFailureReasonCache[type] = result;
                return result;
            }

            var anyCtor =
                type.GetConstructor(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null);

            if (anyCtor != null)
            {
                result = new CreateFailure(CreateFailureReason.PrivateDefaultConstructorOnly, type);
                createFailureReasonCache[type] = result;
                return result;
            }

            result = new CreateFailure(CreateFailureReason.NoDefaultConstructor, type);
            createFailureReasonCache[type] = result;
            return result;
        }
        /// <summary>
        /// 抽象クラスやインターフェースは直接描画できないので、実装/継承する具体的なクラスを選択して描画する。選択されていない場合は、選択ボタンを表示する。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        static object DrawAbstractOrInterface(Type type, string name, object value, DrawPathKey pathKey)
        {
            if (abstractToClass.TryGetValue(pathKey, out var concreteType))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{type.Name} ▶ {concreteType.Name}", EditorStyles.boldLabel);
                    if (GUILayout.Button(Delete, GUIContentCache.GetWidth(100)))
                    {
                        abstractToClass.Remove(pathKey);
                        return null;
                    }
                }
                return DrawField(
                    concreteType,
                    name,
                    value,
                    pathKey.Child(CombineToken(ConcreteTypeToken, concreteType.MetadataToken)));
            }
            if (GUILayout.Button($"Select Class ({type.Name})"))
            {
                ShowTypeMenu(type, pathKey);
            }

            return value;
        }
        /// <summary>
        /// 具象クラスの選択メニューを表示する。選択されたクラスは、抽象クラスやインターフェースのキャッシュに保存される。次回以降は直接描画されるようになる。
        /// </summary>
        /// <param name="baseType"></param>
        private static void ShowTypeMenu(Type baseType, DrawPathKey pathKey)
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
            string path = GetPathString(pathKey);
            foreach (var type in types)
            {
                menu.AddItem(new GUIContent(type.FullName, path), false, () =>
                {
                    abstractToClass[pathKey] = type;
                });
            }
            menu.ShowAsContext();
        }
        static void DrawScriptableObjectInline(ScriptableObject so, DrawPathKey pathKey)
        {
            if (so == null)
                return;

            bool fold = GetFoldout(pathKey);

            fold = EditorGUILayout.Foldout(fold, so.name, true);

            SetFoldout(pathKey, fold);

            if (!fold)
                return;
            Editor editor = NestesObjectDrawLogic.GetOrCreateEditorCache(so);

            EditorGUILayout.BeginVertical("box");

            editor.OnInspectorGUI();

            EditorGUILayout.EndVertical();
        }
        private static object GetDefaultOrNull(Type t)
        {
            if (t == null)
                return null;
            //プリミティブ型は作れる
            if (t.IsValueType)
                return Activator.CreateInstance(t);
            //UnityEngine.Objectは、作っちゃダメ
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
                return null;
            //抽象クラスは作れない
            if (t.IsAbstract || t.IsInterface)
                return null;

            //生成できない型(プライベートコンストラクタしかない、デフォルトコンストラクタがない,etc...)の場合はnullを返す
            try
            {
                return Activator.CreateInstance(t);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
        private static bool GetFoldout(in DrawPathKey pathKey)
        {
            if (!foldouts.TryGetValue(pathKey, out bool value))
            {
                value = false;
                foldouts[pathKey] = value;
            }

            return value;
        }

        private static void SetFoldout(in DrawPathKey pathKey, bool value)
        {
            foldouts[pathKey] = value;
        }

    }
}

namespace Syacapachi.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    public static class EditorReflectionCache
    {
        /// <summary>
        /// プロパティの情報をまとめた構造体
        /// </summary>
        private readonly struct PathInfo
        {
            /// <summary>
            /// SerializeReference属性が付いているフィールドを含むプロパティパスかどうかのフラグ
            /// </summary>
            public readonly bool HasSerializeReference => ReflectionStartIndex >= 0;
            /// <summary>
            /// プロパティパスの要素（例: "myList[0].myField" -> ["myList[0]", "myField"]）
            /// </summary>
            public readonly PathElement[] FullElements;//フィールドへの完全パス
            /// <summary>
            /// SerializeReference属性が付いているフィールドの。自分で探し始める最初のインデックス。
            /// 途中までゲッターがある。
            /// </summary>
            public readonly int ReflectionStartIndex;
            public PathInfo(PathElement[] fullElements, int reflectionPath = -1)
            {
                this.FullElements = fullElements;
                this.ReflectionStartIndex = reflectionPath;
            }
        }
        /// <summary>
        /// プロパティパスの要素を表す構造体（例: "myList[0]" -> Name: "myList", Index: 0）
        /// </summary>
        private readonly struct PathElement
        {
            public readonly string Name;
            public readonly int Index;
            public PathElement(string name, int index = -1)
            {
                Name = name;
                Index = index;
            }
            /// <summary>
            /// フィールド名を抽出（例: "myList[0]" から "myList"と "0" を抽出）
            /// </summary>
            /// <param name="elements"></param>
            /// <returns></returns>
            public PathElement WithIndex(int index) => new(Name, index);

            public static PathElement[] Parse(string path)
            {
                //ReadOnlySpanでGCを消す
                ReadOnlySpan<char> span = path.AsSpan();

                // Array は除外されるので実際の要素数を数える
                int count = 0;
                int start = 0;

                while (start < span.Length)
                {
                    // '.'で区切る
                    int end = span[start..].IndexOf('.');

                    ReadOnlySpan<char> part;

                    if (end < 0)
                    {
                        part = span[start..];
                        start = span.Length;
                    }
                    else
                    {
                        part = span.Slice(start, end);
                        start += end + 1;
                    }

                    if (!part.SequenceEqual("Array"))
                        count++;
                }

                PathElement[] result = new PathElement[count];

                int resultIndex = 0;
                start = 0;

                while (start < span.Length)
                {
                    // '.'で区切る
                    int end = span[start..].IndexOf('.');

                    ReadOnlySpan<char> part;

                    //最後の要素
                    if (end < 0)
                    {
                        part = span[start..];
                        start = span.Length;
                    }
                    else
                    {
                        part = span.Slice(start, end);
                        start += end + 1;
                    }

                    //--------------------------------------------------
                    // Array は捨てる
                    //--------------------------------------------------

                    if (part.SequenceEqual("Array"))
                        continue;

                    //--------------------------------------------------
                    // data[123]
                    //--------------------------------------------------

                    if (part.StartsWith("data["))
                    {
                        int index = ParseIndex(part);

                        result[resultIndex - 1] =
                            result[resultIndex - 1].WithIndex(index);

                        continue;
                    }

                    //--------------------------------------------------
                    // 通常フィールド
                    //--------------------------------------------------

                    result[resultIndex++] =
                        new PathElement(part.ToString());
                }

                return result;
            }

            private static int ParseIndex(ReadOnlySpan<char> span)
            {
                // data[123]

                int value = 0;

                for (int i = 5; i < span.Length - 1; i++)
                {
                    value = value * 10 + (span[i] - '0');
                }

                return value;
            }
        }
        // 拡張するなら、GetterCache<TArg,TResult>
        // 返り値ごとに必要なので、static + ジェネリック
        // 条件フィールドの値を高速に取得するためのキャッシュ(Typeとフィールド名の組み合わせで一意に特定でき、instanceを渡して実行するのでstatic)
        static class GetterCache<T>
        {
            internal static readonly Dictionary<
                (Type, string),
                Func<object, T>
            > Cache = new();
        }
        // 条件フィールドの FieldInfo をキャッシュするための辞書(FieldInfo は型から一意に求まるのでstatic)
        private static readonly Dictionary<(Type, string), FieldInfo> fieldCache = new();
        // Pathのフィールドを含むオブジェクトのゲッターのキャッシュ(Typeとフィールド名の組み合わせで一意に特定でき、instanceを渡して実行するのでstatic)
        private static readonly Dictionary<(Type, string), Func<object, object>> propertyPathGetterCache = new();
        // propertyPath をたどるためのアクセサーのキャッシュ（TypeとpropertyPathの組み合わせで一意に特定でき、instanceを渡して実行するのでstatic）
        private static readonly Dictionary<(Type, string), PathInfo> pathCache = new();

        static EditorReflectionCache()
        {

        }
        /// <summary>
        /// 手動キャッシュクリア関数 一応使わなくてもスクリプトの再コンパイルやエディタの再起動でキャッシュはリセットされるが、必要に応じてエディタ拡張から呼び出してキャッシュをクリアできるようにしておく
        /// </summary>
        public static void ClearCache()
        {
            fieldCache.Clear();
            propertyPathGetterCache.Clear();
            pathCache.Clear();
        }

        /// <summary>
        /// ネストされたインラインクラス用
        /// Monobehaviour->InlineClassA->InlineClassB->targetField みたいな場合に、
        /// SerializedProperty.propertyPath をたどって「そのフィールドを含むオブジェクト(InlineClassB)」を返す
        /// </summary>
        /// <param name="property">評価対象のプロパティ</param>
        /// <returns>フィールドを含むオブジェクト、見つからなければ null</returns>
        public static object GetParentTarget(SerializedProperty property)
        {
            //最上位のオブジェクト
            var rootObject = property.serializedObject.targetObject;
            Type rootType = rootObject.GetType();
            //propertyPathは呼ぶたびに生成されるので、一回で止める。
            //キャッシュ化したいが、SerializedPropertyは毎フレーム再生成される->HashCodeが異なるので辞書がムズイ。
            string propertyPath = property.propertyPath;
            Func<object, object> getter = CreateOrGetPropertyPathGetter(rootObject, propertyPath);
            if (getter == null)
            {
                Debug.LogError($"[{nameof(EditorReflectionCache)}] Failed to create getter for property path '{propertyPath}' on targetType '{rootType}'. Please check the property path and ensure it is valid for the target targetType.", rootObject as UnityEngine.Object);
                return null;
            }
            //一応キャッシュにあるはずだが、念のため確認しておく。
            if (!pathCache.TryGetValue((rootType, propertyPath), out var cachedPathInfo))
            {
                Debug.LogError($"[{nameof(EditorReflectionCache)}] Failed to find cached path info for property path '{propertyPath}' on targetType '{rootType}'. This should not happen if the getter was created successfully. Please check the caching logic.", rootObject as UnityEngine.Object);
                return null;
            }
            //SerializeReferenceは実装の型を動的に決定するため、直前までキャッシュを作りそれ以降は自力で辿る。
            if (cachedPathInfo.HasSerializeReference)
            {
                try
                {
                    object parent = getter(rootObject);
                    if (parent == null)
                    {
                        Debug.LogError($"[{nameof(EditorReflectionCache)}] Failed to access parent object for property path '{propertyPath}' on targetType '{rootType}'. The parent object is null, which may indicate an issue with the property path or the target object's state.", rootObject as UnityEngine.Object);
                        return null;
                    }
                    return GetValueByPath(parent, cachedPathInfo.FullElements, cachedPathInfo.ReflectionStartIndex);
                }
                catch (Exception e)
                {
                    Debug.LogException(
                        new Exception(
                            $"[{nameof(EditorReflectionCache)}] Failed to access parent object for property path '{propertyPath}' on targetType '{rootType}'. Please check the property path and ensure it is valid for the target targetType.",
                            e),
                        rootObject as UnityEngine.Object);
                    return null;
                }
            }
            try
            {
                return getter(rootObject);
            }
            catch (Exception e)
            {
                Debug.LogException(
                    new Exception(
                        $"[{nameof(EditorReflectionCache)}] Failed to access property path '{propertyPath}' on targetType '{rootType}'."
                        , e),
                    rootObject as UnityEngine.Object);
                return null;
            }
        }

        /// <summary>
        /// このプロパティが属する「そのオブジェクト実体」（フィールドをもつインスタンス）を返す補助
        /// </summary>
        /// <param name="property">評価対象のプロパティ</param>
        /// <param name="root">ルートオブジェクト</param>
        /// <returns>フィールドを含むオブジェクト、見つからなければ null</returns>
        public static object GetObjectContainingField(SerializedProperty property, object root)
        {
            // GetParentTarget で得たものが root のはずなのでそのまま返す（冗長だが将来の拡張用）
            return root ?? GetParentTarget(property);
        }

        /// <summary>
        /// フィールド値取得用Getterを取得
        /// キャッシュ済みならそれを返す
        /// <param name="targetType"/> 参照するフィールドをもつType
        /// <param name="fieldName"> 参照するフィールド名
        /// <typeparamref name="TReturn"/> 返り値の型
        /// </summary>
        public static Func<object, TReturn> GetOrCreateGetter<TReturn>(Type targetType, string fieldName)
        {
            if (GetterCache<TReturn>.Cache.TryGetValue(
                (targetType, fieldName),
                out var getter))
            {
                return getter;
            }
            //Debug.Log($"[{nameof(FieldUtility)}] Add new getterCache Type {type},Name {fieldName}");
            FieldInfo field = GetFieldRecursive(targetType, fieldName);

            if (field == null)
            {
                return null;
            }
            //フィールドが宣言されたクラスのゲッターでないといけない
            getter = CreateGetter<TReturn>(field.DeclaringType, field);
            GetterCache<TReturn>.Cache[(targetType, fieldName)] = getter;
            return getter;
        }
        /// <summary>
        /// フィールド用のキャッシュ済みゲッターを取得する。 
        /// </summary>
        /// <typeparam name="TReturn">返り値の型</typeparam>
        /// <param name="fieldInfo"> 参照するFieldInfo </param>
        /// <returns></returns>
        public static Func<object, TReturn> GetOrCreateGetter<TReturn>(FieldInfo fieldInfo)
        {
            if (GetterCache<TReturn>.Cache.TryGetValue((fieldInfo.DeclaringType, fieldInfo.Name), out var getter))
            {
                return getter;
            }

            //フィールドが宣言されたクラスのゲッターでないといけない
            getter = CreateGetter<TReturn>(fieldInfo.DeclaringType, fieldInfo);
            GetterCache<TReturn>.Cache[(fieldInfo.DeclaringType, fieldInfo.Name)] = getter;
            return getter;
        }
        /// <summary>
        /// 自力でリフレクションをたどってプロパティパスを処理する関数
        /// （SerializeReference属性が付いているフィールドを含むプロパティパスの処理用）
        /// </summary>
        /// <param name="root">開始する点</param>
        /// <param name="fullPathes">自力でたどる点</param>
        /// <returns></returns>
        private static object GetValueByPath(object root, in PathElement[] fullPathes, int startIndex)
        {
            if (root == null) return null;
            if (startIndex < 0) return null;
            object current = root;
            Type currentType = root.GetType();
            //開始点から1つ手前まで行く
            for (int i = startIndex; i < fullPathes.Length - 1; i++)
            {
                int index = fullPathes[i].Index;
                string fieldName = fullPathes[i].Name;
                FieldInfo field = GetFieldRecursive(currentType, fieldName);
                if (field == null)
                {
                    Debug.LogError($"[{nameof(EditorReflectionCache)}] Failed to find field '{fieldName}' on targetType '{currentType}' while processing property path. Please check the field name and ensure it exists in the target targetType.");
                    return null;
                }
                current = field.GetValue(current);
                if (current == null)
                    return null;
                //実体がある方をとる。
                currentType = current.GetType();
                if (index >= 0)
                {
                    if (currentType.IsArray)
                    {
                        Array array = current as Array;
                        if (index < 0 || index >= array.Length)
                        {
                            Debug.LogError($"[{nameof(EditorReflectionCache)}] Index {index} out of range for '{fieldName}' on targetType '{currentType}' while processing property path. Please check the collection size and ensure the index is valid.");
                            return null;
                        }
                        current = array.GetValue(index);
                        currentType = current?.GetType() ?? typeof(object);
                    }
                    else if (current is IList list)
                    {
                        if (index < 0 || index >= list.Count)
                        {
                            Debug.LogError($"[{nameof(EditorReflectionCache)}] Index {index} out of range for '{fieldName}' on targetType '{currentType}' while processing property path. Please check the collection size and ensure the index is valid.");
                            return null;
                        }
                        current = list[index];
                        currentType = current?.GetType() ?? typeof(object);
                    }
                    else if (current is System.Collections.IEnumerable enumerable)
                    {
                        var enumerator = enumerable.GetEnumerator();
                        for (int j = 0; j <= index; j++)
                        {
                            if (!enumerator.MoveNext())
                            {
                                Debug.LogError($"[{nameof(EditorReflectionCache)}] Index {index} out of range for '{fieldName}' on targetType '{currentType}' while processing property path. Please check the collection size and ensure the index is valid.");
                                return null;
                            }
                        }
                        current = enumerator.Current;
                        currentType = current?.GetType() ?? typeof(object);
                    }
                    else
                    {
                        Debug.LogError($"[{nameof(EditorReflectionCache)}] Field '{fieldName}' on targetType '{currentType}' is not a collection while processing property path. Please check the field targetType and ensure it supports indexing.");
                        return null;
                    }
                }
            }
            return current;
        }
        /// <summary>
        /// Monobehaviour.InlineClassA.InlineClassB.targetField のような ネストされたプロパティパスをたどる
        /// return InlineClassB のゲッターを作成する関数
        /// </summary>
        /// <param name="rootObject"> 検索を始める基底(UnityEngine.Object) </param>
        /// <param name="propertyPath"> 対象のプロパティpath </param>
        /// <returns> propertyPathの直接の親クラスのゲッター </returns>
        private static Func<object, object> CreateOrGetPropertyPathGetter(UnityEngine.Object rootObject, string propertyPath)
        {
            //最上位のオブジェクト
            Type rootType = rootObject.GetType();
            if (propertyPathGetterCache.TryGetValue((rootType, propertyPath), out var getter))
            {
                return getter;
            }
            PathElement[] cachedPath;
            if (pathCache.TryGetValue((rootType, propertyPath), out var cachedInfo))
            {
                cachedPath = cachedInfo.FullElements;
            }
            else
            {
                // propertyPath を簡単な配列に変換する（例: "myList.Array.data[0].myField" -> ["myList[0]", "myField"]）
                cachedPath = PathElement.Parse(propertyPath);
                //Debug.Log($"[{nameof(EditorReflectionCache)}] Creating new getter for property path '{propertyPath}' on type '{rootType}'. This may take some time, but it will be cached for future use.");
            }

            //インデックスアクセス用に今の型を記憶
            Type currentType = rootType;
            //SerializeReference 属性が付いているフィールドを含むプロパティパスの場合、そこまでキャッシュ式生成してそれ以降は自力で辿るためのインデックス
            int breakIndex = -1;
            // fun(object instance) と同じ
            //名前はなくてもよいが、デバッグ時にわかりやすいように "instance" としています。
            ParameterExpression instanceParam =
                Expression.Parameter(typeof(object), "instance");
            //フィールドアクセスを積み重ねていくための Expression を作成
            Expression currentExpression = Expression.Convert(instanceParam, currentType);

            //パスの1つ手前まで行く
            for (int i = 0; i < cachedPath.Length - 1; i++)
            {
                string element = cachedPath[i].Name;
                //Array用
                int index = cachedPath[i].Index;

                string fieldName = element;
                // current.field
                FieldInfo field = GetFieldRecursive(currentType, fieldName);
                if (field == null)
                {
                    Debug.LogError($"[{nameof(EditorReflectionCache)}] Failed to find field '{fieldName}' on targetType '{currentType}' while processing property path '{propertyPath}'. Please check the field name and ensure it exists in the target targetType.");
                    return null;
                }
                //SerializeReference 属性が付いているフィールドは、途中まで作る。(残りは自分で探索してもらう。)
                if (field.IsDefined(typeof(SerializeReference), true))
                {
                    breakIndex = i;
                    // SerializeReference フィールドの直前まで生成
                    break;
                }
                //Monobehaviour.InlineClassA.InlineClassB.targetField のように連続している場合、currentExpression は (TargetType)instance からスタートして、順番にフィールドアクセスを積み重ねていくことになる
                //(TargetType)instance.field
                currentExpression = Expression.Field(currentExpression, field);
                currentType = field.FieldType;

                if (index >= 0)
                {
                    // 配列やリストの要素アクセスを処理
                    Expression indexed = BuildIndexer(currentExpression, currentType, index);

                    if (indexed == null)
                    {
                        Debug.LogError($"[{nameof(EditorReflectionCache)}] Failed to build indexer for '{fieldName}' on targetType '{currentType}' while processing property path '{propertyPath}'. Please check the field targetType and ensure it supports indexing.");
                        return null;
                    }

                    currentExpression = indexed;
                    currentType = GetIndexedElementType(currentType);
                }
            }
            // boxing
            // fieldAccess(返り値) を object にキャストする Expression を作成
            UnaryExpression castResult = Expression.Convert(currentExpression, typeof(object));
            Func<object, object> compiledGetter = Expression
                .Lambda<Func<object, object>>(
                    castResult,
                    instanceParam)
                .Compile();

            // キャッシュに保存（SerializeReference 属性が付いているフィールドを含むかどうかの情報も一緒に保存）
            if (breakIndex >= 0)
            {
                pathCache[(rootType, propertyPath)] = new PathInfo(cachedPath, breakIndex);
            }
            else
            {
                pathCache[(rootType, propertyPath)] = new PathInfo(cachedPath);
            }
            //Debug.Log($"[{nameof(EditorReflectionCache)}] Created new getter for property path '{property.propertyPath}' on type '{rootType}'. This getter will be cached for future use.");
            propertyPathGetterCache[(rootType, propertyPath)] = compiledGetter;

            return compiledGetter;
        }
        /// <summary>
        /// コレクションの中身を取得するExperssionを作成する。
        /// </summary>
        /// <param name="currentExpression"> 今のExperssion </param>
        /// <param name="currentType">コレクション自体のType</param>
        /// <param name="index">アクセスインデックス</param>
        /// <returns></returns>
        private static Expression BuildIndexer(Expression currentExpression, Type currentType, int index)
        {
            if (currentType.IsArray)
            {
                return Expression.ArrayIndex(currentExpression, Expression.Constant(index)); // Array はインデックスアクセスをサポート
            }
            //これがあるやつがListとかIListとかIEnumerableとかのはず
            var itemProperty = currentType.GetProperty("Item");
            if (itemProperty != null)
            {
                return Expression.MakeIndex(currentExpression, itemProperty, new Expression[] { Expression.Constant(index) }); // IList もサポート
            }
            // IEnumerable もサポート（ただしインデックスアクセスは遅いので注意）
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(currentType))
            {
                Type collectionType = GetIndexedElementType(currentType);
                //Enumratableの関数を呼び出す。
                return Expression.Call(
                    typeof(Enumerable),//Enumerable.
                    nameof(Enumerable.ElementAt),//ElementAt 関数
                    new Type[] { collectionType },//ジェネリック引数<collectionType>
                    currentExpression,//引数の列挙可能なオブジェクト
                    Expression.Constant(index));
            }
            return null; // インデックスアクセス非対応の型
        }
        /// <summary>
        /// コレクションの要素の型を取得する補助
        /// </summary>
        /// <param name="type">コレクションの型</param>
        /// <returns>要素の型</returns>
        private static Type GetIndexedElementType(Type type)
        {
            // 配列の場合は要素の型を直接取得
            if (type.IsArray)
                return type.GetElementType();

            // IList や List<T> の場合は Item プロパティの型を取得
            foreach (var i in type.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return i.GetGenericArguments()[0];
                }
            }

            // その他の型の場合は、ジェネリックを実装しているか確認して要素の型を取得
            var args = type.GetGenericArguments();

            return args.Length > 0
                ? args[0]
                : typeof(object);
        }
        /// <summary>
        /// 再帰的に型情報から FieldInfo を探す（継承チェーン対応）
        /// </summary>
        /// <param name="findType">探索を開始する型</param>
        /// <param name="fieldName">探すフィールドの名前</param>
        /// <returns>見つかった FieldInfo、見つからなければ null</returns>
        private static FieldInfo GetFieldRecursive(Type findType, string fieldName)
        {
            // キャッシュにあればそれを返す（ただし継承などの同名フィールドの衝突には非対応）
            if (fieldCache.TryGetValue((findType, fieldName), out var cachedField))
            {
                return cachedField;
            }
            //Debug.Log($"[{nameof(FieldUtility)}] Add new fieldCache Type {findType},Name {fieldName}");
            Type current = findType;
            while (current != null)
            {
                var f = current.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null)
                {
                    fieldCache[(findType, fieldName)] = f;
                    return f;
                }
                current = current.BaseType;
            }
            return null;
        }

        /// <summary>
        /// コードをデータとして組み立てて、あとから高速な関数としてコンパイルする関数
        /// object instance => (object)((TargetType)instance).field;を動的生成
        /// 通常のリフレクションに比べて大幅に高速なフィールドアクセスが可能になる
        /// 理由は、リフレクションは一般的に遅い(内部でチェックが行われる)が、
        /// Expression Tree を使用して動的にコードを生成し、それをコンパイルすることで、通常のリフレクションよりも高速なアクセスが可能になります。
        /// ExpressionTreeで高速Getter生成
        /// </summary>
        private static Func<object, object> CreateGetter(
            Type targetType,
            FieldInfo field)
        {
            // object instance
            // 引数情報を表す Expression.Parameter を作成
            // fun(object instance) と同じ
            //名前はなくてもよいが、デバッグ時にわかりやすいように "instance" としています。
            ParameterExpression instanceParam =
                Expression.Parameter(typeof(object), "instance");

            // (TargetType)instance
            // instance を targetType にキャストする Expression を作成
            // 一項演算子関数の作成クラス
            UnaryExpression castInstance =
                Expression.Convert(instanceParam, targetType);

            // instance.field
            // castInstance から field にアクセスする Expression を作成
            // フィールドアクセスを表す Expression クラス
            // castInstance.field と同等の処理を表す Expression を作成
            MemberExpression fieldAccess =
                Expression.Field(castInstance, field);

            // boxing
            // fieldAccess(返り値) を object にキャストする Expression を作成
            UnaryExpression castResult =
                Expression.Convert(fieldAccess, typeof(object));

            // object instance => (object)((TargetType)instance).field
            // 上記の Expression をラムダ式としてまとめ、Func<object, object> 型のデリゲートにコンパイルする
            return Expression
                .Lambda<Func<object, object>>(
                    castResult,
                    instanceParam)
                .Compile();
        }
        private static Func<object, TReturn> CreateGetter<TReturn>(Type targetType, FieldInfo field)
        {
            // object instance
            // 引数情報を表す Expression.Parameter を作成
            // fun(object instance) と同じ
            //名前はなくてもよいが、デバッグ時にわかりやすいように "instance" としています。
            ParameterExpression instanceParam =
                Expression.Parameter(typeof(object), "instance");

            // (TargetType)instance
            // instance を targetType にキャストする Expression を作成
            // 一項演算子関数の作成クラス
            UnaryExpression castInstance =
                Expression.Convert(instanceParam, targetType);

            // instance.field
            // castInstance から field にアクセスする Expression を作成
            // フィールドアクセスを表す Expression クラス
            // castInstance.field と同等の処理を表す Expression を作成
            MemberExpression fieldAccess =
                Expression.Field(castInstance, field);

            // boxing
            // fieldAccess(返り値) を T にキャストする Expression を作成
            // キャストできない場合は、例外を吐く。
            UnaryExpression castResult =
                Expression.Convert(fieldAccess, typeof(TReturn));
            // object instance => (T)((TargetType)instance).field
            // 上記の Expression をラムダ式としてまとめ、Func<object, T> 型のデリゲートにコンパイルする
            return Expression
                .Lambda<Func<object, TReturn>>(
                    castResult,
                    instanceParam)
                .Compile();
        }
    }
}
# Editor拡張 unsafe / 低レベル制御 最適化候補

## 結論

`Assets/Editor` 配下では、`unsafe` を広く使うよりも、次の低レベル寄りの制御を限定的に入れる方が効果と安全性のバランスがよいです。

- `Enum` の `Convert.ToInt64` / `Enum.Parse` / `Enum.GetValues` 経路を型別キャッシュまたは `Unsafe.As` 系に置き換える
- `SerializedProperty.propertyPath` の `Replace` / `Split` / `Substring` パーサーを `ReadOnlySpan<char>` ベースにする
- Dictionary描画時の一時 `List<object>` 生成を再利用バッファ、またはプールへ逃がす
- `Type` 判定、enum情報、factory、getterを型ごとにキャッシュし、描画中のReflection/boxingを避ける
- 構造体キーの `Equals` / `GetHashCode` を値型のまま完結させる

一方、`EditorGUI` / `EditorGUILayout` / `SerializedObject` / `AssetDatabase` / `MethodInfo.Invoke` / `Editor.CreateCachedEditor` はUnity側のコストが支配的です。ここに `unsafe` を入れても速度向上は限定的です。

## 前提と注意点

- `unsafe` を使う場合、該当asmdefまたはPlayer Settingsで unsafe code を許可する必要があります。
- Editor拡張は可読性とデバッグ容易性が重要です。unsafe化は、Profilerで該当関数がホットパスだと確認できた箇所に限定してください。
- Unity Editor上では、低レベル化よりもGC.Alloc削減の方が効果が出やすいです。
- `System.Runtime.CompilerServices.Unsafe` は既に一部ファイルで使用されています。重複実装を増やすより、共通ユーティリティ化した方が保守しやすいです。

## 優先度高

| 対象 | 現状 | 改善案 | unsafe/低レベル手法 | 推奨度 |
| --- | --- | --- | --- | --- |
| `Editor/PropertyDrawer/EnableIf/EnableIfEvaluator.cs` の enum 評価 | `Enum e => Convert.ToInt64(e)` を使用 | `Enum` 型ごとの変換delegateをキャッシュし、`Convert.ToInt64` 経路を避ける | `Unsafe.As<T, byte/ushort/uint/ulong>`、または型別delegate | 高 |
| `Editor/PropertyDrawer/SingleFlagOnlyDrawer.cs` の enum 描画 | `Enum.Parse`、`Enum.GetName`、`Array.IndexOf`、`Convert.ToInt32` が描画時に走る | enum型ごとに `name -> int`、`int -> index`、先頭値、Flags有無をキャッシュ | unsafeより型別キャッシュ優先。値変換だけ `Unsafe.As` 候補 | 高 |
| `Editor/EditorCache/EditorReflectionCache.cs` の `PathElement.Parse` | `Replace`、`Split`、`Substring`、`int.Parse` を使用 | `ReadOnlySpan<char>` で1文字ずつ走査し、配列要素を直接作る | unsafe不要。Spanベースparser | 高 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` の `DrawDictionary` | 毎描画 `List<object> keys = new();` | staticな一時Listを使う、または `ListPool<object>` 相当を導入して返却する | unsafe不要。バッファ再利用 | 高 |
| `Editor/PropertyDrawer/ShowInspectorButtonDrawer.cs` の `DrawDictionary` | `new(dict.Keys.Count + 10)` を毎回作る | 同上。旧Drawerなら使用頻度次第で廃止/統合も検討 | unsafe不要。バッファ再利用 | 高 |

## `EnableIfEvaluator` の enum 変換

### 問題

`EvaluateConditionsRecursive` と `EvaluateEnumConditionRecursive` では、enum値判定に `Convert.ToInt64(e)` を使っています。

```csharp
Enum e => Convert.ToInt64(e) != 0
```

`fieldValue` が `object` で返ってくるため、getterの時点で値型enumはboxingされています。`Convert.ToInt64` はさらに型判定と変換処理を通るため、描画中に多数の `EnableIf` がある場合はコストになります。

### 改善案

型ごとに enum 変換delegateをキャッシュします。

```csharp
private static readonly Dictionary<Type, Func<object, ulong>> enumValueReaders = new();

private static ulong ReadEnumAsUInt64(object value)
{
    Type type = value.GetType();
    if (!enumValueReaders.TryGetValue(type, out var reader))
    {
        reader = CreateEnumReader(type);
        enumValueReaders[type] = reader;
    }

    return reader(value);
}
```

実装は2段階で考えるのが安全です。

1. まずは `Enum.GetUnderlyingType(type)` ごとの明示cast delegateにする。
2. Profiler上まだ重い場合だけ、generic helper + `Unsafe.As<TUnderlying>` へ進める。

### unsafe採用判断

- **採用候補**: enum変換delegate内部。
- **非推奨**: `EvaluateConditionsRecursive` 全体をunsafe化すること。SerializedProperty、Reflection getter、ログ出力が混在しており、unsafe化しても支配的コストは下がりません。

## `SingleFlagOnlyDrawer` の enum キャッシュ

### 問題

`DrawEnumWithArrayOrList` では変更時のみとはいえ、次の処理が走ります。

- `Enum.Parse(enumType, property.enumNames[newIndex])`
- `Convert.ToInt32(...)`
- `Enum.GetName(enumType, intValue)`
- `Array.IndexOf(property.enumNames, fixedName)`
- `GetFirstEnumValue` 内の `Enum.GetValues(enumType)`

現在 `IsSingleFlag`、`GetFirstBit`、`MaskToLayer` には `AggressiveInlining` が付いていますが、より効果が大きいのは enum メタデータのキャッシュです。

### 改善案

enum型ごとのキャッシュを追加します。

```csharp
private sealed class EnumCache
{
    public readonly Dictionary<string, int> NameToValue;
    public readonly Dictionary<int, int> ValueToIndex;
    public readonly int FirstNonZeroValue;
    public readonly bool IsFlags;
}
```

これにより、描画中の `Enum.Parse` / `Enum.GetName` / `Enum.GetValues` / `Array.IndexOf` を避けられます。

### unsafe採用判断

- **基本は不要**。キャッシュ化だけで十分効果が見込めます。
- `EnumToUInt64<T>` は既に `Unsafe.As` を使っていますが、現状の `SerializedProperty` ベース描画では generic `T` に到達しづらいため、利用箇所を増やす前に型別キャッシュを優先してください。
- `GetFirstEnumValue` に `AggressiveInlining` が付いていますが、内部で `Enum.GetValues` を呼ぶため、低レベル最適化対象としては不適切です。キャッシュに置き換える方が妥当です。

## `EditorReflectionCache.PathElement.Parse`

### 問題

現在のパーサーは初回キャッシュ時のみですが、以下の一時文字列と配列を作ります。

```csharp
elements = elements.Replace(".Array.data[", "[");
string[] pathes = elements.Split('.');
string fieldName = element.Substring(0, bracket);
int index = int.Parse(element.Substring(bracket).Replace("[", "").Replace("]", ""));
```

`EnableIf` 系でネストしたプロパティが多い場合、初回描画時のGC.Allocに出ます。

### 改善案

`ReadOnlySpan<char>` で1文字ずつ走査し、`.Array.data[` を読み飛ばす専用parserにします。

方針:

- `Split('.')` を使わず、区切り位置を走査する。
- `Substring` ではなく、必要なフィールド名だけ最後に `new string(span)` する。
- indexは `int.Parse` ではなく、数字を走査して手動で整数化する。
- `PathElement[]` の要素数は先にドット数を数えるか、一時 `List<PathElement>` を使う。GCを詰めるなら配列長を先に数える。

例:

```csharp
private static int ParsePositiveInt(ReadOnlySpan<char> text)
{
    int value = 0;
    for (int i = 0; i < text.Length; i++)
        value = value * 10 + (text[i] - '0');
    return value;
}
```

### unsafe採用判断

- **unsafe不要**。`ReadOnlySpan<char>` で十分です。
- `fixed char*` による手動parserは保守コストが高く、Editor拡張ではリスクに対する利得が小さいです。

## Dictionary描画のキー一時バッファ

### 対象

- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` の `DrawDictionary`
- `Editor/PropertyDrawer/ShowInspectorButtonDrawer.cs` の `DrawDictionary`

### 問題

Dictionaryのキー変更に対応するため、毎描画でキー一覧をコピーしています。

```csharp
List<object> keys = new();
foreach (var k in dict.Keys)
    keys.Add(k);
```

これは展開中のDictionaryがある限り、Repaint/LayoutごとにGC.Allocが出ます。

### 改善案

static再利用バッファを使います。

```csharp
private static readonly List<object> dictionaryKeyBuffer = new();

dictionaryKeyBuffer.Clear();
foreach (var key in dict.Keys)
    dictionaryKeyBuffer.Add(key);

try
{
    for (int i = 0; i < dictionaryKeyBuffer.Count; i++)
    {
        object key = dictionaryKeyBuffer[i];
        // draw
    }
}
finally
{
    dictionaryKeyBuffer.Clear();
}
```

ネストしたDictionary描画がある場合、単一staticバッファでは破壊されます。その場合は次のいずれかにします。

- `Stack<List<object>>` の簡易プールを作る
- depthごとの `List<object>` を持つ
- UnityEditor/UnityEngine側に利用可能なListPoolがあるならそれを使う

### unsafe採用判断

- **unsafe不要**。GC削減にはバッファ再利用で十分です。
- `CollectionsMarshal.AsSpan` は `Dictionary<TKey,TValue>` の内部配列には使えません。`IDictionary` 経由でも使えないため、この用途には不適切です。

## `OnInstectorButtonDrawLogic` の型判定とfactory

### 問題

`DrawField` は描画ごとに大量の型判定を行います。

- `Nullable.GetUnderlyingType(t)`
- `t.IsEnum`
- `t.GetCustomAttribute<FlagsAttribute>()`
- `typeof(UnityEngine.Object).IsAssignableFrom(t)`
- `t.IsGenericType`
- `t.GetGenericTypeDefinition()`
- `t.GetGenericArguments()`
- `Activator.CreateInstance`
- `MakeGenericType`

### 改善案

unsafeではなく、型ごとの描画メタ情報をキャッシュします。

```csharp
private enum DrawKind
{
    Int,
    Float,
    String,
    Enum,
    UnityObject,
    Array,
    List,
    Dictionary,
    Object,
    Unsupported,
}

private readonly struct TypeDrawInfo
{
    public readonly DrawKind Kind;
    public readonly Type ElementType;
    public readonly Type KeyType;
    public readonly Type ValueType;
    public readonly bool IsFlagsEnum;
}
```

また、null時生成は `Activator.CreateInstance` ではなくfactory delegateを型ごとにキャッシュします。

```csharp
private static readonly Dictionary<Type, Func<object>> factoryCache = new();
```

### unsafe採用判断

- **unsafe不要**。Reflection/Activator回避のキャッシュ化が主。
- `Expression.New(type).Compile()` か `ConstructorInfo.CreateDelegate` 相当が使える場合、そちらを優先します。
- unmanaged構造体のdefault生成だけを `RuntimeHelpers.GetUninitializedObject` などで攻めるのは非推奨です。コンストラクタやUnityのシリアライズ前提を壊す可能性があります。

## `EditorReflectionCache` の getter生成

### 現状

`Expression.Compile()` でgetterを生成してキャッシュしています。これはReflectionの直接呼び出しより高速になりやすく、方向性は妥当です。

### 低レベル化候補

- `PathInfo.ReflectionPath` で `cachedPath.Skip(breakIndex).ToArray()` があるため、`ArraySegment<PathElement>` または `(PathElement[] array, int start)` に変える。
- `BuildIndexer` 内の `new Expression[] { Expression.Constant(index) }` は初回のみなので優先度低。
- `GetFieldRecursive` の結果は既に `fieldCache` されているため、unsafe化不要。

### unsafe採用判断

- **非推奨**。Expression TreeやReflection周りをunsafe化しても、コード複雑化に対して効果が薄いです。
- ここは `Span` と配列コピー削減で十分です。

## `AsyncReferenceFinder` / `ScritableObjectManagerWindow`

### 対象

- `Editor/WindowEditor/AsyncReferenceFinder.cs`
- `Editor/WindowEditor/ScritableObjectManagerWindow.cs`

### 問題

参照検索とScriptableObject管理は、主に次が重いです。

- `AssetDatabase.FindAssets`
- `AssetDatabase.LoadAssetAtPath`
- `SerializedObject` / `SerializedProperty` 走査
- `GetComponentsInChildren<Component>`
- `Distinct().ToList()`
- `searchText.Split(' ')`

### 改善案

- 検索tokenは `string[]` ではなく、入力変更時に正規化済みtokenリストを保持する。
- `Distinct().ToList()` は結果格納時点で `HashSet<int>` / `HashSet<Object>` を併用して重複排除する。
- `GetFieldType` のキーは現在 `(Type, prop.name)` だが、ネストや同名フィールドを考えるなら `propertyPath` 寄りのキャッシュを検討する。ただし `propertyPath` は文字列コストがあるため、検索処理中だけのキャッシュに限定する。
- `MatchSearch(string name, in Span<string> tokens, ...)` は `Span<string>` を使う利点が薄いです。`string[]` または `ReadOnlySpan<string>` にして呼び出し側を単純化する方がよいです。

### unsafe採用判断

- **非推奨**。I/O、AssetDatabase、Unityオブジェクト走査が支配的です。
- 低レベル化より、検索頻度削減、差分更新、HashSetによる重複排除が有効です。

## `GUIContentCache`

### 現状

`GUILayout.Width(width)` と `GUIContent` をキャッシュしています。これはGC削減として妥当です。

### 改善案

- `GetWidth(float width)` は `float -> int` に丸めて辞書キー化しており妥当。
- 呼び出し幅が定数中心なら、よく使う幅を `static readonly GUILayoutOption` として直接持つ方がDictionary lookupを避けられます。
- `GetContent(string)` は文字列キーのDictionary lookupが残るため、ホットパスでは事前に `static readonly GUIContent` を持つ方がよいです。

### unsafe採用判断

- **不要**。GUIContent/GUILayoutOptionはUnity API側のオブジェクトなのでunsafe化対象ではありません。

## 適用しない方がよい箇所

| 対象 | 理由 |
| --- | --- |
| `EditorGUI` / `EditorGUILayout` 呼び出し全般 | Unity側のGUI処理が支配的。unsafe化不可。 |
| `SerializedObject` / `SerializedProperty` 走査 | Unity管理オブジェクト。低レベルメモリアクセスで扱うべきではない。 |
| `AssetDatabase` 処理 | I/OとUnity内部処理が支配的。 |
| `MethodInfo.Invoke` | unsafe化ではなく、必要ならdelegate化を検討。ただし引数object[]の汎用ボタンでは設計変更が必要。 |
| `Editor.CreateCachedEditor` | Unity側のEditor生成/保持。unsafe化対象外。 |
| `UnityEngine.Object` 参照管理 | InstanceID以外の低レベル参照操作は危険。Unityのライフサイクルと衝突しやすい。 |

## 実装優先順

1. `SingleFlagOnlyDrawer` の enumメタデータキャッシュを追加し、`Enum.Parse` / `Enum.GetValues` / `Array.IndexOf` を描画中から外す。
2. `EnableIfEvaluator` の enum判定を `Convert.ToInt64` から型別変換delegateに置き換える。
3. `EditorReflectionCache.PathElement.Parse` を `ReadOnlySpan<char>` parserに置き換える。
4. `OnInstectorButtonDrawLogic.DrawDictionary` と `ShowInspectorButtonDrawer.DrawDictionary` のキーコピーListを再利用バッファ化する。
5. `OnInstectorButtonDrawLogic.DrawField` の型判定結果とfactoryを `Type -> TypeDrawInfo` / `Type -> Func<object>` でキャッシュする。
6. Profilerで `GC.Alloc`、`EditorLoop`、`GUI.Repaint`、`PropertyDrawer.OnGUI` の差分を確認し、unsafe部分は実測効果があるものだけ残す。

## まとめ

このEditor拡張で速度向上が見込める低レベル化は、ポインタ操作ではなく、主に「boxing削減」「一時文字列削減」「一時List削減」「Reflection結果の事前計算」です。

`unsafe` を使う価値がある候補は enum値の読み取り部分にほぼ限定されます。それ以外は `ReadOnlySpan<char>`、構造体キー、型別キャッシュ、バッファ再利用で対応する方が、速度・安全性・保守性のバランスがよいです。

# Editor拡張 インライン展開候補調査

## 方針

Unity Editor拡張の描画負荷を下げる目的では、`MethodImplOptions.AggressiveInlining` は「頻繁に呼ばれる」「小さい」「分岐やDictionary/Reflection/GUI API呼び出しが少ない」関数に限定して使うのが安全です。

Editor描画の主なボトルネックは `EditorGUI` / `EditorGUILayout` / `SerializedObject` / `Reflection` / `AssetDatabase` / `Dictionary` の探索であることが多く、これらを含む関数へ `AggressiveInlining` を付けても効果は出にくいです。むしろコードサイズ増加や読みづらさの方が目立つ可能性があります。

そのため、このレポートでは次の3段階で分類します。

- **付ける候補**: 小さく、純粋に近く、Dictionaryキー比較やビット演算など描画中に大量実行される可能性がある関数。
- **書き換え候補**: 大きな関数から小さな判定関数を切り出すと、JIT/コンパイラが自然にインラインしやすくなる関数。
- **付けない候補**: Unityコールバック、GUI描画、Reflection、AssetDatabase、SerializedObject、Editor生成を含む関数。

`AggressiveInlining` を使う場合は、対象ファイルに `using System.Runtime.CompilerServices;` を追加します。

## 優先度高

| ファイル | 対象 | 改善候補 | AggressiveInlining |
| --- | --- | --- | --- |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `DrawPathKey.Equals(DrawPathKey)` | `Dictionary<DrawPathKey, ...>` の比較で呼ばれるため、現状の式形式のまま属性を付ける候補。 | **付ける候補**。小さく純粋で、キー比較のホットパスになりやすい。 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `DrawPathKey.GetHashCode()` | `Dictionary<DrawPathKey, ...>` のハッシュ計算で呼ばれるため、現状の `unchecked` ハッシュ計算に属性を付ける候補。 | **付ける候補**。整数演算のみで小さい。 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `DrawPathKey.Child(int token, int childIndex = 0)` | ネストしたパラメータ描画で繰り返し呼ばれる。`CombineToken` とコンストラクタ呼び出しだけなので属性候補。 | **付ける候補**。小さく、`DrawField` 系から何度も呼ばれる。 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `CombineToken(int parentToken, int childToken)` | `DrawPathKey.Child` から呼ばれる整数ハッシュ補助。 | **付ける候補**。整数演算のみ。 |
| `Editor/PropertyDrawer/EnableIf/EnableIfDrawer.cs` | `EvaluationCacheKey.Equals(EvaluationCacheKey)` | `evaluationCache` のキー比較で呼ばれる。 | **付ける候補**。小さく、1描画中キャッシュのホットパス。 |
| `Editor/PropertyDrawer/EnableIf/EnableIfEnumDrawer.cs` | `EvaluationCacheKey.Equals(EvaluationCacheKey)` | `EnableIfDrawer` と同様。 | **付ける候補**。小さく、辞書キー比較向き。 |
| `Editor/PropertyDrawer/SingleFlagOnlyDrawer.cs` | `IsSingleFlag(int value, SingleFlagOnlyAttribute attr)` | ビット判定のみ。`OnGUI` 内の補正判定から呼ばれる。 | **付ける候補**。GUI APIを含まず小さい。 |
| `Editor/PropertyDrawer/SingleFlagOnlyDrawer.cs` | `GetFirstBit(int value)` | `value & -value` のみ。 | **付ける候補**。小さく純粋。 |
| `Editor/PropertyDrawer/SingleFlagOnlyDrawer.cs` | `LayerToMask(int layer)` | Layer番号からmaskへの変換のみ。 | **付ける候補**。整数演算のみ。 |
| `Editor/PropertyDrawer/EnableIf/EnableIfEvaluator.cs` | `EnumToUInt64<T>(T value)` | `Unsafe.SizeOf<T>()` と `Unsafe.As` のみ。 | **付ける候補**。ただし実使用箇所が少ない場合は優先度低。 |
| `Editor/PropertyDrawer/SingleFlagOnlyDrawer.cs` | `EnumToUInt64<T>(T value)` | `EnableIfEvaluator` と同じ変換関数。 | **付ける候補**。ただし重複実装なので共通化も検討。 |

### 書き換え例

```csharp
using System.Runtime.CompilerServices;

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly bool Equals(DrawPathKey other)
{
    return rootId == other.rootId
        && methodToken == other.methodToken
        && paramToken == other.paramToken
        && depth == other.depth
        && index == other.index;
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly override int GetHashCode()
{
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
```

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static int CombineToken(int parentToken, int childToken)
{
    unchecked
    {
        return (parentToken * 397) ^ childToken;
    }
}
```

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
static bool IsSingleFlag(int value, SingleFlagOnlyAttribute attr)
{
    return (attr.allowNothing || value != 0) && (value & (value - 1)) == 0;
}
```

## 条件付き候補

| ファイル | 対象 | 改善候補 | AggressiveInlining |
| --- | --- | --- | --- |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `GetRootId(object target)` | `UnityEngine.Object` なら `GetInstanceID()`、それ以外は `GetHashCode()`。 | **条件付き**。小さいが型判定とUnity API呼び出しを含む。Profilerで呼び出し回数が多い場合のみ。 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `GetFoldout(in DrawPathKey pathKey)` | Dictionary lookupと初期値登録を含む。 | **基本は付けない**。Dictionaryコストが支配的。自然インラインに任せる程度でよい。 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `SetFoldout(in DrawPathKey pathKey, bool value)` | Dictionary代入のみ。 | **条件付き**。非常に小さいが、Dictionary代入が支配的。付けても効果は限定的。 |
| `Editor/PropertyDrawer/EnableIf/EnableIfDrawer.cs` | `EvaluationCacheKey.GetHashCode()` | 現状は `HashCode.Combine`。 | **条件付き**。`HashCode.Combine` 呼び出しが残るため、手書きhashへ変えるなら属性候補。現状のままなら不要。 |
| `Editor/PropertyDrawer/EnableIf/EnableIfEnumDrawer.cs` | `EvaluationCacheKey.GetHashCode()` | `EnableIfDrawer` と同様。 | **条件付き**。手書きhash化するなら候補。 |
| `Editor/PropertyDrawer/EnableIf/EnableIfDrawer.cs` | `PrepareCacheForCurrentDraw()` | `Event.current` と `Time.frameCount` の比較で、1描画中キャッシュをクリアする。 | **基本は付けない**。小さいがUnity状態参照とDictionary clearを含む。 |
| `Editor/PropertyDrawer/EnableIf/EnableIfEnumDrawer.cs` | `PrepareCacheForCurrentDraw()` | `EnableIfDrawer` と同様。 | **基本は付けない**。 |
| `Editor/PropertyDrawer/SingleFlagOnlyDrawer.cs` | `MaskToLayer(int mask)` | ループで最初のbitを探す。 | **条件付き**。小さいが最大32回ループ。`System.Numerics.BitOperations` が使える環境なら置換候補。 |
| `Editor/PropertyDrawer/SingleFlagOnlyDrawer.cs` | `FixEnum(Type enumType, int value, SingleFlagOnlyAttribute attr)` | `value == 0` 分岐と `GetFirstEnumValue` / `GetFirstBit` 呼び出し。 | **付けない寄り**。`GetFirstEnumValue` がReflection/列挙を含む。 |
| `Editor/EditorCache/EditorReflectionCache.cs` | `PathElement` コンストラクタ | 値代入のみ。 | **付けてもよいが優先度低**。Parse中のみで、Split/Substring/Parseの方が重い。 |
| `Editor/EditorCache/EditorReflectionCache.cs` | `GetObjectContainingField(SerializedProperty property, object root)` | `root ?? GetParentTarget(property)` の薄いラッパー。 | **付けない**。呼び出し元に直接書いてもよく、`GetParentTarget` が重い。 |
| `Editor/CustomEditor/Logic/NestesObjectDrawLogic.cs` | `TryGetOrCreateEditorCache(UnityEngine.Object obj, out Editor editor)` | 薄いラッパー。 | **付けない**。内部でEditor取得/生成に進むため、インライン効果は限定的。 |

### `EvaluationCacheKey.GetHashCode` の書き換え候補

`HashCode.Combine` は読みやすい一方、インライン対象としては呼び出し先に依存します。Dictionaryキーのホットパスでさらに詰めるなら、`DrawPathKey` と同じ手書きhashへ寄せる候補があります。

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly override int GetHashCode()
{
    unchecked
    {
        int hash = targetId;
        hash = (hash * 397) ^ attributeId;
        hash = (hash * 397) ^ (propertyPath != null ? propertyPath.GetHashCode() : 0);
        return hash;
    }
}
```

ただし `propertyPath.GetHashCode()` はstring側の処理が残るため、`DrawPathKey` ほど効果は出ません。

## 大きな関数の分割候補

`AggressiveInlining` を大きな描画関数へ直接付けるのではなく、判定部分を小さな関数へ分けると、コンパイラが自然にインラインしやすくなります。

### `OnInstectorButtonDrawLogic.DrawField`

`DrawField(Type t, string name, object currentValue, DrawPathKey pathKey)` は、型判定、Unity GUI描画、配列/List/Dictionary/Object分岐、Nullable/Enum/Object生成まで含む大きな関数です。ここへ `AggressiveInlining` を付けるべきではありません。

候補:

- `IsPrimitiveLike(Type type)` のような型判定を小関数化する。
- `IsUnityObjectType(Type type)` を小関数化する。
- `IsListType(Type type)` / `TryGetListElementType(Type type, out Type elementType)` を小関数化する。
- `IsDictionaryType(Type type)` / `TryGetDictionaryTypes(Type type, out Type keyType, out Type valueType)` を小関数化する。
- `GetDrawKind(Type type)` を作り、`DrawField` の先頭で型分類を済ませる。

例:

```csharp
private enum DrawKind
{
    Unsupported,
    Bool,
    Int,
    Float,
    String,
    Enum,
    UnityObject,
    List,
    Array,
    Dictionary,
    Object,
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static bool IsUnityObjectType(Type type)
{
    return typeof(UnityEngine.Object).IsAssignableFrom(type);
}
```

この場合、`AggressiveInlining` を付けるのは `IsUnityObjectType` のような小さい判定関数だけです。`GetDrawKind` は分岐が増えるため、最初は属性なしでよいです。

### `SingleFlagOnlyDrawer.MaskToLayer`

現在は最大32回ループで最初のbitを探しています。Unity 6000 / 現在のC#ランタイムで `System.Numerics.BitOperations` が使用できるなら、次のように書き換えると短くなり、インラインされやすくなります。

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
static int MaskToLayer(int mask)
{
    return mask <= 0 ? 0 : BitOperations.TrailingZeroCount((uint)mask);
}
```

ただし Unity のAPI互換レベルや対象ランタイムで使えるかを確認してから採用してください。互換性を優先するなら現状のループ維持で問題ありません。

### `EnableIfEvaluator`

`EvaluateConditionsRecursive` と `EvaluateEnumConditionRecursive` は、SerializedProperty探索、Reflection getter取得、switch、ログ出力を含むためインライン対象ではありません。

分割するなら、次のような純粋判定部分だけ候補です。

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static bool ApplyNegate(bool value, bool negate)
{
    return negate ? !value : value;
}
```

ただしこの程度の関数はJITが自然に処理できる可能性が高く、可読性が上がる場合だけ分けるのがよいです。

## 付けない方がよい関数

以下は `AggressiveInlining` を付けない方がよいです。

| ファイル | 対象 | 理由 |
| --- | --- | --- |
| `Editor/CustomEditor/OnInspectorButtonEditor.cs` | `OnInspectorGUI()` | Unity callbackかつGUI描画本体。 |
| `Editor/CustomEditor/NetCodeButtonEditor.cs` | `OnInspectorGUI()` | Unity callbackかつGUI描画本体。 |
| `Editor/PropertyDrawer/*Drawer.cs` | `OnGUI()` / `GetPropertyHeight()` | Unity callback。EditorGUI呼び出しが支配的。 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `DrawInspectorButtons()` | Reflectionキャッシュ、GUI、メソッド呼び出しを含む。 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `DrawButtonForMethod()` | GUIとReflection/Invoke準備を含む。 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `InvokeMethod()` | `MethodInfo.Invoke` が支配的。 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `DrawField()` / `DrawList()` / `DrawArray()` / `DrawDictionary()` / `DrawObject()` | 大きな分岐とGUI描画が中心。 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `GetPathString()` | Dictionary lookupと文字列生成を含む。文字列結合削減済みなので属性対象外。 |
| `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs` | `GetDefaultOrNull()` / `CanCreate()` | `Activator.CreateInstance` / Reflection / 例外処理を含む。 |
| `Editor/CustomEditor/Logic/NestesObjectDrawLogic.cs` | `DrawNestedScriptableObject()` / `DrawNestedScriptableObjectsRecrusiveInternal()` / `DrawSOReference()` | SerializedObject、Editor生成、GUI、再帰を含む。 |
| `Editor/EditorCache/EditorReflectionCache.cs` | `GetParentTarget()` / `CreateOrGetPropertyPathGetter()` / `CreateGetter()` / `GetFieldRecursive()` | Reflection、Expression生成、SerializedProperty処理が中心。 |
| `Editor/PropertyDrawer/SerializeReferenceViewDrawer.cs` | `ShowTypeMenu()` / `GetOrCreateCache()` | TypeCache、GenericMenu、GUIContent生成を含む。 |
| `Editor/PropertyDrawer/SceneDrawer.cs` | `GetSceneAsset()` | AssetDatabase探索を含む。 |
| `Editor/WindowEditor/*` | `OnGUI()` / `Refresh()` / `FindAllScriptableObjects()` / `SearchReference()` | AssetDatabase、SerializedObject、EditorWindow描画、非同期探索が中心。 |
| `Editor/AutoEventGenerator.cs` | 生成系関数 | MenuItem/ファイル生成/AssetDatabase更新が中心。 |
| `Editor/Utility/LogUtility.cs` | ログ文字列生成関数 | string構築とログ出力が目的。インラインより呼び出し回数削減が有効。 |

## 実装時の推奨順

1. `OnInstectorButtonDrawLogic.DrawPathKey` の `Equals` / `GetHashCode` / `Child` / `CombineToken` に限定して `AggressiveInlining` を付ける。
2. `EnableIfDrawer` / `EnableIfEnumDrawer` の `EvaluationCacheKey.Equals` を対象にする。`GetHashCode` は手書きhashへ変える場合だけ対象にする。
3. `SingleFlagOnlyDrawer` のビット演算系 `IsSingleFlag` / `GetFirstBit` / `LayerToMask` / `EnumToUInt64` を対象にする。
4. `DrawField` のような大きな関数は、属性を付けずに型分類や純粋判定を小関数へ分割する。
5. Profilerで `GC.Alloc` と `EditorLoop` の描画時間を再測定し、属性追加による実測差がない関数は属性を外す。

## 注意点

- `AggressiveInlining` は命令ではなくヒントです。JITやUnityの実行環境が必ず従うわけではありません。
- Editor拡張では、インライン化よりも `GUIContent` キャッシュ、Reflection結果キャッシュ、文字列生成削減、`SerializedProperty.propertyPath` の扱い、Dictionaryキーの構造体化の方が効果が大きいことが多いです。
- `AggressiveInlining` を広範囲に付けると、コードサイズ増加で逆に命令キャッシュ効率が悪化する可能性があります。
- まずは「Dictionaryキーの `Equals` / `GetHashCode`」「整数/ビット演算の小関数」に限定するのが安全です。

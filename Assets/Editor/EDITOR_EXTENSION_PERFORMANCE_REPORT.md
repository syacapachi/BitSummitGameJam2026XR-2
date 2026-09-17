# Editor拡張パフォーマンス改善調査

## 前提

- 対象: `Assets/Editor` 以下の EditorWindow / CustomEditor / PropertyDrawer
- 症状: Inspector などの1回描画で約100KB GC.Alloc、描画時間約100ms
- 調査方法: 静的コード確認。Profilerのサンプル名までは未確認なので、以下は「Alloc/CPUを生みやすい箇所」の優先度付き改善案です。

## 結論

最優先で見るべき箇所は次の3つです。

1. `OnInspectorButtonEditor` / `NetCodeButtonEditor` が全Inspector描画で `DrawInspectorButtons` と `DrawNestedScriptableObject` を呼んでいる
2. `OnInstectorButtonDrawLogic` が描画中に文字列補間、`GUILayout`、`Activator`、`ObjectNames.NicifyVariableName`、辞書キー文字列生成を多用している
3. `EnableIf` 系 PropertyDrawer が `OnGUI` と `GetPropertyHeight` の両方で同じ条件評価を行い、`propertyPath` 解析とgetter取得を2回走らせている

100KB級のGC.Allocは、1箇所の巨大確保というより、`GUILayout` のレイアウト確保、文字列生成、LINQ/配列生成、PropertyDrawer の二重評価が積み重なっている可能性が高いです。

## 優先度A: すぐ効きそうな改善

### A-1. ネストScriptableObject探索を型キャッシュで早期スキップする

対象:

- `Editor/CustomEditor/OnInspectorButtonEditor.cs:62-67`
- `Editor/CustomEditor/NetCodeButtonEditor.cs:21-26`
- `Editor/CustomEditor/Logic/NestesObjectDrawLogic.cs:38-72`

現状:

- すべてのInspector描画で `NestesObjectDrawLogic.DrawNestedScriptableObject(target)` が呼ばれる
- 内部で毎回 `new HashSet<UnityEngine.Object>(2)` と `new SerializedObject(obj)` を作り、全プロパティを走査する
- ScriptableObject参照を持たない型でも同じ処理が走る

改善案:

- `Type -> bool hasNestedObjectReferenceCandidate` をキャッシュする
- `SerializedObject` 走査前に、対象型のフィールドに `ScriptableObject` / `UnityEngine.Object` / 配列・List があるかをReflectionで一度だけ判定する
- 候補なしなら `DrawNestedScriptableObject` を呼ばない
- `visited` は毎回newせず、描画用の静的 `HashSet` を `Clear()` して使う。ただし再帰や例外に備えて `try/finally` で必ずクリアする

期待効果:

- 通常のMonoBehaviourでネストSOがない場合、`SerializedObject` 生成と全プロパティ走査を丸ごと削減
- Inspector描画1回あたりのGCとCPUの両方に効く

### A-2. `EnableIf` の条件評価を1描画イベント中でキャッシュする

対象:

- `Editor/PropertyDrawer/EnableIf/EnableIfDrawer.cs:11-32`
- `Editor/PropertyDrawer/EnableIf/EnableIfEnumDrawer.cs:11-40`
- `Editor/PropertyDrawer/EnableIf/EnableIfEvaluator.cs:19-142`
- `Editor/EditorCache/EditorReflectionCache.cs:110-118`

現状:

- `OnGUI` と `GetPropertyHeight` の両方で `EvaluateConditionsRecursive` / `EvaluateEnumConditionRecursive` を呼ぶ
- 1フィールドにつき最低2回、`GetParentTarget`、`property.propertyPath` 取得、getter探索が走る
- 条件名が `!field` の場合に `Substring(1)` が走る
- Enum判定で `Convert.ToInt64` によるboxing/変換コストがある

改善案:

- `(targetInstanceID, property.propertyPath, attribute instance, Event.current.type)` か、少なくとも `(targetInstanceID, property.propertyPath, attribute)` で評価結果を短期キャッシュする
- `Layout` と `Repaint` で同じ結果を使い回す
- `EnableIfAttribute` 側で条件名を事前パースできるなら、`name`, `negate` を配列として持つ
- Enum値比較は、可能なら `SerializedProperty.enumValueIndex` を使う Drawer と、Reflection getter を使う Drawer を分ける

期待効果:

- EnableIf付きフィールドが多いInspectorで、描画時間と小さなGCをまとめて削れる

### A-3. `OnInstectorButtonDrawLogic` の描画中文字列生成を減らす

対象:

- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:281`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:342`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:509`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:529`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:557`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:579`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:605`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:629-630`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:707`

現状:

- `DrawField` の再帰中に `$"{path}#..."`、`$"{name} Element[{i}]"`、`$"{name} [{list.Count}]"` を毎回生成している
- `ObjectNames.NicifyVariableName(name)` を毎回呼ぶ
- Foldoutキーが文字列なので、ネストが深いほど文字列確保が増える

改善案:

- Foldoutキーを文字列から構造体キーにする  
  例: `readonly struct DrawPathKey { int rootId; int methodToken; int paramToken; int depth; int index; }`
- ラベルは `GUIContent` としてキャッシュする  
  例: `(Type, fieldName)` -> `GUIContent`
- `ObjectNames.NicifyVariableName` の結果を `(Type, fieldName)` でキャッシュする
- リスト/配列の `Element[i]` ラベルは、展開中の要素数分だけキャッシュし、サイズ変化時だけ増やす

期待効果:

- 100KB Alloc のうち、文字列由来の細かい確保を大きく削れる
- GC頻度を下げやすい

### A-4. `GUILayout` / `EditorGUILayout` をホットパスから減らす

対象:

- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:267-306`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:501-646`
- `Editor/CustomEditor/Logic/NestesObjectDrawLogic.cs:107-132`
- `Editor/WindowEditor/ScritableObjectManagerWindow.cs:40-164`

現状:

- `GUILayout` / `EditorGUILayout` はレイアウト計算用オブジェクトを作りやすい
- `VerticalScope`, `HorizontalScope`, `GUILayout.Width(50)` などが大量にある
- 特にPropertyDrawer相当の処理で `GUILayout` を混ぜると、制御しにくいAllocになりやすい

改善案:

- PropertyDrawer/Inspectorの繰り返し部分は `Rect` ベースの `EditorGUI.*` に寄せる
- `GUILayoutOption[]` を使う場合は static readonly でキャッシュする  
  例: `static readonly GUILayoutOption[] Width50 = { GUILayout.Width(50) };`
- まずはボタン、Foldout、Labelなど数が多い箇所から置き換える

期待効果:

- `GC.Alloc` と Layout/Repaint のCPUを同時に削減できる

## 優先度B: 中期改善

### B-1. `OnInstectorButtonDrawLogic` の型分岐を描画デリゲート化する

対象:

- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:340-499`

現状:

- `DrawField` で毎回 `if (t == typeof(...))` を上から順に判定する
- `t.IsEnum`, `t.IsGenericType`, `typeof(UnityEngine.Object).IsAssignableFrom(t)` なども毎回走る

改善案:

- `Dictionary<Type, FieldDrawerDelegate>` を作る
- 初回だけ型を分類し、2回目以降は直接delegateを呼ぶ
- primitive / Unity value type / enum / object reference / list / array / dictionary / object の分類結果を `Type -> DrawKind` としてキャッシュする

期待効果:

- フィールド数が多いInspectorでCPU時間を削減
- コンパイラ/JITが最適化しやすい小さい関数に分割できる

### B-2. `Activator.CreateInstance` と `MakeGenericType` をファクトリキャッシュ化する

対象:

- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:504`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:601`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:853-858`
- `Editor/PropertyDrawer/ShowInspectorButtonDrawer.cs:153`
- `Editor/PropertyDrawer/ShowInspectorButtonDrawer.cs:196`
- `Editor/PropertyDrawer/ShowInspectorButtonDrawer.cs:252`

現状:

- nullのList/Dictionary/Objectに遭遇するたびに `Activator.CreateInstance`
- Listの場合は `typeof(List<>).MakeGenericType(elementType)` も実行

改善案:

- `Dictionary<Type, Func<object>> defaultFactoryCache` を作る
- `Expression.New(type).Compile()` で生成delegateをキャッシュ
- `List<T>` 型も `elementType -> Type listType` と `elementType -> Func<IList>` でキャッシュ

期待効果:

- 初回以外のReflection生成コストを削減
- null値を含むネスト構造の描画で効く

### B-3. `EditorReflectionCache.PathElement.Parse` を手書きparserにする

対象:

- `Editor/EditorCache/EditorReflectionCache.cs:55-68`
- `Editor/EditorCache/EditorReflectionCache.cs:382`
- `Editor/EditorCache/EditorReflectionCache.cs:410`

現状:

- `Replace`, `Split`, `Substring`, `Replace`, `Skip(...).ToArray()`, `new Expression[]` が初回パス解析時に発生
- pathCacheで2回目以降は抑えているが、Inspector初回表示や配列要素が多い場合にまとめてAllocする

改善案:

- `propertyPath` を1文字ずつ読むparserにする
- `.Array.data[n]` を中間文字列なしで `PathElement(name, n)` に変換する
- `ReflectionPath` は `ArraySegment<PathElement>` 相当で保持し、`Skip().ToArray()` を避ける
- `Expression.MakeIndex` の引数配列も可能ならキャッシュ、または `Expression.Constant(index)` 周辺の生成回数を初回のみに限定できているか確認する

期待効果:

- 初回表示時のGCスパイクを削減

### B-4. `ScritableObjectManagerWindow` の検索結果をキャッシュする

対象:

- `Editor/WindowEditor/ScritableObjectManagerWindow.cs:50-66`
- `Editor/WindowEditor/ScritableObjectManagerWindow.cs:175-179`
- `Editor/WindowEditor/ScritableObjectManagerWindow.cs:212-222`

現状:

- `OnGUI` のたびに `new List<ScriptableObject>()`
- 検索中は毎フレーム `Where(...).ToList()`
- `MatchSearch` が毎回 `searchText.Split(' ')` と LINQ `All` を呼ぶ
- `FindAllScriptableObjects` も LINQ chain で一括確保する

改善案:

- 検索文字が変わったタイミングだけ `searchTokens` を作る
- フィルタ結果は `Dictionary<Type, List<ScriptableObject>> filteredGroups` に保持し、検索条件変更時だけ再構築
- `Where/Select/ToList` をfor文化し、Listを再利用する
- `GUILayout` から `ListView` / UI Toolkit へ移行すると、件数が多い時に仮想化できる

期待効果:

- Windowを開いているだけで発生する毎フレームAllocを削減

## 優先度C: 構造改善

### C-1. `CustomEditor(typeof(UnityEngine.Object), true)` の影響範囲を狭める

対象:

- `Editor/CustomEditor/OnInspectorButtonEditor.cs:13`
- `Editor/CustomEditor/NetCodeButtonEditor.cs:13`

現状:

- ほぼすべてのUnityEngine.Objectに対して独自Editorがかかる
- その結果、`[OnInspectorButton]` がない型でも毎回ボタン探索とネストSO探索の入口を通る

改善案:

- 最低限、`Type -> bool hasInspectorExtensionFeature` をキャッシュして、対象外なら `DrawDefaultInspector()` のみでreturnする
- 可能なら対象を `MonoBehaviour` / `ScriptableObject` に絞る
- さらに進めるなら、属性付き型だけに専用Editorを生成する方式を検討する。ただしUnityのCustomEditor解決仕様との相性確認が必要

期待効果:

- 拡張機能を使っていない通常Inspectorのオーバーヘッドをほぼ消せる

### C-2. `ShowInspectorDrawer` と `OnInstectorButtonDrawLogic.DrawField` の重複を整理する

対象:

- `Editor/PropertyDrawer/ShowInspectorButtonDrawer.cs`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs`

現状:

- 似た型描画ロジックが2系統ある
- `ShowInspectorDrawer` 側は `GetFields` キャッシュがなく、辞書キーコピー `new(dict.Keys.Count + 10)` などがある
- `PropertyDrawer` 内で `GUILayout.Button` を使っており、Rectベースの描画と混ざっている

改善案:

- 型分類・デフォルト値生成・GUIContent生成・field list取得を共通キャッシュに寄せる
- PropertyDrawer版はRectベースに統一する
- 高さ計算も実際の展開状態と要素数に合わせて返す

期待効果:

- メンテ対象が減り、同じ最適化を2箇所に入れなくて済む

### C-3. 静的キャッシュの寿命と破棄を管理する

対象:

- `Editor/CustomEditor/Logic/NestesObjectDrawLogic.cs:10-12`
- `Editor/CustomEditor/Logic/OnInstectorButtonDrawLogic.cs:165-180`
- `Editor/EditorCache/EditorReflectionCache.cs:77-86`

現状:

- static Dictionary が増え続ける可能性がある
- `Editor` インスタンスを `editorCache` に保持するが、破棄タイミングがない

改善案:

- `AssemblyReloadEvents.beforeAssemblyReload` で `EditorReflectionCache.ClearCache()` と Editor破棄を呼ぶ
- `EditorApplication.playModeStateChanged` や `Selection.selectionChanged` で不要なEditor cacheを掃除する
- `Editor` は `UnityEngine.Object.DestroyImmediate(editor)` で破棄する
- `UnityEngine.Object` キーは destroyed object が残りやすいので、null扱いになったキーを定期的に削除する

期待効果:

- 長時間Editor作業時のメモリ増加を抑制

## 個別メモ

### `SingleFlagOnlyDrawer`

対象:

- `Editor/PropertyDrawer/SingleFlagOnlyDrawer.cs:121`
- `Editor/PropertyDrawer/SingleFlagOnlyDrawer.cs:146-165`
- `Editor/PropertyDrawer/SingleFlagOnlyDrawer.cs:248`
- `Editor/PropertyDrawer/SingleFlagOnlyDrawer.cs:281`

改善案:

- `property.enumDisplayNames` / `property.enumNames` の配列参照は型ごとにキャッシュできるか確認する
- `Enum.Parse`, `Array.IndexOf`, `Enum.GetValues` を型ごとの辞書に置き換える
- `InternalEditorUtility.layers` はLayer変更時以外はキャッシュする

### `SceneDrawer`

対象:

- `Editor/PropertyDrawer/SceneDrawer.cs:45-62`

改善案:

- `EditorBuildSettings.scenes` の走査と `AssetDatabase.LoadAssetAtPath` を毎描画で行わない
- `sceneName -> SceneAsset` / `sceneName -> path` の辞書を作り、`EditorBuildSettings.sceneListChanged` で更新する

### `AsyncReferenceFinder`

対象:

- `Editor/WindowEditor/AsyncReferenceFinder.cs:61`
- `Editor/WindowEditor/AsyncReferenceFinder.cs:112`
- `Editor/WindowEditor/AsyncReferenceFinder.cs:149-154`
- `Editor/WindowEditor/AsyncReferenceFinder.cs:180`
- `Editor/WindowEditor/AsyncReferenceFinder.cs:225-226`

改善案:

- `SerializedObject` は `using` で明示的にDisposeする
- `GetComponentsInChildren<Component>(true)` は配列確保するため、検索頻度が高いならキャッシュか段階処理を検討する
- `GetField(prop.name, ...)` は毎プロパティReflectionなので `(Type, propertyPath/name)` キャッシュする
- `Distinct().ToList()` は `HashSet<Object>` を検索中から使えば最後のLINQ確保を消せる

これはInspectorの1描画GCではなく、検索実行中のEditor update負荷として扱うのがよいです。

## 実装順序案

1. `NestesObjectDrawLogic` の早期スキップを追加
2. `EnableIf` の1イベント内評価キャッシュを追加
3. `OnInstectorButtonDrawLogic` の文字列キー/ラベルキャッシュを追加
4. `ScritableObjectManagerWindow` の検索フィルタをOnGUI外で再構築する
5. `GUILayout` をホットパスから順に `EditorGUI` へ置き換える
6. 型ごとの描画delegate / default factory cache を入れる
7. 静的Editor cacheの破棄処理を入れる

## 計測するときの見方

- Profilerは `EditorLoop` / `InspectorWindow.OnGUI` / `GUIView.Repaint` / `GUILayoutUtility.Layout` / `EditorGUILayout` 周辺を見る
- Deep ProfileはEditor拡張では重くなりやすいので、まず `ProfilerMarker` を追加して区間を見る
- 追加候補:
  - `OnInspectorButtonEditor.OnInspectorGUI`
  - `OnInstectorButtonDrawLogic.DrawInspectorButtons`
  - `NestesObjectDrawLogic.DrawNestedScriptableObject`
  - `EnableIfEvaluator.EvaluateConditionsRecursive`
  - `ScritableObjectManagerWindow.OnGUI`

例:

```csharp
static readonly Unity.Profiling.ProfilerMarker Marker =
    new Unity.Profiling.ProfilerMarker("Syacapachi.Editor.DrawInspectorButtons");

using (Marker.Auto())
{
    OnInstectorButtonDrawLogic.DrawInspectorButtons(target);
}
```

## まず狙うべき目標

- 通常Inspectorで、拡張機能対象外の型は追加Allocほぼ0にする
- `EnableIf` 付きフィールドの評価回数を半分にする
- `OnInstectorButtonDrawLogic` の展開済みコレクション描画で毎要素の文字列生成を止める
- `ScritableObjectManagerWindow` を開いているだけの状態で `Where/ToList/Split` が走らないようにする

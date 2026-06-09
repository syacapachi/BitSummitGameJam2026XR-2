# Tutorial ScriptableObject 化 計画

## 目的

現在のチュートリアルは `TutorialManager` が `TutorialStep` を見て `Step1_Target` / `Step2_Block` / `Step3_Marker` / `Step4_Coop` を `new` し、各イベントを `currentStepLogic` に転送して進行を管理している。

これを、各ステップの進行条件とスポーン設定を ScriptableObject データとして定義し、`TutorialManager` が現在の進行フラグに応じて必要なイベントだけを購読する構造へ変更する。

狙いは以下。

- ステップ追加・順番変更を Inspector 上のデータ差し替えで行えるようにする。
- `TutorialManager` から `new StepX(...)` と `switch` による分岐を減らす。
- ステップごとに必要なイベントだけ購読し、未使用イベントの転送をなくす。
- 完了条件の実行時カウントは Manager 側に持たせ、ScriptableObject は共有アセットとして状態を持たない。

## 現状の整理

### TutorialManager

- `NetworkVariable<TutorialStep> CurrentStep` で進行中ステップを同期している。
- `StartStep(TutorialStep step)` 内でステップクラスを `new` している。
- `OnNetworkSpawn` 時点で `AttackBlockedEvent` と `markerPlaceServerEvent` を常時購読している。
- `OnAttackBlocked` / `OnMarkerPlacedServer` / `OnEnemyKilled` などを `currentStepLogic` に転送している。
- ステップ完了後、`StepCompleteRoutine` で UI 通知を出し、3.1 秒後に次へ進める。

### 各ステップ

| 現在のクラス | 開始処理 | 完了条件 |
| --- | --- | --- |
| `Step1_Target` | `step1Enemies` をプレイヤー分スポーン | `TutorialSpawner.OnAllEnemyDead` |
| `Step2_Block` | `step2Enemies` をプレイヤー分スポーン、敵を攻撃不可にする | 全プレイヤーが 1 回以上ブロック |
| `Step3_Marker` | カウント初期化のみ | 全プレイヤーがマーカー設置 |
| `Step4_Coop` | 既存敵を攻撃可能にする | `TutorialSpawner.OnAllEnemyDead` |

## 作成する ScriptableObject

### TutorialSequenceSO

チュートリアル全体の並びを定義する親データ。

予定ファイル:

- `Scripts/Tutorial/TutorialSequenceSO.cs`

予定フィールド:

```csharp
[CreateAssetMenu(fileName = "TutorialSequence", menuName = "Tutorial/Tutorial Sequence")]
public class TutorialSequenceSO : ScriptableObject
{
    [SerializeField] TutorialStepSO[] steps;

    public IReadOnlyList<TutorialStepSO> Steps => steps;
}
```

役割:

- `TutorialManager` は `TutorialStep` enum ではなく、`TutorialSequenceSO.Steps[index]` を現在ステップとして扱う。
- `NetworkVariable<int> CurrentStepIndex` を使い、クライアントにはステップ番号を同期する。
- `End` は配列終端として扱い、最後のステップ完了後に `StartMainSimulation()` を呼ぶ。

### TutorialStepSO

各ステップの共通設定を持つデータ。

予定ファイル:

- `Scripts/Tutorial/TutorialStepSO.cs`

予定フィールド:

```csharp
public enum TutorialCompletionCondition
{
    AllTutorialEnemiesDead,
    AllPlayersBlocked,
    AllPlayersPlacedMarker,
    ManualRequest
}

public enum TutorialSpawnMode
{
    None,
    SpawnEnemiesForEachPlayer,
    UseExistingTutorialEnemies
}

[CreateAssetMenu(fileName = "TutorialStep", menuName = "Tutorial/Tutorial Step")]
public class TutorialStepSO : ScriptableObject
{
    [SerializeField] string stepId;
    [SerializeField] TutorialSpawnMode spawnMode;
    [SerializeField] EnemySO[] enemies;
    [SerializeField] bool killExistingEnemiesOnStart;
    [SerializeField] bool killEnemiesOnEnd;
    [SerializeField] bool setAttackableAfterStart;
    [SerializeField] bool attackableValue;
    [SerializeField] TutorialCompletionCondition completionCondition;
    [SerializeField] int requiredCountPerPlayer = 1;
}
```

役割:

- スポーンの有無、スポーンする敵、攻撃可能状態、完了条件を Inspector で編集できるようにする。
- `requiredCountPerPlayer` はブロック回数などの拡張用。現状の Step2 は `1`。
- ScriptableObject 自身はカウント用 `Dictionary` や `HashSet` を持たない。実行中の状態は `TutorialManager` が保持する。

### 必要なら後で分割する派生 SO

初期実装は `TutorialStepSO` 1 種類にまとめる。条件が増えて Inspector が複雑になったら、以下のように分割する。

- `EnemyClearTutorialStepSO`
- `PlayerCountTutorialStepSO`
- `ManualTutorialStepSO`

ただし最初から継承 SO にするとアセット作成数と分岐が増えるため、まずは enum と共通フィールドで進める。

## TutorialManager の変更方針

### 主なフィールド

現在:

```csharp
public NetworkVariable<TutorialStep> CurrentStep;
[SerializeField] EnemySO[] step1Enemies;
[SerializeField] EnemySO[] step2Enemies;
TutorialBase currentStepLogic;
```

変更後:

```csharp
[SerializeField] TutorialSequenceSO tutorialSequence;
public NetworkVariable<int> CurrentStepIndex = new(0);

TutorialStepSO currentStep;
readonly Dictionary<ulong, int> playerProgressCounts = new();
readonly HashSet<ulong> completedPlayers = new();
bool isSubscribedStepEvents;
```

削除予定:

- `TutorialBase currentStepLogic`
- `step1Enemies`
- `step2Enemies`
- `TutorialStep` enum 依存
- `Step1_Target` / `Step2_Block` / `Step3_Marker` / `Step4_Coop` の生成処理

### StartStep の流れ

1. 前ステップ用イベントを解除する。
2. ランタイム進行状態を初期化する。
3. `CurrentStepIndex` から `TutorialStepSO` を取得する。
4. `killExistingEnemiesOnStart` が true なら `spawner.KillAll()`。
5. `spawnMode` に応じて敵をスポーンする。
6. `setAttackableAfterStart` が true なら `spawner.ApplyAttackableAfterSpawn(attackableValue)`。
7. `completionCondition` に応じて必要なイベントだけ購読する。
8. `OnTutorialStepChanged` に現在 index を通知する。
9. すでに条件達成済みの可能性がある条件は即時チェックする。

### イベント購読切り替え

`TutorialManager` はチュートリアル開始中かつサーバー側でのみ、現在ステップに必要なイベントを購読する。

| 完了条件 | 購読するイベント |
| --- | --- |
| `AllTutorialEnemiesDead` | `TutorialSpawner.OnAllEnemyDead` |
| `AllPlayersBlocked` | `AttackBlockedEvent` |
| `AllPlayersPlacedMarker` | `ULongEvent markerPlaceServerEvent` |
| `ManualRequest` | なし。`NextStepRequretRpc` などから進行 |

注意:

- `OnNetworkSpawn` では `CurrentStepIndex.OnValueChanged` のみ常時購読する。
- `AttackBlockedEvent` と `markerPlaceServerEvent` はステップ開始時に必要な場合だけ `Register` する。
- `OnNetworkDespawn` / `OnTutorialStartServerOnly` / `StartStep` 前に必ず解除する。
- 同じイベントを二重購読しないよう、`currentSubscribedCondition` か `isSubscribedStepEvents` を持つ。

### 完了判定

#### AllTutorialEnemiesDead

- `spawner.OnAllEnemyDead += OnAllTutorialEnemiesDead`
- `spawner.IsAllDead` が true の場合は開始直後に完了扱いにする。

#### AllPlayersBlocked

- `AttackBlockedEvent` を購読する。
- `AttackBlocked.Collector.ClientId` を playerId として扱う。
- `playerProgressCounts[playerId]++` する。
- `requiredCountPerPlayer` 以上になった playerId を達成扱いにする。
- `NetworkManager.ConnectedClientsIds.Count` 人が達成したら完了。

#### AllPlayersPlacedMarker

- `markerPlaceServerEvent` を購読する。
- 受け取った playerId を `completedPlayers` に追加する。
- `NetworkManager.ConnectedClientsIds.Count` 人が達成したら完了。

#### ManualRequest

- `NextStepRequretRpc` など、明示操作から `CompleteCurrentStep()` または `NextStep()` を呼ぶ。
- 現在の Step4 相当を `AllTutorialEnemiesDead` にする場合は不要。

## 作成するアセット案

置き場所は既存の `Assets/ScritableObject` 配下が候補。ただしチュートリアル専用として分かりやすくするなら以下を作る。

- `Assets/ScritableObject/Tutorial/TutorialSequence.asset`
- `Assets/ScritableObject/Tutorial/Step1_Target.asset`
- `Assets/ScritableObject/Tutorial/Step2_Block.asset`
- `Assets/ScritableObject/Tutorial/Step3_Marker.asset`
- `Assets/ScritableObject/Tutorial/Step4_Coop.asset`

設定案:

| アセット | spawnMode | enemies | attackable | completionCondition |
| --- | --- | --- | --- | --- |
| `Step1_Target` | `SpawnEnemiesForEachPlayer` | 現 `step1Enemies` | 変更なし | `AllTutorialEnemiesDead` |
| `Step2_Block` | `SpawnEnemiesForEachPlayer` | 現 `step2Enemies` | `false` | `AllPlayersBlocked` |
| `Step3_Marker` | `None` | 空 | 変更なし | `AllPlayersPlacedMarker` |
| `Step4_Coop` | `UseExistingTutorialEnemies` | 空 | `true` | `AllTutorialEnemiesDead` |

## 移行手順

1. `TutorialSequenceSO` と `TutorialStepSO` を追加する。
2. `TutorialManager` に `tutorialSequence` と `CurrentStepIndex` を追加する。
3. 既存 `CurrentStep` enum と `StartStep(TutorialStep step)` を index ベースに置き換える。
4. `TutorialBase` と `Step*_` クラスのロジックを `TutorialManager` の汎用処理へ移す。
5. `OnNetworkSpawn` の常時イベント購読をやめ、ステップ開始時の条件別購読へ変更する。
6. `OnNetworkDespawn` とステップ切り替え時に、現在条件のイベント解除を必ず呼ぶ。
7. `OnTutorialStepChanged` は既存 UI 互換のため `int` index 通知を維持する。
8. Unity Editor で `TutorialSequence.asset` と各 `TutorialStep.asset` を作成し、`TutorialManager` に割り当てる。
9. 既存 `step1Enemies` / `step2Enemies` の参照をアセットへ移す。
10. 動作確認後、未使用になった `TutorialBase` と `Step*_` クラスを削除する。

## 確認項目

- サーバー以外で進行条件イベントを処理していないこと。
- ステップ切り替え時に旧ステップのイベント購読が残らないこと。
- `OnTutorialStepCleared` と `OnTutorialStepChanged` の UI 通知タイミングが現状と変わらないこと。
- `TutorialSpawner.KillAll()` の呼び出しタイミングが Step4 の既存敵利用を壊さないこと。
- プレイヤー人数が途中で変化した場合に、完了判定へ使う人数を「ステップ開始時に固定」するか「現在接続数を見る」かを決めること。

## 実装時の注意

- ScriptableObject はアセット共有されるため、実行中に変更される値を持たせない。
- `EnemySO[]` は `IReadOnlyList<EnemySO>` として `TutorialSpawner` に渡せる。
- 現在の `Step1_Target` は `playerCount` を保持しているが、スポーン時は `step1Enemies.Count` を渡している。SO 化時は「プレイヤー数分スポーン」を基本にして、敵リスト不足時の扱いを明示する。
- `TutorialSpawner.SpawnTargetsForEachPlayer` は `remain` をリセットしていないため、連続スポーン時の扱いを実装前に確認する。
- `NextStepRequretRpc` の名前は既存互換で残してもよいが、後で `RequestNextStepRpc` にリネームしたい。

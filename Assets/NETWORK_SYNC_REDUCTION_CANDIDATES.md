# Network Sync Reduction Candidates

## 概要

このメモは、`Assets` 配下を調査して、次の同期要素の削減候補をまとめたものです。

- `NetworkVariable`
- `[Rpc]` / `[ClientRpc]`
- `INetworkSerializable` / `ISerializable` 相当の型

確認時点の件数:

- `NetworkVariable` 系: 21 件
- `Rpc` 系: 39 件
- `INetworkSerializable` / `ISerializable` 系: 5 件

## 結論サマリ

高優先度で削減できそうなものは次です。

1. 未使用の同期変数を削除する
2. UI 専用の連続同期をイベントやローカル計算へ寄せる
3. 結果表示専用の同期値を既存の結果 RPC に寄せる
4. 使われていない `INetworkSerializable` 型を削除する

特に強い候補:

- `PhaseManager.phaseFinished`
- `AvatarSyncronize.JumpCount`
- `NetworkEntry`
- `PhaseManager.phaseProgress`
- `PhaseManager.CountdownValue`
- `ScoreManager.totalBonus`
- `ScoreManager.lastClearBonus`

## 高確度の削減候補

| 優先度 | 対象 | 種類 | 根拠 | 削減案 |
| --- | --- | --- | --- | --- |
| 高 | `Assets/Scripts/RikutoScripts/PhaseManager.cs` の `phaseFinished` | `NetworkVariable<bool>` | 宣言のみで、参照・更新が見当たらない | そのまま削除 |
| 高 | `Assets/Scripts/Syncronize/AvatarSyncronize.cs` の `JumpCount` | `NetworkVariable<int>` | 参照は `StatusViewer` のデバッグ表示のみで、更新処理が見当たらない | デバッグ用途ならローカル値へ変更、不要なら削除 |
| 高 | `Assets/Scripts/utils/NetworkEntry.cs` | `INetworkSerializable` | 実使用箇所がなく、`PlayerItemControll` 側もコメントアウト済み | 型ごと削除候補 |
| 高 | `Assets/Scripts/RikutoScripts/PhaseManager.cs` の `phaseProgress` | `NetworkVariable<float>` | フェーズ進行中に毎フレーム更新される。UI 目的の連続同期としてコストが高い | `phaseStartTime + duration` を 1 回だけ送って、UI は各クライアントでローカル計算 |
| 高 | `Assets/Scripts/RikutoScripts/PhaseManager.cs` の `CountdownValue` | `NetworkVariable<int>` | 参照先が実質 `PhaseUI` だけで、整数カウントダウン表示用途 | `StartCountdown(duration, startValue)` の 1 回通知へ寄せて、UI はローカルカウント |
| 高 | `Assets/Scripts/RikutoScripts/ScoreManager.cs` の `lastClearBonus` | `NetworkVariable<int>` | 利用先は `PhaseUI` の演出表示のみ | フェーズクリア通知イベントに bonus 値を載せて送る |
| 高 | `Assets/Scripts/RikutoScripts/ScoreManager.cs` の `totalBonus` | `NetworkVariable<int>` | 利用先は `ResultUI` の結果表示のみ | 既存の `PlayerResultData[]` 結果送信に集約するか、結果表示専用 RPC に載せる |

## 中確度の削減候補

| 優先度 | 対象 | 種類 | 根拠 | 削減案 |
| --- | --- | --- | --- | --- |
| 中 | `Assets/Scripts/RikutoScripts/NetworkGameManager.cs` の `difficulty` | `NetworkVariable<Difficulty>` | 難易度は主にゲーム開始時の初期化用途。`DifficultyEvent` も別に存在する | ロビー確定後に 1 回だけイベント or 開始 RPC に同梱して、常時同期をやめる |
| 中 | `Assets/Scripts/EffectScripts/NEnemyDeathFxEmitter.cs` の `reachedProtectArea` | `NetworkVariable<bool>` | クライアントで `OnNetworkDespawn` 時に FX を出すかどうかの分岐だけに使っている | despawn 前に 1 回だけ理由通知 RPC を送る、または死亡イベントデータに理由を含める |
| 中 | `Assets/Scripts/Controller/MarkerController.cs` の `CreateMerkerRpc` / `PlaceMarkerRpc` / `MoveMarkerServerRpc` | `Rpc` 3 本 | マーカー処理が細かく分割されすぎている | 生成はサーバー初期化時へ寄せ、設置 RPC と移動処理を統合する |
| 中 | `Assets/Scripts/RikutoScripts/RoleButton.cs` の `HideClientRpc` | `Rpc` | UI を閉じるだけの単発同期 | `NetworkVariable<bool>` ではなく、既存のゲーム状態イベントに乗せるか、サーバー側で役割選択完了状態を 1 箇所管理 |
| 中 | `Assets/Scripts/ChatBoard/ChatSystem.cs` の `SendHistoryClientRpc` | `Rpc` | 履歴 1 件ごとに全員へ送ってから `targetClientId` でローカル破棄している | 対象クライアント指定 RPC に変える、または履歴を配列でまとめて 1 回送る |
| 中 | `Assets/Scripts/RikutoScripts/NEnemy.cs` の `ApplyMaxHpRpc` | `Rpc` | immutable な最大 HP を同期するためだけの RPC。敵種別が共有できれば不要 | 敵 ID / EnemySO 参照を spawn 時に解決できる形へ寄せて削除 |
| 中 | `Assets/Scripts/RikutoScripts/NEnemy.cs` の `ChangeJobRpc` / `SetVisibleRpc` / `RestoreLayerRpc` | `Rpc` | 状態反映用 RPC が細かく分かれている | `ApplyEnemyVisualStateRpc(job, visible)` のように統合可能 |
| 中 | `Assets/Scenes/testScene/Cube/BombAction.cs` の `timer` / `isExploded` / `explosionRadiusNetwork` | `NetworkVariable` | テストシーン用。演出確認以外では本番同期の価値が薄い | テスト専用なら削除、本番利用なら開始時刻 1 回同期でローカル補間 |

## 低確度または現状維持推奨

次は、現状では削減効果よりも意味の方が大きいので、すぐ削らない方がよさそうです。

| 対象 | 種類 | 理由 |
| --- | --- | --- |
| `Assets/Scripts/NetworkPlayer/PlayerHealth.cs` の `currentHP` | `NetworkVariable<float>` | UI 反映と死亡判定の両方で使っていて、同期対象として自然 |
| `Assets/Scripts/RikutoScripts/NEnemy.cs` の `currentHP` | `NetworkVariable<float>` | 複数クライアントで HP バー表示が必要 |
| `Assets/Scripts/NetworkPlayer/SyncroPropaty.cs` の `syncroJob` | `NetworkVariable<PlayerJob>` | ジョブが他プレイヤーから見える必要があり、頻度も低い |
| `Assets/Scripts/Tutorial/TutorialManager.cs` の `CurrentStep` | `NetworkVariable<TutorialStep>` | チュートリアル進行の共有状態として妥当 |
| `Assets/Scripts/Shota/WorldViewManager.cs` の `pageIndex` | `NetworkVariable<int>` | ページ送りを全員で共有したいなら妥当 |
| `Assets/Scripts/ChatBoard/ChatMessage.cs` | `INetworkSerializable` | チャット payload として実使用中 |
| `Assets/Scripts/RikutoScripts/PlayerResultData.cs` | `INetworkSerializable` | 結果画面データの受け渡しで実使用中 |
| `Assets/Scripts/RikutoScripts/GameEffectData.cs` | `INetworkSerializable` | 効果音再生 payload として実使用中 |

## 詳細メモ

### 1. `PhaseManager` は一番削減効果が大きい

`PhaseManager` は現状、UI 表示のために次を同期しています。

- `syncedPhaseIndex`
- `CountdownValue`
- `phaseFinished`
- `phaseProgress`

このうち、

- `phaseFinished` は未使用
- `phaseProgress` は毎フレーム更新
- `CountdownValue` は整数 UI 用

なので、ここが最優先です。

理想形:

- フェーズ開始時に `phaseIndex`, `serverTime`, `duration` を 1 回通知
- クライアント UI はその時刻からローカル補間
- カウントダウンも開始イベントだけ送る

### 2. `ScoreManager` の bonus 系は結果表示専用に寄っている

`score` 自体はゲームプレイ中の共有状態なので残す価値がありますが、

- `totalBonus`
- `lastClearBonus`

は用途がかなり UI 寄りです。

参照先:

- `PhaseUI.LastClearBonus`
- `ResultUI.TotalBonus`

どちらも「その瞬間に結果を見せたい」用途なので、常時同期よりイベント送信の方が向いています。

### 3. `NEnemy` は RPC が細かく割れている

`NEnemy` の RPC は、状態同期・見た目同期・最大 HP 同期が分散しています。

削減の方向性は、

- immutable な情報は spawn 時に解決
- 見た目状態は 1 つの RPC に統合
- layer / visible / job の反映を 1 つの state 構造に寄せる

です。

### 4. `ChatSystem` は関数数より送信方式の見直し余地が大きい

`BroadcastMessageRpc` はそのままでよい可能性がありますが、
`SendHistoryClientRpc` は「履歴 1 件ごとに送る」「全員に飛ばして受信側で捨てる」という形なので無駄があります。

これは削減というより整理対象です。

## すぐ着手するならこの順番

1. `PhaseManager.phaseFinished` を削除
2. `AvatarSyncronize.JumpCount` を削除
3. `NetworkEntry` を削除
4. `PhaseManager.phaseProgress` をイベント + ローカル補間へ変更
5. `PhaseManager.CountdownValue` をイベント化
6. `ScoreManager.totalBonus` / `lastClearBonus` を結果通知へ統合
7. `NEnemy` の RPC 群を整理

## 補足

このメモは「削減できそうか」をコード読みに基づいてまとめたもので、実際に削る前には次を確認した方が安全です。

- Prefab / Inspector 経由の参照
- UI 演出がその同期値に依存していないか
- Join mid-game を許すか
- 遅延参加クライアントに過去状態を見せる必要があるか

特に `NetworkVariable` をイベントに置き換える場合は、
「途中参加者が現在値を自動取得できなくなる」点が最大のトレードオフです。

# Tutorial Step1 無限ループの原因調査

## 症状
- ゲームクリアまたはゲームオーバー後にやり直す
- チュートリアルの `Step1` が開始される
- `step1Enemies` を全滅させても `Step2` へ進まず、再び `Step1` の敵が出現する
- 以降これが繰り返される

## 結論
- 最有力の原因は、**再挑戦時に `TutorialManager.CurrentStep` が `Step1` にリセットされていないこと**です。
- その状態で `OnTutorialStartServerOnly()` が **`CurrentStep` を更新せずに `StartStep(TutorialStep.Step1)` を直接呼んでいる**ため、内部状態と実際に表示・進行しているステップがズレます。
- その後 `NextStep()` が `CurrentStep.Value++` を行うと、前回の終了位置からさらに進んで **enum 範囲外の値** になり、`StartStep()` の switch に該当 case がなく、結果として **直前の `currentStepLogic` がもう一度 `OnStart()` される** 可能性があります。
- この流れだと、再挑戦後に `Step1` の `OnStart()` が何度も呼ばれ、`step1Enemies` が再スポーンし続けます。

## 最有力原因

### 1. `CurrentStep` が再挑戦時にリセットされていない
- 根拠:
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L16) `CurrentStep` は `NetworkVariable<TutorialStep>` として保持されている
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L162) `StartMainSimulation()` では `stateManager.OnTutorialEndServerOnly()` を呼ぶだけ
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L165) `isTutorlalStartedServerOnly = false;` には戻している
  - しかし `CurrentStep.Value = TutorialStep.Step1;` のようなリセット処理はない
- 影響:
  - 1 回目のチュートリアル終了後、`CurrentStep` は `End` のまま残る
  - 2 回目の開始時に内部状態が前回の続きのままになる

### 2. 再開始時に `CurrentStep` を使わず `StartStep(Step1)` を直接呼んでいる
- 根拠:
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L51) `OnTutorialStartServerOnly()`
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L55) `StartStep(TutorialStep.Step1);`
- 問題:
  - 実際に開始するステップは `Step1` でも、`CurrentStep.Value` 自体は更新されない
  - そのため「見た目の進行」と「NetworkVariable が持っている進行」が一致しない

### 3. `NextStep()` が enum 範囲外への遷移を防いでいない
- 根拠:
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L129)
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L133) `CurrentStep.Value++;`
- 問題:
  - `CurrentStep` がすでに `End` のままなら、次は `5` になります
  - `TutorialStep` は `Step1, Step2, Step3, Step4, End` までしか定義されていません
- 影響:
  - `CurrentStep` が不正値になってもそのまま `OnValueChanged` が走る可能性がある

### 4. `StartStep()` に不正 enum 値のガードがない
- 根拠:
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L63)
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L65) 先に `currentStepLogic?.OnEnd();` を呼ぶ
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L91) `TutorialStep.End` だけ特別扱い
  - `switch` に `default` がない
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L96)
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L97) 最後に `currentStepLogic?.OnStart();` を呼ぶ
- 問題:
  - `step` が `5` などの不正値でも `switch` が何も処理しない
  - その結果、`currentStepLogic` が前の `Step1_Target` のまま残り、最後の `currentStepLogic?.OnStart()` で再起動されうる
- 症状との一致:
  - `Step1` 完了後にまた `Step1` 敵が湧く、という現象と非常に整合します

## 補助的な原因候補

### 5. `Step1` のスポーン数が `playerCount` ではなく `step1Enemies.Count` 基準
- 根拠:
  - [Step1_Target.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/Step1_Target.cs#L18) `spawner.SpawnTargetsForEachPlayer(step1Enemies.Count, step1Enemies);`
  - 一方 `Step2` は [Step2_Block.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/Step2_Block.cs#L20) で `playerCount` を使っている
- 問題:
  - `TutorialSpawner` の `remain` は渡された第1引数で決まるため、Step ごとに意味がズレている
  - ただし今回の「再挑戦後の Step1 無限ループ」の直接原因としては弱い
- 備考:
  - プレイヤー数と `step1Enemies.Count` がズレたとき、別の進行不具合の火種にはなります

### 6. `StartMainSimulation()` 後に `CurrentStep` も `currentStepLogic` も明示リセットしていない
- 根拠:
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L162-L165)
- 問題:
  - 終了フラグだけ戻して、中身の進行状態は残したままになっている
  - 再挑戦時の状態汚染が起きやすい

## 関連クラスの観点

### 7. `NetworkGameManager` は `GameState.Tutorial` になるたびに `OnTutorialStartServerOnly()` を呼ぶ
- 根拠:
  - [NetworkGameManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/NetworkGameManager.cs#L63)
  - [NetworkGameManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/NetworkGameManager.cs#L73-L75)
  - [NetworkGameManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/NetworkGameManager.cs#L108-L110)
- 問題:
  - `GameState` 側で再度 `Tutorial` に入ると、`TutorialManager` は再スタートする
  - そのとき `CurrentStep` がリセットされていないため、上記不整合が再発する

### 8. `GameStateManager` は再挑戦時に `Tutorial` へ戻すが、チュートリアル進行状態までは面倒を見ていない
- 根拠:
  - [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L175)
  - [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L182)
- 問題:
  - ゲーム状態は初期化されるが、`TutorialManager.CurrentStep` は別管理なので取り残される

## 可能性の優先度
- 高: `CurrentStep` 未リセット
- 高: `OnTutorialStartServerOnly()` が `CurrentStep` を更新せず `StartStep(Step1)` を直接呼ぶ
- 高: `NextStep()` の範囲外進行と `StartStep()` の default ガード不足
- 中: `StartMainSimulation()` 後に `currentStepLogic` を明示クリアしていない
- 低〜中: `Step1_Target` のスポーン数指定が `playerCount` でなく `step1Enemies.Count`

## ひとことで言うと
- 1 回目のチュートリアルは動いている
- 2 回目以降は `TutorialManager` の「開始フラグだけ戻して、進行段階は戻していない」ため、`Step1` を手動で始めても内部状態がずれたままになる
- そのズレが `NextStep()` 後に顕在化して、`Step1` の `OnStart()` が再実行され続けている可能性が高いです

## 修正方針の候補
- `OnTutorialStartServerOnly()` で `CurrentStep.Value = TutorialStep.Step1;` を明示する
- 再挑戦前または `StartMainSimulation()` 後に `CurrentStep.Value = TutorialStep.Step1;` を戻す
- `NextStep()` は `CurrentStep.Value >= TutorialStep.End` のとき進めない
- `StartStep()` に `default` を追加し、不正値なら `Debug.LogError` を出して処理中断する
- 必要なら `currentStepLogic = null;` を終了時に明示する

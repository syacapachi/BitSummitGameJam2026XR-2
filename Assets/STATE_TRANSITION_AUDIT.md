# 状態遷移監査メモ

## 対象
- `GameState`
- `LocalState`
- `GameStateManager`
- `GameStateManager` を呼び出している UI / Tutorial / WorldView 系

## 前提ルール

### LocalState
```text
LanguageSelect -> NetworkConnect -> WorldView -> Playing
LanguageSelect -> WorldView
```

補足:
- `LanguageSelect -> NetworkConnect`: 言語選択後、未接続なら遷移
- `LanguageSelect -> WorldView`: 言語選択後、接続済みなら遷移
- `NetworkConnect -> WorldView`: 接続完了時に遷移
- `WorldView -> Playing`: 全クライアントが `WorldView` にいて、進む操作が行われたときに遷移

### GameState
```text
Initializing -> Tutorial -> Playing -> GameClear/GameOver -> Home
Initializing -> Playing -> GameClear/GameOver -> Home
```

補足:
- `Initializing` は初期化およびチュートリアル分岐判定
- `Tutorial -> Playing` はチュートリアル終了後に必ず遷移
- `GameClear` / `GameOver -> Home` は戻る操作で遷移

## 結論
- `GameState` / `LocalState` の実際の書き込みはほぼ `GameStateManager` に集約されており、外部から `CurrentGameState` を直接壊しているコードは見つかりませんでした。
- ただし、`GameStateManager` 自身と、その呼び出し側に「遷移元チェックがない」箇所があり、仕様どおりの遷移制約は守り切れていません。

## 問題あり

### 1. `WorldView -> Playing` が「全員が WorldView」の条件を確認していない
- 根拠:
  - [WorldViewManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Shota/WorldViewManager.cs#L75) で、最後のページにいると `gameStateManager.OnGameInitialize()` を即時実行
  - [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L104) で、そのまま `LocalState.Playing` と `GameState.Initializing` 以降へ進む
- 問題:
  - 仕様では「全クライアントが `WorldView` にいる時、進むを押したとき」にのみ進むべきですが、現状は 1 クライアントの操作だけで開始できます。
- 影響:
  - まだ説明画面を見ているクライアントがいてもゲーム開始できる
  - ホストだけが先に進める構造になりやすい

### 2. `OnGameStartServerOnly()` が任意状態から `Playing` へ飛べる
- 根拠:
  - [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L119) で `CurrentGameState = GameState.Playing;`
  - 直前状態の検証がない
  - [StartButton.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/StartButton.cs#L96) から呼べる
- 問題:
  - `Home -> Playing`
  - `GameClear -> Playing`
  - `GameOver -> Playing`
  - `Tutorial -> Playing`
  などを無条件で許してしまいます。
- 仕様との差分:
  - 本来は `Initializing -> Playing`、または `Tutorial -> Playing` のみが許可されるべきです。

### 3. `OnBackToHomeServerOnly()` が任意状態から `Home` に戻せる
- 根拠:
  - [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L124) で `CurrentGameState = GameState.Home;`
  - 直前状態の検証がない
  - [StartButton.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/StartButton.cs#L102) から呼べる
- 問題:
  - 仕様では `GameClear` / `GameOver -> Home` が許可経路ですが、実装上は `Playing -> Home` や `Tutorial -> Home` も可能です。

### 4. `Tutorial -> Playing` が `Auto` モードでしか成立しない
- 根拠:
  - [TutorialManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Tutorial/TutorialManager.cs#L162) でチュートリアル終了時に `stateManager.OnTutorialEnd()` を呼ぶ
  - [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L84) では、`gameStartMode == GameStartMode.Auto` のときだけ `Playing` に進む
- 問題:
  - `Button` モードでは、チュートリアル完了後も `GameState.Tutorial` に留まります。
- 仕様との差分:
  - 仕様では `Tutorial -> Playing` は必須遷移です。

### 5. `GameState` の変更時に、ほぼ無条件で `LocalState.Playing` に潰している
- 根拠:
  - [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L61) から [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L67)
- 問題:
  - `newState != Home` ならすべて `SetState(LocalState.Playing)` になります。
  - `Initializing` や `Tutorial` のような中間状態でも、ローカル表示上は即 `Playing` 扱いになります。
- 仕様との差分:
  - 仕様上は `LocalState.Playing` は `WorldView` の後に入る状態ですが、実装上は `GameState` の変化に引きずられてしまっています。
- 備考:
  - これは即バグとは限りませんが、`LocalState` を独立した遷移モデルとして持つ意味がかなり薄くなっています。

### 6. `EnterWorldView()` / `EnterNetworkConnect()` に遷移元チェックがない
- 根拠:
  - [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L135)
  - [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L146)
- 問題:
  - `EnterWorldView()` はどの状態からでも `WorldView` に入れます。
  - `EnterNetworkConnect()` も `IsSpawned` の有無だけで分岐しており、`LanguageSelect` 以外からの遷移制御はありません。
- 備考:
  - 現在の呼び出し元は主に [LanguageSelectManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/Shota/LanguageSelectManager.cs#L9) と [ButtonAction.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/ButtonAction.cs#L152) ですが、メソッド単体としては状態機械のガードがありません。

## 守れている点

### 1. `GameState` の直接書き換えはほぼ `GameStateManager` に集約されている
- 直接 `CurrentGameState = ...` を行っているのは [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs) 内のみでした。
- そのため、修正方針は「呼び出し側を全部直す」より「`GameStateManager` に遷移ガードを入れる」が有効です。

### 2. `Playing -> GameClear / GameOver` には一応ガードがある
- [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L92)
- [GameStateManager.cs](/D:/UNITY/BitSummitGameJam2026XR-2/Assets/Scripts/RikutoScripts/GameStateManager.cs#L98)
- `CurrentGameState != GameState.Playing` のときは遷移しないので、終了状態への誤遷移はある程度抑えられています。

## 改善案

### 優先度高
- `GameStateManager` に「許可された遷移だけを通す」共通関数を作る
- `OnGameStartServerOnly()` は `Initializing` または `Tutorial` からのみ許可する
- `OnBackToHomeServerOnly()` は `GameClear` または `GameOver` からのみ許可する
- `OnTutorialEnd()` は `GameStartMode` に関係なく `Tutorial -> Playing` を成立させる
- `WorldViewManager` で開始前に「全クライアントが `WorldView` にいるか」を確認する

### 優先度中
- `EnterLanguageSelect()` / `EnterNetworkConnect()` / `EnterWorldView()` に遷移元ガードを入れる
- `LocalState` と `GameState` の責務を分ける
  - `LocalState`: タイトル導線 UI
  - `GameState`: 実ゲーム進行
- `HandleGameStateChanged()` で無条件に `LocalState.Playing` へ潰さない

## 修正の最小方針
- `bool CanTransition(GameState from, GameState to)` を `GameStateManager` に追加
- すべての `CurrentGameState = ...` の前で `CanTransition` を通す
- `bool CanTransition(LocalState from, LocalState to)` も追加
- `WorldViewManager` 側に「全員準備完了」判定を追加

## ひとことで言うと
- 状態の書き込み位置は整理されています。
- ただし「どの状態からどの状態へ進んでよいか」の制約はまだ弱く、今の実装は“状態変数を持っている”段階で、“状態機械として厳密に守っている”段階には達していません。

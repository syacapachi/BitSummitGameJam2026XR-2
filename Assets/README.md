# Project Coding Rules

このドキュメントは、本プロジェクトでの **コード規約・設計ルール** をまとめたものです。
チーム開発時の可読性向上・バグ防止・ネットワーク同期の明確化を目的としています。

---

# 1. 命名規則

## ネットワーク参照の命名

Netcodeを使用するため、**どのマシンが参照を持つかを名前で明示する**。

| Prefix   | 意味                     | 例                           |
| -------- | ---------------------- | --------------------------- |
| `local`  | オーナー（ローカルプレイヤー）のみ参照を持つ | `localCamera`, `localInput` |
| `server` | サーバーのみ参照を持つ            | `serverGameManager`         |
| `all`    | 全クライアントが参照可能           | `allPlayerList`             |

### 例

```csharp
// Local only
private Camera localPlayerCamera;

// Server only
private GameManager serverGameManager;

// All clients
private Dictionary<ulong, Player> allPlayers;
```

---

## 変数

| 種類             | 命名         |
| -------------- | ---------- |
| private        | camelCase  |
| public         | PascalCase |
| SerializeField | camelCase  |

### 例

```csharp
[SerializeField]
private Rigidbody body;

private int playerScore;

public int PlayerID { get; private set; }
```

---

## クラス

クラス名は **PascalCase**

```csharp
PlayerController
ItemManager
AttachableRigidBody
```

---

## メソッド

メソッド名は **PascalCase**

```csharp
Initialize()
AttachItem()
DetachItem()
CalculateScore()
```

---

# 2. Netcode設計ルール

## ServerRpc

ServerRpcは **状態変更のみ行う**

```csharp
[Rpc(SendTo.Server)]
private void AttachRpc(NetworkObjectReference node)
```

---

## Client側処理

Clientは **入力処理のみ**

```csharp
public void Focus()
{
    AttachRpc(nodeRef);
}
```

---

## NetworkObject参照

RPCでGameObject参照を渡す場合は **NetworkObjectReferenceを使用**

```csharp
NetworkObjectReference
NetworkBehaviourReference
```

---

# 3. Nullチェック

Network同期では **nullが頻繁に発生するため必ずチェックする**

```csharp
if(node == null)
    return;
```

---

# 4. Inspector参照

SerializeFieldの参照は **Awakeで補完する**

```csharp
void Awake()
{
    if(body == null)
        body = GetComponent<Rigidbody>();
}
```

---

# 5. フォルダ構成

推奨フォルダ構成

```
Assets
 ├ Script
 │   ├ Core
 │   ├ Network
 │   ├ Player
 │   ├ XR
 │   └ UI
 ├ Prefab
 ├ Material
 ├ Scene
 └ Resource
```

---

# 6. XR関連ルール

XRオブジェクトは以下の階層構造を基本とする

```
Player
 └ XROrigin
     ├ Camera
     ├ LeftHand
     └ RightHand
```

---

# 7. 重要ルール

以下は禁止

* Server専用データをClientが直接参照
* Client専用データをServerが参照
* NetworkObjectを直接送信

必ず

```
NetworkObjectReference
NetworkVariable
RPC
```

を使用する。

---

# 8. 目的

このルールの目的

* マルチプレイバグの削減
* コード可読性向上
* 新規メンバーの理解促進
* VR + Netcode の安定化

---

# 参考

Unity Netcode for GameObjects
XR Interaction Toolkit

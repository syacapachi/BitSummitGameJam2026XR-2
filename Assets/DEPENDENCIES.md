# BitSummitGameJam2026XR-2 依存関係一覧

## 概要

- Unity Editor: `6000.0.61f1`
- 主な技術スタック:
  - XR / Meta Quest 系
  - Netcode for GameObjects
  - Unity Transport
  - URP
  - Input System
- 調査元:
  - `Packages/manifest.json`
  - `Packages/packages-lock.json`
  - `Assets` 配下の自作スクリプト

## パッケージ依存関係

### XR / Meta 系

| パッケージ | バージョン | 用途 |
| --- | --- | --- |
| `com.meta.xr.sdk.all` | `85.0.0` | Meta XR SDK 一式 |
| `com.unity.xr.androidxr-openxr` | `1.0.2` | Android XR / OpenXR 連携 |
| `com.unity.xr.arfoundation` | `6.3.3` | AR / XR 基盤 |
| `com.unity.xr.core-utils` | `2.5.3` | XR 共通ユーティリティ |
| `com.unity.xr.hands` | `1.7.3` | ハンドトラッキング |
| `com.unity.xr.interaction.toolkit` | `3.3.1` | XR インタラクション |
| `com.unity.xr.management` | `4.5.4` | XR ローダー管理 |
| `com.unity.xr.meta-openxr` | `2.4.0` | Meta OpenXR 拡張 |
| `com.unity.xr.openxr` | `1.15.1` | OpenXR ランタイム連携 |

### マルチプレイ / 通信系

| パッケージ | バージョン | 用途 |
| --- | --- | --- |
| `com.unity.netcode.gameobjects` | `2.9.2` | メインのネットワーク同期 |
| `com.unity.transport` | `2.6.0` | 通信トランスポート |
| `com.community.netcode.extensions` | Git URL | NGO 拡張 |
| `com.unity.multiplayer.center` | `1.0.0` | Multiplayer 関連ツール |
| `com.veriorpies.parrelsync` | Git URL | ローカル複数起動検証 |

### 描画 / 入力 / ゲームプレイ補助

| パッケージ | バージョン | 用途 |
| --- | --- | --- |
| `com.unity.render-pipelines.universal` | `17.0.4` | URP |
| `com.unity.inputsystem` | `1.15.0` | Input System |
| `com.unity.ai.navigation` | `2.0.11` | NavMesh / AI Navigation |
| `com.unity.timeline` | `1.8.9` | Timeline |
| `com.unity.2d.sprite` | `1.0.0` | 2D Sprite サポート |

### 開発支援 / Editor 系

| パッケージ | バージョン | 用途 |
| --- | --- | --- |
| `com.unity.feature.development` | `1.0.2` | テスト、コードカバレッジ、プロファイラ補助 |
| `com.unity.ide.rider` | `3.0.39` | Rider 連携 |
| `com.unity.learn.iet-framework` | `5.0.2` | Unity Learn 用フレームワーク |

## lock ファイルから見える主な間接依存

`packages-lock.json` では、主に次の間接依存が解決されています。

- `com.meta.xr.sdk.core`
- `com.meta.xr.sdk.audio`
- `com.meta.xr.sdk.voice`
- `com.meta.xr.sdk.haptics`
- `com.meta.xr.sdk.platform`
- `com.meta.xr.sdk.interaction`
- `com.meta.xr.sdk.interaction.ovr`
- `com.meta.xr.mrutilitykit`
- `com.unity.ugui`
- `com.unity.textmeshpro`
- `com.unity.burst`
- `com.unity.collections`
- `com.unity.mathematics`
- `com.unity.shadergraph`
- `com.unity.test-framework`
- `com.unity.testtools.codecoverage`

そのほか、`com.unity.modules.*` の組み込みモジュールも多数有効です。

## 実際に使われている依存の傾向

コードとアセットから、次の依存は実利用されていることを確認しました。

- Netcode for GameObjects
  - `Assets/Script/ButtonAction.cs`
  - `Assets/Script/ManagerLocator.cs`
  - `Assets/Script/NetworkObject/*`
  - `Assets/Script/NetworkPlayer/*`
  - `Assets/Scenes/NSystemScene/*`
- Unity Transport
  - `Assets/Script/ButtonAction.cs`
  - `Assets/Script/Discover/MyNetworkDiscover.cs`
  - `Assets/Script/NetworkObject/ServerAction.cs`
- Input System
  - `Assets/BugMove/CameraMove.cs`
  - `Assets/Script/LocalPlayer/*`
  - `Assets/Scenes/SystemScene/PlayerInputActions.cs`
- XR / Meta
  - `Assets/Script/Controller/LockItemHold.cs`
  - `Assets/Script/LocalPlayer/*`
  - `Assets/Script/Syncronize/*`
  - `Assets/Scenes/NSystemScene/StatusViewer.cs`
- UI
  - `TextMeshPro`
  - `UnityEngine.UI`

## 改善ポイント

### 1. Git 依存をタグまたはコミットで固定する

現在、以下の Git パッケージは URL のみで指定されています。

- `com.community.netcode.extensions`
- `com.veriorpies.parrelsync`

`packages-lock.json` にハッシュは残りますが、`manifest.json` 側も `#tag` か `#commit` まで固定した方が、環境再現性が上がります。

### 2. Netcode Extensions と NGO 2.9.2 の互換性確認

`packages-lock.json` 上では、

- `com.community.netcode.extensions` が `com.unity.netcode.gameobjects: 1.0.2` 前提
- プロジェクト本体は `com.unity.netcode.gameobjects: 2.9.2`

となっており、依存の前提にズレがあります。現状動いていても、将来の不具合要因になりやすいです。

### 3. XR 系パッケージのバージョンずれを減らす

例:

- `manifest.json` では `com.unity.xr.arfoundation: 6.3.3`
- `packages-lock.json` では `com.unity.xr.arfoundation: 6.4.0`

XR 系は組み合わせ相性が出やすいため、検証済みのバージョンセットとしてそろえる方が安全です。

### 4. `Assets/Samples` の整理

以下のサンプルがインポートされています。

- `Assets/Samples/XR Hands/...`
- `Assets/Samples/XR Interaction Toolkit/...`

これらは asmdef、シーン、プレハブ、スクリプト、テクスチャを多く追加します。製品コードで参照していないなら削減候補です。

### 5. 開発専用パッケージの扱いを明確にする

以下は本番実行に必須でない可能性があります。

- `com.unity.feature.development`
- `com.unity.learn.iet-framework`
- `com.unity.multiplayer.center`
- `com.veriorpies.parrelsync`

開発用として残すなら、その理由を README か運用メモに書いておくとチーム開発しやすくなります。

### 6. スクリプト内の Editor / NUnit 参照を整理する

いくつかの通常スクリプトに `UnityEditor` や `NUnit.Framework` の参照が見えます。

例:

- `Assets/Script/ButtonAction.cs`
- `Assets/Script/ChatBoard/CanvasManager.cs`
- `Assets/Script/Manager/CheckPointManager.cs`
- `Assets/Script/NetworkPlayer/PlayerManager.cs`
- `Assets/Script/Syncronize/Syncronize.cs`

ランタイムコードに混ざっていると、ビルド対象やアセンブリ分離の観点で将来的に整理が必要になる可能性があります。

## 優先度付きの整理順

1. `com.community.netcode.extensions` と NGO `2.9.2` の互換性を確認する
2. XR パッケージ群を検証済みの組み合わせにそろえる
3. `Assets/Samples` の参照有無を監査して不要なら削除する
4. Git パッケージをタグまたはコミットで固定する
5. ランタイムスクリプトに混ざった `UnityEditor` / `NUnit` 参照を整理する

## スクリプトごとの依存関係

このセクションでは、`Assets` 配下の自作スクリプトを対象に、各スクリプトの依存を整理しています。

除外対象:

- `Assets/Samples/*`
- `Assets/TutorialInfo/*`
- `Assets/UnityChan/*`
- `Assets/VRTemplateAssets/*`
- `Assets/Supercyan Character Pack Zombie Sample/*`
- `Assets/Gunasset/*`

`主な依存` は、`using`、継承元、実装インターフェースから見た主要カテゴリです。

## BugMove/CameraMove.cs
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/BugMove/CameraMove.cs` | class | MonoBehaviour | Input System |

## Editor/EnableIfDrawer.cs
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Editor/EnableIfDrawer.cs` | class | PropertyDrawer | Unity Editor |

## Editor/EnableIfEnumDrawer.cs
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Editor/EnableIfEnumDrawer.cs` | class | PropertyDrawer | Unity Editor |

## Editor/NetCodeButtonEditor.cs
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Editor/NetCodeButtonEditor.cs` | class | NetcodeEditorBase<NetworkBehaviour> | Netcode, Unity Editor |

## Editor/OnInspectorButtonEditor.cs
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Editor/OnInspectorButtonEditor.cs` | class | Editor | Unity Editor |

## Editor/SerializeReferenceViewDrawer.cs
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Editor/SerializeReferenceViewDrawer.cs` | class | PropertyDrawer | Unity Editor |

## Editor/ShowInspectorButtonDrawer.cs
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Editor/ShowInspectorButtonDrawer.cs` | class | PropertyDrawer | Unity Editor |

## Editor/SingleFlagOnlyDrawer.cs
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Editor/SingleFlagOnlyDrawer.cs` | class | PropertyDrawer | Unity Editor |

## Scenes/NSystemScene
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Scenes/NSystemScene/AmmoUI.cs` | class | MonoBehaviour, IProgressUI, ICountDownUI | TextMeshPro, uGUI |
| `Assets/Scenes/NSystemScene/Audio/GameAudioManager.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/NSystemScene/Audio/MarkerAudioController.cs` | class | NetworkBehaviour | Netcode, Input System |
| `Assets/Scenes/NSystemScene/Audio/NBulletImpactAudio.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/NSystemScene/Audio/NEnemyDespawnAudio.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/Audio/NEnemyLoopAudio.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/Audio/NGunAudioObserver.cs` | class | NetworkBehaviour, IShotSound, IReloadSound | Netcode |
| `Assets/Scenes/NSystemScene/Audio/NPhaseAudioObserver.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/NSystemScene/Audio/ProtectAreaAudioTrigger.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/NSystemScene/Crystal.cs` | class | MonoBehaviour, IDamageReciever | UnityEngine / System |
| `Assets/Scenes/NSystemScene/CrystalHPUI.cs` | class | MonoBehaviour | TextMeshPro, uGUI |
| `Assets/Scenes/NSystemScene/EffectManager/EnemyFxRule.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/NSystemScene/EffectManager/NBulletFxState.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/NSystemScene/EffectManager/NEnemyDeathFxEmitter.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/EffectManager/NEnemyImpactFxReceiver.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/EffectManager/NetFxSpawnUtility.cs` |  | - | UnityEngine / System |
| `Assets/Scenes/NSystemScene/EffectManager/ProtectAreaEnemyFxMarker.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/NSystemScene/EnemyBullet.cs` | class | BulletBaseController | Netcode |
| `Assets/Scenes/NSystemScene/EnemyMove.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/EnemyResultRow.cs` | class | MonoBehaviour | TextMeshPro, uGUI |
| `Assets/Scenes/NSystemScene/EnemyShoot.cs` | class | GunController | Netcode |
| `Assets/Scenes/NSystemScene/GameManagaerSpawner.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/HandLaser.cs` | class | MonoBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/NBullet.cs` | class | BulletBaseController | Netcode, Object Pool |
| `Assets/Scenes/NSystemScene/NEnemy.cs` | class | NetworkBehaviour, IDamageReciever, IEnemy | Netcode, TextMeshPro, uGUI |
| `Assets/Scenes/NSystemScene/NEnemyBullet.cs` | class | BulletBaseController | Netcode |
| `Assets/Scenes/NSystemScene/NEnemyShoot.cs` | class | GunController | Netcode |
| `Assets/Scenes/NSystemScene/NEnemySpawner.cs` | class | NetworkBehaviour, IEnemyBrokenReciever | Netcode |
| `Assets/Scenes/NSystemScene/NGameManager.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/NGun.cs` | class | GunController | UnityEngine / System |
| `Assets/Scenes/NSystemScene/NMaker.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/NSystemScene/PhaseBarUI.cs` | class | MonoBehaviour | uGUI |
| `Assets/Scenes/NSystemScene/PhaseManager.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/PhaseUI.cs` | class | MonoBehaviour | TextMeshPro |
| `Assets/Scenes/NSystemScene/PlayerEffects.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/NSystemScene/PlayerResultData.cs` | struct | INetworkSerializable, IEquatable<PlayerResultData> | Netcode |
| `Assets/Scenes/NSystemScene/PlayerStats.cs` | class | MonoBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/ResultUI.cs` | class | MonoBehaviour | Netcode, TextMeshPro |
| `Assets/Scenes/NSystemScene/RoleButton.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/ScoreManager.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/ScoreUI.cs` | class | MonoBehaviour | TextMeshPro |
| `Assets/Scenes/NSystemScene/StartButton.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Scenes/NSystemScene/StatusViewer.cs` | class | NetworkBehaviour | Netcode, XR / Meta |

## Scenes/SampleScene
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Scenes/SampleScene/Controllertest.cs` | class | MonoBehaviour | Input System |

## Scenes/SystemScene
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Scenes/SystemScene/Bullet.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/SystemScene/Enemy.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/SystemScene/EnemySpawner.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/SystemScene/GameManager.cs` | class | MonoBehaviour | TextMeshPro |
| `Assets/Scenes/SystemScene/Gun.cs` | class | MonoBehaviour | Input System |
| `Assets/Scenes/SystemScene/Maker.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Scenes/SystemScene/PlayerInputActions.cs` |  | - | Input System |
| `Assets/Scenes/SystemScene/PlyaerMove.cs` | class | MonoBehaviour | Input System |

## Scenes/testScene
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Scenes/testScene/Cube/BombAction.cs` | class | NetworkBehaviour, IDamageSender | Netcode |

## Script/Attribute
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/Attribute/EnableIfAttribute.cs` | class | PropertyAttribute | UnityEngine / System |
| `Assets/Script/Attribute/EnableIfEnumAttribute.cs` | class | PropertyAttribute | UnityEngine / System |
| `Assets/Script/Attribute/OnInspectorAttribute.cs` | class | PropertyAttribute | UnityEngine / System |
| `Assets/Script/Attribute/SerialRefrenceViewAttribute.cs` | class | PropertyAttribute | Unity Editor |
| `Assets/Script/Attribute/ShowInspectorAttribute.cs` | class | PropertyAttribute | UnityEngine / System |
| `Assets/Script/Attribute/SingleFlagOnlyAttribute.cs` | class | PropertyAttribute | UnityEngine / System |

## Script/ButtonAction.cs
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/ButtonAction.cs` | class | MonoBehaviour | Netcode, Unity Transport, TextMeshPro, uGUI, Unity Editor, NUnit |

## Script/ChatBoard
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/ChatBoard/CanvasCamera.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Script/ChatBoard/CanvasManager.cs` | class | MonoBehaviour | NUnit |
| `Assets/Script/ChatBoard/ChatMessage.cs` | class | INetworkSerializable | Netcode |
| `Assets/Script/ChatBoard/ChatSystem.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Script/ChatBoard/UI_Board.cs` | class | MonoBehaviour | Netcode, TextMeshPro, uGUI |

## Script/Controller
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/Controller/BulletBaseController.cs` | class | NetworkBehaviour, IDamageSender | Netcode |
| `Assets/Script/Controller/GunController.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Script/Controller/LockItemHold.cs` | class | MonoBehaviour | XR / Meta |
| `Assets/Script/Controller/MarkerController.cs` | class | NetworkBehaviour | Netcode |

## Script/Discover
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/Discover/MyNetworkDiscover.cs` | class | MonoBehaviour | Netcode, Unity Transport |
| `Assets/Script/Discover/myNetworkDiscoveryHud.cs` | class | MonoBehaviour | Netcode, Unity Transport, Unity Editor |
| `Assets/Script/Discover/WifiIPV4Info.cs` | class | - | uGUI |

## Script/Enemy
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/Enemy/EnemyDeathReciver.cs` | class | MonoBehaviour, IEnemyBrokenReciever | UnityEngine / System |
| `Assets/Script/Enemy/LookStateChange.cs` | class | NetworkBehaviour | Netcode |

## Script/EventAction
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/EventAction/GameEventHandlerBase.cs` | class | MonoBehaviour, IInvokable | UnityEngine / System |
| `Assets/Script/EventAction/GameEventSO.cs` | class | ScriptableObject, IResisterable<IInvokable>, IInvokable | UnityEngine / System |
| `Assets/Script/EventAction/NetCodeGameEventHandlerBase.cs` | class | NetworkBehaviour, IInvokable | Netcode |

## Script/Interface
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/Interface/ICountDownUI.cs` | interface | - | UnityEngine / System |
| `Assets/Script/Interface/IDamageReciever.cs` | interface | - | Netcode |
| `Assets/Script/Interface/IDamageSender.cs` | interface | - | UnityEngine / System |
| `Assets/Script/Interface/IEnemy.cs` | interface | - | Netcode |
| `Assets/Script/Interface/IEnemyBrokenReciever.cs` | interface | - | UnityEngine / System |
| `Assets/Script/Interface/IInvokable.cs` | interface | - | UnityEngine / System |
| `Assets/Script/Interface/IProgressUI.cs` | interface | - | UnityEngine / System |
| `Assets/Script/Interface/IRegisterable.cs` | interface | - | UnityEngine / System |
| `Assets/Script/Interface/IReloadSound.cs` | interface | - | UnityEngine / System |
| `Assets/Script/Interface/IShotSound.cs` | interface | - | UnityEngine / System |
| `Assets/Script/Interface/ITutorialStart.cs` | interface | - | UnityEngine / System |

## Script/JobSetting
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/JobSetting/JobSettingSO.cs` | class | ScriptableObject | UnityEngine / System |
| `Assets/Script/JobSetting/PlayerJobManager.cs` | class | MonoBehaviour | UnityEngine / System |

## Script/LocalPlayer
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/LocalPlayer/InputReciever.cs` | class | MonoBehaviour | Input System |
| `Assets/Script/LocalPlayer/JobCamera.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Script/LocalPlayer/LocalCameraSetting.cs` | class | MonoBehaviour | Input System, XR / Meta |
| `Assets/Script/LocalPlayer/LocalCharactorControll.cs` | class | MonoBehaviour | Netcode, Input System |
| `Assets/Script/LocalPlayer/LocalPlayerRoot.cs` | class | MonoBehaviour | Netcode, Input System, XR / Meta, Unity Editor |

## Script/Manager
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/Manager/CheckPointManager.cs` | class | MonoBehaviour | NUnit |

## Script/ManagerLocator.cs
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/ManagerLocator.cs` | class | MonoBehaviour | Netcode |

## Script/NetworkObject
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/NetworkObject/NetworkObjectPool.cs` | class | NetworkBehaviour | Netcode, Object Pool |
| `Assets/Script/NetworkObject/PrefabHandler.cs` | class | INetworkPrefabInstanceHandler | Netcode |
| `Assets/Script/NetworkObject/ServerAction.cs` | class | NetworkBehaviour | Netcode, Unity Transport |

## Script/NetworkPlayer
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/NetworkPlayer/AnimationSuncro.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Script/NetworkPlayer/NetworkPlayerRoot.cs` | class | NetworkBehaviour | Netcode, Input System, XR / Meta, Unity Editor |
| `Assets/Script/NetworkPlayer/PlayerHealth.cs` | class | NetworkBehaviour, IDamageReciever | Netcode |
| `Assets/Script/NetworkPlayer/PlayerItemControll.cs` | class | NetworkBehaviour | Netcode, Unity Editor |
| `Assets/Script/NetworkPlayer/PlayerManager.cs` | class | MonoBehaviour | Netcode, Input System, NUnit |
| `Assets/Script/NetworkPlayer/PlayerMeshChange.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Script/NetworkPlayer/PlayerPropaty.cs` | class | MonoBehaviour | XR / Meta |
| `Assets/Script/NetworkPlayer/PlayerSpawner.cs` | class | NetworkBehaviour | Netcode |
| `Assets/Script/NetworkPlayer/SampleScript.cs` | class | MonoBehaviour | UnityEngine / System |
| `Assets/Script/NetworkPlayer/SyncroPropaty.cs` | class | NetworkBehaviour | Netcode |

## Script/ObjectPool
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/ObjectPool/LocalObjectPoolManager.cs` | class | MonoBehaviour | Object Pool |

## Script/Ranking
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/Ranking/RankingData.cs` | class | - | UnityEngine / System |
| `Assets/Script/Ranking/RankingManager.cs` | class | MonoBehaviour | UnityEngine / System |

## Script/RespawnField.cs
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/RespawnField.cs` | class | MonoBehaviour | UnityEngine / System |

## Script/SO
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/SO/EnemySO.cs` | class | ScriptableObject | UnityEngine / System |
| `Assets/Script/SO/EnemyWeaponSettingSO.cs` | class | WeaponSettingsSO | UnityEngine / System |
| `Assets/Script/SO/PhaseSO.cs` | class | ScriptableObject | UnityEngine / System |
| `Assets/Script/SO/WeaponSettingSO.cs` | class | ScriptableObject | UnityEngine / System |

## Script/Syncronize
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/Syncronize/AttachableRigidBody.cs` | class | AttachableBehaviour | Netcode, XR / Meta |
| `Assets/Script/Syncronize/MeshController.cs` | class | NetworkBehaviour | Netcode, Netcode Extensions |
| `Assets/Script/Syncronize/Syncronize.cs` | class | NetworkBehaviour | Netcode, XR / Meta, NUnit |

## Script/Tutorial
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/Tutorial/TutorialManager.cs` | class | MonoBehaviour, IEnemyBrokenReciever, ITutorialStart | uGUI |

## Script/utils
| スクリプト | 種別 | 継承 / 実装 | 主な依存 |
| --- | --- | --- | --- |
| `Assets/Script/utils/NetworkEntry.cs` | struct | INetworkSerializable, IEquatable<NetworkEntry> | Netcode |

## 補足

- このファイルは、Unity Package 依存と自作スクリプトの依存を中心にまとめています。
- `Assets` に入っている配布アセットやサンプルアセット全体のライセンス棚卸しは別途行うのが安全です。

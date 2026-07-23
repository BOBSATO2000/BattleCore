# BattleCore（ルート）

## IGameEvent
ゲーム内イベントのマーカーインターフェース。SimulationContext.EventQueueに積まれUIが参照する。

---

# BattleCore.Events

## BattleLogEvent : IGameEvent
戦闘結果の詳細ログイベント。BattleResolverが生成しBattleSystemがEventQueueに積む。

| メンバー | 種別 | 説明 |
|---|---|---|
| `WinnerName` | property | 勝者の名前 |
| `LoserName` | property | 敗者の名前 |
| `WinnerLosses` | property | 勝者が失った兵力数 |
| `LoserLosses` | property | 敗者が失った兵力数 |
| `Terrain` | property | 戦闘発生地の地形 |
| `Weather` | property | 戦闘時の天気 |
| `TerrainBonus` | property | 地形ボーナスが適用されたか |
| `RainPenalty` | property | 雨ペナルティが適用されたか |
| `CastleBonus` | property | 城ボーナスが適用されたか |
| `GrowthDetail` | property | 武将成長の詳細。例: "謙信 統率+1(156)" |

---

## BetrayalEvent : IGameEvent
武将の離反イベント。LoyaltySystemが裏切り判定を行った際に発生する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `OfficerId` | property | 離反した武将のID |
| `FromClanId` | property | 離反元の勢力ID |
| `BetrayalScore` | property | 離反時の裏切りスコア（デバッグ・ログ用） |

---

## CastleCapturedEvent : IGameEvent
城が占領されたときに発火するイベント。

| メンバー | 種別 | 説明 |
|---|---|---|
| `CastleId` | property | 占領された城のID |
| `CastleName` | property | 占領された城の名前 |
| `NewOwnerClanId` | property | 新たに占領した勢力ID |

---

## DecisionExplanationEvent : IGameEvent
AI判断の説明イベント。ClanDecisionSystemが発火しUIがデバッグ表示に使用する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `OfficerId` | property | 判断を行った武将のID |
| `OfficerName` | property | 判断を行った武将の名前 |
| `Summary` | property | 判断の要約文 |
| `Factors` | property | 判断に影響した要因のリスト |

---

## GameOverEvent : IGameEvent
ゲーム終了イベント。VictorySystemが発火する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `WinnerClanId` | property | 勝利した勢力ID。nullの場合は引き分け |
| `Reason` | property | ゲーム終了理由のメッセージ |

---

## MovementEvent : IGameEvent
軍が目的地に到着したイベント。MovementSystemが発生させる。

| メンバー | 種別 | 説明 |
|---|---|---|
| `ArmyId` | property | 到着した軍のID |
| `OfficerName` | property | 到着した軍の指揮官名 |
| `HexId` | property | 到着先のHexID |

---

## OfficerRefusedOrderEvent : IGameEvent
武将が命令を拒否したイベント。

| メンバー | 種別 | 説明 |
|---|---|---|
| `OfficerId` | property | 拒否した武将のID |
| `OfficerName` | property | 拒否した武将の名前 |
| `Reason` | property | 拒否の理由 |

---

## OfficerRequestedRetreatEvent : IGameEvent
武将が撤退を進言したイベント。

| メンバー | 種別 | 説明 |
|---|---|---|
| `OfficerId` | property | 進言した武将のID |
| `OfficerName` | property | 進言した武将の名前 |
| `Soldiers` | property | 進言時の兵力 |

---

## RecruitEvent : IGameEvent
武将の仕官イベント。RecruitmentSystemが無所属武将を勢力へ登用した際に発生する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `OfficerId` | property | 仕官した武将のID |
| `ToClanId` | property | 仕官先の勢力ID |

---

## ScenarioEvent : IGameEvent
シナリオトリガーが発火したときのイベント。UIログに表示する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `TriggerId` | property | トリガーID |
| `Message` | property | 表示メッセージ |

---

## SupplyEvent : IGameEvent
兵力補充イベント。SupplySystemが閾値以上の補充を行った際に発生する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `ArmyId` | property | 補充を受けた軍のID |
| `OfficerName` | property | 補充を受けた軍の指揮官名 |
| `Amount` | property | 補充量 |
| `NewSoldiers` | property | 補充後の兵力 |

---

# BattleCore.Battle

## Battle
1件の戦闘情報を表すクラス。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Attacker` | property | 攻撃側の軍 |
| `Defender` | property | 防御側の軍 |

---

## BattleFinder
同じHexに存在する異なる勢力の軍を探し戦闘ペアを生成する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Find(WorldState)` | method | WorldStateを走査し戦闘が発生する全ペアを返す |

---

## BattleResolver
1件の戦闘を解決し兵力更新・武将成長・イベント生成を行う。

| メンバー | 種別 | 説明 |
|---|---|---|
| `TrustBonusThreshold` | const | Trustボーナスを適用する信頼度の閾値（=60） |
| `Resolve(Battle, WorldState)` | method | 戦闘を解決しBattleLogEventを返す。地形・天気・城・Trust・武将成長を全て適用する |

---

# BattleCore.Save

## SaveData
セーブデータ本体。メタ情報＋シミュレーション状態の完全スナップショット。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Metadata` | property | メタ情報（バージョン・日時・シナリオ・ターン・フェーズ） |
| `Time` | property | ゲーム内時間のスナップショット |
| `CurrentPhase` | property | 現在のターンフェーズ |
| `World` | property | WorldStateのスナップショット |

---

## SaveMetadata
セーブデータのメタ情報。セーブ一覧表示・バージョン互換チェックに使用する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Version` | property | セーブデータのフォーマットバージョン |
| `EngineVersion` | property | セーブ時のエンジンバージョン |
| `SavedAt` | property | セーブ日時（UTC） |
| `ScenarioId` | property | シナリオID（例: "sengoku1560"） |
| `Turn` | property | セーブ時のターン数 |
| `Phase` | property | セーブ時のフェーズ名 |

---

## SaveSystem
セーブ／ロードの責務を持つ静的クラス。

| メンバー | 種別 | 説明 |
|---|---|---|
| `CurrentVersion` | const | 現在のセーブデータフォーマットバージョン |
| `EngineVersion` | const | エンジンバージョン |
| `Save(SimulationContext, string, string)` | static method | 現在のシミュレーション状態をファイルに保存する |
| `Load(string)` | static method | ファイルからセーブデータを読み込みSimulationContextを復元する |
| `LoadMetadata(string)` | static method | メタデータのみ読み込む（セーブ一覧表示用） |

---

# BattleCore.Scenario

## ScenarioLoader
JSONシナリオファイルを読み込みWorldStateを構築する静的クラス。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Load(string)` | static method | JSONファイルを読み込みWorldState・タイトル・イベントトリガーリストを返す |

---

## ScenarioData
シナリオファイル全体のルートDTO。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Title` | property | シナリオタイトル |
| `StartYear` | property | 開始年 |
| `Map` | property | マップHexのリスト |
| `Clans` | property | 勢力のリスト |
| `Officers` | property | 武将のリスト |
| `Memberships` | property | 武将と勢力の所属関係リスト |
| `Armies` | property | 軍隊のリスト |
| `Relationships` | property | 武将間の関係値リスト |
| `Alliances` | property | 初期同盟リスト |
| `Castles` | property | 城・拠点リスト |
| `EventTriggers` | property | シナリオイベントトリガーリスト |

---

## EventTriggerData
イベントトリガーのDTO。条件が全て満たされたとき一度だけ発火する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Id` | property | トリガーID |
| `MinTick` | property | 指定Tick以降に評価する |
| `OfficerId` | property | 対象武将ID |
| `MinDislike` | property | 対象武将のDislike合計がこの値以上 |
| `MaxLoyalty` | property | 対象武将のLoyaltyがこの値以下 |
| `AdjacentToOfficerId` | property | 指定武将の軍がこのOfficerIDの軍と隣接Hexにいる |
| `Message` | property | イベントログに表示するメッセージ |

---

# BattleCore.Player

## IPlayerController
プレイヤー入力から命令を生成する層のインターフェース。

| メンバー | 種別 | 説明 |
|---|---|---|
| `EnqueueCommand(ICommand)` | method | プレイヤーの命令を内部キューに積む |
| `FlushTo(CommandQueue)` | method | 内部キューに積まれた命令をCommandQueueへ移す |

---

## PlayerController : IPlayerController
ローカルプレイヤー用の命令入口。

| メンバー | 種別 | 説明 |
|---|---|---|
| `PendingCount` | property | 未送信の命令数 |
| `EnqueueCommand(ICommand)` | method | プレイヤーの命令を内部キューに積む |
| `FlushTo(CommandQueue)` | method | 内部キューの命令を全てCommandQueueへ移す |

---

## LocalPlayerController : IPlayerController
WinForms UIからのプレイヤー入力を管理するIPlayerController実装。

| メンバー | 種別 | 説明 |
|---|---|---|
| `PendingCommands` | property | 現在キューに積まれている命令（読み取り専用） |
| `EnqueueCommand(ICommand)` | method | 命令を内部キューに積む |
| `FlushTo(CommandQueue)` | method | 内部キューの命令を全てCommandQueueへ移す |
| `CancelCommand(int)` | method | 指定部隊の未実行命令をキューから削除する |

---

## PlayerSession
プレイヤーのセッション情報。操作中の勢力IDを保持する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `PlayerClanId` | property | プレイヤーが操作する勢力のID |
| `OwnsClan(int)` | method | 指定した勢力IDがプレイヤーの勢力かどうかを返す |

---

# BattleCore.Debug

## DebugPanelBuilder
選択部隊のデバッグ情報を構築する静的クラス。UI非依存。

| メンバー | 種別 | 説明 |
|---|---|---|
| `DebugLine` | record | デバッグパネルの1行。表示テキストと色情報を保持する |
| `DebugColor` | enum | デバッグパネルの行色。Normal/Header/Info/Good/Warn/Dim/Path/AI |
| `Build(int, WorldState, SimulationContext, ...)` | static method | 指定部隊のデバッグ情報行リストを構築する。部隊情報・AI判断・経路（A*）・Vision・CommandQueueを含む |

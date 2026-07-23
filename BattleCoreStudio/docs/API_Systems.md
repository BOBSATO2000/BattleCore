# BattleCore.Systems

## BattleSystem : ISimulationSystem
戦闘システム。BattleFinderで戦闘ペアを探し、BattleResolverで解決する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Update(SimulationContext)` | method | 同Hexにいる異なる勢力の軍を戦闘させ、BattleLogEventをEventQueueに積む |

---

## CastleSystem : ISimulationSystem
城システム。毎Tick占領判定と兵力補充を処理する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Update(SimulationContext)` | method | 城のHexに敵軍のみいる場合は占領。占領勢力の軍がいる場合は補充 |

---

## ClanDecisionSystem : ISimulationSystem
勢力単位の戦略決定システム。IClanStrategy → OfficerDecisionの2層構造。

| メンバー | 種別 | 説明 |
|---|---|---|
| `ClanDecisionSystem(IClanStrategy, OfficerDecision?)` | constructor | 戦略とOfficerDecisionを注入する |
| `Update(SimulationContext)` | method | 各勢力のAI命令を生成しCommandQueueに積む。武将イベントはEventQueueに積む |

---

## CommandExecutionSystem : ISimulationSystem
命令実行システム。CommandQueueに積まれた命令を全てDequeueして実行する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Update(SimulationContext)` | method | CommandQueueを全て消化し各命令を実行する |

---

## DecisionSystem : ISimulationSystem
AI判断システム。各ArmyにIArmyDecision.Decide()を呼び命令をCommandQueueに積む。

| メンバー | 種別 | 説明 |
|---|---|---|
| `DecisionSystem(IArmyDecision)` | constructor | 使用するAI実装を注入する |
| `Update(SimulationContext)` | method | 全ArmyのAI判断を実行しCommandQueueに積む |

---

## DiplomacySystem : ISimulationSystem
外交システム。同盟の期限管理とAI自動同盟締結を処理する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `AutoAllianceInterval` | property | AI自動同盟を試みる間隔（Tick） |
| `AutoAllianceDuration` | property | AI自動同盟の期間（Tick） |
| `Update(SimulationContext)` | method | 同盟期限チェックとAI自動同盟評価を実行する |

---

## EventTriggerSystem : ISimulationSystem
シナリオイベントトリガーを毎Step評価し、条件を満たしたら一度だけ発火する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `EventTriggerSystem(IEnumerable<EventTriggerData>)` | constructor | トリガーリストを注入する |
| `Update(SimulationContext)` | method | 全トリガーの条件を評価し、満たしたものをEventQueueに積む |

---

## LoyaltySystem : ISimulationSystem
忠誠変動・裏切り判定システム。

**忠誠変動ルール:**
- +WinLoyaltyBonus: 所属勢力のArmyが兵力を保っている
- +SpringBonus: 季節が春
- -LossLoyaltyPenalty: 所属勢力のArmyが全滅している

**裏切りスコア:** Ambition - Loyalty/2 - Membership.Loyalty/2 + Dislike

| メンバー | 種別 | 説明 |
|---|---|---|
| `BetrayalThreshold` | property | 裏切り判定の閾値（デフォルト:80） |
| `ChainBetrayalLoyaltyDrop` | property | 連鎖離反時の忠誠低下量（デフォルト:10） |
| `WinLoyaltyBonus` | property | 勝利時の忠誠ボーナス（デフォルト:3） |
| `LossLoyaltyPenalty` | property | 敗北時の忠誠ペナルティ（デフォルト:5） |
| `SpringBonus` | property | 春の忠誠ボーナス（デフォルト:2） |
| `Update(SimulationContext)` | method | 忠誠変動を適用し裏切り判定を実行する |

---

## MovementSystem : ISimulationSystem
移動システム。ArmyのDestinationHexIdを見て1Hexずつ移動させる。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Update(SimulationContext)` | method | 地形チェック・AP消費・MoveCooldownを処理し、到着時にMovementEventを積む |

---

## RecruitmentSystem : ISimulationSystem
無所属武将の再仕官システム。ClanId=0のArmyを指揮するOfficerを最も近い勢力へ仕官させる。

| メンバー | 種別 | 説明 |
|---|---|---|
| `RecruitAmbitionThreshold` | property | 仕官判定の基準Ambition（デフォルト:30） |
| `Update(SimulationContext)` | method | 無所属武将を探し仕官処理を実行する |

---

## RelationshipSystem : ISimulationSystem
武将間の関係変動システム。同Hex共闘・敵対・放置・戦闘結果に応じてRelationshipを更新する。
`RelationTrigger` が登録されている場合、関係値変動後に突発イベントも評価する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `AllyTrustGain` | property | 同Hex共闘時のTrust上昇量（デフォルト:1） |
| `EnemyDislikeGain` | property | 同Hex敵対時のDislike上昇量（デフォルト:2） |
| `RespectGain` | property | 強い味方へのRespect上昇量（デフォルト:1） |
| `NeglectRespectLoss` | property | 放置時のRespect低下量（デフォルト:1） |
| `BattleWinTrustGain` | property | 戦闘勝利時のTrust上昇量（デフォルト:3） |
| `BattleLossDislike` | property | 戦闘敗北時の主君へのDislike上昇量（デフォルト:2） |
| `NeglectDistanceThreshold` | property | 放置と判定するHex距離の閾値（デフォルト:4） |
| `RelationshipSystem(int, int, int, int, int, int, int)` | constructor | 後方互換用コンストラクタ。RelationTriggerなし・乱数はデフォルト |
| `RelationshipSystem(int, int, int, int, int, int, int, IEnumerable<RelationTrigger>?, int?)` | constructor | RelationTriggerと乱数シードを指定する拡張コンストラクタ |
| `Update(SimulationContext)` | method | 共闘・敵対・放置・戦闘結果による関係変動を適用し、RelationTriggerの突発イベントを評価する |

---

## SupplySystem : ISimulationSystem
兵力補充システム。毎Step勢力に所属するArmyの兵力を回復させる。

| メンバー | 種別 | 説明 |
|---|---|---|
| `BaseReplenishment` | property | 毎Stepの基本補充量（デフォルト:50） |
| `SpringBonus` | property | 春の追加補充量（デフォルト:30） |
| `MaxSoldiers` | property | 兵力の上限（デフォルト:1000） |
| `EventThreshold` | property | SupplyEventを発火する補充量の閾値（デフォルト:200） |
| `Update(SimulationContext)` | method | 全Armyの兵力補充を処理する |

---

## VictorySystem : ISimulationSystem
毎Step勝利条件を評価し、条件を満たしたらGameOverEventを発火する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Update(SimulationContext)` | method | 兵力が残っている勢力数を確認し勝利・引き分けを判定する |

---

## VisionSystem : ISimulationSystem
索敵システム。各ArmyのVisionRange内にあるHexを計算しWorldState.Visionsを更新する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Update(SimulationContext)` | method | 全ArmyのVisionDataを計算しWorldState.Visionsに格納する |

---

# BattleCore.Systems.Battle

## IDamageCalculator
戦闘ダメージ計算のインターフェース。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Calculate(Army, Army)` | method | 地形・天気・武将能力値なしの簡易計算 |

---

## DamageCalculator : IDamageCalculator
戦闘ダメージを計算するクラス。Officer能力値・地形・天気を考慮する。

**補正ルール:**
- Leadership（統率）: 実効兵力に補正。100基準で±50%
- Strategy（戦術）: 与ダメージに補正。100基準で±30%
- Courage（武勇）: 接近戦追加ダメージ。100基準で±10%

| メンバー | 種別 | 説明 |
|---|---|---|
| `Calculate(Army, Army, Officer?, Officer?, TerrainType, Weather, bool)` | method | Officer能力値・地形・天気・城ボーナスを考慮して戦闘結果を計算する |

---

## BattleResult
1件の戦闘結果を表す不変クラス。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Winner` | property | 勝者の軍 |
| `Loser` | property | 敗者の軍 |
| `WinnerLosses` | property | 勝者が失った兵力数 |
| `LoserLosses` | property | 敗者が失った兵力数 |

# BattleCore.Simulation

## Season
ゲーム内の季節列挙型。

| 値 | 説明 |
|---|---|
| `Spring` | 春。忠誠ボーナスあり |
| `Summer` | 夏。標準 |
| `Autumn` | 秋。標準 |
| `Winter` | 冬。補給量減少（将来実装） |

---

## Weather
天気の種別列挙型。

| 値 | 説明 |
|---|---|
| `Sunny` | 晴れ。補正なし |
| `Rain` | 雨。Forest進入コスト+1、戦闘ダメージ10%減 |
| `Fog` | 霧。AIの移動判断を妨害（将来実装） |

---

## TurnPhase
ターンのフェーズ列挙型。StepPhase()で1フェーズずつ進める。

| 値 | 説明 |
|---|---|
| `PlayerPhase` | プレイヤー命令入力フェーズ |
| `AIPhase` | AI命令決定フェーズ |
| `Movement` | 移動解決フェーズ |
| `Battle` | 戦闘解決フェーズ |
| `Supply` | 補給・徴兵フェーズ |
| `Victory` | 勝利判定フェーズ |

---

## GameTime
ゲーム内時間を管理するクラス。Stepごとに`Advance()`が呼ばれ季節・年が進む。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Tick` | property | 経過ステップ数 |
| `Year` | property | 現在の年。初期値は1560年 |
| `Season` | property | 現在の季節 |
| `Weather` | property | 現在の天気。毎Tick確率で変化する |
| `RestoreFrom(int, int, Season, Weather)` | method | セーブデータから状態を復元する |
| `Advance()` | method | 時間を1ステップ進める。季節・天気を更新する |

---

## ISimulationSystem
シミュレーションシステムの共通インターフェース。SimulationEngineに登録され毎StepにUpdate()が呼ばれる。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Update(SimulationContext)` | method | シミュレーションを1ステップ進める |

---

## IPhasedSystem
特定のTurnPhaseにのみ実行されるSystemのインターフェース。ISimulationSystemと組み合わせて実装する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Phase` | property | このSystemが実行されるフェーズ |

---

## SimulationContext
1ステップ分のシミュレーション実行コンテキスト。全ISimulationSystemに渡される統一アクセス口。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Time` | property | ゲーム内時間 |
| `World` | property | ゲーム世界の全状態 |
| `CurrentPhase` | property | 現在のターンフェーズ |
| `CommandQueue` | property | 命令キュー。DecisionSystemがEnqueueし、CommandExecutionSystemがDequeue |
| `EventQueue` | property | イベントキュー。各SystemがEnqueueし、UIが参照する |
| `SimulationContext(WorldState)` | constructor | WorldStateからコンテキストを生成する |
| `SimulationContext(WorldState, GameTime)` | constructor | SaveSystemからの復元用。外部で構築したGameTimeを渡す |

---

## SimulationEngine
シミュレーションエンジンの中核。登録されたISimulationSystemを順番に実行し世界を1ステップ進める。

**推奨登録順:** CastleSystem → ClanDecisionSystem → CommandExecutionSystem → MovementSystem → BattleSystem → LoyaltySystem → RecruitmentSystem → SupplySystem → RelationshipSystem → DiplomacySystem → EventTriggerSystem → VictorySystem

| メンバー | 種別 | 説明 |
|---|---|---|
| `Context` | property | 現在のシミュレーションコンテキスト |
| `SimulationEngine(WorldState)` | constructor | WorldStateからエンジンを生成する |
| `SimulationEngine(SimulationContext)` | constructor | 既存のSimulationContextからエンジンを生成する（テスト用途） |
| `Register(ISimulationSystem)` | method | システムを登録する。登録順が実行順になる |
| `RegisterSystem(ISimulationSystem)` | method | Registerの別名（既存テストとの互換用） |
| `Step()` | method | 世界を1ステップ進める。全Systemを登録順に実行し最後にGameTimeを進める |
| `StepPhase()` | method | 現在のフェーズに対応するSystemだけを実行し次のフェーズへ進む |

---

## SimulationRunner
シミュレーションの自動実行を管理する。UI非依存。

| メンバー | 種別 | 説明 |
|---|---|---|
| `IntervalMs` | property | ターン間の待機ミリ秒。0=MAX速度 |
| `IsRunning` | property | 自動実行中かどうか |
| `TurnCompleted` | event | 1ターン完了時に発火。引数は現在の統計 |
| `RunCompleted` | event | 実行完了時に発火（停止・勝利・ターン上限） |
| `RunAsync(int, CancellationToken)` | method | 指定ターン数だけ実行する。maxTurns=0で勝利まで無制限 |
| `RunBatchAsync(...)` | method | N回バッチ実行。各回はシナリオをロードし直して実行する |
| `Pause()` | method | 実行を一時停止する |
| `Stop()` | method | Pauseの別名 |

---

## SimulationStats
シミュレーション実行中の統計を収集する。UI非依存。

| メンバー | 種別 | 説明 |
|---|---|---|
| `TurnsExecuted` | property | 実行済みターン数 |
| `BattleCount` | property | 発生した戦闘数 |
| `SupplyCount` | property | 発生した補給イベント数 |
| `BetrayalCount` | property | 発生した裏切り数 |
| `RefusalCount` | property | 発生した命令拒否数 |
| `IndependentCount` | property | 発生した独断行動数 |
| `TotalEvents` | property | 発生した全イベント数 |
| `WinnerClanId` | property | 勝利した勢力ID。nullの場合は未終了または引き分け |
| `WinReason` | property | ゲーム終了理由のメッセージ |
| `FiredEvents` | property | 発火したシナリオイベントID一覧 |
| `OfficerStats` | property | 武将別統計。Key=OfficerId |
| `Collect(SimulationContext)` | method | 1ターン分のイベントキューを処理して統計を更新する |

---

## BattleRunRecord
バッチ実行1回分の結果。BattleRunSummaryが集計に使用する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `RunIndex` | property | 実行番号（1始まり） |
| `Seed` | property | 乱数シード。nullの場合はランダム |
| `WinnerClanId` | property | 勝利した勢力ID |
| `TurnsExecuted` | property | 実行ターン数 |
| `BattleCount` | property | 発生戦闘数 |
| `SupplyCount` | property | 発生補給イベント数 |
| `BetrayalCount` | property | 発生裏切り数 |
| `RefusalCount` | property | 発生命令拒否数 |
| `IndependentCount` | property | 発生独断行動数 |
| `TotalEvents` | property | 発生全イベント数 |
| `FiredEvents` | property | 発火したシナリオイベントID一覧 |
| `OfficerStats` | property | 武将別統計 |
| `From(int, int?, SimulationStats)` | static method | SimulationStatsからBattleRunRecordを生成する |
| `ToCsvRow(string)` | method | CSV1行分を返す |

---

## BattleRunSummary
バッチ実行N回分の集計。勢力別勝率・性格別統計を提供する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Records` | property | 全実行レコードの読み取り専用リスト |
| `TotalRuns` | property | 実行回数 |
| `Add(BattleRunRecord)` | method | 実行レコードを追加する |
| `WinsByClan()` | method | 勢力別勝利数を返す。Key=ClanId |
| `StatsByPersonality()` | method | 性格別統計を返す。Key=Personality名 |
| `AvgTurns()` | method | 平均ターン数を返す |
| `AvgBattles()` | method | 平均戦闘数を返す |
| `EventStats()` | method | イベント別統計を返す。Key=TriggerId |
| `CalcStats(Func<BattleRunRecord, int>)` | method | 指定フィールドの統計（平均/最小/最大/標準偏差）を返す |
| `ToCsvLines(Func<int?, string>)` | method | CSV全行（ヘッダー含む）を返す |

---

## OfficerRunStats
武将1人分の実行統計。

| メンバー | 種別 | 説明 |
|---|---|---|
| `OfficerId` | property | 武将ID |
| `Name` | property | 武将名 |
| `Personality` | property | 性格名 |
| `RefusalCount` | property | 命令拒否回数 |
| `IndependentCount` | property | 独断行動回数 |
| `Survived` | property | 生存したかどうか。falseの場合は戦死 |

---

## PersonalityStats
性格別の集計統計。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Personality` | property | 性格名 |
| `TotalOfficers` | property | 対象武将の総数 |
| `RefusalCount` | property | 命令拒否の総数 |
| `IndependentCount` | property | 独断行動の総数 |
| `DeathCount` | property | 戦死数 |

---

## FieldStats
数値フィールドの統計情報。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Avg` | property | 平均値 |
| `Min` | property | 最小値 |
| `Max` | property | 最大値 |
| `StdDev` | property | 標準偏差 |

---

## EventStat
イベント別統計。

| メンバー | 種別 | 説明 |
|---|---|---|
| `TriggerId` | property | トリガーID |
| `FiredCount` | property | 発火回数 |
| `WinsByClan` | property | 発火時の勝者分布。Key=ClanId |
| `FiredRate(int)` | method | 発火率（%）を返す |

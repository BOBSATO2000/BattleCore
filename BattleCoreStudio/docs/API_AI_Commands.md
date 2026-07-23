# BattleCore.AI

## IArmyDecision
軍の行動決定インターフェース。実装を差し替えることでAIを交換可能にする。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Decide(Army, WorldState)` | method | 軍の行動を決定し命令を返す。何もしない場合はnull |

---

## IClanStrategy
勢力単位の戦略決定インターフェース。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Decide(Clan, WorldState)` | method | 勢力の戦略を決定し命令リストを返す |

---

## AggressiveClanStrategy : IClanStrategy
積極攻撃戦略。Fog of War対応版。

| メンバー | 種別 | 説明 |
|---|---|---|
| `RetreatThreshold` | property | 撤退判定の兵力閾値（デフォルト:300） |
| `Decide(Clan, WorldState)` | method | 視界内の敵へ進軍。兵力不足時は撤退。敵が見えない場合は索敵前進 |

---

## SimpleArmyDecision : IArmyDecision
最初の簡易AI実装。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Decide(Army, WorldState)` | method | 最も近い敵へPathFinderで経路を計算し次のHexへ移動命令を返す |

---

## OfficerDecision
武将単位の意思決定フィルタ。ClanStrategyが生成した命令を武将の性格・忠誠・人間関係でフィルタする。

**判断フロー:**
1. 忠誠が極めて低い → 命令拒否
2. 慎重な性格 + 兵力不足 → 撤退進言
3. 野心的 + 低忠誠 → 独断行動
4. 主君への不満（Dislike高）→ 命令変更
5. 上記以外 → 元の命令をそのまま通す
6. `SpontaneousEventTable` が注入されている場合 → 突発イベント評価

| メンバー | 種別 | 説明 |
|---|---|---|
| `RefusalLoyaltyThreshold` | property | 命令拒否する忠誠閾値（`RandomizedThreshold`型） |
| `CautiousRetreatSoldiers` | property | 慎重な武将が撤退進言する兵力閾値（`RandomizedThreshold`型） |
| `IndependentActionLoyalty` | property | 野心的な武将が独断行動する忠誠閾値（`RandomizedThreshold`型） |
| `DissatisfiedDislikeThreshold` | property | 主君への反感が命令変更を引き起こす閾値（`RandomizedThreshold`型） |
| `OfficerDecision(int, int, int, int, int?, SpontaneousEventTable?)` | constructor | int で閾値を直接指定する後方互換コンストラクタ。Spread=0の固定閾値として扱われる |
| `OfficerDecision(AiParams, int?, SpontaneousEventTable?)` | constructor | AiParamsから構築する。seedを指定すると再現性が保証される |
| `Evaluate(IEnumerable<ICommand>, Clan, WorldState)` | method | 命令リストを武将の意思で評価しDecisionResultのリストを返す |
| `Filter(IEnumerable<ICommand>, Clan, WorldState)` | method | 後方互換用ラッパー。commands・eventsのタプルを返す |

---

## DecisionResult
武将1人分の意思決定結果。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Command` | property | 実行する命令。nullの場合は命令拒否 |
| `Reason` | property | 判断理由 |
| `Accepted` | property | 命令が受諾されたか |
| `Event` | property | この判断に伴って発生するイベント |
| `Explanation` | property | 判断の詳細説明（デバッグ・UI表示用） |
| `Accept(...)` | static method | 命令受諾の結果を生成する |
| `Refuse(...)` | static method | 命令拒否の結果を生成する |

---

## DecisionExplanation
AI判断の説明。デバッグUI・ログ・プレイヤー表示に共通利用する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Reason` | property | 判断の理由分類 |
| `Summary` | property | 判断の要約文 |
| `Factors` | property | 判断に影響した要因のリスト |
| `Create(DecisionReason, string, string[])` | static method | インスタンスを生成するファクトリメソッド |

---

## AiParams
武将意思決定のパラメータ。ai_params.jsonから読み込む。
各閾値は `RandomizedThreshold` 型で `Center`（中心値）と `Spread`（乱数幅）を持つ。

| メンバー | 種別 | 説明 |
|---|---|---|
| `RefusalLoyaltyThreshold` | property | 命令拒否する忠誠閾値（Center=20, Spread=8） |
| `CautiousRetreatSoldiers` | property | 慎重な武将が撤退進言する兵力閾値（Center=300, Spread=80） |
| `IndependentActionLoyalty` | property | 野心的な武将が独断行動する忠誠閾値（Center=35, Spread=10） |
| `DissatisfiedDislikeThreshold` | property | 主君への反感が命令変更を引き起こす閾値（Center=60, Spread=15） |
| `Default` | static property | デフォルト値のインスタンスを返す |

---

## RandomizedThreshold
閾値に乱数幅を持たせる構造体。毎回 `Roll()` を呼ぶたびに `Center ± Spread` の範囲でブレた値を返す。
「忠誠21なら絶対従う」のような確定的な判定を防ぐ。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Center` | property | 閾値の中心値。ai_params.json で設定する基準値 |
| `Spread` | property | 乱数幅。Center ± Spread の範囲でブレる |
| `Roll(Random)` | method | Center ± Spread の範囲でランダムな閾値を返す |
| `implicit operator RandomizedThreshold(int)` | operator | int から暗黙変換。Spread=0 の固定閾値として扱う |

---

## SpontaneousEventRule
突発イベントの発火ルール1件。条件・確率・確率補正・イベント生成関数をひとまとめにする。
`SpontaneousEventTable` に登録して使用する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Id` | property | ルールを識別するID。デバッグ・ログ用途 |
| `Condition` | property | 発火条件。Officer・主君Officer・Membership・WorldState を受け取り true/false を返す |
| `BaseProbability` | property | 基本発火確率（0.0〜1.0） |
| `ProbabilityModifier` | property | 確率を動的に補正する関数（省略可）。Officer と主君へのRelationship を受け取り乗数を返す |
| `EventFactory` | property | 発火時に生成するイベントのファクトリ関数。Officer と WorldState を受け取り IGameEvent を返す |

---

## SpontaneousEventTable
突発イベントテーブル。登録された `SpontaneousEventRule` を毎ターン評価し、条件を満たしたルールを確率で発火する。
`OfficerDecision` に注入して使用する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Rules` | property | 登録されているルールの読み取り専用リスト |
| `SpontaneousEventTable(Random)` | constructor | 乱数インスタンスを外部から注入する |
| `Add(SpontaneousEventRule)` | method | ルールをテーブルに追加する |
| `Evaluate(Officer, Officer?, Membership?, Relationship?, WorldState)` | method | 指定した武将に対して全ルールを評価し、発火したイベントを返す |

---

## RelationTrigger
2武将間の関係値が特定の条件を満たしたとき確率でイベントを発火するトリガー。
`RelationshipSystem.Update()` の末尾で評価される。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Id` | property | トリガーを識別するID。デバッグ・ログ用途 |
| `Condition` | property | 発火条件。Relationship を受け取り true/false を返す |
| `Probability` | property | 発火確率（0.0〜1.0） |
| `EventFactory` | property | 発火時に生成するイベントのファクトリ関数。起点の武将と対象の武将を受け取り IGameEvent を返す |

---

## AiParamsLoader
ai_params.jsonを読み込んでAiParamsを返す静的クラス。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Load(string)` | static method | 指定パスのJSONファイルを読み込む。ファイルが存在しない・パース失敗時はデフォルト値を返す |
| `LoadFromBaseDir()` | static method | 実行ファイルと同じディレクトリのai_params.jsonを読み込む |

---

# BattleCore.Commands

## ICommand
ゲーム内命令の共通インターフェース。Commandパターン。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Execute(SimulationContext)` | method | 命令を実行する。SimulationContext経由でWorldを変更する |

---

## DecisionReason
命令の判断理由列挙型。MoveArmyCommandに付与される。

| 値 | 説明 |
|---|---|
| `Advance` | 通常の進軍命令 |
| `TargetCastle` | 敵城を優先攻略 |
| `Retreat` | 兵力不足による撤退 |
| `Reinforce` | 友軍救援のための移動 |
| `CautiousRetreat` | 武将の性格（慎重）による撤退進言 |
| `IndependentAction` | 武将の独断行動（野心・低忠誠） |
| `Dissatisfied` | 主君への不満による命令変更 |

---

## MoveArmyCommand : ICommand
指定した軍を指定したHexへ移動させる命令。

| メンバー | 種別 | 説明 |
|---|---|---|
| `ArmyId` | property | 移動させる軍のID |
| `DestinationHexId` | property | 移動先HexID |
| `Reason` | property | 命令の判断理由 |
| `Execute(SimulationContext)` | method | WorldStateからArmyを検索し移動目標を設定する |

---

## MoveOrder : ICommand
MoveArmyCommandの別名。

| メンバー | 種別 | 説明 |
|---|---|---|
| `ArmyId` | property | 移動させる軍のID |
| `DestinationHexId` | property | 移動先HexID |
| `Execute(SimulationContext)` | method | WorldStateからArmyを検索し移動目標を設定する |

---

## WaitOrder : ICommand
指定した軍を待機させる命令。

| メンバー | 種別 | 説明 |
|---|---|---|
| `ArmyId` | property | 待機させる軍のID |
| `Execute(SimulationContext)` | method | 何もしない |

---

## CommandQueue
ゲーム内命令キュー。AI・プレイヤー両方がEnqueueし、CommandExecutionSystemがDequeueして実行する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Count` | property | キューに積まれている命令の数 |
| `Enqueue(ICommand)` | method | 命令をキューに積む |
| `Dequeue()` | method | 先頭の命令を取り出す |
| `Clear()` | method | キューを空にする |

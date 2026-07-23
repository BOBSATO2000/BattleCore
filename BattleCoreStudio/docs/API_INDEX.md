# BattleCore API ドキュメント

## ファイル一覧

| ファイル | 内容 |
|---|---|
| [API_Entities.md](API_Entities.md) | Entity / Clan / Officer / Army / Castle / Alliance / Relationship |
| [API_AI_Commands.md](API_AI_Commands.md) | IArmyDecision / IClanStrategy / AggressiveClanStrategy / OfficerDecision / DecisionResult / AiParams / ICommand / CommandQueue など |
| [API_Map_World.md](API_Map_World.md) | Hex / GameMap / HexDistance / HexPathFinder / PathResult / WorldState / VisionData / Membership |
| [API_Simulation.md](API_Simulation.md) | GameTime / SimulationEngine / SimulationContext / SimulationRunner / SimulationStats / BattleRunRecord / BattleRunSummary など |
| [API_Systems.md](API_Systems.md) | BattleSystem / CastleSystem / ClanDecisionSystem / MovementSystem / LoyaltySystem / SupplySystem / VictorySystem など全System |
| [API_Events_Battle_Save_Scenario_Player.md](API_Events_Battle_Save_Scenario_Player.md) | IGameEvent / 全Eventクラス / BattleFinder / BattleResolver / SaveSystem / ScenarioLoader / PlayerController / DebugPanelBuilder |

## アーキテクチャ概要

```
BattleCore（コアライブラリ）
├── Entities        ゲームオブジェクト（Clan / Officer / Army / Castle / Alliance / Relationship）
├── AI              戦略・意思決定（IClanStrategy / OfficerDecision / AiParams）
├── Commands        命令パターン（ICommand / MoveArmyCommand / CommandQueue）
├── Systems         シミュレーションシステム群（各ISimulationSystem実装）
├── Simulation      エンジン・コンテキスト・統計（SimulationEngine / GameTime）
├── Map             ヘックスマップ（Hex / GameMap / HexDistance）
├── Navigation      経路探索（HexPathFinder / PathResult）
├── World           ワールド状態（WorldState）
├── Vision          索敵（VisionData）
├── Events          イベント群（IGameEvent実装）
├── Battle          戦闘ロジック（BattleFinder / BattleResolver）
├── Save            セーブ・ロード（SaveSystem / Snapshots）
├── Scenario        シナリオ読み込み（ScenarioLoader / ScenarioData）
├── Player          プレイヤー入力（IPlayerController / PlayerSession）
├── Relations       所属関係（Membership）
└── Debug           デバッグUI支援（DebugPanelBuilder）
```

## シミュレーション実行フロー（1ターン）

```
SimulationEngine.Step()
  ├── 全軍のAP リセット
  ├── CastleSystem.Update()        城の占領・補充
  ├── ClanDecisionSystem.Update()  AI命令生成 → CommandQueue
  ├── CommandExecutionSystem.Update() 命令実行
  ├── MovementSystem.Update()      移動処理
  ├── BattleSystem.Update()        戦闘解決
  ├── LoyaltySystem.Update()       忠誠変動・裏切り判定
  ├── RecruitmentSystem.Update()   武将仕官
  ├── SupplySystem.Update()        兵力補充
  ├── RelationshipSystem.Update()  関係値変動
  ├── DiplomacySystem.Update()     同盟管理
  ├── EventTrig[TurnFlow.md](TurnFlow.md)gerSystem.Update()  シナリオイベント発火
  ├── VictorySystem.Update()       勝利判定
  └── GameTime.Advance()           時間を進める
```

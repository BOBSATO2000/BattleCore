# BattleCore Studio 開発会話履歴

## 開発期間
Stage 11〜v1.0完成まで

## ソースパス
`C:\Dropbox\Game\AmazonQ\BattleCoreStudio\BattleCoreStudio`

## GitリポジトリURL
https://github.com/BOBSATO2000/BattleCore.git

---

## Stage 11: 地形ボーナス（TerrainBonus）
**コミット**: `b4e997f`

### 変更内容
- `DamageCalculator.Calculate()` に `TerrainType defenderTerrain` オーバーロード追加
  - Forest: 防御側損害 **20%軽減**
  - Mountain: 防御側損害 **30%軽減**
- `BattleResolver.Resolve()` で防御側の Hex を WorldState から取得して渡す
- `TerrainTests.cs` に3テスト追加（Plain/Forest/Mountain）

### テスト
75テスト全通過（+3）

---

## Scenario: osaka1615
**コミット**: `9479a73`

### 内容
- 勢力: 豊臣 vs 徳川
- 武将: 幸村(155/145/170)・治長・秀頼 vs 家康・秀忠・忠直
- 地形: 中央Hex13=Mountain（大坂城）、周囲8Hex=Forest（堀・防衛線）
- 兵力: 豊臣2600 vs 徳川3700
- トリガー: 夏の陣開幕(Tick1)・真田突撃(Tick3)・豊臣存亡の危機(Tick5)・家康天下統一(Tick7)

---

## Stage 12: 移動コスト（MovementCost）
**コミット**: `c0d62a0`

### 変更内容
- `Army.MoveCooldown` プロパティ追加
- `MovementSystem` で Forest 進入時に `MoveCooldown = 1` をセット → 次Tickは移動スキップ
- Plain は従来通り1Tick/1Hex
- `MovementSystemTests.cs` に2テスト追加

### テスト
77テスト全通過（+2）

---

## UI: マップ描画改善
**コミット**: `ad84145`

### 変更内容
- 同Hexに複数の軍がいる場合、最大4つまでオフセット表示（重ならない）
- 移動先への黄色矢印（DestinationHexId がある軍に表示）
- 左上に地形凡例（平地・森・山 + 移動コスト説明）
- 兵力バーの上限を実兵力ベースに修正

---

## Scenario: kawanakajima1561
**コミット**: `8825046`

### 内容
- 勢力: 上杉 vs 武田（2勢力直接対決）
- 武将: 謙信・景勝・直江 vs 信玄・信繁・山県
- 地形: 左端=Mountain（妻女山）、右端=Mountain（海津城）、中央=Plain（川中島）、川沿い=Forest（千曲川・犀川）
- 兵力: 上杉3000 vs 武田3300
- 関係値: 謙信↔信玄に相互Respect+Dislike（宿命のライバル）
- トリガー: 開幕(Tick1)・謙信単騎突撃(Tick3)・信玄軍配(Tick4)・信繁奮戦(Tick5)・引き分け(Tick8)

---

## Stage 13: 天気システム（WeatherSystem）
**コミット**: `54a4990`

### 変更内容
- `Weather.cs` 新規作成（Sunny/Rain/Fog）
- `GameTime.cs` に `Weather` プロパティ追加、毎Tick確率遷移
  - Sunny→Rain 25%、Rain→Fog 20%、Fog→Sunny 50%
- `WorldState.cs` に `Weather` プロパティ追加
- `SimulationEngine.cs` Step後に `WorldState.Weather` を同期
- `MovementSystem.cs` Rain時 Forest コスト=2（通常は1）
- `DamageCalculator.cs` Rain時 双方ダメージ10%軽減
- `frMain.cs` lblStatus に ☀晴/🌧雨/🌫霧 表示
- `WeatherTests.cs` 新規作成（4テスト）

### テスト
81テスト全通過（+4）

---

## Stage 14: 戦闘ログ詳細化（BattleLog）
**コミット**: `f79aba8`

### 変更内容
- `BattleLogEvent.cs` 新規作成（勝者/敗者名・損害・地形・天気・成長詳細）
- `BattleResolver.Resolve()` が `BattleLogEvent` を返すよう変更
- `BattleSystem` が毎戦闘後に EventQueue へ積む
- `frMain.cs` でログ表示例:
  ```
  [Tick3] 謙信勝 損:120 / 信玄敗 損:450[森-20%][雨-10%] ★謙信 統率+1(156)
  ```

### テスト
81テスト全通過（変化なし）

---

## Stage 15: 城・拠点システム（CastleSystem）
**コミット**: `fb09854`

### 変更内容
- `Castle.cs` 新規エンティティ（HexId・OwnerClanId・ReinforcementPerTick）
- `CastleSystem.cs` 毎Tick：占領判定 → CastleCapturedEvent発火、補充処理
- `CastleCapturedEvent.cs` 占領イベント
- `DamageCalculator` 城ありHexで防御側損害**20%軽減**
- `BattleLogEvent` に `CastleBonus` フラグ追加、ログに`[城-20%]`表示
- `frMain.cs` 城アイコン⛩描画（勢力色）、占領ログ表示、CastleSystem登録
- sengoku1560に4城、kawanakajima1561に2城追加
- `CastleSystemTests.cs` 新規作成（4テスト）

### テスト
85テスト全通過（+4）

---

## Stage 16: AIの城優先戦略（CastleAI）
**コミット**: `e5d2fd4`

### 変更内容
- `AggressiveClanStrategy` を全面改修
  - 敵城が敵軍より近い（距離 × 0.8以内）場合 → 城を優先して進軍
  - 撤退時 → 自勢力の城へ逃げ込む（城がなければ従来通り最遠Hexへ）
- `MoveArmyCommand` に `ArmyId`/`DestinationHexId` プロパティ追加（テスト用）
- `CastleAITests.cs` 新規作成（3テスト）

### テスト
88テスト全通過（+3）

---

## Scenario: osaka1615・sekigahara1600 城データ追加
**コミット**: `7ac3549`

### osaka1615 城一覧
| 城名 | HexId | 所有 | 補充/Tick |
|---|---|---|---|
| 大坂城 | 12 | 豊臣 | 80 |
| 山里の陣 | 8 | 豊臣 | 40 |
| 山里の陣南 | 14 | 豊臣 | 40 |
| 徳川本陣 | 1 | 徳川 | 60 |
| 徳川南陣 | 21 | 徳川 | 60 |

### sekigahara1600 城一覧
| 城名 | HexId | 所有 | 補充/Tick |
|---|---|---|---|
| 徳川本陣 | 7 | 東軍 | 70 |
| 徳川山陣 | 6 | 東軍 | 50 |
| 関ヶ原要衝 | 13 | 中立(0) | 60 |
| 西軍本陣 | 22 | 西軍 | 70 |
| 山城 | 24 | 西軍 | 50 |

---

## UI: 勢力概要に城数表示 / README更新
**コミット**: `75d15b0`

### 変更内容
- 勢力概要パネルの表示例:
  ```
  ■ 豊臣  兵:2,600  軍:3  城:3
  ■ 徳川  兵:3,700  軍:3  城:2
  ```
- README.md を現在の実装状況に合わせて全面更新

---

## v1.0 完成
**タグ**: `v1.0`
**最終コミット**: `75d15b0`

### 最終テスト数
**88テスト全通過**

### 全シナリオ城データ一覧
| シナリオ | 城数 |
|---|---|
| sengoku1560 | 4 |
| kawanakajima1561 | 2 |
| sekigahara1600 | 5 |
| osaka1615 | 5 |
| **合計** | **16** |

---

## 未実装（v1.0時点）

### ゲームプレイ系
- 補給線（拠点から離れると兵力減少）
- 夜戦（Battle.cs コメントに「将来実装」と明記）
- 季節効果（GameTime.cs コメントに「農業・補給・行軍速度への影響」と明記）
- Fog の移動妨害（Weather.cs コメントに「将来実装」と明記）

### UI系
- 武将雇用UI（ゲーム中に新武将を雇える）
- シナリオエディタ

### AI系
- 守備AI（城に籠もって待つ戦略）
- 包囲戦術（複数軍で1軍を囲む）

### コード品質
- MSTest警告の解消（Assert.HasCount / Assert.IsEmpty への置き換え）

---

## システム登録順（frMain.cs）
1. CastleSystem
2. ClanDecisionSystem（AggressiveClanStrategy）
3. CommandExecutionSystem
4. MovementSystem
5. BattleSystem
6. LoyaltySystem
7. RecruitmentSystem
8. SupplySystem
9. RelationshipSystem
10. DiplomacySystem
11. EventTriggerSystem
12. VictorySystem

## 主要ファイル一覧
| ファイル | 役割 |
|---|---|
| `src/BattleCore/Entities/Army.cs` | 軍隊エンティティ（MoveCooldown含む） |
| `src/BattleCore/Entities/Officer.cs` | 武将（BattleWins含む） |
| `src/BattleCore/Entities/Castle.cs` | 城エンティティ |
| `src/BattleCore/Entities/Alliance.cs` | 同盟エンティティ |
| `src/BattleCore/Entities/Relationship.cs` | 武将間関係値 |
| `src/BattleCore/World/WorldState.cs` | 世界状態（Castles・Weather含む） |
| `src/BattleCore/Battle/BattleResolver.cs` | 戦闘解決（地形・天気・城・Trust補正） |
| `src/BattleCore/Battle/BattleFinder.cs` | 戦闘ペア探索（同盟チェック） |
| `src/BattleCore/Systems/Battle/DamageCalculator.cs` | ダメージ計算（地形・天気・城補正） |
| `src/BattleCore/Systems/MovementSystem.cs` | 移動（Forest/Rain コスト） |
| `src/BattleCore/Systems/CastleSystem.cs` | 城占領・補充 |
| `src/BattleCore/Systems/DiplomacySystem.cs` | 同盟管理・AI自動同盟 |
| `src/BattleCore/Systems/VictorySystem.cs` | 勝利判定 |
| `src/BattleCore/Systems/EventTriggerSystem.cs` | 条件付きイベント発火 |
| `src/BattleCore/AI/AggressiveClanStrategy.cs` | AI戦略（城優先・城への撤退） |
| `src/BattleCore/Simulation/GameTime.cs` | 時間管理（Weather含む） |
| `src/BattleCore/Simulation/Weather.cs` | 天気enum |
| `src/BattleCore/Scenario/ScenarioLoader.cs` | JSONシナリオ読み込み |
| `src/BattleCore/Events/BattleLogEvent.cs` | 戦闘ログイベント |
| `src/BattleCore/Events/CastleCapturedEvent.cs` | 城占領イベント |
| `src/BattleCore.WinForms/frMain.cs` | メインUI |
| `src/BattleCore.WinForms/frScenarioSelect.cs` | シナリオ選択ダイアログ |
| `scenarios/sengoku1560.json` | 桶狭間前夜1560 |
| `scenarios/kawanakajima1561.json` | 川中島1561 |
| `scenarios/sekigahara1600.json` | 関ヶ原1600 |
| `scenarios/osaka1615.json` | 大坂夏の陣1615 |

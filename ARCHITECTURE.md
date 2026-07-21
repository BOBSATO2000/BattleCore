# BattleCoreStudio アーキテクチャ解説

## 目次
1. [全体構成](#1-全体構成)
2. [レイヤー構造](#2-レイヤー構造)
3. [クラス一覧と役割](#3-クラス一覧と役割)
4. [クラス間リレーションシップ](#4-クラス間リレーションシップ)
5. [静的データと動的データ](#5-静的データと動的データ)
6. [1ステップの処理フロー](#6-1ステップの処理フロー)
7. [AIの意思決定フロー](#7-aiの意思決定フロー)
8. [イベントの流れ](#8-イベントの流れ)

---

## 1. 全体構成

```
BattleCoreStudio/
├── src/
│   ├── BattleCore/          # ゲームロジック（UIに依存しない）
│   └── BattleCore.WinForms/ # UI層（WinForms）
└── BattleCore.Tests/        # テストプロジェクト
```

BattleCore はUIに一切依存しない純粋なゲームロジック層。
WinForms は BattleCore を呼び出すだけで、ゲームルールを知らない。

---

## 2. レイヤー構造

```
┌─────────────────────────────────────┐
│  UI層  BattleCore.WinForms          │  frMain.cs
│        ↓ engine.Step() を呼ぶだけ   │
├─────────────────────────────────────┤
│  エンジン層  Simulation/            │  SimulationEngine
│              ↓ System を順番に実行  │  SimulationContext
├─────────────────────────────────────┤
│  システム層  Systems/               │  BattleSystem, MovementSystem ...
│              ↓ World を読み書き     │  各 ISimulationSystem 実装
├─────────────────────────────────────┤
│  世界状態層  World/                 │  WorldState
│              ↓ エンティティを保持   │
├─────────────────────────────────────┤
│  エンティティ層  Entities/          │  Army, Officer, Clan, Castle ...
│                  Map/               │  Hex, GameMap
│                  Relations/         │  Membership
└─────────────────────────────────────┘
```

---

## 3. クラス一覧と役割

### エンジン層

#### `SimulationEngine`
- **役割**: ゲームループの中核。登録された System を順番に実行する
- **保持するもの**: `List<ISimulationSystem>`, `SimulationContext`
- **主要メソッド**:
  - `Register(system)` — System を登録する（登録順 = 実行順）
  - `Step()` — 全 System を実行し、最後に GameTime.Advance() を呼ぶ
- **設計ポイント**: UI は `Step()` を呼ぶだけ。ゲームロジックを知らなくてよい

#### `SimulationContext`
- **役割**: 1ステップ分の実行コンテキスト。全 System への共通アクセス口
- **保持するもの**:
  - `GameTime Time` — 現在時刻
  - `WorldState World` — 世界の全状態
  - `Queue<ICommand> CommandQueue` — AI が積んだ命令キュー
  - `Queue<IGameEvent> EventQueue` — 発生したイベントキュー
- **設計ポイント**: System 同士が直接依存しないよう、Context を介してやり取りする

#### `GameTime`
- **役割**: ゲーム内時間（Tick・Year・Season・Weather）を管理する
- **主要メソッド**:
  - `Advance()` — Tick++、季節を進める、天気を確率で変化させる
- **天気遷移確率**:
  - Sunny → Rain: 25%
  - Rain → Fog: 20%
  - Fog → Sunny: 50%

---

### 世界状態層

#### `WorldState`
- **役割**: ゲーム世界全体のスナップショット。全エンティティのリストを保持する
- **保持するもの**:

| プロパティ | 型 | 内容 |
|---|---|---|
| Officers | List\<Officer\> | 全武将 |
| Clans | List\<Clan\> | 全勢力 |
| Memberships | List\<Membership\> | 武将と勢力の所属関係 |
| Relationships | List\<Relationship\> | 武将間の人間関係 |
| Armies | List\<Army\> | 全軍隊 |
| Alliances | List\<Alliance\> | 勢力間の同盟 |
| Castles | List\<Castle\> | 城・拠点 |
| Map | GameMap | ヘックスマップ |
| Visions | Dictionary\<int, VisionData\> | 各軍の索敵情報 |
| Weather | Weather | 現在の天気 |

- **主要メソッド**:
  - `GetArmyById(id)` — ID で軍を検索
  - `AreAllied(clanId1, clanId2)` — 2勢力が同盟中か判定
  - `GetOrCreateRelationship(fromId, toId)` — 武将間の関係を取得（なければ新規作成）

---

### エンティティ層

#### `Entity`（基底クラス）
- **役割**: 全エンティティの基底。`Id` プロパティのみ持つ
- Army / Officer / Clan / Castle / Alliance / Relationship / Membership が継承する

#### `Army`（軍隊）
- **役割**: マップ上を移動して戦闘する駒
- **主要プロパティ**:
  - `ClanId` — 所属勢力（0=無所属・離反状態）
  - `OfficerId` — 指揮官（null=未配属）
  - `CurrentHexId` — 現在地
  - `DestinationHexId` — 移動目標（null=待機中）
  - `Soldiers` — 兵力（0=全滅、SupplySystem が毎Step回復させる）
  - `MoveCooldown` — 移動クールダウン（Forest進入時に1セット）
- **主要メソッド**:
  - `OrderMove(hexId)` — 移動命令を設定
  - `MoveTo(hexId)` — 実際に移動（MovementSystem が呼ぶ）
  - `LoseSoldiers(count)` — 兵力を減らす（0未満にならない）
  - `Reinforce(amount)` — 兵力を補充
  - `Defect(newClanId)` — 離反して別勢力へ移る
  - `AssignOfficer(officerId)` — 指揮官を配属

#### `Officer`（武将）
- **役割**: 軍を指揮する人物。能力値と人間関係を持つ
- **能力値**:
  - `Leadership`（統率）— 実効兵力に補正（±50%）
  - `Strategy`（戦術）— 与ダメージに補正（±30%）
  - `Courage`（武勇）— 接近戦追加ダメージ（±10%）
  - `Intelligence`（知略）— 外交・謀略（将来実装）
  - `Ambition`（野心）— 離反判定に影響
  - `Loyalty`（忠誠）— 離反判定に影響
  - `BattleWins` — 戦闘勝利回数（成長システム用）

#### `Clan`（勢力）
- **役割**: 大名家・武将集団を表す
- **主要プロパティ**: `Name`, `Gold`（将来実装）, `OfficerIds`, `ArmyIds`
- **注意**: `OfficerIds` / `ArmyIds` は参照用リストだが、実際の所属判定は `Membership.ClanId` / `Army.ClanId` で行う

#### `Castle`（城）
- **役割**: 特定 Hex に配置された拠点。占領勢力に補充ボーナスを与える
- **主要プロパティ**:
  - `HexId` — 配置 Hex（不変）
  - `OwnerClanId` — 占領勢力（0=中立）
  - `ReinforcementPerTick` — 毎 Tick の補充量（デフォルト50）
- **防御ボーナス**: 城のある Hex での戦闘は敗者損害を20%軽減

#### `Alliance`（同盟）
- **役割**: 2勢力間の期限付き同盟
- **主要プロパティ**: `ClanId1`, `ClanId2`, `RemainingTicks`
- `RemainingTicks` が0になると DiplomacySystem が解消する

#### `Relationship`（人間関係）
- **役割**: 武将間の一方向の関係値
- **主要プロパティ**: `Trust`（信頼）, `Respect`（尊敬）, `Dislike`（反感）
- 一方向なので A→B と B→A は別オブジェクト

#### `Membership`（所属関係）
- **役割**: Officer と Clan を結ぶ中間テーブル
- **主要プロパティ**: `OfficerId`, `ClanId`, `Loyalty`（この勢力への忠誠）
- Officer.Loyalty（個人の忠誠心）とは別に、勢力ごとの忠誠を管理する

---

### マップ層

#### `GameMap`
- **役割**: Hex の追加・検索・隣接取得を担当する地理ルール管理クラス
- **主要メソッド**:
  - `AddHex(hex)` — Hex を追加
  - `GetHexById(id)` — ID で検索
  - `GetNeighbors(hexId)` — 隣接する全 Hex を返す（6方向）

#### `Hex`
- **役割**: マップ上の1マス
- **主要プロパティ**: `Id`, `X`, `Y`, `Terrain`（Plain/Forest/Mountain）
- Army は Hex を直接保持せず `CurrentHexId` で参照する設計

#### `TerrainType`（enum）
| 値 | 移動 | 戦闘補正 |
|---|---|---|
| Plain | 通常 | なし |
| Forest | +1Tick（雨時+2Tick） | 敗者損害-20% |
| Mountain | 移動不可 | 敗者損害-30% |

---

### システム層（ISimulationSystem 実装）

登録順 = 実行順。frMain.cs で以下の順に登録される。

| # | クラス | 役割 |
|---|---|---|
| 1 | `CastleSystem` | 城の占領判定・兵力補充 |
| 2 | `ClanDecisionSystem` | 勢力単位のAI命令生成（IClanStrategy） |
| 3 | `CommandExecutionSystem` | CommandQueue を全消化して実行 |
| 4 | `MovementSystem` | Army を1Hexずつ移動させる |
| 5 | `BattleSystem` | 同Hexの敵と戦闘（BattleFinder + BattleResolver） |
| 6 | `LoyaltySystem` | 忠誠変動・離反判定 |
| 7 | `RecruitmentSystem` | 無所属武将の仕官処理 |
| 8 | `SupplySystem` | 兵力補充（全滅軍も毎Step回復） |
| 9 | `RelationshipSystem` | 武将間の関係値更新 |
| 10 | `DiplomacySystem` | 同盟期限管理・AI自動同盟 |
| 11 | `EventTriggerSystem` | シナリオイベントの発火 |
| 12 | `VictorySystem` | 勝利条件判定 |

#### `BattleSystem` の内部構造
```
BattleSystem.Update()
  └─ BattleFinder.Find()      → 同Hexの敵ペアを全列挙
       └─ BattleResolver.Resolve()  → 1件の戦闘を解決
            └─ DamageCalculator.Calculate()  → ダメージ計算
```

#### `DamageCalculator` の計算式
```
実効兵力 = Soldiers × (Leadership / 100)  ← 0.5〜1.5倍
勝者損害 = 敗者兵力 / 3 × Strategy補正 × Courage補正
敗者損害 = 敗者兵力 × 地形補正 × 天気補正 × 城補正
```

#### `LoyaltySystem` の離反スコア計算式
```
score = Ambition - Loyalty/2 - Membership.Loyalty/2 + 主君へのDislike
score >= BetrayalThreshold(80) → 離反イベント発生
```

---

### AIレイヤー

#### `IClanStrategy` / `AggressiveClanStrategy`
- **役割**: 勢力単位の戦略決定
- **AggressiveClanStrategy の行動**:
  - 兵力 > RetreatThreshold(300): 敵城が敵軍より近ければ城を優先攻略、そうでなければ最近敵軍へ進軍
  - 兵力 ≤ RetreatThreshold: 自勢力の城へ撤退（城がなければ敵から最遠の Hex へ）

#### `IArmyDecision` / `SimpleArmyDecision`
- **役割**: Army 単位の行動決定（DecisionSystem から使用）
- **SimpleArmyDecision の行動**: 最も近い敵へ BFS 経路で1歩進む

---

### コマンド層

#### `ICommand`
- **役割**: AI が生成し CommandExecutionSystem が実行する命令のインターフェース
- **実装クラス**:
  - `MoveArmyCommand` / `MoveOrder` — 移動命令（Army.OrderMove を呼ぶ）
  - `WaitOrder` — 待機命令（何もしない）

---

### イベント層

#### `IGameEvent`
- **役割**: EventQueue に積まれるイベントのマーカーインターフェース
- **実装クラス**:

| クラス | 発生元 | 内容 |
|---|---|---|
| `BattleLogEvent` | BattleSystem | 戦闘結果詳細 |
| `CastleCapturedEvent` | CastleSystem | 城占領 |
| `BetrayalEvent` | LoyaltySystem | 武将離反 |
| `RecruitEvent` | RecruitmentSystem | 武将仕官 |
| `ScenarioEvent` | EventTriggerSystem / DiplomacySystem | シナリオメッセージ |
| `GameOverEvent` | VictorySystem | ゲーム終了 |

---

## 4. クラス間リレーションシップ

### エンティティ間の関係

```
Clan ──────────────── Membership ──────────────── Officer
 │  (1対多: ClanId)   (中間テーブル)  (1対多: OfficerId)  │
 │                                                        │
 │                    Relationship ───────────────────────┘
 │                    (FromOfficerId → ToOfficerId: 一方向)
 │
Army ──── ClanId ────→ Clan
 │
 └── OfficerId ──────→ Officer
 │
 └── CurrentHexId ───→ Hex（GameMap内）

Castle ── HexId ──────→ Hex
       ── OwnerClanId →  Clan

Alliance ── ClanId1 ──→ Clan
         ── ClanId2 ──→ Clan
```

### 重要な設計判断

**Officer と Clan を直接結ばない理由**
```
× Officer.ClanId = 1  ← 勢力を変えると履歴が消える
○ Membership(OfficerId=5, ClanId=1, Loyalty=80)
                      ← 離反・仕官の履歴を表現できる
```

**Army と Hex を直接結ばない理由**
```
× Army.CurrentHex = hexObject  ← Hexが変わるたびに参照を張り替える必要がある
○ Army.CurrentHexId = 3        ← IDだけ持ち、GameMap.GetHexById()で検索する
```

### システム間の依存関係

```
frMain（UI）
  └─ SimulationEngine
       └─ SimulationContext
            ├─ WorldState ←── 全 System が読み書き
            ├─ CommandQueue ← ClanDecisionSystem/DecisionSystem が Enqueue
            │                  CommandExecutionSystem が Dequeue
            └─ EventQueue ←── BattleSystem/LoyaltySystem 等が Enqueue
                               frMain.UpdateUI() が Dequeue して表示
```

---

## 5. 静的データと動的データ

### 静的データ（シナリオ読み込み時に確定し、以後変わらない）

| データ | クラス | プロパティ |
|---|---|---|
| Hex の位置・地形 | Hex | Id, X, Y, Terrain |
| 城の配置場所 | Castle | HexId, Name, ReinforcementPerTick |
| 武将の名前 | Officer | Name |
| 勢力の名前 | Clan | Name |
| 武将と勢力の初期所属 | Membership | OfficerId, ClanId |

### 動的データ（毎 Step 変化する）

| データ | クラス | 変化させる System |
|---|---|---|
| 軍の位置 | Army.CurrentHexId | MovementSystem |
| 軍の兵力 | Army.Soldiers | BattleSystem, SupplySystem, CastleSystem |
| 軍の移動目標 | Army.DestinationHexId | CommandExecutionSystem, MovementSystem |
| 軍の所属勢力 | Army.ClanId | LoyaltySystem（離反）, RecruitmentSystem（仕官） |
| 城の占領勢力 | Castle.OwnerClanId | CastleSystem |
| 同盟の残り期間 | Alliance.RemainingTicks | DiplomacySystem |
| 武将の忠誠 | Membership.Loyalty | LoyaltySystem |
| 武将間の関係値 | Relationship.Trust/Respect/Dislike | RelationshipSystem |
| 武将の能力値 | Officer.Leadership/Strategy | BattleResolver（成長） |
| 武将の勝利数 | Officer.BattleWins | BattleResolver |
| 天気 | WorldState.Weather | SimulationEngine（GameTime同期） |
| 季節・年・Tick | GameTime | SimulationEngine.Step() |
| 索敵情報 | WorldState.Visions | VisionSystem |

### シナリオ読み込みの流れ

```
scenarios/*.json
  └─ ScenarioLoader.Load()
       └─ ScenarioData（DTO）
            ├─ HexData → Hex → GameMap.AddHex()
            ├─ ClanData → Clan → WorldState.Clans
            ├─ OfficerData → Officer → WorldState.Officers
            ├─ MembershipData → Membership → WorldState.Memberships
            ├─ ArmyData → Army → WorldState.Armies
            ├─ RelationshipData → Relationship → WorldState.Relationships
            ├─ AllianceData → Alliance → WorldState.Alliances
            ├─ CastleData → Castle → WorldState.Castles
            └─ EventTriggerData → List<EventTriggerData> → EventTriggerSystem
```

---

## 6. 1ステップの処理フロー

```
frMain: engine.Step()
  │
  ├─ 1. CastleSystem.Update()
  │       各城のHexを確認
  │       → 単独占領中なら OwnerClanId 更新 + CastleCapturedEvent
  │       → 占領勢力の軍に ReinforcementPerTick 補充
  │
  ├─ 2. ClanDecisionSystem.Update()
  │       各 Clan に AggressiveClanStrategy.Decide() を呼ぶ
  │       → MoveArmyCommand を CommandQueue に Enqueue
  │
  ├─ 3. CommandExecutionSystem.Update()
  │       CommandQueue を全消化
  │       → MoveArmyCommand.Execute() → Army.OrderMove(destinationHexId)
  │
  ├─ 4. MovementSystem.Update()
  │       DestinationHexId がある Army を1Hex移動
  │       → Mountain は移動不可
  │       → Forest 進入時は MoveCooldown = 1（雨時は2）
  │
  ├─ 5. BattleSystem.Update()
  │       BattleFinder で同Hexの敵ペアを列挙
  │       → BattleResolver.Resolve() で戦闘解決
  │            → DamageCalculator で損害計算
  │            → Army.LoseSoldiers() で兵力減少
  │            → Officer.BattleWins++ で成長判定
  │       → BattleLogEvent を EventQueue に Enqueue
  │
  ├─ 6. LoyaltySystem.Update()
  │       Membership.Loyalty を変動（勝ち続け+3、全滅-5、春+2）
  │       → 離反スコア計算 → 閾値超えで BetrayalEvent
  │            → Membership 削除 + Army.Defect(0)
  │
  ├─ 7. RecruitmentSystem.Update()
  │       ClanId=0 の Army を指揮する Officer を探す
  │       → Ambition >= 30 なら最近勢力へ仕官
  │       → Membership 追加 + Army.Defect(targetClanId) + RecruitEvent
  │
  ├─ 8. SupplySystem.Update()
  │       ClanId != 0 かつ Soldiers < 1000 の Army に補充
  │       → 基本50 + 春ボーナス30
  │
  ├─ 9. RelationshipSystem.Update()
  │       同Hex・同勢力 → Trust +1、Respect +1（兵力多い方へ）
  │       同Hex・異勢力 → Dislike +2
  │
  ├─ 10. DiplomacySystem.Update()
  │        Alliance.RemainingTicks-- → 0で解消 + ScenarioEvent
  │        10Tick毎: 共通の敵を持つ2勢力が自動同盟
  │
  ├─ 11. EventTriggerSystem.Update()
  │        各トリガーの条件（MinTick/MinDislike/MaxLoyalty）を評価
  │        → 条件成立で ScenarioEvent（一度だけ）
  │
  ├─ 12. VictorySystem.Update()
  │        生存勢力が1つ → GameOverEvent（天下統一）
  │        生存勢力が0 → GameOverEvent（全滅）
  │
  └─ GameTime.Advance()
          Tick++, 季節進行, 天気変化
          WorldState.Weather = GameTime.Weather
```

---

## 7. AIの意思決定フロー

```
ClanDecisionSystem
  └─ AggressiveClanStrategy.Decide(clan, world)
       各 Army について：
         兵力 <= 300（撤退閾値）
           → 自勢力の城へ MoveArmyCommand
           → 城がなければ敵から最遠の Hex へ
         兵力 > 300
           → 敵城への距離 vs 敵軍への距離 × 0.8 を比較
           → 敵城が近ければ城を優先
           → そうでなければ最近敵軍へ
           → HexPathFinder.FindPath() で経路計算
           → 経路の次の1Hex への MoveArmyCommand
       → CommandQueue に Enqueue

CommandExecutionSystem
  → Army.OrderMove(destinationHexId) を呼ぶ

MovementSystem
  → Army.MoveTo(nextHex) で1Hexずつ移動
```

---

## 8. イベントの流れ

```
各 System
  └─ context.EventQueue.Enqueue(event)

frMain.UpdateUI()
  └─ while (EventQueue.Count > 0)
       └─ ev = EventQueue.Dequeue()
            BattleLogEvent    → lstEvents に戦闘ログ表示
            CastleCapturedEvent → lstEvents に占領ログ表示
            BetrayalEvent     → lstEvents に離反ログ表示
            RecruitEvent      → lstEvents に仕官ログ表示
            ScenarioEvent     → lstEvents にメッセージ表示
            GameOverEvent     → ボタン無効化 + MessageBox 表示
```

---

## 補足：未実装項目（v1.0時点）

| 機能 | 関連クラス | 状態 |
|---|---|---|
| 補給線 | SupplySystem | 未実装（距離による補充減衰なし） |
| 夜戦 | DamageCalculator | 未実装 |
| 季節効果（冬の移動ペナルティ等） | MovementSystem | 未実装 |
| 霧による移動妨害 | VisionSystem | 基盤のみ実装 |
| 武将雇用UI | RecruitmentSystem | 自動のみ |
| シナリオエディタ | ScenarioLoader | 未実装 |
| 守備AI | IClanStrategy | AggressiveのみでDefensiveなし |
| 包囲戦術 | BattleFinder | 未実装 |
| Clan.Gold（資金） | Clan | フィールドのみ、使用なし |

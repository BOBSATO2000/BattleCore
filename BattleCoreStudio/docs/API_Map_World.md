# BattleCore.Map

## TerrainType
Hexの地形種別列挙型。

| 値 | 説明 |
|---|---|
| `Plain` | 平地。移動コスト1。補正なし |
| `Forest` | 森。移動コスト2。防御補正あり |
| `Mountain` | 山岳。移動不可。防御補正大（将来実装） |

---

## HexDirection
Hexマップ上の6方向列挙型。

| 値 | 説明 |
|---|---|
| `East` | 東（X+1） |
| `West` | 西（X-1） |
| `NorthEast` | 北東（X+1, Y-1） |
| `NorthWest` | 北西（X-1, Y-1） |
| `SouthEast` | 南東（X+1, Y+1） |
| `SouthWest` | 南西（X-1, Y+1） |

---

## Hex
ヘックスマップ上の1マスを表す。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Id` | property | Hexを一意に識別するID |
| `X` | property | X座標（東西方向） |
| `Y` | property | Y座標（南北方向） |
| `Terrain` | property | 地形種別。MovementSystemが移動可否の判定に使用 |

---

## HexDistance
2つのHex間のマンハッタン距離を計算するユーティリティ。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Calculate(Hex, Hex)` | static method | 2つのHex間の距離を返す（X差+Y差の絶対値の和） |

---

## GameMap
ゲーム世界の地理ルールを管理するクラス。Hexの追加・検索・隣接取得を担当する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Hexes` | property | マップ上の全Hexの読み取り専用リスト |
| `AddHex(Hex)` | method | Hexをマップに追加する |
| `GetHexById(int)` | method | IDでHexを検索する。見つからない場合はnull |
| `GetHex(int, int)` | method | 座標でHexを検索する。見つからない場合はnull |
| `GetNeighbors(int)` | method | 指定HexIDに隣接する全Hexを返す（6方向） |

---

# BattleCore.Navigation

## IPathFinder
経路探索インターフェース。

| メンバー | 種別 | 説明 |
|---|---|---|
| `FindPath(GameMap, int, int)` | method | 開始HexIDから目標HexIDまでの経路をHex IDのリストで返す |

---

## HexPathFinder : IPathFinder
地形コスト対応A*による経路探索実装。Plain=1, Forest=2, Mountain=通過不可。

| メンバー | 種別 | 説明 |
|---|---|---|
| `FindPath(GameMap, int, int)` | method | A*でstartHexIdからtargetHexIdへの最小コスト経路を返す |
| `FindPathWithCost(GameMap, int, int)` | method | A*でコスト付き経路（PathResult）を返す |

---

## PathResult
A*経路探索の結果。HexId列とステップごとのコストを保持する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Empty` | static property | 空の経路結果 |
| `HexIds` | property | 経路上のHexId列（start含む） |
| `StepCosts` | property | 各ステップの移動コスト |
| `TotalCost` | property | 合計コスト |

---

# BattleCore.World

## WorldState
ゲーム世界全体の状態を保持するクラス。全ISimulationSystemから参照される。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Officers` | property | 世界に存在する全武将 |
| `Clans` | property | 世界に存在する全勢力 |
| `Memberships` | property | 武将と勢力の所属関係 |
| `Relationships` | property | 武将間の人間関係 |
| `Armies` | property | 世界に存在する全軍隊 |
| `Alliances` | property | 勢力間の同盟リスト |
| `Map` | property | ヘックスマップ |
| `Castles` | property | 城・拠点リスト |
| `Weather` | property | 現在の天気 |
| `Visions` | property | 各軍の索敵情報。Key=ArmyId |
| `GetArmyById(int)` | method | IDで軍を検索する |
| `AreAllied(int, int)` | method | 2勢力が同盟中かどうかを返す |
| `GetOrCreateRelationship(int, int)` | method | 2武将間のRelationshipを取得する。存在しない場合は新規作成 |

---

# BattleCore.Vision

## VisionData
1つのArmyの索敵情報。VisionSystemが毎Step生成しWorldState.Visionsに格納する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `ArmyId` | property | この索敵情報を持つArmyのID |
| `VisibleHexes` | property | 視界内にあるHexのIDセット |

---

# BattleCore.Relations

## Membership : Entity
武将と勢力の所属関係を表すエンティティ。

| メンバー | 種別 | 説明 |
|---|---|---|
| `OfficerId` | property | 所属する武将のID |
| `ClanId` | property | 所属する勢力のID |
| `Loyalty` | property | この勢力への忠誠心（0〜100）。初期値50 |

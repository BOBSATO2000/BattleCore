# BattleCore.Entities

## Entity
全エンティティの基底クラス。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Id` | property | エンティティを一意に識別するID |

---

## Clan : Entity
勢力エンティティ。大名家・武将集団を表す。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Name` | property | 勢力名 |
| `Gold` | property | 資金。内政・雇用・外交に使用 |
| `OfficerIds` | property | 所属武将IDリスト |
| `ArmyIds` | property | 所属軍IDリスト |
| `DaimyoOfficerId` | property | 君主となるOfficerのID。nullの場合は未設定 |
| `IsPlayerControlled` | property | プレイヤーが操作する勢力かどうか。trueの場合AIをスキップ |
| `Clan(int id)` | constructor | IDを直接指定するコンストラクタ |
| `Clan(string name)` | constructor | 名前を指定するコンストラクタ。IDは自動採番 |

---

## Officer : Entity
武将エンティティ。性格・関係・状況によってAIが判断を変える基盤。

| メンバー | 種別 | 説明 |
|---|---|---|
| `Name` | property | 武将名 |
| `Loyalty` | property | 忠誠心。裏切り・離反判定に使用 |
| `Intelligence` | property | 知略。外交・謀略・戦術判断に影響 |
| `Ambition` | property | 野心。高いほど独立行動を取りやすい |
| `Leadership` | property | 統率力。戦闘時の兵力補正に影響 |
| `Strategy` | property | 戦術能力。与ダメージ補正に影響 |
| `Courage` | property | 武勇。接近戦追加ダメージに影響 |
| `BattleWins` | property | 戦闘勝利回数。成長システムで使用 |
| `Personality` | property | 武将の性格。AI意思決定の基盤 |

---

## OfficerPersonality
武将の性格列挙型。

| 値 | 説明 |
|---|---|
| `Brave` | 勇猛。攻撃命令を好み、撤退進言をしない |
| `Cautious` | 慎重。兵力不足時に撤退を進言しやすい |
| `Ambitious` | 野心的。忠誠が低いと独断行動を取りやすい |
| `Loyal` | 忠義。忠誠が低くても命令に従いやすい |
| `Opportunist` | 日和見。勝ち馬に乗る。勢力が劣勢だと離反しやすい |

---

## Army : Entity
軍隊エンティティ。勢力に属しHexマップ上を移動して戦闘を行う。

| メンバー | 種別 | 説明 |
|---|---|---|
| `OfficerId` | property | 指揮官のID。未配属の場合はnull |
| `ClanId` | property | 所属勢力のID |
| `CurrentHexId` | property | 現在いるHexのID |
| `Soldiers` | property | 兵力。0になると全滅 |
| `DestinationHexId` | property | 移動目標HexのID。nullの場合は待機中 |
| `MoveCooldown` | property | 移動クールダウン。Forest進入時に1がセットされる |
| `ActionPoints` | property | 行動力（AP）。移動1Hex=1AP消費 |
| `MaxActionPoints` | const | 1ターンあたりの最大行動力（=2） |
| `MaxSoldiers` | property | 兵力の上限 |
| `MoveTo(int)` | method | 指定HexへArmyを瞬間移動させる |
| `Arrive()` | method | 目的地到着時にDestinationHexIdをクリア |
| `OrderMove(int)` | method | 移動命令を設定する |
| `LoseSoldiers(int)` | method | 兵力を減らす。0未満にはならない |
| `ClearDestination()` | method | 移動目標をクリアして待機状態にする |
| `ResetActionPoints()` | method | APを最大値にリセットする。ターン開始時に呼ぶ |
| `AssignOfficer(int)` | method | 指揮官を配属する |
| `Reinforce(int)` | method | 兵力を補充する。MaxSoldiersを超えない |
| `SetInitialSoldiers(int)` | method | 初期兵力を設定する。ScenarioLoaderから呼ぶ |
| `SetSoldiers(int)` | method | 兵力を直接セットする。SaveSystemのロード時専用 |
| `Defect(int)` | method | 軍が離反し新しい勢力へ移る |

---

## Castle : Entity
城・拠点エンティティ。占領した勢力に毎Tick兵力補充ボーナスを与える。

| メンバー | 種別 | 説明 |
|---|---|---|
| `HexId` | property | 城が配置されているHexのID |
| `Name` | property | 城の名前 |
| `OwnerClanId` | property | 現在の占領勢力ID。0=中立 |
| `ReinforcementPerTick` | property | 毎Tick補充する兵力 |

---

## Alliance : Entity
2勢力間の同盟。RemainingTicksが0になるとDiplomacySystemが解消する。

| メンバー | 種別 | 説明 |
|---|---|---|
| `ClanId1` | property | 同盟に関与する勢力ID（1） |
| `ClanId2` | property | 同盟に関与する勢力ID（2） |
| `RemainingTicks` | property | 同盟の残り有効Tick数 |
| `Involves(int)` | method | 指定した勢力IDが同盟に関与しているか返す |

---

## Relationship : Entity
武将間の人間関係エンティティ。

| メンバー | 種別 | 説明 |
|---|---|---|
| `FromOfficerId` | property | 関係の起点となる武将ID |
| `ToOfficerId` | property | 関係の対象となる武将ID |
| `Trust` | property | 信頼度。高いほど協力・援護行動を取りやすい |
| `Respect` | property | 尊敬度。命令への従順さに影響 |
| `Dislike` | property | 反感度。高いほど対立・妨害行動を取りやすい |

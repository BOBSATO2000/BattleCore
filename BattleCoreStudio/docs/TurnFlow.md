# 1ターン実行フロー・アルゴリズム解説

## 概要

UIが `SimulationEngine.Step()` を1回呼ぶと1ターンが完結する。  
内部では以下の順序で12のSystemが順番に実行される。

```
SimulationEngine.Step()
│
├─ [前処理] 全軍のAP リセット
│
├─ 1. CastleSystem.Update()
├─ 2. ClanDecisionSystem.Update()
├─ 3. CommandExecutionSystem.Update()
├─ 4. MovementSystem.Update()
├─ 5. BattleSystem.Update()
├─ 6. LoyaltySystem.Update()
├─ 7. RecruitmentSystem.Update()
├─ 8. SupplySystem.Update()
├─ 9. RelationshipSystem.Update()
├─ 10. DiplomacySystem.Update()
├─ 11. EventTriggerSystem.Update()
├─ 12. VictorySystem.Update()
│
└─ [後処理] GameTime.Advance() → WorldState.Weather 同期
```

---

## 前処理：全軍のAP リセット

**呼び出し:** `Army.ResetActionPoints()` × 全Army数

各Armyの `ActionPoints` を `MaxActionPoints`（=2）に戻す。  
これにより毎ターン最大2Hex移動できる状態になる。

---

## 1. CastleSystem.Update()

**目的:** 城の占領判定と兵力補充

```
全城をループ
  └─ 城のHexにいる生存Army一覧を取得
       ├─ Armyがいない → スキップ
       ├─ 複数勢力が混在 → 戦闘中のためスキップ
       └─ 1勢力のみ
            ├─ 城の所有者と異なる → 占領（OwnerClanId更新 + CastleCapturedEvent）
            └─ 占領勢力のArmyに ReinforcementPerTick 分補充
```

**ポイント:**  
複数勢力が同Hexにいる場合は占領をスキップする。戦闘解決（BattleSystem）より前に実行されるため、戦闘中の城は占領されない設計になっている。

---

## 2. ClanDecisionSystem.Update()

**目的:** AI勢力の命令生成（2層構造）

```
全Clanをループ
  └─ IsPlayerControlled == true → スキップ
  └─ Layer1: IClanStrategy.Decide(clan, world)
       └─ AggressiveClanStrategy の場合:
            全Armyをループ
              ├─ 視界内に敵がいない → 最寄り敵城方向へ索敵前進
              ├─ 同Hexに敵がいる → 移動命令なし（BattleSystemに任せる）
              ├─ 兵力 <= RetreatThreshold → 自勢力の最寄り城へ撤退
              └─ 視界内に敵あり → 敵城 or 敵軍の近い方へ進軍
            → MoveArmyCommand を yield return
  └─ Layer2: OfficerDecision.Evaluate(commands, clan, world)
       各命令をループ
         ├─ ① 忠誠 <= RefusalLoyaltyThreshold.Roll()  かつ非Loyal性格 → 命令拒否（OfficerRefusedOrderEvent）
         ├─ ② Cautious性格 かつ 兵力 <= CautiousRetreatSoldiers.Roll() → 撤退進言（撤退命令に差し替え）
         ├─ ③ Ambitious性格 かつ 忠誠 <= IndependentActionLoyalty.Roll() → 独断行動（最寄り敵城へ）
         ├─ ④ 主君へのDislike >= DissatisfiedDislikeThreshold.Roll() → 不満撤退（撤退命令に差し替え）
         ├─ ⑤ 上記以外 → 元の命令をそのまま通す
         └─ ⑥ SpontaneousEventTable が注入済み → Evaluate() で突発イベントを評価し EventQueue に積む
       → DecisionResult を返す
  └─ Accepted == true → CommandQueue.Enqueue(command)
  └─ Event != null → EventQueue.Enqueue(event)
  └─ Explanation != null → EventQueue.Enqueue(DecisionExplanationEvent)
```

**ポイント:**  
Layer1（戦略）とLayer2（武将意思）を分離することで、「勢力として何をしたいか」と「この武将は従うか」を独立して変更できる。

---

## 3. CommandExecutionSystem.Update()

**目的:** CommandQueueの命令を全て実行する

```
CommandQueue が空になるまでループ
  └─ CommandQueue.Dequeue()
       └─ MoveArmyCommand.Execute(context)
            └─ WorldState.GetArmyById(armyId)
                 └─ Army.OrderMove(destinationHexId)
                      → Army.DestinationHexId をセット
```

**ポイント:**  
この時点では「目的地をセットするだけ」で実際の移動は行わない。移動はMovementSystemが担当する。

---

## 4. MovementSystem.Update()

**目的:** ArmyをDestinationHexIdへ1Hexずつ移動させる

```
全Armyをループ
  ├─ Soldiers == 0 → DestinationHexId クリアしてスキップ
  ├─ ActionPoints == 0 → スキップ（AP不足）
  ├─ MoveCooldown > 0 → Cooldown-- してスキップ
  ├─ DestinationHexId == null → スキップ（待機中）
  └─ GetNextStep() で次のHexを決定
       ├─ 隣接Hexに目的地がある → 直接移動
       └─ ない場合 → 目的地方向に最もHexDistance が小さい隣接Hexへ
  ├─ 次のHexがMountain → スキップ（移動不可）
  └─ Army.MoveTo(next.Id)
       Army.ActionPoints--
       ├─ Forest進入 → MoveCooldown = 1（雨天時は2）
       └─ 目的地到着 → DestinationHexId クリア + MovementEvent
```

**ポイント:**  
- 1ターンに最大2Hex移動できる（MaxActionPoints=2）
- Forest進入時はMoveCooldownが1セットされ次ターンは移動スキップ
- 雨天時のForest進入はCooldown=2（2ターン足止め）
- 経路探索はA*（HexPathFinder）ではなくグリーディ（最近傍）で1ステップずつ進む

---

## 5. BattleSystem.Update()

**目的:** 同Hexにいる異なる勢力の軍を戦闘させる

```
BattleFinder.Find(world)
  └─ 全ArmyをCurrentHexIdでグループ化
       └─ 同Hex内の全Army組み合わせをループ
            └─ ClanIdが異なる かつ 同盟中でない → Battle(attacker, defender) を生成

各Battleをループ
  └─ BattleResolver.Resolve(battle, world)
       ├─ 攻撃側・防御側のOfficerを取得
       ├─ 防御側Hexの地形・城の有無を確認
       ├─ DamageCalculator.Calculate(...)
       │    ├─ 実効兵力 = 兵力 × Clamp(Leadership/100, 0.5, 1.5)
       │    ├─ 実効兵力が多い方が勝者
       │    ├─ 勝者損害 = 敗者兵力/3 × Strategy補正 × Courage補正
       │    ├─ 敗者損害 × 地形係数（Forest:0.8 / Mountain:0.7）
       │    ├─ 雨天時 → 双方損害 × 0.9
       │    └─ 城ボーナス → 敗者損害 × 0.8
       ├─ Trust >= 60 → 勝者損害 × 0.95（5%軽減）
       ├─ Attacker.LoseSoldiers() / Defender.LoseSoldiers()
       └─ 勝者Officerの成長
            ├─ BattleWins++
            ├─ 奇数回目 → Leadership +1
            └─ 偶数回目 → Strategy +1
  └─ BattleLogEvent を EventQueue に積む
```

**ダメージ計算式まとめ:**

| 項目 | 計算式 |
|---|---|
| 実効兵力 | `兵力 × Clamp(Leadership/100, 0.5, 1.5)` |
| 勝敗判定 | 実効兵力が多い方が勝者 |
| 勝者損害 | `敗者兵力/3 × Clamp(Strategy/100, 0.7, 1.3) × Clamp(Courage/100, 0.9, 1.1)` |
| 敗者損害（地形） | Forest: ×0.8 / Mountain: ×0.7 / Plain: ×1.0 |
| 雨天補正 | 双方 ×0.9 |
| 城ボーナス | 敗者損害 ×0.8 |
| Trustボーナス | 勝者損害 ×0.95（Trust>=60の場合） |

---

## 6. LoyaltySystem.Update()

**目的:** 忠誠変動と裏切り判定

```
① 忠誠変動（全Membershipをループ）
  └─ 所属勢力のArmyを確認
       ├─ 全滅 → Loyalty -= LossLoyaltyPenalty(5)
       └─ 生存あり → Loyalty += WinLoyaltyBonus(3)
  └─ 季節が春 → Loyalty += SpringBonus(2)
  ※ Loyaltyは0〜100にClamp

② 裏切り判定（全Membershipをループ）
  └─ 裏切りスコア計算
       score = Ambition - Loyalty/2 - Membership.Loyalty/2 + 主君へのDislike合計
  └─ score >= BetrayalThreshold(80)
       ├─ Membership を削除
       ├─ 指揮するArmyをDefect（ClanId=0に変更）
       ├─ 同勢力の他武将のLoyalty -= ChainBetrayalLoyaltyDrop(10)（連鎖離反）
       └─ BetrayalEvent を EventQueue に積む
```

**ポイント:**  
裏切りスコアは「野心が高く、忠誠が低く、主君を嫌っているほど高くなる」設計。  
連鎖離反により1人の裏切りが他の武将の忠誠も下げる。

---

## 7. RecruitmentSystem.Update()

**目的:** 無所属武将（ClanId=0）を最寄り勢力へ仕官させる

```
ClanId=0 かつ Soldiers>0 かつ OfficerId有りのArmyを抽出（浪人リスト）
各浪人をループ
  ├─ Ambition < RecruitAmbitionThreshold(30) → スキップ（仕官を急がない）
  └─ 生存Armyを持つ勢力の中で最寄りのArmyを探す
       └─ 距離計算: Math.Abs(CurrentHexId差)（簡易計算）
       └─ 仕官処理
            ├─ Membership 追加（Loyalty=50）
            ├─ Army.Defect(targetClanId)
            └─ RecruitEvent を EventQueue に積む
```

**ポイント:**  
距離計算はHexIdの差の絶対値による簡易計算。HexPathFinderは使用しない。

---

## 8. SupplySystem.Update()

**目的:** 勢力に所属するArmyの兵力を毎ターン補充する

```
全Armyをループ
  ├─ ClanId==0（無所属）→ スキップ
  ├─ Soldiers==0（全滅）→ スキップ
  ├─ Soldiers >= MaxSoldiers → スキップ
  └─ 補充量計算
       amount = BaseReplenishment(50)
       └─ 春 → amount += SpringBonus(30)
  └─ Army.Reinforce(gain)
  └─ gain >= EventThreshold(200) → SupplyEvent を EventQueue に積む
```

**ポイント:**  
全滅したArmyは補充されない（再建なし）。城のCastleSystemによる補充とは別の自然回復だが、全滅（Soldiers==0）はスキップされるため再建は起きない。

---

## 9. RelationshipSystem.Update()

**目的:** 武将間の関係値（Trust/Respect/Dislike）を更新する

```
① 同Hex共闘・敵対（同Hexの全Army組み合わせをループ）
  ├─ 同勢力 → Trust += 1（双方）
  │           兵力多い方への Respect += 1
  └─ 異勢力 → Dislike += 2（双方）

② 放置（同勢力なのに遠い）
  同勢力Army組み合わせで HexDistance >= 4 → Respect -= 1（双方）

③ 戦闘結果（EventQueueのBattleLogEventを参照）
  ├─ 勝者→敗者の Dislike += 3（戦場での恨み）
  └─ 敗者→主君の Dislike += 2（敗戦の不満）

④ RelationTriggerによる突発イベント評価
  _relationTriggers が空 → スキップ
  全Relationshipをループ
    全RelationTriggerをループ
      ├─ Condition(rel) == false → スキップ
      ├─ rng.NextSingle() >= Probability → スキップ（確率判定）
      └─ 通過 → EventFactory(fromOfficer, toOfficer) を呼び EventQueue に積む
```

---

## 10. DiplomacySystem.Update()

**目的:** 同盟の期限管理とAI自動同盟締結

```
① 同盟期限チェック
  全Allianceをループ
    └─ RemainingTicks-- → 0未満になったら同盟解消 + ScenarioEvent

② AI自動同盟（AutoAllianceInterval(10)ターンごとに評価）
  生存勢力の全ペアをループ
    ├─ 既に同盟中 → スキップ
    ├─ 共通の敵が存在しない → スキップ
    ├─ 兵力比が 0.4〜2.5 の範囲外 → スキップ（極端な差は同盟しない）
    └─ 同盟締結（AutoAllianceDuration(15)ターン）+ ScenarioEvent
```

---

## 11. EventTriggerSystem.Update()

**目的:** シナリオイベントトリガーの条件評価と発火

```
全トリガーをループ
  ├─ 発火済み（fired セット内）→ スキップ
  ├─ Tick < MinTick → スキップ
  └─ 条件チェック（全てAND条件）
       ├─ OfficerId 指定あり → 対象武将が存在するか
       ├─ MinDislike 指定あり → 対象武将のDislike合計が閾値以上か
       ├─ MaxLoyalty 指定あり → 対象武将のLoyaltyが閾値以下か
       └─ AdjacentToOfficerId 指定あり → 2武将の軍が隣接Hexにいるか
  └─ 全条件クリア → fired に追加 + ScenarioEvent を EventQueue に積む
```

---

## 12. VictorySystem.Update()

**目的:** 勝利条件の判定

```
gameOver == true → 即リターン（二重発火防止）

兵力 > 0 かつ ClanId != 0 の勢力IDを収集
  ├─ 1勢力のみ → 天下統一 → GameOverEvent(winnerClanId)
  ├─ 0勢力 → 全滅 → GameOverEvent(null)
  └─ 2勢力以上 → 何もしない
```

---

## 後処理：GameTime.Advance()

**目的:** ゲーム内時間を1ステップ進める

```
Tick++

季節を進める（4ステップで1年）
  Spring → Summer → Autumn → Winter → Spring（Year++）

天気を確率で更新
  Sunny → Rain:  25%
  Rain  → Fog:   20%
  Fog   → Sunny: 50%
  それ以外は現状維持

WorldState.Weather = GameTime.Weather に同期
```

---

## データフロー図

```
CommandQueue ←── ClanDecisionSystem（AI命令生成）
     │
     ▼
CommandExecutionSystem（Army.DestinationHexId をセット）
     │
     ▼
MovementSystem（1Hexずつ移動）
     │
     ▼
BattleSystem（同Hexで戦闘 → 兵力更新）
     │
     ▼
EventQueue ←── BattleLogEvent / MovementEvent / BetrayalEvent / ...
     │
     ▼
UI（EventQueueを読んでログ・演出を表示）
```

# BattleCore Studio

戦国シミュレーションエンジン + WinForms UI。C# / .NET 10 製。

## 機能一覧

### コアシステム
| Stage | 機能 | 概要 |
|---|---|---|
| 2f | RelationshipSystem | 同Hex同勢力→Trust+1、異勢力→Dislike+2 |
| 2g | TrustBonus | Trust≥60で勝者損害5%軽減 |
| 4 | ScenarioLoader | JSONからWorldStateを構築 |
| 5 | DiplomacySystem | 同盟管理・AI自動同盟 |
| 6 | EventTriggerSystem | 条件付きイベント発火 |
| 9 | VictorySystem | 天下統一・全滅判定 |
| 10 | OfficerGrowth | 戦闘勝利でLeadership/Strategy交互+1（上限200） |
| 11 | TerrainBonus | Forest-20%・Mountain-30% 防御補正 |
| 12 | MovementCost | Forest進入+1Tick、Rain時+2Tick |
| 13 | WeatherSystem | Sunny/Rain/Fog 毎Tick確率遷移 |
| 14 | BattleLog | 地形・天気・成長をイベントログに詳細表示 |
| 15 | CastleSystem | 城占領・毎Tick補充・防御ボーナス-20% |
| 16 | CastleAI | 敵城優先進軍・自城への撤退 |

### シナリオ（4本）
| ファイル | タイトル | 勢力 | 城数 |
|---|---|---|---|
| sengoku1560.json | 桶狭間前夜 1560 | 織田・武田・上杉 | 4 |
| kawanakajima1561.json | 川中島の戦い 1561 | 上杉・武田 | 2 |
| sekigahara1600.json | 関ヶ原 1600 | 東軍・西軍 | 5 |
| osaka1615.json | 大坂夏の陣 1615 | 豊臣・徳川 | 5 |

### UI
- Hexマップ描画（地形色・城アイコン・軍隊駒・移動矢印・凡例）
- 勢力概要パネル（兵力・軍数・城数・同盟状態）
- 軍隊リスト（ダブルクリックで武将詳細ポップアップ）
- イベントログ（戦闘詳細・占領・離反・シナリオイベント）
- シナリオ選択ダイアログ・オート進行・リスタート

## テスト
88テスト全通過（MSTest）

## 技術スタック
- C# / .NET 10
- WinForms（UI）
- System.Text.Json（シナリオ読み込み）
- MSTest（テスト）

## ビルド方法
```
cd BattleCoreStudio
dotnet build
dotnet run
```

## テスト実行
```
cd BattleCore.Tests
dotnet test
```

## リポジトリ
https://github.com/BOBSATO2000/BattleCore

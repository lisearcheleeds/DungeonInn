# Milestone 3 Roadmap

このドキュメントは Milestone 3 の実装計画をまとめる。

対象ループ:

```text
冒険者が来訪する
-> ダンジョンへ向かう（複数フロア対応）
-> モンスターと戦闘・経験値獲得・レベルアップ
-> アイテムを拾う
-> 帰還する
-> ギルドでアイテムを売却・装備を更新する
-> 宿屋で回復する（満室・宿泊費の処理を含む）
-> 目的を達成した冒険者はデスポーンする
```

## 到達目標

- 冒険者が成長し、強くなるほどより深い階層へ到達できる
- アイテム経済が循環している（ドロップ → 拾得 → 売却 → 宿泊費支払い）
- 満室・資金不足などの例外ケースが適切にハンドリングされる
- 一定の目的を達成した冒険者が自然にデスポーンする

## 前提

- Milestone 2 のゲームループが安定して動作していること
- View 表示は引き続き不要（ログで確認）
- NavMesh は使わない（Milestone 5 で対応）

---

## Phase 1: アイテムマスターデータ定義

以降の全フェーズの基盤となるアイテムドメインを定義する。

注: マスタデータは将来的に MasterMemory を利用する予定。現時点では `HardcodedMasterRepository` にハードコードし、後で差し替えやすい構造を保つ。Addressables は使用しない。

作るもの:

- `ItemMaster`（アイテム定義: ID, 名前, タイプ, 売却価格, 装備ステータス）
  - 既存の `ItemMaster`（id, name, ItemCategory, basePrice, quality, canTrade）は概ね対応済み
  - `ItemCategory`（Material / Consumable / Equipment）が `ItemType` の役割を担う
  - 装備ステータスは `EquipmentMaster.Defense` + `WeaponMaster` で既に表現済み
- `IItemMasterRepository`（`IMasterRepository` からアイテム参照に特化した部分インターフェース）

完了条件:

- `IItemMasterRepository` が定義され、`HardcodedMasterRepository` が実装していること
- Equipment / Material それぞれのマスターが最低1件定義されていること

---

## Phase 2: ゴールド / 通貨

冒険者の財布と宿泊費支払いの基盤を作る。

作るもの:

- `Actor.Gold`（所持金フィールドまたは Value Object）
- `AdventurerGuild.Treasury`（宿屋収入の記録）
- `ChargeInnFeeUseCase`（宿泊費の徴収）

初期仕様:

- 冒険者は初期ゴールドを持った状態でスポーンする
- 宿屋予約時に宿泊費を徴収する
- 残高不足の場合は予約を取れず、宿屋待機には入らずに準備状態へ戻す

完了条件:

```text
[Inn] Adventurer A paid 10G for inn room (remaining: 90G)
[Guild] Treasury +10G (total: 10G)
```

---

## Phase 3: 経験値

モンスターを倒したとき冒険者に経験値を付与する。

作るもの:

- `Actor.Experience`（現在XP / 次のレベルまでのXP）
- 撃破された `Actor.Experience` に基づく経験値報酬
- `GrantExperienceUseCase`

接続:

- `AdvanceCombatUseCase` が `ActorDefeated` を受けて呼び出す、または `DecideAdventurerReturnUseCase` の前に呼ぶ

完了条件:

```text
[Growth] Adventurer A gained 30 EXP (total: 30/100)
```

---

## Phase 4: レベルアップ

経験値が閾値を超えたらレベルアップし、ステータスが成長する。

作るもの:

- `LevelUpUseCase`
- `LevelTable`（レベルごとの必要XPテーブル）
- `ActorParams` の成長反映

完了条件:

```text
[Growth] Adventurer A leveled up! Lv1 -> Lv2 (ATK: 10 -> 12, DEF: 5 -> 6)
```

---

## Phase 5: モンスタードロップ

モンスター討伐時にアイテムインスタンスをダンジョン内に生成する。

作るもの:

- `ActorDropEntry`（アイテムID, ドロップ確率 [0.0–1.0], 個数範囲 [min, max]）
  - 既存の `ItemStack` ベースの `SpeciesDrops` を `IReadOnlyList<ActorDropEntry>` に置き換える
  - `SpeciesMaster.SpeciesDrops` の型を `IReadOnlyList<ActorDropEntry>` として扱う
- `ItemInstance`（ドロップしたアイテムの実体: インスタンスID, マスターID, 位置, フロアIndex）
- `GameWorldState.Items`（フロア上のアイテムインスタンス一覧）
- `DropItemUseCase`

接続:

- `AdvanceCombatUseCase` がモンスター討伐時に呼び出す

完了条件:

```text
[Drop] Slime dropped Iron Sword at Floor 1 (12.5, 8.3)
[Drop] Slime dropped 15G at Floor 1 (12.5, 8.3)
```

---

## Phase 6: アイテム拾得

探索中の冒険者が近くのアイテムを拾う。

作るもの:

- `PickUpItemUseCase`

初期仕様:

- 探索中（`Exploring` 状態）のみ拾得対象
- 同フロアかつ一定距離以内のアイテムを自動拾得
- インベントリに空きがなければ拾わない（後フェーズで上限実装）

完了条件:

```text
[Item] Adventurer A picked up Iron Sword
[Item] Adventurer A picked up 15G (wallet: 105G)
```

---

## Phase 7: インベントリ整理

冒険者がアイテムを管理できるインベントリを実装する。

作るもの:

- `Inventory`（所持アイテムリスト, 上限スロット数）
- `Actor.Inventory`
- インベントリ超過時の自動ドロップまたは拾得スキップ

初期仕様:

- スロット上限は固定（例: 10スロット）
- 同種アイテムのスタック対応は最小限

完了条件:

- インベントリが満杯の状態でアイテムを拾おうとしたときにスキップされること

---

## Phase 8: 装備更新

帰還後、インベントリ内により強い装備があれば自動的に装備を更新する。

作るもの:

- `EquipmentSlots`（武器, 防具など）
- `UpdateEquipmentUseCase`
- `ActorParams` への装備ボーナス反映

初期仕様:

- 帰還して `Recovering` 状態になったタイミングで自動実行
- 現在の装備より ATK or DEF が高いものがあれば差し替え
- 外した装備はインベントリに戻る

完了条件:

```text
[Equip] Adventurer A equipped Iron Sword (ATK: 8 -> 14)
```

---

## Phase 9: 売買

帰還後、インベントリ内の不要アイテムをギルドに売却してゴールドを得る。

作るもの:

- `SellItemsUseCase`

初期仕様:

- 帰還して `Recovering` 状態になったタイミングで自動実行
- 装備中でなく Material / 低品質 Equipment を売却
- 売却額は `ItemMaster.BasePrice` と `PricePolicy` に基づく

完了条件:

```text
[Shop] Adventurer A sold Herb x2 for 20G (wallet: 125G)
```

---

## Phase 10: 複数フロア探索

ダンジョンを複数フロアに拡張し、冒険者がより深い階層を目指す。

作るもの:

- `InitializeDungeonUseCase` の複数フロア対応
- `AdvanceActorSimpleLifecycleUseCase` のフロア間移動ロジック拡張
- `AdventurerBehavior.TargetFloorDepth`（目指す最深階）

初期仕様:

- 地下1〜3階を初期生成
- 冒険者のレベルに応じて目標フロアを決定（Lv1→1F, Lv3→2F, Lv5→3F など）
- 上り/下り階段を使って移動

完了条件:

```text
[Actor] Adventurer A (Lv3) descended to Floor 2
[Actor] Adventurer A (Lv3) ascending to Floor 1
```

---

## Phase 11: 探索目的の詳細化

冒険者が単純なループではなく、目的を持って探索する。

作るもの:

- `AdventurerGoal`（KillTarget, ReachFloor, CollectItem など）
- `DecideAdventurerReturnUseCase` の拡張（目的達成判定）

初期仕様:

- `KillTarget`: 特定種のモンスターを指定数倒す
- `ReachFloor`: 指定フロアへ到達する
- 目的達成で帰還フラグが立つ

完了条件:

```text
[Goal] Adventurer A completed goal: Kill 3 Slimes (2/3 -> 3/3)
[Actor] Adventurer A starts returning (goal achieved)
```

---

## Phase 11.5: Actor / Species マスタ整理

Phase 11 の探索目的詳細化で、討伐対象や Actor 識別情報を扱うようになったため、Phase 12 に進む前に Actor / 種族まわりのマスタ責務を整理する。

背景:

- 現状、冒険者は主に `ActorArchetypeMaster` を参照し、モンスターは `MonsterSpeciesMaster` と `ActorArchetypeMaster` の両方を参照している
- `MonsterSpeciesMaster` は実質的にモンスター専用の追加 Actor 定義になっており、`ActorArchetypeMaster` と情報・責務が重複している
- 種族はモンスター専用概念ではなく、冒険者にも人間・エルフ・ドワーフなどの種族を持たせられる
- 討伐目標判定では、Actor が削除された後も `ActorProfileRegistry` から種族情報を参照できる状態にしたい

作るもの:

- `SpeciesMaster`
  - `Id`
  - `Name`
  - `SpeciesDrops`
- `ActorArchetypeMaster` の拡張
  - `SpeciesId`
  - `DefaultWeaponType`
- `AdventurerSpawnMaster` の追加
  - 固有名
  - `ActorArchetypeId`
  - 一度だけスポーンするか
- `MonsterSpeciesMaster` の廃止
- `SpawnTableMaster` の生成対象を、冒険者は `AdventurerSpawnMaster`、モンスターは `ActorArchetypeMaster` に整理
- `ActorProfileRegistry` に登録する識別情報を、Actor 削除後も討伐目標判定に使える形へ整理

初期仕様:

- `SpeciesMaster` は種族固有情報だけを持つ
- `ActorArchetypeMaster` は実際に生成される Actor のテンプレートを表す
- 種族ドロップは `SpeciesMaster.SpeciesDrops` に集約する
- 自然武器・初期武器種としての `DefaultWeaponType` は `ActorArchetypeMaster` 側に持たせる
- 冒険者・モンスターのどちらも `ActorArchetypeMaster.SpeciesId` を持つ
- 冒険者の固有名は `AdventurerSpawnMaster.DisplayName` に持たせる
- モンスター生成時は `ActorArchetypeMaster` と `SpeciesMaster` を参照して Actor を作成する

完了条件:

- `MonsterSpeciesMaster` への参照がなくなっていること
- 冒険者とモンスターの生成経路が、どちらも `ActorArchetypeMaster` を生成単位として扱っていること
- 冒険者スポーンが `AdventurerSpawnMaster` から固有名と `ActorArchetypeMaster` を解決していること
- 討伐目標の判定が、削除済み Actor でも `ActorProfileRegistry` に残った種族情報から行えること
- `uloop.cmd compile --project-path Client` が成功すること
- EditMode テストが成功すること

---

## Phase 12: 宿屋満室時の詳細挙動

宿屋が満室の場合と、宿泊費不足の場合の冒険者の挙動を実装する。

作るもの:

- `AdventurerBehavior` への待機状態追加（`WaitingForInn`）
- `AdvanceActorSimpleLifecycleUseCase` の待機ループ

初期仕様:

- 満室の場合 `WaitingForInn` に入る
- 残金不足の場合は宿屋待機には入らず、`Preparing` に戻って再準備する
- 一定 tick ごとに再試行
- 空きが出たら予約を取り `Recovering` へ遷移
- 長時間待機でHPが減らない（ただし再探索もしない）

完了条件:

```text
[Inn] Adventurer B waiting for inn vacancy (all 2 rooms occupied)
[Inn] Adventurer B reserved inn room (Adventurer A checked out)
```

---

## Phase 13: 冒険者の旅立ち / デスポーン

宿屋を手配できない状態が一定期間続いた冒険者が宿を去る。

作るもの:

- `DespawnAdventurerUseCase`
- `AdventurerBehavior.WaitingForInnStartedDay`（宿屋手配待ち開始日）

初期仕様:

- 宿屋を手配できない状態で 3 日（ゲーム内時間）経過したら旅立つ
- 宿屋を確保できている間は旅立たない
- 宿泊費は宿屋に入室するときに 1 日分を支払う
- 宿泊費を払えない場合は宿屋に入れないため、再度ダンジョンに突入するなど別の選択肢へ戻る
- 旅立ち時に宿泊費の追加精算は行わない
- `GameWorldState.RemoveActor` でデスポーン

完了条件:

```text
[Guild] Adventurer A departed after waiting 3 days for inn
[Spawn] Adventurer C spawned (replacing departed adventurer)
```

---

## 推奨実装順

1. Phase 1: アイテムマスターデータ定義
2. Phase 2: ゴールド / 通貨
3. Phase 3: 経験値
4. Phase 4: レベルアップ
5. Phase 5: モンスタードロップ
6. Phase 6: アイテム拾得
7. Phase 7: インベントリ整理
8. Phase 8: 装備更新
9. Phase 9: 売買
10. Phase 10: 複数フロア探索
11. Phase 11: 探索目的の詳細化
12. Phase 11.5: Actor / Species マスタ整理
13. Phase 12: 宿屋満室時の詳細挙動
14. Phase 13: 冒険者の旅立ち / デスポーン

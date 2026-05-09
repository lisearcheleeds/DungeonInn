# Minimum Playable Game Loop Roadmap

このドキュメントは、DungeonInn を「ログ上でゲームとして動いている」と確認できる最小ループまで進めるためのロードマップをまとめる。

対象ループ:

```text
冒険者が来訪する
-> ダンジョンへ向かう
-> モンスターと戦闘する
-> 帰還する
-> 宿屋で回復する
-> 再びダンジョンへ向かう
```

レベルアップ、アイテム拾得、ドロップ、売買、装備更新、探索目的の高度化は後回しにする。

## 到達目標

まずはView表示なしでよい。ログで以下のような流れが確認できれば、最小ゲームループ到達とする。

```text
[Spawn] Adventurer A spawned
[Spawn] Monster Slime spawned at Floor 1
[Actor] Adventurer A entered dungeon floor 1
[Combat] Adventurer A attacked Slime for 8
[Combat] Slime attacked Adventurer A for 3
[Combat] Slime defeated
[Actor] Adventurer A returned to ground
[Inn] Adventurer A recovering HP 32/50
[Inn] Adventurer A fully recovered
[Actor] Adventurer A entered dungeon floor 1
```

## 前提

- UnityのView表示は不要。
- NavMeshは使わない。
- 移動はApplication / Domain座標の簡易移動でよい。
- `Time.timeScale` は使わない。
- ゲーム進行は `GameClock` の専用 `TimeScale` と `ElapsedGameTimeSeconds` を使う。
- 低頻度処理は `CurrentScheduleTick` を使ってよい。
- 戦闘や回復など秒以下の精度が必要な処理は `float seconds` で扱う。

## Phase 1: GameWorldState 初期化

World開始時に、ゲームランに必要な最低限のDomain状態を生成して `GameWorldState` に保持する。

作るもの:

- `InitializeGameWorldUseCase`

処理内容:

- `InitializeWorldMapUseCase` で `GroundMap` を生成する
- `InitializeDungeonUseCase` で `Dungeon` と地下1階を生成する
- `AdventurerGuild` を生成する
- 宿屋Facilityを最低1つ生成する
- `GameWorldState.Initialize(...)` を呼ぶ

完了条件:

- World開始ログで `GameWorldState initialized` が出る
- `GroundMap`, `Dungeon`, `AdventurerGuild` がnullでない

## Phase 2: 冒険者スポーン

一定間隔で冒険者を生成し、`GameWorldState` に登録する。

作るもの:

- `SpawnScheduledAdventurerUseCase`
- 必要なら `AdventurerSpawnScheduler`

初期仕様:

- `CurrentScheduleTick` を使って数秒ごとに1人生成
- Lv1固定または既存Masterから固定IDで生成
- 生成位置は地上のダンジョン入口付近
- `GameWorldState.RegisterActor()` に登録

完了条件:

```text
[Spawn] Adventurer A spawned
[WorldGameLoop] Actors=1 Adventurers=1 Monsters=0 ...
```

## Phase 3: モンスタースポーン

地下1階にモンスターを生成する。

作るもの:

- `SpawnInitialMonstersUseCase`

初期仕様:

- World初期化時に固定数を生成
- 生成位置は地下1階の通路またはRoom
- 種族は固定でよい
- `GameWorldState.RegisterActor()` に登録

完了条件:

```text
[Spawn] Monster Slime spawned at Floor 1
[WorldGameLoop] Actors=4 Adventurers=1 Monsters=3 ...
```

## Phase 4: 冒険者の簡易ライフサイクル

既存AIを完全に使い切る前に、最小の状態遷移でゲームループを成立させる。

作るもの:

- `AdvanceActorSimpleLifecycleUseCase`

初期状態遷移:

```text
Arrived
-> GoingToDungeon
-> Exploring
-> Returning
-> Recovering
-> Preparing
-> GoingToDungeon
```

方針:

- まずはAdventurerのみ対象
- Monster / Pet / GuildStaff はこのUseCaseでは扱わない
- 既存の `AdventurerLifecycleState` で表現できる範囲は既存値を使う
- 足りない状態があれば、暫定的にApplication側のRuntime Stateとして持つ

完了条件:

```text
[Actor] Adventurer A lifecycle Arrived -> GoingToDungeon
```

## Phase 5: 簡易移動

View / NavMeshなしで、Actorを目的地へ近づける。

作るもの:

- `MoveActorTowardDestinationUseCase`

初期仕様:

- `LayerPosition` 同士の直線移動
- 移動速度は固定値
- `deltaGameSeconds` を使って移動距離を計算
- 到着距離以下になったら目的地到着扱い
- 壁判定は最初は最小限でよい

接続:

- 地上ではダンジョン入口へ向かう
- ダンジョン内ではモンスターまたは帰還先階段へ向かう

完了条件:

```text
[Move] Adventurer A moved toward dungeon entrance
[Move] Adventurer A arrived at dungeon entrance
```

## Phase 6: ダンジョン入退場

地上と地下1階を行き来できるようにする。

使うもの:

- `UseDungeonStairUseCase`

初期仕様:

- 地上のDungeonEntranceに着いたら地下1階へ移動
- 帰還時は地下1階の上り階段へ移動
- 上り階段に着いたら地上へ戻る

完了条件:

```text
[Actor] Adventurer A entered dungeon floor 1
[Actor] Adventurer A returned to ground
```

## Phase 7: 戦闘遭遇

冒険者とモンスターが遭遇したら戦闘状態に入る。

作るもの:

- `DetectCombatEncounterUseCase`

初期仕様:

- 同じDungeonFloorにいる
- 敵対Factionである
- 20m以内
- ダンジョン壁によるLine of Sight判定（`DetectCombatEncounterUseCase` 内に実装済み）

完了条件:

```text
[Combat] Adventurer A encountered Slime
```

## Phase 8: 戦闘処理

Direct攻撃のみで戦闘を進める。

作るもの:

- `AdvanceCombatUseCase`
- `ActorCombatState` / `ActorCombatService`（per-Actorの戦闘状態管理）

初期仕様:

- AdventurerとMonsterが交互または攻撃速度ごとに攻撃
- 攻撃間隔は `float seconds` で管理
- Direct攻撃のみ
- HPが0になったら戦闘終了
- Monsterが倒れたら一旦削除
- Adventurerが倒れたら死亡扱い

完了条件:

```text
[Combat] Adventurer A attacked Slime for 8
[Combat] Slime attacked Adventurer A for 3
[Combat] Slime defeated
```

## Phase 9: ゲームイベントシステム

Use Case の処理結果を pub/sub で通知する基盤を作り、戦闘ログと冒険者戦績の記録を実現する。

作るもの:

- `IGameEvent` / `IGameEventBus` / `GameEventBus`
- 戦闘イベント: `CombatAttackOccurred`, `ActorDefeated`, `CombatEncounterStarted`, `CombatEncounterEnded`
- `AdventurerBattleRecord`（長期保管購読者: 戦闘回数・ダメージ統計・直前戦闘サマリー）
- `CombatLogPresenter`（揮発性購読者: ログ表示UI）
- `AdvanceCombatUseCase` に Publish を追加し `AdvanceCombatResult` を廃止

方針:

- イベントは「通知」であり「命令」ではない（`docs/game-event-design.md` 参照）
- 発行は UseCase 層のみ。Domain Entity は発行しない
- 購読者はゲームの状態を変更しない

完了条件:

```text
[Combat] Adventurer A attacked Slime for 8   ← CombatLogPresenter が表示
[Record] Adventurer A: 3 combats, 120 total damage dealt
```

## Phase 10: 帰還判断

戦闘後、冒険者を帰還へ向かわせる。

作るもの:

- `DecideAdventurerReturnUseCase`

初期仕様:

- Monsterを倒したら帰還
- HPが減っていたら帰還
- HPが0なら死亡
- アイテムや探索目的は見ない

完了条件:

```text
[Actor] Adventurer A starts returning
```

## Phase 11: 宿屋回復

地上に戻った冒険者を宿屋で回復させる。

作るもの:

- `RecoverAdventurerAtInnUseCase`

初期仕様:

- 地上へ戻ったら宿屋予約を取る
- 宿屋が空いていれば `Recovering`
- 1分で最大HPの10%回復
- `deltaGameSeconds` から回復量を計算
- 全回復したら `Preparing -> GoingToDungeon`

宿屋満室時:

- 初期実装では満室を起こさない設定でよい
- 後で「HPが減ったまま再探索」へ接続する

完了条件:

```text
[Inn] Adventurer A recovering HP 32/50
[Inn] Adventurer A fully recovered
[Actor] Adventurer A starts next exploration
```

## 最小実装で後回しにするもの

- レベルアップ
- 経験値
- アイテム拾得
- モンスタードロップ
- インベントリ整理
- 売買
- 装備更新
- 探索目的の詳細化
- 複数フロア探索
- Projectile / Area攻撃
- 高度なNavMesh連携
- 宿屋満室時の詳細挙動
- 冒険者の旅立ち / デスポーン

## 推奨実装順

1. `InitializeGameWorldUseCase`
2. `SpawnInitialMonstersUseCase`
3. `SpawnScheduledAdventurerUseCase`
4. `MoveActorTowardDestinationUseCase`
5. `UseDungeonStairUseCase` 接続
6. `AdvanceActorSimpleLifecycleUseCase`
7. `DetectCombatEncounterUseCase`
8. `AdvanceCombatUseCase`
9. `GameEventBus` / `AdventurerBattleRecord` / `CombatLogPresenter`
10. `DecideAdventurerReturnUseCase`
11. `RecoverAdventurerAtInnUseCase`

この順序なら、各段階でログ確認できる。

## ログ方針

Viewなしで挙動確認するため、初期実装ではログを積極的に出す。

ログカテゴリ例:

- `[World]`
- `[Spawn]`
- `[Actor]`
- `[Move]`
- `[Dungeon]`
- `[Combat]`
- `[Inn]`

後でView表示が整ったら、ログはDebug用途に縮小する。

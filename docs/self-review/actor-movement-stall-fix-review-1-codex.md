# Actor移動停止問題対応 セルフレビュー 1

対象:
- 地上からダンジョンに入るブロック上で停止するActor、接敵時に一定距離で停止するActorへの対応差分
- `ActorMovementService` 追加
- `ActorPathState` / `ActorNavigationService` の経路キャッシュ修正
- `AdvanceCombatUseCase` の戦闘接近移動修正

確認した観点:
- 設計
- 整合性
- パフォーマンス
- 重複した責務を持つクラス / データクラス
- その他総合

確認した資料:
- `docs/guidelines/self-review-preset.md`
- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`

## レビュー結果

### 1. 経路探索失敗キャッシュが同一 start / goal / layer で永久化する

重大度: 高

問題:

`ActorPathState.MarkFailed` は失敗した `layerId` / `start` / `goal` を保持し、`NeedsRecalculation` は `HasFailed` の場合に `CachedStart` が変わった時だけ再計算する。つまり、同じ位置から同じ目標へ向かう経路探索が一度失敗すると、同じ条件では再試行されない。

これは「しばらく動かない」「敵から一定距離に移動してそのまま動かない」という症状を完全には潰せていない。NavMesh の準備タイミング、階層生成直後の一時的不整合、目標地点の一時的な不可到達判定などで一度 `HasFailed` になったActorは、位置か目標が変わらない限り復帰できない。

原因:

経路キャッシュの目的は毎フレーム探索を避けることだが、成功キャッシュと失敗キャッシュを同じ寿命で扱っている。成功キャッシュは安定利用できる一方、失敗は一時的な環境状態や経路プロバイダの準備状態に依存する可能性があるため、再試行条件を別に持つ必要がある。

解決案:

失敗キャッシュには再試行条件を追加する。

- 最低限: 失敗状態でも一定時間または一定フレーム後に再探索する
- より適切: layer の navmesh / walkability revision を持ち、revision が変わったら失敗キャッシュを無効化する
- `ActorMovementService` 側で `HasFailed` を即 return するだけでなく、再試行可能な状態かを `ActorNavigationService` に問い合わせる

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/ActorPathState.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/ActorNavigationService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/ActorMovementService.cs`

完了条件:

- [x] 一度経路探索に失敗しても、同じ `layerId` / `start` / `goal` で再試行できる
- [x] 再試行が毎フレーム無制限に発生しない
- [x] 「初回探索失敗、次回探索成功」の EditMode test が追加されている
- [x] `uloop.cmd compile --project-path Client` が成功する
- [x] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する
- [x] PlayMode 30秒確認で `[World] GameWorldState initialized` が出力され、Errorログがない

### 2. 戦闘接近移動で毎フレーム・Actorごとにキャプチャdelegateを生成している

重大度: 中

問題:

`AdvanceCombatUseCase.MoveTowardTarget` は、戦闘対象に接近するActorごとに `position => floor.IsWalkable(position)` を生成して `ActorMovementService.MoveToward` に渡している。戦闘接近は毎フレーム実行される可能性があり、同一階層に多数Actorがいる場合に不要なdelegate生成が積み上がる。

原因:

`ActorMovementService` の汎用性を優先して `Func<GridPosition, bool>` を受ける形にしたが、フレームループ上の呼び出しでキャプチャdelegateを作るコストが残っている。レビュー観点上、Frame Loop 内の不要allocationは避けるべき。

解決案:

毎フレームのキャプチャdelegate生成を避ける。

- `ActorMovementService` に `MapLayer` と walkability provider を表す軽量オブジェクトを渡す
- または `DungeonFloor` / `MapLayer` 向けの非キャプチャ経路を用意する
- 既存の lifecycle 移動呼び出しも同じ問題がないか確認し、必要なら同じAPIへ寄せる

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AdvanceCombatUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/ActorMovementService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/MoveActorTowardDestinationUseCase.cs`

完了条件:

- [x] 戦闘接近のフレームループでキャプチャdelegateを生成しない
- [x] lifecycle 移動と戦闘移動のwalkability渡し方が一貫している
- [x] 100体Actorが同一階層で移動する前提でも、不要allocationが増えない設計になっている
- [x] `uloop.cmd compile --project-path Client` が成功する
- [x] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する

### 3. `ActorMovementService` の新規概念追加に対する責務記録が不足している

重大度: 中

問題:

`ActorMovementService` は、通常移動と戦闘接近移動で共通利用する新規Application Serviceとして追加されている。一方で、既存には `MoveActorTowardDestinationUseCase` と `IActorNavigationService` が存在するため、責務境界を記録しないと「移動UseCase」「移動Service」「経路Service」の役割が曖昧になる。

現在の実装意図は読み取れるが、`docs/guidelines/self-review-preset.md` と `implementation-quality-guidelines.md` の新規概念追加ゲート上、類似概念との差分、代替しなかった理由、将来の統合 / 削除条件を記録すべき。

原因:

バグ修正の過程で共有移動ロジックを切り出したが、設計記録がコード差分に追随していない。コード上は重複削減になっているものの、ドキュメントなしでは将来の修正で責務重複が再発しやすい。

解決案:

`docs/design/` か該当タスクログに、Actor移動関連の責務境界を追記する。

- `ActorMovementService`: waypoint移動、到達判定、Actor座標更新、spatial index / view data同期
- `MoveActorTowardDestinationUseCase`: 通常AI移動のユースケース入口、通常移動の到達ポリシー
- `IActorNavigationService`: 経路探索とActor単位の経路状態キャッシュ
- 戦闘接近移動は `AdvanceCombatUseCase` が戦闘判断を持ち、移動の実処理だけ `ActorMovementService` に委譲する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/ActorMovementService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/MoveActorTowardDestinationUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/ActorNavigationService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AdvanceCombatUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`
- `docs/design/`

完了条件:

- [x] Actor移動関連の責務境界が `docs/design/` またはタスクログに記録されている
- [x] `ActorMovementService` と `MoveActorTowardDestinationUseCase` の役割重複が説明可能になっている
- [x] 将来、移動種別が増えた時にどこへ処理を追加するか判断できる
- [x] 今回のレビュー項目を「対応済み」とする根拠がコードまたはdocsに残っている

## 対応ログ

### 2026-05-20 Codex対応

- 指摘1: `ActorPathState` に失敗経路の再試行間隔を追加し、同一 `layerId` / `start` / `goal` でも一定呼び出し後に再探索するように変更した。
- 指摘2: `IGridWalkability` を追加し、`GroundMap` / `DungeonFloor` を直接渡すことで、移動フレームループ上のキャプチャdelegate生成を削除した。
- 指摘3: `docs/design/map-dungeon-domain-design.md` に `ActorMovementService` / `MoveActorTowardDestinationUseCase` / `ActorNavigationService` / `IGridWalkability` の責務境界を追記した。

対応後の確認:

- `uloop.cmd compile --project-path Client`: 成功
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功、276/276 pass
- PlayMode 30秒確認: `[World] GameWorldState initialized` を確認、Errorログなし

## 現時点で問題なしと判断した点

- `AdvanceCombatUseCase` から直線移動を外し、共有の経路移動へ寄せた点は、接敵時に壁越し直進しないための整合性改善になっている。
- `ActorMovementService` により、Actor座標更新後の `ActorSpatialIndexService.SyncActor` と `ActorViewDataStore.SyncActor` が通常移動 / 戦闘移動で共通化されている。
- `WorldLifetimeScope` への `ActorMovementService` 登録は、constructor injection とLifetimeScope登録の整合性を満たしている。

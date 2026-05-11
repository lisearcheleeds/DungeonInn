# Milestone 4 Phase 6 Boundary Refactor Self Review

実施日: 2026-05-11

## 対応内容

- `IEventPublisher` / `IEventSubscriber` を追加し、Runtime 側の直接 `IGameEventBus` 注入を登録・実装以外から除去した。
- `DecideAdventurerReturnUseCase` のイベント購読、Dirty Actor 管理、撃破数集計を `AdventurerReturnTrackingService` に分離した。
- `RecoverAdventurerAtInnUseCase` の回復蓄積状態と撃破時クリア購読を `AdventurerRecoveryStateService` に分離した。
- `AdvanceActorSimpleLifecycleUseCase` の探索目的地状態と撃破時クリア購読を `AdventurerExplorationStateService` に分離した。
- Publish のみ行う UseCase / Combat クラスは `IEventPublisher` 依存に変更した。
- Subscribe のみ行う Presenter / 集計 Service は `IEventSubscriber` 依存に変更した。

## 検証

- `uloop.cmd compile --project-path Client`: 成功
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（203 passed）
- 差分内の禁止 API 追加スキャン: 追加なし
- `IGameEventBus` Runtime 直接注入スキャン: `GameEventBus` 実装、interface 定義、`WorldLifetimeScope` 登録のみ

## 残作業

- UseCase から UseCase を呼ぶ既存箇所の Orchestrator 化は、挙動変更範囲が広いため未対応。
- `IGameWorldStateReader` / `IGameWorldStateWriter` 分離は、呼び出し契約の見直しが広範囲になるため未対応。
- `Actor.Inventory` / `Actor.Equipment` の読み取り専用化と Domain Entity の static catalog 参照除去は、Domain API 変更を伴うため別フェーズで扱う。

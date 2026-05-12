# 将来の整理候補メモ

このメモは、Milestone 4 の節目では対応しないが、今後の機能追加前後で整理を検討したい Domain / UseCase の候補を残すためのもの。

Milestone 4 で対応する対象:

- 交換 / 在庫 / 取引ログの統一
- Actor 生成系の共通化

## Actor 状態遷移系

対象:

- `AdventurerLifecycleState`
- `ActorGoal`
- `ActorPlan`
- `ActorAction`
- `ActorAiDecision`

観点:

- いずれも Actor の意思決定、目的、行動、状態を表す。
- ただし全部を 1 つの汎用 StateMachine に統一すると、意味の違いが見えにくくなる。

将来方針:

- 汎用化より、責務階層の明文化を優先する。
- `Lifecycle` は長期状態、`Goal` は目的、`Plan` は方針、`Action` は現在の具体行動、`AiDecision` は判断ログとして扱う。
- 状態遷移が増えて読みにくくなった段階で、共通の遷移検証やログ補助だけを切り出す。

## 回復 / 効果適用系

対象:

- `RecoverAdventurerAtInnUseCase`
- `UseConsumableItemUseCase`
- `AdvanceActorEffectsUseCase`
- `ActorEffectInstance`
- `ActiveStatusEffect`
- 施設の休憩 / 食事効果

観点:

- HP、MP、疲労、ストレス、負傷の増減という意味では共通している。
- 宿泊や食事は、料金、予約、施設品質、満足度とも結びつくため、すべてを効果システムへ押し込むと責務が混ざる。

将来方針:

- 共通化するなら、回復量計算と Actor への適用部分を `ActorEffectApplier` や `RecoveryEffectPolicy` として切り出す。
- 予約、料金、満足度、施設利用イベントは UseCase 側に残す。

## レポート / 履歴 / 統計系

対象:

- `GameEventHistoryService`
- `InnEconomyStatisticsService`
- `InnDailyReportStore`
- `InnEconomyStatusCalculator`
- `GetGameEventHistoryUseCase`
- `GetInnEconomyStatusUseCase`
- `GetInnEconomyReportUseCase`

観点:

- どれもイベントや現在状態から、後で参照するための履歴、集計、スナップショットを作る。
- 現時点では宿屋経済が主対象なので、今すぐ抽象化すると用途より抽象が先行しやすい。

将来方針:

- 施設別売上、店舗別在庫履歴、冒険者別戦績などが増えた段階で `SnapshotStore<T>` や `DailyReportStore<T>` を検討する。
- イベント履歴は原因追跡ログ、日次レポートは永続スナップショットとして責務を分ける。

## CombatEffect 実行系

対象:

- `AdvanceCombatUseCase`
- `AdvanceProjectileUseCase`
- `AdvanceAreaEffectUseCase`
- `CombatEffectExecutor`
- `CombatDamageResolver`
- `CombatDefeatResolver`
- `AttackAreaTargetResolver`

観点:

- Direct / Projectile / Area は、発生源、対象解決、命中、効果実行、イベント発行という流れを共有している。
- Milestone 4 時点で CombatEffect はすでにかなり抽象化されているため、追加の統一は過抽象化になる可能性がある。

将来方針:

- Projectile / Area / Direct の挙動差分が増え、UseCase 間の重複が再び目立った段階で、共通の combat effect runtime pipeline を検討する。
- 現時点では既存の `CombatEffectExecutor` を中心に、重複が悪化しないように保つ。

## Map / Dungeon / Navigation の位置・移動系

対象:

- `LayerPosition`
- `GridPosition`
- `MapLayerId`
- `DungeonStair`
- `MoveActorTowardDestinationUseCase`
- `UseDungeonStairOrchestrator`
- `ActorNavigationService`

観点:

- 地上、ダンジョン、階層移動、将来の屋内マップは、すべて「場所」と「移動経路」を扱う。
- ただし移動仕様はまだ拡張途中で、今の段階で `Location` や `TravelRoute` にまとめると設計が先走る。

将来方針:

- 屋内マップ、複数施設内移動、ワールドマップ移動が入る段階で、位置表現と移動遷移の統一を検討する。
- 当面は `LayerPosition` と `GridPosition` の責務を混ぜないようにする。

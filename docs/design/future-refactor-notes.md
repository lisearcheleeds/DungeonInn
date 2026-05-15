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

## プレイヤー向けログ / 通知 / 履歴 UI

`WorldDebugGameLogPresenter` はデバッグ診断専用のシンクであり、プレイヤー向けランタイム UI の基盤ではない。
将来のプレイヤーログ・通知・戦闘履歴・分析画面において、View / Presenter コードが `IGameWorldStateReader` の広範な状態を直接読むことは禁止する。

将来方針:

1. UI 専用の Application 層イベント履歴・ナローリードモデル・クエリを追加する。
2. UI が必要とするイベント事実と派生表示 DTO フィールドのみを保持する。
3. ランタイム Presenter は `IGameWorldStateReader` ではなく、ナローなクエリ / リードモデルに依存させる。
4. UI が追加の派生データを必要とする場合は、View 側で補完するのではなく、クエリ / リードモデルの境界で定義する。

## ActorId キーの一時状態ホルダー

ActorId をキーとする `Dictionary<Guid, ...>` や `HashSet<Guid>` を新規追加する際は、実装前にオーナー・ライフタイム・クリーンアップイベントを記録すること。
永続的な Actor プロファイルメタデータと、探索ランの実績やフレーム単位のランタイムフラグを混在させないこと。

現在のオーナー一覧:

| オーナー | 状態 | ライフタイム | クリーンアップ / リセットイベント |
|---|---|---|---|
| `ActorDecisionScheduler` | AI ランタイム評価状態 | ワールドシーンスコープ、Actor ごと | `ActorDefeated`, `ActorDeparted` |
| `AdventurerExplorationStateService` | 探索目的地 | ワールドシーンスコープ、Actor ごと | `ActorDefeated`, `ActorDeparted` |
| `AdventurerRecoveryStateService` | 宿屋回復累積 HP | ワールドシーンスコープ、Actor ごと | `ActorDefeated`, `ActorDeparted` |
| `AdventurerReturnTrackingService` | 帰還評価対象の Dirty Actor キュー | ワールドシーンスコープ、一時フラグ | `ActorDefeated`, `ActorDeparted`、欠落 Actor スキャン、明示的クリア |
| `ActorExplorationAchievementRegistry` | 今回の探索で倒したモンスター種カウント | 探索ラン単位 | `ActorEnteredDungeon` でリセット、`ActorDefeated` / `ActorDeparted` でクリーンアップ |
| `ActorProfileRegistry` | 永続 Actor プロファイル / スポーンメタデータ | ワールドシーン内の Actor ライフタイムメタデータ | 探索ランのクリーンアップには使用しない。`ActorId`・表示名・アーキタイプ / 種族・スポーン時 Behavior 種別のみを保持する |

新規 Actor キーホルダーのチェックリスト:

1. 状態が永続プロファイルメタデータ・探索ラン実績・フレーム単位ランタイム一時状態のどれに該当するかを定義する。
2. イベントを購読するか長期状態を保持する場合は、シーンの `LifetimeScope` に登録する。
3. オーナーがより短いライフタイムを持つことを証明できない限り、Actor 削除イベント（`ActorDefeated`, `ActorDeparted`）を購読する。
4. クリーンアップイベントを発行し、ActorId エントリが消えることを検証する EditMode クリーンアップテストを追加する。

## 宿屋予約フロー境界

`AdvanceInnRecoveryOrchestrator.EnsureInnReservation()` は現在、宿屋予約のドメイン操作（`AdventurerGuild.ReserveInn()` および `AdventurerBehavior` のライフサイクル変更）を直接実行している。
既存の UseCase に操作を移動すると UseCase-in-UseCase 結合が再発し、専用の予約 UseCase を追加するとナローなフローのために Application 公開契約が膨らむため、現マイルストーンでは許容している。

将来方針:

1. 宿屋予約フローの分岐が増えた段階で、予約専用の Application 境界を導入する。
2. `AdvanceInnRecoveryOrchestrator` は処理順の制御のみを担当させる。
3. 料金請求・予約作成・ライフサイクル遷移・候補マーキング・イベント発行は明示的なトランザクション境界にまとめる。

## Actor 処理候補ヒント

`ActorProcessingCandidateService.SyncActor()` は現在、`AdventurerBehavior` のライフサイクル状態から候補セットを導出している。
`IActorBehavior` に polymorphic hook を今追加すると、Application のスケジューリング関心事が Domain の Behavior 契約に混入するか、候補セマンティクスを持つ 2 つ目の Behavior が登場する前にすべての Behavior が実装しなければならないクロスレイヤー DTO 形状を要求することになる。

将来方針:

1. 別の Behavior 種がスケジュール済み候補参加を必要とした段階で再検討する。
2. 必要であれば、Application スケジューリングの概念を Domain エンティティに押し込めるのではなく、Application オーナーのポリシー境界で候補ヒントを定義する。
3. Domain の Behavior インターフェースは Actor のルール・回復・Behavior オーナーの状態に集中させる。

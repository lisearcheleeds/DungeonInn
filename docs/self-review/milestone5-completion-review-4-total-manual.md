# Milestone 5 完了確認レビュー 第4回（Claude Code + Codex merge）

作成日: 2026-05-14

## 設計レビュー

### 1. UseCase から別 UseCase を直接呼ぶ宿回復フローが残っている

重大度: 高

問題:

`RecoverAdventurerAtInnUseCase` が `ChargeInnFeeUseCase` と `DespawnAdventurerUseCase` を constructor injection し、宿泊料徴収や退去処理を直接呼んでいる。`application-boundary-guidelines.md` は UseCase を単一トランザクション境界とし、UseCase 間の順序制御は Orchestrator に置く方針を定めているため、宿回復フローだけ境界が崩れている。

原因:

宿回復、料金徴収、満室待機、所持金不足、退去/despawn の順序制御が `RecoverAdventurerAtInnUseCase` に集約され、Orchestrator と単体 UseCase の責務分離が不完全なまま残っている。

解決案:

`AdvanceInnRecoveryOrchestrator` または `RecoverAdventurerAtInnOrchestrator` を追加し、宿回復の進行順序をそこへ移す。`RecoverAdventurerAtInnUseCase` は回復処理そのもの、`ChargeInnFeeUseCase` は料金徴収、`DespawnAdventurerUseCase` は退去処理に限定する。料金徴収や退去が宿回復内部操作であると判断する場合は、UseCase ではなく Application Service として再分類する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/RecoverAdventurerAtInnUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/ChargeInnFeeUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/DespawnAdventurerUseCase.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `RecoverAdventurerAtInnUseCase` が `ChargeInnFeeUseCase` / `DespawnAdventurerUseCase` を constructor injection していない（本指摘のスコープは宿回復フローに限定）
- [ ] 宿回復、料金徴収、退去/despawn の順序が Orchestrator または明示的な Application Service に移っている
- [ ] 料金不足、満額支払い、満室待機、退去/despawn の EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 2. 戦闘 helper が即時 publish 経路をまだ公開している

重大度: 中

問題:

`CombatDamageResolver`・`CombatEffectExecutor`・`CombatDefeatResolver` の 3 クラスが `IEventPublisher` を保持し、buffer / collector を渡さない public overload から即時 publish できる。主要経路では `BufferedEventPublisher` に寄せられているが、API としてはトランザクション完了後 publish が保証されていない。

原因:

既存の即時 publish 設計に後から buffer overload を追加したため、古い直接 publish 経路が互換用に残っている。結果として、今後の呼び出し追加時にガイドライン違反の経路を再利用できてしまう。

解決案:

Resolver / Executor から `IEventPublisher` field と no-buffer overload を削除し、event collector / buffer を明示引数にする。あるいは Resolver / Executor は発生イベント DTO を返し、外側の UseCase / Orchestrator がトランザクション完了後に publish する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatDamageResolver.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatEffectExecutor.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatDefeatResolver.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AdvanceCombatUseCase.cs`
- `docs/design/game-event-design.md`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `CombatDamageResolver` / `CombatEffectExecutor` / `CombatDefeatResolver` が global event bus に直接 publish できない
- [ ] buffer / collector を通さない public overload がない
- [ ] 通常攻撃、projectile、area effect、defeat / drop / reward のイベント発行順を検証する EditMode test がある
- [ ] `docs/design/game-event-design.md` が現行のイベント発行契約と一致している
- [ ] `uloop.cmd compile --project-path Client` が成功している

再発理由:

第3回レビューで「戦闘イベントのトランザクション完了後 publish」は対応済み扱いになったが、主要経路の修正に留まり、旧 API をコード上から閉じる完了条件が不足していた。

再発防止策:

レビュー完了条件に「古い publish overload が消えていること」を含め、`rg "IEventPublisher" Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat` で直接 publish 可能な helper を確認する。

### 3. Debug Log Presenter が広い world state を View 層から直接読んでいる

重大度: 中

問題:

`WorldGameLogPresenter` が `IGameWorldStateReader` を直接注入し、Actor、Guild、施設予約、所持金などを読みながら表示用ログ文字列を組み立てている。Milestone 5 roadmap は UI 表示を専用 Query / ReadModel / DTO に寄せる方針を定めており、debug build 限定であっても View 層が広い Domain 集約に到達できる状態は境界を曖昧にする。

> **検証注記（2026-05-14）:** コードを直接確認した結果、`Initialize()` メソッドは `if (!Debug.isDebugBuild) { return; }` で始まり、subscriptions は Debug build 以外では登録されない。ただし DI constructor は常に `IGameWorldStateReader worldState` を受け取り、`OnActorWaitingForInn()`・`OnActorReservedInn()`・`OnItemPickedUp()` 内で `worldState.Guild.GetFacility()`・`worldState.FindActor()` を呼んで補助情報を取得している。実行は Debug build に限定されるが、依存関係と読み取り経路はコード上に常に存在する。完了条件「Debug.isDebugBuild に境界違反の許容を依存していない」はこの状態を指している。

原因:

イベントログに必要な補助情報が event DTO または narrow query として用意されておらず、Presenter が不足情報を `IGameWorldStateReader` から直接補っている。具体的には `ActorWaitingForInn` イベント DTO が `InnFacilityId` のみを持ち、room 数・Gold などを持たないため、Presenter が `worldState` から後引きせざるを得ない。

解決案:

Debug log 用の narrow read model / query を Application 層に置く。Presenter は `IGameWorldStateReader` ではなく、表示に必要な DTO だけを参照する。イベントに含めるべき事実情報と、Query で後引きする表示補助情報を整理する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLogPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/IGameWorldState.cs`
- `docs/roadmap/milestone5-roadmap.md`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `WorldGameLogPresenter` が `IGameWorldStateReader` に依存していない
- [ ] ログ表示に必要な状態は event DTO または narrow Application query / read model から取得される
- [ ] `Debug.isDebugBuild` に境界違反の許容を依存していない
- [ ] Debug log 用 query / DTO の責務が docs またはテスト名で明確になっている

### 4. WorldMapViewDataProvider.GetLayers() が毎フレームラムダクロージャを生成する

重大度: 中

問題:

`WorldMapViewDataProvider.GetLayers()` は毎フレーム `WorldGameLoopEntryPoint.Update()` → `WorldMapView.UpdateVisuals()` 経由で呼ばれる。内部の `GetOrCreateLayer()` は `Func<GridPosition, WorldMapCellViewKind> resolveCellKind` を受け取り、呼び出しごとに `groundMap`・`DungeonFloor` を捕捉するラムダクロージャを生成している（Ground 用 1 個 + Dungeon Floor 数分）。キャッシュヒット後にラムダの本体は呼び出されないが、ラムダオブジェクト自体は毎フレーム新規生成される。

```csharp
// GetLayers() 内（毎フレーム）
layers.Add(GetOrCreateLayer(
    MapLayerId.Ground,
    "Ground",
    groundMap.Layer.Width,
    groundMap.Layer.Depth,
    position => groundMap.IsWalkable(position)  // ← 毎フレーム新規生成
        ? WorldMapCellViewKind.GroundWalkable
        : WorldMapCellViewKind.GroundBlocked));
```

原因:

`GetOrCreateLayer()` メソッドシグネチャが `Func<>` を受け取る設計のため、呼び出し元でラムダを記述せざるを得ない。Layer が静的（マップは完成後に変化しない）にもかかわらず、毎フレームクロージャが生成されている。

解決案:

`GetLayers()` がキャッシュ済みの場合は `viewDataProvider.GetLayers()` 自体を呼ばないか、`WorldMapView` が新規 layer のみを通知ベースで受け取る設計にする。短期対応としては `GetOrCreateLayer()` 内で `if (cachedLayers.TryGetValue(...))` がヒットした場合に即 `return` し、ラムダを評価しない形は維持しつつ、呼び出しコンテキスト（`GetLayers()` 自体）も all-hit のときは早期 `return` を入れる。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`（WorldMapViewDataProvider.GetLayers()）
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`（UpdateVisuals）

完了条件:

- [ ] `GetLayers()` が全キャッシュヒット時にラムダクロージャを生成しないか、呼び出し自体をスキップできる
- [ ] 毎フレームの GC Alloc がラムダ生成由来でゼロになっていることを Profiler で確認済み
- [ ] Dungeon Floor 追加時にキャッシュが適切に追加されることを EditMode test で確認済み
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 5. DetectCombatEncounterUseCase のイベント発行順序契約が docs にない

重大度: 中

問題:

`AdvanceCombatUseCase` / `AdvanceProjectileUseCase` / `AdvanceAreaEffectUseCase` では `BufferedEventPublisher` を使い、ダメージ・死亡解決・ドロップ完了後にイベントをまとめて発行する設計になった。一方 `DetectCombatEncounterUseCase` は `IEventPublisher`（= `GameEventBus`）へ `CombatEncounterStarted` / `CombatEncounterEnded` を即時 publish する。

`WorldSimulationOrchestrator.AdvanceFrameAsync()` では `detectCombatEncounterUseCase.ExecuteAsync()` が `advanceCombatUseCase.ExecuteAsync()` より先に呼ばれるため、「戦闘遭遇開始イベント → 戦闘未進行」という状態で購読者に通知が届く。`AdventurerBattleRecordService` が `CombatEncounterStarted` で記録を作成し始めると、同フレーム内の `AdvanceCombatUseCase` の `CombatAttackOccurred`（BufferedFlush 後）より先に record 生成が起きる。

原因:

`CombatEncounterStarted/Ended` は「遭遇の開始/終了」を表すため、ダメージ系とは異なり即時通知でも問題ないと判断された可能性があるが、その判断が docs に明記されていない。

解決案:

> **訂正（Codex レビュー 2026-05-14）:** 旧解決案1「`BufferedEventPublisher` を使い末尾で `Flush()`」は問題を解決しない。`Flush()` を UseCase 末尾で呼んでも `DetectCombatEncounterUseCase` の実行は `advanceCombatUseCase.ExecuteAsync()` より前のまま（`WorldSimulationOrchestrator` の実行順序は変わらない）。`CombatEncounterStarted` が戦闘結果より先に届くという事実は Buffer 統一では変わらない。主要解決策は docs への契約明記。

1. 「`CombatEncounterStarted` / `CombatEncounterEnded` は遭遇状態変更の即時イベントであり、同フレーム内の戦闘結果より先に届く」という発行順契約を `docs/design/game-event-design.md` に明記する（主要解決策）。
2. もし CombatEncounterStarted を受けた購読者が同フレームの戦闘結果に依存することが問題になるなら、`WorldSimulationOrchestrator` の実行順序（detect → combat → flush）を見直すか、FrameEnd 後の後処理フェーズに遭遇通知を分割する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/DetectCombatEncounterUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Event/BufferedEventPublisher.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`（AdvanceFrameAsync 実行順序）
- `docs/design/game-event-design.md`

完了条件:

- [ ] `CombatEncounterStarted` / `CombatEncounterEnded` の発行タイミングと「同フレーム内の戦闘結果との順序」が `game-event-design.md` に契約として記載されている
- [ ] 発行順に依存する購読者（`AdventurerBattleRecordService` 等）の想定が docs と一致している
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 6. IActorBehavior が空マーカーインターフェースのまま、かつ Domain 内で具体型依存が拡大している

重大度: 中

再発理由:

review-2 で「Milestone 6以降の大規模変更」と判断され、review-3 でも同様の延期判断が下された。しかし現行コードを読むと `Actor.Recover()` 内で `if (Behavior is AdventurerBehavior adventurerBehavior)` が使われており、Domain 層内の具体型依存が View 層だけでなく Domain 自身まで波及している。これは前回指摘時には含まれていなかった場所。

再発防止策:

`IActorBehavior` に型別分岐が不要な polymorphic hook（`OnRecover(RecoverContext)` 等）を定義し、Domain コアが具体型を参照しなくて済む設計にする。新規 Behavior 追加時の型チェック分散を CI で警告する仕組みを入れる。

問題:

`IActorBehavior` はメンバを一切持たないマーカーインターフェースであり、これは `domain-design-guidelines.md` の方針（`ActorBehaviorType` を持たせない）に沿った意図的な設計である。問題は空マーカーであること自体ではなく、Domain コアが `AdventurerBehavior` 具体型に直接依存している点にある。具体的には `Actor.Recover()` が Domain 層内で `if (Behavior is AdventurerBehavior adventurerBehavior)` とキャストして `ReduceStress()` を呼んでおり、新しい Behavior 追加時に Domain コアを変更せざるを得ない。Application 層（`WorldViewDataProviders.cs` の `ResolveBehaviorType()`）でも同様の型チェックが散在している。

原因:

Domain コアが具体型 Behavior に依存する polymorphic hook が `IActorBehavior` に定義されていないため、`Actor.Recover()` が型チェックで具体型に到達する必要が生じている。

解決案:

> **訂正（Codex レビュー 2026-05-14）:** 旧解決案1・2 の `IActorBehavior.Kind`/`ActorBehaviorKind` 追加は、`docs/guidelines/domain-design-guidelines.md` 第2節「IActorBehavior に ActorBehaviorType Type を持たせない」と正面衝突するため採用しない。

1. `IActorBehavior` に `void OnRecover(RecoverContext context)` のような polymorphic hook を定義し、`Actor.Recover()` が型チェックせずに `Behavior.OnRecover(context)` を呼ぶ。ストレス軽減は `AdventurerBehavior.OnRecover()` の内部で処理する
2. または `IStressRecoverable` のような capability interface を導入し、`Actor.Recover()` が `if (Behavior is IStressRecoverable recoverable) { recoverable.ReduceStress(...); }` のように具体クラスではなく interface に依存する
3. どちらの方針を採用するかを `docs/guidelines/domain-design-guidelines.md` に明記する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/IActorBehavior.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs`（Recover() 内の型チェック）
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`（ResolveBehaviorType()）
- `docs/guidelines/domain-design-guidelines.md`（第2節: IActorBehavior に識別子プロパティを持たせない）

完了条件:

- [ ] `IActorBehavior` に polymorphic hook（`OnRecover(RecoverContext)` 等）または capability interface（`IStressRecoverable` 等）が定義されている
- [ ] `Actor.Recover()` が `AdventurerBehavior` に直接キャストしていない
- [ ] Domain 層のコアコード（Actor / ActorEffects 等）に `is AdventurerBehavior` の型チェックが残っていない
- [ ] `docs/guidelines/domain-design-guidelines.md` に Behavior 判定の推奨パターンが記載されている
- [ ] Actor の Behavior 別 Recover / 処理分岐を検証する EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 7. ~~InnEconomyStatistics.Demand プロパティと InnEconomyStatusCalculator が重複計算する~~

→ **重複1「InnEconomyStatus / InnDailyReport の正典がまだ分散している」に統合済み**（Codex レビュー 2026-05-14）

> **注:** 「統合済み」はこのドキュメント内の項目番号を整理したことを意味する。コード上の問題（`InnEconomyStatistics.Demand` の定義が `InnEconomyStatusCalculator` に参照されず `statistics.Guests + statistics.RejectedGuests` が重複計算されている）は**未解決**。完了条件は重複1で管理している。

---

### 8. SpawnScheduledAdventurerOrchestrator と Monster 版で LINQ 使用パターンが非対称（パフォーマンス3・パフォーマンス7 統合）

重大度: 中

> **統合（Codex レビュー 2026-05-14）:** パフォーマンス3「Adventurer spawn の候補抽選に LINQ / 一時配列が残っている」およびパフォーマンス7「SpawnScheduledAdventurerOrchestrator が毎 schedule tick に LINQ GC Alloc を発生させる」を本項目に吸収した。

問題:

`SpawnScheduledMonsterOrchestrator` は LINQ を使わず `foreach` ループで実装されている（GC Alloc なし）。一方 `SpawnScheduledAdventurerOrchestrator` は毎 schedule tick で以下の LINQ を実行する。

- `worldState.Actors.Count(x => x.Behavior is AdventurerBehavior)` （全 Actor 走査 + ラムダ）
- `spawnTable.Entries.Where(...).ToArray()` （中間配列生成）
- `entries.Sum(entry => entry.Weight)` （中間 Enumerable）

同じスポーン責務を持つ 2 つの Orchestrator が異なるパフォーマンス特性を持っており、Actor 数が増えるとアドベンチャラー版だけが重くなる。

原因:

Monster 版は後から実装されてパフォーマンス改善が適用されたが、Adventurer 版の既存実装に対する同等の改善が行われていない。

解決案:

1. `worldState.Actors.Count(x => x.Behavior is AdventurerBehavior)` を明示ループに変更する。必要であれば `GameWorldState` または Actor 管理 Service 側で Adventurer 数をキャッシュする（`IActorBehavior.Kind` 比較への変更は `domain-design-guidelines.md` の方針と矛盾するため採用しない）
2. `Where().ToArray()` を `foreach` + 条件チェックの明示ループに変更する
3. `Sum()` を for ループに変更する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledAdventurerOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledMonsterOrchestrator.cs`

完了条件:

- [ ] `SpawnScheduledAdventurerOrchestrator` に `using System.Linq` がないか、LINQ による中間配列生成が schedule tick ごとに発生していない
- [ ] `Count(x => x.Behavior is AdventurerBehavior)` が全走査ラムダではなく、キャッシュまたはプロパティに変わっている
- [ ] アドベンチャラー版と Monster 版で同等のパフォーマンス特性になっている
- [ ] Adventurer スポーン処理の EditMode test が通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

## パフォーマンスレビュー

### 1. Frame loop に Actor / Item 数比例の全走査が残っている

重大度: 高

問題:

`WorldSimulationOrchestrator.AdvanceFrameAsync()` で、AI、戦闘検出、item pickup、actor effects、宿回復が frame loop 側から呼ばれている。特に `PickUpItemUseCase` は Actor と Item の組み合わせで探索するため、Actor 数と Item 数が増えると毎フレーム負荷が急増する。

原因:

第3回レビュー対応で売却や装備更新の一部は schedule tick 側へ移ったが、frame loop / schedule tick / event-driven の分類がまだコード上の所有者とデータ構造に落ち切っていない。Item 位置の spatial index や dirty actor 集合がないため、全体走査に頼っている。

解決案:

Frame loop には移動、戦闘進行、projectile / area effect のような毎フレーム必須処理だけを残す。Item pickup は item spatial index または近傍 dirty actor 起点へ移す。Actor effects と宿回復は effect / recovery state を持つ Actor の候補集合だけを処理する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Items/PickUpItemUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdvanceActorEffectsUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/RecoverAdventurerAtInnUseCase.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `PickUpItemUseCase` が毎フレーム Actor x Item 全組み合わせを探索しない
- [ ] Actor effects は効果を持つ Actor、または dirty / scheduled 候補だけを処理している
- [ ] 宿回復は回復中 Actor の state service / queue から処理対象を取得している
- [ ] Actor / Item 数を増やした EditMode または profiler 確認で、処理量が全件組み合わせに比例しないことを確認している

### 2. Schedule tick に複数の全 Actor / Inventory 系処理が集中している

重大度: 中

問題:

`AdvanceScheduleSystemsAsync()` は同一 schedule tick で spawn、lifecycle、予約、装備更新、売却、回復アイテム、帰還判断をまとめて実行している。`SellItemsUseCase` や `UpdateEquipmentUseCase` は Actor / Inventory / Facility 数に比例するため、schedule tick 到達フレームでスパイクが起きやすい。

原因:

毎フレームから外した処理を schedule tick に寄せたが、tick 内の処理予算や候補 queue がない。Inventory 変更、施設変更、Actor lifecycle 変更といった event-driven の起点もまだ処理対象絞り込みに使われていない。

解決案:

Schedule tick 内に処理予算を設け、複数フレームに分割する。装備更新、売却、回復アイテム、帰還判断は Actor lifecycle / Inventory dirty / Facility dirty の候補 queue から処理する。売却先施設や category lookup はキャッシュする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/SellItemsUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Equipment/UpdateEquipmentUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/UseRecoveryItemOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/DecideAdventurerReturnUseCase.cs`

完了条件:

- [ ] Schedule tick 処理に per-frame work budget または候補 queue がある
- [ ] 装備更新、売却、回復アイテム使用が全 Actor 無条件 scan ではなく、候補 Actor だけを処理している
- [ ] Inventory / Facility 変更時に必要な dirty flag または candidate queue が更新される
- [ ] schedule tick 大量発生時の負荷を検証する EditMode test または profiler 記録がある

### 3. ~~Adventurer spawn の候補抽選に LINQ / 一時配列が残っている~~

→ **設計8「SpawnScheduledAdventurerOrchestrator と Monster 版で LINQ 使用パターンが非対称」に統合済み**（Codex レビュー 2026-05-14）

> **注:** 「統合済み」はこのドキュメント内の項目番号を整理したことを意味する。コード上の問題（`SpawnScheduledAdventurerOrchestrator` の `.Where().ToArray()` による一時配列生成）は**未解決**。完了条件は設計8で管理している。

### 4. Actor View の生成 / 削除が pooled ではない

重大度: 中

問題:

`WorldActorViewRegistry` は Actor spawn 時に `new GameObject` と `AddComponent<SpriteRenderer>` を行い、despawn 時に `Destroy` する。Milestone 5 の placeholder 表示としては成立するが、Actor の入退場が増えると GameObject / Component の生成破棄によるスパイクが起きる。

原因:

debug Sphere から SpriteRenderer 表示へ置き換えることを優先し、Actor View の object pooling は後回しになっている。Registry が識別子管理と生成破棄の両方を持っており、pooling の差し込み点がまだ分離されていない。

解決案:

`WorldActorViewFactory` または `WorldActorViewPool` を導入し、`WorldActorViewRegistry` は ActorId と View の対応管理に寄せる。削除時は `Destroy` ではなく非表示化して pool へ戻す。Object name に GUID を含める処理は diagnostics 限定にする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorViewRegistry.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `docs/roadmap/milestone5-roadmap.md`

完了条件:

- [ ] Actor spawn / despawn が pooled view を再利用する
- [ ] `WorldActorViewRegistry` が生成破棄の詳細ではなく対応管理を主責務にしている
- [ ] Actor の大量入退場テストまたは profiler 確認で GameObject / SpriteRenderer 生成破棄スパイクがない

### 5. AStarPathfinder.TryFindPath() の openSet.Contains が O(n)

重大度: 中

問題:

`AStarPathfinder.TryFindPath()` は `ActorNavigationService` でフィールドバッファを渡して再利用されており、毎回の新規 List/Dictionary 生成は解消されている。しかし `openSet.Contains(neighbor)` が O(n) の線形探索であり、経路が長い場合（広いフロア、大回りルート）に open set が大きくなるほど探索コストが増加する。また `PopLowestF()` も O(n) のスキャンをしており、合計で O(n²) の動作になる可能性がある。

```csharp
// AStarPathfinder.TryFindPath() 内
if (!openSet.Contains(neighbor))  // O(n) 毎近傍
{
    openSet.Add(neighbor);
}
// ...
var current = PopLowestF(openSet, fScore);  // O(n) スキャン
```

原因:

A* の基本実装として List を使っているため、open set への重複チェックと最小 f値ノード取得が O(n) になっている。

解決案:

1. `openSet.Contains()` を `HashSet<GridPosition>` との二重管理に変更し、重複チェックを O(1) にする
2. `PopLowestF()` を最小ヒープ（優先度キュー）に変更し、O(log n) にする
3. 当面の暫定対応として、`gScore.ContainsKey(neighbor)` のみで重複チェックを代替する（openSet への登録前に gScore 更新済みかで判定）

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/AStarPathfinder.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/ActorNavigationService.cs`

完了条件:

- [ ] `openSet.Contains(neighbor)` が O(1) の構造（HashSet / closed set による管理）に変わっている
- [ ] `PopLowestF()` が O(log n) の優先度キューまたは同等の構造に変わっている
- [ ] パス探索の EditMode test（既存 `DecideAdventurerReturnUseCaseTests` 等）が通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 6. AttackAreaTargetResolver.ContainsFan() に Math.Cos（三角関数）が残っている

重大度: 中

問題:

review-2 / review-3 で「Fan 判定は dot / cross と距離二乗で行い、`Sqrt` / `Atan2` などの三角関数を避ける」と指摘されていた。Fan 判定では `Math.Sqrt` / `Math.Atan2` は使われていないが（Rectangle の bounding radius 計算では `CalculateBoundingRadius()` 内で `Math.Sqrt` が残存している）、Fan 判定の `ContainsFan()` では `Math.Cos(halfAngle * Math.PI / 180f)` が残っている。三角関数は整数演算や乗算より大幅にコストが高く、AreaEffect 数 × 近傍 Actor 数が増えると積み上がる。

```csharp
// ContainsFan() 内
var cos = Math.Cos(halfAngle * Math.PI / 180f);  // 三角関数残存
return distSq * cos * cos <= dz * dz;
```

また `cos` の計算は `AreaEffectInstance.AreaSpec.AngleDegrees` が変化しない限り定値なので、スポーン時に `AreaEffectInstance` へキャッシュできる。

原因:

review-2 の解決案は「`Sqrt` / `Atan2` を避ける」と書いていたが、`Math.Cos` は明示的に言及されなかったため対応が不完全になった。

解決案:

1. `AreaEffectInstance` 生成時に `cos` を計算し、`HalfAngleCos` プロパティとして持たせる
2. `ContainsFan()` はキャッシュ済みの `cos` を参照し、`Math.Cos` を毎回呼ばない
3. または Fan 判定を `cos` を含む lookup table、あるいは vector dot product に変換する（`dz / Math.Sqrt(distSq)` との比較を `cos` でできるが実際には `distSq * cos² <= dz²` のまま三角関数自体は不要）

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AttackAreaTargetResolver.cs`（ContainsFan()）
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/AreaEffectInstance.cs`（または相当ファイル）

完了条件:

- [ ] `AttackAreaTargetResolver.ContainsFan()` が毎呼び出しで `Math.Cos()` を実行していない
- [ ] `cos` がスポーン時 / 生成時に計算・キャッシュされているか、三角関数を使わない dot/cross 判定に変わっている
- [ ] Fan / Circle / Rectangle 判定の EditMode test が通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 7. ~~SpawnScheduledAdventurerOrchestrator が毎 schedule tick に LINQ GC Alloc を発生させる~~

→ **設計8「SpawnScheduledAdventurerOrchestrator と Monster 版で LINQ 使用パターンが非対称」に統合済み**（Codex レビュー 2026-05-14）

> **注:** 「統合済み」はこのドキュメント内の項目番号を整理したことを意味する。コード上の問題（`.Count()` / `.Sum()` による毎 schedule tick の GC Alloc）は**未解決**。完了条件は設計8で管理している。

---

### 8. Actor.RefreshParams() が毎回 new ActorParamCalculator() を生成する

重大度: 低

問題:

`Actor.RefreshParams()` は装備変更・Stats 変更・Level 変更などでパラメータ再計算が必要なたびに呼ばれる。その内部で `new ActorParamCalculator().Calculate(...)` を毎回生成している。`ActorParamCalculator` が状態を持たない純粋計算クラスであれば、static utility として扱えばアロケーションをゼロにできる。

原因:

`ActorParamCalculator` の設計方針（純粋 Domain 計算か DI 対象戦略か）が docs で明文化されておらず、インスタンス生成が必要かどうかが設計ドキュメントから読み取れない。review-3 の「設計6: Domain Entity 内の factory / calculator 依存方針が曖昧」として指摘されていたが、完了条件は未チェック。

解決案:

1. `ActorParamCalculator` が純粋計算クラスなら `static class ActorParamCalculator` に変更し、`ActorParamCalculator.Calculate(...)` と直接呼ぶ
2. または `ActorParamCalculator.Calculate()` を `static` メソッドに変更してインスタンス生成をやめる
3. 決定した方針を `docs/guidelines/domain-design-guidelines.md` に記載する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs`（RefreshParams()）
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/ActorParamCalculator.cs`（または相当ファイル）

完了条件:

- [ ] `Actor.RefreshParams()` が `new ActorParamCalculator()` を毎回生成していない
- [ ] `ActorParamCalculator` が純粋 Domain 計算として static 化または shared instance 化されている
- [ ] `ActorParamCalculator.Calculate()` 内の `equipment.Sum(x => x.Defense)` LINQ と `CollectBonuses()` の `new List<StatBonus>()` 一時生成が排除されている（`RefreshParams()` 呼び出しごとに発生するアロケーション）
- [ ] WeaponCombatCalculatorFactory / WeaponCalculatorFactory との設計方針が揃っている
- [ ] `docs/guidelines/domain-design-guidelines.md` に Domain Calculator の扱い方針が記載されている
- [ ] Actor パラメータ再計算の EditMode test が通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

## 重複した機能を持つクラス / データクラスレビュー

### 1. InnEconomyStatus / InnDailyReport の正典がまだ分散している（設計7・重複5 統合）

重大度: 中

> **統合（Codex レビュー 2026-05-14）:** 設計7「InnEconomyStatistics.Demand プロパティと InnEconomyStatusCalculator が重複計算する」および 重複5「InnEconomyStatus の 12 個の alias proxy プロパティが未整理」を本項目に吸収した。いずれも InnEconomySummary 正典を中心に据えれば同一作業で解決できる。

問題:

`InnEconomySummary` は追加済みだが、以下の 3 点で正典の分散が残っている。

1. `InnEconomyStatus` と `InnDailyReport` は同じ経済値を proxy property と constructor 引数として保持し続けており、`InnEconomyStatusCalculator` は report-to-status の field-by-field 詰め替えを行っている
2. `InnEconomyStatus` には `GuestsToday`、`RejectedGuestsToday`、`DemandToday` など `Current.Xxx` への委譲となる 12 個の alias proxy プロパティが残っており、UI 互換維持か削除かの方針が未決定（review-3 完了条件「alias proxy を持たないか互換維持理由が明記」は未チェック）
3. `InnEconomyStatistics.Demand { get => Guests + RejectedGuests; }` プロパティが存在するが、`InnEconomyStatusCalculator.CalculateDailyReport()` では `statistics.Demand` を使わず `statistics.Guests + statistics.RejectedGuests` を直接計算しており、定義が 2 箇所に重複している

原因:

既存 API 互換のために alias property を残した結果、Summary 抽出後も field-by-field の詰め替えが残っている。`Demand` プロパティの存在を `CalculateDailyReport()` が参照しなかった（または意図的に無視した）ため重複計算が生じた。

解決案:

1. `InnEconomyStatus` と `InnDailyReport` を `(Day, InnEconomySummary)` の薄い wrapper に寄せる
2. alias proxy プロパティを削除し、呼び出し元をすべて `status.Current.Guests` 形式に統一する。UI 互換用に維持する場合は削除条件を docs に明記する
3. `InnEconomyStatusCalculator.CalculateDailyReport()` 内の `statistics.Guests + statistics.RejectedGuests` を `statistics.Demand` に変更する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/InnEconomyStatus.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/InnDailyReport.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/InnEconomyStatusCalculator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/InnEconomyStatistics.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/InnEconomySummary.cs`
- `docs/self-review/milestone5-completion-review-3-codex.md`
- `docs/guidelines/domain-design-guidelines.md`

完了条件:

- [ ] `InnEconomyStatus` が `InnEconomySummary` を直接受け取り保持している
- [ ] `InnDailyReport` が `InnEconomySummary` を直接受け取り保持している
- [ ] `InnEconomyStatusCalculator` に report-to-status の field-by-field 詰め替えがない
- [ ] `InnEconomyStatusCalculator.CalculateDailyReport()` が `statistics.Guests + statistics.RejectedGuests` を直接計算せず `statistics.Demand` を使用している
- [ ] `InnEconomyStatus` の alias proxy プロパティが削除されているか、使用継続理由が docs またはファイル内に明記されている
- [ ] status / report の summary equivalence を検証する EditMode test がある
- [ ] `InnEconomyUseCaseTests` が更新後も通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 2. Actor-keyed transient state holder が増え続けている

重大度: 中

問題:

`ActorDecisionScheduler`、`AdventurerExplorationStateService`、`AdventurerRecoveryStateService`、`AdventurerReturnTrackingService`、`ActorViewDataStore` など、ActorId を key にする長期状態 holder が複数存在する。死亡・帰還時 cleanup は一部で対応されたが、新しい holder を追加するたびに cleanup 対象イベントを個別に実装する必要がある。

> **検証注記（2026-05-14）:** コードを直接確認した結果、`AdventurerReturnTrackingService` は `OnActorDeparted` で `dirtyActorIds` と `defeatedMonsterCountsByActor` を cleanup している。一方 `ActorDecisionScheduler` は `Dictionary<Guid, ActorAiRuntimeState> states` を保持するが、`ActorDefeated` / `ActorDeparted` の cleanup handler が存在しない（IEventSubscriber を constructor inject していない）。Actor が死亡・退場しても `states` に entry が残り続ける。

原因:

Actor-keyed state の所有者、寿命、cleanup event の共通契約がなく、各機能が個別に Dictionary / HashSet と購読処理を持っている。

解決案:

すぐに storage を統合しない場合でも、`IActorTransientState` のような cleanup 契約、または docs 上の actor-keyed state registry 表を作る。各 holder の owner、lifetime、cleanup event、テストを一覧化し、新規追加時のチェックリストにする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Ai/ActorDecisionScheduler.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Ai/AdventurerExplorationStateService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdventurerRecoveryStateService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdventurerReturnTrackingService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`
- `docs/self-review/milestone5-completion-review-2-total.md`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] Actor-keyed state holder の owner、lifetime、cleanup event が docs または review log に一覧化されている
- [ ] `ActorDecisionScheduler` に `ActorDefeated` / `ActorDeparted` 購読による `states` cleanup が追加されている（現状は cleanup なし・コード確認済み）
- [ ] `AdventurerReturnTrackingService.OnActorDefeated()` が倒された Actor 自身の `dirtyActorIds` / `defeatedMonsterCountsByActor` を cleanup している（現状は killer の dirty mark のみ・倒された actor 自身の cleanup なし・コード確認済み）
- [ ] `ActorDefeated` / `ActorDeparted` cleanup を各 transient state holder で検証する EditMode test がある
- [ ] 新しい Actor-keyed state holder の追加時に同じ cleanup checklist を通す運用が task template または guideline にある

再発理由:

第2回レビューでも ActorDeparted cleanup 漏れとして同系統の問題が出ており、個別 holder の修正だけでは新規 holder 追加時の漏れを防げなかった。

再発防止策:

「Actor-keyed state を追加したら cleanup event と test を必ず書く」ゲートを `application-boundary-guidelines.md` または task template に追加する。

### 3. ActorProfileRegistry が表示名辞書と gameplay snapshot を兼ねている

重大度: 中

問題:

`ActorProfile` は `ActorId`、`DisplayName`、`ArchetypeId`、`SpeciesId`、`BehaviorType` を保持し、`WorldGameLogPresenter` の表示名解決と `AdventurerReturnTrackingService` の戦闘帰還判定の両方で使われている。表示用 directory と、削除済み Actor の gameplay snapshot が同じ概念に混ざっている。

> **検証注記（2026-05-14）:** コードを直接確認した結果、`AdventurerReturnTrackingService.OnActorDefeated()` が `profileRegistry.TryGetProfile(gameEvent.ActorId, out var profile)` を呼び、`profile.SpeciesId` で倒したモンスターの種族を集計している。これは gameplay logic（帰還判断の素材）として `ActorProfileRegistry` を使う具体的な実例。`WorldGameLogPresenter.GetName()` も同じ registry から `DisplayName` を取得しており、表示用と gameplay 用が同一 registry に混在していることをコードレベルで確認した。

原因:

イベントが ActorId 中心で発行されるため、後から表示名や species / archetype を解決する side channel として registry が導入され、そのまま gameplay lookup にも拡張された。

解決案:

責務を分ける。表示用途は `ActorDisplayNameDirectory`、削除後も必要な gameplay 情報は `ActorSpawnSnapshotStore` のように命名し、寿命と cleanup / persistence を定義する。単一 registry を維持するなら、表示用ではなく「spawn snapshot store」であることを名前と docs に反映する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Profiles/ActorProfile.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Profiles/ActorProfileRegistry.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/CompleteActorSpawnUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdventurerReturnTrackingService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLogPresenter.cs`

完了条件:

- [ ] Profile registry の責務が display-only、snapshot-only、または分割済みとして明確になっている
- [ ] gameplay code が display profile に依存していない
- [ ] 削除済み Actor 情報を保持する場合、その persistence / cleanup 方針がテストされている
- [ ] 新規概念追加ゲートとして既存 Actor / archetype / behavior との差分が docs に記録されている

### 4. Spawn UseCase に DI を迂回する重複 constructor が残っている

重大度: 低

問題:

`SpawnAdventurerUseCase` と `SpawnMonsterUseCase` が、`CompleteActorSpawnUseCase` を手動 `new` する public constructor を持っている。DI 管理対象の構成を Runtime public API で複製しており、セルフレビュープリセットの差分許可モデルに反する可能性がある。

原因:

Factory / Request 統合の過程で、既存テストや互換用の簡易構築 path が Runtime 側に残った。

解決案:

Runtime public constructor は DI で使う 1 系統に統一する。テスト側で `CompleteActorSpawnUseCase` を組み立てる helper / fixture を用意し、Runtime API にテスト都合の constructor を残さない。

> **観察（Codex レビュー 2026-05-14）:** `SellItemsUseCase` にも第2 constructor があり `new NullGameClock()` を使用している。ただし `NullGameClock` は private inner class（DI 管理対象ではない）のため、DI を迂回して管理対象 UseCase を手動生成する Spawn UseCase とは性質が異なる。本項目は Spawn UseCase スコープに留める。「Runtime にテスト都合 constructor を残さない」方針として一般化する場合は `SellItemsUseCase` も対象に含めること。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnAdventurerUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnMonsterUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/CompleteActorSpawnUseCase.cs`
- `docs/guidelines/self-review-preset.md`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `SpawnAdventurerUseCase` / `SpawnMonsterUseCase` の Runtime constructor が DI 用の 1 系統に統一されている
- [ ] Tests 側に必要最小限の fixture / helper がある
- [ ] Runtime code に DI 管理対象 UseCase を手動 `new` する composition path がない
- [ ] `uloop.cmd compile --project-path Client` と該当 spawn tests が成功している

### 5. ~~InnEconomyStatus の 12 個の alias proxy プロパティが未整理~~

→ **重複1「InnEconomyStatus / InnDailyReport の正典がまだ分散している」に統合済み**（Codex レビュー 2026-05-14）

> **注:** 「統合済み」はこのドキュメント内の項目番号を整理したことを意味する。コード上の問題（`GuestsToday` / `RejectedGuestsToday` 等 12 個の alias proxy プロパティが `InnEconomyStatus` に残存）は**未解決**。完了条件は重複1で管理している。

---

### 6. SpawnAdventurerUseCase / SpawnMonsterUseCase のフロントエンド並列構造が継続

重大度: 低

> **訂正（Codex レビュー 2026-05-14）:** 旧問題文の「どちらも `ActorFactory` を呼んで Actor を生成し、GameWorldState に追加する」は現行コードと異なる。両 UseCase は Actor を生成して返すのみで、GameWorldState への登録は `SpawnScheduledAdventurerOrchestrator`（L95）および対応 Monster Orchestrator が行っている。また `SpawnAdventurerUseCase` は rookie equipment 支給・guild transaction・lifecycle 初期化を持ち、`SpawnMonsterUseCase` との単純重複とは言いにくい。本項目は「将来の Behavior 追加時に統合検討」に downgrade している。

問題:

review-2 / review-3 で「ActorFactoryCore による共通化は完了、上層 interface が冗長」と指摘され、Factory 層は統合済み。UseCase 層では `SpawnAdventurerUseCase` と `SpawnMonsterUseCase` が引き続き並列に存在する。両 UseCase は内部で `ActorFactory` を呼んで Actor を生成して返し（GameWorldState への登録は Orchestrator が担う）、基本フローは類似しているが、Adventurer 版は rookie equipment 支給・guild transaction・lifecycle 初期化などの固有ロジックを持つ。

今後 NPC / Pet などの Behavior が追加された場合、新しい `SpawnXxxUseCase` が生まれるリスクがある。ただし現時点での機能重複は限定的であり、統合は Milestone 6 の Behavior 設計と合わせて検討する方が適切。

原因:

UseCase 統合は Milestone 6 の移動・AI 整理と合わせて対応する方針で延期されており、追跡は継続中。

解決案:

将来 Behavior 追加が続く場合に `SpawnActorUseCase` への統合を検討する。Behavior 固有の初期化ロジック（装備支給、guild transaction 等）は `IActorSpawnInitializer` のような strategy pattern で分離できる。現時点での強制統合は Adventurer 固有ロジックの位置を不明瞭にするリスクがある。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnAdventurerUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnMonsterUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/ActorFactory.cs`

完了条件（Milestone 6 統合検討時):

- [ ] 新しい Behavior 追加時に統合が必要かどうかの判断基準が docs に記載されている
- [ ] 共通化すべき重複が増えた場合に `SpawnActorUseCase` 統合を検討する設計メモが task または guideline にある
- [ ] 現時点での強制統合は行わない（Adventurer 固有ロジックの位置が不明瞭になるリスクがある）

---

### 7. ActorViewDataStore.ConsumeChanges() が内部バッファへの参照を返す

重大度: 低

問題:

`ActorViewDataStore.ConsumeChanges()` は内部フィールド `changedActors` と `removedActorIds` に対して `IReadOnlyList<T>` ビューを返す。次回 `ConsumeChanges()` が呼ばれると `changedActors.Clear()` と `removedActorIds.Clear()` が走るため、呼び出し元が返された参照を次フレームまで保持していると空になる。

```csharp
// WorldActorPresenter.UpdateVisuals() 内
var changes = viewDataProvider.ConsumeChanges();  // 内部 List への参照
foreach (var actor in changes.ChangedActors) { ... }
// 同フレーム内なら安全だが、非同期処理や他コードが ConsumeChanges() を呼ぶと壊れる
```

現状は `WorldActorPresenter.UpdateVisuals()` が同フレーム内で消費するため実害はないが、将来の非同期化や複数 Presenter への拡張時に無言の不具合になりうる。

原因:

GC Alloc を避けるために内部バッファをそのまま返している。このパターンは `.Clear()` のタイミングと参照ライフタイムが一致しないと壊れる設計になっている。

解決案:

1. 返す前に `changedActors` / `removedActorIds` を `ReadOnlyCollection` のスナップショットとして返す（GC Alloc 許容）
2. または `ConsumeChanges()` のコントラクトに「返された参照は次回 ConsumeChanges() 呼び出しで無効になる」を XML コメントで明文化し、呼び出し元に同期消費を強制する
3. API を `ConsumeChanges(Action<ActorViewData> onChanged, Action<Guid> onRemoved)` コールバック形式に変更し、参照問題を排除する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`（ActorViewDataStore.ConsumeChanges()）
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`（UpdateVisuals()）

完了条件:

- [ ] `ConsumeChanges()` の返却 `IReadOnlyList<T>` が次回 `ConsumeChanges()` 呼び出し後も有効か、無効になる旨がコントラクトに明記されている
- [ ] `WorldActorPresenter` 以外が `ConsumeChanges()` を呼んだ場合のライフタイム安全性が保証されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

## その他総合レビュー

### 1. WorldActorPresenter.Dispose() が空実装

重大度: 低

> **検証注記（2026-05-14）:** `WorldActorViewRegistry` はすでに `IDisposable` を実装し、`Dispose()` 内で保持する全 GameObject を `Object.Destroy()` している。かつ `WorldLifetimeScope` では `Lifetime.Scoped` で登録されており、VContainer が LifetimeScope 破棄時に自動的に `Dispose()` を呼ぶ。したがって「GameObjects が残留する可能性がある」という元の記述は事実と異なる。GameObject リークは実際には発生しない。本項目を重大度「低」に下げ、問題説明を修正した。

問題:

`WorldActorPresenter : IDisposable` を実装しているが `Dispose()` メソッドが空で何もしていない。実際の GameObject 破棄は `WorldActorViewRegistry.Dispose()`（Scoped/VContainer 自動呼び出し）が担っており、Scene 遷移時のリークは発生しない。ただし空 `Dispose()` はコード読者に「クリーンアップ責務がどこにあるか」を伝えず、将来 `WorldActorPresenter` が R3 購読や他のリソースを持つようになった場合に cleanup 漏れを招くリスクがある。

原因:

`WorldActorViewRegistry` が `IDisposable` を持ち Scoped で登録されているため、`WorldActorPresenter` 側で明示的な解放を書かなくても動作した。結果として空の `Dispose()` が残り、責務の所在がコードから読み取れない状態になっている。

解決案:

1. `WorldActorPresenter : IDisposable` を実装する必要がなければ `IDisposable` を interface から外し、空 `Dispose()` を削除する（主要解決策）
2. `WorldActorPresenter.Dispose()` を維持する場合は内に `// cleanup is handled by WorldActorViewRegistry.Dispose() via VContainer` のようなコメントを付けて意図を明示する
3. または `WorldActorPresenter` に将来 R3 購読を追加する際に `DisposableBag` を追加し、その時点で `Dispose()` も実装する
4. `WorldActorViewRegistry` が Scoped で Dispose される設計を `docs/design` または LifetimeScope コメントに記録する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`（Dispose()）
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorViewRegistry.cs`（IDisposable 実装確認済み）
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`（Scoped 登録確認済み）

完了条件:

- [ ] `WorldActorPresenter.Dispose()` の空実装に対して、クリーンアップ責務が `WorldActorViewRegistry` にあることをコメントまたは docs で明示している
- [ ] 将来 `WorldActorPresenter` が disposable リソースを持つ場合に備え、Dispose 実装のガイドが task または guideline に記録されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 2. WorldSimulationOrchestrator が pause 中でも DetectCombatEncounterUseCase を実行する

重大度: 低

問題:

`WorldSimulationOrchestrator.AdvanceFrameAsync()` では `shouldAdvanceTimeDependentSystems` フラグで pause 判定をしているが、`detectCombatEncounterUseCase.ExecuteAsync()` はこのフラグのガード外で呼ばれる（L134）。

```csharp
// WorldSimulationOrchestrator.AdvanceFrameAsync()
await detectCombatEncounterUseCase.ExecuteAsync(gameWorldState);  // pause 判定なし
// ...
if (shouldAdvanceTimeDependentSystems && ...)
{
    await advanceCombatUseCase.ExecuteAsync(...);  // pause 時スキップ
}
```

pause 中は新規移動が発生しないため `ActorSpatialIndexService.dirtyActorIds` は通常増えにくく、`DetectCombatEncounterUseCase.BuildDetectionActorIds()` は空またはほぼ空のまま早期終了することが多い。ただし pause 直前フレームで追加された dirty actor が consume されずに残っていた場合は検出が走ることがあり、実害がゼロとは言い切れない。設計意図と実装が乖離しており、将来の変更で pause 中に誤って遭遇検出が走るリスクがある。

原因:

pause 中の挙動を「時間依存処理のスキップ」と定義したが、遭遇検出は dirty Actor ベースで「毎フレームの時間進行」に厳密には依存しないため、ガード外に置いたと考えられる。しかしその判断が docs に記録されていない。

解決案:

`detectCombatEncounterUseCase.ExecuteAsync()` も `shouldAdvanceTimeDependentSystems` ガードに入れるか、「pause 中でも遭遇状態を最新化する理由」を `WorldSimulationOrchestrator` のコメントまたは docs に明記する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`

完了条件:

- [ ] pause 中の `DetectCombatEncounterUseCase` 実行可否がコードまたは docs に明記されている
- [ ] pause 中の挙動が意図通りであることを検証する EditMode test がある、または「dirty Actor がゼロの時に何も起きない」ことが既存テストで証明されている
- [ ] `uloop.cmd compile --project-path Client` が成功している
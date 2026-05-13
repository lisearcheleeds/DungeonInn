# Milestone 5 完了確認レビュー 第4回（Claude Code）

作成日: 2026-05-14

## レビュー方針

`milestone5-completion-review-3-codex.md`（第3回）の現在状態索引・対応ログで「対応済み」とされた項目は本ファイルでは再掲しない。
第3回レビューの未対応・一部対応・延期項目を確認したうえで、現行コードを直接読んで追加の問題を洗い出す。

---

## 現在状態索引（本レビュー）

| 項目 | 重大度 | 分類 |
|---|---|---|
| 設計1: WorldSimulationOrchestrator の18依存 | 中 | 新規 |
| 設計2: WorldMapViewDataProvider 毎フレームラムダ生成 | 中 | 新規 |
| 設計3: DetectCombatEncounterUseCase が BufferedEventPublisher 未使用 | 中 | 新規 |
| 設計4: IActorBehavior 空インターフェース + Domain 具体型依存 | 中 | 再発（3回目） |
| 整合性1: InnEconomyStatus コンストラクタ 13param 残存 | 中 | 一部対応の継続 |
| 整合性2: InnEconomyStatistics.Demand の重複計算 | 低 | 新規 |
| 整合性3: SpawnScheduledAdventurerOrchestrator と Monster 版の LINQ 非対称 | 中 | 新規 |
| パフォーマンス1: AStarPathfinder.TryFindPath() の openSet.Contains が O(n) | 中 | 既存延期の継続 |
| パフォーマンス2: AttackAreaTargetResolver.ContainsFan() の Math.Cos 残存 | 中 | 既存延期の継続 |
| パフォーマンス3: SpawnScheduledAdventurerOrchestrator 毎 schedule tick LINQ Alloc | 中 | 新規 |
| パフォーマンス4: Actor.RefreshParams() 内の new ActorParamCalculator() 毎回生成 | 低 | 新規 |
| 重複1: InnEconomyStatus の 12 alias proxy プロパティ残存 | 中 | 一部対応の継続 |
| 重複2: SpawnAdventurerUseCase / SpawnMonsterUseCase 並列構造 | 低 | 既存延期継続 |
| 重複3: ActorViewDataStore.ConsumeChanges() の内部参照返却 | 低 | 新規 |
| 総合1: WorldActorPresenter.Dispose() が空実装 | 中 | 新規 |
| 総合2: WorldLifetimeScope の DI 登録数 60+ | 低 | 新規 |
| 総合3: View / PlayMode テスト証跡不足 | 高 | 既存延期継続 |
| 総合4: DetectCombatEncounterUseCase の pause 中 dirty Actor 消費 | 低 | 新規 |

---

## 設計レビュー

### 1. WorldSimulationOrchestrator のコンストラクタが 18 依存を持つ

重大度: 中

問題:

`WorldSimulationOrchestrator` のコンストラクタが 18 個の UseCase / Orchestrator を直接受け取っている。これは `WorldGameLoopEntryPoint` からゲーム進行パイプラインを Application 層へ移した結果だが、Orchestrator 自体が View 層の代わりに巨大な依存ハブになっている。`application-boundary-guidelines.md` では「Orchestrator はサブドメインごとに分割する」方針を定めており、現状はその方針と合っていない。

原因:

Milestone 5 の主目的が「View からゲーム進行を引き剥がす」だったため、まず 1 対 1 で移管した。Application 内の Orchestrator 分割は Milestone 6 のゲームループ整理と合わせて行う予定だが、まだ着手されていない。

解決案:

spawn、lifecycle、combat、economy といったサブドメインごとに Application 内 Orchestrator を分割し、`WorldSimulationOrchestrator` はそれらを順番に呼ぶ薄いコーディネータに絞る。例として `CombatSimulationOrchestrator`、`ActorLifecycleOrchestrator`（既存の `AdvanceActorLifecycleOrchestrator` とは別）、`InnEconomyOrchestrator` を Application 層に追加する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `WorldSimulationOrchestrator` のコンストラクタが 18 依存未満に削減されている
- [ ] spawn / lifecycle / combat / economy のサブドメインが Application 層の Orchestrator に分割されている
- [ ] `WorldSimulationOrchestrator` は分割された Orchestrator を順番に呼ぶだけになっている
- [ ] 分割した各 Orchestrator の EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 2. WorldMapViewDataProvider.GetLayers() が毎フレームラムダクロージャを生成する

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

---

### 3. DetectCombatEncounterUseCase が BufferedEventPublisher を使わず直接 publish する

重大度: 中

問題:

`AdvanceCombatUseCase` / `AdvanceProjectileUseCase` / `AdvanceAreaEffectUseCase` では `BufferedEventPublisher` を使い、ダメージ・死亡解決・ドロップ完了後にイベントをまとめて発行する設計になった。一方 `DetectCombatEncounterUseCase` は `IEventPublisher`（= `GameEventBus`）へ `CombatEncounterStarted` / `CombatEncounterEnded` を即時 publish する。

`WorldSimulationOrchestrator.AdvanceFrameAsync()` では `detectCombatEncounterUseCase.ExecuteAsync()` が `advanceCombatUseCase.ExecuteAsync()` より先に呼ばれるため、「戦闘遭遇開始イベント → 戦闘未進行」という状態で購読者に通知が届く。`AdventurerBattleRecordService` が `CombatEncounterStarted` で記録を作成し始めると、同フレーム内の `AdvanceCombatUseCase` の `CombatAttackOccurred`（BufferedFlush 後）より先に record 生成が起きる。

原因:

`CombatEncounterStarted/Ended` は「遭遇の開始/終了」を表すため、ダメージ系とは異なり即時通知でも問題ないと判断された可能性があるが、その判断が docs に明記されていない。

解決案:

1. `DetectCombatEncounterUseCase` も `BufferedEventPublisher` を使い、メソッド末尾で `Flush()` する（他 UseCase と統一）
2. または「CombatEncounterStarted は遭遇検出時の即時通知であり、同フレーム内の戦闘結果より先に届く」という契約を `docs/design/game-event-design.md` に明記する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/DetectCombatEncounterUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Event/BufferedEventPublisher.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`（AdvanceFrameAsync 実行順序）
- `docs/design/game-event-design.md`

完了条件:

- [ ] `DetectCombatEncounterUseCase` が `BufferedEventPublisher` を使うか、即時発行の理由が `game-event-design.md` に明記されている
- [ ] `CombatEncounterStarted` / `CombatEncounterEnded` のフレーム内発行順序が docs に契約として記載されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 4. IActorBehavior が空マーカーインターフェースのまま、かつ Domain 内で具体型依存が拡大している

重大度: 中

再発理由:

review-2 で「Milestone 6以降の大規模変更」と判断され、review-3 でも同様の延期判断が下された。しかし現行コードを読むと `Actor.Recover()` 内で `if (Behavior is AdventurerBehavior adventurerBehavior)` が使われており、Domain 層内の具体型依存が View 層だけでなく Domain 自身まで波及している。これは前回指摘時には含まれていなかった場所。

再発防止策:

`IActorBehavior` に `ActorBehaviorKind Kind { get; }` を追加し、新規の型チェック追加時に lint か CI で警告を出す仕組みを入れる。

問題:

`IActorBehavior` はメンバを一切持たないマーカーインターフェース。Domain / Application / View の各層で `behavior is AdventurerBehavior` 等の型チェックが散在している。特に `Actor.Recover()` が Domain 層内で `AdventurerBehavior` に直接キャストし、`ReduceStress()` を呼んでいる。Domain が具体型 Behavior に直接依存することで、新しい Behavior 追加時に Domain コアを変更せざるを得ない。

原因:

`IActorBehavior` のメンバレス設計は「型そのものが役割を表す」意図だったが、Domain 内にその型を参照するコードが増えた結果、型チェック分岐が Domain コアにまで入り込んでいる。

解決案:

1. `IActorBehavior` に `ActorBehaviorKind Kind { get; }` を追加し、各 Behavior 実装に enum 値を返させる
2. `Actor.Recover()` の `is AdventurerBehavior` チェックを `Behavior.Kind == ActorBehaviorKind.Adventurer` に変更し、`AdventurerBehavior` への具体型依存をなくす
3. ストレス軽減などの Behavior 固有ロジックは `IActorBehavior.OnRecover(RecoverContext)` のようなメソッドとして interface に定義し、Behavior 実装側で処理する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/IActorBehavior.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs`（Recover() 内の型チェック）
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`（ResolveBehaviorType()）

完了条件:

- [ ] `IActorBehavior` に識別用メンバ（`Kind` プロパティ等）が追加されている
- [ ] `Actor.Recover()` が `AdventurerBehavior` に直接キャストしていない
- [ ] `ActorViewDataStore.ResolveBehaviorType()` が `IActorBehavior` の識別メンバを使用している
- [ ] Domain 層のコアコード（Actor / ActorEffects 等）に `is AdventurerBehavior` の型チェックが残っていない
- [ ] `docs/guidelines/domain-design-guidelines.md` に Behavior 判定の推奨パターンが記載されている
- [ ] Actor の Behavior 別 Recover / 処理分岐を検証する EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## 整合性レビュー

### 1. InnEconomyStatus コンストラクタが依然として 13 パラメータを受け取る

重大度: 中

問題:

review-3 で `InnEconomySummary` を切り出し `InnEconomyStatus` が `InnEconomySummary Current` を内包する設計になった。しかしコンストラクタは依然として 13 個の個別 int パラメータを受け取り、内部で `InnEconomySummary` に詰め替えている。`InnEconomyStatusCalculator.Calculate()` も `InnDailyReport` から 13 フィールドを個別に取り出して `InnEconomyStatus` コンストラクタへ渡している（合計 13 引数の手動ミラーリング）。`InnEconomySummary` を直接受け取るコンストラクタがなく、`InnEconomySummary` 導入の恩恵が呼び出し側に届いていない。

```csharp
// InnEconomyStatusCalculator.Calculate() 内（現状）
return new InnEconomyStatus(
    report.Day,
    report.Summary.Guests,          // 個別フィールドを手動列挙
    report.Summary.RejectedGuests,
    // ... 11 個続く
);
```

原因:

review-3 の完了条件「`InnEconomyStatus` が UI alias の大量 proxy を持たない」「`InnEconomyStatusCalculator` が report から status へ不要な詰め替えをしていない」はいずれも未チェックのまま対応ログが終了しており、実装が中途半端な状態。

解決案:

1. `InnEconomyStatus` に `public InnEconomyStatus(int currentDay, InnEconomySummary summary)` を追加し、13param コンストラクタを削除する
2. `InnEconomyStatusCalculator.Calculate()` を `new InnEconomyStatus(report.Day, report.Summary)` の 1 行に短縮する
3. alias proxy プロパティ（`GuestsToday`, `RejectedGuestsToday` ... 12 個）は Presenter / ViewModel 側へ移動するか削除する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/InnEconomyStatus.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/InnEconomyStatusCalculator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/InnEconomySummary.cs`（または相当ファイル）

完了条件:

- [ ] `InnEconomyStatus` が `(int currentDay, InnEconomySummary summary)` の 2 引数コンストラクタを持つ
- [ ] 13 引数コンストラクタが削除されている
- [ ] `InnEconomyStatusCalculator.Calculate()` が `InnEconomySummary` を直接渡して `InnEconomyStatus` を生成している
- [ ] `InnEconomyStatus` の alias proxy プロパティが削除されているか、使用理由が Presenter 側コメントに記載されている
- [ ] `InnEconomyUseCaseTests` が新コンストラクタに合わせて更新されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 2. InnEconomyStatistics.Demand プロパティと InnEconomyStatusCalculator が重複計算する

重大度: 低

問題:

`InnEconomyStatistics` には算出プロパティ `Demand { get => Guests + RejectedGuests; }` が定義されている。しかし `InnEconomyStatusCalculator.CalculateDailyReport()` は `statistics.Demand` を使わず、`statistics.Guests + statistics.RejectedGuests` を直接計算して `InnDailyReport` に渡している。同じロジックが 2 箇所に存在し、定義が変わった場合に片方が追従し忘れるリスクがある。

原因:

`CalculateDailyReport()` の実装が `InnEconomyStatistics.Demand` プロパティの存在に気付かず（または意図的に）直接計算している。

解決案:

`InnEconomyStatusCalculator.CalculateDailyReport()` 内の `statistics.Guests + statistics.RejectedGuests` を `statistics.Demand` に変更する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/InnEconomyStatistics.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/InnEconomyStatusCalculator.cs`

完了条件:

- [ ] `InnEconomyStatusCalculator.CalculateDailyReport()` が `statistics.Guests + statistics.RejectedGuests` を直接計算せず `statistics.Demand` を使用している
- [ ] `Demand` の計算ロジックが `InnEconomyStatistics` の 1 箇所にのみ存在する
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 3. SpawnScheduledAdventurerOrchestrator と Monster 版で LINQ 使用パターンが非対称

重大度: 中

問題:

`SpawnScheduledMonsterOrchestrator` は LINQ を使わず `foreach` ループで実装されている（GC Alloc なし）。一方 `SpawnScheduledAdventurerOrchestrator` は毎 schedule tick で以下の LINQ を実行する。

- `worldState.Actors.Count(x => x.Behavior is AdventurerBehavior)` （全 Actor 走査 + ラムダ）
- `spawnTable.Entries.Where(...).ToArray()` （中間配列生成）
- `entries.Sum(entry => entry.Weight)` （中間 Enumerable）

同じスポーン責務を持つ 2 つの Orchestrator が異なるパフォーマンス特性を持っており、Actor 数が増えるとアドベンチャラー版だけが重くなる。

原因:

Monster 版は後から実装されてパフォーマンス改善が適用されたが、Adventurer 版の既存実装に対する同等の改善が行われていない。

解決案:

1. `worldState.Actors.Count(x => x.Behavior is AdventurerBehavior)` を `GameWorldState` に `AdventurerCount` プロパティとしてキャッシュ、または `IActorBehavior.Kind` 比較に変更してラムダアロケーションを減らす
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

---

## パフォーマンスレビュー

### 1. AStarPathfinder.TryFindPath() の openSet.Contains が O(n)

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

### 2. AttackAreaTargetResolver.ContainsFan() に Math.Cos（三角関数）が残っている

重大度: 中

問題:

review-2 / review-3 で「Fan 判定は dot / cross と距離二乗で行い、`Sqrt` / `Atan2` などの三角関数を避ける」と指摘されていた。現行実装では `Math.Sqrt` / `Math.Atan2` は使われていないが、`Math.Cos(halfAngle * Math.PI / 180f)` が残っている。三角関数は整数演算や乗算より大幅にコストが高く、AreaEffect 数 × 近傍 Actor 数が増えると積み上がる。

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

### 3. SpawnScheduledAdventurerOrchestrator が毎 schedule tick に LINQ GC Alloc を発生させる

重大度: 中

問題:

`SpawnScheduledAdventurerOrchestrator.ExecuteAsync()` は毎 schedule tick（ゲーム時間経過ごと）に以下の GC Alloc を発生させる。

1. `worldState.Actors.Count(x => x.Behavior is AdventurerBehavior)` → ラムダ生成
2. `spawnTable.Entries.Where(e => !spawnedSet.Contains(e.EntryId)).ToArray()` → 中間配列 `SpawnTableEntryMaster[]` 生成
3. `entries.Sum(entry => entry.Weight)` → ラムダ生成

Actor 数とスポーンテーブルサイズが増えるほど割り当てが増加する。

原因:

整合性3で述べた通り、Monster 版への LINQ 削減が Adventurer 版に反映されていない。

解決案:

整合性3の解決案と同一。for ループへの変換とカウント専用プロパティの導入。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledAdventurerOrchestrator.cs`

完了条件:

- [ ] `Count(x => ...)` / `Where().ToArray()` / `Sum(...)` が LINQ を使わない実装に変わっている
- [ ] schedule tick ごとの GC Alloc が削減されていることが Profiler または コードレビューで確認できる
- [ ] Adventurer スポーン判定の EditMode test が通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 4. Actor.RefreshParams() が毎回 new ActorParamCalculator() を生成する

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
- [ ] WeaponCombatCalculatorFactory / WeaponCalculatorFactory との設計方針が揃っている
- [ ] `docs/guidelines/domain-design-guidelines.md` に Domain Calculator の扱い方針が記載されている
- [ ] Actor パラメータ再計算の EditMode test が通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## 重複した機能を持つクラス・データクラスレビュー

### 1. InnEconomyStatus の 12 個の alias proxy プロパティが未整理

重大度: 中

問題:

`InnEconomyStatus` は `InnEconomySummary Current` を内包する設計に変わったが、`GuestsToday`, `RejectedGuestsToday`, `DemandToday`, `SalesToday`, `SatisfactionDeltaToday`, `Reputation`, `OccupiedRooms`, `RoomCapacity`, `OccupancyPercent`, `GuildGold`, `RookieSwordStock`, `RookieArmorStock` という 12 個の `Current.Xxx` への委譲プロパティが残っている。review-3 の完了条件「`InnEconomyStatus` が UI alias の大量 proxy を持たない、または互換維持理由が明記されている」は未チェックのまま。

```csharp
// InnEconomyStatus（現状）
public int GuestsToday => Current.Guests;       // Current.Guests の alias
public int RejectedGuestsToday => Current.RejectedGuests; // alias
// ... 10 個続く
```

これは今後 UI 向け ViewModel が追加される際に「どちらを使うか」が曖昧になり、`InnEconomySummary` と `InnEconomyStatus` と ViewModel の三層に同じ値が散在するリスクがある。

原因:

`InnEconomySummary` 導入前の API 互換を維持するために alias プロパティを残した。互換維持の方針が明記されていないため、削除タイミングが宙に浮いている。

解決案:

1. alias プロパティを削除し、呼び出し元をすべて `status.Current.Guests` 形式に統一する
2. または「呼び出し元の Presenter が alias を使う期間だけ維持し、Presenter 側が `InnEconomySummary` を直接参照するよう移行後に削除する」方針を docs に明記する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/InnEconomyStatus.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/InnEconomySummary.cs`（または相当ファイル）

完了条件:

- [ ] `InnEconomyStatus` の alias proxy プロパティが削除されているか、使用継続理由が docs またはファイル内に明記されている
- [ ] 呼び出し元が `status.Current.Xxx` または `InnEconomySummary` を直接参照している
- [ ] `InnEconomyUseCaseTests` が更新後も通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 2. SpawnAdventurerUseCase / SpawnMonsterUseCase のフロントエンド並列構造が継続

重大度: 低

問題:

review-2 / review-3 で「ActorFactoryCore による共通化は完了、上層 interface が冗長」と指摘され、Factory 層は統合済み。しかし UseCase 層では `SpawnAdventurerUseCase` と `SpawnMonsterUseCase` が引き続き並列に存在し、実体的な処理フローが類似している。どちらも `ActorFactory` を呼んで Actor を生成し、GameWorldState に追加する。

今後 NPC / Pet などの Behavior が追加された場合、新しい `SpawnXxxUseCase` が生まれるリスクがある。

原因:

UseCase 統合はMilestone 6 の移動・AI 整理と合わせて対応する方針で延期されており、追跡は継続中。

解決案:

`SpawnActorUseCase` に統合し、Behavior 指定は `ActorFactoryRequest.Behavior` で行う。既存の `SpawnAdventurerUseCase` / `SpawnMonsterUseCase` は `SpawnActorUseCase` のラッパーまたは削除する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnAdventurerUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnMonsterUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/ActorFactory.cs`

完了条件:

- [ ] `SpawnAdventurerUseCase` / `SpawnMonsterUseCase` が削除されているか、統合済みの `SpawnActorUseCase` 呼び出しに変わっている
- [ ] 新しい Behavior を追加する際に新規 UseCase を作らなくてよい
- [ ] Adventurer / Monster スポーンの EditMode test が通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 3. ActorViewDataStore.ConsumeChanges() が内部バッファへの参照を返す

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

---

## その他総合レビュー

### 1. WorldActorPresenter.Dispose() が空実装

重大度: 中

問題:

`WorldActorPresenter : IDisposable` を実装しているが `Dispose()` メソッドが空で何もしていない。`WorldActorPresenter` は内部的に `WorldActorViewRegistry` を保持しており、Actor の `GameObject` が追加されている場合でも `Dispose()` 呼び出しで何もクリーンアップされない。Scene が閉じる際（LifetimeScope の破棄時）に GameObjects が残留する可能性がある。

一方で VContainer の Scoped 登録では IDisposable を実装するクラスは LifetimeScope 破棄時に `Dispose()` が呼ばれる。実際に Resource のリークが起きているかは Scene の遷移テストがないと確認困難。

原因:

`Dispose()` パターンを形式として実装したが、実際に解放すべきリソース（`WorldActorViewRegistry` 内の View オブジェクト）の管理が `WorldActorPresenter` の責務か `WorldActorViewRegistry` の責務かが不明確なため、空のままになっている。

解決案:

1. `WorldActorViewRegistry` に `Clear()` / `Dispose()` を実装し、保持する GameObject を Destroy する
2. `WorldActorPresenter.Dispose()` から `actorViewRegistry.Clear()` を呼ぶ
3. または `WorldActorViewRegistry` も `IDisposable` として DI Scoped 登録し、LifetimeScope 破棄で自動クリアされるようにする

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`（Dispose()）
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorViewRegistry.cs`

完了条件:

- [ ] `WorldActorPresenter.Dispose()` または `WorldActorViewRegistry` のいずれかが Actor GameObject を適切に Destroy / Clear する
- [ ] Scene 閉鎖時に Actor GameObject がヒエラルキーに残留しないことを確認済み（PlayMode または DI 解体テスト）
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 2. WorldLifetimeScope の DI 登録数が 60 超

重大度: 低

問題:

`WorldLifetimeScope.Configure()` に 60 以上の DI 登録がある。Lighthouse の LifetimeScope パターン上は単一 scope への多数登録は許容されているが、以下の実務上の問題がある。

1. EditMode test でシーン全体の DI 構成を検証する際、60+ クラスのインスタンス化コストが高い
2. 追加・削除の際にファイルが巨大すぎて差分が読みにくい
3. `WorldSimulationOrchestrator` の依存解消（設計1）が完了するまで、この登録数は増え続ける

原因:

ゲーム進行パイプラインを Application 層へ移したことで、World Scope に登録が集中している。Application 内 Orchestrator 分割が完了するまでの一時的な状態。

解決案:

設計1（WorldSimulationOrchestrator 分割）完了後、Combat / Actor / Economy など機能カテゴリごとに `Installer` または `SubLifetimeScope` を分割する。Lighthouse の Installer パターン（P8）を参照する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`
- `docs/guidelines/lighthouse-patterns.md`（P8 Installer パターン）

完了条件:

- [ ] `WorldLifetimeScope.Configure()` の登録数が機能 Installer への分割で 30 以下になっている（またはその方針が task に記録されている）
- [ ] 分割した Installer のテストが通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 3. View / PlayMode テスト証跡不足（継続指摘）

重大度: 高

問題:

review-3 でも指摘された View 層のテスト不足が継続している。EditMode テスト（225 件）は Application / Domain の単体テストが中心であり、以下が未カバー。

- `WorldMapView.UpdateVisuals()` のチャンクキュー制御・Layer 追加検知
- `WorldActorPresenter.UpdateVisuals()` の差分更新・カメラ yaw 変更
- `WorldLifetimeScope` の DI 構成全体解決
- `WorldSimulationOrchestrator.AdvanceFrameAsync()` のゲーム進行順序

これらのテストがないため、View とゲーム進行の統合部分の回帰を PlayMode 手動確認に頼っている状態が続いている。

原因:

Unity の PlayMode テストは MonoBehaviour の依存が多く、EditMode で書くには Abstract Factory が必要。Milestone 5 で View 接続を終えた後のテスト整備が次マイルストーンに先送りされている。

解決案:

1. `WorldMapView` のチャンクキュー制御は `IWorldMapViewDataProvider` モックで EditMode test 可能
2. `WorldActorPresenter` の差分更新は `IActorViewDataProvider` モックで EditMode test 可能
3. `WorldSimulationOrchestrator` のゲーム進行順序テストはすでに `WorldGameLoopEntryPointArchitectureTests` が存在するが、実際の UseCase 呼び出し順を検証するテストを追加する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Tests/EditMode/WorldGameLoopEntryPointArchitectureTests.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`

完了条件:

- [ ] `WorldMapView` のチャンクキューイング・Layer 追加を検証する EditMode test がある
- [ ] `WorldActorPresenter` の Actor 生成・削除・移動・カメラ yaw 変更を検証する EditMode test がある
- [ ] `WorldSimulationOrchestrator.AdvanceFrameAsync()` のゲーム進行呼び出し順を検証するテストがある
- [ ] `WorldLifetimeScope` の主要 DI 解決を確認する PlayMode または構成テストがある
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 4. WorldSimulationOrchestrator が pause 中でも DetectCombatEncounterUseCase を実行する

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

pause 中は Actor が移動しないため `ActorSpatialIndexService.dirtyActorIds` が増えず、`DetectCombatEncounterUseCase.BuildDetectionActorIds()` は空のまま早期終了するため実害は小さい。ただし設計意図と実装が乖離しており、将来の変更で pause 中に誤って遭遇検出が走るリスクがある。

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

---

## 推奨対応順

### Milestone 6 開始前に対応推奨（優先）

| 優先 | 項目 | 理由 |
|---|---|---|
| 1 | 整合性1: InnEconomyStatus コンストラクタ統合 | review-3 の未チェック完了条件。修正コスト低。 |
| 2 | 重複1: InnEconomyStatus alias proxy 整理 | review-3 の未チェック完了条件。上と同時対応可。 |
| 3 | 整合性2: InnEconomyStatistics.Demand 重複 | 1 行の修正。副作用なし。 |
| 4 | 設計3: DetectCombatEncounterUseCase のイベント発行方針明記 | docs 更新のみで可。コード変更不要な場合も。 |
| 5 | 総合1: WorldActorPresenter.Dispose() 空実装 | Scene 遷移で GameObject 残留リスク。早期修正推奨。 |
| 6 | 総合3: View / PlayMode テスト証跡 | Milestone 6 拡張前にテスト基盤を作る。 |

### Milestone 6 で合わせて対応

| 項目 | 理由 |
|---|---|
| 設計1: WorldSimulationOrchestrator 18依存 | Application Orchestrator 分割と一体化 |
| 設計2: GetLayers() ラムダ生成 | Floor 追加イベント化と一体化 |
| 設計4: IActorBehavior 空インターフェース | 大規模変更のため AI / Behavior 整理と同タイミング |
| パフォーマンス1: A* openSet.Contains O(n) | NavMesh 追加後に A* を限定利用する方針確定後 |
| パフォーマンス2: ContainsFan Math.Cos | AreaEffect 数が増える前に修正 |
| パフォーマンス3,4: Adventurer Orchestrator LINQ / Actor.RefreshParams | Actor 数増加前に修正 |
| 重複2: SpawnUseCase 並列構造 | 移動・AI 整理と合わせて |
| 重複3: ActorViewDataStore 内部参照 | 非同期化・複数 Presenter 化前に修正 |
| 整合性3: Spawn Orchestrator LINQ 非対称 | Actor 数・Floor 数増加前に修正 |
| 総合2: WorldLifetimeScope 60+ | 設計1 完了後に Installer 分割 |
| 総合4: pause 中 DetectCombatEncounterUseCase | 低リスク、ゲームループ整理時に整理 |

### 別タスクとして追跡（既存）

| 項目 | 備考 |
|---|---|
| Launcher.cs SceneManager.LoadSceneAsync | bootstrap / reboot 例外として review-3 続き4 で記録済み |

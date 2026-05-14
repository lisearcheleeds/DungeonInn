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

### 3. Debug Log Presenter が本番 Presenter として広い world state を View 層から直接読める

重大度: 中

問題:

`WorldGameLogPresenter` は現状 `Debug.Log` 出力のみを行う開発診断用途のクラスだが、名前・配置・DI 登録上は通常の World Presenter と同じ扱いになっている。その状態で `IGameWorldStateReader` を直接注入し、Actor、Guild、施設予約、所持金などを読みながらログ文字列を組み立てている。

Milestone 5 roadmap は UI 表示を専用 Query / ReadModel / DTO に寄せる方針を定めている。プレイヤー向けログ、通知、履歴 UI、分析などの本番機能として扱う場合、View 層が広い Domain 集約に到達できる状態は境界違反となる。一方、現状の用途が Debug.Log のみであるなら、本番 Presenter に間借りさせるのではなく、`#if DEBUG` でのみコンパイル・DI 登録される diagnostics 専用クラスへ隔離するのが妥当。

> **検証注記（2026-05-14）:** コードを直接確認した結果、`Initialize()` メソッドは `if (!Debug.isDebugBuild) { return; }` で始まり、subscriptions は Debug build 以外では登録されない。ただし DI constructor は常に `IGameWorldStateReader worldState` を受け取り、`OnActorWaitingForInn()`・`OnActorReservedInn()`・`OnItemPickedUp()` 内で `worldState.Guild.GetFacility()`・`worldState.FindActor()` を呼んで補助情報を取得している。実行は Debug build に限定されるが、依存関係と読み取り経路はコード上に常に存在する。完了条件「Debug.isDebugBuild に境界違反の許容を依存していない」はこの状態を指している。

原因:

開発診断ログと将来の本番ログ UI の責務が分離されていない。現状は Debug.Log 出力だけにもかかわらず、クラス名と登録位置が通常機能の Presenter として見えるため、広い world state 依存が View 層の通常依存として残っている。

また、ログに必要な補助情報が event DTO または narrow query として用意されていないため、Presenter が不足情報を `IGameWorldStateReader` から直接補っている。具体的には `ActorWaitingForInn` イベント DTO が `InnFacilityId` のみを持ち、room 数・Gold などを持たないため、Presenter が `worldState` から後引きせざるを得ない。

解決案:

短期対応として、現行 `WorldGameLogPresenter` は Debug.Log 以外の本番処理を持たないため、`WorldDebugGameLogPresenter` などの diagnostics 専用クラスへリネーム・移動する。クラス本体と `WorldLifetimeScope` の DI 登録を `#if DEBUG` で囲み、Release ビルドではコンパイル・Resolve されない状態にする。広い `IGameWorldStateReader` 依存はこの diagnostics 専用例外として扱い、クラスコメントで「Debug diagnostics only. Do not use for runtime UI.」のように明記する。

将来、プレイヤー向けログ、通知、履歴 UI、分析などを作る場合は、この debug クラスを昇格させず、Application 層に event history / narrow read model / query を新設する。Presenter は `IGameWorldStateReader` ではなく、表示に必要な DTO だけを参照する。イベントに含めるべき事実情報と、Query で後引きする表示補助情報をその時点で整理する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLogPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/IGameWorldState.cs`
- `docs/roadmap/milestone5-roadmap.md`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] 現行 Debug.Log 用途のクラスが `WorldDebugGameLogPresenter` など diagnostics 専用名・専用配置になっている
- [ ] diagnostics 専用クラス本体と DI 登録が `#if DEBUG` で囲まれ、Release ビルドではコンパイル・Resolve されない
- [ ] diagnostics 専用クラスに、広い world state 依存が Debug diagnostics 例外であり runtime UI に使わないことがコメントまたは docs で明記されている
- [ ] 将来プレイヤー向けログ / 通知 / 履歴 UI を作る場合は、event history / narrow Application query / read model を新設する方針が task または docs に記録されている

### 4. WorldMapViewDataProvider.GetLayers() が毎フレームラムダクロージャを生成する

重大度: 中

問題:

`WorldMapViewDataProvider.GetLayers()` は毎フレーム `WorldGameLoopEntryPoint.Update()` → `WorldMapView.UpdateVisuals()` 経由で呼ばれる。内部の `GetOrCreateLayer()` は `Func<GridPosition, WorldMapCellViewKind> resolveCellKind` を受け取り、呼び出し元が `groundMap`・`DungeonFloor` を捕捉するラムダを渡している（Ground 用 1 個 + Dungeon Floor 数分）。

キャッシュヒット後にラムダの本体は呼び出されないが、`position => groundMap.IsWalkable(position)` や `position => ResolveDungeonCellKind(floor, position)` のラムダオブジェクト自体は `GetLayers()` 呼び出しごとに生成される可能性がある。単に private 関数へ切り出しても、`position => ResolveGroundCellKind(groundMap, position)` のように文脈オブジェクトを捕捉するラムダを渡し続ける限りクロージャ生成は残る。

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

`GetOrCreateLayer()` メソッドシグネチャが `Func<>` を受け取る設計のため、呼び出し元で「Layer 種別ごとのセル解決」と「キャッシュ取得」を同じ汎用メソッドに詰め込んでいる。Layer が静的（マップは完成後に変化しない）にもかかわらず、キャッシュ済み Layer に対しても `GetLayers()` 内で捕捉ラムダを作る構造になっている。

解決案:

短期対応として、`GetOrCreateLayer()` の `Func<>` 引数をやめ、Ground / Dungeon 用の private 作成メソッドに分ける。各メソッドは冒頭で `cachedLayers.TryGetValue(...)` を確認し、キャッシュ済みならセル生成処理へ入らず即 return する。新規作成が必要な場合だけ、メソッド内の通常ループで `ResolveGroundCellKind(groundMap, position)` / `ResolveDungeonCellKind(floor, position)` を呼ぶ。

つまり、`position => ...` を private 関数に置き換えるだけではなく、ラムダを `Func<>` として渡す構造そのものをなくす。より大きな整理としては、`WorldMapView` が毎フレーム `GetLayers()` を呼ばず、新規 layer 追加時だけ通知または差分取得する設計にする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`（WorldMapViewDataProvider.GetLayers()）
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`（UpdateVisuals）

完了条件:

- [ ] `GetOrCreateLayer()` の `Func<GridPosition, WorldMapCellViewKind>` 引数が削除されている
- [ ] Ground / Dungeon Floor の Layer 作成が private メソッドに分離され、各メソッド冒頭でキャッシュヒット時に即 return する
- [ ] `GetLayers()` 内に `position => ...` の捕捉ラムダが残っていない
- [ ] 毎フレームの GC Alloc がラムダ生成由来でゼロになっていることを Profiler で確認済み
- [ ] Dungeon Floor 追加時にキャッシュが適切に追加されることを EditMode test で確認済み
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 5. 戦闘系イベントのフレーム内発行順序契約が docs にない

重大度: 中

問題:

`AdvanceCombatUseCase` / `AdvanceProjectileUseCase` / `AdvanceAreaEffectUseCase` では `BufferedEventPublisher` を使い、ダメージ・死亡解決・ドロップ完了後にイベントをまとめて発行する設計になった。一方 `DetectCombatEncounterUseCase` は `IEventPublisher`（= `GameEventBus`）へ `CombatEncounterStarted` / `CombatEncounterEnded` を即時 publish する。

`WorldSimulationOrchestrator.AdvanceFrameAsync()` では `detectCombatEncounterUseCase.ExecuteAsync()` が `advanceCombatUseCase.ExecuteAsync()` より先に呼ばれるため、「戦闘遭遇開始イベント → 戦闘未進行」という状態で購読者に通知が届く。`AdventurerBattleRecordService` が `CombatEncounterStarted` で記録を作成し始めると、同フレーム内の `AdvanceCombatUseCase` の `CombatAttackOccurred`（BufferedFlush 後）より先に record 生成が起きる。

この順序自体は「遭遇状態変更」と「攻撃・ダメージ結果」を別イベントとして扱う設計として成立する。問題は、`CombatEncounterStarted` / `CombatEncounterEnded` だけでなく、通常攻撃、projectile、area effect、死亡解決、経験値、ドロップまで含めた戦闘系イベント全体のフレーム内発行順序が `docs/design/game-event-design.md` に契約としてまとまっていない点にある。将来このイベント群を利用する実装者が、同フレーム内の後続イベントが既に届いていると誤解するリスクがある。

原因:

`CombatEncounterStarted/Ended` は「遭遇の開始/終了」を表すため、ダメージ系とは異なり即時通知でも問題ないと判断された可能性があるが、その判断が docs に明記されていない。また、戦闘系イベントの publish 順が各 UseCase / Orchestrator の実装を読まないと分からず、購読者がどのイベント順を前提にしてよいか判断しにくい。

解決案:

> **訂正（Codex レビュー 2026-05-14）:** 旧解決案1「`BufferedEventPublisher` を使い末尾で `Flush()`」は問題を解決しない。`Flush()` を UseCase 末尾で呼んでも `DetectCombatEncounterUseCase` の実行は `advanceCombatUseCase.ExecuteAsync()` より前のまま（`WorldSimulationOrchestrator` の実行順序は変わらない）。`CombatEncounterStarted` が戦闘結果より先に届くという事実は Buffer 統一では変わらない。主要解決策は docs への契約明記。

`docs/design/game-event-design.md` に、戦闘系イベントのフレーム内発行順序を契約として記載する。`CombatEncounterStarted` / `CombatEncounterEnded` だけを個別に説明するのではなく、`WorldSimulationOrchestrator.AdvanceFrameAsync()` 内の戦闘関連処理順に沿って、以下を番号付きリストなどの形式で整理する。

1. 遭遇検出フェーズ
   - `DetectCombatEncounterUseCase`
   - `CombatEncounterStarted` / `CombatEncounterEnded`
   - `ActorAiDecisionRecorded(StartCombat)` など
   - 同フレームの攻撃・ダメージ結果より先に届く
2. 通常攻撃フェーズ
   - `AdvanceCombatUseCase`
   - `CombatAttackOccurred`
   - 攻撃効果により projectile / area が生成される場合は `ProjectileFired` / `AreaEffectCreated`
   - UseCase 内では `BufferedEventPublisher` に蓄積し、フェーズ末尾で flush される
3. 死亡解決フェーズ
   - 通常攻撃・projectile・area effect の各処理内で対象死亡が確定した場合に実行される
   - `ExperienceGranted`
   - `ActorLeveledUp`
   - `ItemDropped`
   - `CombatEncounterEnded`
   - `ActorDefeated`
   - これらは状態変更後に buffer から順に publish される
4. Projectile フェーズ
   - `AdvanceProjectileUseCase`
   - `ProjectileHit`
   - 命中時に linked effect があれば追加の `CombatAttackOccurred` / `AreaEffectCreated` 等が続く
   - フェーズ末尾で flush される
5. Area Effect フェーズ
   - `AdvanceAreaEffectUseCase`
   - `AreaEffectHit`
   - linked effect があれば追加イベントが続く
   - フェーズ末尾で flush される

この順序は「購読者がゲーム状態を変更しない」前提の通知順であり、購読者が同フレーム内の後続イベントに依存する処理を書く場合は、設計 docs の順序契約を参照することも明記する。

もし `CombatEncounterStarted` を受けた購読者が同フレームの戦闘結果に依存することが問題になるなら、その時点で `WorldSimulationOrchestrator` の実行順序（detect → combat → flush）を見直すか、FrameEnd 後の後処理フェーズに遭遇通知を分割する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/DetectCombatEncounterUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Event/BufferedEventPublisher.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`（AdvanceFrameAsync 実行順序）
- `docs/design/game-event-design.md`

完了条件:

- [ ] `docs/design/game-event-design.md` に戦闘系イベントのフレーム内発行順序が番号付きで記載されている
- [ ] `CombatEncounterStarted` / `CombatEncounterEnded` が同フレーム内の攻撃・ダメージ結果より先に届くことが明記されている
- [ ] 通常攻撃 / projectile / area effect / defeat / drop / reward の publish 順が現行コードと一致している
- [ ] 発行順に依存する購読者（`AdventurerBattleRecordService` 等）の想定が docs と一致している
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 6. IActorBehavior が空マーカーインターフェースのまま、かつ Domain 内で具体型依存が拡大している

重大度: 中

再発理由:

review-2 で「Milestone 6以降の大規模変更」と判断され、review-3 でも同様の延期判断が下された。しかし現行コードを読むと `Actor.Recover()` 内で `if (Behavior is AdventurerBehavior adventurerBehavior)` が使われており、Domain 層内の具体型依存が View 層だけでなく Domain 自身まで波及している。これは前回指摘時には含まれていなかった場所。

再発防止策:

`IActorBehavior` に型別分岐が不要な polymorphic hook を定義し、Domain コアが具体型を参照しなくて済む設計にする。新規 Behavior 追加時の型チェック分散を CI で警告する仕組みを入れる。

問題:

`IActorBehavior` はメンバを一切持たないマーカーインターフェースであり、これは `domain-design-guidelines.md` の方針（`ActorBehaviorType` を持たせない）に沿った意図的な設計である。問題は空マーカーであること自体ではなく、Domain コアが `AdventurerBehavior` 具体型に直接依存している点にある。具体的には `Actor.Recover()` が Domain 層内で `if (Behavior is AdventurerBehavior adventurerBehavior)` とキャストして `ReduceStress()` を呼んでおり、新しい Behavior 追加時に Domain コアを変更せざるを得ない。Application 層（`WorldViewDataProviders.cs` の `ResolveBehaviorType()`）でも同様の型チェックが散在している。

原因:

Domain コアが具体型 Behavior に依存する polymorphic hook が `IActorBehavior` に定義されていないため、`Actor.Recover()` が型チェックで具体型に到達する必要が生じている。

解決案:

> **訂正（Codex レビュー 2026-05-14）:** 旧解決案1・2 の `IActorBehavior.Kind`/`ActorBehaviorKind` 追加は、`docs/guidelines/domain-design-guidelines.md` 第2節「IActorBehavior に ActorBehaviorType Type を持たせない」と正面衝突するため採用しない。
>
> **方針整理（2026-05-15）:** `ActorBehaviorType` ルールは緩和しない。過去にこのルールを置いた理由は、型別分岐で十分な箇所に Runtime Behavior へ enum 識別子を重複保持させる設計を避けるためであり、今回も `Behavior.Type == Adventurer` のような分岐へ置き換えても Domain コアの型別分岐問題は解決しない。`Actor.Recover()` の問題は「Actor が Behavior 具体型を知っていること」であり、`ActorBehaviorType` を追加しても `AdventurerBehavior` キャストが enum switch に変わるだけで本質は残る。

採用方針:

1. `IActorBehavior` に `void OnRecovered(int hpAmount, int mpAmount, int fatigueReduction, int stressReduction, int injuryReduction)` を追加する
2. `Actor.Recover()` は `AdventurerBehavior` へ直接キャストせず、既存の `Recover()` 引数をそのまま `Behavior.OnRecovered(...)` に渡す
3. `AdventurerBehavior.OnRecovered()` は `stressReduction` を使って `ReduceStress()` を呼ぶ
4. `MonsterBehavior` / `PetBehavior` / `GuildStaffBehavior` など、現時点で回復時の固有処理を持たない Behavior は no-op 実装にする
5. `RecoverContext` は現時点では導入しない。将来 `Recover` / `Damage` の hook 引数をまとめて構造化する段階で、`RecoverContext` / `DamageContext` の struct 化を検討する
6. `docs/guidelines/domain-design-guidelines.md` に、Behavior 固有処理は `ActorBehaviorType` ではなく polymorphic hook で扱う方針を記載する

補足:

`OnRecovered()` へ渡す値は、まずは `Actor.Recover()` に渡された引数をそのまま渡す。実回復量（上限 clamp 後に実際に増えた HP など）を扱う必要が出た場合は、その時点で `RecoverContext` を導入し、要求値と実適用値を名前で分ける。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/IActorBehavior.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs`（Recover() 内の型チェック）
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`（ResolveBehaviorType()）
- `docs/guidelines/domain-design-guidelines.md`（第2節: IActorBehavior に識別子プロパティを持たせない）

完了条件:

- [ ] `IActorBehavior` に `OnRecovered(int hpAmount, int mpAmount, int fatigueReduction, int stressReduction, int injuryReduction)` が定義されている
- [ ] `Actor.Recover()` が `AdventurerBehavior` に直接キャストしていない
- [ ] Domain 層のコアコード（Actor / ActorEffects 等）に `is AdventurerBehavior` の型チェックが残っていない
- [ ] `ActorBehaviorType` / `IActorBehavior.Kind` による Runtime Behavior 判定へ置き換えていない
- [ ] 回復時固有処理が不要な Behavior は `OnRecovered()` を no-op 実装している
- [ ] `docs/guidelines/domain-design-guidelines.md` に Behavior 固有処理は polymorphic hook で扱う方針が記載されている
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

### 3. ActorProfileRegistry と探索単位実績の責務境界が未定義

重大度: 中

問題:

`ActorProfile` は `ActorId`、`DisplayName`、`ArchetypeId`、`SpeciesId`、`BehaviorType` を保持し、`WorldGameLogPresenter` の表示名解決と `AdventurerReturnTrackingService` の戦闘帰還判定の両方で使われている。

恒久的な Actor profile / spawn metadata registry として `ActorProfileRegistry` を持つこと自体は問題ない。`DisplayName`、`ArchetypeId`、`SpeciesId`、生成時 Behavior 種別のような「Actor が生成された時点で確定し、その後も参照したい情報」は、削除済み Actor も含めて保持する価値がある。

問題は、`ActorProfileRegistry` が恒久 profile registry なのか、表示用 directory なのか、探索単位の一時 state holder なのかが docs 上で未定義な点にある。特に「探索開始から今回の探索中に倒したモンスター数」のような run / exploration 単位の状態は、Actor の恒久 profile ではなく、探索開始時に reset される別の state として扱う必要がある。

> **検証注記（2026-05-14）:** コードを直接確認した結果、`AdventurerReturnTrackingService.OnActorDefeated()` が `profileRegistry.TryGetProfile(gameEvent.ActorId, out var profile)` を呼び、`profile.SpeciesId` で倒したモンスターの種族を集計している。これは gameplay logic（帰還判断の素材）として `ActorProfileRegistry` を使う具体的な実例。`WorldGameLogPresenter.GetName()` も同じ registry から `DisplayName` を取得しており、表示用と gameplay 用が同一 registry に混在していることをコードレベルで確認した。

原因:

イベントが ActorId 中心で発行されるため、後から表示名や species / archetype を解決する side channel として `ActorProfileRegistry` が導入された。その後、倒したモンスターの species 集計のような探索単位の gameplay state も同じ周辺に置かれ、恒久 profile と探索中 achievement の境界が曖昧になった。

解決案:

責務を以下の2つに分ける。

1. `ActorProfileRegistry`
   - 恒久的な Actor profile / spawn metadata registry として維持する
   - 保持してよい情報は `ActorId`、`DisplayName`、`ArchetypeId`、`SpeciesId`、生成時 Behavior 種別など、Actor 生成時に確定し、Actor 削除後も参照したい lifetime metadata に限定する
   - 将来 lifetime aggregate（生成から現在までの総撃破数など）を持つ場合も、探索単位ではなく Actor 生涯累積として意味がある情報に限定する
2. `ActorExplorationAchievementRegistry`
   - 探索開始から探索終了までの一時的な実績を保持する registry として新設する
   - 保持対象は「今回の探索で倒したモンスター数」「今回の探索で倒した種族別カウント」「今回の探索で拾ったアイテム数」など、探索開始時に reset される achievement に限定する
   - 探索開始時に reset し、探索終了・死亡・退場時に cleanup する

`AdventurerReturnTrackingService` の `defeatedMonsterCountsByActor` は「探索を開始してからの種族別撃破数」として扱うため、恒久 `ActorProfileRegistry` ではなく `ActorExplorationAchievementRegistry` 側へ移す。`dirtyActorIds` は achievement ではなく評価対象 queue / transient flag なので、`AdventurerReturnTrackingService` か別の transient state holder に残し、achievement registry へ混ぜない。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Profiles/ActorProfile.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Profiles/ActorProfileRegistry.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/CompleteActorSpawnUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdventurerReturnTrackingService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLogPresenter.cs`

完了条件:

- [ ] `ActorProfileRegistry` が恒久的な Actor profile / spawn metadata registry であると docs またはクラスコメントに明記されている
- [ ] `ActorProfileRegistry` に保持してよい情報が lifetime metadata / lifetime aggregate に限定され、探索単位・戦闘単位・frame 単位の transient state を保持しない契約が明記されている
- [ ] 探索開始からの実績を保持する `ActorExplorationAchievementRegistry` が新設されている
- [ ] `defeatedMonsterCountsByActor` 相当の種族別撃破数が `ActorExplorationAchievementRegistry` に移り、探索開始時に reset される
- [ ] 探索終了・死亡・退場時に `ActorExplorationAchievementRegistry` の該当 Actor entry が cleanup される
- [ ] `dirtyActorIds` など評価対象 queue / transient flag が achievement registry に混在していない
- [ ] 恒久 profile と探索単位 achievement の寿命差を検証する EditMode test がある

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

### 6. ~~SpawnAdventurerUseCase / SpawnMonsterUseCase のフロントエンド並列構造が継続~~

→ **今回の未解決項目から除外**（2026-05-15）

現行コードでは `SpawnAdventurerUseCase` と `SpawnMonsterUseCase` の責務差が明確にある。両 UseCase は Actor を生成して返すのみで、GameWorldState への登録は `SpawnScheduledAdventurerOrchestrator` および対応 Monster Orchestrator が担っている。また Adventurer 版は rookie equipment 支給、guild transaction 記録、lifecycle 初期化、初期装備 equip、level 条件チェックを持ち、Monster 版との単純重複とは言いにくい。

今すぐ統合すると Adventurer 固有ロジックの置き場所が曖昧になるため、Milestone 5 完了レビューの修正対象からは外す。NPC / Pet / Staff など新しい Behavior の spawn が増え、UseCase 並列構造が再び問題になった時点で、Milestone 6 以降の Behavior / Spawn 設計レビュー対象として扱う。

---

### 7. ~~ActorViewDataStore.ConsumeChanges() が内部バッファへの参照を返す~~

→ **今回の未解決項目から除外**（2026-05-15）

`ActorViewDataStore.ConsumeChanges()` は内部フィールド `changedActors` と `removedActorIds` に対して `IReadOnlyList<T>` ビューを返すため、次回 `ConsumeChanges()` 呼び出しで内容が無効になる。ただし現状の呼び出し元は `WorldActorPresenter.UpdateVisuals()` のみであり、同フレーム内で同期的に即時消費している。現時点では実害がなく、GC Alloc 回避を優先した実装として維持する。

将来、`ConsumeChanges()` の結果をフレームをまたいで保持する、非同期処理へ渡す、複数 Presenter / consumer が読む、などの用途が出た場合に再検討する。その時点では、内部バッファ参照の無効化コントラクトを明文化する、snapshot を返す、または callback 形式に変更する。

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

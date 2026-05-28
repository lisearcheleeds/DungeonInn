# ActorActionPhase システム 実装完了レビュー (task_0001〜task_0004)

作成日: 2026-05-29  
レビュー担当: Claude Code  
対象タスク: task_0001（AIクールダウン硬直）/ task_0002（Phaseシステム基盤）/ task_0003（Phase統合）/ task_0004（Viewアニメーション統合）

## レビュー体制

| フェーズ | 担当 |
|---|---|
| 先行フェーズ: Coding Rules | 専任エージェント |
| Lighthouse パターン | 専任エージェント（並列） |
| Domain Design | 専任エージェント（並列） |
| Application Boundary | 専任エージェント（並列） |
| Implementation Quality | 専任エージェント（並列） |
| 統合レビュー・完了可否判定 | Claude Code |

---

## 完了可否判定

**現状: 完了**

task_0005 で指摘1・2（ハードゲート違反）が修正され、compile 0 errors / tests 336/336 pass / PlayMode `[World] GameWorldState initialized` 確認済み。  
指摘3（`AdventurerAiPolicy` 設計）は **Policy B 確定**（設計意図を task_0001 ログに記録）。application-boundary-guidelines.md のハードゲートは UseCase に特定されており Policy クラスは対象外のため、コード変更なし。

---

## 指摘一覧

| # | 指摘タイトル | 重大度 | ハードゲート | 状態 |
|---|---|---|---|---|
| 1 | `>=` 比較演算子の使用 | 重大 | Yes (Coding Rules) | 対応済み（task_0005） |
| 2 | テスト用コンストラクタが Runtime コードに含まれている | 重大 | Yes (ImplQuality) | 対応済み（task_0005） |
| 3 | `AdventurerAiPolicy` が長期状態と `IDisposable` を持つ | 高 | 設計判断要 | Policy B 確定（task_0001 ログ記録） |
| 4 | `TickAll` で毎フレーム List を new している | 中 | No | 別タスク化推奨 |
| 5 | フェーズ秒数がマジックナンバー | 中 | No | 別タスク化推奨 |
| 6 | フェーズイベント遅延Flush の設計意図がコメントで未記録 | 低 | No | 別タスク化推奨 |
| 7 | `ActorActionPhaseName/Def/Key` の名前空間と設計ドキュメント分類の齟齬 | 低 | No | 記録のみ（コード変更不要） |
| 8 | `ConsumeRealtimeMovedSeconds` の GC Alloc | 低 | No | 既存コードのため別タスク化 |

---

## 指摘詳細

### 1. `>=` 比較演算子の使用

重大度: 重大

問題:

`ActorActionPhaseRuntimeState.cs` の L13 で `>=` を使用している。
`docs/guidelines/coding-rules.md` Rule 11-1「`<` を使い `>` は使わない（数直線の向きに合わせる）」に違反する。

原因:

`IsCompleted` プロパティが `CurrentPhaseIndex >= Phases.Count` で実装されており、右辺が左辺より大きい向きの比較になっている。

解決案:

```csharp
// Before (違反)
public bool IsCompleted => CurrentPhaseIndex >= Phases.Count;

// After
public bool IsCompleted => Phases.Count <= CurrentPhaseIndex;
```

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Phase/ActorActionPhaseRuntimeState.cs` (L13)
- `docs/guidelines/coding-rules.md` Rule 11-1

完了条件:

- [ ] `ActorActionPhaseRuntimeState.cs` L13 が `Phases.Count <= CurrentPhaseIndex` に変更されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 2. テスト用コンストラクタが Runtime コードに含まれている

重大度: 重大

問題:

以下の2つのコンストラクタが Runtime コードに含まれており、`docs/guidelines/implementation-quality-guidelines.md` §12 のハードゲート「Runtime コードにテスト用 / 互換用 / デバッグ用 constructor を追加していない」に違反する。

2a. `AdventurerAiPolicy` の引数なしコンストラクタ:
```csharp
public AdventurerAiPolicy()
{
}
```
このコンストラクタは `ActorAiTests.cs` L247 と `WorldGameLoopEntryPointArchitectureTests.cs` L418 からテスト目的で使われている。引数なし版は `ActorDefeated` / `ActorDeparted` 購読がないため、Actor 退場後も Dictionary エントリが残留し続けるという動作上の問題もある。

2b. `AdvanceActorAiOrchestrator` の2つ目の `public` コンストラクタ:
```csharp
public AdvanceActorAiOrchestrator(
    ActorDecisionScheduler scheduler,
    IReadOnlyList<IActorAiPolicy> policies,
    ApplyActorAiDecisionUseCase applyActorAiDecisionUseCase,
    IActorActionPhaseStateStore phaseStateStore)
```
テストが `new AdvanceActorAiOrchestrator(scheduler, new[] { policy }, usecase, phaseStore)` の形でこのコンストラクタを使用している。DI が注入する経路ではない `public` コンストラクタが存在する。

原因:

テスト都合でコンストラクタを追加した。Runtime コードのテスト補助は Tests 側の helper / fixture / test double に閉じるべきところを、Runtime 側に漏れている。

解決案:

2a. `AdventurerAiPolicy` の引数なしコンストラクタを削除する。  
テスト側では `IEventSubscriber` の NoOp スタブを渡す:
```csharp
// Tests 側で NullEventSubscriber スタブを定義
sealed class NullEventSubscriber : IEventSubscriber
{
    public Observable<T> OnEvent<T>() where T : class, IGameEvent
        => Observable.Empty<T>();
}

// テストで使用
var policy = new AdventurerAiPolicy(new NullEventSubscriber());
```

2b. `AdvanceActorAiOrchestrator` の2つ目コンストラクタを `internal` に変更し、`InternalsVisibleTo` でテストアセンブリに公開する:
```csharp
// AdvanceActorAiOrchestrator.cs
internal AdvanceActorAiOrchestrator(
    ActorDecisionScheduler scheduler,
    IReadOnlyList<IActorAiPolicy> policies,
    ApplyActorAiDecisionUseCase applyActorAiDecisionUseCase,
    IActorActionPhaseStateStore phaseStateStore)
{ ... }
```
`AssemblyInfo.cs` に `[assembly: InternalsVisibleTo("DungeonInn.Tests.EditMode")]` を追加する（既存 InternalsVisibleTo の有無を事前確認すること）。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Ai/AdventurerAiPolicy.cs` (L17-19)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Ai/AdvanceActorAiOrchestrator.cs` (L53-63)
- `Client/Assets/DungeonInn/Tests/EditMode/ActorAiTests.cs` (L247, L418)
- `Client/Assets/DungeonInn/Tests/EditMode/WorldGameLoopEntryPointArchitectureTests.cs` (L418)
- `docs/guidelines/implementation-quality-guidelines.md` §12

完了条件:

- [ ] `AdventurerAiPolicy` の引数なしコンストラクタが削除されている
- [ ] テスト側が `NullEventSubscriber` スタブを渡す形で動作している
- [ ] `AdvanceActorAiOrchestrator` の2つ目コンストラクタが `internal` になっている
- [ ] `InternalsVisibleTo` でテストアセンブリに公開されている（または別の対応策が取られている）
- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が全 pass している

---

### 3. `AdventurerAiPolicy` が長期状態と `IDisposable` を持つ

重大度: 高

問題:

`AdventurerAiPolicy` は `IActorAiPolicy` を実装しているが、同時に以下の特性を持つ:
- `Dictionary<Guid, int>` × 2 の長期状態を保持している
- `IDisposable` を実装している
- コンストラクタでイベント（`ActorDefeated`、`ActorDeparted`）を購読している

`docs/guidelines/application-boundary-guidelines.md` の設計方針テーブルでは:
- Policy / UseCase: ステートレス、`IDisposable` を実装しない
- Service: 長期状態を保持、`IDisposable` を実装、イベントを購読して状態を更新する

現状の `AdventurerAiPolicy` は実質的に Service の責務を抱えている。application-boundary-guidelines.md の「UseCase が `IDisposable` を実装していない」ハードゲートは UseCase に特定されているが、Policy クラスも同じ設計方針に従うべきである。

原因:

AIクールダウン検出で「前回値からのデルタ」を計算する必要があるが、デルタ追跡状態を Policy 本体に置いた。イベント購読によるクリーンアップも Policy に含まれている。

解決案:

`AdventurerAiPolicyStateService`（仮称）を分離し、差分追跡 Dictionary とイベント購読を Service に委譲する。`AdventurerAiPolicy` はこの Service を DI 注入で受け取り、ステートレスな Policy として整理する。

**ただし、この変更は設計判断を伴うためユーザー確認後に実施すること。**

差分方式の設計意図の記録（task ログへの追記）:
- `AdventurerAiPolicy` がアクターごとの前回値記録（デルタ検知）を行う理由
- Actor エンティティや Domain に差分情報を持たせなかった理由
- `ActorDefeated` / `ActorDeparted` による cleanup パスの存在

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Ai/AdventurerAiPolicy.cs`
- `docs/guidelines/application-boundary-guidelines.md` §2, §5, 設計方針テーブル
- `docs/design/actor-action-phase-system.md` §AI クールダウン硬直の実装方針

完了条件（ユーザー確認後）:

**方針 A（設計分離）:**
- [ ] `AdventurerAiPolicyStateService` が作成され、長期状態とイベント購読を保持している
- [ ] `AdventurerAiPolicy` が `IDisposable` を実装せず、ステートレスになっている
- [ ] `AdventurerAiPolicy` が Service を DI 注入で受け取っている

**方針 B（現状維持 + 記録）:**
- [ ] task ログに「AdventurerAiPolicy が長期状態を持つ設計意図」が記録されている
- [ ] ガイドラインの例外扱いであることが明示されている

---

### 4. `TickAll` で毎フレーム List を new している

重大度: 中

問題:

`ActorActionPhaseStateStore.TickAll` は `WorldSimulationOrchestrator` から毎フレーム呼ばれる Frame Loop 処理である。内部で毎呼び出し2つのリストを `new` しており、フェーズアクティブ Actor が 0 体でもアロケーションが発生する:
```csharp
// L79-80: 毎フレーム new
var transitions = new List<ActorActionPhaseTransition>();
var completedActorIds = new List<Guid>();
```

また `RecordEnteredPhase` 内でもフェーズ遷移ごとに `new List<ActorActionPhaseDef>()` を生成している（L160）。

`docs/guidelines/application-boundary-guidelines.md` §16「Frame Loop / Entity Loop に allocation を追加しない」に反する。

原因:

`TickAll` の戻り値型 `ActorActionPhaseTickResult` が `IReadOnlyList<T>` を保持するため、呼び出しごとにリスト生成が必要な構造になっている。

解決案:

クラスフィールドとして再利用バッファを持つ:
```csharp
readonly List<ActorActionPhaseTransition> transitionsBuffer = new();
readonly List<Guid> completedActorIdsBuffer = new();

public ActorActionPhaseTickResult TickAll(float currentTime)
{
    transitionsBuffer.Clear();
    completedActorIdsBuffer.Clear();
    // ...
    return new ActorActionPhaseTickResult(transitionsBuffer, completedActorIdsBuffer);
    // 有効期間: 次の TickAll 呼び出しまで（同フレーム内のみ）
}
```

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Phase/ActorActionPhaseStateStore.cs` (L79-80, L160)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs` (L189)
- `docs/guidelines/application-boundary-guidelines.md` §16

完了条件:

- [ ] `TickAll` 内でフレームごとの `new List<T>()` が発生しない
- [ ] 再利用バッファが `Clear()` → 利用 → 次フレームの `Clear()` サイクルで動作している
- [ ] `ActorActionPhaseTickResult` の有効期間（同フレーム内のみ）がコメントで明示されている

---

### 5. フェーズ秒数がマジックナンバー

重大度: 中

問題:

`HardcodedActorActionPhaseMasterRepository` にハードコードされた Attack フェーズ秒数が名前付き定数になっていない:
```csharp
new ActorActionPhaseDef(ActorActionPhaseName.WindUp, 0.2f),   // マジックナンバー
new ActorActionPhaseDef(ActorActionPhaseName.Effect, 0f),     // マジックナンバー
new ActorActionPhaseDef(ActorActionPhaseName.Recovery, 0.8f)  // マジックナンバー
```

これらはゲームバランスに直結する調整可能な値であり、`docs/guidelines/implementation-quality-guidelines.md` §4 の対象となる。

原因:

`GameConstants` またはフェーズ専用の定数クラスへの切り出しが行われていない。

解決案:

```csharp
// GameConstants.Combat.cs 内または ActorActionPhaseConstants 専用ファイル
public static class ActorActionPhaseSeconds
{
    public const float AttackWindUp = 0.2f;
    public const float AttackEffect = 0f;
    public const float AttackRecovery = 0.8f;
}
```

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Phase/HardcodedActorActionPhaseMasterRepository.cs` (L18-20)
- `docs/guidelines/implementation-quality-guidelines.md` §4

完了条件:

- [ ] フェーズ秒数が名前付き定数として定義されている
- [ ] `HardcodedActorActionPhaseMasterRepository` がその定数を参照している

---

### 6. フェーズイベント遅延Flush の設計意図がコメントで未記録

重大度: 低

問題:

`WorldSimulationOrchestrator.AdvanceFrameAsync` では `CombatAttackOccurred` は即時 Flush されるが、`ActorActionPhaseStartedEvent` は `phaseEventPublisher?.Flush()` まで遅延発行される。この差異の設計意図がコードに記録されていない。

原因:

意図的な遅延設計と思われるが、理由の記録がない。

解決案:

フェーズイベントのバッファ登録箇所付近にコメントを追加する:
```csharp
// [EventOrder] フェーズ移行イベントは全フレーム処理完了後にまとめて Flush する。
// View アニメーション更新を同フレームの全 Domain 処理後に行うため。
```

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`
- `docs/guidelines/application-boundary-guidelines.md` §15, §20

完了条件:

- [ ] フェーズイベントの遅延 Flush の設計意図がコメントで明示されている

---

### 7. `ActorActionPhaseName/Def/Key` の名前空間と設計ドキュメント分類の齟齬

重大度: 低

問題:

設計ドキュメント（`actor-action-phase-system.md`）では `ActorActionPhaseName`、`ActorActionPhaseDef`、`ActorActionPhaseKey` が「Master 層」として分類されているが、実装は `Application.Actors.Phase` 名前空間に置かれている。

依存方向（Application が Domain を参照）には問題ない。コード変更は不要だが、Magic アクション実装または MasterMemory 導入時に整理が必要になる。

原因:

実装時に `HardcodedActorActionPhaseMasterRepository` と同一フォルダに配置した。

解決案:

コード変更は不要。Magic アクション実装タスクに「Phase 型の名前空間整理（Master 層 vs Application 層）」を追加する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Phase/ActorActionPhaseName.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Phase/ActorActionPhaseDef.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Phase/ActorActionPhaseKey.cs`
- `docs/design/actor-action-phase-system.md` 型定義セクション（Master 層分類）

完了条件:

- [ ] Magic アクション実装タスクに「Phase 型名前空間整理」が追記されている（またはリファクタリングタスクとして別途作成されている）

---

### 8. `ConsumeRealtimeMovedSeconds` の GC Alloc

重大度: 低（別タスク化）

問題:

`WorldSimulationOrchestrator.ConsumeRealtimeMovedSeconds` が毎回 `new List<int>(realtimeMovedSecondsByLayer.Keys)` を生成している。これは本タスクのスコープ外（既存コード）であり、別タスクとして追跡する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`

完了条件:

- [ ] 別タスクとして追跡されている

---

## 統合判断ログ

### 重複排除

- App Boundary 指摘3（`TickAll` GC Alloc）と ImplQuality 指摘4 → 同一問題、本レビュー指摘4に統合
- ImplQuality 指摘1（引数なしコンストラクタ）・指摘6（状態保持設計記録）・App Boundary 指摘1（IDisposable）→ 同一根本原因、本レビュー指摘2・3に再分類

### guideline 間優先度調整

- `AdventurerAiPolicy.IDisposable` は application-boundary-guidelines のハードゲート（UseCase 限定）の直接適用対象ではないが、設計方針テーブルの「Policy / UseCase = ステートレス」に反する。ハードゲートレベルではなく「高」として分類し、ユーザー確認後に対応する

### 今回スコープ外として記録のみ

- `ConsumeRealtimeMovedSeconds` の GC Alloc（指摘8）: 既存コードのため今回スコープ外。別タスク化
- `HardcodedActorActionPhaseMasterRepository` の将来 Addressables 移行（Lighthouse 統合確認）: 現状の動作に問題なし。ScriptableObject/Addressables 化は将来タスクで判断

---

## 修正依頼（Codex 向け）

以下の指摘1・2についてCodexに修正を依頼する。指摘3についてはユーザーに設計方針を確認してから依頼する。

**即時対応（ハードゲート確定）:**
1. `ActorActionPhaseRuntimeState.cs` L13 の `>=` → `Phases.Count <=` に修正
2. `AdventurerAiPolicy` の引数なしコンストラクタ削除 + テスト側スタブ対応
3. `AdvanceActorAiOrchestrator` の2つ目コンストラクタを `internal` に変更 + `InternalsVisibleTo` 追加

**設計方針 B（コード変更なし）:**
4. `AdventurerAiPolicy` の設計意図を task_0001 ログに記録済み（Policy B 確定 — ハードゲートは UseCase 限定のため対応不要）

**次マイルストーン以降（推奨）:**
5. `TickAll` の再利用バッファ対応
6. フェーズ秒数の定数化
7. フェーズイベント遅延Flush コメント追加

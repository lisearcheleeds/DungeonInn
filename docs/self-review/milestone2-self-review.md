# Milestone 2 セルフレビュー

Phase 1〜11 の実装完了後に実施した自己レビュー（パフォーマンス / 整合性 / 設計）。問題がなくなるまで繰り返す。

---

## Round 1

## パフォーマンスレビュー

### [P1] `GameWorldState.RegisterActor` — O(n) 重複チェック

**問題**  
`actors.Any(x => x.Id.Equals(actor.Id))` をスポーン毎に実行。アクター数が増えると線形になる。

**修正内容**  
`Dictionary<Guid, Actor> actorById` を追加し、`RegisterActor` の重複チェックを `actorById.ContainsKey(actor.Id)` の O(1) に変更。`RemoveActor` も辞書と同期するよう更新。

---

### [P2] `AdvanceCombatUseCase.FindActor` — O(n) を毎フレーム複数回呼び出し

**問題**  
戦闘ループ内で `FindActor` をアクター存在チェックとターゲット取得の 2 回呼んでいた。`IGameWorldState` に O(n) の static 検索を使用。

**修正内容**  
`IGameWorldState` インターフェースと `GameWorldState` 実装に `Actor FindActor(Guid actorId)` メソッドを追加（Dictionary による O(1) 参照）。`AdvanceCombatUseCase` の static `FindActor` ヘルパーを削除し、`worldState.FindActor(id)` に統一。

---

### [P3] `AdventurerGuild` の宿確認が毎フレーム LINQ

**問題**  
`HasActiveInnReservation` と `CountActiveInnReservations` が `List<InnReservation>` を LINQ で走査。回復中アクター × フレーム数で実行されるためホットパス。

**修正内容**  
`AdventurerGuild` に `Dictionary<Guid, InnReservation> activeReservationByAdventurer` を追加。`HasActiveInnReservation` を Dictionary の O(1) ルックアップに変更。`CountActiveInnReservations` も active のみを対象とした辞書走査に変更（対象数が最大アクター数に限定されるため実質 O(k)）。  
加えて `EnsureReservationsAsync` を `RecoverAdventurerAtInnUseCase` から分離し、`WorldGameLoopEntryPoint` のスケジュールティックブロック（毎フレームではなく毎ティック）で実行するよう変更。

---

### [P4] `AdvanceCombatUseCase` の「戦闘終了監視者」ループ

**問題**  
ターゲット死亡時に「そのターゲットを狙っていた全員」を探すために全アクターを O(n) イテレート。

**修正内容**  
`IActorCombatService` に `IReadOnlyCollection<Guid> GetAttackers(Guid targetId)` を追加。`ActorCombatService` では既存の `targetedBy` HashSet を返すだけで O(1)。`AdvanceCombatUseCase` の witness ループを `actorCombatService.GetAttackers(target.Id)` の使用に置き換え（P4 + D4 を同時対応）。

---

## 整合性レビュー

### [C1] スポーン間隔がハードコード

**問題**  
`SpawnScheduledMonsterUseCase` 内に `10` がマジックナンバー。冒険者側は `GameConstants` を参照しているのに非統一。

**修正内容**  
`GameConstants.GameLoop.cs` に `MonsterSpawnIntervalTicks = 10` を追加。`SpawnScheduledMonsterUseCase` で `GameConstants.MonsterSpawnIntervalTicks` を参照するよう変更。

---

### [C2] リスト操作の方針が不統一

**問題**  
- `AdvanceActorSimpleLifecycleUseCase`: `new List<Actor>(worldState.Actors)` でコピーしてからイテレート  
- `DetectCombatEncounterUseCase`: `worldState.Actors` を直接イテレート  

それぞれの方針が適切かどうかを確認する必要があった。

**修正内容**  
- `AdvanceCombatUseCase`: ループ中に `worldState.RemoveActor` を呼ぶためスナップショットコピーが必須 → そのまま維持  
- `DetectCombatEncounterUseCase`: ループ中にアクター削除は行わないため直接参照が正しい → そのまま維持  
- `AdvanceActorSimpleLifecycleUseCase`: ループ中にアクター削除を行わない（ライフサイクル状態変更のみ）にもかかわらず不要なコピーを行っていた → コピーを削除し `worldState.Actors` を直接イテレートに変更  

---

### [C3] 完全修飾型名が混在

**問題**  
`RecoverAdventurerAtInnUseCase` のプライベートメソッド引数に `DungeonInn.Domain.Guild.AdventurerGuild` と完全修飾名が使われていた。他の UseCase は `using` で解決済み。

**修正内容**  
`using DungeonInn.Domain.Guild;` を追加し、完全修飾名を `AdventurerGuild` に統一。

---

### [C4] 移動速度定数が重複定義

**問題**  
`AdvanceCombatUseCase` に `const float CombatApproachSpeedMetersPerSecond = 5.0f`、`AdvanceActorSimpleLifecycleUseCase` に `5.0f` リテラルが 3 箇所（GoingToDungeon, Exploring, Returning）と分散していた。

**修正内容**  
`GameConstants.GameLoop.cs` に `ActorMoveSpeedMetersPerSecond = 5.0f` を追加。両 UseCase の全リテラルを `GameConstants.ActorMoveSpeedMetersPerSecond` に統一。

---

## 設計レビュー

### [D1] `ActorCombatState.SetTarget` / `ClearTarget` がパブリックのまま

**問題**  
`ActorCombatService` の `targetedBy` 逆引きインデックスは `IActorCombatService.SetTarget` 経由でのみ整合性が保たれる。しかし `ActorCombatState` のミューテーションメソッドが `public` のためサービスを迂回した直接操作でインデックスが破壊されるリスクがあった。

**修正内容**  
`ActorCombatState.SetTarget` と `ClearTarget` を `internal` に変更。同一アセンブリの `ActorCombatService` からは引き続きアクセス可能だが、外部からの直接変更を禁止。

---

### [D2] 回復中に死亡したアクターの `accumulatedHp` がリーク

**問題**  
`RecoverAdventurerAtInnUseCase.accumulatedHp` は「HP満タン」時にしかクリアされない。戦闘で死亡した場合エントリが永続的に残る。

**修正内容**  
`RecoverAdventurerAtInnUseCase` に `IDisposable` を実装。コンストラクタで `eventBus.OnEvent<ActorDefeated>().Subscribe(e => accumulatedHp.Remove(e.ActorId))` にてデスイベントを購読し、死亡時に対応するエントリを自動削除。スコープ破棄時に `Dispose` でサブスクリプションを解放。

---

### [D3] `DetectCombatEncounterUseCase` の `actor.Hp <= 0` ガードが冗長

**問題**  
`AdvanceCombatUseCase` が死亡アクターを `RemoveActor` で除去するようになったため、`Actors` リストに `Hp <= 0` のアクターが残ることはない。処理順は `detect → advance` のため、前フレームで死亡したアクターはすでに除去済み。

**修正内容**  
`DetectCombatEncounterUseCase` の `actor.Hp <= 0` ガードブロック全体を削除。

---

### [D4] 戦闘終了イベントの責務が `AdvanceCombatUseCase` に集中

**問題**  
「誰かが死んだ → 周囲の戦闘も終わらせる」伝播ロジックが `AdvanceCombatUseCase` にあり、全アクターのイテレートと責務の混在が生じていた。

**修正内容**  
P4 の対応として `actorCombatService.GetAttackers(target.Id)` を利用することで、witness ループを `targetedBy` 逆引きインデックスから直接取得する形に変更。戦闘終了の伝播に必要な情報（攻撃者リスト）をサービス層が管理するようになり、UseCase が全アクターを直接スキャンする責務を持たなくなった。

---

## Round 2

### [C5] `AdventurerGuild.cs` に未使用の using

**問題**  
`using DungeonInn.Domain.Common;` が残っていたが、`AdventurerGuild` 内に `GameConstants` などの参照がなく未使用。

**修正内容**  
該当 `using` 行を削除。

---

### [C6] `RecoverAdventurerAtInnUseCase.cs` の `using R3;` が誤順序

**問題**  
`using R3;` が `DungeonInn.*` 名前空間の途中に挿入されており、コーディング規約（System → 外部ライブラリ → プロジェクト内）に反していた。

**修正内容**  
`using R3;` を `using Cysharp.Threading.Tasks;` の直後（外部ライブラリグループ内）に移動。

---

### [D5] `AdvanceActorSimpleLifecycleUseCase.exploringDestinations` が死亡時にリーク

**問題**  
アクターがダンジョン内で死亡した場合、`exploringDestinations` の対応エントリが永続的に残り続ける（`accumulatedHp` と同種の問題）。`AdvanceCombatUseCase` が死亡アクターを `RemoveActor` で除去するため、そのアクターのエントリは再参照されないが辞書には残り続ける。

**修正内容**  
`AdvanceActorSimpleLifecycleUseCase` に `IDisposable` を実装。コンストラクタ（既存の `IGameEventBus` 注入を利用）で `ActorDefeated` を購読し、死亡時に `exploringDestinations.Remove(e.ActorId)` を実行。スコープ破棄時に `Dispose` でサブスクリプションを解放。

---

### [D6] `ReleaseInnReservation` に `occurredAtTick = 0` をハードコード

**問題**  
`RecoverAdventurerAtInnUseCase.TickRecovery` 内で `guild.ReleaseInnReservation(actor.Id, 0)` と呼んでおり、予約解放のティック情報が常に `0` になる。P3 対応で `ExecuteAsync` から `currentTick` を除いたことで生じた欠損。`InnReservation.ReleasedAtTick` の履歴データが不正確になる。

**修正内容**  
`RecoverAdventurerAtInnUseCase` に `IGameClock gameClock` を追加で注入。`TickRecovery` 内の `ReleaseInnReservation(actor.Id, 0)` を `ReleaseInnReservation(actor.Id, gameClock.CurrentScheduleTick)` に変更。

---

## Round 3

### [C7] `DetectCombatEncounterUseCase` の単行 if がコーディング規約違反

**問題**  
`if (worldState == null) throw new ArgumentNullException(nameof(worldState));` が1行で書かれており、Rule 2-2（Allman スタイル、必ず波括弧で囲む）に違反。

**修正内容**  
Allman スタイルのブロック形式に修正。

---

### [C8] `DecideAdventurerReturnUseCase` の不要な `List<Actor>` コピー

**問題**  
`var actors = new List<Actor>(worldState.Actors)` でスナップショットコピーを作成しているが、このUseCase はループ中にアクター削除を行わない（ライフサイクル状態変更のみ）。C2 で `AdvanceActorSimpleLifecycleUseCase` に適用した修正と同様の問題が残存していた。

**修正内容**  
`worldState.Actors` を直接参照するよう変更し、不要な配列確保を除去。`using System.Collections.Generic` が不要になるため削除。

---

## Round 4

### 問題なし

Round 1〜3 の修正後、以下の観点で全ファイルを再確認した結果、新たな問題は見つからなかった。

- **パフォーマンス**: 全ホットパスのコレクション操作が O(1) または必要最小限のスキャンに収まっている
- **整合性**: `using` 順序・定数の重複・リスト直接参照の方針が揃っている
- **設計**: 死亡時クリーンアップ（`IDisposable` + R3 `Subscribe`）が `accumulatedHp` / `exploringDestinations` の両方に適用済み。`ActorCombatState` のミューテーションが `internal` に限定されており外部迂回の余地がない
- **テスト互換性**: `AdvanceActorSimpleLifecycleUseCase` のテストは `NoOpGameEventBus`（`Observable.Empty<T>()` を返す）を使っており新たな `deathSubscription` と競合しない。`RecoverAdventurerAtInnUseCase` を直接構築するテストは存在せず `IGameClock` 注入の追加による破壊もない

セルフレビュー完了。

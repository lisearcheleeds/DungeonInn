# Milestone 5 完了レビュー 第5回 Claude

作成日: 2026-05-16

## レビュー範囲

- `docs/` 配下の全 Markdown を確認した
- レビュー基準として `docs/guidelines/` 配下の以下を確認した
  - `lighthouse-patterns.md`
  - `coding-rules.md`
  - `domain-design-guidelines.md`
  - `application-boundary-guidelines.md`
  - `implementation-quality-guidelines.md`
  - `self-review-preset.md`
- 対象実装として `Client/Assets/DungeonInn/Runtime/Scripts` 配下の現行コードを確認した
- 既存レビュー（review-1〜review-4-total-manual、review-5-codex）本文と対応ログを確認し、解消済みと判断できるものは未解決として再掲していない
- review-5-codex で既に指摘済みの項目（TODO milestone 未紐付け、ActorViewDataStore 内部バッファ参照、WorldMapView 毎フレームポーリング、DecideAdventurerReturnUseCase HashSet 生成、using 順序違反）は本文書では再掲しない

---

## 総評

Milestone 5 の主目的である debug Sphere / Plane から chunk mesh / SpriteRenderer 表示への置換、および `WorldGameLoopEntryPoint` から `WorldSimulationOrchestrator` へのゲーム進行順序の移管は完了している。review-4 で指摘された多数の問題（InnEconomyStatus 2-arg 化、IActorBehavior ポリモーフィック化、AStarPathfinder O(log n) 化等）も現行コードで解消が確認できた。

未解決の主なリスクは、`WorldSimulationOrchestrator` のコンストラクタ肥大化（19 パラメータ）、`Launcher.cs` の禁止 API 残存、`PickUpItemUseCase` のガード抜け、フレーム内イベント発行順序の契約不在である。前2者はいずれも複数回の指摘を経ており、再発防止策の明文化が必要。

---

## 現在状態索引

| 項目 | 現在状態 | 根拠 |
|---|---|---|
| InnEconomyStatus 2-arg constructor / alias proxy 削除 | 対応済み | 現行コード確認（2-arg, no alias） |
| InnEconomyStatistics.Demand 正しく利用 | 対応済み | InnEconomyStatusCalculator 確認 |
| ActorDecisionScheduler ActorDefeated/ActorDeparted cleanup | 対応済み | 現行コード確認 |
| IActorBehavior OnRecovered ポリモーフィック hook | 対応済み | 現行コード確認 |
| AStarPathfinder SortedSet + HashSet 化 | 対応済み | 現行コード確認 |
| AttackAreaTargetResolver.ContainsFan HalfAngleCos キャッシュ利用 | 対応済み | 現行コード確認 |
| SpawnScheduledAdventurerOrchestrator LINQ 除去 | 対応済み | 現行コード確認 |
| WorldGameLogPresenter → WorldDebugGameLogPresenter (Debug/ 配置) | 対応済み | 現行コード確認 |
| WorldActorPresenter IDisposable 削除 | 対応済み | 現行コード確認 |
| WorldCameraController Lighthouse InputLayer 利用 | 対応済み | 現行コード確認 |
| RecoverAdventurerAtInnUseCase が他 UseCase を注入しない | 対応済み | 現行コード確認（AdventurerRecoveryStateService / ActorProcessingCandidateService のみ） |
| CombatDamageResolver / CombatEffectExecutor IEventPublisher をメソッドパラメータ化 | 対応済み | 現行コード確認 |
| WorldMapViewDataProvider.GetLayers() ラムダクロージャ除去 | 対応済み | 現行コード確認 |
| DetectCombatEncounterUseCase shouldAdvanceTimeDependentSystems ガード内に配置 | 対応済み | WorldSimulationOrchestrator 現行コード確認 |
| ActorExplorationAchievementRegistry 導入・cleanup あり | 対応済み | 現行コード確認 |
| ActorProfileRegistry 責務コメント明記 | 対応済み | 現行コード確認 |
| WorldSimulationOrchestrator 19 コンストラクタパラメータ | **許容（設計判断）** | ユーザー判断：オーケストレーターはパラメータ数制約を外してよい |
| PickUpItemUseCase shouldAdvanceTimeDependentSystems ガード欠如 | **未対応** | 現行コード確認（line 155-158） |
| game-event-design.md フレーム内発行順序未定義 | **未対応** | docs 確認 |
| Launcher.cs SceneManager 禁止 API | **未対応** | 現行コード確認（line 29, 61） |
| AttackAreaTargetResolver.CalculateBoundingRadius() Math.Sqrt | **未対応** | 現行コード確認（line 74） |
| SpawnAdventurerUseCase.ToItemStacks() LINQ 使用 | **未対応** | 現行コード確認 |
| WorldLifetimeScope 60+ DI 登録 | **Milestone 6 延期** | 設計変更コストが高い |
| Actor View pooling 未実装 | **Milestone 6 延期** | Milestone 5 スコープ外 |

---

## 設計

### 設計判断メモ: WorldSimulationOrchestrator 19 パラメータは許容

review-4 では「依存数が多すぎる」として指摘されたが、ユーザーレビューにより「オーケストレーターはこの制約を外してよい」との判断が示された。

オーケストレーターの責務は「複数の UseCase / Orchestrator を束ねてゲーム進行順序を所有すること」であり、その性質上、依存数が多くなるのは自然。`WorldSimulationOrchestrator` を人為的に分割しても、分割先がドメインの実概念ではなく「パラメータ削減のための中間層」になるだけで、かえって設計が不明瞭になる。この判断を以後のレビューで再指摘しない。

ただし、ゲーム進行の関心が増加し続けた場合に備えて、将来 **自然なドメイン境界が生まれた場合**（例: 戦闘パイプラインが独立した上位概念として確立した場合）は、その時点で改めて分割の是非を検討する。

---

### 2. PickUpItemUseCase が shouldAdvanceTimeDependentSystems ガードなしに実行される

**重大度: 中**

問題:

`WorldSimulationOrchestrator.AdvanceFrameAsync()` 内で `pickUpItemUseCase.Execute(gameWorldState)` は `0 < gameWorldState.Items.Count` のみでガードされており、`shouldAdvanceTimeDependentSystems` によるポーズ中の実行停止ガードが適用されていない。ゲームが一時停止中でもアイテム取得処理が実行される。

`application-boundary-guidelines.md` は「ゲーム時間の進行に依存する処理はポーズ中に実行しない」方針を定める。アイテム取得はアクターの位置とゲーム時間に依存した判定であり、ポーズ中に実行されることは仕様上の矛盾となる。

原因:

同一メソッド内の他の UseCase 呼び出し（AI 評価、戦闘など）はすべて `shouldAdvanceTimeDependentSystems` ガードを通過しているが、`pickUpItemUseCase` だけがアイテム存在チェックのみで分岐しており、ガード漏れが生じている。

解決案:

```csharp
// Before
if (0 < gameWorldState.Items.Count)
{
    pickUpItemUseCase.Execute(gameWorldState);
}

// After
if (shouldAdvanceTimeDependentSystems && 0 < gameWorldState.Items.Count)
{
    pickUpItemUseCase.Execute(gameWorldState);
}
```

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`（line 155-158）
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `WorldSimulationOrchestrator` 内の `pickUpItemUseCase.Execute()` 呼び出しが `shouldAdvanceTimeDependentSystems` 条件でガードされている
- [ ] ポーズ中（`shouldAdvanceTimeDependentSystems == false`）にアイテム取得が実行されないことが確認できる
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## 整合性

### 3. game-event-design.md にフレーム内イベント発行順序の契約が定義されていない

**重大度: 中**

問題:

`docs/design/game-event-design.md` はゲームイベントの種類と設計を記述しているが、1 フレーム内でのイベント発行順序（「ActorDefeated は CombatEncounterEnded より前に発行されるか」等）が明文化されていない。Milestone 5 で `BufferedEventPublisher` が導入され、一部経路で発行タイミングが変更されているにもかかわらず、購読者が依存してよい順序保証がドキュメントに存在しない。

`WorldSimulationOrchestrator.AdvanceFrameAsync()` が進行順序を担うようになった現在、フレーム内の「DetectCombatEncounter → AdvanceCombat → ActorDefeated → AdvanceAreaEffect」のような処理順と、それに対応するイベント発行順を整合性資料として持つことは Milestone 6 の戦闘表示・NavMesh 拡張の安全な実装に不可欠。

原因:

イベント設計書はイベントの型・フィールドを記述することを主目的として作成されており、フレームレベルの「どの処理内で発行されるか」「どの順序で購読者に届くか」という実行モデルが追記されていない。`WorldSimulationOrchestrator` が進行順序の所有者になったタイミングで合わせて更新されなかった。

解決案:

`docs/design/game-event-design.md` に「フレーム内イベント発行順序」のセクションを追加し、以下を記載する。

1. `WorldSimulationOrchestrator.AdvanceFrameAsync()` のフェーズ（スケジュール → AI → 戦闘検出 → 戦闘進行 → プロジェクタイル → エリア効果 → アイテム取得 → Actor エフェクト → 宿屋回復）
2. 各フェーズで発行されるイベント型の一覧
3. `BufferedEventPublisher` を経由する経路と直接 `IEventPublisher` を使う経路の区別
4. 購読者が「同フレーム内の順序」に依存してよいかどうかの保証方針

根拠となるファイルリスト:

- `docs/design/game-event-design.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/self-review/milestone5-completion-review-4-claude.md`

完了条件:

- [ ] `docs/design/game-event-design.md` に「フレーム内イベント発行順序」セクションが存在する
- [ ] `WorldSimulationOrchestrator` の各処理フェーズと、そこで発行されるイベント型が対応表として記載されている
- [ ] BufferedEventPublisher 経由の経路と直接発行の経路が明示されている
- [ ] 購読者が同フレーム内の順序に依存してよいかの方針が明記されている

---

## パフォーマンス

### 4. AttackAreaTargetResolver.CalculateBoundingRadius() が Rectangle で Math.Sqrt を使用

**重大度: 低**

問題:

`AttackAreaTargetResolver.CalculateBoundingRadius()` は Rectangle 形状のバウンディング半径計算に `Math.Sqrt(halfWidth * halfWidth + halfLength * halfLength)` を使用している（line 74）。この値は `ActorSpatialIndexService.CollectNearbyActors()` の近傍セル半径計算に用いられ、攻撃エフェクトごとに毎回呼び出される。

`lighthouse-patterns.md` および `application-boundary-guidelines.md` はフレームループ内での不要な浮動小数点演算削減を推奨する。一方、Fan 形状の `ContainsFan()` は `HalfAngleCos` キャッシュを利用しており、同クラス内で最適化の一貫性が取れていない。

原因:

Circle / Fan は半径をそのまま返せるため最適化が容易だったが、Rectangle の斜辺計算は `Math.Sqrt` が必要なため後回しになった。`AttackAreaSpec` が不変データ（マスタ由来）であるため、計算結果をキャッシュする機会があったが実装されていない。

解決案:

`AttackAreaSpec` 生成時（または初回利用時）に `BoundingRadius` を計算してキャッシュプロパティとして持たせる。`AttackAreaSpec` が不変であれば、コンストラクタ内で事前計算するだけで毎フレームの `Math.Sqrt` を排除できる。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AttackAreaTargetResolver.cs`（line 64-78）
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/AttackAreaSpec.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `AttackAreaTargetResolver.CalculateBoundingRadius()` 内に Rectangle 分岐での `Math.Sqrt` 呼び出しが残っていない
- [ ] `AttackAreaSpec` または `AreaEffectInstance` にバウンディング半径のキャッシュプロパティが存在する
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 5. SpawnAdventurerUseCase.ToItemStacks() で LINQ GroupBy が使用されている

**重大度: 低**

問題:

`SpawnAdventurerUseCase.ToItemStacks()` が `System.Linq` の `GroupBy` / `Select` / `ToArray` を使用してアイテム ID リストをスタック化している。スポーン処理自体はスケジュール tick 駆動で毎フレームではないが、アクター追加時に呼ばれるためゲームループ中の GC Alloc 源になりうる。`SpawnScheduledAdventurerOrchestrator` では同類の LINQ が除去されており、同じスポーン経路の中で一貫性が取れていない。

原因:

`SpawnScheduledAdventurerOrchestrator` の LINQ 除去が `review-4` で対応されたが、`SpawnAdventurerUseCase` 側の `ToItemStacks()` は同回のレビュー対象に含まれず残存した。

解決案:

`ToItemStacks()` を手動の `Dictionary<int, int>` 集約ループに置き換える。`id → quantity` の辞書から `ItemStack` リストを生成し、`GroupBy` を排除する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnAdventurerUseCase.cs`（line 119-125）
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/self-review/milestone5-completion-review-4-total-manual.md`

完了条件:

- [ ] `SpawnAdventurerUseCase.ToItemStacks()` 内に `using System.Linq` 由来の LINQ 呼び出しが残っていない
- [ ] 同メソッドが手動ループで実装されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## 総合

### 6. Launcher.cs が SceneManager.LoadSceneAsync / UnloadSceneAsync を直接呼び出している

**重大度: 中**  
**再発項目（review-2-total 別タスク、review-3-codex 対応済み判定、review-4-claude 再指摘に続き 4 度目）**

問題:

`Launcher.cs` の line 29 と line 61 でそれぞれ `UnityEngine.SceneManagement.SceneManager.LoadSceneAsync()` / `UnloadSceneAsync()` が直接呼ばれている。これらは `lighthouse-patterns.md` の禁止 API（SceneManager 直接呼び出し）に該当する。

`ILauncher.Reboot()` 経路のブートストラップで Launcher シーンを直接ロード・アンロードするため、現状では `ISceneManager` で代替できないという技術的制約がある。しかしその制約が **コード内に** 明示されておらず、将来の修正担当者が禁止 API 使用に気づいても「例外として許可された」経緯が追跡できない。

原因:

review-3 で「Launcher 例外を docs 明文化」として対応済みとされたが、docs への記載先が `lighthouse-patterns.md` の例外欄であったか不明確であり、かつコード側に `// TODO(milestone:X): ISceneManager に移行する` コメントが追加されなかった。完了条件に「コード内への TODO 追記」が含まれていなかったため、docs 更新だけで対応済みと判定された。

解決案:

短期対応（コメント追加）:

```csharp
// TODO(milestone:X): ISceneManager が Reboot 用 LoadScene Single をサポートする際に移行する
await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(LauncherSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
```

根治対応: `ISceneManager` または `ILauncherSceneLoader` インターフェースに `ReloadAsBootstrap()` メソッドを追加し、Launcher がそれを通じて Lighthouse LifetimeScope 管理下でシーン遷移を行う。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Core/Launcher.cs`（line 29, 61）
- `docs/guidelines/lighthouse-patterns.md`
- `docs/self-review/milestone5-completion-review-2-total.md`（「別タスクとして追跡」）
- `docs/self-review/milestone5-completion-review-3-codex.md`（「対応済み: docs 明文化」）
- `docs/self-review/milestone5-completion-review-4-claude.md`

完了条件:

- [ ] `Launcher.cs` の禁止 API 呼び出し箇所に `// TODO(milestone:X): ISceneManager に移行する` コメントが追加されているか、または `ISceneManager` 経由に置き換えられている
- [ ] 例外として継続する場合、`docs/guidelines/lighthouse-patterns.md` の禁止 API 例外欄に「Launcher.cs bootstrap ロード：Lighthouse LifetimeScope 外のため暫定直接呼び出し」が明記されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

再発理由:

review-3 の完了条件に「コード内 TODO コメント追記」が含まれていなかった。docs への例外明記だけを「対応」として扱ったため、コード上は禁止 API のままで次のレビュアーに伝わらなかった。

再発防止策:

1. 禁止 API 例外を認める場合の完了条件に「コード側に `// TODO(milestone:X)` コメントを追加する」を必須項目として含める
2. `docs/guidelines/lighthouse-patterns.md` の禁止 API セクションに「例外を認める場合はコード内 TODO コメント必須」と明記する
3. `rg "SceneManager\.LoadSceneAsync\|SceneManager\.UnloadSceneAsync" Client/Assets/DungeonInn/Runtime/Scripts` を定期実行し、TODO コメントなし呼び出しの追加を検出する

---

## Milestone 6 延期項目

以下は Milestone 5 のスコープ外と判断済みの項目を記録する。

### Actor View のオブジェクトプーリング未実装

Milestone 5 の `WorldActorViewRegistry` は Actor 追加のたびに GameObject を生成しており、プーリングは行われていない。NavMesh / 戦闘表示 / Actor 数増加を見込む Milestone 6 前に対応すべきだが、Milestone 5 の主目的である SpriteRenderer 表示移行は完了しているため延期。

延期理由: Actor View の生成・破棄パターンが Milestone 6 の表示拡張とともに確定する見込みのため、仕様確定後に実装する。

### WorldLifetimeScope DI 登録の整理

`WorldLifetimeScope` の DI 登録数が 60+ に達している。現時点でバグは発生していないが、登録が単一ファイルに集中しており、Milestone 6 で登録が増えるにつれて責務境界の曖昧さが悪化するリスクがある。

延期理由: Milestone 6 でシーン追加・SubScope 整理を行うタイミングで合わせて分割する。

---

## 最終チェック

- [x] 各レビュー項目に「問題」がある
- [x] 各レビュー項目に「原因」がある
- [x] 各レビュー項目に「解決案」がある
- [x] 各レビュー項目に「根拠となるファイルリスト」がある
- [x] 各レビュー項目に「完了条件」がある
- [x] 既存レビューの対応ログを確認した
- [x] 解消済み項目を未解決として再掲していない
- [x] 未解決項目を対応済みとして扱っていない
- [x] 同じ指摘が 3 回以上出ている場合（Launcher.cs 禁止 API）、再発理由と再発防止策を書いた
- [x] WorldSimulationOrchestrator 19 param はユーザー判断により「許容」として記録し、再発項目扱いから除外した
- [x] review-5-codex と重複する項目を再掲していない
- [x] レビュー結果を `docs/self-review/` 配下に保存した

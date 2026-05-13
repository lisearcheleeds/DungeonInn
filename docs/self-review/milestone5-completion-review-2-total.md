# Milestone 5 完了前レビュー 第2回 統合版

作成日: 2026-05-13

## 概要

`milestone5-completion-review-2-claude.md`（Claude Code 実施）と `milestone5-completion-review-2-codex.md`（Codex 実施）を統合・整理したドキュメント。

- 両者が同じ問題を指摘した場合は1項目に統合し、情報を補完した
- 第1回レビュー（`milestone5-completion-review.md`）で「対応済み」とされた項目は除外した
- 第1回で「Milestone 6へ延期」とされた項目のうち、第2回でも重要と判断されたものは改めて記載し、延期理由を引き継いだ
- 指摘元は各項目末尾に **[Claude]** / **[Codex]** / **[両者]** で示す

---

## 設計レビュー

### 1. Inventory が集約境界の外から直接変更されている

**重大度**: 高　**[両者]**

**問題**:

`Actor.Inventory` が可変オブジェクトとして公開されており、UseCase が `actor.Inventory.Add()` / `actor.Inventory.Remove()` / `actor.Inventory.TrySpendGold()` を直接呼んでいる。Actor 集約を経由せずに inventory 状態を変更できるため、将来的にキャッシュ更新・イベント発行・装備との整合性維持が漏れやすい。Equipment 側は読み取り専用 interface 化が進んでいる一方、Inventory 側は同じ整理が未完了。

**原因**:

Inventory 操作が多いため、Actor を経由するメソッドを用意せず直接アクセスを許した設計になった。

**解決案**:

1. `public IReadOnlyInventory Inventory { get; }` に変更し、読み取りのみを公開する
2. 変更は `actor.GainItem(ItemStack)` / `actor.TrySpendGold(int)` / `actor.RemoveItem(Guid)` など Actor 経由のメソッドに集約する
3. `IExchangeParticipant` も `Inventory` を返すのではなく取引操作を表すメソッドを持たせる
4. Inventory に連動するキャッシュを Actor 内で一元管理する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs` (line 21)
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Item/Inventory.cs` (line 72)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/PickUpItemUseCase.cs` (line 66)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/UpdateEquipmentUseCase.cs` (line 147)
- `docs/guidelines/usecase-boundary-guidelines.md` §4

---

### 2. Domain / Master 層で static Catalog への直接参照が残っている

**重大度**: 高　**[Claude]**

**問題**:

`WeaponCombatCalculatorFactory`（`Domain/Combat` 配下）が `WeaponTypeCombatMasterCatalog.Get(weaponType)` を静的に呼び出している。`WeaponMaster`（`Master` 名前空間）のコンストラクタも同様に `WeaponTypeCombatMasterCatalog.Get()` を直接呼ぶ。これにより、依存をモック不可能でテスト容易性が損なわれ、DI の恩恵が受けられない。

> 表現補足: `WeaponMaster` は `Master` 名前空間のクラスのため「Domain 層」と断定するのは不正確。正確には「Domain/Master 周辺で static Catalog 直接参照が残っている」。問題の核は `Domain/Combat` 配下の `WeaponCombatCalculatorFactory` が static Catalog に依存している点。

**原因**:

マスタデータ参照を DI 経由に統一する前に、static Catalog への直接アクセスが各所に残った。

**解決案**:

1. `WeaponCombatCalculatorFactory` の `WeaponTypeCombatMasterCatalog.Get()` 呼び出しを、コンストラクタ引数または `IWeaponTypeCombatMasterRepository` への DI 経由に変更する
2. `WeaponMaster` のコンストラクタも同様に、外部から `WeaponTypeCombatMaster` を注入するよう変更する
3. Catalog 参照は Factory 層に集約し、Domain Entity 生成時はすべてマスタを注入する形へ移行する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/WeaponCombatCalculatorFactory.cs` (line 11)
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/WeaponMaster.cs` (line 24-28)
- `docs/guidelines/domain-usecase-design-guidelines.md` §6

---

### 3. View が IGameWorldStateReader を通じて Domain ビジネス集約全体を読める

**重大度**: 中　**[Codex]**

**問題**:

`IGameWorldStateReader` は `AdventurerGuild Guild` / `GroundMap GroundMap` / `Dungeon Dungeon` / `InnEconomyState InnEconomy` / `IReadOnlyList<Actor> Actors` など Domain のビジネス集約全体を公開している。`WorldMapView` / `WorldActorPresenter` がこの interface を直接受け取るため、表示層から Domain 全体を参照できる。

> コード確認補足: `ActorSpriteVisualConfig.cs` での `actor.Behavior is AdventurerBehavior` のような Behavior 型チェックは View 内に閉じており、Domain Entity の直接変更は行っていないため、これ自体は許容できる使い方。問題は `Guild` / `Dungeon` などのビジネス集約全体が View から無制限に参照できる点。

**原因**:

Read / Write 分離は行われているが、「Application 内部の読み取り」と「View 表示用の読み取り」が同じ interface に残っている。

**解決案**:

Application 側に `IWorldMapViewDataProvider` / `IActorViewDataProvider`、または Query UseCase を追加し、View には座標・表示ロール・表示状態・必要な map cell 情報だけを渡す。Guild や Dungeon の集約全体を View 側から直接参照しない構造にする。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/IGameWorldState.cs` (line 12)
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs` (line 12)
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs` (line 10)

---

### 4. 状態保持 Service が Application/UseCase フォルダに混在している

**重大度**: 中　**[Codex]**

**問題**:

`AdventurerRecoveryStateService` / `AdventurerExplorationStateService` / `AdventurerReturnTrackingService` のように `IDisposable` とイベント購読、長期状態を持つ Service が `Application/UseCase` 名前空間に残っている。UseCase はステートレスなトランザクション単位というガイドラインと配置が一致していない。

**原因**:

Milestone 4/5 の設計整理で責務は Service 化されたが、フォルダ / namespace が追従していない。

**解決案**:

`Application/Service` または `Application/State` へ移動し、namespace も分ける。UseCase フォルダには一回の実行で Domain を変更し Event を発行するクラスだけを置く。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerRecoveryStateService.cs` (line 10)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerExplorationStateService.cs` (line 11)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerReturnTrackingService.cs` (line 11)

---

### 5. View 層がゲーム進行パイプラインを握っている（延期項目の補足）

**重大度**: 高　**[両者]**　**→ Milestone 6 対応**

> 第1回レビュー「設計§1」で Milestone 6 延期と判断済み。両者が同様に指摘したため、対応内容を補足して再掲する。

**問題**:

`WorldGameLoopEntryPoint` が多数の UseCase / Orchestrator を直接注入し、spawn・lifecycle・combat・projectile・area effect・item・inn recovery・report 発行までの順序を View 層で固定している。

**解決案補足**:

Application 層に `AdvanceWorldFrameUseCase` または `WorldSimulationOrchestrator` を追加し、ゲーム進行順序をそこへ移す。`WorldGameLoopEntryPoint` は初期化・delta time 受け渡し・View 更新呼び出し・キャンセル管理に限定する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs` (line 16, 43, 142)

---

---

## 整合性レビュー

### 1. AI 実行基盤がゲームループに接続されていない

**重大度**: 高　**[Codex]**

**問題**:

`docs/design/actor-ai-desing.md` では `AdvanceActorAiOrchestrator` が AI 評価・dirty 管理・Decision 適用を担う設計になっている。実装でも `WorldLifetimeScope` に DI 登録されているが、`WorldGameLoopEntryPoint` の `TickAsync()` の実行順に `AdvanceActorAiOrchestrator` が含まれていない。DI 登録済みで実行されていない状態になっている。

**原因**:

Milestone 5 の表示置き換え中に旧来の lifecycle / UseCase 直接呼び出しがゲームループに残り、AI Orchestrator は登録済みのまま未使用になっている。

**解決案**:

ゲーム進行パイプラインで `AdvanceActorAiOrchestrator.ExecuteAsync(...)` を呼ぶ。現行の直接 lifecycle 制御を正とするなら、設計 docs に「現在は未接続、Milestone 6 で接続予定」と状態を明記する。

**根拠**:

- `docs/design/actor-ai-desing.md` (line 25, 202)
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs` (line 124)
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs` (line 43, 142)

---

### 2. モンスタースポーンが SpawnTable の重みを無視している

**重大度**: 高　**[Codex]**

**問題**:

設計では `SpawnTableMaster` がスポーン対象と重みを持ち、重み付き抽選を行う想定になっている。実装では `SpawnScheduledMonsterOrchestrator` が `spawnTable.Entries[0]` 固定でスポーン対象を選んでおり、`SpawnTableEntryMaster.Weight` が使われていない。Floor 1 の Goblin / Goblin Archer 70 / 30 のような重み設定が実際の出現に反映されない。

**原因**:

TODO の暫定実装が残っており、重み付き抽選が未実装のまま。

**解決案**:

冒険者側の spawn entry 抽選と同様に、`Weight` に基づく重み付き抽選へ統一する。`SpawnTableTargetType.ActorArchetype` の検証も合わせて追加する。

**根拠**:

- `docs/design/guild-domain-design.md` (line 420)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/SpawnScheduledMonsterOrchestrator.cs` (line 61)
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs` (line 336)

---

### 3. 宿泊費不足時の状態遷移が仕様と異なる

**重大度**: 高　**[Codex]**

**問題**:

Milestone 3 仕様では「満室または残金不足の場合 `WaitingForInn` に入る」とされている。実装では満室時は `WaitingForInn` になるが、宿泊費を払えない場合は `Preparing` に戻る。仕様と実装が不一致。

**原因**:

空室待ちと支払い不能が別扱いになっており、支払い不能時だけ待機 / 再試行ループから外れている。

**解決案**:

どちらを正とするかユーザーに確認する。

- 仕様通りにするなら: 支払い不能時も `WaitingForInn` 相当へ遷移させる
- 現行挙動を正とするなら: 仕様を「残金不足時は再準備へ戻る」に更新する

**根拠**:

- `docs/roadmap/milestone3-roadmap.md` (line 333, 344)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/RecoverAdventurerAtInnUseCase.cs` (line 135, 137)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/ChargeInnFeeService.cs` (line 61)

---

### 4. 武器計算インターフェースが 2 系統に分裂している

**重大度**: 中　**[Claude]**（重大度を高→中に修正：バグではなく設計判断の再確認が必要な論点）

**問題**:

`IWeaponCalculator`（攻撃力計算）と `IWeaponCombatCalculator`（射程・攻撃速度・攻撃定義）という 2 つのインターフェースが存在し、それぞれに `WeaponCalculatorFactory` と `WeaponCombatCalculatorFactory` がある。`Actor` クラスは両方を別プロパティで保持している。設計ドキュメントには「責務が異なるため分離」と明記されているが、呼び出し側では両方を常に参照するため、分離の意義が薄れている。

**原因**:

武器攻撃システム整理の過程で 2 つのファクトリと計算器が異なるニーズから導入されたが、統合の判断がされなかった。

**解決案**:

1. ドキュメントで「攻撃力 vs 戦闘性能」の分離が本当に必要かを再検討する
2. 統合が適切であれば単一の `IWeaponAttackCalculator` へ統合し、`WeaponAttackInfo`（攻撃力・射程・速度・攻撃定義を含む）を返す
3. 分離を維持するなら、それぞれの責務と使い分け基準をドキュメントに明記する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/IWeaponCalculator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/IWeaponCombatCalculator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/WeaponCalculators.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/WeaponCombatCalculatorFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs` (line 32-33)
- `docs/design/combat-domain-design.md` (line 61-62)

---

### 5. アイテム / ドロップ設計資料と実装マスタが不一致

**重大度**: 中　**[Codex]**

**問題**:

`docs/design/spec_item_money.md` の現在アイテムマスタには `Iron Sword (3004)` が見当たらない。また Goblin のドロップは Goblin Ear と Gold のみと記載されている。実装の `HardcodedMasterRepository` には `Iron Sword` があり、Goblin drop にも含まれている。

**原因**:

マスタ拡張後に設計資料の「現在のマスタ」表が更新されていないか、実装側のドロップ追加が暫定値のまま残っている。

**解決案**:

`Iron Sword` とドロップ率を正式仕様にするなら docs を更新する。意図しない値なら `SpeciesMaster` の Goblin drop から `3004` を削除または確率調整する。

**根拠**:

- `docs/design/spec_item_money.md` (line 100, 164)
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs` (line 122, 214)

---

### 6. AdventurerBattleRecord のイベント購読が設計より不足している

**重大度**: 中　**[Codex]**

**問題**:

イベント設計では `AdventurerBattleRecord` が `CombatEncounterStarted` / `CombatEncounterEnded` で 1 戦闘サマリーを作り、`ActorDefeated` と `ActorExitedDungeon` も扱う想定になっている。実装の `AdventurerBattleRecordService` は `CombatEncounterStarted` と `CombatAttackOccurred` だけを購読しており、終了・死亡・退出による確定処理が未接続。

**原因**:

戦闘ログの最小集計が先に実装され、「1戦闘ごとのサマリー」「退出時の保管ストア移行」が未接続のまま。

**解決案**:

設計通りにするなら `CombatEncounterEnded` / `ActorDefeated` / `ActorExitedDungeon` を購読してサマリー確定を実装する。現状の累積統計だけを正とするなら、docs の `AdventurerBattleRecord` 仕様を現在の責務へ縮小する。

**根拠**:

- `docs/design/game-event-design.md` (line 112)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AdventurerBattleRecordService.cs` (line 21)

---

### 7. IActorBehavior が空インターフェースで型チェックに依存している

**重大度**: 中　**[Claude]**

**問題**:

`IActorBehavior` インターフェースがメンバを一切持たないマーカーインターフェースになっており、Actor や UseCase は `behavior is AdventurerBehavior` のような型チェックで役割を判定している。型スイッチが各所に散在する原因になる。

**原因**:

各 Behavior 実装クラスの型そのものが役割を定義する意図で設計されたが、意図がドキュメント化されていない。

**解決案**:

型チェックのみが意図なら、その設計判断をドキュメント化する。将来の拡張を見越すなら `ActorBehaviorKind` プロパティなど識別用のメンバを追加し、型チェックを排除する。大規模リファクタリングが必要な場合は Milestone 6 以降のタスクとして扱う。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/IActorBehavior.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/UpdateEquipmentUseCase.cs` (line 34)
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs` (line 141)

---

### 8. Actor.RefreshParams() のキャッシュ更新ポリシーが不明確

**重大度**: 中　**[Claude]**

**問題**:

`Actor.RefreshParams()` が `public` で公開されており、「UseCase が外部からキャッシュ更新をトリガーできる」とコメントされている。一方でガイドラインは「キャッシュ更新経路を Entity 経由に集約する」と定める。自動更新と手動呼び出しが混在している。

**解決案**:

どちらかに統一し、ドキュメント化する。

- **Option A**: Entity の状態変更メソッドがキャッシュを自動更新し、`RefreshParams()` を非公開にする
- **Option B**: UseCase が明示的に呼び出す契約を正式化し、呼び出し忘れを防ぐ仕組みを用意する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs` (line 302-307)

---

## パフォーマンスレビュー

### 1. 毎フレーム処理に schedule tick で十分な処理が混在している（延期項目の補足）

**重大度**: 高　**[両者]**　**→ Milestone 6 対応**

> 第1回レビュー「パフォーマンス§1」で一部対応済み・残りは Milestone 6 延期と判断済み。両者が同様に指摘したため、対応内容を補足して再掲する。

**問題補足**:

装備更新・売却・回復アイテム使用・帰還判定まで毎フレーム走っており、Actor 数 × 所持品 / 装備の処理が積み上がる。schedule tick / dirty flag / イベント起点に分離すれば毎フレームの不要処理を削減できる。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs` (line 102)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SellItemsUseCase.cs` (line 47)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/UseRecoveryItemOrchestrator.cs` (line 33)

---

### 2. WorldGameLogPresenter が常時大量購読・大量ログを行う

**重大度**: 中　**[Codex]**

**問題**:

`WorldGameLogPresenter` が多数のイベント型を常時購読し、`Debug.Log` と文字列補間が高頻度に発生する。`GameEventBus.OnEvent<T>()` は共有 `Subject` に対して `Where + Select` を積み、イベント種別ごとの fanout が重複している。

**原因**:

debug / log 表示が通常 World scene の entry point として常時有効で、イベント種別ごとの fanout が重複している。

**解決案**:

log presenter を debug flag または development build 限定にする。イベント配送は型別 channel / MessagePipe / 辞書 fanout に寄せる。ログは必要ならバッチ化・レート制限する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Event/GameEventBus.cs` (line 27)
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLogPresenter.cs` (line 35)

---

### 3. AreaEffect の target 解決が全 Actor 走査になっている（延期項目の補足）

**重大度**: 中　**[Codex]**　**→ Milestone 6 対応**

> 第1回レビュー「パフォーマンス§3」で Milestone 6 延期と判断済み。Codex が再指摘したため、解決案を補足して再掲する。

**解決案補足**:

戦闘遭遇用の spatial index を共通化し、効果範囲周辺セルだけを候補にする。Fan 判定は dot / cross と距離二乗で行い、`Sqrt` / `Atan2` などの三角関数を避ける。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AttackAreaTargetResolver.cs` (line 25)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceAreaEffectUseCase.cs` (line 36)

---

### 4. Map chunk mesh 生成が同期スパイクになりうる（延期項目の補足）

**重大度**: 中　**[両者]**　**→ Milestone 6 対応**

> 第1回レビュー「パフォーマンス§5」で Milestone 6 延期と判断済み。両者が再指摘し、具体的な根拠が追加されたため補足して再掲する。

**問題補足**:

`WorldMapView.UpdateVisuals()` が毎フレーム全 floor をポーリングし、未構築 layer を見つけると全 chunk を同期生成する。chunk ごとに `List` / `Dictionary` / `Mesh` / material 配列を作り、`RecalculateNormals()` / `RecalculateBounds()` も走る。

**解決案補足**:

1. 毎フレームポーリングを floor 追加イベント起点に変える
2. chunk 生成 queue を設け、1 フレームあたりの生成数を制限する
3. 頂点 / UV / triangle バッファをサービスフィールドとして再利用し、平面 mesh の法線・bounds は明示値で設定して再計算を避ける

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs` (line 30, 97)
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMeshBuildService.cs` (line 35, 70)

---

### 5. 売却 / 取引経路に GC Alloc が複数ある

**重大度**: 中　**[Codex]**

**問題**:

売却経路で以下の GC Alloc が確認されている。

- `SellItemsUseCase.cs` (line 67): `var toSell = new List<ItemStack>();` を呼び出しごとに生成
- `ExchangeExecutor.cs` (line 71-74): `NormalizeItems()` で `GroupBy` / `Select` / `ToArray` LINQ チェーンが実行され中間 Enumerable が生成される
- `Inventory.cs` (line 151-162): `CanAddAll()` / `AddAll()` 内で `slots.ToList()` による防御的コピーが毎回発生する

> コード確認補足: `SellItemsUseCase.Execute()` は `WorldGameLoopEntryPoint.cs` (line 198-199) で `TickAsync()` 内から毎フレーム呼ばれている。高頻度 GC spike の原因になりうる。

**原因**:

取引 API が `IEnumerable` と防御的コピーを前提にしており、単一アイテム売買にも配列生成と LINQ 正規化を通している。

**解決案**:

1. まず `SellItemsUseCase.Execute()` がゲームループで毎フレーム呼ばれているか確認し、呼ばれているなら schedule tick / イベント起点に変える（パフォーマンス§1 と連動）
2. 単一 `ItemStack` 用の overload を用意し、LINQ 正規化を通さないパスを追加する
3. `CanAddAll()` の `slots.ToList()` を事前確保バッファへ変更する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SellItemsUseCase.cs` (line 67)
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Commerce/ExchangeExecutor.cs` (line 71-74)
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Item/Inventory.cs` (line 151-162)

---

### 6. AI Orchestrator 接続後、ポリシー選択が評価ごとに線形探索になる

**重大度**: 低　**[Claude]**（現時点では未接続のため実害なし）

**問題**:

`AdvanceActorAiOrchestrator.ResolvePolicy()` が評価対象 Actor ごとに `policies.FirstOrDefault(x => x.CanHandle(actor))` でポリシーリストを線形走査している。現状では `AdvanceActorAiOrchestrator` 自体が `WorldGameLoopEntryPoint` から呼ばれていないため（整合性§1 参照）実害はないが、接続後は Actor 数に比例して走査コストが増加する。

> 補足: 「毎 Actor 毎フレーム」の表現は不正確。正確には「AI Orchestrator が接続された後、評価対象 Actor の評価ごとにポリシー線形探索が発生する」。

**解決案**:

AI Orchestrator をゲームループに接続する際（整合性§1 の対応と同タイミング）に、`Behavior` 型から `IActorAiPolicy` への辞書キャッシュを初期化時に構築し、`ResolvePolicy()` を O(1) lookup にする。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/AdvanceActorAiOrchestrator.cs` (line 94-101)

---

### 7. Guild 施設ポイント計算で毎回 LINQ チェーンが実行される

**重大度**: 中　**[Claude]**

**問題**:

`AdventurerGuild.RecalculateFacilityPoints()` が施設ごとに `.Where()` → `.Select()` → `.Sum()` の LINQ チェーンを実行し、毎回スタッフ割り当てリストの線形走査と中間 Enumerable の生成が発生する。

**解決案**:

施設ごとのスタッフリストをキャッシュし、割り当て変更時のみ更新する delta 更新を導入する。LINQ チェーンを単純な for ループに変更する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/AdventurerGuild.cs` (line 65-81)

---

### 8. SelectDungeonTargetFloorUseCase で複数の LINQ チェーンと中間配列が生成される

**重大度**: 低　**[Claude]**

**問題**:

全 floor を `ToArray()` した後、さらに `.Where()` → `.OrderByDescending()` → `.FirstOrDefault()` を実行し、失敗時にも再度 `.OrderBy()` → `.First()` を実行する。複数の中間配列・Enumerable が生成される。

**解決案**:

単一の for ループで条件に合う最高 floor を直接特定する実装に変更する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SelectDungeonTargetFloorUseCase.cs` (line 38-46)

---

## 重複クラス・データクラスレビュー

### 1. InnEconomyStatus / InnDailyReport / InnEconomyStatistics のフィールドが重複している

**重大度**: 中　**[両者]**

**問題**:

宿の経済状態を表すデータが複数のクラスに分散し、`Guests` / `Sales` / `SatisfactionDelta` / `Reputation` 等のフィールドが複数 DTO に混在している。`InnEconomyStatusCalculator` が report から status へ詰め替えており、どれが正典か読み取りにくい。

| クラス | 役割 |
|---|---|
| `Domain/Guild/InnEconomyState` | Domain Entity（評判のみ） |
| `Application/GameLoop/InnEconomyStatus` | 読み取り専用 DTO（13 フィールド） |
| `Application/GameLoop/InnEconomyStatistics` | 統計用 struct |
| `Application/GameLoop/InnDailyReport` | 日次レポート（重複フィールド多数） |

**原因**:

UI 用 DTO と履歴用 report を型として分けた一方、データ構造はほぼ同じまま複製されている。

**解決案**:

1. `InnEconomyStatus` を「現在の経済状態」の正式なモデルとして確立する
2. `InnEconomyStatus` が `InnDailyReport CurrentReport` を内包する、または共通の `InnEconomySummary` 値型を切り出す
3. `InnEconomyStatistics` は日次集計用の内部 struct、`InnDailyReport` は履歴保存用の不変データとして役割を明確化する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/InnEconomyStatus.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/InnDailyReport.cs`（※ Application/GameLoop ではなく Domain/Guild 配下）
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/InnEconomyStatistics.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/InnEconomyStatusCalculator.cs`

---

### 2. ActiveStatusEffect が master/spec 由来の不変値を runtime state にコピーしている

**重大度**: 中　**[Codex]**

**問題**:

`ActiveStatusEffect` が `StatusEffectSpec` の `Type` / `AggregationPolicy` / `TickIntervalSeconds` / `Amount` / `DurationSeconds` をコピーしている。Runtime state と master/spec 由来の不変値が同じクラスに混在しており、再付与時の挙動や変化点が読み取りにくい。

**原因**:

状態異常の「現在の進行状態」と「仕様定義」を早く使いやすくするために同一 object へ畳んでいる。

**解決案**:

`ActiveStatusEffectState` は elapsed / applied / 累積値など可変値だけを持つ。不変値は `StatusEffectSpec` 参照または spec id から resolver で解決する。再付与時に変わる値だけを runtime 側へ明示的に持たせる。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/ActiveStatusEffect.cs` (line 8)
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/StatusEffectSpec.cs` (line 8)
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/ActorEffectInstance.cs` (line 13)

---

### 3. Adventurer / Monster の Factory 上層インターフェースが並列に存在している

**重大度**: 低　**[Claude]**（重大度を中→低に修正）

**問題**:

`ActorFactory` / `ActorFactoryRequest` / `ActorFactoryCore` が共通基盤として存在し、実体の生成ロジックはすでに共通化されている。一方で上層に型別の専用インターフェース層が冗長に存在している。

| Adventurer | Monster |
|---|---|
| `IAdventurerFactory` / `AdventurerFactory` | `IMonsterFactory` / `MonsterFactory` |
| `AdventurerCreateRequest` | `MonsterCreateRequest` |
| `SpawnAdventurerUseCase` | `SpawnMonsterUseCase` |
| `SpawnScheduledAdventurerOrchestrator` | `SpawnScheduledMonsterOrchestrator` |

> コード確認補足: `AdventurerCreateRequest` と `MonsterCreateRequest` を比較すると、`DisplayName` フィールドの有無だけが異なる。`ActorFactoryCore` で共通の `CreateActor()` が実装済みのため、指摘当初の「完全な並列重複」ではなく「上層 interface が冗長」という表現が正確。

**解決案**:

1. `AdventurerCreateRequest` と `MonsterCreateRequest` を `ActorSpawnRequest` に統合し、`DisplayName` をオプション項目にする
2. `IAdventurerFactory` / `IMonsterFactory` を廃止し、単一の `IActorFactory` へ統一する（`ActorFactoryCore` の共通実装をそのまま活かせる）
3. UseCase 層の並列構造は Milestone 6 の移動・AI 整理と合わせて検討する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/IAdventurerFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/IMonsterFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/AdventurerCreateRequest.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/MonsterCreateRequest.cs`

---

### 4. 状態追跡 Service が同パターンで乱立している

**重大度**: 中　**[両者]**

**問題**:

「Actor ID → 状態値の Dictionary」というパターンを持つ Service が個別クラスとして乱立しており、共通の抽象化がない。

| クラス | 追跡内容 |
|---|---|
| `AdventurerRecoveryStateService` | 回復中 HP |
| `AdventurerExplorationStateService` | 冒険目的地 |
| `AdventurerReturnTrackingService` | 帰還進行状況 |
| `AdventurerBattleRecordService` | 戦闘記録 |
| `ActorSpawnCompletionService` | スポーン完了状態 |

**解決案**:

新規追加時は共通パターンを用いる方針をドキュメントに明記する。Milestone 6 以降に汎用 `ActorStateRepository<T>` または統合 `ActorTransientStateRegistry` への統合を検討する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerRecoveryStateService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerExplorationStateService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerReturnTrackingService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AdventurerBattleRecordService.cs`

---

### 5. UseCase と Service の命名基準が統一されていない

**重大度**: 低　**[両者]**

**問題**:

`Application/UseCase/` フォルダ内に「UseCase」と「Service」が混在しており、命名から責務を判別しにくい。副作用を持つコマンド実行でありながら「Service」と命名されている例:

- `ChargeInnFeeService` / `GrantExperienceService` / `DropItemService` / `DespawnAdventurerService`

**解決案**:

命名規則を文書化し、新規実装時から適用する。既存コードは一括リネームよりも段階的対応とする。

- **UseCase**: 入力を受け、副作用を起こすコマンド実行
- **Service**: 状態管理・計算・バリデーション（Repository / Registry パターン相当）

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/ChargeInnFeeService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/GrantExperienceService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/DespawnAdventurerService.cs`

---

### 6. ID 型の型安全性が不均一

**重大度**: 低　**[Claude]**

**問題**:

`CombatEffectExecutionId` という Value Object 型が存在する一方、Actor / Projectile / AreaEffect 等の主要な概念は型付きではない `Guid` をそのまま使用しており、型安全性の扱いが不統一。

**解決案**:

主要な概念に対しても `ActorId` / `ProjectileInstanceId` 等の Value Object 型を導入するか、`CombatEffectExecutionId` も `Guid` に統一するか、方針を決定しドキュメント化する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/CombatEffectExecutionId.cs`

---

## 総合レビュー

### 1. ActorDeparted（帰還完了）時の actor-keyed state cleanup が漏れている可能性がある

**重大度**: 中　**[Codex]**（重大度を高→中に修正、問題を精緻化）

**問題**:

`AdventurerRecoveryStateService` / `AdventurerExplorationStateService` はともに `ActorDefeated` イベントを購読して cleanup を実装している。一方、**帰還完了時**（`ActorDeparted` 相当のイベント）に対する cleanup 購読が見当たらない。冒険者が死亡ではなく帰還で Actor が削除される場合、回復累積値・探索目的地などが残りうる。

> コード確認補足: `ActorDefeated` での cleanup は実装済み（`AdventurerRecoveryStateService.cs` line 23-24、`AdventurerExplorationStateService.cs` line 24-25）。問題は帰還（departure）経路の cleanup 漏れに絞られる。

**原因**:

死亡イベントで cleanup を実装したが、帰還完了時のイベントで同様の cleanup が追加されなかった。

**解決案**:

1. `ActorDeparted` または帰還完了を表すイベントを各 Service が購読し、同様の cleanup を呼ぶ
2. `ActorNavigationService` にも同イベントでの `RemovePathState(actorId)` 呼び出しを追加する
3. 将来の Actor 削除経路が増えた場合のために、「Actor が GameWorldState から除去されるすべての経路」を一覧化しドキュメント化する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerRecoveryStateService.cs` (line 23-24)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerExplorationStateService.cs` (line 24-25)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/ActorNavigationService.cs` (line 10)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/DespawnAdventurerService.cs` (line 42)

---

### 2. 戦闘イベントがトランザクション完了前に発行される経路がある

**重大度**: 中　**[Codex]**

**問題**:

`CombatAttackOccurred` や `ProjectileHit` が、死亡処理や linked damage の完了前に publish される経路がある。購読者が「事後通知」として扱っても、通知時点で state が不確定になりうる。

**原因**:

Effect 実行中に Resolver / Executor がその場で event を publish している。

**解決案**:

戦闘 UseCase 内で発生イベントを一旦収集し、ダメージ適用・死亡解決・報酬・ドロップ・Actor 削除が完了した後にまとめて publish する。少なくとも `ProjectileHit` / `AreaEffectHit` は実際の効果適用後の通知に寄せる。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatDamageResolver.cs` (line 52, 59)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceCombatUseCase.cs` (line 87)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatEffectExecutor.cs` (line 90)

---

## 推奨対応順

### Milestone 5 完了前に確認推奨

| 優先 | 項目 | 理由 |
|---|---|---|
| 1 | 整合性3: 宿泊費不足時の状態遷移（仕様 vs 実装） | ユーザー確認で方針を決める必要がある |
| 2 | 整合性1: AI Orchestrator 未接続 | Milestone 6 の AI 拡張前に接続か明記か決める |
| 3 | 整合性2: SpawnTable 重み無視 | 暫定 TODO が動作に影響している |
| 5 | 重複5: UseCase / Service 命名規則の文書化 | 実装変更なし、Milestone 6 前に方針確定 |

### Milestone 6 開始前に対応推奨

| 優先 | 項目 | 理由 |
|---|---|---|
| 1 | 設計1: Inventory 外部公開（集約境界違反） | 戦闘・アイテム拡張前に修正 |
| 2 | 設計2: Domain static Catalog 依存 | 早期修正が負債を広げない |
| 3 | 総合1: ActorDeparted 時の state cleanup 漏れ修正 | NavMesh / 新 Actor 種別追加前に修正（ActorDefeated 側は対応済み、帰還経路が漏れ） |
| 4 | 整合性4: 武器計算インターフェースの設計判断確定 | 戦闘拡張前に統合か分離かを決定（バグではなく設計論点） |
| 5 | 整合性8: RefreshParams() キャッシュポリシー確定 | 大規模キャッシュ拡張前に方針確定 |
| 6 | 設計3: View の IGameWorldStateReader 依存 | UI 追加前に Query / DTO を分離 |
| 7 | 設計4: 状態保持 Service のフォルダ整理 | 新規 Service 追加前に namespace 整備 |
| 8 | パフォーマンス2: WorldGameLogPresenter の常時購読 | debug build 限定化は影響小 |
| 9 | 重複1: InnEconomyStatus / InnDailyReport 整理 | UI 追加前に読み取りモデルを確定 |
| 10 | 重複2: ActiveStatusEffect の runtime/spec 分離 | 状態異常拡張前に修正 |

### Milestone 6 で合わせて対応

| 項目 | 理由 |
|---|---|
| 設計5（延期）: WorldGameLoopEntryPoint 責務移行 | Application Orchestrator 追加と同タイミング |
| パフォーマンス1（延期）: 毎フレーム処理の schedule 化 | Application Orchestrator 分離と連動 |
| パフォーマンス3（延期）: AreaEffect 全走査 | Spatial Index 共通化と連動 |
| パフォーマンス4（延期）: Chunk mesh 生成スパイク | フロア数増加前に対応 |
| パフォーマンス5: 売却 GC Alloc | 毎フレーム化解消と連動 |
| パフォーマンス6: AI ポリシー選択辞書化 | Actor 数増加に備えた最適化 |
| パフォーマンス7: Guild 施設ポイント LINQ 削減 | スタッフ数増加に備えた最適化 |
| 重複3: Adventurer / Monster Factory 上層 interface 統合 | 移動・AI 整理と同タイミングが効率的（`ActorFactoryCore` は既に共通化済みのため上層のみ） |
| 重複4: 状態追跡 Service の統合 | 新規 Service 追加前に方針確定 |
| 総合1: ActorDeparted 時の state cleanup 漏れ | NavMesh / 新 Actor 種別追加前に修正 |
| 総合2: 戦闘イベントのトランザクション後発行 | 戦闘表示（Milestone 6）追加前に修正 |
| 整合性5: アイテム / ドロップ仕様更新 | 正式アセット差し替え前に整合 |
| 整合性6: AdventurerBattleRecord イベント購読 | 戦闘ログ UI 追加前に実装 |
| 整合性7: IActorBehavior の方針確定 | 大規模変更のため後続 |

### 別タスクとして追跡（第1回レビュー判断を継承）

| 項目 | 備考 |
|---|---|
| Lighthouse 禁止 API（Resources.LoadAsync / SceneManager.LoadSceneAsync） | 既存違反・今回追加なし |
| 重複6: ID 型統一 | 方針決定のみ先行 |

---

## Codex対応ログ 2026-05-13

対応項目:

- 整合性3: 宿泊費不足時の状態遷移は現行挙動（`Preparing` に戻す）を正とし、`docs/roadmap/milestone3-roadmap.md` の Phase 5 / Phase 12 記述を更新した。
- 整合性1: `WorldGameLoopEntryPoint.TickAsync()` に `AdvanceActorAiOrchestrator.ExecuteAsync()` を接続した。既存の `WorldLifetimeScope` 登録を利用し、DI注入を追加した。
- 整合性2: `SpawnScheduledMonsterOrchestrator` の `spawnTable.Entries[0]` 固定参照を、`Weight` に基づく重み付き抽選へ変更した。`SpawnTableTargetType.ActorArchetype` 検証も追加した。
- 重複5: `docs/guidelines/application-boundary-guidelines.md` に DungeonInn の UseCase / Service / StateService / Orchestrator 命名規則を追記した。既存クラスのリネームは互換性維持のため今回行わない方針を明記した。

検証:

- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- 追加差分に対する禁止API検索: 追加なし
- `Addressables.LoadAssetAsync`: 追加なし
- `Resources.Load` / `Resource.Load`: 追加なし
- `Task` / `ValueTask`: 追加なし
- DI登録: `AdvanceActorAiOrchestrator` は既存の `WorldLifetimeScope` 登録を利用
- `LighthouseGenerated` 以下: 編集なし

## Claude Code対応ログ 2026-05-13（続き）

ユーザー指示により、推奨対応順の「Milestone 5 完了前」4項目完了後、「Milestone 6 開始前」10項目をCodexへ1項目ずつ送付して対応。

### 設計1: Inventory 集約境界（ユーザー追加指示）

- `IExchangeParticipant.Inventory { get; }` を削除し、`HasAll` / `CanAddAfterRemoving` / `RemoveRange` / `AddRange` の4メソッドに置き換えた
- `Actor.cs`: `public Inventory` を `public IReadOnlyInventory` に変更、4メソッドの明示的実装を追加（`GainItem` / `GainItems` / `RemoveItem` / `RemoveItems` / `TrySpendGold` も追加）
- `AdventurerGuild.cs`: 同様に4メソッドの明示的実装を追加
- `Facility.cs`: 同様に4メソッドの明示的実装を追加
- `ExchangeExecutor.cs`: `initiator.Inventory.HasAll()` → `initiator.HasAll()` 等に全面置き換え
- `Actor.RefreshParams()`: `public` → `private`（整合性8 Option A と同時対応）

### 設計2: Domain static Catalog 依存

- 確認の結果、`WeaponCombatCalculatorFactory` はすでに `WeaponTypeCombatMaster` をパラメータで受け取る形に修正済みだった（本セッション前に対応済み）
- `WeaponMaster` コンストラクタも同様に外部注入形式になっていた
- 対応不要と判断

### 総合1: ActorDeparted 時の state cleanup 漏れ

- `AdventurerRecoveryStateService` / `AdventurerExplorationStateService`: `ActorDeparted` イベントの購読と cleanup を追加
- `ActorNavigationService`: `IDisposable` 実装追加、`ActorDeparted` / `ActorDefeated` 購読追加、`RemovePathState()` 追加
- 上記3クラスを `Application/UseCase/` から `Application/Service/` へ移動（設計4と同時対応）

### 整合性4: 武器計算インターフェース設計判断確定

- 「攻撃力計算（IWeaponCalculator）」と「戦闘性能（IWeaponCombatCalculator）」の分離を維持する判断を確定
- `docs/design/combat-domain-design.md` に分離方針と使い分け根拠を追記

### 整合性8: RefreshParams() キャッシュポリシー確定

- Option A（Entity 内部で自動更新）を採用
- `Actor.RefreshParams()` を `private` に変更（設計1対応の中で実施）

### 設計3: View の IGameWorldStateReader 依存（ユーザー追加指示）

- `WorldMapLayerViewData` から `MapLayer Layer` プロパティを除去し、`Width` / `Height` を追加
- `WorldMapViewDataProvider` を新設して View へは DTO のみを渡す構造に変更
- `WorldMapView.cs`: `IGameWorldStateReader` 依存を `IWorldMapViewDataProvider` に置き換え
- `MapMeshBuildService.BuildChunk()` を `MapLayer` → `MapLayerId` 引数に変更、`layer.GetCellCenter()` を `GameConstants.MapCellSizeMeters` で置き換え
- `WorldGameLogPresenter` は debug build 限定（`Debug.isDebugBuild` ガード済み）のため許容

### 設計4: 状態保持 Service のフォルダ整理

- `AdventurerRecoveryStateService` / `AdventurerExplorationStateService` / `AdventurerReturnTrackingService` を `Application/UseCase/` → `Application/Service/` へ移動
- namespace を `DungeonInn.Application.Service` に変更

### パフォーマンス2: WorldGameLogPresenter の常時購読

- 確認の結果、`Initialize()` 冒頭に `if (!Debug.isDebugBuild) return;` ガードがすでに実装済みだった
- 対応不要と判断

### 重複1: InnEconomyStatus / InnDailyReport 整理

- `Domain/Guild/InnEconomySummary.cs` を新設（共通フィールドを保持する値型）
- `InnEconomyStatus` が `InnEconomySummary Current` を内包する形に変更
- 既存プロパティ（`GuestsToday` 等）は `Current.` 経由の委譲に変更（外部 API 互換を維持）

### 重複2: ActiveStatusEffect の runtime/spec 分離

- `ActiveStatusEffect` に `public StatusEffectSpec Spec { get; }` を追加
- `Type` / `AggregationPolicy` / `TickIntervalSeconds` は `Spec` への委譲に変更
- 再付与時に変わる可変値（`Amount` / `DurationSeconds` / `ElapsedSeconds` / `AppliedAmount`）のみ runtime state として保持

### 最終検証（全対応完了後）

- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- 禁止API追加なし（Addressables.LoadAssetAsync / Resources.Load / Task / ValueTask）
- LighthouseGenerated 以下: 編集なし

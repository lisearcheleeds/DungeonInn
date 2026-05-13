# Milestone 5 完了前レビュー 第2回

作成日: 2026-05-13

## 目的

`milestone5-completion-review.md`（第1回）で指摘・対応済みの項目を除いた上で、以下の4観点から改めて詳細にレビューする。

- 設計
- 整合性
- パフォーマンス
- 重複クラス・データクラス

> 第1回レビューで「対応済み」とされた項目は本ファイルでは再掲しない。
> 第1回で「Milestone 6へ延期」とされた項目は、延期判断を引き継ぎつつ補足が必要なものだけ記載する。

---

## 設計レビュー

### 1. Domain 層が static Catalog に依存している

**重大度**: 高

**問題**:

`WeaponMaster` コンストラクタが `WeaponTypeCombatMasterCatalog.Get(weaponType)` を直接呼び出している。`WeaponCombatCalculatorFactory` も同様に静的アクセスを行っている。Domain 層が static な Infrastructure 依存を持つことで、テスト容易性が損なわれ、DI の恩恵が受けられない。

**原因**:

マスタデータの初期化時に Catalog への直接参照を使う設計が選ばれた。Infrastructure/Master 管理層と Domain 層の責務が混在している。

**解決案**:

1. `WeaponTypeCombatMaster` をコンストラクタ引数で注入する方式へ統一する
2. Catalog 参照は Factory 層に集約し、Domain Entity 生成時はすべてマスタを注入する形へ移行する
3. 必要に応じて `IWeaponTypeCombatMasterRepository` を作成し、DI 経由の参照へ切り替える

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Master/WeaponMaster.cs` (line 24-28)
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/WeaponCombatCalculatorFactory.cs` (line 11)
- `docs/guidelines/domain-usecase-design-guidelines.md` §6「Domain Entity が static クラスに依存しない」

---

### 2. Inventory が集約境界の外から直接変更されている

**重大度**: 中

**問題**:

`Actor.Inventory` が `public Inventory Inventory { get; }` として公開されており、UseCase 等の外部コードが `actor.Inventory.Add()` / `actor.Inventory.Remove()` を直接呼び出している。これは Actor 集約の境界を迂回しており、キャッシュ同期の責務が分散する原因にもなる。

**原因**:

Inventory 操作が多いため、Actor を経由するメソッドを用意せず直接アクセスを許した設計が採られた。

**解決案**:

1. `public IReadOnlyInventory Inventory { get; }` に変更し、読み取りのみを公開する
2. 変更は `actor.GainItem(ItemStack)` / `actor.LoseItem(Guid)` など Actor 経由のメソッドに集約する
3. Inventory に連動するキャッシュ（装備スロット参照など）を Actor 内で一元管理する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs` (line 21)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/PickUpItemUseCase.cs` (line 66)
- `docs/guidelines/usecase-boundary-guidelines.md` §4「集約内部の可変オブジェクトを直接公開しない」

---

### 3. DecideAdventurerReturnUseCase が読み取り専用なのに IGameWorldState を受け取っている

**重大度**: 低

**問題**:

`DecideAdventurerReturnUseCase.ExecuteAsync(IGameWorldState worldState)` は状態を変更せず読み取りのみ行うが、読み書き両方を持つ `IGameWorldState` を受け取っている。インターフェース分離の原則に違反しており、意図せず状態変更ができる契約になっている。

**原因**:

UseCase の引数型を統一する際に、読み取りだけの場合に `IGameWorldStateReader` へ絞る判断がされなかった。

**解決案**:

引数を `IGameWorldStateReader worldState` に変更する。同様のパターンが他の読み取り専用 UseCase にもないか確認し、一括修正する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/DecideAdventurerReturnUseCase.cs` (line 37)
- `docs/guidelines/usecase-boundary-guidelines.md` §8「IGameWorldState は読み書きでインターフェースを分ける」

---

## 整合性レビュー

### 1. 武器計算インターフェースが 2 系統に分裂している

**重大度**: 高

**問題**:

`IWeaponCalculator`（攻撃力計算）と `IWeaponCombatCalculator`（射程・攻撃速度・攻撃定義）という 2 つのインターフェースが存在し、それぞれに対応する `WeaponCalculatorFactory` と `WeaponCombatCalculatorFactory` がある。`Actor` クラスは両方を別プロパティで保持している。設計ドキュメント（`combat-domain-design.md`）には「責務が異なるため分離」と明記されているが、呼び出し側では両方を常に参照するため、分離の意義が薄れている。

**原因**:

Milestone 5 での武器攻撃システム整理の過程で 2 つのファクトリと計算器が異なるニーズから導入されたが、統合の判断がされなかった。

**解決案**:

1. ドキュメントで「攻撃力 vs 戦闘性能」の分離が本当に必要かを再検討する
2. 統合が適切であれば単一の `IWeaponAttackCalculator` へ統合し、戻り値として攻撃力・射程・速度・攻撃定義を含む `WeaponAttackInfo` を返す
3. 分離を維持する場合は、それぞれの責務と使い分け基準をドキュメントに明記する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/IWeaponCalculator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/IWeaponCombatCalculator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/WeaponCalculators.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/WeaponCombatCalculatorFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs` (line 32-33)
- `docs/design/combat-domain-design.md` (line 61-62)

---

### 2. IActorBehavior が空インターフェースで型チェックに依存している

**重大度**: 中

**問題**:

`IActorBehavior` インターフェースがメンバを一切持たないマーカーインターフェースになっており、Actor や UseCase は `behavior is AdventurerBehavior` のような型チェックで役割を判定している。これはドメイン設計ガイドライン「導出できる識別情報を二重に持たない」に反し、型スイッチが各所に散在する原因になる。

**原因**:

各 Behavior 実装クラスの型そのものが役割を定義する意図で設計されたが、意図がドキュメント化されていない。

**解決案**:

1. 型チェックのみが意図なら、その設計判断をドキュメント化する
2. 将来の拡張を見越すなら、`ActorBehaviorKind` プロパティなど識別用のメンバを追加し、型チェックを排除する
3. 大規模リファクタリングが必要な場合は Milestone 6 以降のタスクとして扱う

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/IActorBehavior.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/UpdateEquipmentUseCase.cs` (line 34)
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs` (line 141)
- `docs/guidelines/domain-usecase-design-guidelines.md` (line 162-200)

---

### 3. Actor.RefreshParams() のキャッシュ更新ポリシーが不明確

**重大度**: 中

**問題**:

`Actor.RefreshParams()` が `public` で公開されており、コメントには「UseCase が外部からキャッシュ更新をトリガーできる」と書かれている。一方でドメイン設計ガイドラインは「キャッシュを持つなら更新経路を Entity 経由に集約する」と定める。状態変更メソッドが自動更新するのか、UseCase が明示呼び出しするのかが混在している。

**原因**:

キャッシュ更新の契約が明文化されないまま実装が進んだ。

**解決案**:

更新ポリシーをどちらかに統一し、ドキュメント化する。

- **Option A**: `Equip` / `IncreaseStats` など Entity の状態変更メソッドがキャッシュを自動更新し、`RefreshParams()` を非公開にする
- **Option B**: UseCase が明示的に `RefreshParams()` を呼び出す契約を正式化し、呼び出し忘れを防ぐ仕組み（例: Result 型で更新トリガーを返す）を用意する

Milestone 6 の大規模キャッシュ管理前に方針を確定する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs` (line 302-307)
- `docs/guidelines/domain-usecase-design-guidelines.md` (line 259-286)

---

## パフォーマンスレビュー

### 1. AI ポリシー選択が毎回線形探索している

**重大度**: 中

**問題**:

`AdvanceActorAiOrchestrator.ResolvePolicy()` が毎回の AI 評価時に `policies.FirstOrDefault(x => x.CanHandle(actor))` を実行し、ポリシーリストを線形走査している。Actor 数が増えるとフレームあたりの走査コストが比例して増加する。

**原因**:

Actor の Behavior 型に基づくポリシー選択にキャッシュやルックアップテーブルを使わず、毎回リスト走査している。

**解決案**:

1. `Behavior` 型から `IActorAiPolicy` への辞書キャッシュを初期化時に構築し、`ResolvePolicy()` を O(1) lookup にする
2. または Actor の Behavior 型ごとに単一ポリシーを注入し、直接参照できるようにする

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/AdvanceActorAiOrchestrator.cs` (line 94-101)

---

### 2. Guild 施設ポイント計算で毎回 LINQ チェーンが実行される

**重大度**: 中

**問題**:

`AdventurerGuild.RecalculateFacilityPoints()` が施設ごとに `.Where()` → `.Select()` → `.Sum()` の LINQ チェーンを実行し、毎回スタッフ割り当てリストの線形走査と中間 Enumerable の生成が発生する。スタッフ数・施設数が増えると累積コストになる。

**原因**:

施設ごとのスタッフリストをキャッシュせず、毎回フィルタリングして集計している。

**解決案**:

1. 施設ごとのスタッフリストをキャッシュし、割り当て変更時のみ更新する delta 更新を導入する
2. LINQ チェーンを単純な for ループに変更し、中間 Enumerable 生成を排除する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/AdventurerGuild.cs` (line 65-81)

---

### 3. WorldMapView が毎フレーム全 floor の未構築チェックを行っている

**重大度**: 中

**問題**:

`WorldMapView.UpdateVisuals()` が毎フレーム呼ばれ、全 floor を走査して未構築の floor がないか確認している。Dungeon floor が多い場合、チェック自体のコストが積み上がる。

**原因**:

新規 floor が追加されたタイミングを直接検知せず、毎フレームのポーリングで未構築を発見する設計になっている。

**解決案**:

1. 最後に構築した floor の index をキャッシュし、新しい index だけ確認する
2. または floor 追加イベント（GameEvent）を起点に Mesh 構築をトリガーし、毎フレームのポーリングを排除する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs` (line 48-67)

---

### 4. MapMeshBuildService が chunk ごとに List/Dictionary を新規生成している

**重大度**: 中

**問題**:

`MapMeshBuildService.BuildChunk()` が chunk ごとに `vertices`、`uv`、`trianglesByKind`、`materials`、`visualKinds` の List/Dictionary を毎回 new している。マップが大きいほど（例: 64×64 ÷ 16×16 = 16 chunk/layer）生成時の GC Alloc が増加する。

**原因**:

chunk 生成が共有バッファを持たず、毎回ローカル変数で Collection を生成している。

**解決案**:

`MapMeshBuildService` にフィールドレベルの再利用バッファ（`List<Vector3>`、`List<Vector2>`、`Dictionary<TileVisualKind, List<int>>`）を持たせ、chunk 生成ごとに `Clear()` して再利用する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMeshBuildService.cs` (line 35-39)

---

### 5. AI ポリシー評価の順序依存と早期終了の欠如

**重大度**: 低

**問題**:

`AdvanceActorAiOrchestrator` の `policies.FirstOrDefault(...)` は最初にマッチしたポリシーを返すが、ポリシーリストの順序が実行時の優先度を決める暗黙の契約になっている。ポリシーの優先度が変わった場合、バグの原因になりやすい。

**原因**:

ポリシー選択の優先度がリストの登録順に暗黙的に依存している。

**解決案**:

`IActorAiPolicy` に優先度プロパティを追加するか、ポリシー登録時に明示的な順序を保証する仕組みを導入する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/AdvanceActorAiOrchestrator.cs`

---

### 6. SelectDungeonTargetFloorUseCase での複数 LINQ チェーンと中間配列生成

**重大度**: 低

**問題**:

`SelectDungeonTargetFloorUseCase.ExecuteAsync()` が全 floor を `ToArray()` した後、さらに `.Where()` → `.OrderByDescending()` → `.FirstOrDefault()` を実行し、失敗時にも再度 `.OrderBy()` → `.First()` を実行する。複数の中間配列・Enumerable が生成される。

**原因**:

Floor 選択ロジックが複数の LINQ チェーンで記述されており、中間結果が都度生成される。

**解決案**:

単一の for ループで戦闘力・floor index を比較し、条件に合う最高 floor を直接特定する実装に変更する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SelectDungeonTargetFloorUseCase.cs` (line 38-46)

---

## 重複クラス・データクラスレビュー

### 1. 状態追跡 Service が個別クラスとして乱立している

**重大度**: 中

**問題**:

Actor/Adventurer の一時状態を追跡する Service が別々のクラスとして複数存在し、実装パターンが類似している。

| クラス | 追跡内容 |
|---|---|
| `AdventurerRecoveryStateService` | 回復中 HP（`Dictionary<Guid, float>`） |
| `AdventurerExplorationStateService` | 冒険目的地（`Dictionary<Guid, LayerPosition>`） |
| `AdventurerReturnTrackingService` | 帰還進行状況（`Dictionary<Guid, Dictionary<int, int>>`） |
| `AdventurerBattleRecordService` | 戦闘記録（`Dictionary<Guid, AdventurerBattleRecord>`） |
| `ActorSpawnCompletionService` | スポーン完了状態 |

いずれも「Actor ID → 状態値の辞書」というパターンを持つが、共通の抽象化がない。

**原因**:

各機能追加時に個別の状態追跡 Service が独立して作られ、共通のパターン化がされなかった。

**解決案**:

1. 即時対応が不要であれば、現状のまま個別クラスを維持し、新規追加時に共通パターンを用いる方針をドキュメントに明記する
2. Milestone 6 以降のリファクタリングで汎用 `ActorStateRepository<T>` または統合 `ActorTransientStateRegistry` への統合を検討する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerRecoveryStateService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerExplorationStateService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerReturnTrackingService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AdventurerBattleRecordService.cs`

---

### 2. Adventurer / Monster の Factory + UseCase 構造が並列に重複している

**重大度**: 中

**問題**:

Adventurer と Monster に対して、ほぼ同じ責務を持つ並列構造が存在している。

| Adventurer | Monster |
|---|---|
| `IAdventurerFactory` / `AdventurerFactory` | `IMonsterFactory` / `MonsterFactory` |
| `AdventurerCreateRequest` | `MonsterCreateRequest` |
| `SpawnAdventurerUseCase` | `SpawnMonsterUseCase` |
| `SpawnScheduledAdventurerOrchestrator` | `SpawnScheduledMonsterOrchestrator` |

`ActorFactory` / `ActorFactoryRequest` / `ActorFactoryCore` が共通基盤として存在するにもかかわらず、上層に型別の専用 Factory 層が冗長に存在している。

**原因**:

Domain Entity として Adventurer / Monster が別型だが、Factory / Spawn ロジックは共通のため、共通化の判断が後回しになった。

**解決案**:

1. `IAdventurerFactory` / `IMonsterFactory` を廃止し、単一の `IActorFactory` へ統一する
2. `AdventurerCreateRequest` / `MonsterCreateRequest` を `ActorSpawnRequest` に統合する
3. Adventurer / Monster の区別は Behavior プロパティで行い、Factory 選択ロジックを排除する

Milestone 6 で移動・AI 整理と合わせて対応するのが適切。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/IAdventurerFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/IMonsterFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/AdventurerCreateRequest.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/MonsterCreateRequest.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SpawnAdventurerUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SpawnMonsterUseCase.cs`

---

### 3. 宿の経済状態を表す DTO が複数の層に分散・重複している

**重大度**: 中

**問題**:

宿の経済状態を表すデータが複数のクラスに分散しており、重複フィールドが多数存在する。

| クラス | 役割 |
|---|---|
| `Domain/Guild/InnEconomyState` | Domain Entity（評判のみ） |
| `Application/GameLoop/InnEconomyStatus` | 読み取り専用 DTO（13 フィールド） |
| `Application/GameLoop/InnEconomyStatistics` | 統計用 struct |
| `Application/GameLoop/InnDailyReport` | 日次レポート |

`Guests`、`Sales`、`SatisfactionDelta`、`Reputation` 等のフィールドが複数 DTO に混在しており、どれを使うべきか不明確。

**原因**:

「現在状態」「統計」「レポート」を分ける意図があったが、役割の境界が明確化されないまま実装が進んだ。

**解決案**:

1. `InnEconomyStatus` を「現在の経済状態」の正式なモデルとして確立する
2. `InnEconomyStatistics` は日次集計用の内部 struct、`InnDailyReport` は履歴保存用の不変データとして役割を明確化し、ドキュメント化する
3. 重複フィールドは統合するか、計算により導出する

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/InnEconomyState.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/InnEconomyStatus.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/InnEconomyStatistics.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/InnEconomyStatisticsService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/InnEconomyStatusCalculator.cs`

---

### 4. UseCase と Service の命名基準が統一されていない

**重大度**: 低

**問題**:

`Application/UseCase/` フォルダ内に「UseCase」と「Service」が混在しており、命名から責務を判別しにくい。

実質的に副作用を持つコマンド実行であるが「Service」と命名されている例:

- `ChargeInnFeeService`
- `GrantExperienceService`
- `DropItemService`
- `DespawnAdventurerService`

**原因**:

「UseCase は非同期コマンド実行、Service は状態管理・計算用途」という傾向があったが、明確な規則が定められないまま命名が進んだ。

**解決案**:

命名規則を文書化し、新規実装時に統一する。当面は既存コードの一括リネームより、規則を明文化して新規追加分から適用する。

- **UseCase**: 入力を受け、副作用を起こすコマンド実行
- **Service**: 状態管理、計算、バリデーション（Repository / Registry パターン相当）

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/ChargeInnFeeService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/GrantExperienceService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/DropItemService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/DespawnAdventurerService.cs`

---

### 5. ID 型の型安全性が不均一

**重大度**: 低

**問題**:

`CombatEffectExecutionId` という Value Object 型が存在する一方、Actor、Projectile、AreaEffect 等の主要な概念は型付きではない `Guid` をそのまま使用している。型安全性の扱いがクラス間で統一されていない。

**原因**:

CombatEffect が複雑なドメイン概念だったため特化 ID 型が導入されたが、他の概念には適用されなかった。

**解決案**:

`ActorId`、`ProjectileInstanceId`、`AreaEffectInstanceId` などの Value Object 型を主要な概念にも導入するか、または `CombatEffectExecutionId` も `Guid` に統一するか、方針を決定してドキュメント化する。

**根拠**:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/CombatEffectExecutionId.cs`

---

## 推奨対応順

### Milestone 5 完了前に対応推奨

| 項目 | 理由 |
|---|---|
| 設計3: DecideAdventurerReturnUseCase のインターフェース分離 | 修正コスト小、契約の明確化 |
| 重複4: UseCase / Service 命名規則の文書化 | 実装変更なし、Milestone 6 での新規追加前に確定が必要 |

### Milestone 6 開始前に対応推奨

| 項目 | 理由 |
|---|---|
| 設計1: Domain static Catalog 依存 | テスト容易性・DI 違反、早期修正が負債を広げない |
| 設計2: Inventory 外部公開 | 集約境界違反、戦闘・アイテム拡張前に修正 |
| 整合性1: 武器計算インターフェース統一 | Milestone 6 の戦闘拡張前に設計判断が必要 |
| 整合性3: RefreshParams() キャッシュポリシー確定 | 大規模キャッシュ拡張前に方針確定 |
| パフォーマンス3: WorldMapView 毎フレーム層チェック | NavMesh 追加でフロア数増加前に修正 |
| パフォーマンス4: MapMeshBuildService バッファ再利用 | フロア数増加でスパイクが目立つ前に修正 |

### Milestone 6 で合わせて対応

| 項目 | 理由 |
|---|---|
| パフォーマンス1: AI ポリシー選択辞書キャッシュ | Actor 数増加に備えた最適化 |
| パフォーマンス2: Guild 施設ポイント LINQ 削減 | スタッフ数増加に備えた最適化 |
| 重複1: 状態追跡 Service の統合 | Milestone 6 での新規 Service 追加前に方針確定 |
| 重複2: Adventurer / Monster Factory 統合 | NavMesh・移動整理と同タイミングが効率的 |
| 重複3: 宿経済 DTO 整理 | UI 追加前に読み取りモデルを確定 |

### 別タスクとして追跡（既存判断を引き継ぎ）

| 項目 | 備考 |
|---|---|
| 整合性2: IActorBehavior 空インターフェース | 大規模変更、Milestone 6 以降 |
| 重複5: ID 型統一 | 方針決定のみ先行 |

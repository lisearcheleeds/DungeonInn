# 設計レビュー報告（Milestone 5 Phase 1）

## 概要

DungeonInnプロジェクト（Milestone 5フェーズ1）の設計レビューを実施しました。全体的には Clean Architecture の原則をよく理解し、基本的な層の分離、イベント駆動設計、Lighthouse フレームワーク統合が適切に実装されています。しかし、以下の重要な設計問題が複数見つかりました。

**評価**: 中程度の設計品質。良い基礎がありますが、Domain層への外部依存、UseCase層での責務曖昧化、インターフェース分割の不完全さが改善の対象です。

---

## 問題一覧

### [設計-1] Domain層が静的クラスに依存（Domain層の汚染）

**重要度**: 高  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs` : 81行、149行

**問題**:
```csharp
// Actor.cs:81
NaturalWeaponTypeCombatMaster = WeaponTypeCombatMasterCatalog.Get(NaturalWeaponType);

// Actor.cs:149
ChangeNaturalWeaponType(WeaponTypeCombatMasterCatalog.Get(weaponType));
```
Domain Entity（Actor）が`WeaponTypeCombatMasterCatalog`（staticクラス）に直接依存している。

**原因**:
- マスタデータへのアクセスが Infrastructure (Repository) ではなく、static Catalog に直接依存している。
- コンストラクタおよびメソッド内で static クラスを呼び出すと、テスト時に差し替え不可能になり、暗黙のグローバル依存が生まれる。

**解決案**:
- `IWeaponMasterRepository` をコンストラクタまたはFactory経由で注入する。
- `WeaponTypeCombatMasterCatalog.Get()` の呼び出しを削除し、Actor生成時にすでに必要なマスタを注入する。
- Factoryパターンを使って、Actor生成責務を集約する。

---

### [設計-2] IGameWorldStateReader が過度に幅広い読み取りメソッドを公開

**重要度**: 中  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/IGameWorldState.cs` : 12-26行

**問題**:
```csharp
public interface IGameWorldStateReader
{
    bool IsInitialized { get; }
    AdventurerGuild Guild { get; }
    GroundMap GroundMap { get; }
    Dungeon Dungeon { get; }
    InnEconomyState InnEconomy { get; }
    IReadOnlyList<Actor> Actors { get; }
    IReadOnlyList<ItemInstance> Items { get; }
    IReadOnlyList<ProjectileInstance> Projectiles { get; }
    IReadOnlyList<AreaEffectInstance> AreaEffects { get; }
    SpawnScheduleState SpawnSchedule { get; }
    Actor FindActor(Guid actorId);
}
```
View層が `IGameWorldStateReader` を通じて Domain Entity 全体にアクセス可能になっている。View は「今どこに何があるか」を知るだけで十分だが、Guild全体、Dungeon全体、複雑な Economy 状態まで読める設計になっている。

**原因**:
- usecase-boundary-guidelines.md §8 に「IGameWorldState は読み書きでインターフェースを分ける」と記載されているが、Reader側がすべてを公開している。
- 将来的に View が Domain ロジックに依存するリスクがある。

**解決案**:
- View が必要な情報を限定し、View 専用の読み取りインターフェース（例: `IGameWorldViewRepository`）を定義する。
- `IGameWorldStateReader` は Application / UseCase 層が Domain 判定に使う用途に絞る。
- View側では `LayerPositionViewMapper` と Actor/Item の位置・タイプだけを参照する設計に変更する。

---

### [設計-3] Service と UseCase の責務が曖昧（命名と実装の混在）

**重要度**: 中  
**場所**: 複数ファイル
- `Application/UseCase/ChargeInnFeeService.cs`
- `Application/UseCase/DespawnAdventurerService.cs`
- `Application/UseCase/GrantExperienceService.cs`
- `Application/UseCase/AdventurerRecoveryStateService.cs`

**問題**:
- `ChargeInnFeeService` / `DespawnAdventurerService` / `GrantExperienceService` は「Service」という名前だが、実装はステートレスな UseCase 処理。
- `AdventurerRecoveryStateService` は `IDisposable` を実装し、イベント購読を持つ → これは true Service だが、同じ `UseCase` フォルダに置かれている。
- review-policy-guideline.md §5, §7 に「UseCase と Service の区別を明確にする」と指摘されているが、まだ混在している。

**原因**: Orchestrator / UseCase / Service の責務範囲が、名前と実装で揺らいでいる。

**解決案**:
- **ステートレス処理 → `XxxUseCase`** に統一（`ChargeInnFeeUseCase`, `DespawnAdventurerUseCase`, `GrantExperienceUseCase`）
- **長期状態管理 → `XxxStateService`** に統一（`IDisposable` 実装必須）
- フォルダも `Application/UseCase/` と `Application/Service/` で分離する。

---

### [設計-4] 長期状態管理 Service が UseCase フォルダに混在

**重要度**: 中  
**場所**: `Application/UseCase/` フォルダ全体

**問題**:
- `IDisposable` を実装し、イベント購読を持つ Service が UseCase フォルダに混在している。
- Milestone 4 phase 7/8 レビュー報告で「UseCase はステートレスに保つ」と指摘されているが、フォルダ構成に反映されていない。

**原因**: 当初 UseCase フォルダに入れたものが、リファクタリング中に責務が分化したが、フォルダ構造は変わっていない。

**解決案**:
```
Application/
  UseCase/          ← ステートレス、外部Entity変更とEvent発行
  Service/          ← 長期状態管理、IDisposable実装
  Orchestration/    ← UseCase / Service の調整
```

---

### [設計-5] RecoverAdventurerAtInnUseCase が UseCase と Service を混在させている

**重要度**: 中  
**場所**: `Application/UseCase/RecoverAdventurerAtInnUseCase.cs`

**問題**:
- `ChargeInnFeeService` / `DespawnAdventurerService` を注入し、その中で処理を実行している。
- これは「UseCase から UseCase を呼ぶ」禁止ルール（usecase-boundary-guidelines.md §1）を Service 経由で迂回している。
- 複数のコンストラクタ重載で互換性を作ろうとしているが、設計上の曖昧さを増幅させている。

**原因**:
- `ChargeInnFeeUseCase` / `DespawnAdventurerUseCase` を作るかどうかの決定が定まらず、両方のパターンをサポートしようとしている。

**解決案**:
- `ChargeInnFeeUseCase` / `DespawnAdventurerUseCase` を正式な UseCase として定義する。
- 複数コンストラクタの重載は削除し、DI で一つだけにする。

---

### [設計-6] WorldActorPresenter が Domain の Behavior 型に依存

**重要度**: 中  
**場所**: `View/Scene/MainScene/World/WorldActorPresenter.cs` : 31-42行

**問題**:
```csharp
Material ResolveActorMaterial(Actor actor)
{
    if (actor.Behavior is AdventurerBehavior)
        return adventurerMaterial;
    if (actor.Behavior is MonsterBehavior)
        return monsterMaterial;
    return otherActorMaterial;
}
```
View Presenter が `actor.Behavior` の型を確認して Material を決定している。View が Domain Behavior クラス型に依存し、将来的に新しい Behavior が追加されるたびに Presenter が変わる。

**原因**: View に「Actor の種別を表示に反映する」ロジックが直接入っている。

**解決案**:
- View 専用の抽象化層（`IActorVisualResolver`）を作り、Domain 型への依存を遮断する。
- Presenter は `IActorVisualResolver` を注入し、アクター ID だけで Material を決定する。

---

### [設計-7] WorldMapView が Milestone 5 方針（chunk mesh）に反した GameObject 大量生成

**重要度**: 中  
**場所**: `View/Scene/MainScene/World/WorldMapView.cs` : 46-137行

**問題**:
```csharp
void CreateTile(Transform layerRoot, MapLayer layer, GridPosition position, Material material)
{
    var tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
    // ...
}
```
- `GameObject.CreatePrimitive()` でタイル数分の Plane GameObject を生成。
- Milestone 5 roadmap では「chunk mesh 生成」が方針だが、この実装は単純な GameObject 生成のまま。

**原因**: Milestone 5 Phase 4 の chunk mesh 生成基盤が未実装のため、placeholder 表示に留まっている。

**解決案**:
- Milestone 5 Phase 4 の実装予定に従い、chunk mesh 生成基盤を設計・実装する。
- 当面 debug 表示として限定するか、feature flag で on/off 可能にする。

---

### [設計-8] WorldGameLoopEntryPoint の責務過多（20+ 依存注入）

**重要度**: 高  
**場所**: `View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs` : 14-80行

**問題**:
- MonoBehaviour が 20+ の UseCase / Orchestrator を直接注入されており、テスト困難。
- ゲームループ全体の複雑な構成が Entry Point 1箇所に集中している。
- 将来の機能追加でさらに肥大化する傾向。

**原因**: View Entry Point が「全 UseCase の呼び出し窓口」として設計されている。

**解決案**:
- Entry Point は「毎フレーム呼び出す最小のオーケストレーション」のみ責務にする。
- `IGameLoopOrchestrator` を Application 層に定義し、全 UseCase の呼び出し順を一元管理する。
- MonoBehaviour は `IGameLoopOrchestrator` だけに依存する形にする。

---

### [設計-9] ActorDefeatOrchestrator の Service / Resolver 混在で順序不明確

**重要度**: 中  
**場所**: `Application/Orchestration/ActorDefeatOrchestrator.cs` : 10-36行

**問題**:
- `GrantExperienceService` / `DropItemService` の実行順序と `CombatDefeatResolver` の関係が不明確。
- Orchestrator が「順序付きの複数処理実行」になっており、各ステップの責務が整理されていない。

**原因**: Orchestrator 内での UseCase 間呼び出し緩和の基準が不明確。

**解決案**:
- 各ステップを独立した UseCase として定義し、Orchestrator は「複数 UseCase の調整」のみに留める。

---

## 総評

### 良い点

1. **IEventPublisher / IEventSubscriber の分離** — usecase-boundary-guidelines.md に完全に従っている。
2. **IGameWorldStateReader / Writer の分離** — 基本的な Read/Write 分離が実装されている。
3. **Orchestrator パターンの導入** — UseCase間の直接呼び出しを避け、Orchestrator を経由させている。
4. **Domain層の基本構造** — Entity / Behavior / Master/Spec の分離が概ね適切。
5. **Lighthouse フレームワーク統合** — Scene分割、LifetimeScope、非同期処理が適切。

### 改善が必要な点（優先順位順）

| 優先度 | 問題 | 影響 |
|---|---|---|
| 高 | Domain層の static 依存（WeaponTypeCombatMasterCatalog） | テスト不可能、グローバル依存 |
| 高 | WorldGameLoopEntryPoint の責務過多 | テスト困難、拡張性低 |
| 高 | Service vs UseCase の命名・責務の曖昧化 | 保守性低下 |
| 中 | IGameWorldStateReader が過度に広い | Domain変更時の影響範囲拡大 |
| 中 | View が Domain 型（Behavior）に依存 | Domain変更時に View も変更が必要 |
| 中 | WorldMapView の GameObject 大量生成 | パフォーマンス問題（Phase 4 で解決予定） |

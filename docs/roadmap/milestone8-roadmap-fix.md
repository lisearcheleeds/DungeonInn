# Milestone 8 Fix — LifetimeScope 責務修正

## 背景

以下の問題が発覚したため、正式タスクとして対処する。

1. `WorldHudCanvasProvider` が `FindFirstObjectByType` でシーン検索（Lighthouse 禁止パターン）
2. スタンドアロンゲームでフォールバック Canvas がデフォルト動作になっている
3. `WorldLifetimeScope` に 3D 描画と HUD/UI が混在している
4. HUD プレゼンターが `WorldCameraController` 等の World 具象型を直接参照しており、HUD が World に不当依存している
5. `MainGameLifetimeScope` が Application/Domain 登録にもかかわらず `View/Scene/MainScene/` 以下に配置されている
6. `SceneGroupLifetimeScopeManager` が設定 SO の非同期ロードを `Configure()` 同期タイミング前に完了させるための回避設計になっており、Repository パターンで解消できる

---

## 今回のスコープ（M8 fix）

- `WorldHudCanvasProvider` / `FindFirstObjectByType` / フォールバック Canvas の削除
- HUD/UI Presenter を `WorldLifetimeScope` から分離する
- World の座標変換・アクティブ階層取得を proxy/registry パターンで抽象化し、GameHUD が World 具象型に依存しない構成にする
- `ActorScenePosition` DTO を定義し、World 型を HUD 側の公開契約に露出させない
- HUD Presenter の Initialize / Update 所有者として `GameHUDEntryPoint` を `GameHUDLifetimeScope` に追加する
- `WorldGameLoopEntryPoint` から HUD/UI Presenter の Initialize / Update 呼び出しを削除する
- `MainGameLifetimeScope` を `Core/` 配下へ移す
- `SceneGroupLifetimeScopeManager` を削除し、Settings Repository パターンに切り替える
- `ActorDetailViewData` の mutable buffer aliasing 問題を記録・対処する

---

## 別宿題（M8 fix スコープ外）

### ModuleScene の親スコープ変更

**問題**: 現在の `ProductEntryPoint` はモジュールシーンの親を `MainGameLifetimeScope` に固定している。`GameHUDLifetimeScope` は本来 `WorldLifetimeScope` の子スコープとすることで HUD プレゼンターが World 実装を直接 inject できるべきだが、Lighthouse 本体の ModuleScene parent-scope API 改善が必要になる。

**M8 fix での暫定対応**: `GameHUDLifetimeScope` は `MainGameLifetimeScope` の子スコープ（`WorldLifetimeScope` と兄弟）として起動する。World との接続は **proxy/registry パターン**（本ドキュメントで定義）で行い、GameHUD は World 具象型に依存しない。

**暫定対応の箇所**: `GameHUDLifetimeScope` が `MainGameLifetimeScope` を親にしている箇所は Lighthouse 制約による暫定。別宿題（ModuleScene parent-scope 改修）完了後は `WorldLifetimeScope` を親に変更し、proxy 経由ではなく直接 inject に切り替える。

**制約**:
- `ScreenStack` が `MainGameLifetimeScope` 配下になることは短期的に許容する
- `ScreenStack` が `WorldLifetimeScope` 配下になる設計は避ける

---

## LifetimeScope 責務設計方針

### MainGameLifetimeScope が持つもの / 持たないもの

| 持ってよいもの | 持つべきでないもの |
|---|---|
| MainGame セッション中に共有される Repository | World の Camera 具象（`WorldCameraController` 等） |
| MainGame セッション中に共有される `IAssetScope` | `MapLayerViewRegistry` 等 World scene 内だけで完結する具象 |
| GameHUD / World の中継 proxy（`ActorScreenPositionProviderProxy` 等） | HUD Presenter 具象 |
| セッション単位のサービス（Domain / Application / Infrastructure） | World scene 内だけで完結する View 実装 |

### Proxy / Registry パターン

ScreenStackModuleProxy と同じアプローチを採用する。

- **`MainGameLifetimeScope`**: `ActorScreenPositionProviderProxy` を登録する。consumer-facing interface（`IActorScreenPositionProvider`）と registration-facing interface（`IActorScreenPositionProviderRegistry`）の両方で公開する
- **`WorldLifetimeScope`**: `WorldActorScreenPositionProvider`（World 具象実装）を登録する。EntryPoint（`WorldActorScreenPositionProviderEntryPoint`）が起動時に proxy へ Register し、破棄時に Unregister する
- **`GameHUDLifetimeScope`**: `IActorScreenPositionProvider`（proxy）だけを inject する。`WorldCameraController` / `MapLayerViewRegistry` / `LayerPositionViewMapper` を直接 inject しない

`IActiveLayerProvider` も同じ proxy/registry パターンで実装する。

**proxy の挙動**:
- provider 未登録時は例外を投げず `false` を返す（HUD 更新中に World が未ロード/破棄済みの場合を考慮）
- 二重 Register は `InvalidOperationException` を投げる
- Unregister は同一インスタンスのときだけ解除し、不一致の場合は無視する

### ActorScenePosition DTO

`LayerPosition` は World 側の概念であり、`IActorScreenPositionProvider` の公開契約に直接露出しない。HUD 側が理解できる汎用 DTO を定義し、World 実装側でそれを `LayerPosition` / `MapLayerViewRegistry` 等に変換する。

```csharp
public readonly struct ActorScenePosition
{
    public ActorScenePosition(int layerId, Vector2Int cell)
    {
        LayerId = layerId;
        Cell = cell;
    }

    public int LayerId { get; }   // "scene layer" の識別子。World 固有の LayerPosition 型は公開しない
    public Vector2Int Cell { get; }
}
```

`LayerId` は現状 World の MapLayer 識別子と 1 対 1 だが、`LayerPosition` 型そのものを公開契約に出していないため今回の抽象化として許容範囲とする。将来 World 以外（GuildHouse 等）でも使う場合は `LayerId` を「シーン内の論理レイヤー」として扱い、各シーン実装がそれを具象レイヤーへ変換する。

### M8 fix 後の暫定 LifetimeScope 構成

```
ProductLifetimeScope
  └── MainGameLifetimeScope  ← MainGame/ 配下。Repository / IAssetScope / proxy を登録
        ├── WorldLifetimeScope     ← 3D描画。World実装 + proxy 登録 EntryPoint を配置
        └── GameHUDLifetimeScope   ← 全HUDプレゼンター。IActorScreenPositionProvider（proxy）を inject
              ※ Lighthouse 制約による暫定。別宿題完了後は WorldLifetimeScope の子にする
```

---

## FIX-1 — 抽象 DTO・Interface・Proxy 定義 + MainGameLifetimeScope 登録

### 変更内容

#### ActorScenePosition DTO の定義

配置: `View/Scene/ActorScenePosition.cs`（または Application 層の共有型として定義）

```csharp
public readonly struct ActorScenePosition
{
    public ActorScenePosition(int layerId, Vector2Int cell)
    {
        LayerId = layerId;
        Cell = cell;
    }

    public int LayerId { get; }
    public Vector2Int Cell { get; }
}
```

#### IActorScreenPositionProvider / IActorScreenPositionProviderRegistry の定義

配置: `View/Scene/IActorScreenPositionProvider.cs`

```csharp
public interface IActorScreenPositionProvider
{
    bool TryGetScreenPosition(ActorScenePosition position, out Vector2 screenPosition);
}

public interface IActorScreenPositionProviderRegistry
{
    void Register(IActorScreenPositionProvider provider);
    void Unregister(IActorScreenPositionProvider provider);
}
```

#### ActorScreenPositionProviderProxy の定義

配置: `View/Scene/ActorScreenPositionProviderProxy.cs`

```csharp
public sealed class ActorScreenPositionProviderProxy :
    IActorScreenPositionProvider,
    IActorScreenPositionProviderRegistry
{
    IActorScreenPositionProvider current;

    public bool TryGetScreenPosition(ActorScenePosition position, out Vector2 screenPosition)
    {
        if (current == null)
        {
            screenPosition = default;
            return false;
        }
        return current.TryGetScreenPosition(position, out screenPosition);
    }

    public void Register(IActorScreenPositionProvider provider)
    {
        if (current != null)
        {
            throw new InvalidOperationException("Duplicate actor screen position provider registration.");
        }
        current = provider;
    }

    public void Unregister(IActorScreenPositionProvider provider)
    {
        if (ReferenceEquals(current, provider))
        {
            current = null;
        }
    }
}
```

#### IActiveLayerProvider / IActiveLayerProviderRegistry の定義

配置: `View/Scene/IActiveLayerProvider.cs`

```csharp
public interface IActiveLayerProvider
{
    int? ActiveLayerId { get; }
}

public interface IActiveLayerProviderRegistry
{
    void Register(IActiveLayerProvider provider);
    void Unregister(IActiveLayerProvider provider);
}
```

#### ActiveLayerProviderProxy の定義

配置: `View/Scene/ActiveLayerProviderProxy.cs`（`ActorScreenPositionProviderProxy` と同じ構造で実装）

#### MainGameLifetimeScope に両 proxy を登録

```csharp
builder.Register<ActorScreenPositionProviderProxy>(Lifetime.Scoped)
    .As<IActorScreenPositionProvider>()
    .As<IActorScreenPositionProviderRegistry>();

builder.Register<ActiveLayerProviderProxy>(Lifetime.Scoped)
    .As<IActiveLayerProvider>()
    .As<IActiveLayerProviderRegistry>();
```

### 完了条件

- [ ] `ActorScenePosition` が定義されており、World 専用型（`LayerPosition` 等）を含まない
- [ ] `IActorScreenPositionProvider` / `IActorScreenPositionProviderRegistry` / `ActorScreenPositionProviderProxy` が存在する
- [ ] `IActiveLayerProvider` / `IActiveLayerProviderRegistry` / `ActiveLayerProviderProxy` が存在する
- [ ] `MainGameLifetimeScope` に両 proxy が登録されている
- [ ] `uloop compile` ErrorCount=0

---

## FIX-2 — World 実装 + Proxy 登録 EntryPoint の追加

### 変更内容

#### WorldActorScreenPositionProvider の実装

配置: `View/Scene/MainScene/World/WorldActorScreenPositionProvider.cs`

`WorldCameraController` + `LayerPositionViewMapper` + `MapLayerViewRegistry` を inject し、`ActorScenePosition`（layerId / cell）を World 座標 → スクリーン座標に変換して返す。World 具象型への依存はこのクラスに閉じる。

```csharp
public sealed class WorldActorScreenPositionProvider : IActorScreenPositionProvider
{
    // WorldCameraController / LayerPositionViewMapper / MapLayerViewRegistry を inject
    public bool TryGetScreenPosition(ActorScenePosition position, out Vector2 screenPosition)
    {
        // layerId / cell を用いて World 座標 → スクリーン座標に変換
    }
}
```

#### WorldActorScreenPositionProviderEntryPoint の実装

配置: `View/Scene/MainScene/World/WorldActorScreenPositionProviderEntryPoint.cs`

```csharp
public sealed class WorldActorScreenPositionProviderEntryPoint : IStartable, IDisposable
{
    readonly WorldActorScreenPositionProvider provider;
    readonly IActorScreenPositionProviderRegistry registry;

    [Inject]
    public WorldActorScreenPositionProviderEntryPoint(
        WorldActorScreenPositionProvider provider,
        IActorScreenPositionProviderRegistry registry)
    {
        this.provider = provider;
        this.registry = registry;
    }

    public void Start() => registry.Register(provider);
    public void Dispose() => registry.Unregister(provider);
}
```

#### WorldActiveLayerProvider / WorldActiveLayerProviderEntryPoint の実装

配置: `View/Scene/MainScene/World/WorldActiveLayerProvider.cs` / `WorldActiveLayerProviderEntryPoint.cs`

`WorldActiveLayerProvider`: `MapLayerViewRegistry` から `ActiveLayerId`（int?）を返す。  
`WorldActiveLayerProviderEntryPoint`: `WorldActorScreenPositionProviderEntryPoint` と同じ構造で proxy に Register / Unregister する。

#### WorldLifetimeScope に登録

```csharp
builder.Register<WorldActorScreenPositionProvider>(Lifetime.Scoped).AsSelf();
builder.RegisterEntryPoint<WorldActorScreenPositionProviderEntryPoint>();

builder.Register<WorldActiveLayerProvider>(Lifetime.Scoped).AsSelf();
builder.RegisterEntryPoint<WorldActiveLayerProviderEntryPoint>();
```

### 完了条件

- [ ] `WorldActorScreenPositionProvider` が `IActorScreenPositionProvider` を実装し、World 具象型（`WorldCameraController` 等）を内部で使っている
- [ ] `WorldActorScreenPositionProviderEntryPoint` が起動時に registry.Register、破棄時に registry.Unregister を呼んでいる
- [ ] `WorldActiveLayerProvider` / `WorldActiveLayerProviderEntryPoint` が同様に実装されている
- [ ] `WorldLifetimeScope` にこれらが登録されている
- [ ] `uloop compile` ErrorCount=0
- [ ] `uloop run-tests EditMode` 全 pass
- [ ] Play モード 30 秒で `[World] GameWorldState initialized`・エラーログ 0

---

## FIX-3 — GameHUD モジュールシーン新設 + WorldLifetimeScope HUD 分離 + WorldHudCanvasProvider 削除

### 変更内容

#### GameHUD モジュールシーンの新設

- **シーンファイル**: `Assets/DungeonInn/Runtime/Scene/ModuleScene/GameHUD.unity`
- **コンポーネント**: `GameHUDModuleScene.cs`
  - 基底クラス: **実装前に既存の ModuleScene 実装パターンを確認すること**（`ProductCanvasModuleSceneBase` を使っている可能性がある）
  - `[SerializeField] Canvas hudCanvas`
  - `public Canvas HUDCanvas => hudCanvas`

#### GameHUDLifetimeScope の実装

親スコープ = `MainGameLifetimeScope`（暫定。別宿題完了後は `WorldLifetimeScope` へ変更）。

`IActorScreenPositionProvider` / `IActiveLayerProvider` は `MainGameLifetimeScope` に登録された proxy が VContainer の親スコープ解決で inject される。**`GameHUDLifetimeScope` 内で World 具象型を直接登録しない。**

```csharp
builder.RegisterComponentInHierarchy<GameHUDModuleScene>();
// HUD プレゼンター
builder.Register<ActorHUDViewPool>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
builder.Register<WorldActorStatusPresenter>(Lifetime.Scoped).AsSelf();
builder.Register<ActorDetailPopupPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
builder.Register<MinimapPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
builder.Register<WorldHudPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
builder.Register<InnStatusPanelPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
builder.Register<PlayerGameEventLogPresenter>(Lifetime.Scoped).AsSelf();
// エントリーポイント
builder.RegisterEntryPoint<GameHUDEntryPoint>();
```

#### HUD プレゼンターの依存変更

`WorldActorStatusPresenter` / `ActorDetailPopupPresenter` / `MinimapPresenter` が inject する依存を変更する。

| 変更前 | 変更後 |
|---|---|
| `WorldCameraController` + `LayerPositionViewMapper` + `MapLayerViewRegistry` | `IActorScreenPositionProvider`（proxy） |
| `MapLayerViewRegistry`（アクティブ階層取得） | `IActiveLayerProvider`（proxy） |

各プレゼンターは `GameHUDModuleScene` を inject して `.HUDCanvas` を参照する（`WorldHudCanvasProvider` の置き換え）。

#### GameHUDEntryPoint の実装

配置: `View/Scene/ModuleScene/GameHUD/GameHUDEntryPoint.cs`

全 HUD プレゼンターを inject し、`IAsyncStartable` / `ITickable` / `IDisposable` として Initialize / Update / Dispose を委譲する。

```csharp
public sealed class GameHUDEntryPoint : IAsyncStartable, ITickable, IDisposable
{
    // 全 HUD プレゼンターを inject して Initialize / Tick / Dispose を委譲
}
```

#### WorldGameLoopEntryPoint から HUD 呼び出しを削除

- HUD プレゼンター（`WorldActorStatusPresenter` 等 6 クラス）の `Initialize()` 呼び出しを削除
- それらの `Update()` / `Tick()` 呼び出しを削除
- 上記プレゼンターへの inject を削除

#### WorldHudCanvasProvider 削除

- `WorldHudCanvasProvider.cs` を完全削除
- `FindFirstObjectByType` によるシーン検索・フォールバック Canvas 生成は以後禁止

#### WorldLifetimeScope から HUD/UI 登録を除去

`WorldHudCanvasProvider` / `ActorHUDViewPool` / 全 HUD プレゼンターの登録を削除する。

#### WorldUI モジュールシーンの扱い

HUD を全て `GameHUD` に移した後、`WorldUI` モジュールシーン・`WorldUILifetimeScope` が不要であれば削除する。削除する場合は `SceneGroupProvider` / `DungeonInnModuleSceneId` も更新する。

#### SceneGroupProvider / DungeonInnModuleSceneId の更新

World グループに `GameHUD` を追加。`DungeonInnModuleSceneId` に `GameHUD` エントリを追加（生成コード更新）。

### 完了条件

- [ ] `GameHUD.unity` / `GameHUDModuleScene` / `GameHUDLifetimeScope` / `GameHUDEntryPoint` が存在する
- [ ] `GameHUDLifetimeScope` が World 具象型（`WorldCameraController` / `MapLayerViewRegistry` / `LayerPositionViewMapper`）を直接登録・inject していない
- [ ] `WorldActorStatusPresenter` / `ActorDetailPopupPresenter` / `MinimapPresenter` が `IActorScreenPositionProvider` / `IActiveLayerProvider` だけを使っている
- [ ] `WorldHudCanvasProvider.cs` が存在しない
- [ ] `FindFirstObjectByType` が View 層コードに存在しない
- [ ] `WorldUICanvas_Fallback` が Play 時に生成されない
- [ ] `WorldLifetimeScope` に HUD/UI 関連登録が存在しない
- [ ] `WorldGameLoopEntryPoint` が HUD プレゼンターを inject / Initialize / Update しない
- [ ] `uloop compile` ErrorCount=0
- [ ] `uloop run-tests EditMode` 全 pass
- [ ] Play モード 30 秒で `[World] GameWorldState initialized`・エラーログ 0

---

## FIX-4 — MainGameLifetimeScope の配置方針（決定済み）

設計ドキュメント初期案では `Core/` 配下・namespace `DungeonInn.Core` への移動を記載していたが、
以下の理由から `MainGame/` 配下・namespace `DungeonInn.MainGame` に残す方針とした（2026-05-24 確定）。

- `MainGameLifetimeScope` はゲームセッション固有の登録を持つ層であり、Product/Core の共通基盤とは責務が異なる
- `ProductEntryPoint`（`DungeonInn.Core`）が `DungeonInn.MainGame` を参照するのは意図的な上位→下位方向の依存であり許容する
- 移動コストに対してメリットが小さい

**現行構成（確定）**:
- `MainGameLifetimeScope.cs`: `Runtime/Scripts/MainGame/` / namespace `DungeonInn.MainGame`
- `MainGameLifetimeScopeController.cs`: `Runtime/Scripts/MainGame/` / namespace `DungeonInn.MainGame`
- `MainGameAssetScopeHolder.cs`: `Runtime/Scripts/MainGame/` / namespace `DungeonInn.MainGame`

---

## FIX-5 — SceneGroupLifetimeScopeManager の削除 & Settings Repository 化

### 問題

`SceneGroupLifetimeScopeManager` が存在する根本原因は、設定 SO を `LifetimeScope.Configure()` の同期タイミングでインスタンスとして `RegisterInstance` する必要があり、そのために Addressables 非同期ロードをスコープ作成前に完了させなければならない設計になっているため。

Repository パターンで解消できる。設定値を直接 inject するのではなく、Repository を inject して初期化時に遅延ロードすれば、スコープ作成前の非同期処理は不要になる。

### AssetScope の所有方針（確定）

| 項目 | 方針 |
|---|---|
| **IAssetScope の所有者** | `MainGameLifetimeScope`。MainGame のライフサイクル中に共有する `IAssetScope` を `MainGameLifetimeScope` に登録する |
| **Dispose** | `IAssetScope` は `MainGameLifetimeScope` の寿命に従って Dispose される。Repository は `IAssetScope` を自前で生成・破棄しない |
| **Repository の責務** | `IAssetScope` をコンストラクタで受け取り、Settings をロードして提供するだけ |
| **WorldLifetimeScope の関与** | `WorldLifetimeScope` は `IAssetScope` を直接扱わない。ロード済み Settings を Repository 経由で参照する |

### 未確定事項（実装前に確認する）

| 確認事項 | 内容 |
|---|---|
| **GameRandom の seed 生成** | `SceneGroupLifetimeScopeManager` が seed 生成を担っている場合、削除後に seed の生成タイミングと所有者を別途設計する |
| **settings load 完了保証** | Repository の `LoadAsync()` が完了する前に設定値を参照するプレゼンター・サービスが存在しないことを保証する初期化順序を明記する |

### 変更内容

#### Settings Repository のインターフェースと実装

設定 SO の種類ごとにインターフェースと実装クラスを作成する。`IAssetScope` はコンストラクタで inject し、`LoadAsync` の引数で渡す形にしない。

```csharp
public interface IActorVisualSettingsRepository
{
    UniTask LoadAsync(CancellationToken cancellationToken);
    ActorVisualSettings Get();
}

public sealed class ActorVisualSettingsRepository : IActorVisualSettingsRepository
{
    readonly IAssetScope assetScope;  // MainGameLifetimeScope から inject される
    ActorVisualSettings settings;

    public ActorVisualSettingsRepository(IAssetScope assetScope)
    {
        this.assetScope = assetScope;
    }

    public async UniTask LoadAsync(CancellationToken cancellationToken)
    {
        // assetScope を使って ScriptableObject をロードし settings に格納する
    }

    public ActorVisualSettings Get() => settings;
}
```

**禁止事項**:
- `LoadAsync` の引数で `IAssetScope` を毎回渡す形にしない
- Repository 内で `AssetScope` を new / Create / Dispose しない
- `WorldLifetimeScope` 側に `IAssetScope` の所有・破棄責務を漏らさない

同様に `WorldGameSettingsRepository` / `LayerPositionViewSettingsRepository` / `WorldCameraSettingsRepository` を作成する。

#### MainGameAssetScopeHolder の定義

`AssetScope` は Lighthouse Extends 側で `internal sealed class` のため、`MainGameLifetimeScope` 側から `Register<AssetScope>()` することはできない。代わりに `IAssetManager.CreateScope()` で生成したスコープを `MainGameLifetimeScope` の寿命で Dispose する薄いホルダーを作成する。

配置: `Core/MainGameAssetScopeHolder.cs`

```csharp
public sealed class MainGameAssetScopeHolder : IDisposable
{
    public MainGameAssetScopeHolder(IAssetManager assetManager)
    {
        AssetScope = assetManager.CreateScope();
    }

    public IAssetScope AssetScope { get; }

    public void Dispose()
    {
        AssetScope.Dispose();
    }
}
```

#### MainGameLifetimeScope での IAssetScope と Repository 登録

```csharp
// MainGameAssetScopeHolder を登録し、その AssetScope を IAssetScope として公開する
// AssetScope は Lighthouse Extends 側で internal のため直接 Register<AssetScope>() は不可
builder.Register<MainGameAssetScopeHolder>(Lifetime.Scoped).AsSelf();
builder.Register(container => container.Resolve<MainGameAssetScopeHolder>().AssetScope, Lifetime.Scoped)
    .As<IAssetScope>();

builder.Register<WorldGameSettingsRepository>(Lifetime.Scoped).As<IWorldGameSettingsRepository>();
builder.Register<LayerPositionViewSettingsRepository>(Lifetime.Scoped).As<ILayerPositionViewSettingsRepository>();
builder.Register<WorldCameraSettingsRepository>(Lifetime.Scoped).As<IWorldCameraSettingsRepository>();
```

#### 初期化 Orchestration での読み込み

`WorldGameLoopEntryPoint.InitializeAsync()` 内で `viewFactory.LoadAsync()` と同じタイミングで Repository の `LoadAsync()` を呼ぶ。

```csharp
await worldGameSettingsRepository.LoadAsync(ct);
await layerPositionViewSettingsRepository.LoadAsync(ct);
await worldCameraSettingsRepository.LoadAsync(ct);
await viewFactory.LoadAsync(ct);
// 以降は通常の初期化処理（この時点で全 Repository はロード済み）
```

#### 設定利用クラスの変更

現在 `WorldCameraSettings` 等を直接 inject しているクラスは、対応 Repository インターフェースを inject して `.Get()` でアクセスする。

#### SceneGroupLifetimeScopeManager の削除

`SceneGroupLifetimeScopeManager.cs` を削除する。

#### MainGameLifetimeScopeController（または MainGameSessionScopeManager）の導入

`SceneGroupLifetimeScopeManager` を削除した後も、`MainGameLifetimeScope` の生成・保持・破棄を担うセッション管理責務は Product/Core 側に残す。`MainGameLifetimeScopeController`（または `MainGameSessionScopeManager`）がその役割を担う。

**担当する責務のみ**:

- `ProductLifetimeScope.CreateChild<MainGameLifetimeScope>()` による子スコープ生成
- アクティブな `MainGameLifetimeScope` の保持
- reboot / session end 時のスコープ破棄
- `SetEnqueueParentLifetimeScope` に渡す親スコープの提供

**持たせない責務**:

- Settings の事前非同期ロード（これが `SceneGroupLifetimeScopeManager` の問題だった）
- `GameRandom` の seed 生成（未確定事項を参照）

`MainGameLifetimeScope` は `ProductLifetimeScope` の子として生成される側であるため、Product 側から通常の DI で直接 inject することはできない。`MainGameLifetimeScopeController` が `CreateChild<T>()` を呼んで生成し、参照を保持して `SetEnqueueParentLifetimeScope` に渡す。

#### Launcher の変更

- `PrepareForWorldSceneAsync` の呼び出しを削除

### 完了条件

- [ ] 未確定事項（GameRandom seed・load 完了保証）が設計ドキュメントに明記されている
- [ ] `SceneGroupLifetimeScopeManager.cs` が存在しない
- [ ] `IAssetScope` が `MainGameLifetimeScope` に登録されている
- [ ] Settings Repository が `IAssetScope` をコンストラクタで受け取り、`LoadAsync(CancellationToken)` でロードしている
- [ ] `LoadAsync` の引数に `IAssetScope` が存在しない
- [ ] Repository が `IAssetScope` を new / Dispose していない
- [ ] `MainGameLifetimeScope` が設定インスタンスを直接登録していない（Repository インターフェースを登録している）
- [ ] `WorldGameLoopEntryPoint.InitializeAsync()` 内で Repository の `LoadAsync(ct)` を呼んでいる
- [ ] `Launcher` が `PrepareForWorldSceneAsync` を呼んでいない
- [ ] `uloop compile` ErrorCount=0
- [ ] `uloop run-tests EditMode` 全 pass
- [ ] Play モード 30 秒で `[World] GameWorldState initialized`・エラーログ 0

---

## 記録 — ActorDetailViewData mutable buffer aliasing 問題

### 問題

`ActorDetailViewData` は `readonly struct` だが `IReadOnlyList<string> EquipmentNames` 等のフィールドが可変リストの読み取り専用ビューになっている場合、外部から内部リストを変更できる。

より重大なリスクとして、**Repository / Query が内部の mutable buffer を再利用している場合、過去に返した DTO の内容が次回 Query 時に書き換わる**。`WorldViewDataProviders` の `CopyActiveActorsTo(List<ActorViewData> results)` / `ConsumeRemovedActorIds()` のパターンがこれに該当しないかを確認する必要がある。

### 修正方針

- ViewData / DTO は返却後に内容が変化しない形にする
- 内部 buffer を再利用する場合は DTO にコピー済み配列を持たせる（`IReadOnlyList<T>` を返すだけでは不十分な場合がある）
- `CopyActiveActorsTo` パターン（渡されたリストへ `Clear + AddRange`）は外部バッファへの書き込みなので問題なし。内部バッファへの参照を直接渡している場合は修正が必要

### 対処

- M8 fix 内で修正するか、別タスクとして切り出すかは実装者がコードを確認して判断する
- 実装前に `WorldViewDataProviders` の buffer 管理方針を確認し、aliasing の有無を明示する

---

## 実施順序

1. **FIX-1**: DTO / Interface / Proxy 定義 + `MainGameLifetimeScope` 登録
2. **FIX-2**: World 実装 + Proxy 登録 EntryPoint 追加（FIX-1 完了後）
3. **FIX-3**: GameHUD 新設 + WorldLifetimeScope HUD 分離 + WorldHudCanvasProvider 削除（FIX-1・FIX-2 完了後）
4. **FIX-4**: `MainGameLifetimeScope` の配置方針確定（`MainGame/` 配下に残す）
5. **FIX-5**: `SceneGroupLifetimeScopeManager` 削除 + Settings Repository 化（未確定事項確認後）

FIX-5 の未確定事項（GameRandom seed・load 完了保証）が確定していない場合は確認を先行させ、実装はその後とする。

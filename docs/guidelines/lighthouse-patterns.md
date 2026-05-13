# Lighthouse パターン集

このプロジェクトにおける Lighthouse フレームワークのルールと実装パターンの逆引きリファレンス。
Claude Code・Codex ともに実装前に本ドキュメントを確認すること。

---

## ルール・制約

### シーン責務の定義（設計の起点）

**シーン分割はすべての設計の起点。実装前に必ず責務を確定させること。**

| シーン種別 | 責務 |
|---|---|
| **MainScene** | そのゲーム状態における核となるコンテンツ（3D 表現・ゲームロジック起点・シーン固有 UI） |
| **ModuleScene** | 複数の MainScene をまたいで再利用できる補助システム（カメラ・モーダル・多言語・オーディオ等） |

**判断基準**:
```
「このコンテンツは複数の MainScene で再利用されるか？」
  Yes → ModuleScene
  No  → MainScene に直接置く
```

**禁止**: 「将来再利用するかもしれない」という仮定での ModuleScene 化。実際に再利用が必要になったときにリファクタリングする。

**設計時に必ずユーザーへ相談すること**:
- 各シーンの責務分担を決める前（何を MainScene / ModuleScene に置くか）
- 迷いが 1 つでもある場合は実装を止めてユーザーに確認する

---

### 絶対禁止事項

#### 1. Addressables 直接呼び出し禁止

```csharp
// NG: 絶対禁止
Addressables.LoadAssetAsync<T>(address);
Addressables.LoadAssetAsync<T>(address).WaitForCompletion();
Resources.Load<T>(path);
```

**理由**: `IAssetScope` が ref-count を管理しているため、直接呼び出すと解放漏れ・二重解放が発生する。`WaitForCompletion` はメインスレッドをブロックしフリーズを引き起こす。

```csharp
// OK: IAssetManager → IAssetScope 経由
readonly IAssetManager assetManager;

public async UniTask<WorldConfigData> LoadAsync()
{
    if (cached != null) return cached;
    using var scope = assetManager.CreateScope();
    var handle = await scope.LoadAsync<WorldConfigSO>("Config/WorldConfig");
    cached = handle.Asset.ToData();
    return cached;
}
```

**AssetScope の寿命ルール**:

- `IAssetScope` は「そのアセットを必要とする機能・画面・サービスの寿命」に合わせて保持する。
- Product 全体の singleton loader に複数機能のロード責務を集約しない。
- ScreenStack prefab は ScreenStack の LifetimeScope / factory が scope を持つ。
- TextTable は TextTable 専用 loader に分離し、ScreenStack や汎用 asset loader と混ぜない。

```csharp
// NG: Product 全体の singleton loader が複数責務を抱え、scope 寿命も機能寿命と一致しない
public sealed class ProductAssetLoader : IScreenStackInstanceFactory, ITextTableLoader, IDisposable
{
    readonly Dictionary<string, (IAssetScope scope, GameObject prefab)> screenStackPrefabCache = new();

    public async UniTask<TScreenStack> CreateScreenStackInstance<TScreenStack>(...)
    {
        var scope = assetManager.CreateScope();
        var handle = await scope.LoadAsync<GameObject>(screenStackAddress, ct);
        screenStackPrefabCache.Add(screenStackAddress, (scope, handle.Asset));
        ...
    }

    public UniTask<IReadOnlyDictionary<string, string>> LoadAsync(...) { ... }
}

// OK: ScreenStack module scope の中で ScreenStack 用 factory が scope を持つ
public sealed class ScreenStackInstanceFactory : IScreenStackInstanceFactory, IDisposable
{
    readonly IAssetScope assetScope;

    public ScreenStackInstanceFactory(IAssetManager assetManager)
    {
        assetScope = assetManager.CreateScope();
    }

    public async UniTask<TScreenStack> CreateScreenStackInstance<TScreenStack>(...)
    {
        var handle = await assetScope.LoadAsync<GameObject>(screenStackAddress, ct);
        ...
    }

    public void Dispose() => assetScope.Dispose();
}
```

**サンプル / 一時的な仮実装の例外**:

学習用サンプル、検証用プロトタイプ、段階的移行中の仮実装では、責務が広い loader や仮の `Resources.Load` 経路を一時的に置いてよい。ただし、必ず TODO コメントで「なぜ一時的か」「正式対応でどこへ移すか」を書くこと。

```csharp
// TODO(milestoneX): Prototype only. Replace with ScreenStackInstanceFactory
// scoped to ScreenStackLifetimeScope before production use.
```

#### 2. SceneManager 直接呼び出し禁止

```csharp
// NG: 絶対禁止
SceneManager.LoadScene("WorldScene");
SceneManager.LoadSceneAsync("WorldScene");
```

**理由**: Lighthouse の遷移パイプライン（アニメーション・DI・カメラ再構築）が壊れる。

```csharp
// OK
await sceneManager.TransitionScene(new WorldScene.WorldTransitionData());
await sceneManager.BackScene();
```

**例外: bootstrap / reboot 用 Launcher**

`Launcher` のように、VContainer / Lighthouse の root を起動・再起動する bootstrap scene では、Lighthouse の MainScene / ModuleScene 遷移がまだ利用できない、または作り直し対象そのものになる。この場合に限り、`SceneManager.LoadSceneAsync` を例外として許可する。

例外として許可する条件:

- root / bootstrap scene のロードまたは reboot 経路である
- 通常のゲーム画面遷移、MainScene 遷移、ModuleScene 遷移ではない
- 例外理由をコメントまたは設計ドキュメントに明記している
- 将来 Lighthouse 側に正式な bootstrap API が用意された場合の移行 TODO を残している

```csharp
// OK: bootstrap / reboot exception.
// TODO: Replace with Lighthouse bootstrap API if the framework provides one.
await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
    LauncherSceneName,
    UnityEngine.SceneManagement.LoadSceneMode.Single);
```

#### 3. Task / ValueTask 禁止

```csharp
// NG
public async Task<T> LoadAsync() { }
public async ValueTask<T> LoadAsync() { }

// OK
public async UniTask<T> LoadAsync() { }
```

#### 4. UnityEngine.UI.Button 禁止

```csharp
// NG
Button myButton;

// OK
LHButton myButton; // LighthouseExtends.UIComponent
```

**理由**: マルチタッチ時の誤タップ防止、`ExclusiveInputService` との統合のため。

#### 5. 旧 Input System 禁止

```csharp
// NG
Input.GetKey(KeyCode.Space);
Keyboard.current.spaceKey.wasPressedThisFrame; // Update() 内でのポーリング禁止
```

**OK**: `IInputLayer` を実装し、MainScene でスタックに積む（→ パターン P4 参照）。

#### 6. モジュールシーンの手動操作禁止

Lighthouse が `SceneTransitionDiff` で管理する。手動 Activate / Deactivate は禁止。

#### 7. モーダルの手動生成禁止

```csharp
// NG
Instantiate(dialogPrefab);
Destroy(dialogInstance);

// OK
screenStackManager.Open(new ConfirmDialog.Data());
screenStackManager.Close();
```

#### 8. Camera.main 依存禁止

シーン遷移後に参照が変わるため。URP カメラスタックの手動組み立ても禁止。
`GetSceneCameraList()` をオーバーライドして `SceneCameraManager` に任せる。

---

### セッション開始時チェックリスト

コードに触れる前に以下をスキャンして違反を報告する:

```
□ Keyboard.current / Mouse.current の直接使用     → IInputLayer 違反
□ Input.GetKey / Input.GetAxis の使用             → 旧 Input System 違反
□ Resources.Load の使用                           → Addressables 違反
□ Addressables.LoadAssetAsync の直接呼び出し      → IAssetScope 違反
□ Task / ValueTask の使用                         → UniTask 違反
□ UnityEngine.UI.Button の使用                    → LHButton 違反
□ SceneManager.LoadScene の直接呼び出し           → Lighthouse 違反
□ IScreenStackManager を使わないモーダル管理      → ScreenStack 違反
□ GetSceneCameraList() 未オーバーライドの MainScene → カメラ未登録
```

違反を発見した場合:
- **バグ修正中**: ユーザーに報告したうえで作業を継続する（スコープ外のため即修正しない）
- **設計・実装中**: 修正を提案し、承認を得てから実装する

既存コードに違反パターンが「動いている」として存在しても、そのパターンを踏襲しない。新規コードは必ず正しいパターンで書く。

---

### Lighthouse 遷移シーケンス（参考）

```
ExclusiveSequence（シーングループ切替時）:
  1. PreTransitionPhase      → 遷移前処理（遮断可能）
  2. OutAnimationPhase       → 退場アニメーション
  3. UnloadCurrentPhase      → 現シーンリソース解放
  4. LoadNextPhase           → 次シーンリソース読み込み
  5. LoadNextSceneStatePhase → TransitionData.LoadSceneState（遮断可能・リダイレクト可）
  6. EnterScene + ResolveCamera（並列）
     ├─ EnterSceneStep:     MainScene.Enter → ModuleScenes.Enter
     └─ ResolveCameraStep:  UpdateCameraStack → InitializeCanvas
  7. InAnimationPhase        → 入場アニメーション
  8. PostTransitionPhase     → 遷移後処理
```

MainScene のオーバーライドポイント:
```
OnLoad()             → アセット読み込み後・DI 前の初期化
OnSetup()            → 初回 Enter 時のみの一回限り初期化
OnEnter()            → シーン入場ロジック（base を必ず呼ぶ）
OnLeave()            → シーン退場ロジック（base を必ず呼ぶ）
InAnimation()        → 入場アニメーション（async）
OutAnimation()       → 退場アニメーション（async）
SaveSceneState()     → 退場時の状態保存。LHSceneInterceptException で遷移リダイレクト可
GetSceneCameraList() → このシーンが持つカメラリストを返す
```

---

## 実装パターン

### 目次

- [P1. MainScene を追加する](#p1-mainscene-を追加する)
- [P2. ModuleScene を追加する](#p2-modulescene-を追加する)
- [P3. ダイアログ（ScreenStack）を追加する](#p3-ダイアログscreenstack-を追加する)
- [P4. シーンに Input を追加する](#p4-シーンに-input-を追加する)
- [P5. アセットを非同期ロードする](#p5-アセットを非同期ロードする)
- [P6. Config Repository を作る](#p6-config-repository-を作る)
- [P7. 新しいシーンをシーングループに登録する](#p7-新しいシーンをシーングループに登録する)
- [P8. シーンの LifetimeScope を書く](#p8-シーンの-lifetimescope-を書く)
- [P9. ProductLifetimeScope にサービスを登録する](#p9-productlifetimescope-にサービスを登録する)
- [P10. シーン遷移を呼び出す](#p10-シーン遷移を呼び出す)

---

## P1. MainScene を追加する

### 基底クラス

`ProductCanvasMainSceneBase<TTransitionData>` を継承する。

```csharp
using Cysharp.Threading.Tasks;
using DungeonInn.LighthouseGenerated;
using DungeonInn.Runtime.Scripts.View.Base;
using Lighthouse.Scene;

namespace DungeonInn.Runtime.Scripts.View.Scene.MainScene.Inn
{
    public class InnScene : ProductCanvasMainSceneBase<InnScene.InnTransitionData>
    {
        // シーン ID は自動生成ファイル DungeonInnMainSceneId.g.cs から取得する
        public override MainSceneId MainSceneId => DungeonInnMainSceneId.Inn;

        // 遷移データは必ずシーンクラスの内部クラスとして定義する
        public class InnTransitionData : ProductTransitionDataBase
        {
            public override MainSceneId MainSceneId => DungeonInnMainSceneId.Inn;
        }

        // ライフサイクルフック（必要なものだけオーバーライドする）
        protected override UniTask OnLoad(ISceneTransitionContext context)
        {
            // アセット読み込み後・DI 前の初期化
            return UniTask.CompletedTask;
        }

        protected override UniTask OnSetup(ISceneTransitionContext context, CancellationToken cancelToken)
        {
            // 初回 Enter 時のみ呼ばれる一回限りの初期化
            return UniTask.CompletedTask;
        }

        protected override UniTask OnEnter(ISceneTransitionContext context, CancellationToken cancelToken)
        {
            // シーン入場ロジック（毎回呼ばれる）
            return base.OnEnter(context, cancelToken); // 必ず base を呼ぶ（InputLayer の Push がある）
        }

        protected override UniTask OnLeave(ISceneTransitionContext context, CancellationToken cancelToken)
        {
            // シーン退場ロジック
            return base.OnLeave(context, cancelToken); // 必ず base を呼ぶ（InputLayer の Pop がある）
        }
    }
}
```

### 必要な Unity コンポーネント（.unity シーンファイル）

シーン GameObject のルートに以下を配置する:
- `InnScene`（MonoBehaviour）
- `CanvasGroup`
- `SceneCanvasInitializer`
- `LHSceneTransitionAnimatorManager`（`[RequireComponent]` で自動要求される）

シーン内に別 GameObject として:
- `InnLifetimeScope`（MonoBehaviour + LifetimeScope）

### 注意

- `MainSceneId` は `DungeonInnMainSceneId.g.cs`（自動生成）から取得する
- 自動生成ファイルは編集しない → 新しい ID を追加したい場合は Claude Code に確認する

---

## P2. ModuleScene を追加する

### 基底クラス

`ProductCanvasModuleSceneBase` を継承する。

```csharp
using DungeonInn.LighthouseGenerated;
using DungeonInn.Runtime.Scripts.View.Base;
using Lighthouse.Scene;

namespace DungeonInn.Runtime.Scripts.View.Scene.ModuleScene.Audio
{
    public class AudioModuleScene : ProductCanvasModuleSceneBase
    {
        public override ModuleSceneId ModuleSceneId => DungeonInnModuleSceneId.Audio;
    }
}
```

### 必要な Unity コンポーネント（.unity シーンファイル）

シーン GameObject のルートに以下を配置する:
- `AudioModuleScene`（MonoBehaviour）
- `CanvasGroup`
- `SceneCanvasInitializer`
- `LHSceneTransitionAnimatorManager`

シーン内に別 GameObject として:
- `AudioLifetimeScope`（MonoBehaviour + LifetimeScope）

---

## P3. ダイアログ（ScreenStack）を追加する

### 基底クラス

`StandardDialogBase`（→ `ProductScreenStackBase` → `ScreenStackBase`）を継承する。

```csharp
using Cysharp.Threading.Tasks;
using DungeonInn.Runtime.Scripts.View.Base;
using LighthouseExtends.ScreenStack;
using UnityEngine;

namespace DungeonInn.Runtime.Scripts.View.UI.Dialog
{
    public class ConfirmDialog : StandardDialogBase
    {
        // 表示データ（IScreenStackData を実装する）
        public class Data : IScreenStackData { }

        // ダイアログを開く
        public override UniTask OnEnter(bool isResume)
        {
            // isResume: true なら Suspend から復元された場合
            return base.OnEnter(isResume); // InputLayer Push がある
        }

        public override UniTask OnLeave()
        {
            return base.OnLeave(); // InputLayer Pop がある
        }
    }
}
```

### 開く / 閉じる

`IScreenStackManager` を `[Inject]` で受け取り、`Open` / `Close` を呼ぶ。

```csharp
// 開く
screenStackManager.Open(new ConfirmDialog.Data());

// 閉じる（現在最前面の画面を閉じる）
screenStackManager.Close();
```

- 手動 `Instantiate` / `Destroy` は禁止
- `UnityEngine.UI.Button` は禁止 → `LHButton` を使う

---

## P4. シーンに Input を追加する

### IInputLayer の実装

```csharp
using DungeonInn.Input;
using LighthouseExtends.InputLayer;
using UnityEngine.InputSystem;

namespace DungeonInn.Runtime.Scripts.Input.Layer
{
    public class InnSceneInputLayer : IInputLayer
    {
        readonly InputActions.SceneActions sceneActions;

        public InnSceneInputLayer(InputActions inputActions)
        {
            sceneActions = inputActions.Scene;
        }

        // true にすると下層レイヤーへの入力伝播をすべて遮断する
        public bool BlocksAllInput => false;

        public bool OnActionStarted(InputAction.CallbackContext ctx) => false;

        public bool OnActionPerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.action.id == sceneActions.Back.id)
            {
                // Back 入力を処理した場合は true を返す（下層に伝播しない）
                return true;
            }
            return false;
        }

        public bool OnActionCanceled(InputAction.CallbackContext ctx) => false;
    }
}
```

### MainScene での登録

`ProductCanvasMainSceneBase` を継承したシーンで以下をオーバーライドする。

```csharp
protected override IInputLayer CreateInputLayer(InputActions inputActions)
{
    return new InnSceneInputLayer(inputActions);
}

protected override InputActionMap GetInputLayerActionMap(InputActions inputActions)
{
    return inputActions.Scene;
}
```

`base.OnEnter()` / `base.OnLeave()` が自動で `PushLayer` / `PopLayer` を呼ぶ。
**`OnEnter` / `OnLeave` をオーバーライドするときは必ず `base` を呼ぶこと。**

---

## P5. アセットを非同期ロードする

### 単一アセット

```csharp
using LighthouseExtends.Addressable;
using VContainer;

public class SomeService
{
    readonly IAssetManager assetManager;

    [Inject]
    public SomeService(IAssetManager assetManager)
    {
        this.assetManager = assetManager;
    }

    public async UniTask DoSomething()
    {
        using var scope = assetManager.CreateScope();
        var handle = await scope.LoadAsync<SomeSO>("Config/SomeKey");
        var data = handle.Asset.ToData();
        // scope を抜けるとアセットが自動解放される
        // scope 外で使いたいデータは ToData() 等で値型に変換しておく
    }
}
```

### キャッシュが必要な場合（Repository パターン）

→ [P6 Config Repository を作る](#p6-config-repository-を作る) を参照。

### 複数アセットの一括ロード

```csharp
using var scope = assetManager.CreateScope();
var handles = await scope.LoadAsync<SomeSO>(new[] { "Config/A", "Config/B" });
```

### ラベル一括ロード

```csharp
using var scope = assetManager.CreateScope();
var handles = await scope.LoadByLabelAsync<SomeSO>("config-label");
```

---

## P6. Config Repository を作る

### インターフェース（Domain 層）

```csharp
// Domain/Inn/IInnConfigRepository.cs
using Cysharp.Threading.Tasks;

namespace DungeonInn.Domain.Inn
{
    public interface IInnConfigRepository
    {
        UniTask<InnConfigData> LoadAsync();
    }
}
```

### 実装（Infrastructure 層）

```csharp
// Infrastructure/Repository/InnConfigRepository.cs
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Inn;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.Infrastructure.Repository
{
    public class InnConfigRepository : IInnConfigRepository
    {
        readonly IAssetManager assetManager;
        InnConfigData cached;

        [Inject]
        public InnConfigRepository(IAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        public async UniTask<InnConfigData> LoadAsync()
        {
            if (cached != null) return cached;
            using var scope = assetManager.CreateScope();
            var handle = await scope.LoadAsync<InnConfigSO>("Config/InnConfig");
            cached = handle.Asset.ToData();
            return cached;
        }
    }
}
```

### ScriptableObject（Infrastructure 層）

```csharp
// Infrastructure/Repository/InnConfigSO.cs
using DungeonInn.Domain.Inn;
using UnityEngine;

namespace DungeonInn.Infrastructure.Repository
{
    [CreateAssetMenu(menuName = "DungeonInn/Config/InnConfig")]
    public class InnConfigSO : ScriptableObject
    {
        public InnConfigData ToData() => new InnConfigData(/* ... */);
    }
}
```

### ProductLifetimeScope への登録

```csharp
builder.Register<InnConfigRepository>(Lifetime.Singleton).As<IInnConfigRepository>();
```

---

## P7. 新しいシーンをシーングループに登録する

`SceneGroupProvider.cs` を編集する。

```csharp
// Core/SceneGroupProvider.cs

// 常駐モジュール（全 MainScene で使うもの）
static readonly ModuleSceneId[] RequireSceneModuleIds =
{
    DungeonInnModuleSceneId.ScreenStack,
};

// MainScene ごとの追加モジュール
static readonly IReadOnlyDictionary<MainSceneId, ModuleSceneId[]> SceneModuleMap =
    new Dictionary<MainSceneId, ModuleSceneId[]>
    {
        { DungeonInnMainSceneId.World, new[] { DungeonInnModuleSceneId.HUD } },
        { DungeonInnMainSceneId.Inn,   new[] { DungeonInnModuleSceneId.HUD } },
    };

// シーングループ
static readonly MainSceneId[][] MainSceneGroupList =
{
    new[] { DungeonInnMainSceneId.World },
    new[] { DungeonInnMainSceneId.Inn },
};
```

### 注意

- `DungeonInnMainSceneId` / `DungeonInnModuleSceneId` は自動生成ファイルから取得する
- 自動生成ファイル（`.g.cs`）は編集禁止

---

## P8. シーンの LifetimeScope を書く

```csharp
// View/Scene/MainScene/Inn/InnLifetimeScope.cs
using VContainer;
using VContainer.Unity;

namespace DungeonInn.Runtime.Scripts.View.Scene.MainScene.Inn
{
    public class InnLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<InnScene>();
            // builder.Register<InnPresenter>(Lifetime.Scoped).AsImplementedInterfaces();
        }
    }
}
```

### 注意

- LifetimeScope は必ずシーン固有の namespace を持つこと（グローバル namespace は禁止）
- `ProductLifetimeScope`（ゲーム全体）への登録と混同しないこと
- シーン MonoBehaviour に `[Inject]` を使う場合は `RegisterComponentInHierarchy` が必須

---

## P9. ProductLifetimeScope にサービスを登録する

`Core/ProductLifetimeScope.cs` に追記する。

```csharp
// Singleton サービス
builder.Register<MyService>(Lifetime.Singleton).As<IMyService>();

// ScriptableObject インスタンス
builder.RegisterInstance(mySettings);

// MonoBehaviour prefab（DontDestroyOnLoad で常駐させる）
builder.RegisterComponentInNewPrefab(myPrefab, Lifetime.Singleton)
       .DontDestroyOnLoad()
       .AsImplementedInterfaces();
```

---

## P10. シーン遷移を呼び出す

```csharp
using DungeonInn.Runtime.Scripts.Core;
using VContainer;

public class SomePresenter
{
    readonly IProductSceneManager sceneManager;

    [Inject]
    public SomePresenter(IProductSceneManager sceneManager)
    {
        this.sceneManager = sceneManager;
    }

    public async UniTask GoToInn()
    {
        await sceneManager.TransitionScene(new InnScene.InnTransitionData());
    }

    public async UniTask GoBack()
    {
        await sceneManager.BackScene();
    }
}
```

---

## 補足: 自動生成ファイルについて

以下のファイルは Lighthouse が自動生成する。**手動編集禁止**。

```
LighthouseGenerated/DungeonInnMainSceneId.g.cs    ← MainSceneId の定義
LighthouseGenerated/DungeonInnModuleSceneId.g.cs  ← ModuleSceneId の定義
LighthouseGenerated/ScreenStackEntityFactory.g.cs ← ScreenStack ファクトリ
```

新しいシーン ID が必要な場合は Claude Code に確認すること。

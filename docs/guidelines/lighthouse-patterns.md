# Lighthouse パターン集

このプロジェクトにおける Lighthouse フレームワークのルールと実装パターンの逆引きリファレンス。
Claude Code・Codex ともに実装前に本ドキュメントを確認すること。

---

## ハードゲート

この節は即時停止・修正が必要な禁止事項を列挙する。
この節に載っていない実装が自動的に許可されるわけではなく、本文の設計方針・判断基準に反する場合もレビュー指摘または作業停止対象とする。

- [ ] `Addressables.LoadAssetAsync` / `Resources.Load` / `Resource.Load` を直接使用していない
- [ ] `SceneManager.LoadScene` / `SceneManager.LoadSceneAsync` を通常のゲーム画面遷移で直接使用していない
- [ ] `Task` / `ValueTask` を使わず、非同期処理は `UniTask` に統一している
- [ ] `UnityEngine.UI.Button` を使わず、Lighthouse の `LHButton` を使っている
- [ ] 旧 Input System の `Input.GetKey` / `Input.GetAxis` / `Keyboard.current` / `Mouse.current` ポーリングを追加していない
- [ ] ModuleScene の Activate / Deactivate を手動操作していない
- [ ] ScreenStack / Modal を手動 `Instantiate` / `Destroy` で管理していない
- [ ] 3D / World 系 MainScene に Screen Space Overlay の UI Canvas / HUD / Popup を直接配置していない
- [ ] MainScene / ModuleScene / LifetimeScope の境界を跨いで、他シーンが所有する Canvas / View / Presenter / Pool / scene-owned component を直接参照・操作していない
- [ ] LifetimeScope にゲームコンテンツ Prefab / UI View Prefab / Popup View の実体を `SerializedField` していない
- [ ] Projectile / AreaEffect / Prop 等のコンテンツ Prefab アドレスを、World 横断の Prefab 一覧ではなく発生元 Master / Spec / Definition から解決している
- [ ] `Camera.main` 依存や URP カメラスタックの手動構築を追加していない
- [ ] Lighthouse / VContainer / 既存フレームワークコードを複製していない
- [ ] LighthouseGenerated 以下の `.g.cs` を手動編集していない
- [ ] Addressables から非同期ロードした設定値を `Configure()` 内で `RegisterInstance` していない（外部ロードが必要な設定は Repository パターンを使う → P6）
- [ ] `FindFirstObjectByType` / `FindObjectOfType` / `FindObjectsOfType` を View 層コードで使用していない
- [ ] スタンドアロンゲームの通常フローで到達しない状況に対してフォールバック動作（代替オブジェクトの生成など）を実装していない（到達しないなら例外またはアサートにする）
- [ ] Launcher / bootstrap / Product 起動処理が、実ゲームセッションや特定コンテンツシーンを直接開始していない

## 完了前チェックリスト

このチェックリストは本文の設計方針を省略するためのものではない。
実装・レビュー時は本文を確認したうえで、最後に確認漏れを防ぐ目的で使用する。

- [ ] 変更内容に該当する Lighthouse パターン（P1〜P10）を本文で確認した
- [ ] シーン責務が MainScene / ModuleScene の判断基準に沿っている
- [ ] 3D / World 系 MainScene の Canvas / HUD / Popup は Canvas ModuleScene に分離している
- [ ] シーン間の連携は、所有者側の Presenter / EntryPoint / Factory / Pool、または親 scope に登録された抽象 interface 経由になっている
- [ ] アセットロードは `IAssetManager` / `IAssetScope` の寿命ルールに沿っている
- [ ] Prefab 生成は Addressable Factory / Pool 経由で行い、LifetimeScope に直接 Prefab 参照を置いていない
- [ ] コンテンツ Prefab の選択責務が発生元 Master / Spec / Definition にある
- [ ] シーン遷移は Lighthouse の `ISceneManager` 経由で行っている
- [ ] Launcher / bootstrap は最初の入口 MainScene へ遷移するだけで、実ゲームセッション開始は Title / Menu / NewGame / Continue などの明示フローに置いている
- [ ] 入力は `IInputLayer` と MainScene 登録経由で処理している
- [ ] ScreenStack / Dialog は Lighthouse の ScreenStack 経由で開閉している
- [ ] LifetimeScope / ProductLifetimeScope への登録漏れがない
- [ ] 自動生成が必要な変更では `.g.cs` を手動編集せず、生成元を更新している

---

## ルール・制約

### シーン責務の定義（設計の起点）

**シーン分割はすべての設計の起点。実装前に必ず責務を確定させること。**

| シーン種別 | 責務 |
|---|---|
| **MainScene** | そのゲーム状態における核となるコンテンツ（3D 表現・ゲームロジック起点・シーン固有 UI。ただし 3D / World 系 MainScene では Canvas UI を置かない） |
| **ModuleScene** | MainScene と分離して管理・描画すべき補助システム（カメラ・モーダル・多言語・オーディオ・HUD Canvas 等） |

**判断基準（順に確認する）**:
```
1. 複数の MainScene で再利用されるか？
     Yes → ModuleScene が適切
2. 1つの MainScene 専用だが、描画パイプライン / Canvas 分離の明確な理由があるか？
     Yes → ModuleScene として切り出してよい（ユーザーに確認の上）
   （例: 3D World シーンと Screen Space Overlay Canvas を別シーンで管理することで描画フローを明確にする）
     No  → MainScene に直接置く
```

**禁止**: 「将来再利用するかもしれない」という仮定のみでの ModuleScene 化。描画分離などの具体的な理由がない場合は MainScene に直接置く。

**3D / World 系 MainScene の追加ルール**:
- World / Dungeon / Battle Field など、3D 表現やゲーム空間を主責務にする MainScene には Screen Space Overlay の UI Canvas を置かない。
- HUD、ActorStatus、Popup、EventLog、Menu などは Canvas ModuleScene（例: `WorldUI`）へ分離する。
- MainScene 側から Canvas ModuleScene の Canvas / View / Presenter / Pool を直接参照・操作してはならない。HUD / Popup の初期化・更新・入力処理は ModuleScene 側の EntryPoint / Presenter が所有する。
- MainScene と ModuleScene の連携が必要な場合は、MainScene が抽象 interface を登録し、ModuleScene 側はその抽象だけを読む。逆に MainScene が ModuleScene の具象を inject して更新してはならない。
- Canvas が見つからない場合の fallback 生成は開発時の保険に留め、正規経路は ModuleScene に置いた Canvas とする。

**設計時に必ずユーザーへ相談すること**:
- 各シーンの責務分担を決める前（何を MainScene / ModuleScene に置くか）
- 迷いが 1 つでもある場合は実装を止めてユーザーに確認する

---

### Bootstrap / Launcher / ゲームセッション開始の分離

Launcher / bootstrap / Product 起動処理は、アプリケーション基盤を立ち上げ、最初の入口 MainScene へ遷移するための System 層である。
実ゲームセッションや特定コンテンツの開始処理をここに置いてはならない。

**責務の分離**:

| 層 | 責務 | 置いてはいけないもの |
|---|---|---|
| Bootstrap / Launcher / Product | Root / Product scope 起動、言語・入力・共通基盤の初期化、最初の入口 MainScene への遷移 | NewGame / Continue、World / Battle / Dungeon など実ゲームセッションの直接開始 |
| Title / Menu / Session Start Flow | ユーザー操作を受け、NewGame / Continue / Load などの開始意思を確定する | Product 全体の初期化、Root / bootstrap の再構築 |
| GameSession scope | 1 回のゲームセッションで共有する Application / Domain state を生成・保持する | Product 全体の常駐基盤、Title 以前から必要な global service |
| Content MainScene | World / Battle / Dungeon / Inn など、開始済みセッションの表示・操作を担う | セッション生成そのもの、Product 起動処理 |

**ルール**:

- Launcher は `Title` / `MainMenu` / `BootComplete` など入口 MainScene へ遷移する。
- Launcher から `World` / `Battle` / `Dungeon` のような実ゲームシーンへ直接遷移しない。
- GameSession scope は Product 起動時ではなく、NewGame / Continue / Load などの明示フローで生成する。
- ProductEntryPoint は Product 初期化を担い、ゲームセッション開始判断を持たない。
- 一時的に Launcher から実ゲームシーンへ直遷移する場合は、削除条件、正式な移動先、撤去 milestone を TODO に明記する。ただし正式な入口シーンが既に存在するなら、一時遷移を延命しない。

```csharp
// NG: Launcher が実ゲームセッションを開始してコンテンツシーンへ直遷移する
gameSessionController.CreateSession();
await sceneManager.TransitionScene(new WorldScene.WorldTransitionData());
```

```csharp
// OK: Launcher は入口 MainScene へ遷移するだけ
await sceneManager.TransitionScene(new TitleScene.TitleTransitionData());
```

```csharp
// OK: Title / Menu 側のユーザー操作でセッションを開始する
newGameButton.onClick.AddListener(() =>
{
    gameSessionController.CreateSession();
    sceneManager.TransitionScene(new WorldScene.WorldTransitionData()).Forget();
});
```

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

#### 1-a. コンテンツ Prefab は Addressable Factory / Pool 経由で生成する

ゲームコンテンツや UI View の Prefab は、Scene / LifetimeScope の直参照から生成してはならない。

```csharp
// NG: LifetimeScope がコンテンツ Prefab を直接保持する
public sealed class WorldLifetimeScope : LifetimeScope
{
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] ActorStatusView actorStatusViewPrefab;
    [SerializeField] ActorDetailPopup actorDetailPopup;
}
```

```csharp
// OK: Addressable Factory が scope を持ち、Pool / Presenter は Factory 経由で取得する
public sealed class WorldViewFactory : IDisposable
{
    readonly IAssetScope assetScope;

    public async UniTask LoadAsync(CancellationToken ct)
    {
        var handle = await assetScope.LoadAsync<GameObject>(address, ct);
        cachedPrefab = handle.Asset;
    }
}
```

**コンテンツ Prefab アドレスの置き場所**:
- Projectile / AreaEffect / SkillEffect: 武器・スキル・効果など、発生元 Master / Spec / Definition に置く。
- Prop / Environment object: セル種別・施設種別・環境物マスタなど、発生元データに置く。
- UI 固定 View（ActorStatus / Popup / Log 等）: UI ModuleScene 用 Factory の固定アドレス、または UI 定義マスタに置く。

**禁止**:
- `WorldContentPrefabConfigSO` のような World 横断の「コンテンツ Prefab 一覧」を作り、Projectile / AreaEffect / Prop の種類選択責務を集約すること。
- コンテンツ種別が増えるたびに LifetimeScope の `SerializedField` が増える設計。
- Presenter 以外が Popup View 実体を直接 DI できる登録。

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
- TODO は直接呼び出しの近くに置き、検索で例外理由と移行先が追える

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
public static IDisposable SubscribeOnClick(this Button button, Action onClick) { }

// OK
LHButton myButton; // LighthouseExtends.UIComponent
```

**理由**: マルチタッチ時の誤タップ防止、`ExclusiveInputService` との統合のため。

未使用の helper / extension であっても `UnityEngine.UI.Button` を受ける Runtime API は残さない。
呼び出し箇所が 0 件でも、API が存在すると後続実装が Lighthouse の `LHButton` ではなく Unity UI Button に乗る導線になる。

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
using DungeonInn.View.Base;
using Lighthouse.Scene;

namespace DungeonInn.View.Scene.MainScene.Inn
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

Canvas MainScene の UI GameObject は、root / Canvas / child UI まで `UI` Layer に設定する。
`SceneCanvasInitializer` や UI 用 camera の culling mask は UI Layer を前提にするため、Default Layer のままだと scene 自体は遷移していても UI が表示されないことがある。

確認項目:
- [ ] Canvas MainScene の root GameObject が `UI` Layer
- [ ] Canvas GameObject が `UI` Layer
- [ ] Button / Text / Image など child UI が `UI` Layer
- [ ] Editor OneShot や scene setup script で UI を生成する場合、生成直後に layer を再帰設定している

### 注意

- `MainSceneId` は `DungeonInnMainSceneId.g.cs`（自動生成）から取得する
- 自動生成ファイルは編集しない → 新しい ID を追加したい場合は Claude Code に確認する

---

## P2. ModuleScene を追加する

### 基底クラス

`ProductCanvasModuleSceneBase` を継承する。

```csharp
using DungeonInn.LighthouseGenerated;
using DungeonInn.View.Base;
using Lighthouse.Scene;

namespace DungeonInn.View.Scene.ModuleScene.Audio
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

Canvas ModuleScene の UI GameObject も、root / Canvas / child UI まで `UI` Layer に設定する。
HUD / Popup / Dialog などが Default Layer のままだと、Canvas 初期化や overlay camera 設定が正しくても描画対象から外れることがある。

---

## P3. ダイアログ（ScreenStack）を追加する

### 基底クラス

`StandardDialogBase`（→ `ProductScreenStackBase` → `ScreenStackBase`）を継承する。

```csharp
using Cysharp.Threading.Tasks;
using DungeonInn.View.Base;
using LighthouseExtends.ScreenStack;
using UnityEngine;

namespace DungeonInn.View.UI.Dialog
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

namespace DungeonInn.Input.Layer
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

namespace DungeonInn.View.Scene.MainScene.Inn
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
- LifetimeScope は composition root であり、コンテンツ catalog ではない。Prefab / Popup / UI View 実体を `SerializedField` して登録してはならない
- LifetimeScope に許可される `SerializedField` は、Scene root、Camera root、設定 ScriptableObject、または composition に必要な scene-owned component に限定する
- Prefab は Addressable Factory / Pool / Presenter が生成し、必要なアドレスは発生元 Master / Spec / Definition から受け取る

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
using DungeonInn.Core;
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

## P11. LifetimeScope ツリー構造と責務ルール

LifetimeScope は「どの型を登録するか」の置き場ではなく、**寿命・所有者・依存可視性を定義する composition root** である。

ある型をどの LifetimeScope に登録するかは、フォルダ、機能名、実装都合ではなく、以下で決める。

- そのインスタンスはいつ生成され、いつ破棄されるべきか
- そのインスタンスを所有する画面・モジュール・セッションはどれか
- どの子スコープから参照されてよく、どのスコープからは参照できてはいけないか
- その型が Scene-owned component / View / Presenter / Application service / Infrastructure service のどれか

### 基本ツリー

具体的な名前はプロジェクトごとに異なるが、責務の層は以下のように分ける。

```
ProductLifetimeScope
  └── Product 全体で共有する基盤
      例: Lighthouse service, language, asset manager, input root, global UI infrastructure

  └── GameSessionLifetimeScope / MainGameLifetimeScope
      └── 1つのゲームセッションで共有する Application / Domain / game state
          例: world state, game clock, usecase, state service, config repository

      └── MainSceneLifetimeScope
          └── その MainScene が所有する scene object / scene adapter / scene-specific presenter
              例: 3D world root, camera controller, map layer registry, scene-specific input

          └── ModuleSceneLifetimeScope
              └── その ModuleScene が所有する UI / popup / HUD / auxiliary view
                  例: HUD canvas, HUD presenter, popup presenter, view pool
```

親スコープの登録型は子スコープから参照できる。逆に、親スコープは子スコープの登録型に依存してはならない。

この依存可視性が LifetimeScope ツリーの本質である。  
「親から子を使いたい」「兄弟スコープ同士で直接 inject したい」という設計になった場合は、登録場所か抽象境界が間違っている。

### 責務ルール

**1. Scope の責務は寿命と所有者で説明する**

LifetimeScope を新設・変更する前に、以下を1行で説明できること。

```
この scope は、{所有者} が所有し、{寿命} で破棄される {責務} を登録する。
```

言えない場合は、スコープが広すぎるか、責務が混在している。

**2. 親 scope は共有基盤、子 scope は具体的な所有物を持つ**

上位 scope ほど長寿命で、具体的な Scene / View / Prefab から遠い責務だけを持つ。

- Product scope: アプリ全体の基盤
- Game session scope: ゲームセッション状態と Application service
- MainScene scope: MainScene が所有する scene object と scene adapter
- ModuleScene scope: ModuleScene が所有する UI / auxiliary view

長寿命 scope に短寿命 object を登録してはならない。  
例: Product scope や Game session scope に Scene-owned `MonoBehaviour`、View、HUD Presenter、Popup Presenter を登録しない。

**3. 関心が違うものを同じ scope に登録しない**

LifetimeScope は「その画面で使うもの全部」を集める場所ではない。

3D 描画、HUD 表示、Popup、ScreenStack、Audio、Input、Application state は、それぞれ寿命と所有者が違うなら scope を分ける。

特に以下は混ぜない。

- 3D / world representation と Screen Space UI
- MainScene 固有の scene object と global module
- Product/global infrastructure と game session state
- Application service と View / Presenter / `MonoBehaviour`
- System / bootstrap 処理と content / game session 処理

**4. 親子 scope で共有する game session state は、親 scope 内で共有寿命にする**

同じ game session state を MainScene と ModuleScene の両方から読む場合、その state / service は GameSession scope に登録し、子 scope 間で同じ instance を参照できる寿命にする。
子 scope ごとに別 instance になる寿命を選ぶと、World と HUD、Battle と HUD などで状態が分裂する。

```csharp
// OK: GameSession scope 配下の MainScene / ModuleScene から同じ session state を読む
builder.Register<GameWorldState>(Lifetime.Singleton).As<IGameWorldState>();
```

**5. 子 scope が必要とする依存は親 scope または自 scope に登録する**

ModuleScene の Presenter が MainScene 固有の情報を必要とする場合、Presenter は MainScene の具象クラスではなく、MainScene が登録する抽象 interface に依存する。

```csharp
// OK: ModuleScene 側は抽象に依存する
public sealed class ActorHudPresenter
{
    readonly IActorScreenPositionProvider screenPositionProvider;
}

// OK: MainScene scope が scene-specific 実装を登録する
builder.Register<WorldActorScreenPositionProvider>(Lifetime.Scoped)
    .As<IActorScreenPositionProvider>();
```

ModuleScene 側から `WorldCameraController` や `MapLayerViewRegistry` のような MainScene 具象型を直接 inject したくなった場合は、抽象境界を追加する。

**6. シーン所有物を他シーンから直接参照・操作しない**

Scene-owned component、Canvas、View、Presenter、Pool、Factory、EntryPoint は、その Scene / ModuleScene / LifetimeScope の所有物である。
別の Scene がこれらの具象 instance を直接持つと、所有者、初期化順、破棄順、表示責務が壊れる。

```csharp
// NG: MainScene が ModuleScene の Canvas や View を直接保持して操作する
public sealed class WorldPresenter
{
    readonly Canvas hudCanvas;
    readonly ActorStatusViewPool actorStatusViewPool;
}
```

```csharp
// OK: ModuleScene 側が HUD の View / Pool / Presenter を所有する
public sealed class HudEntryPoint
{
    readonly ActorHudPresenter actorHudPresenter;

    void Update()
    {
        actorHudPresenter.UpdatePositions();
    }
}
```

```csharp
// OK: MainScene 固有情報は抽象 interface として親または MainScene scope に登録する
public interface IActorScreenPositionProvider
{
    bool TryGetScreenPosition(ActorId actorId, out Vector2 screenPosition);
}
```

許可される連携:

- 親 scope に登録された Application service / Store / Query を子 scope が読む
- MainScene が登録した抽象 interface を ModuleScene が読む
- ModuleScene 内の Presenter / Pool / Factory が同じ ModuleScene の View / Canvas を操作する
- Framework が定義した SceneCamera / Canvas 初期化経路に従う

禁止される連携:

- MainScene が ModuleScene の Canvas / View / Presenter / Pool を inject または serialized field で保持する
- ModuleScene が MainScene の具象 controller / registry / camera を直接 inject する
- 親 scope が子 scope の scene-owned component を解決して更新する
- `FindObjectOfType` / hierarchy 探索 / scene serialized reference で他 Scene の所有物を探して操作する

**7. 親 scope が子 scope の型を inject しない**

MainScene の EntryPoint が ModuleScene の Presenter を inject して更新する設計は、親が子の存在を知るため構造が逆転する。

ModuleScene に Presenter を置くなら、その初期化・更新の起点も ModuleScene 側に置く。

```csharp
// NG: MainScene EntryPoint が ModuleScene Presenter を所有する
public sealed class WorldEntryPoint : MonoBehaviour
{
    [Inject] HudPresenter hudPresenter;
}

// OK: ModuleScene EntryPoint が ModuleScene Presenter を所有する
public sealed class HudEntryPoint : MonoBehaviour
{
    [Inject] HudPresenter hudPresenter;
}
```

**8. 外部ロードが必要な設定値を `Configure()` に間に合わせない**

Addressables から非同期ロードする設定は、`Configure()` の時点では存在しない。  
これを解決するために、scope 作成前に非同期ロードを完了させる専用 manager を作ると、LifetimeScope ツリーが「設定ロード都合」に引きずられる。

代わりに、Repository / Store / Provider を登録し、ロードは初期化フローで行う。

```csharp
// NG: Addressables ロード済みインスタンスを Configure() に間に合わせて RegisterInstance する
builder.RegisterInstance(await LoadSettingsAsync());

// OK: Repository を登録し、ロードは初期化フローで行う
builder.Register<GameSettingsRepository>(Lifetime.Scoped);
// InitializeAsync() などで: await gameSettingsRepository.LoadAsync(ct);
```

Repository / Store / Provider を使う場合は、asset scope の所有者、Dispose 責務、ロード完了前アクセスの扱いを定義する。

**9. Framework 制約の回避策を scope 設計に混ぜない**

「本当は module ごとに親 scope を変えたいが、framework が単一 callback しか持たない」などの制約がある場合、回避策を恒久設計にしない。

短期対応として許容する場合も、以下を明記する。

- 何の framework 制約を回避しているか
- その回避策で依存可視性がどこまで広がるか
- 将来 framework 側をどう直すべきか
- 回避策を撤去する条件

### DungeonInn での適用例

以下は DungeonInn の一例であり、全プロジェクト共通の固定名ではない。

```
ProductLifetimeScope
  └── MainGameLifetimeScope
        ├── WorldLifetimeScope
        │     └── GameHUDLifetimeScope
        └── ScreenStackLifetimeScope
```

- `MainGameLifetimeScope`: ゲームセッション共有の Application / Domain / StateService / Repository
- `WorldLifetimeScope`: World MainScene が所有する 3D 表現、camera、layer registry、scene adapter
- `GameHUDLifetimeScope`: HUD Canvas、HUD Presenter、Popup Presenter、HUD ViewPool
- `ScreenStackLifetimeScope`: global UI stack。World 固有 scope には依存させない

`GameHUDLifetimeScope` が World 固有情報を必要とする場合、`WorldLifetimeScope` が `IActorScreenPositionProvider` などの抽象を登録し、HUD 側はその抽象だけに依存する。

ただし、framework 制約により ModuleScene ごとの parent scope を分けられない場合は、`ScreenStack` が `MainGameLifetimeScope` 配下になる短期対応は許容できる。  
一方で、global module である `ScreenStack` を `WorldLifetimeScope` の子にする設計は避ける。

---

## 補足: 自動生成ファイルについて

以下のファイルは Lighthouse が自動生成する。**手動編集禁止**。

```
LighthouseGenerated/DungeonInnMainSceneId.g.cs    ← MainSceneId の定義
LighthouseGenerated/DungeonInnModuleSceneId.g.cs  ← ModuleSceneId の定義
LighthouseGenerated/ScreenStackEntityFactory.g.cs ← ScreenStack ファクトリ
```

新しいシーン ID が必要な場合は Claude Code に確認すること。

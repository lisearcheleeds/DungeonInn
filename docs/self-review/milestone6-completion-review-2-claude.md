# Milestone 6 完了前セルフレビュー 第2回 (Claude Code)

作成日: 2026-05-18  
レビュアー: Claude Code  
対象: Milestone 6 全差分（task_0001〜task_0006）  
観点: 設計 / 整合性 / パフォーマンス / 重複 / 総合

## レビュー前確認

- [x] `docs/guidelines/lighthouse-patterns.md` 確認済み
- [x] `docs/guidelines/coding-rules.md` 確認済み
- [x] `docs/guidelines/application-boundary-guidelines.md` 確認済み
- [x] `docs/guidelines/implementation-quality-guidelines.md` 確認済み
- [x] `docs/roadmap/milestone6-roadmap.md` 確認済み
- [x] `docs/self-review/milestone6-completion-review-1-total.md`（既存レビュー）確認済み
- [x] 第1回レビュー（total）で解消済みとされた項目の再掲なし（根拠を示した上で扱う）

---

## 差分許可モデル

Milestone 6 差分に含まれる production 契約変更の分類と許可判定を記録する。

### 新規 public API・interface・constructor

| 変更 | 分類 | 判定 | 許可理由 |
|---|---|---|---|
| `INavigationPathProvider.TryFindPath()` 追加（Application 層 interface） | public interface 追加 | 許可 | View 層 NavMesh 実装を Application 境界から隔離するために必要。UnityEngine 非依存 interface として Application 層に置かれており依存方向が正しい |
| `VisualConfigLoader.LoadAsync(CancellationToken)` 追加 | public API 追加 | 許可 | Addressables ロードの非同期エントリポイントとして必要。IDisposable で寿命管理されている |
| `ActorSpriteSet.GetSprite(direction, isWalking, walkFrame)` 追加 | public API 追加 | 許可 | View 層内部の表示ロジックを保持する DTO 相当クラス。View 層内で閉じており依存方向違反なし |
| `MapLayerViewRegistry.GetTileRoot(MapLayerId)` 追加 | public API 追加 | 許可 | `NavMeshBuildService` から tile root GameObjectを参照するために必要。View 層内部で閉じている |
| `WorldMapView.IsTileBuildCompleted(MapLayerId)` 追加 | public API 追加 | 許可 | `WorldGameLoopEntryPoint` がマップ描画完了を確認するために必要な Query |
| `WorldActorViewRegistry.ForEachActorView(Action<Guid, ActorView>)` 追加 | public API 追加 | 許可 | イテレーション時のコレクションコピーを避けるために必要。View 層内部に閉じている |

### 長期状態の追加

| 変更 | 分類 | 判定 | 許可理由 |
|---|---|---|---|
| `WorldMapView.scheduledLayerIds / completedLayerIds / remainingChunkCountsByLayer` | View 層の長期状態 | 許可（ただしパフォーマンス指摘あり、後述 §5） | チャンク分割ビルドのための進捗管理。View 層で完結している。ただし再生成に対する invalidation が未実装（§1 参照） |
| `NavMeshBuildService.bakedLayerIds` | View 層の長期状態 | 許可（ただし再生成問題あり、§1 参照） | NavMesh の重複 bake を防ぐための進捗管理。View 層で完結 |
| `WorldActorPresenter.actorBehaviorTypes / walkingActorsThisFrame` | View 層の長期状態 | 許可 | アクター方向・ウォーク判定に必要な View 層状態。Application 境界を越えていない |

### DI 登録変更

| 変更 | 判定 | 許可理由 |
|---|---|---|
| `UnityNavMeshPathProvider` を `.As<INavigationPathProvider>()` で登録 | 許可 | Application 境界インターフェースによる View 実装の隔離。正当なパターン |
| `VisualConfigLoader`, `ActorPrefabSource`, `ActorSpriteVisualConfig`, `MapMeshBuildService`, `NavMeshBuildService`, `EnvironmentObjectPlacer` の Scoped 登録追加 | 許可 | いずれも View 層で寿命が WorldLifetimeScope に一致する。IDisposable 実装が Scoped 解放と整合している |

---

## 新規概念追加ゲート

Milestone 6 で追加された主要な型について記録する。

### VisualConfigLoader

追加した型: `VisualConfigLoader`  
既存の類似概念: `MapMaterialSet`（Milestone 5 以前から存在）  
意味差分:  
- `MapMaterialSet` は Material の fallback セットを保持する View 層定数管理クラス  
- `VisualConfigLoader` は Addressables から Material・Sprite を非同期ロードし、IAssetScope で寿命管理する。所有者・更新契機・ロード責務が異なる  

代替しなかった理由: `MapMaterialSet` に Addressables ロード責務を追加すると、fallback 管理とアセットロード管理が混在し、Dispose 契約が複雑化する  
統合・削除条件: アセットロードと fallback 管理を統合する共通基盤が導入された場合

---

### ActorSpriteSet

追加した型: `ActorSpriteSet`  
既存の類似概念: `ActorSpriteVisualConfig`（同 Milestone 6 で追加）  
意味差分:  
- `ActorSpriteSet` は 1 ActorBehaviorType 分の idle/walk スプライト配列を保持する DTO。所有者は VisualConfigLoader  
- `ActorSpriteVisualConfig` は全 BehaviorType のスプライト逆引きを持つ View 層 Service。所有者は DI コンテナ（Scoped）  

代替しなかった理由: 1 種の BehaviorType 分のスプライト群と、全種の逆引き Service は寿命・利用者・更新契機が異なる  
統合・削除条件: スプライト管理が Actor 以外（Item、Projectile など）に拡張され、共通 AssetSet 基底型が導入された場合

---

### INavigationPathProvider / UnityNavMeshPathProvider

追加した型: `INavigationPathProvider`（Application 層 interface）、`UnityNavMeshPathProvider`（View 層実装）  
既存の類似概念: `AStarPathfinder`（既存。Application 層内の pathfinding 実装）  
意味差分:  
- `AStarPathfinder` は Domain/Application 内で完結する A* 実装  
- `INavigationPathProvider` は UnityEngine NavMesh など外部エンジン依存の実装を Application 境界で隔離するための抽象。利用者（ActorNavigationService）から Unity 実装を切り離す  

代替しなかった理由: `AStarPathfinder` に NavMesh 実装を追加すると、Application 層が `UnityEngine.AI` に依存する境界違反が生じる  
統合・削除条件: ナビゲーション実装が Unity NavMesh 一本に統一され A* fallback が不要になった場合、または複数実装を統合する Composite が導入された場合

---

### NavMeshBuildService

追加した型: `NavMeshBuildService`  
既存の類似概念: `MapMeshBuildService`（Mesh ジオメトリビルド）  
意味差分:  
- `MapMeshBuildService` はタイルの Mesh を構築する  
- `NavMeshBuildService` は構築済み Mesh から NavMesh Surface を bake する。依存する Unity API、実行タイミング（チャンク完了後）、所有状態（bakedLayerIds）が異なる  

代替しなかった理由: `MapMeshBuildService` に NavMesh bake を追加すると Mesh ビルドと NavMesh ベイクが同じクラスに混在し、bake 状態管理と Mesh 生成の責務が混じる  
統合・削除条件: NavMesh ベイクの自動化 API が導入され手動 bake 不要になった場合

---

### EnvironmentObjectPlacer

追加した型: `EnvironmentObjectPlacer`  
既存の類似概念: `MapMeshBuildService`（タイルの Mesh 生成）  
意味差分:  
- `MapMeshBuildService` は床・壁・階段の Mesh を構築するタイル地形ビルダー  
- `EnvironmentObjectPlacer` は特定タイル位置に Prop オブジェクト（StairUp/Down のマーカー）を配置するオブジェクト管理クラス。所有する GameObject リストとその Dispose 責務が異なる  

代替しなかった理由: `MapMeshBuildService` に GameObject 生成・破棄の責務を追加すると、Mesh Buffer 管理と GameObject 管理が混在する  
統合・削除条件: Prop の数が増え、Prop 管理の共通基盤（Prefab pool など）が導入された場合

---

## 設計レビュー

### 1. NavMesh 再生成後の更新が機能しない

重大度: 高

問題:

roadmap Phase 5 の完了条件「ダンジョン再生成後も NavMesh が正しく更新される」が未達。  
`NavMeshBuildService.bakedLayerIds` に一度 layer id が記録されると以降の bake をスキップする。同様に `WorldMapView.scheduledLayerIds` / `completedLayerIds` も永続する。  
同じ layer id でダンジョンフロアが再生成された場合、chunk mesh も NavMesh も再構築されない。

原因:

`NavMeshBuildService` と `WorldMapView` の状態管理がいずれも「初回ビルド完了の記録」として設計されており、layer の再生成（revision 変化）イベントを受け取る契約が存在しない。  
再生成イベントが Application 層で発行されても、View 層の `scheduledLayerIds` / `bakedLayerIds` を invalidate する経路がない。

解決案（短期）:

```csharp
// WorldMapView に追加
public void InvalidateLayer(MapLayerId layerId)
{
    scheduledLayerIds.Remove(layerId.Value);
    completedLayerIds.Remove(layerId.Value);
    remainingChunkCountsByLayer.Remove(layerId.Value);
    layerViewRegistry.DestroyLayerObjects(layerId);
}

// NavMeshBuildService に追加
public void InvalidateLayer(MapLayerId layerId)
{
    bakedLayerIds.Remove(layerId.Value);
}
```

上位の `WorldLayerViewController` または `WorldPresenter` が再生成イベントを受け取り、両 Service を invalidate する。

解決案（根治）:

`WorldMapLayerViewData` に revision フィールドを追加し、`WorldMapView` が `layerId + revision` の組み合わせで build 完了を管理する。revision 変化を検出した時点で古いオブジェクトを破棄して再スケジュールする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/NavMeshBuildService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapLayerViewRegistry.cs`
- `docs/roadmap/milestone6-roadmap.md`（Phase 5 完了条件）

完了条件:

- [ ] `NavMeshBuildService.InvalidateLayer(MapLayerId)` が存在し、`bakedLayerIds` から対象 id を除去する
- [ ] `WorldMapView.InvalidateLayer(MapLayerId)` が存在し、`scheduledLayerIds` / `completedLayerIds` / `remainingChunkCountsByLayer` から対象 id を除去し、layer root を破棄する
- [ ] 再生成イベント発生時に上記 invalidate が呼ばれる経路が存在する（イベント購読またはコールバック）
- [ ] uloop PlayMode 30 秒確認でダンジョン再生成後にマップが再描画されることを確認
- [ ] `uloop.cmd compile --project-path Client` が成功している

対応区分: **要修正**（第1回レビュー [A] と同一。未対応）

---

### 2. UnityNavMeshPathProvider の DI グループ分類

重大度: 低

問題:

`WorldLifetimeScope.cs:82` で `UnityNavMeshPathProvider`（View 層クラス）が `// === Application: ナビゲーション / 空間 ===` グループに含まれている。View 層のクラスが Application グループのコメント下に記述されているため、実態と乖離したコメントになっている。

原因:

登録インターフェース（`INavigationPathProvider`）が Application 層であるため、グループ分類の基準が「登録 interface の層」になってしまった。実装クラスの層で分類すべきところ、interface の層で分類した。

解決案:

`WorldLifetimeScope.cs` の `UnityNavMeshPathProvider` 登録行を `// === View: マップ描画 ===` グループに移動する。`.As<INavigationPathProvider>()` を明示することで、Application インターフェースとして機能することは読み取れる。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`

完了条件:

- [ ] `WorldLifetimeScope.cs` で `builder.Register<UnityNavMeshPathProvider>(...)` が `// === View: マップ描画 ===` ブロック内にある
- [ ] `uloop.cmd compile --project-path Client` が成功している

対応区分: **推奨改善（次マイルストーン）**（第1回レビュー [G] と同一。未対応）

---

## 整合性レビュー

### 3. EnvironmentObjectPlacer の Prefab 化要件（ユーザー判断待ち）

重大度: 中

問題:

`docs/roadmap/milestone6-roadmap.md` Phase 4 の完了条件に「プロップ Prefab が tile 位置に配置される」「初期実装は柱・松明・木などの小型プロップを想定する（3D モデル Prefab）」と記述されているが、`EnvironmentObjectPlacer.PlacePropAt()` は `GameObject.CreatePrimitive(PrimitiveType.Cube)` で Cube を直生成しており Prefab 差し替え口が存在しない。  
一方、task_0004 の完了条件は「StairUp / StairDown タイル位置にプロップキューブが配置される」と記述されており、task と roadmap に齟齬がある。

原因:

task 設計時に roadmap の Prefab 要件が task 完了条件に正確に反映されなかった。Claude Code が task_0004 を承認した際に roadmap との齟齬を見落としている。

解決案（延期する場合）:

`EnvironmentObjectPlacer` は Prefab 未設定時の fallback として Cube を表示する実装として扱い、Prefab 差し替え口の追加を Milestone 7 タスクとして起票する。  
roadmap Phase 4 完了条件の記述を「Cube 仮プロップが tile 位置に配置される（Prefab 化は Milestone 7）」に更新する。

解決案（Milestone 6 内で対応する場合）:

```csharp
public sealed class EnvironmentObjectPlacer : IDisposable
{
    readonly VisualConfigSettings settings; // ActorSpriteVisualConfigSO の prefab 参照を経由

    void PlacePropAt(...)
    {
        var prefab = settings.ActorSpriteVisualConfigSO != null
            ? settings.ActorSpriteVisualConfigSO.ActorPrefab
            : null;
        var propObject = prefab != null
            ? Object.Instantiate(prefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);
        // ...
    }
}
```

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/EnvironmentObjectPlacer.cs`
- `docs/roadmap/milestone6-roadmap.md`（Phase 4 完了条件）
- `tasks/task_0004.md`（完了条件）

完了条件（延期する場合）:

- [ ] `docs/roadmap/milestone6-roadmap.md` の Phase 4 完了条件が実装状態（Cube 仮配置）と一致するよう更新されている
- [ ] Milestone 7 タスクとして「EnvironmentObjectPlacer Prefab 化」が起票されている

完了条件（Milestone 6 内で対応する場合）:

- [ ] `EnvironmentObjectPlacer` に Prefab 差し替え口が追加されている
- [ ] SO 未設定時に Cube fallback で動くことを EditMode test が確認している
- [ ] `uloop.cmd compile --project-path Client` が成功している

対応区分: **ユーザー判断待ち**（第1回レビュー [C] と同一）

---

### 4. roadmap Milestone 7 送り記述と実装の矛盾

重大度: 中

問題:

`docs/roadmap/milestone6-roadmap.md` の「Milestone 7 へ移動する項目」に「4 方向 / 8 方向スプライト（Milestone 6 は左右反転のみ）」という記述が残っている。しかし Phase 3 では NE/NW/SE/SW の 4 方向スプライトが Milestone 6 内で実装済みであり、roadmap の記述が現行実装と矛盾している。

原因:

Phase 3 の task 設計時にアセット提供状況に合わせて task 完了条件が「4 方向スプライト」に更新されたが、roadmap の Milestone 7 送り記述が追従しなかった。

解決案:

`docs/roadmap/milestone6-roadmap.md` の Milestone 7 送り項目を更新する。

Before:
```
- 4 方向 / 8 方向スプライト（Milestone 6 は左右反転のみ）
```

After:
```
- 8 方向スプライト化（Milestone 6 では NE/NW/SE/SW 4 方向実装済み）
- 戦闘アニメーション（combat / hit / dead）
- 正式 Walk フレーム差し替え（現在は 2 フレームの仮実装）
```

根拠となるファイルリスト:

- `docs/roadmap/milestone6-roadmap.md`
- `tasks/task_0003.md`（4 方向スプライト実装済みの記録）

完了条件:

- [ ] `docs/roadmap/milestone6-roadmap.md` の Milestone 7 送り項目に「4 方向スプライトは Milestone 6 実装済み」という記述がない（矛盾が解消されている）
- [ ] 8 方向化・戦闘アニメーション等の次マイルストーン向け項目が明記されている

対応区分: **ドキュメント更新（コミット前に対応可）**（第1回レビュー [F] と同一。未対応）

---

## パフォーマンスレビュー

### 5. WorldMapView.EnqueueMissingLayerTiles() の毎フレーム全件ポーリング

重大度: 高

問題:

`WorldMapView.UpdateVisuals()` は毎フレーム `EnqueueMissingLayerTiles()` を呼んでおり、この内部で `viewDataProvider.GetLayers()` を全件イテレートしている。layer がすべて schedule 済みになった後も毎フレームの全件走査が継続する。  
`application-boundary-guidelines.md §17` が禁止する「Frame Loop で一度きりの差分検出を polling する」パターンに該当する。

```csharp
// WorldMapView.cs:41-44 — 毎フレーム呼ばれる
public void UpdateVisuals()
{
    EnqueueMissingLayerTiles(); // 全 layer を毎フレームスキャン
    BuildQueuedChunks(GameConstants.MapChunkBuildsPerFrame);
}

// WorldMapView.cs:68-79 — 全 layer を確認、schedule 済みなら skip するだけ
void EnqueueMissingLayerTiles()
{
    foreach (var layerData in viewDataProvider.GetLayers()) // 全件走査
    {
        if (scheduledLayerIds.Contains(layerData.LayerId.Value))
        {
            continue; // schedule 済みでも毎フレームここまで到達
        }
        ...
    }
}
```

なお `application-boundary-guidelines.md §17` のコード例「Before」は、このコードと構造が完全に一致している。

原因:

`WorldMapView` が「新しい layer が来たか」の差分検出を毎フレームの全件走査で行っている。layer 追加イベントや dirty flag を受け取る設計になっていないため、schedule が完了した後も全件チェックを継続せざるを得ない。

解決案（短期・イベント化）:

```csharp
// WorldMapView
public void NotifyLayerAdded(WorldMapLayerViewData layerData)
{
    if (scheduledLayerIds.Contains(layerData.LayerId.Value)) return;
    EnqueueLayerChunks(layerData);
    scheduledLayerIds.Add(layerData.LayerId.Value);
}

public void UpdateVisuals()
{
    BuildQueuedChunks(GameConstants.MapChunkBuildsPerFrame); // チャンク消化のみ
}
```

呼び出し元（`WorldPresenter` または `WorldLayerViewController`）が layer 追加時に `NotifyLayerAdded` を明示的に呼ぶ。

解決案（根治・revision 差分検知）:

`IWorldMapViewDataProvider` に `revision` または変更通知 Observable を追加し、`WorldMapView` が layer 増減をイベント駆動で検知する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `docs/guidelines/application-boundary-guidelines.md`（§17: Frame Loop で一度きりの差分検出を polling しない）

完了条件:

- [ ] `WorldMapView.UpdateVisuals()` が `viewDataProvider.GetLayers()` を呼ばない（毎フレームの全件走査が存在しない）
- [ ] layer 追加の差分検知が event / dirty flag / 明示 refresh のいずれかで実装されている
- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] uloop PlayMode 30 秒確認でマップが正常に描画されることを確認

対応区分: **要修正**（第1回・第2回レビューで新規発見）

---

### 6. WorldActorPresenter の毎フレームラムダ GC Alloc

重大度: 中

問題:

`WorldActorPresenter.UpdateVisuals()` で `actorViewRegistry.ForEachActorView()` に渡すラムダが、ローカル変数（`cameraYawDegrees`、`cameraYawChanged`、`walkFrame`）を毎フレームキャプチャする。ローカル変数をキャプチャするクロージャは毎回新しいオブジェクトをヒープに生成するため、GC Alloc が毎フレーム発生する。  
`application-boundary-guidelines.md §17` および `§16` が禁止する「ラムダをメソッド引数として毎フレーム生成」パターンに該当する。

```csharp
// WorldActorPresenter.cs:80-99 — cameraYawDegrees, cameraYawChanged, walkFrame をキャプチャ
actorViewRegistry.ForEachActorView((actorId, actorView) =>
{
    var isWalking = walkingActorsThisFrame.Contains(actorId);
    var direction = ComputeDirection(actorView.Facing, cameraYawDegrees);  // ローカル変数キャプチャ
    ...
    if (cameraYawChanged || isWalking)           // ローカル変数キャプチャ
    {
        actorView.SetRotationY(cameraYawDegrees); // ローカル変数キャプチャ
    }
});
```

原因:

`cameraYawDegrees`、`cameraYawChanged`、`walkFrame` がメソッドのローカル変数として宣言されているため、これらをキャプチャするラムダは毎フレームヒープにクロージャオブジェクトを生成する。フィールド変数のキャプチャであれば `this` 参照のみになるためヒープ生成は発生しない。

解決案:

`cameraYawDegrees`、`cameraYawChanged`、`walkFrame` をフィールドとして保持し、`UpdateVisuals()` 内で先にフィールドへ書き込んでからラムダを呼ぶ。これによりラムダが `this` のみをキャプチャする形になり、デリゲートをフィールドキャッシュすることで毎フレームのヒープ生成を完全に排除できる。

```csharp
// フィールドキャッシュ方式
readonly Action<Guid, ActorView> updateActorViewAction;

public WorldActorPresenter(...)
{
    updateActorViewAction = UpdateActorView;
}

float frameYawDegrees;
bool frameYawChanged;
int frameWalkIndex;

public void UpdateVisuals()
{
    frameYawDegrees = worldCameraController.CurrentYawDegrees;
    frameYawChanged = !hasLastCameraYawDegrees || !Mathf.Approximately(lastCameraYawDegrees, frameYawDegrees);
    frameWalkIndex = Mathf.FloorToInt(Time.unscaledTime * WalkFrameRate) % 2;
    // ...
    actorViewRegistry.ForEachActorView(updateActorViewAction); // キャッシュ済みデリゲートを使用
}

void UpdateActorView(Guid actorId, ActorView actorView)
{
    var isWalking = walkingActorsThisFrame.Contains(actorId);
    var direction = ComputeDirection(actorView.Facing, frameYawDegrees);
    // ...
}
```

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `docs/guidelines/application-boundary-guidelines.md`（§16: Frame Loop での LINQ / GC Alloc 禁止、§17: ラムダ毎フレーム生成禁止）

完了条件:

- [ ] `WorldActorPresenter.UpdateVisuals()` で `ForEachActorView` に渡すラムダがローカル変数（`cameraYawDegrees`、`cameraYawChanged`、`walkFrame`）を直接キャプチャしていない
- [ ] デリゲートがフィールドキャッシュまたはメソッド参照渡しにより毎フレームのヒープ生成を回避している
- [ ] `uloop.cmd compile --project-path Client` が成功している

対応区分: **要修正**（第2回レビューで新規発見）

---

## 重複レビュー

### 7. プレースホルダー定数・テクスチャ生成ロジックの重複

重大度: 中

問題:

以下の定数とテクスチャ生成メソッドが `VisualConfigLoader` と `ActorSpriteVisualConfig` の両方に存在している。

```csharp
// VisualConfigLoader.cs:14-17 と ActorSpriteVisualConfig.cs:13-16 に共通
const int SpriteWidth = 32;
const int SpriteHeight = 48;
const float PixelsPerUnit = 16f;
```

加えて `CreatePlaceholderTexture` / `CreatePlaceholderSprite` の実装（`new Texture2D` → `SetPixels` → `Apply`）が両クラスに重複している。スプライトサイズや PPU が変更された場合に両クラスを修正する必要がある。

原因:

Phase 1（VisualConfigLoader）と Phase 3（ActorSpriteVisualConfig）が独立して実装されたため、フェーズ間で共通化の機会が見逃された。

解決案:

`VisualConfigLoader` の定数と `CreatePlaceholderTexture` を `ActorSpriteVisualConfig` から参照するか、共通 internal static class（例: `SpritePlaceholderFactory`）に切り出す。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/VisualConfigLoader.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSpriteVisualConfig.cs`

完了条件:

- [ ] `SpriteWidth` / `SpriteHeight` / `PixelsPerUnit` 定数の定義がコードベースに 1 か所だけ存在する
- [ ] `CreatePlaceholderTexture` / `CreatePlaceholderSprite` の実装が重複していない
- [ ] `uloop.cmd compile --project-path Client` が成功している

対応区分: **推奨改善（次マイルストーン）**（第1回レビュー [D] と同一）

---

### 8. Destroy ヘルパーの重複

重大度: 低

問題:

`ActorPrefabSource.DestroyPrefabObject`（private static）と `WorldActorViewPool.DestroyActorObject`（private static）が同一の実装を持っている。  
いずれも `#if UNITY_EDITOR` 分岐で `Object.DestroyImmediate` / `Object.Destroy` を切り替えるパターンであり、全く同一のコードが 2 か所に存在する。

原因:

Phase 2 で `ActorPrefabSource` と `WorldActorViewPool` が独立して実装されたため、共通パターンの抽出が行われなかった。

解決案:

`ObjectDestroyHelper` のような internal static utility に切り出し、両クラスから参照する。あるいは一方から他方の helper を呼ぶ。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorPrefabSource.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorViewPool.cs`

完了条件:

- [ ] `#if UNITY_EDITOR` 分岐で `DestroyImmediate` / `Destroy` を切り替えるコードがコードベースに 1 か所のみ存在する
- [ ] `uloop.cmd compile --project-path Client` が成功している

対応区分: **推奨改善（次マイルストーン）**（第1回レビュー [E] と同一）

---

## 総合レビュー

### 9. INavigationPathProvider 内部バッファ契約の明文化

重大度: 低

問題:

`UnityNavMeshPathProvider.TryFindPath()` が内部 `List<GridPosition>`（`resultPath`）を `IReadOnlyList<GridPosition>` として返している。現状は `ActorPathState.SetPath()` が即座にディープコピーしているため実害なし。ただし Application 層インターフェースのドキュメントに「同一フレームで消費すること」という contract が doc comment のみで保証されており、将来の別実装・別呼び出しで誤用されやすい。

原因:

Application 境界インターフェースで「内部バッファを返す」という contract は doc comment のみによる合意であり、型システムで強制されない。

解決案（保守的）: `INavigationPathProvider.TryFindPath()` の doc comment に「戻り値は provider 内部の一時バッファ。呼び出し元は同一フレームで消費し、参照を保持しないこと」を明記する。  
解決案（根治）: `TryFindPath(..., List<GridPosition> results)` の形式に変更し、呼び出し元バッファへの書き込み形式にする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/INavigationPathProvider.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/UnityNavMeshPathProvider.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/ActorPathState.cs`

完了条件:

- [ ] `INavigationPathProvider.TryFindPath()` の doc comment に内部バッファ返却 contract と「同一フレーム消費」が明記されている（保守的対応の場合）
- [ ] または `TryFindPath` シグネチャが呼び出し元バッファへの書き込み形式に変更されている（根治の場合）

対応区分: **推奨改善（次マイルストーン）**（第1回レビュー [H] と同一）

---

### 10. Lighthouse 禁止 API・コーディングルール総合確認

重大度: -

以下を確認した。

| 確認項目 | 結果 |
|---|---|
| `Addressables.LoadAssetAsync` 直接呼び出し | なし（IAssetScope 経由のみ） |
| `Resources.Load` 利用 | `VisualAssetSetup.cs`（Editor のみ）に `Resources.FindObjectsOfTypeAll` あるが Editor スクリプトのため対象外 |
| `Camera.main` 利用 | なし |
| `Task` / `ValueTask` 利用（UniTask 以外） | なし |
| 旧 Input System 利用 | なし |
| `>` 比較演算子（coding-rules.md 11-1） | なし（`0f <=` のみ使用） |
| `SceneManager.LoadScene` 直接呼び出し | なし |
| DI 管理対象を LifetimeScope 以外で `new` | なし（`ActorPrefabSource` の fallback `new GameObject` は DI 管理対象ではない View レイヤー内部オブジェクト） |
| Runtime 側にテスト専用 constructor 追加 | なし |
| UseCase が `IDisposable` を実装 | なし |
| UseCase が長期状態を保持 | なし |

ハードゲート違反: **なし**

---

## 最終判定サマリ

| # | 観点 | 指摘タイトル | 重大度 | 対応区分 |
|---|---|---|---|---|
| 1 | 設計 | NavMesh 再生成後の更新が機能しない | 高 | **要修正**（Milestone 6 完了ゲート） |
| 2 | 設計 | UnityNavMeshPathProvider の DI グループ分類 | 低 | 推奨改善（次マイルストーン） |
| 3 | 整合性 | EnvironmentObjectPlacer の Prefab 化要件 | 中 | **ユーザー判断待ち** |
| 4 | 整合性 | roadmap Milestone 7 送り記述の矛盾 | 中 | ドキュメント更新（コミット前） |
| 5 | パフォーマンス | WorldMapView の毎フレーム全件ポーリング | 高 | **要修正**（第2回レビュー新規発見） |
| 6 | パフォーマンス | WorldActorPresenter の毎フレームラムダ GC Alloc | 中 | **要修正**（第2回レビュー新規発見） |
| 7 | 重複 | プレースホルダー定数・テクスチャ生成の重複 | 中 | 推奨改善（次マイルストーン） |
| 8 | 重複 | Destroy ヘルパーの重複 | 低 | 推奨改善（次マイルストーン） |
| 9 | 総合 | INavigationPathProvider 内部バッファ契約 | 低 | 推奨改善（次マイルストーン） |

**Milestone 6 完了判定**: 要修正項目 [1][5][6] の対応完了、[3] のユーザー判断後に完了とする。

- [1] は第1回レビュー（total）[A] と同一。未対応。
- [5][6] は第2回レビューで新規発見。いずれも `application-boundary-guidelines.md §16/§17` 違反。  
  [5] は §17 の Before 例と構造が完全一致しており、優先度が高い。

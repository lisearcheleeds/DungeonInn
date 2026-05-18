# Milestone 6 完了前統合レビュー 第2回

作成日: 2026-05-18  
レビュアー: Claude Code（設計・コード品質・整合性・役割重複・パフォーマンス）、Codex（同観点 + roadmap 整合性 + asset pipeline 検証）  
対象: Milestone 6 全差分（task_0001〜task_0006）  
前回レビュー: `docs/self-review/milestone6-completion-review-1-total.md`

## 前回レビューとの関係

第1回統合レビューの [A]〜[I] 項目のうち、本回レビューで新規根拠が追加されたもの、または対応区分に変更があったものを再掲する。  
それ以外は対応区分を引き継ぎ、重複記載を避ける。

---

## 差分許可モデル

Milestone 6 差分の production 契約変更分類を記録する。

| 変更 | 分類 | 判定 | 許可理由 |
|---|---|---|---|
| `INavigationPathProvider` 追加（Application 層 interface） | public interface 追加 | 許可 | View 層 NavMesh 実装を Application 境界から隔離する設計上必要。UnityEngine 非依存 |
| `VisualConfigLoader.LoadAsync(CancellationToken)` 追加 | public API 追加 | 許可 | Addressables ロードのエントリポイント。IDisposable で寿命管理済み |
| `ActorSpriteSet.GetSprite(...)` 追加 | public API 追加 | 許可 | View 層内部の表示 DTO。依存方向違反なし |
| `MapLayerViewRegistry.GetTileRoot(MapLayerId)` 追加 | public API 追加 | 許可 | `NavMeshBuildService` から tile root 参照に必要。View 層内部で完結 |
| `WorldMapView.IsTileBuildCompleted(MapLayerId)` 追加 | public API 追加 | 許可 | ゲームループから描画完了確認に必要な Query |
| `WorldMapView.scheduledLayerIds / completedLayerIds` 追加 | 長期状態追加 | 許可（ただし後述パフォーマンス指摘 [E] 参照） | チャンク分割ビルド進捗管理。View 層で完結 |
| `NavMeshBuildService.bakedLayerIds` 追加 | 長期状態追加 | 許可（ただし後述設計指摘 [A] 参照） | NavMesh 重複 bake 防止の進捗管理。View 層で完結 |
| `UnityNavMeshPathProvider` を `.As<INavigationPathProvider>()` で登録 | DI 登録変更 | 許可 | Application 境界インターフェースによる View 実装の隔離。正当なパターン |
| `VisualConfigLoader`, `ActorPrefabSource`, `NavMeshBuildService`, `EnvironmentObjectPlacer` 等の Scoped 登録追加 | DI 登録変更 | 許可 | View 層で WorldLifetimeScope 寿命に一致。IDisposable 実装が Scoped 解放と整合 |

不許可または要修正の差分:

- `MapMaterialSet.asset` の entries が空のまま「テクスチャマテリアル適用」完了条件を満たしているように見える差分（後述指摘 [G]）
- `DungeonInn Visual` Addressables group が schema なしで作成されており、Packed Mode build での動作保証がない差分（後述指摘 [H]）
- roadmap の animation 設計（`ActorSpriteAnimator` / `ActorSpriteAnimationClip`）と task_0003 の具体実装が同期されていない差分（後述指摘 [I]）

---

## 新規概念追加ゲート

以下の型が Milestone 6 で追加された。各型の許可理由を記録する。

| 型 | 類似既存概念 | 意味差分 | 統合・削除条件 |
|---|---|---|---|
| `INavigationPathProvider` | `IActorNavigationService` | Application 境界での外部経路探索能力の抽象。`IActorNavigationService` は Actor ごとの path state 管理を持つ | NavMesh 一本化で A* fallback 不要になった場合、または Composite pattern 導入時 |
| `UnityNavMeshPathProvider` | `AStarPathfinder` | Unity NavMesh API と座標変換のみを担当。Application の path state を持たない | NavMesh をゲームルールに使わない方針になった場合 |
| `NavMeshBuildService` | `MapMeshBuildService` | 生成済み Mesh から NavMesh を bake する。移動判断・Actor 状態を持たない | NavMesh 自動 bake API が導入された場合 |
| `VisualConfigLoader` | `MapMaterialSet`（旧来の fallback 管理） | Addressables load と asset lifetime を担当。`MapMaterialSet` / `ActorSpriteVisualConfig` はロード済み結果の解決のみ | Visual Config が共通 asset load service に統合された場合 |
| `ActorSpriteSet` | `ActorSpriteVisualConfig` | 1 BehaviorType 分の idle/walk スプライト配列 DTO。所有者は VisualConfigLoader | Actor / Item など Visual Config の共通 AssetSet 基底型導入時 |
| `ActorView` | 旧 `WorldActorView`（plain C# ラッパー） | MonoBehaviour + SpriteRenderer 操作を閉じ込める Prefab ルート。Inspector / Prefab 経由設定を可能にする | Actor animation が正式 `ActorSpriteAnimator` へ移行した場合 |
| `ActorPrefabSource` | なし（旧来は WorldActorViewPool が直接生成） | Prefab 所有権と fallback GameObject 生成の責務を分離。owned フラグで Dispose 時の Destroy 対象を管理 | 共通 AssetPrefabSource 基底型が導入された場合 |
| `EnvironmentObjectPlacer` | `MapMeshBuildService` | Prop GameObject の配置と Dispose 管理。Mesh Buffer 管理とは別責務 | Prop 管理の共通 pool 基盤導入時 |

---

## 要修正項目

### [A] NavMesh 再生成後の更新が機能しない

重大度: 高  
指摘者: Codex（第1回）、Claude Code（第2回）

問題:

roadmap Phase 5 完了条件「ダンジョン再生成後も NavMesh が正しく更新される」が未達。  
`NavMeshBuildService.bakedLayerIds` に layer id が記録されると以降の bake をスキップし、`WorldMapView.scheduledLayerIds` / `completedLayerIds` も永続するため、同じ layer id でダンジョンフロアが再生成された場合に chunk mesh も NavMesh も再構築されない。

原因:

初回ビルド完了を記録する状態管理になっており、layer の再生成（revision 変化）イベントを受け取る invalidate 経路が存在しない。

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

解決案（根治）: `WorldMapLayerViewData` に revision を持たせ、`layerId + revision` で build 要否を判断する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/NavMeshBuildService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapLayerViewRegistry.cs`
- `docs/roadmap/milestone6-roadmap.md`（Phase 5 完了条件）

完了条件:

- [ ] `NavMeshBuildService.InvalidateLayer(MapLayerId)` が `bakedLayerIds` から対象 id を除去する
- [ ] `WorldMapView.InvalidateLayer(MapLayerId)` が `scheduledLayerIds` / `completedLayerIds` / `remainingChunkCountsByLayer` から除去し layer root を破棄する
- [ ] 再生成イベント発生時に上記 invalidate が呼ばれる経路が存在する
- [ ] uloop PlayMode 30 秒確認でダンジョン再生成後にマップが再描画されることを確認
- [ ] `uloop.cmd compile --project-path Client` が成功している

対応区分: **要修正**（Milestone 6 完了ゲート）

---

### [B] ログ・一時ファイルのステージ除外

重大度: 高  
指摘者: Codex（第1回）

問題:

以下のファイルがステージされており、Milestone 6 の成果物コミットに含めるべきでない。  
`claude-codex-communication.log`（作業監視ログ）、`codex_err_0001.tmp`、`codex_out_0001.tmp`、`codex_stdin_0001.tmp`、`codex_stdin_0002.tmp`

原因: コミット前の unstage 手順が徹底されていない。`.gitignore` に `codex_*.tmp` の除外エントリがない。

解決案: コミット前に上記ファイルを unstage する。`codex_*.tmp` を `.gitignore` に追加する。`claude-codex-communication.log` のコミット運用ルールを AGENTS.md に明記する。

根拠となるファイルリスト:

- `.gitignore`
- `AGENTS.md`

完了条件:

- [ ] `claude-codex-communication.log`、`codex_*.tmp` が git ステージから除外されている
- [ ] `.gitignore` に `codex_*.tmp` が追加されている
- [ ] `git status --short` にログ・一時ファイルが含まれていない

対応区分: **要修正**（コミット前に対応）

---

### [E] WorldMapView.EnqueueMissingLayerTiles() の毎フレーム全件ポーリング

重大度: 高  
指摘者: Claude Code（第2回）

問題:

`WorldMapView.UpdateVisuals()` は毎フレーム `EnqueueMissingLayerTiles()` を呼び、`viewDataProvider.GetLayers()` を全件イテレートする。layer がすべて schedule 済みになった後も毎フレームの全件走査が継続する。  
`application-boundary-guidelines.md §17` の「Frame Loop で一度きりの差分検出を polling しない」に該当する。同 §17 の "Before" コード例と構造が完全一致する。

```csharp
// WorldMapView.cs — 毎フレーム全 layer を確認、schedule 済みなら skip するだけ
void EnqueueMissingLayerTiles()
{
    foreach (var layerData in viewDataProvider.GetLayers()) // 毎フレーム全件走査
    {
        if (scheduledLayerIds.Contains(layerData.LayerId.Value))
            continue; // schedule 済みでも毎フレームここまで到達
        EnqueueLayerChunks(layerData);
        scheduledLayerIds.Add(layerData.LayerId.Value);
    }
}
```

原因:

layer 追加イベントや dirty flag を受け取る設計がなく、「新しい layer が来たか」の差分検出を毎フレームの全件走査で行っている。

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

解決案（根治）: `IWorldMapViewDataProvider` に layer 増減の変更通知 Observable を追加し、イベント駆動で検知する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `docs/guidelines/application-boundary-guidelines.md`（§17）

完了条件:

- [ ] `WorldMapView.UpdateVisuals()` が `viewDataProvider.GetLayers()` を呼ばない
- [ ] layer 追加の差分検知が event / dirty flag / 明示 refresh のいずれかで実装されている
- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] uloop PlayMode 30 秒確認でマップが正常に描画されることを確認

対応区分: **要修正**（Milestone 6 完了ゲート）

---

### [F] WorldActorPresenter の毎フレームラムダ GC Alloc

重大度: 中  
指摘者: Claude Code（第2回）

問題:

`WorldActorPresenter.UpdateVisuals()` 内で `actorViewRegistry.ForEachActorView()` に渡すラムダが、ローカル変数（`cameraYawDegrees`、`cameraYawChanged`、`walkFrame`）を毎フレームキャプチャする。ローカル変数キャプチャのクロージャは毎回ヒープに生成される。  
`application-boundary-guidelines.md §16/§17` の「ラムダをメソッド引数として毎フレーム生成しない」に該当する。

原因:

`cameraYawDegrees`、`cameraYawChanged`、`walkFrame` がメソッドのローカル変数として宣言されているため、これらをキャプチャするラムダは毎フレームクロージャオブジェクトをヒープに生成する。

解決案:

```csharp
// フィールドキャッシュ方式
readonly Action<Guid, ActorView> updateActorViewAction;

public WorldActorPresenter(...) {
    updateActorViewAction = UpdateActorView; // 一度だけ確保
}

float frameYawDegrees; bool frameYawChanged; int frameWalkIndex;

public void UpdateVisuals()
{
    frameYawDegrees = worldCameraController.CurrentYawDegrees;
    frameYawChanged = !hasLastCameraYawDegrees || !Mathf.Approximately(lastCameraYawDegrees, frameYawDegrees);
    frameWalkIndex = Mathf.FloorToInt(Time.unscaledTime * WalkFrameRate) % 2;
    // ...
    actorViewRegistry.ForEachActorView(updateActorViewAction); // キャッシュ済みデリゲートを使用
}

void UpdateActorView(Guid actorId, ActorView actorView) { ... }
```

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `docs/guidelines/application-boundary-guidelines.md`（§16、§17）

完了条件:

- [ ] `ForEachActorView` に渡すラムダがローカル変数（`cameraYawDegrees`、`cameraYawChanged`、`walkFrame`）を直接キャプチャしていない
- [ ] デリゲートがフィールドキャッシュまたはメソッド参照渡しにより毎フレームのヒープ生成を回避している
- [ ] `uloop.cmd compile --project-path Client` が成功している

対応区分: **要修正**（Milestone 6 完了ゲート）

---

### [G] Map texture assets が Addressables / MapMaterialSetSO に接続されていない

重大度: 高  
指摘者: Codex（第2回）

問題:

ステージ済み差分に `Client/Assets/DungeonInn/Runtime/Art/Textures/Map/*.png`（DungeonBlocked、DungeonWalkable、GroundBlocked、GroundWalkable、StairDown、StairUp）が含まれているが、以下の理由で roadmap Phase 1/4 の完了条件「テクスチャ付き Material を差し替えると見た目が変わる」が満たされていない。

- `Client/Assets/DungeonInn/Runtime/StaticResources/Visual/MapMaterialSet.asset` の `entries: []`（空）
- `Client/Assets/AddressableAssetsData/AssetGroups/DungeonInn Visual.asset` に map material の address エントリが存在しない
- `VisualAssetSetup.EnsureAddressablesGroup()` が sprite（Adventurer / Goblin）のみを登録しており、map material の登録コードが存在しない
- `VisualAssetSetup.CreateOrLoadMapMaterialSO()` のコメントに「Addresses will be filled when Map Material assets are created in Phase 4」とあるが、対応するコードが実装されていない

現状は `MapMeshBuildService` が `mapMaterialSet.Get(visualDefinition.Kind)` を呼んでも `VisualConfigLoader.GetMaterial(kind)` が何も返せず、`MapMaterialSet` の fallback debug material（単色）でのみ描画される。

原因:

Phase 4 のタスクで Map 用 Material asset の作成と Addressables 登録が実装されなかった。`VisualAssetSetup` に map material の作成・登録コードが追加されておらず、`MapMaterialSetSO` の entries が空のまま残っている。

解決案（暫定）:

- `TileVisualKind` ごとに Material asset（`.mat`）を作成し、`MapMaterialSet.asset` の entries に追加する
- `VisualAssetSetup.EnsureAddressablesGroup()` に map material の Addressables 登録処理を追加する

解決案（根治）:

- `VisualAssetSetup` に map texture PNG を Material asset に変換し、`TileVisualKind` のアドレスで Addressables に登録する処理を追加する
- `MapMaterialSetSO` の entries が空の場合に warning を出し、PlayMode で誤判定しないようにする

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/StaticResources/Visual/MapMaterialSet.asset`
- `Client/Assets/AddressableAssetsData/AssetGroups/DungeonInn Visual.asset`
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/VisualConfigLoader.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMaterialSet.cs`
- `docs/roadmap/milestone6-roadmap.md`（Phase 1/4 完了条件）

完了条件:

- [ ] `MapMaterialSet.asset` の `entries` に全 `TileVisualKind` の Material address が登録されている
- [ ] `DungeonInn Visual.asset` に map material の address エントリが存在する
- [ ] `VisualConfigLoader.LoadMaterialsAsync()` が少なくとも 1 件以上の Material をロードできる
- [ ] `MapMeshBuildService.BuildChunk()` が fallback debug material ではなく Addressables 由来 Material を使う PlayMode 証跡がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

対応区分: **要修正**（Milestone 6 完了ゲート）

---

## ユーザー判断待ち項目

### [C] EnvironmentObjectPlacer の Prefab 化要件

重大度: 中  
指摘者: Codex（第1回）  
**ユーザー判断: Milestone 6 内で対応（2026-05-18）**

問題:

roadmap Phase 4「プロップ Prefab が tile 位置に配置される」「初期実装は柱・松明・木などの小型プロップを想定する（3D モデル Prefab）」に対し、実装は `GameObject.CreatePrimitive(PrimitiveType.Cube)` による Cube 直生成で Prefab 差し替え口が存在しない。

解決案:

`EnvironmentObjectPlacer` に Prefab 差し替え口を追加する。Prefab 未設定時は既存の Cube 生成を fallback として維持する。

```csharp
public sealed class EnvironmentObjectPlacer : IDisposable
{
    readonly GameObject propPrefab; // null 許容。DI または設定経由で渡す

    void PlacePropAt(Transform parent, int gridX, int gridZ)
    {
        var propObject = propPrefab != null
            ? Object.Instantiate(propPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);
        // 以降は既存の配置処理
    }
}
```

`propPrefab` の注入経路は `VisualConfigSettings` 経由（`ActorSpriteVisualConfigSO` のように SO にフィールドを追加する）か、`WorldLifetimeScope` で `EnvironmentObjectPlacer` の constructor に渡す方式を選択する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/EnvironmentObjectPlacer.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/VisualConfigSettings.cs`
- `docs/roadmap/milestone6-roadmap.md`（Phase 4 完了条件）
- `tasks/task_0004.md`

完了条件:

- [ ] `EnvironmentObjectPlacer` に Prefab 差し替え口（nullable フィールド）が追加されている
- [ ] Prefab 未設定時に Cube fallback で動作し、PlayMode でエラーが出ない
- [ ] SO または DI 経由で Prefab を設定できる経路が存在する
- [ ] `uloop.cmd compile --project-path Client` が成功している

対応区分: **要修正**（Milestone 6 完了ゲート）

---

### [I] Actor animation の roadmap 契約と実装契約が同期されていない

重大度: 中  
指摘者: Codex（第2回）  
**ユーザー判断: roadmap 基準で対応（2026-05-18）**

問題:

roadmap Phase 3 は `ActorSpriteAnimator`（MonoBehaviour または plain class）、`ActorAnimationState`（enum）、`ActorSpriteAnimationClip`（ScriptableObject。フレーム配列・FPS・ループ設定）を作る計画であり、完了条件に「フレーム配列・FPS を差し替えると別のアニメーションが再生できる」を持つ。  
現行実装は `WorldActorPresenter.WalkFrameRate = 4f`（定数）と `ActorSpriteSet` の固定配列を使っており、animation 設定の Inspector 差し替えができない。

解決案:

- `ActorAnimationState` enum を追加する（`Idle` / `Walk`。Milestone 7 以降: `Combat` / `Hit` / `Dead`）
- `ActorSpriteAnimationClip` ScriptableObject を追加し、フレーム配列・FPS・ループ設定を保持する
- `ActorSpriteAnimator`（plain class または MonoBehaviour）を追加し、フレーム切り替えロジックを `WorldActorPresenter` から分離する
- `WorldActorPresenter` は移動/静止の状態判定だけを行い、`SetAnimationState(ActorAnimationState)` を呼ぶ形に変更する
- `WalkFrameRate` 定数を `ActorSpriteAnimationClip` 側の FPS フィールドに移動する

新規概念追加ゲート（`ActorSpriteAnimationClip`）:

- 既存の類似概念: `ActorSpriteSet`
- 意味差分: `ActorSpriteSet` は方向×フレームのスプライト配列を保持するロード済み DTO。`ActorSpriteAnimationClip` は FPS・ループ設定を含む animation 定義で、ScriptableObject として Inspector 差し替えが可能
- 代替しなかった理由: `ActorSpriteSet` に FPS・ループ設定を追加すると Addressables ロード済みアセット DTO と animation 設定の責務が混在する
- 統合・削除条件: Unity Animator または外部 animation ライブラリに移行した場合

根拠となるファイルリスト:

- `docs/roadmap/milestone6-roadmap.md`（Phase 3 作るもの、完了条件）
- `tasks/task_0003.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSpriteSet.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorView.cs`

完了条件:

- [ ] `ActorAnimationState` enum が追加されている（`Idle` / `Walk`）
- [ ] `ActorSpriteAnimationClip` ScriptableObject が追加され、フレーム配列・FPS・ループ設定を Inspector で変更できる
- [ ] `ActorSpriteAnimator`（または同等のクラス）が追加され、`WorldActorPresenter` から animation frame 管理責務が分離されている
- [ ] `WorldActorPresenter` に `WalkFrameRate` 定数が残っていない
- [ ] Actor が静止中は idle フレーム、移動中は walk フレームが再生される PlayMode 確認がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

対応区分: **要修正**（Milestone 6 完了ゲート）

---

## ドキュメント更新項目（コミット前）

### [Q] lifetime-scope-game-loop-design.md の WorldLifetimeScope セクション未更新

重大度: 低  
指摘者: Claude Code（第2回）

問題:

roadmap Phase 6 完了条件「`lifetime-scope-game-loop-design.md` に分割方針が追記されている」に対し、同ファイルの "World LifetimeScope に置くもの" セクションが Milestone 5 以前の古い登録リストのままになっている。Milestone 6 で追加した `VisualConfigLoader` / `ActorPrefabSource` / `NavMeshBuildService` / `UnityNavMeshPathProvider` / `EnvironmentObjectPlacer` 等と、`WorldLifetimeScope.cs` に実装済みの12グループ構成が未反映。

原因:

Phase 6 タスク（task_0006）で `WorldLifetimeScope.cs` のコメント整理は完了したが、対応する設計ドキュメントの更新が staged 差分に含まれていない。

解決案:

`docs/design/lifetime-scope-game-loop-design.md` の "World LifetimeScope に置くもの" セクションを現行の12グループ構成に合わせて更新する。グループ定義（View: シーン基盤 / View: マップ描画 / View: アクター描画 / Application: イベント・アクター状態 / Application: ナビゲーション・空間 / 等）と Milestone 6 追加クラスを反映する。

根拠となるファイルリスト:

- `docs/design/lifetime-scope-game-loop-design.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`
- `docs/roadmap/milestone6-roadmap.md`（Phase 6 完了条件）

完了条件:

- [ ] `docs/design/lifetime-scope-game-loop-design.md` の "World LifetimeScope に置くもの" セクションに Milestone 6 追加クラスが記載されている
- [ ] `WorldLifetimeScope.cs` の12グループ構成がドキュメントに反映されている

対応区分: **ドキュメント更新（コミット前）**

---

### [D] roadmap Milestone 7 送り記述の矛盾

重大度: 中  
指摘者: Codex（第1回）

問題:

`docs/roadmap/milestone6-roadmap.md` の「Milestone 7 へ移動する項目」に「4 方向 / 8 方向スプライト（Milestone 6 は左右反転のみ）」が残っているが、Phase 3 実装では NE/NW/SE/SW 4 方向スプライトが Milestone 6 内で実装済み。

解決案:

「Milestone 7 へ移動する項目」の記述を以下に更新する。

```
- 4 方向スプライトは Milestone 6 で実装済み
- 8 方向スプライト化（Milestone 7 以降）
- 戦闘アニメーション（combat / hit / dead）
- 正式 Walk フレーム差し替え（現在は 2 フレームの仮実装）
```

根拠となるファイルリスト:

- `docs/roadmap/milestone6-roadmap.md`
- `tasks/task_0003.md`

完了条件:

- [ ] `docs/roadmap/milestone6-roadmap.md` に「Milestone 6 は左右反転のみ」という記述がない
- [ ] 8 方向化・戦闘アニメーション等の次マイルストーン向け項目が明記されている

対応区分: **ドキュメント更新（コミット前）**

---

## 推奨改善項目（次マイルストーン以降）

### [H] Addressables group schema が未設定

重大度: 中  
指摘者: Codex（第2回）

問題:

`DungeonInn Visual.asset` の `m_SchemaSet: m_Schemas: []`（空）。`VisualAssetSetup.EnsureAddressablesGroup()` が `settings.CreateGroup(..., null)` を呼んでおり BundledAssetGroupSchema / ContentUpdateGroupSchema が設定されていない。  
**Editor PlayMode（FastMode）では schemas 不要のため現在の PlayMode テストは正常に通る**が、Packed Mode build（実際の配信ビルド）では Addressables group の build / load path 設定がないため asset load が成立しない。

原因:

`settings.CreateGroup(AddressablesGroupName, false, false, false, null)` の第5引数（`schemasToCopy`）が `null`。既存の `Packed Assets.asset` テンプレートが使われていない。

解決案（暫定）: `DungeonInn Visual` group に Editor 上で BundledAssetGroupSchema / ContentUpdateGroupSchema を手動追加する。  
解決案（根治）: `VisualAssetSetup.EnsureAddressablesGroup()` が `Packed Assets.asset` テンプレートを schemasToCopy として渡すよう修正する。schema が空の group を validation で検出できるようにする。

根拠となるファイルリスト:

- `Client/Assets/AddressableAssetsData/AssetGroups/DungeonInn Visual.asset`
- `Client/Assets/AddressableAssetsData/AssetGroupTemplates/Packed Assets.asset`
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`

完了条件:

- [ ] `DungeonInn Visual.asset` の `m_SchemaSet.m_Schemas` が空でない
- [ ] BundledAssetGroupSchema の build / load path が Local profile に接続されている
- [ ] `VisualAssetSetup.EnsureAddressablesGroup()` が schema なし group を生成しない

対応区分: **推奨改善（次マイルストーン。Packed Mode build 前までに対応）**

---

### [J] Milestone 6 中核 View 挙動のテスト証跡が不足

重大度: 中  
指摘者: Codex（第2回）

問題:

EditMode test 245 件は成功しているが、今回の中核差分の以下の挙動を直接検証するテストが存在しない。

- `VisualConfigLoader` が Addressables から sprite / material をロードする
- `MapMaterialSetSO.entries` と Addressables group の address が一致する
- `MapMeshBuildService` が Block / Ramp geometry を期待頂点数・submesh 数で生成する
- `WorldMapView` が chunk build 完了後に NavMesh bake を呼ぶ
- ダンジョンフロア再生成時に chunk / NavMesh が rebuild される
- `WorldActorPresenter` が camera yaw と facing から方向 sprite を選択する

compile / tests が成功していても、今回発見した Map texture 未接続・Addressables group schema 欠落・NavMesh 再生成未対応を既存テストでは検出できない。

原因:

既存テストは Application orchestration と view data snapshot の検証が中心で、Unity View asset / Editor setup / Addressables config の検証が不足している。

解決案（短期）:

- `MapMaterialSet.asset` の entries が空でないことを検証する EditMode test を追加する
- `MapMeshBuildService` の Plane / Block / Ramp geometry を頂点数・submesh 数で検証する EditMode test を追加する

解決案（根治）:

- `VisualAssetSetup` が生成する asset を検証する Editor test を追加する
- Milestone 完了レビューの必須証跡に「asset 設定整合 test」または「Addressables load smoke test」を追加する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Tests/EditMode/`
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMeshBuildService.cs`
- `docs/guidelines/self-review-preset.md`

完了条件:

- [ ] `MapMaterialSet.asset` の entries が空でないことを検証するテストがある
- [ ] Addressables group に sprite / map material address があることを検証するテストがある
- [ ] `MapMeshBuildService` の Plane / Block / Ramp の geometry を検証するテストがある
- [ ] NavMesh bake trigger の検証がある
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している

対応区分: **推奨改善（次マイルストーン）**

---

### [K] Editor 自動セットアップが domain reload ごとに asset 設定へ副作用を持つ

重大度: 低  
指摘者: Codex（第2回）

問題:

`VisualAssetSetup.AutoSetup()` は `[InitializeOnLoadMethod]` で実行され、asset 存在確認が通れば毎回 `SetupAddressables()` を呼ぶ。`SetupAddressables()` は最後に `AssetDatabase.SaveAssets()` を実行するため、domain reload のたびに asset database への書き込みが発生する。

原因:

初期導入の利便性を優先して domain reload 自動実行と明示的 menu 実行が同じ `SetupAddressables()` を呼んでいる。自動実行時に「差分が必要か」を判定する guard が不足している。

解決案（短期）: `AutoSetup()` は asset 不足時だけ `RunSetup()` を呼び、`SetupAddressables()` は menu か明示実行のみに制限する。  
解決案（根治）: `Validate Visual Assets`（副作用なし）と `Setup Visual Assets`（menu のみ）を分離する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `Client/Assets/AddressableAssetsData/AddressableAssetSettings.asset`

完了条件:

- [ ] domain reload だけでは `AddressableAssetsData` に差分が出ない
- [ ] `VisualAssetSetup` に副作用なし validation 経路がある
- [ ] setup が必要な場合だけ `AssetDatabase.SaveAssets()` が呼ばれる

対応区分: **推奨改善（次マイルストーン）**

---

### [L] プレースホルダー定数・テクスチャ生成ロジックの重複

重大度: 中  
指摘者: Claude Code（第1回・第2回）

`VisualConfigLoader.cs` と `ActorSpriteVisualConfig.cs` の両方に `SpriteWidth=32`、`SpriteHeight=48`、`PixelsPerUnit=16f` と `CreatePlaceholderTexture` の実装が重複している。

対応区分: **推奨改善（次マイルストーン）**

---

### [M] UnityNavMeshPathProvider の DI グループ分類

重大度: 低  
指摘者: Claude Code（第1回・第2回）

`WorldLifetimeScope.cs` で View 層クラス `UnityNavMeshPathProvider` が `// === Application: ナビゲーション / 空間 ===` グループに含まれている。`// === View: マップ描画 ===` に移動すべき。

対応区分: **推奨改善（次マイルストーン）**

---

### [N] INavigationPathProvider 内部バッファ契約の明文化

重大度: 低  
指摘者: Codex（第1回）

`UnityNavMeshPathProvider.TryFindPath()` が内部 `List<GridPosition>` を `IReadOnlyList` として返す。現状は `ActorPathState.SetPath()` が即座にディープコピーするため実害なし。インターフェースの doc comment だけが契約。

対応区分: **推奨改善（次マイルストーン）**

---

### [O] Destroy ヘルパーの重複

重大度: 低  
指摘者: Claude Code（第1回・第2回）

`ActorPrefabSource.DestroyPrefabObject` と `WorldActorViewPool.DestroyActorObject` が同一の `#if UNITY_EDITOR` 分岐実装を持つ。

対応区分: **推奨改善（次マイルストーン）**

---

### [P] trailing whitespace（手書き markdown）

重大度: 低  
指摘者: Codex（第1回）

`tasks/task_0003.md`、`tasks/task_0005.md` に trailing whitespace がある。ログファイルを unstage すれば検出量は大幅に減る。

対応区分: **コミット前に対応可**

---

## 最終判定サマリ

| # | 項目 | 重大度 | 指摘者 | 対応区分 |
|---|---|---|---|---|
| A | NavMesh 再生成後の更新が機能しない | 高 | Codex 第1回 / Claude Code 第2回 | **要修正**（完了ゲート） |
| B | ログ・一時ファイルのステージ除外 | 高 | Codex 第1回 | **要修正**（コミット前） |
| C | EnvironmentObjectPlacer の Prefab 化 | 中 | Codex 第1回 | **要修正**（完了ゲート） |
| D | roadmap Milestone 7 送り記述の矛盾 | 中 | Codex 第1回 | ドキュメント更新（コミット前） |
| Q | lifetime-scope-game-loop-design.md の WorldLifetimeScope セクション未更新 | 低 | Claude Code 第2回 | ドキュメント更新（コミット前） |
| E | WorldMapView 毎フレーム全件ポーリング | 高 | Claude Code 第2回 | **要修正**（完了ゲート） |
| F | WorldActorPresenter 毎フレームラムダ GC Alloc | 中 | Claude Code 第2回 | **要修正**（完了ゲート） |
| G | Map texture が Addressables に未接続 | 高 | Codex 第2回 | **要修正**（完了ゲート） |
| H | Addressables group schema 未設定 | 中 | Codex 第2回 | 推奨改善（Packed Mode build 前） |
| I | Actor animation の roadmap・task 契約齟齬 | 中 | Codex 第2回 | **要修正**（完了ゲート） |
| J | 中核 View 挙動のテスト証跡不足 | 中 | Codex 第2回 | 推奨改善（次マイルストーン） |
| K | Editor AutoSetup の domain reload 副作用 | 低 | Codex 第2回 | 推奨改善（次マイルストーン） |
| L | プレースホルダー定数・ロジックの重複 | 中 | Claude Code 第1回・第2回 | 推奨改善（次マイルストーン） |
| M | UnityNavMeshPathProvider の DI グループ分類 | 低 | Claude Code 第1回・第2回 | 推奨改善（次マイルストーン） |
| N | INavigationPathProvider 内部バッファ契約 | 低 | Codex 第1回 | 推奨改善（次マイルストーン） |
| O | Destroy ヘルパーの重複 | 低 | Claude Code 第1回・第2回 | 推奨改善（次マイルストーン） |
| P | trailing whitespace（手書き markdown） | 低 | Codex 第1回 | コミット前に対応可 |

**ハードゲート・ガイドライン違反**: なし  
**コンパイル**: ErrorCount 0（Codex 第2回 uloop compile 確認済み）  
**EditMode テスト**: 245 件 pass（Codex 第2回 uloop run-tests 確認済み）

**Milestone 6 完了判定**: 要修正 [A][B][C][E][F][G][I] の対応完了、およびドキュメント更新 [D][Q] の完了後に完了とする。

### 完了ゲートチェックリスト（コミット前）

**コード対応（要修正）**

- [ ] [A] `NavMeshBuildService.InvalidateLayer` / `WorldMapView.InvalidateLayer` が実装され、ダンジョン再生成後にマップと NavMesh が再構築される
- [ ] [B] ログ・一時ファイルが unstage されている。`.gitignore` に `codex_*.tmp` が追加されている
- [ ] [C] `EnvironmentObjectPlacer` に Prefab 差し替え口が追加され、Prefab 未設定時に Cube fallback で動作する
- [ ] [E] `WorldMapView.UpdateVisuals()` が `viewDataProvider.GetLayers()` を毎フレーム全件走査しない（layer 追加が event / dirty flag / 明示 refresh で検知される）
- [ ] [F] `WorldActorPresenter` の `ForEachActorView` に渡すデリゲートがフィールドキャッシュ済みで、毎フレームのヒープ生成が発生しない
- [ ] [G] `MapMaterialSet.asset` の entries が空でなく、Addressables group に map material address が存在し、マップが fallback 単色ではなくテクスチャで描画される
- [ ] [I] `ActorAnimationState` / `ActorSpriteAnimationClip` / `ActorSpriteAnimator` が追加され、`WorldActorPresenter` に `WalkFrameRate` 定数が残っていない

**ドキュメント更新**

- [ ] [D] `docs/roadmap/milestone6-roadmap.md` の Milestone 7 送り記述が「4 方向スプライトは Milestone 6 実装済み」に更新されている
- [ ] [Q] `docs/design/lifetime-scope-game-loop-design.md` の WorldLifetimeScope セクションに Milestone 6 追加クラスと12グループ構成が反映されている

**動作確認**

- [ ] `uloop.cmd compile --project-path Client` が成功している（ErrorCount: 0）
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している（全件 pass）
- [ ] uloop PlayMode 30 秒確認で `[World] GameWorldState initialized` ログが出力され、エラーログがない

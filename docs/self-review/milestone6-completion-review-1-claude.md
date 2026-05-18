# Milestone 6 完了前レビュー 第1回 (Claude Code)

作成日: 2026-05-18

## 目的

Milestone 6 の全 6 フェーズ（task_0001〜task_0006）のステージング差分を対象に、以下の 4 観点でレビューする。

- 設計レビュー
- コード品質レビュー
- 整合性レビュー
- 役割の重複

---

## Phase 1: Visual Config の ScriptableObject 化 + Addressables 対応

### 設計レビュー ✅

`VisualConfigSettings`（plain class）→ `VisualConfigLoader`（IDisposable）→ `IAssetScope` の責務分離が明確。P5パターン（`assetManager.CreateScope()` + `scope.LoadAsync<T>()`）を正しく使用しており、`Addressables.LoadAssetAsync` 直接呼び出しなし。SO が null の場合に fallback で動く設計も正確に実装されている。

### コード品質レビュー ✅

- `OperationCanceledException` の再スロー済み
- `IAssetScope.Dispose()` でロード済みアセットが解放され、fallback Texture/Sprite は `VisualConfigLoader.Dispose()` 内で `Object.Destroy` している。二重管理なし
- fallback sprite/texture 生成は `VisualConfigLoader` 内に限定

### 整合性レビュー ✅

Phase 1 task では `Dictionary<ActorBehaviorType, Sprite> loadedSprites` / `GetSprite()` と設計されていたが、Phase 3 で `ActorSpriteSet` 方式に変更されており、task_0003 に変更記録がある。最終コードは Phase 3 設計に一致。フェーズ間の整合性は保たれている。

### 役割の重複 ⚠️ 中

`VisualConfigLoader.cs:14-17` と `ActorSpriteVisualConfig.cs:13-16` の両方に以下の同一定数が存在する：

```csharp
const int SpriteWidth = 32;
const int SpriteHeight = 48;
const float PixelsPerUnit = 16f;
```

さらに `CreatePlaceholderTexture` の実装（`new Texture2D` → `SetPixels` → `Apply`）も重複している。詳細は後述の横断的指摘に記載。

---

## Phase 2: Actor MonoBehaviour Prefab 化

### 設計レビュー ✅

`ActorPrefabSource` が `owned` フラグで Prefab 所有権を管理し、`WorldActorViewPool` が `Object.Instantiate` のみを担当するという責務の分離が正確。fallback Prefab の生成が `ActorPrefabSource` に限定されている。

### コード品質レビュー ✅

- `ActorView.SetSprite()` / `SetFlip()` に `??=` lazy init が追加されており、EditMode テストで `Awake()` が呼ばれない場合にも対応している（task_0002 レビューログに記録あり）
- fallback prefab のレイヤー設定（`WorldRenderingLayer.Layer`）が正確

### 整合性レビュー ✅

task_0002 で定義された `Reset(Sprite sprite)` シグネチャは Phase 3 で `Reset()` に変更。変更がタスクドキュメントに記録されている。

### 役割の重複 ⚠️ 低

`ActorPrefabSource.DestroyPrefabObject` と `WorldActorViewPool.DestroyActorObject` が同一実装（`#if UNITY_EDITOR` 分岐で `DestroyImmediate` / `Destroy` を切り替えるパターン）。どちらも private static で影響範囲は限定的。

---

## Phase 3: Actor スプライトアニメーション

### 設計レビュー ✅

`ComputeDirection` のカメラ Yaw を考慮した方向計算（右積・前積のドット積）が正確。比較演算子は `0f <=` のみ使用（coding-rules.md 11-1 準拠）。`ActorSpriteSet.GetSprite` の fallback チェーン（walk → idle → FallbackSprite）が null 安全。

### コード品質レビュー ✅

- `walkFrame = Mathf.FloorToInt(currentTime * WalkFrameRate) % 2` の正確な実装
- `cameraYawChanged || isWalking` の条件で `SetRotationY` を間引いており、不要な呼び出しを削減している
- `walkingActorsThisFrame` が `HashSet<Guid>` で O(1) ルックアップ

### 整合性レビュー ✅

task_0003 の設計（24 スプライトのアドレス命名規則 `{prefix}/Idle{dir}` / `{prefix}/Walk{dir}{frame}`）と `VisualConfigLoader.LoadIdleSpriteAsync` / `LoadWalkSpriteAsync` の実装が一致。`VisualAssetSetup` での 24 エントリ登録も確認できる。

### 役割の重複 ⚠️ 中

Phase 1 指摘と同一。`VisualConfigLoader.CreatePlaceholderSprite` と `ActorSpriteVisualConfig.Add` で同じプレースホルダー生成コードが存在する。

---

## Phase 4: Map 3D トポロジーと背景テクスチャ

### 設計レビュー ✅

`AddQuad` ヘルパーで Block（5面）/ Ramp / Plane を統一し、`mapMaterialSet.Get(visualDefinition.Kind)` を `triangles.Count == 0` の条件で呼ぶことで「マテリアルが Addressables ロード前に bake される」タイミング問題を正確に解消している。

### コード品質レビュー ✅

- Block の各面ワインディングが設計通り（法線が外向き）
- `EnvironmentObjectPlacer` が `GameObject.CreatePrimitive(PrimitiveType.Cube)` 後に BoxCollider を Destroy している（NavMesh の RenderMeshes ベイク用に MeshRenderer は残置）
- `EnsureBufferCapacity(tileCount * 20)` が Block の最大頂点数（5面×4頂点）に対応

### 整合性レビュー ✅

`WorldMapView.BuildQueuedChunk` で `CreateChunkObject` → `environmentObjectPlacer.PlaceChunkProps` → `CompleteChunkBuild` → `navMeshBuildService.BakeLayerIfNeeded` の呼び出し順序が設計通り。

### 役割の重複 ✅

特になし。

---

## Phase 5: NavMesh 連携

### 設計レビュー ✅

`INavigationPathProvider` を Application 層（UnityEngine 非依存）に置き、`UnityNavMeshPathProvider` を View 層に実装するというアーキテクチャ境界が正確。`ActorNavigationService` が「NavMesh 優先 → A* fallback」の順で使用する設計も適切。

### コード品質レビュー ✅

- `UnityNavMeshPathProvider.resultPath` が内部バッファを使い回しているが、`ActorPathState.SetPath` が `for (var i = 0; i < newPath.Count; i++) path.Add(newPath[i])` でディープコピーしているため安全
- `navMeshPath = new NavMeshPath()` をフィールドで保持して毎フレームのアロケーションを回避
- corners[0] をスキップして corners[1] から処理（スタート地点除外）と重複 GridPosition 除去が正確

### 整合性レビュー ✅

task_0005 の設計と実装が一致。`layer.Id`（`MapLayer.Id`）を使用している（task 上の `layer.LayerId` 記載ミスが実装で正しく解釈されている）。

### 役割の重複 ✅

`NavMeshBuildService`（bake 担当）と `UnityNavMeshPathProvider`（path 計算担当）の役割が明確に分離されている。

---

## Phase 6: WorldLifetimeScope DI 整理

### 設計レビュー ⚠️ 低

`UnityNavMeshPathProvider` は View 層クラスだが、`// === Application: ナビゲーション / 空間 ===` グループに分類されている。コメントが実態と乖離している。

```csharp
// === Application: ナビゲーション / 空間 ===  ← View 層クラスが混入
builder.Register<UnityNavMeshPathProvider>(Lifetime.Scoped).As<INavigationPathProvider>();
```

`INavigationPathProvider` が Application 境界のインターフェースであることから混入したと考えられるが、実装クラスは View 層のため `// === View: マップ描画 ===` に含めるべき。

### コード品質レビュー ✅

コメント追加のみ。機能変更なし。

### 整合性レビュー ✅

12 グループが全登録を包含。Milestone 6 追加クラスが含まれている（上記指摘以外）。

### 役割の重複 ✅

特になし。

---

## 横断的指摘

### プレースホルダー生成ロジック・定数の重複（重要度: 中）

**該当箇所**:

- `VisualConfigLoader.cs`: 定数 `SpriteWidth/SpriteHeight/PixelsPerUnit`、`CreatePlaceholderSprite`、`CreatePlaceholderTexture`
- `ActorSpriteVisualConfig.cs`: 同一定数、`Add`（内部で `Texture2D` 生成）、`CreatePlaceholderTexture`

内容が全く同一。将来的にスプライトサイズや PPU が変わった場合に両方の修正が必要になる。

二者の用途は異なる（`VisualConfigLoader` は `ActorSpriteSet` の fallback sprite 用、`ActorSpriteVisualConfig` は `GetSprite` の最終 fallback 用）が、実装の重複は解消できる。

**解消案**: `ActorSpriteVisualConfig` 側の定数を `VisualConfigLoader` から参照する、または共通 static class に切り出す。

### Destroy ヘルパーの重複（重要度: 低）

**該当箇所**:

- `ActorPrefabSource.DestroyPrefabObject`（private static）
- `WorldActorViewPool.DestroyActorObject`（private static）

どちらも `#if UNITY_EDITOR` 条件で `DestroyImmediate` / `Destroy` を切り替えるパターンで全く同一の実装。影響範囲は両クラス内に限定されるため優先度は低い。

---

## 最終判定

| フェーズ | 設計 | コード品質 | 整合性 | 役割重複 | 判定 |
|---|---|---|---|---|---|
| Phase 1 | ✅ | ✅ | ✅ | ⚠️ 中 | 承認 |
| Phase 2 | ✅ | ✅ | ✅ | ⚠️ 低 | 承認 |
| Phase 3 | ✅ | ✅ | ✅ | ⚠️ 中 | 承認 |
| Phase 4 | ✅ | ✅ | ✅ | ✅ | 承認 |
| Phase 5 | ✅ | ✅ | ✅ | ✅ | 承認 |
| Phase 6 | ⚠️ 低 | ✅ | ✅ | ✅ | 承認 |

**要修正（バグ・ハードゲート違反）**: なし

**推奨改善（次マイルストーン以降の対応で可）**:

1. `UnityNavMeshPathProvider` の DI グループコメントを `View: マップ描画` に移動する（Phase 6）
2. プレースホルダー定数（`SpriteWidth/SpriteHeight/PixelsPerUnit`）と `CreatePlaceholderTexture` ロジックを共通化する（Phase 1/3）
3. `DestroyPrefabObject` / `DestroyActorObject` の共通化（Phase 2）

全 6 フェーズを通じて P5/P8 パターン準拠、Application boundary 準拠、coding-rules.md 準拠（`>` 演算子禁止等）が確認できており、全体的に高品質な実装。

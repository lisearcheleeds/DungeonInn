# Milestone 6 完了前統合レビュー 第1回

作成日: 2026-05-18  
レビュアー: Claude Code（設計・コード品質・整合性・役割重複）、Codex（同4観点 + roadmap 整合性）

## レビュー範囲

- 対象差分: `git diff HEAD`（ステージング済み差分）
- 対象マイルストーン: Milestone 6 Phase 1〜Phase 6（task_0001〜task_0006）
- 参照ドキュメント: `docs/roadmap/milestone6-roadmap.md`, `tasks/task_0001〜0006.md`, 各 guideline

---

## 要修正項目

### [A] NavMesh 再生成後の更新が機能しない（重要度: 高）

**指摘者**: Codex

**問題**:

roadmap Phase 5 の完了条件「ダンジョン再生成後も NavMesh が正しく更新される」が未達。

`NavMeshBuildService.BakeLayerIfNeeded()` は `bakedLayerIds` に layer id が入ると以降の bake をスキップする。同時に `WorldMapView` も `scheduledLayerIds` / `completedLayerIds` を保持し続けるため、同じ layer id の dungeon floor が再生成された場合、chunk mesh と NavMesh の双方が再構築されない。

**原因**:

初回 build / 初回 bake を前提とした状態管理になっており、layer の revision や再生成イベントを扱う契約がない。

**解決案（短期）**:

再生成時に対象 layer の状態を明示的に invalidate する API を追加する。

```csharp
// WorldMapView
public void InvalidateLayer(MapLayerId layerId)
// NavMeshBuildService  
public void InvalidateLayer(MapLayerId layerId)
```

`MapLayerViewRegistry` 側で対象 layer の tile root children / generated mesh / prop を破棄する処理も合わせて追加する。

**根治対応**: `WorldMapLayerViewData` に revision を持たせ、`WorldMapView` が `layerId + revision` で chunk / NavMesh 更新要否を判断する。

**影響ファイル**:
- `NavMeshBuildService.cs`
- `WorldMapView.cs`
- `MapLayerViewRegistry.cs`

---

### [B] ログ・一時ファイルのステージ除外（重要度: 高）

**指摘者**: Codex

**問題**:

以下のファイルがステージされており、Milestone 6 の成果物コミットに含めるべきでない。

- `claude-codex-communication.log`（108,610 行、作業監視ログ）
- `codex_err_0001.tmp`
- `codex_out_0001.tmp`
- `codex_stdin_0001.tmp`
- `codex_stdin_0002.tmp`

差分の可読性を大きく落とし、プロンプトや作業内容の意図しない流出リスクがある。

**対応**:

コミット前に上記ファイルを unstage する。`codex_*.tmp` については `.gitignore` への追加を検討する。`claude-codex-communication.log` をコミット対象にするかどうかの運用ルールを AGENTS.md に明記する。

---

### [C] EnvironmentObjectPlacer が Prefab 配置基盤を満たしていない（要確認）

**指摘者**: Codex

**問題**:

roadmap Phase 4 の完了条件「プロップ Prefab が tile 位置に配置される」「初期実装は柱・松明・木などの小型プロップを想定する（3D モデル Prefab）」に対し、実装は `GameObject.CreatePrimitive(PrimitiveType.Cube)` による Cube 直生成となっている。Prefab 差し替え口が存在しない。

一方、task_0004 の完了条件は「StairUp / StairDown タイル位置にプロップキューブが配置される」と記載されており、Claude Code がこの task を設計・承認している。task と roadmap の間に齟齬が生じている。

**ユーザー判断が必要**:

- roadmap 基準で「Prefab 配置基盤」を Milestone 6 内で対応するか
- task_0004 基準で「Cube 仮配置は完了」とし Prefab 化を Milestone 7 以降へ延期するか

**延期する場合の短期状態**: `EnvironmentObjectPlacer` は Prefab 未設定時の fallback として Cube を表示する実装として扱い、Prefab 差し替え口の追加を次マイルストーンのタスクとして起票する。

**影響ファイル**:
- `EnvironmentObjectPlacer.cs`
- `docs/roadmap/milestone6-roadmap.md`（完了条件の記述整合）

---

## フェーズ別レビュー

### Phase 1: Visual Config の ScriptableObject 化 + Addressables 対応

| 観点 | 評価 | 備考 |
|---|---|---|
| 設計 | ✅ | P5 パターン（IAssetScope）準拠、禁止 API なし |
| コード品質 | ✅ | OperationCanceledException 再スロー、fallback Dispose 管理 |
| 整合性 | ✅ | Phase 3 での `AddressPrefix` 変更が task に記録されている |
| 役割重複 | ⚠️ | 後述の横断指摘 [D] 参照 |

**補足 (Codex)**: `VisualConfigLoader.LoadAsync()` は現状初期化時に 1 回だけ呼ばれる運用で実害なし。将来リロードを追加する場合は既存 `IAssetScope` の破棄契約を追加すること。

---

### Phase 2: Actor MonoBehaviour Prefab 化

| 観点 | 評価 | 備考 |
|---|---|---|
| 設計 | ✅ | ActorPrefabSource / WorldActorViewPool / ActorView の責務分離が明確 |
| コード品質 | ✅ | `??=` lazy init で EditMode テスト対応。owned フラグで所有権管理 |
| 整合性 | ✅ | Phase 3 での `Reset()` シグネチャ変更が task に記録されている |
| 役割重複 | ⚠️ | 後述の横断指摘 [E] 参照 |

**補足 (Codex)**: `ActorPrefabSource` の fallback 生成は「設定漏れでも動くための表示 fallback」であり、implementation-quality の DI 依存生成禁止には該当しない。Application / Domain の依存を View 側で手動生成しない契約を維持すること。

---

### Phase 3: Actor スプライトアニメーション

| 観点 | 評価 | 備考 |
|---|---|---|
| 設計 | ✅ | ComputeDirection のカメラ Yaw 考慮が正確。`0f <=` 比較のみ使用 |
| コード品質 | ✅ | HashSet による O(1) ルックアップ。SetRotationY 間引き最適化あり |
| 整合性 | ⚠️ | 後述の横断指摘 [F] 参照（roadmap Milestone 7 送り記述との矛盾） |
| 役割重複 | ✅ | ActorView / WorldActorPresenter / ActorSpriteVisualConfig の責務が分離されている |

---

### Phase 4: Map 3D トポロジーと背景テクスチャ

| 観点 | 評価 | 備考 |
|---|---|---|
| 設計 | ✅ | AddQuad 内の動的 material ルックアップでタイミング問題を解消 |
| コード品質 | ✅ | Block 5 面ワインディング正確。BoxCollider Destroy 済み |
| 整合性 | ⚠️ | 要修正 [C] 参照（EnvironmentObjectPlacer の Prefab 化要件） |
| 役割重複 | ✅ | MapMeshBuildService と EnvironmentObjectPlacer の責務重複なし |

---

### Phase 5: NavMesh 連携

| 観点 | 評価 | 備考 |
|---|---|---|
| 設計 | ✅ | INavigationPathProvider が Application 層、UnityEngine 非依存 |
| コード品質 | ✅ | resultPath 使い回しは ActorPathState.SetPath がコピーするため安全 |
| 整合性 | ✗ | 要修正 [A] 参照（NavMesh 再生成未対応） |
| 役割重複 | ✅ | NavMeshBuildService（bake）と UnityNavMeshPathProvider（path 計算）が分離 |

---

### Phase 6: WorldLifetimeScope DI 整理

| 観点 | 評価 | 備考 |
|---|---|---|
| 設計 | ⚠️ | 後述の横断指摘 [G] 参照（UnityNavMeshPathProvider のグループ分類） |
| コード品質 | ✅ | コメント追加のみ、機能変更なし |
| 整合性 | ✅ | 12 グループが全登録を包含 |
| 役割重複 | ✅ | 特になし |

---

## 横断的指摘（推奨改善）

### [D] プレースホルダー定数・生成ロジックの重複（重要度: 中）

**指摘者**: Claude Code

**該当箇所**:

- `VisualConfigLoader.cs:14-17` — `SpriteWidth=32`, `SpriteHeight=48`, `PixelsPerUnit=16f` の定数と `CreatePlaceholderSprite` / `CreatePlaceholderTexture`
- `ActorSpriteVisualConfig.cs:13-16` — 同一定数と同一テクスチャ生成ロジック

用途は異なる（`VisualConfigLoader` は ActorSpriteSet の fallback sprite 用、`ActorSpriteVisualConfig` は GetSprite の最終 fallback 用）が、スプライトサイズ・PPU が変わった際に両方の修正が必要になる。

**解消案**: `ActorSpriteVisualConfig` 側の定数を `VisualConfigLoader` から参照するか、共通 static class に切り出す。

---

### [E] Destroy ヘルパーの重複（重要度: 低）

**指摘者**: Claude Code

**該当箇所**:

- `ActorPrefabSource.DestroyPrefabObject`（private static）
- `WorldActorViewPool.DestroyActorObject`（private static）

`#if UNITY_EDITOR` 条件で `DestroyImmediate` / `Destroy` を切り替えるパターンが全く同一。影響範囲は両クラス内に限定されており優先度は低い。

---

### [F] roadmap の Milestone 7 送り項目と実装の矛盾（重要度: 中）

**指摘者**: Codex

**問題**:

`docs/roadmap/milestone6-roadmap.md` の「Milestone 7 へ移動する項目」に「4 方向 / 8 方向スプライト（Milestone 6 は左右反転のみ）」という記述が残っている。一方、実装（Phase 3）では NE/NW/SE/SW の 4 方向スプライトが Milestone 6 内で実装済み。

**原因**: 実際のアセット提供に合わせて Phase 3 の task が更新されたが、roadmap の Milestone 7 送り記述が追従していない。

**対応**: `docs/roadmap/milestone6-roadmap.md` の Milestone 7 送り項目を更新し、「4 方向スプライトは Milestone 6 で実装済み」とした上で Milestone 7 送りを「8 方向化」「戦闘アニメーション（combat / hit / dead）」「正式 Walk フレーム差し替え」に整理する。

---

### [G] UnityNavMeshPathProvider の DI グループ分類（重要度: 低）

**指摘者**: Claude Code（Codex との見解相違あり）

**問題**:

`WorldLifetimeScope` では `UnityNavMeshPathProvider`（View 層クラス）が `// === Application: ナビゲーション / 空間 ===` グループに含まれている。

**Codex 見解**: `INavigationPathProvider`（Application 境界）として登録されているため、Application グループへの分類は依存方向の観点から成立する。

**Claude Code 見解**: 登録のインターフェース側が Application 層でも、実装クラス（`UnityNavMeshPathProvider`）は View 層であり、コメントが実態と乖離する。`// === View: マップ描画 ===` に含めるべき。

**推奨**: View 層クラスが Application グループにあると誤読が起きやすい。`// === View: マップ描画 ===` 内に移動し、`.As<INavigationPathProvider>()` という登録形式でインターフェース境界を明示する方が明確。

---

### [H] INavigationPathProvider 戻り値の内部バッファ契約（重要度: 低）

**指摘者**: Codex（Claude Code は安全確認済み）

**問題**:

`UnityNavMeshPathProvider.TryFindPath()` が内部 `List<GridPosition>`（`resultPath`）を `IReadOnlyList<GridPosition>` として返している。現状は `ActorPathState.SetPath()` が即座にコピー（`for (var i = 0; i < newPath.Count; i++) path.Add(newPath[i])`）しているため実害はない。

ただし、Application 層のインターフェースとして「内部バッファを返す」API は将来の別実装・別呼び出しで誤用されやすい。インターフェースのコメント（「provider 内部の一時バッファであり、呼び出し側は同一フレームで消費すること」）が唯一の契約になっている。

**解消案（保守的）**: `TryFindPath` の doc comment を強化し、保持禁止を明記する。  
**解消案（根治）**: `TryFindPath(..., List<GridPosition> results)` のように呼び出し側バッファへの書き込み形式に変更する。

---

### [I] trailing whitespace（重要度: 低）

**指摘者**: Codex

`git diff --cached --check HEAD` で trailing whitespace が検出される。Unity 生成 YAML（`.asset` / `.meta` / scene）は無理に整形しない方が安全だが、手書き markdown（`tasks/task_0003.md`, `tasks/task_0005.md`）については修正が望ましい。ログファイルを unstage すれば検出量は大きく減る。

---

## 最終判定サマリ

| # | 項目 | 重要度 | 指摘者 | 対応区分 |
|---|---|---|---|---|
| A | NavMesh 再生成後の更新が機能しない | 高 | Codex | **要修正** |
| B | ログ・一時ファイルのステージ除外 | 高 | Codex | **要修正** |
| C | EnvironmentObjectPlacer の Prefab 化 | 中 | Codex | **要確認（ユーザー判断）** |
| D | プレースホルダー定数・ロジックの重複 | 中 | Claude Code | 推奨改善（次マイルストーン） |
| E | Destroy ヘルパーの重複 | 低 | Claude Code | 推奨改善（次マイルストーン） |
| F | roadmap Milestone 7 送り項目の記述矛盾 | 中 | Codex | ドキュメント更新（コミット前） |
| G | UnityNavMeshPathProvider の DI グループ分類 | 低 | Claude Code | 推奨改善（次マイルストーン） |
| H | INavigationPathProvider 内部バッファ契約 | 低 | Codex | 推奨改善（次マイルストーン） |
| I | trailing whitespace（手書き markdown） | 低 | Codex | コミット前に対応可 |

**ハードゲート・ガイドライン違反**: なし  
**コンパイル・テスト**: Codex は uLoop 接続不可のため静的確認のみ。Claude Code は task レビュー時に compile ErrorCount 0 / 245 tests pass / PlayMode 30 秒 Error 0 を確認済み。

**Milestone 6 完了判定**: 要修正 [A][B] の対応完了、[C] のユーザー判断後に完了とする。

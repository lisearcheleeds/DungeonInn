# Milestone 6 完了レビュー 第1回 Codex

作成日: 2026-05-18

## レビュー範囲

- 対象差分: `git diff HEAD` のステージ済み差分
- 対象マイルストーン: Milestone 6 Phase 1 から Phase 6
- 観点:
  - 設計レビュー
  - コード品質レビュー
  - 整合性レビュー
  - 役割の重複レビュー
- 確認した guideline:
  - `docs/guidelines/lighthouse-patterns.md`
  - `docs/guidelines/coding-rules.md`
  - `docs/guidelines/domain-design-guidelines.md`
  - `docs/guidelines/application-boundary-guidelines.md`
  - `docs/guidelines/implementation-quality-guidelines.md`
  - `docs/guidelines/self-review-preset.md`
- 参照した主な計画/タスク:
  - `docs/roadmap/milestone6-roadmap.md`
  - `tasks/task_0001.md`
  - `tasks/task_0002.md`
  - `tasks/task_0003.md`
  - `tasks/task_0004.md`
  - `tasks/task_0005.md`
  - `tasks/task_0006.md`

## 総評

Milestone 6 の実装方針は、View 層で Unity 依存を閉じ、Application 層へ必要最小限の interface を置く方向になっており、Clean Architecture / Lighthouse の大枠には沿っている。Phase 1 / 2 / 6 は大きな設計崩れを確認していない。

一方で、Phase 4 のプロップ配置と Phase 5 の NavMesh 再生成に、task / roadmap の完了条件と実装のズレがある。特に NavMesh は「ダンジョン再生成後も更新される」という完了条件を満たしていないため、Milestone 6 完了前に修正対象として扱うのが妥当。

また、実装差分とは別に `claude-codex-communication.log` と `codex_*.tmp` がステージされており、コミット対象としては不適切と判断する。

---

## Phase 1: Visual Config ScriptableObject / Addressables

### 評価

- 設計レビュー: 問題なし
- コード品質レビュー: 大きな問題なし
- 整合性レビュー: 問題なし
- 役割の重複レビュー: 問題なし

### 内容

`VisualConfigLoader` が `IAssetManager` / `IAssetScope` を使って Material / Sprite をロードし、`MapMaterialSet` と `ActorSpriteVisualConfig` が参照する構成は、Lighthouse の Addressables 利用方針に沿っている。Addressables ロード処理が View 層に閉じており、Domain / Application に Unity Addressables 依存を漏らしていない点も妥当。

`LoadAsync()` は現状 `WorldGameLoopEntryPoint.InitializeAsync()` から 1 回だけ呼ばれるため、実運用上の問題は見つけていない。ただし、将来リロードを入れる場合は `assetScope` の再作成前に既存 scope を dispose する契約を明示する必要がある。

### 根拠となるファイルリスト

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/VisualConfigLoader.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMaterialSet.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSpriteVisualConfig.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`

### 完了条件

- [ ] `VisualConfigLoader.LoadAsync()` が初期化時に 1 回だけ呼ばれる運用であることを維持する
- [ ] 将来リロードを追加する場合は、既存 `IAssetScope` の破棄契約を追加する

---

## Phase 2: Actor MonoBehaviour Prefab

### 評価

- 設計レビュー: 問題なし
- コード品質レビュー: 大きな問題なし
- 整合性レビュー: 問題なし
- 役割の重複レビュー: 問題なし

### 内容

`ActorView : MonoBehaviour` が `SpriteRenderer` と actor 表示状態だけを扱い、生成元を `ActorPrefabSource`、プールを `WorldActorViewPool`、登録管理を `WorldActorViewRegistry` に分けている。責務分離は明確。

`ActorPrefabSource` は設定済み Prefab がない場合に fallback Prefab を runtime 生成している。この処理は View 層の表示 fallback であり、DI 管理対象の Application / Domain service を手動生成しているわけではないため、implementation-quality の DI 依存生成禁止には該当しないと判断した。

### 根拠となるファイルリスト

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorPrefabSource.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorViewPool.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorViewRegistry.cs`

### 完了条件

- [ ] `ActorPrefabSource` の fallback 生成が「設定漏れでも動くための表示 fallback」であることを維持する
- [ ] Application / Domain の依存を View 側で手動生成しない

---

## Phase 3: Actor Sprite Animation

### 1. roadmap と task のスコープ記述が矛盾している

重要度: 中

問題:

実装と `tasks/task_0003.md` は NE / NW / SE / SW の 4 方向 sprite を前提にしている。一方で `docs/roadmap/milestone6-roadmap.md` の Milestone 7 送り項目には「4方向 / 8方向 sprite は Milestone 7、Milestone 6 は左右反転のみ」という趣旨の記述が残っている。

今回のユーザー指示では Goblin の方向別素材を追加し、反転による各方向/Walk 素材作成も依頼されているため、実装自体は現在の意図に沿っている。ただし、roadmap と task の整合性が崩れており、後続レビューで「Milestone 6 のスコープ外実装」と誤判定されるリスクがある。

原因:

素材追加に伴って Phase 3 の task は更新されたが、roadmap の Milestone 7 送り項目が追従していない。

解決案:

`docs/roadmap/milestone6-roadmap.md` の Milestone 7 送り項目から「4方向 sprite は Milestone 7」の記述を外し、Milestone 7 送りは「8方向化」「combat / hit / dead など戦闘アニメ」「正式 Walk フレーム差し替え」などに整理する。

根拠となるファイルリスト:

- `tasks/task_0003.md`
- `docs/roadmap/milestone6-roadmap.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorAnimationDirection.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSpriteSet.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`

完了条件:

- [ ] roadmap と task の Phase 3 スコープが一致している
- [ ] Milestone 7 送り項目が「Milestone 6 で未対応のアニメ拡張」に整理されている
- [ ] 4 方向 sprite 実装が Milestone 6 の正式スコープとして読める

### Phase 3 全体評価

- 設計レビュー: 方向選択と sprite 選択は View 層に閉じており妥当
- コード品質レビュー: `WorldActorPresenter` が辞書と HashSet を再利用しており、frame loop の allocation は抑えられている
- 整合性レビュー: roadmap のスコープ記述に矛盾あり
- 役割の重複レビュー: `ActorView` は表示状態、`WorldActorPresenter` は sprite 選択、`ActorSpriteVisualConfig` は sprite 解決で分離されている

---

## Phase 4: Map 3D topology / Texture

### 2. `EnvironmentObjectPlacer` が Prefab 配置基盤ではなく Cube 直生成になっている

重要度: 中

問題:

`tasks/task_0004.md` と roadmap は、StairUp / StairDown などの tile 位置に小物プロップ Prefab を配置する基盤を要求している。一方で実装は `GameObject.CreatePrimitive(PrimitiveType.Cube)` による Cube 直生成で、Prefab 差し替え口や visual config との接続がない。

仮表示として Cube を置くこと自体は動作確認に有効だが、「Prefab 配置基盤」としては完了条件を満たしていない。

原因:

Phase 4 の初期実装で「表示できる簡易オブジェクト」を優先し、Prefab 参照を持つ設定/解決責務が未実装のままになっている。

解決案:

短期対応として、`EnvironmentObjectPlacer` に `VisualConfigSettings` か専用 SO から prop Prefab を受け取る経路を作り、Prefab が未設定の場合だけ Cube fallback にする。根治対応では、`TileVisualKind` もしくは prop kind から Prefab を解決する `EnvironmentPropConfigSO` のような View 層設定に分離する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/EnvironmentObjectPlacer.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `tasks/task_0004.md`
- `docs/roadmap/milestone6-roadmap.md`

完了条件:

- [ ] `EnvironmentObjectPlacer` が Prefab を instantiate できる
- [ ] Prefab 未設定時のみ fallback Cube を使う
- [ ] task / roadmap の「Prefab 配置」完了条件と実装が一致している

### Phase 4 全体評価

- 設計レビュー: `MapMeshBuildService` と `EnvironmentObjectPlacer` の分離は妥当
- コード品質レビュー: mesh 生成側は chunk 単位で責務がまとまっている
- 整合性レビュー: prop 配置が Prefab 要件と不一致
- 役割の重複レビュー: map mesh 生成と prop 配置の責務重複はない

---

## Phase 5: NavMesh

### 3. NavMesh の再生成要件を満たしていない

重要度: 高

問題:

Milestone 6 の roadmap / task は「ダンジョン再生成後も NavMesh が更新される」ことを完了条件としている。しかし `NavMeshBuildService.BakeLayerIfNeeded()` は `bakedLayerIds` に layer id が入っている場合、以後 bake をスキップする。

さらに `WorldMapView` 側も `scheduledLayerIds` / `completedLayerIds` を保持し続けるため、同じ layer id の dungeon floor が再生成された場合、chunk mesh と NavMesh の再構築に入れない。

原因:

初回 build / 初回 bake を前提とした状態管理になっており、map layer の revision や再生成イベントを扱う契約がない。

解決案:

短期対応として、再生成時に `WorldMapView` と `NavMeshBuildService` の対象 layer 状態を明示的に invalidate する API を追加する。例:

- `WorldMapView.InvalidateLayer(MapLayerId layerId)`
- `NavMeshBuildService.InvalidateLayer(MapLayerId layerId)`
- `MapLayerViewRegistry` 側で該当 layer の tile root children / generated mesh / prop を破棄する

根治対応では、`WorldMapLayerViewData` に revision を持たせ、`WorldMapView` が layer id + revision で chunk / navmesh の更新要否を判断する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/NavMeshBuildService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapLayerViewRegistry.cs`
- `tasks/task_0005.md`
- `docs/roadmap/milestone6-roadmap.md`

完了条件:

- [ ] dungeon floor 再生成時に対象 layer の chunk mesh が再構築される
- [ ] dungeon floor 再生成時に対象 layer の NavMesh が再 bake される
- [ ] 旧 chunk / prop / generated mesh が残らない
- [ ] 再生成ケースの EditMode test または PlayMode 確認ログがある

### 4. `INavigationPathProvider` の戻り値契約が内部バッファ前提になっている

重要度: 低

問題:

`UnityNavMeshPathProvider.TryFindPath()` は `resultPath` という内部 `List<GridPosition>` を `IReadOnlyList<GridPosition>` として返している。`INavigationPathProvider` 側のコメントでは「provider 内部の一時バッファであり、呼び出し側は同一フレームで消費する」と契約を明示しているため、現状の実装意図は読める。

呼び出し側の `ActorPathState.SetPath()` は即座に内容をコピーしているため、現時点で実害はない。ただし Application 層の interface として内部バッファ寿命を前提にする API は、implementation-quality-guidelines の「内部バッファを返す API は契約を明示する」に該当する注意点であり、将来の別実装や別呼び出しで誤用されやすい。

原因:

NavMesh 経路探索の allocation を避けるため、provider が内部 buffer を再利用している。

解決案:

現状維持する場合は、`TryFindPath` の名前またはコメントをさらに強め、保持禁止であることを明記する。より安全にする場合は、`CopyPathTo(...)` / `TryFindPath(..., List<GridPosition> results)` のように呼び出し側 buffer へ書き込む API に変更する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/INavigationPathProvider.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/UnityNavMeshPathProvider.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/ActorNavigationService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/ActorPathState.cs`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `INavigationPathProvider` の戻り値を保持してはいけない契約が明示されている
- [ ] または API が呼び出し側 buffer 書き込み形式に変更されている
- [ ] `ActorPathState.SetPath()` が path をコピーする挙動を維持している

### Phase 5 全体評価

- 設計レビュー: Application interface / View 実装の依存方向は妥当
- コード品質レビュー: 内部バッファ再利用は性能上妥当だが API 契約に注意が必要
- 整合性レビュー: NavMesh 再生成の完了条件を満たしていない
- 役割の重複レビュー: `ActorNavigationService` と `UnityNavMeshPathProvider` の役割分離は妥当

---

## Phase 6: WorldLifetimeScope DI 整理

### 評価

- 設計レビュー: 問題なし
- コード品質レビュー: 大きな問題なし
- 整合性レビュー: 問題なし
- 役割の重複レビュー: 問題なし

### 内容

`WorldLifetimeScope` は View / Application の領域ごとに登録が整理されている。`UnityNavMeshPathProvider` は View 層の実装だが、Application 層の `INavigationPathProvider` として登録されており、依存方向は Application interface への注入として成立している。

登録数は多いが、Milestone 6 の Phase 6 の目的が「既存登録の整理」であるため、現時点では SubScope / Installer 分割を必須とは判断しない。

### 根拠となるファイルリスト

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`
- `tasks/task_0006.md`
- `docs/roadmap/milestone6-roadmap.md`

### 完了条件

- [ ] 新規追加クラスが適切な scope に登録されている
- [ ] Application interface と View 実装の登録関係が明確である
- [ ] DI 未登録による runtime 解決漏れがない

---

## 横断指摘

### 5. ログと一時ファイルがステージされている

重要度: 高

問題:

`claude-codex-communication.log` と `codex_*.tmp` がステージされている。AGENTS では `claude-codex-communication.log` へのストリームは必須だが、これは作業監視用ログであり、Milestone 6 の成果物としてコミットする対象ではないと判断する。

`claude-codex-communication.log` は 108,610 行あり、差分の可読性を大きく落としている。プロンプトや作業ログの混入リスクもある。

原因:

作業中に生成された監視ログと Codex の一時入出力ファイルが staging から除外されていない。

解決案:

コミット前に以下を unstaged にする。必要なら `.gitignore` に `codex_*.tmp` を追加する。`claude-codex-communication.log` は AGENTS で監視対象として扱うため、コミット対象にするかどうかのルールを明文化する。

根拠となるファイルリスト:

- `claude-codex-communication.log`
- `codex_err_0001.tmp`
- `codex_out_0001.tmp`
- `codex_stdin_0001.tmp`
- `codex_stdin_0002.tmp`
- `AGENTS.md`

完了条件:

- [ ] `claude-codex-communication.log` が今回の Milestone 6 成果物コミットから除外されている
- [ ] `codex_*.tmp` が staging から除外されている
- [ ] 必要なら `.gitignore` または運用ルールで再発防止されている

### 6. `git diff --cached --check` で trailing whitespace が大量に出ている

重要度: 低

問題:

`git diff --cached --check HEAD` で trailing whitespace が大量に検出された。多くは Unity 生成の `.asset` / `.meta` / scene / Addressables YAML と `claude-codex-communication.log` 由来だが、手書き docs にも検出がある。

原因:

Unity 生成 YAML は空値フィールドに trailing whitespace を含みやすい。加えて、task markdown の一部に手書きの trailing whitespace が残っている。

解決案:

Unity 生成物は無理に整形しない方が安全。手書き docs については修正する。ログファイルを unstaged にすれば、検出量は大きく減る。

根拠となるファイルリスト:

- `tasks/task_0003.md`
- `tasks/task_0005.md`
- `claude-codex-communication.log`
- `Client/Assets/AddressableAssetsData/`
- `Client/Assets/DungeonInn/Runtime/Art/**/*.meta`

完了条件:

- [ ] 手書き markdown の trailing whitespace が解消されている
- [ ] Unity 生成 YAML の trailing whitespace を許容するか、運用ルールが決まっている
- [ ] `claude-codex-communication.log` を staging から外した状態で再確認している

---

## 検証結果

### 実行した確認

- `git diff --stat HEAD`
- `git diff --name-status HEAD`
- `git diff --cached --name-only HEAD`
- `git diff --cached --numstat HEAD -- claude-codex-communication.log codex_err_0001.tmp codex_out_0001.tmp codex_stdin_0001.tmp codex_stdin_0002.tmp`
- `git diff --cached --check HEAD`
- 関連 source / task / roadmap / guideline の静的確認

### uLoop

以下は実行したが、Unity CLI Loop server に接続できず未完了。

```text
uloop.cmd compile --project-path Client
uloop.cmd run-tests --project-path Client --test-mode EditMode
```

結果:

```text
Error: Cannot connect to Unity.
Make sure Unity Editor is open and Unity CLI Loop server is running.
You can start the server from: Window > Unity CLI Loop > Server
```

### 未完了の検証

- [ ] `uloop.cmd compile --project-path Client`
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode`
- [ ] PlayMode で 30 秒以上実行し、Error / Warning がないこと
- [ ] dungeon floor 再生成後の NavMesh 再 bake 確認

---

## Codex 完了前チェック

- [x] `docs/guidelines/` 配下の guideline 本文を確認した
- [x] 各 guideline のハードゲートに違反していないか静的確認した
- [x] 各 guideline の完了前チェックリストを確認した
- [x] ハードゲートだけでなく、本文の設計方針・判断基準に反していないか確認した
- [x] 作業ログとして本レビュー文書にチェック結果を記載した
- [ ] `uloop.cmd compile --project-path Client` が成功した

`uloop.cmd compile --project-path Client` は Unity CLI Loop server に接続できないため未完了。完了判定前に Unity Editor 側で server を起動して再実行が必要。

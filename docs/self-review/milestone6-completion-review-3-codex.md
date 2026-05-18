# Milestone 6 completion self-review 3 - Codex

作成日: 2026-05-19
対象: Milestone 6 全差分、既存レビュー対応後の再セルフレビュー
参照: `docs/guidelines/self-review-preset.md`, `lighthouse-patterns.md`, `coding-rules.md`, `domain-design-guidelines.md`, `application-boundary-guidelines.md`, `implementation-quality-guidelines.md`, `docs/roadmap/milestone6-roadmap.md`, 既存 `docs/self-review/milestone6-completion-review-*-*.md`

## レビュー前提

- 既存レビューで指摘済みかつ現行コードで解消済みの項目は、未解決として再掲しない。
- Runtime API / constructor / DI / Event / State の追加変更は production 契約変更として扱い、責務と寿命を確認した。
- `git diff --check HEAD` は Unity 生成 `.meta` / `.asset` と既存レビュー md の trailing whitespace を多数検出した。今回の Runtime C# 主要差分の妥当性判断とは分けて、コミット前 hygiene として扱う。

## 検証結果

- `uloop.cmd compile --project-path Client`: 成功、ErrorCount 0 / WarningCount 0
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功、247 passed / 0 failed
- `rg` で Lighthouse 禁止 API / `WorldMapView.UpdateVisuals()` の毎フレーム layer 全件走査 / `WalkFrameRate` 残存を確認
- `Client/Assets/AddressableAssetsData/AssetGroups/DungeonInn Visual.asset` の `m_SchemaSet.m_Schemas` は空のまま

## 既存指摘の現状

解消済みと見てよいもの:

- [A] 新規 dungeon floor 生成時の map layer event 経路は `EnsureDungeonFloorGeneratedOrchestrator` の `MapLayerAddedEvent` 発行で追加済み。
- [C] `EnvironmentObjectPlacer` は `VisualConfigSettings.PropPrefab` 経由で Prefab 差し替え口を持ち、未設定時だけ Cube fallback。
- [E] `WorldMapView.UpdateVisuals()` は `BuildQueuedChunks()` のみで、`GetLayers()` / `TakeNewLayers()` の毎フレーム全件走査は残っていない。
- [F] `WorldActorPresenter` は delegate を field cache し、毎フレームローカル変数キャプチャを避けている。
- [G] `MapMaterialSet.asset` に全 `TileVisualKind` の Material address が入り、Addressables group に `Materials/Map/*` が登録済み。
- [I] `ActorAnimationState` / `ActorSpriteAnimationClip` / `ActorSpriteAnimator` が追加され、`WorldActorPresenter` の `WalkFrameRate` は残っていない。

未解決または追加確認が必要なもの:

- [H] Addressables group schema 未設定。
- [J] asset / Addressables / mesh geometry の回帰テスト不足。
- [K] `VisualAssetSetup.AutoSetup()` の domain reload 副作用。
- [R] 対応ログ内に古い完了判定が残っており、現行コードとの差分が読み取りにくい。

---

### 1. Addressables group が schema なしのまま残っている

重大度: 中

問題:

`DungeonInn Visual.asset` の `m_SchemaSet.m_Schemas` が空のまま残っている。`VisualAssetSetup.EnsureAddressablesGroup()` も `settings.CreateGroup(AddressablesGroupName, false, false, false, null)` を使っており、新規生成時に BundledAssetGroupSchema / ContentUpdateGroupSchema をコピーしない。

Editor Fast Mode では通っても、Packed Mode や実配信ビルドでは build / load path が未設定の group になり、Milestone 6 の「Addressables 経由で Material / Sprite をロードする」完了条件の保証が弱い。

原因:

既存の `Packed Assets.asset` template はあるが、`DungeonInn Visual` group 作成時に schema template として使われていない。前回レビューでは推奨改善扱いだったが、今回の全体レビューでも現行 asset は未解消のまま。

解決案:

短期対応として、Editor 上で `DungeonInn Visual` group に BundledAssetGroupSchema / ContentUpdateGroupSchema を追加する。根本対応として `VisualAssetSetup.EnsureAddressablesGroup()` が `Packed Assets.asset` template から schema をコピーして group を作成するようにする。既存 group が schema なしの場合も validation または補正で検出する。

根拠となるファイルリスト:

- `Client/Assets/AddressableAssetsData/AssetGroups/DungeonInn Visual.asset`
- `Client/Assets/AddressableAssetsData/AssetGroupTemplates/Packed Assets.asset`
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `docs/guidelines/lighthouse-patterns.md`

完了条件:

- [ ] `DungeonInn Visual.asset` の `m_SchemaSet.m_Schemas` が空でない
- [ ] BundledAssetGroupSchema の build / load path が Local profile に接続されている
- [ ] `VisualAssetSetup.EnsureAddressablesGroup()` が schema なし group を生成しない
- [ ] Packed Mode または Addressables build の smoke check が成功する

再発理由:

前回レビューで「Packed Mode build 前までの推奨改善」として扱われ、Milestone 6 完了ゲートから外れたため、Fast Mode の PlayMode 成功だけで見逃せる状態が残った。

再発防止策:

Milestone 完了レビューの asset pipeline チェックに、`m_SchemaSet.m_Schemas` が空でないことを機械的に確認する項目を追加する。

---

### 2. VisualAssetSetup が domain reload ごとに Addressables setup と SaveAssets を実行する

重大度: 中

問題:

`VisualAssetSetup.AutoSetup()` は `[InitializeOnLoadMethod]` で実行され、必要 asset がすべて存在する場合でも毎回 `SetupAddressables()` を呼ぶ。`SetupAddressables()` は `EnsureAddressablesGroup()` の後に `AssetDatabase.SaveAssets()` と log 出力を行う。

domain reload ごとに asset database へ副作用を持つ処理が走るため、意図しない Addressables 差分、ログノイズ、schema なし group の再保存が起きやすい。

原因:

初期導入の利便性のために「不足 asset の自動作成」と「Addressables group の明示セットアップ」が同じ自動経路に入っている。差分が必要な場合だけ保存する guard もない。

解決案:

`AutoSetup()` は asset 不足時だけ `RunSetup()` を呼ぶ。Addressables の作成・補正は menu 実行または explicit validation / repair command に分離する。自動実行を残す場合は `EnsureAddressablesGroup()` が changed flag を返し、差分があるときだけ `AssetDatabase.SaveAssets()` を呼ぶ。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `Client/Assets/AddressableAssetsData/AddressableAssetSettings.asset`

完了条件:

- [ ] domain reload だけでは `AddressableAssetsData` に差分が出ない
- [ ] `SetupAddressables()` は menu または明示 repair 経路からだけ呼ばれる、または changed flag がある
- [ ] `AssetDatabase.SaveAssets()` が setup 必要時にだけ呼ばれる
- [ ] schema 欠落などの不完全設定を validation で検出できる

再発理由:

前回レビューの [K] が推奨改善扱いで、`AutoSetup()` の副作用を止める完了条件が Milestone 6 の必須対応に入らなかった。

再発防止策:

Editor 自動化を追加するタスクでは、`InitializeOnLoadMethod` が書き込みを行うか、保存を伴うか、実行 guard があるかをレビュー項目に含める。

---

### 3. Asset pipeline と mesh / Addressables 設定を検出する EditMode test が不足している

重大度: 中

問題:

EditMode test は 247 件通っているが、Milestone 6 の中核である以下の設定を直接検証するテストがない。

- `MapMaterialSet.asset` entries と Addressables group の address 一致
- `DungeonInn Visual.asset` の schema
- `VisualConfigLoader` が Material / Sprite address を少なくとも smoke load できること
- `MapMeshBuildService` の Plane / Block / Ramp geometry の頂点数・submesh 数
- `WorldMapView` の chunk build 完了後に NavMesh bake trigger が呼ばれること

今回 [G] は手修正で解消されたが、テストで検出できないため同じ regression が再発しやすい。

原因:

既存テストは Application orchestration と view data snapshot が中心で、Unity asset YAML / Editor setup / Addressables config の検証が薄い。

解決案:

まず YAML / AssetDatabase を読む EditMode test を追加し、`MapMaterialSet.asset` entries、Addressables entries、Addressables schema を検証する。次に `MapMeshBuildService` の純粋 geometry test を追加する。NavMesh は Unity API 依存が強いため、`NavMeshBuildService` の責務を小さくした上で trigger 経路の spy / fake を検討する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Tests/EditMode/`
- `Client/Assets/DungeonInn/Runtime/StaticResources/Visual/MapMaterialSet.asset`
- `Client/Assets/AddressableAssetsData/AssetGroups/DungeonInn Visual.asset`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMeshBuildService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`

完了条件:

- [ ] `MapMaterialSet.asset` の entries が全 `TileVisualKind` を持つことを検証する test がある
- [ ] Addressables group に sprite / map material address があることを検証する test がある
- [ ] Addressables group schema が空でないことを検証する test がある
- [ ] `MapMeshBuildService` の Plane / Block / Ramp geometry を検証する test がある
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する

再発理由:

前回レビューで [J] として指摘済みだが、asset 設定の実修正が先行し、回帰検出の test 追加が未対応のまま残った。

再発防止策:

Milestone 完了時の「compile / EditMode / PlayMode」に加えて、asset 設定を変更した milestone では「Asset 設定整合 test」を必須にする。

---

### 4. `task_m6_review3.md` に古い完了判定が残り、現行対応と矛盾して見える

重大度: 低

問題:

`tasks/task_m6_review3.md` の前半には「`DungeonFloorRegeneratedEvent` の Publisher は M6 範囲では実装しない」「Publisher は M7 以降」「経路の存在要件はこれで充足」といった古い判定が残っている。末尾には今回の追加対応として `EnsureDungeonFloorGeneratedOrchestrator` が `MapLayerAddedEvent` を発行する説明が追記されているが、古い完了判定を読み替える必要がある。

コード上は「再生成 UseCase はないため `DungeonFloorRegeneratedEvent` publisher は未実装」「新規 floor 生成の実動線は `MapLayerAddedEvent` で対応」という整理になっている。この判断自体は妥当だが、task ログだけ読むと以前の未対応判定と今回の対応が混在している。

原因:

タスクログは追記運用であり、過去ログを削除しない方針のため、後続対応が入った後に「現在の最終判定」セクションが十分に分離されていない。

解決案:

`task_m6_review3.md` の末尾に「最終判定」セクションを追加し、古い判定を上書きせずに現行状態を明記する。内容は以下に整理する。

- `DungeonFloorRegeneratedEvent` は再生成専用で、再生成 UseCase がないため publisher なし
- `EnsureDungeonFloorGeneratedOrchestrator` は新規 floor 生成時に `MapLayerAddedEvent` を publish
- 初期 floor は `InitializeGameWorldOrchestrator` が `GameWorldState.Initialize()` 後に publish
- [A] の M6 必須範囲は上記で完了

根拠となるファイルリスト:

- `tasks/task_m6_review3.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Dungeons/EnsureDungeonFloorGeneratedOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Dungeons/InitializeDungeonOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/InitializeGameWorldOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldMapEvents.cs`

完了条件:

- [ ] `task_m6_review3.md` の末尾に現行最終判定がある
- [ ] 古い「Publisher は M7 以降」記述を読んでも、今回の `MapLayerAddedEvent` 対応との関係が分かる
- [ ] `EnsureDungeonFloorGeneratedPublishesMapLayerAddedEventForNewFloor` test が存在し、成功している

再発理由:

レビュー対応が複数回に分かれたが、追記ログの最終状態を集約する節がなかった。

再発防止策:

レビュー対応タスクでは、最後に「現在の最終判定 / 旧ログとの差分」節を必ず追記する。

---

## ハードゲート確認

- Lighthouse 禁止 API: Milestone 6 の主要変更では `Addressables.LoadAssetAsync` / `Resources.Load` / `SceneManager.LoadScene` / `Camera.main` の直接追加は確認していない。Editor の `Resources.FindObjectsOfTypeAll` は Editor setup 内で、Runtime load ではない。
- Application boundary: `WorldMapView.UpdateVisuals()` の layer polling は解消済み。`WorldActorPresenter` の毎フレーム closure allocation も解消済み。
- Domain boundary: Milestone 6 の主要変更で Domain から View / Infrastructure / Framework への依存追加は確認していない。
- Coding rules: 主要 C# 差分は block namespace / Allman style / explicit public API に沿っている。`git diff --check` の trailing whitespace は Unity 生成 asset / meta / 既存 review md が中心。
- Implementation quality: DI 対象は `WorldLifetimeScope` に登録されている。未解決は Editor setup side effect と asset config regression test 不足。

## 最終判定

Milestone 6 の Runtime 表示・NavMesh・Actor animation の主要不具合は、今回の対応後に compile / EditMode / PlayMode smoke で破綻していない。

ただし、Milestone 6 を「Addressables 経由の asset pipeline まで完了」と扱うなら、`DungeonInn Visual` group schema 未設定と asset pipeline test 不足は残件として扱うべき。Packed Mode build 前に必ず対応する。

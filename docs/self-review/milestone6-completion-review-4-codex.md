# Milestone 6 Completion Review 4 - Codex

対象: マイルストーン6全体、および `milestone6-completion-review-3-codex.md` の対応後差分

確認日: 2026-05-19

## レビュー前提

- 参照 guideline:
  - `docs/guidelines/lighthouse-patterns.md`
  - `docs/guidelines/coding-rules.md`
  - `docs/guidelines/domain-design-guidelines.md`
  - `docs/guidelines/application-boundary-guidelines.md`
  - `docs/guidelines/implementation-quality-guidelines.md`
  - `docs/guidelines/self-review-preset.md`
- 既存レビュー対応ログ:
  - `docs/self-review/milestone6-completion-review-1-total.md`
  - `docs/self-review/milestone6-completion-review-2-total.md`
  - `docs/self-review/milestone6-completion-review-3-codex.md`
  - `tasks/task_m6_review3.md`
- 機械確認:
  - `uloop.cmd compile --project-path Client`: 成功
  - `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功、252/252 passed
  - PlayMode smoke: Error log 0 件

## 観点別確認

### 設計レビュー

問題:

新規問題なし。

原因:

前回指摘した Addressables group schema の欠落は、`DungeonInn Visual` group に `BundledAssetGroupSchema` と `ContentUpdateGroupSchema` を付与する形で解消されている。`VisualAssetSetup.AutoSetup()` は不足 asset がある場合のみセットアップを実行するため、Domain Reload ごとの Addressables 再生成副作用は解消されている。

解決案:

追加対応なし。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `Client/Assets/AddressableAssetsData/AssetGroups/DungeonInn Visual.asset`
- `Client/Assets/AddressableAssetsData/AssetGroups/Schemas/`
- `Client/Assets/DungeonInn/Tests/EditMode/VisualAssetPipelineTests.cs`

完了条件:

- [x] Addressables group に schema が存在することを asset と EditMode test で確認した
- [x] AutoSetup が毎 domain reload で Addressables を保存しないことをコードで確認した
- [x] `uloop compile` と EditMode test が成功した

### コード品質レビュー

問題:

新規問題なし。

原因:

追加した asset pipeline test は Addressables group、MapMaterialSet、MapMeshBuildService の境界に限定されており、Runtime public API や production constructor をテスト都合で増やしていない。EditMode cleanup で必要になった `WorldDebugMaterialFactory.Dispose()` の変更は、PlayMode では `Destroy`、EditMode では `DestroyImmediate` を使う Unity ライフサイクル差分の吸収であり、責務の追加ではない。

解決案:

追加対応なし。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Tests/EditMode/VisualAssetPipelineTests.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/DungeonInn.Tests.EditMode.asmdef`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldDebugMaterialFactory.cs`

完了条件:

- [x] テスト用 helper は test assembly 内に閉じている
- [x] production 側にテスト都合の constructor / interface / DTO を追加していない
- [x] EditMode test 252 件が成功した

### 整合性レビュー

問題:

新規問題なし。

原因:

レビュー3で残っていた `DungeonFloorRegeneratedEvent` と `MapLayerAddedEvent` の記述不整合は、`tasks/task_m6_review3.md` に最終判定を追記して整理済み。初期生成は `InitializeGameWorldOrchestrator`、追加 floor 生成は `EnsureDungeonFloorGeneratedOrchestrator` が `MapLayerAddedEvent` を publish する形で、View invalidation 経路と設計意図が一致している。

解決案:

追加対応なし。

根拠となるファイルリスト:

- `tasks/task_m6_review3.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/InitializeGameWorldOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Dungeons/EnsureDungeonFloorGeneratedOrchestrator.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/GameLoopTests.cs`

完了条件:

- [x] task log の古い判定に対して現在の最終判定を追記した
- [x] 新規 floor 生成時の `MapLayerAddedEvent` 発行テストが含まれている
- [x] PlayMode smoke で Error log 0 件を確認した

### 役割重複レビュー

問題:

新規問題なし。

原因:

Addressables 登録は Editor setup、asset runtime load は `VisualConfigLoader`、material fallback は `MapMaterialSet`、mesh geometry は `MapMeshBuildService` に分かれている。今回追加した test はその境界を検証するだけで、Runtime 側の役割を横取りしていない。

解決案:

追加対応なし。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/VisualConfigLoader.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMaterialSet.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMeshBuildService.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/VisualAssetPipelineTests.cs`

完了条件:

- [x] Editor / Runtime / Test の責務境界が分離されている
- [x] asset 登録、asset load、fallback、mesh build の責務が重複していない
- [x] guideline のハードゲート違反は見つからなかった

## 結論

レビュー4で新規指摘はなし。レビュー3までの未解決項目は、実装・asset・テスト・task log の各レイヤーで対応済み。

このため、ユーザー指定の反復条件に従い、インデックス4で停止する。

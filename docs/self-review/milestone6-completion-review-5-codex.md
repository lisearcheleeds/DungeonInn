# Milestone 6 Completion Review 5 - Codex

対象: Milestone 6 全体の `git diff`
確認日: 2026-05-19

## レビュー前提

- 確認した guideline:
  - `docs/guidelines/lighthouse-patterns.md`
  - `docs/guidelines/coding-rules.md`
  - `docs/guidelines/domain-design-guidelines.md`
  - `docs/guidelines/application-boundary-guidelines.md`
  - `docs/guidelines/implementation-quality-guidelines.md`
  - `docs/guidelines/self-review-preset.md`
- 確認した milestone 資料:
  - `docs/roadmap/milestone6-roadmap.md`
  - 既存の `docs/self-review/milestone6-completion-review-*.md`

## 観点別レビュー

### 1. 再生成済み layer の古い chunk build request が残る

重大度: 中

問題:

`WorldMapView.InvalidateLayer()` は `scheduledLayerIds` / `completedLayerIds` / `remainingChunkCountsByLayer` / view root / NavMesh / view data cache を無効化しているが、`pendingChunkBuilds` にすでに積まれた `MapChunkBuildRequest` は削除されない。Dungeon floor 再生成後に同じ `MapLayerId` が再追加されると、古い request と新しい request が同じ queue 内に混在する。

原因:

chunk build は複数フレームに分割される一方、無効化は layer 単位で行われる。現在の request には build 世代が含まれていないため、再生成前の request か再生成後の request かを判定できない。

解決案:

`WorldMapView` に layer ごとの build version を持たせ、`EnqueueLayerChunks()` 時点の version を `MapChunkBuildRequest` に保存する。`InvalidateLayer()` で version を進め、`BuildQueuedChunk()` では request の version が現行 version と一致しない場合に破棄する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldMapEvents.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Dungeons/EnsureDungeonFloorGeneratedOrchestrator.cs`

完了条件:

- [ ] layer 無効化後に古い queued chunk が build されない
- [ ] 同じ layer が再追加されても新しい queued chunk だけが build される
- [ ] `WorldMapView` の既存責務を超えた Application / Domain 依存を追加しない

### 2. 整合性

重大度: 低

問題:

上記以外の新規整合性問題は見つからない。`MapLayerAddedEvent` は初期生成と追加 floor 生成の view invalidation 入り口として一貫しており、`DungeonFloorRegeneratedEvent` は既存 layer の invalidation に限定されている。

原因:

Milestone 6 の設計では、View は Application の `MapLayerId` event を受けて必要な layer だけを構築する方針になっている。現在の差分はこの方針に沿っている。

解決案:

追加対応なし。上記の queued request 世代管理だけを整合性修正として扱う。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/InitializeGameWorldOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Dungeons/EnsureDungeonFloorGeneratedOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`

完了条件:

- [x] 初期 layer と追加 layer の通知経路が同じ event 系に揃っている
- [x] View 側が Domain / Application の内部集合を毎フレーム全走査しない

### 3. コード重複

重大度: 低

問題:

新規の過剰なコード重複は見つからない。Addressables 登録、material fallback、sprite animation、NavMesh path provider はそれぞれ責務が分かれている。

原因:

Milestone 6 では Editor setup / runtime load / view construction / movement path を別クラスに分離しているため、同じ処理の並列実装は限定的。

解決案:

追加対応なし。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/VisualConfigLoader.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMaterialSet.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/UnityNavMeshPathProvider.cs`

完了条件:

- [x] Editor setup と runtime loader の責務が混ざっていない
- [x] fallback material と loaded material の責務が `MapMaterialSet` に集約されている

### 4. 役割重複

重大度: 低

問題:

新規の役割重複は見つからない。`WorldPresenter` は event 購読と `WorldMapView` への通知に限定され、`WorldMapView` は chunk build / root 管理 / NavMesh bake trigger に限定されている。

原因:

Application event の publish は Orchestrator 側にあり、View は subscribe して表示更新に変換している。Domain / Application / View の依存方向も維持されている。

解決案:

追加対応なし。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldMapEvents.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`

完了条件:

- [x] Application は View を参照しない
- [x] View は `MapLayerId` event を表示更新に変換するだけで、floor 生成判断を持たない

### 5. コーディングルール

重大度: 低

問題:

主要な Runtime C# 差分では、block namespace / Allman style / public API の明示 / field の underscore 禁止 / `[Inject]` 明示に大きな違反は見つからない。

原因:

既存の Milestone 6 差分は概ね既存 style に沿っている。`Resources.FindObjectsOfTypeAll` は Editor setup 内の scene asset assignment 用で、Runtime load の禁止 API には該当しない。

解決案:

追加対応なし。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/*.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/*.cs`
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`

完了条件:

- [x] Runtime C# の主要差分が coding rules の hard gate に違反していない
- [x] Lighthouse 禁止 API の Runtime 追加がない

## 対応方針

Review 5 では `WorldMapView` の queued chunk 世代管理のみを対応対象にする。これは既存 public API を増やさず、View 内部の整合性を補強する局所修正である。

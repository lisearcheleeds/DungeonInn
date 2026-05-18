# Milestone 6 Completion Review 6 - Codex

対象: Review 5 対応後の Milestone 6 全体差分
確認日: 2026-05-19

## レビュー前提

- 確認した guideline:
  - `docs/guidelines/lighthouse-patterns.md`
  - `docs/guidelines/coding-rules.md`
  - `docs/guidelines/domain-design-guidelines.md`
  - `docs/guidelines/application-boundary-guidelines.md`
  - `docs/guidelines/implementation-quality-guidelines.md`
  - `docs/guidelines/self-review-preset.md`
- 確認した直前レビュー:
  - `docs/self-review/milestone6-completion-review-5-codex.md`

## 対応確認

Review 5 の指摘「再生成済み layer の古い chunk build request が残る」は対応済み。

- `WorldMapView` に layer ごとの build version を追加した。
- `InvalidateLayer()` と `EnqueueLayerChunks()` で version を進めるようにした。
- `BuildQueuedChunk()` は request の version が現行 version と一致しない場合、古い request として破棄する。
- EditMode の回帰テスト `WorldMapViewSkipsQueuedChunksFromInvalidatedLayerBuild` を追加した。
- EditMode test で利用する view root / layer root / generated mesh の破棄は、非 PlayMode では `DestroyImmediate` を使うようにした。

## 観点別レビュー

### 1. 整合性

重大度: なし

問題:

新規問題なし。layer 再生成後の古い chunk request は build version で破棄されるため、再追加された同一 `MapLayerId` の build と混ざらない。

原因:

Review 5 で見つかった queue と layer lifecycle の不整合に対して、`WorldMapView` 内部で layer build 世代を所有する形にしたため。

解決案:

追加対応なし。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/WorldMapLayerViewDataTests.cs`

完了条件:

- [x] 無効化前の queued chunk が build されない
- [x] 同一 layer の再追加後も新しい queued chunk だけが build される
- [x] 回帰テストで検証されている

### 2. コード重複

重大度: なし

問題:

新規問題なし。build version 管理は `WorldMapView` に閉じており、別クラスに同じ queue 世代管理を重複実装していない。

原因:

chunk queue の所有者が `WorldMapView` であるため、世代管理も同じクラスに置くのが最小責務である。

解決案:

追加対応なし。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`

完了条件:

- [x] queue invalidation ロジックが複数箇所に分散していない

### 3. 役割重複

重大度: なし

問題:

新規問題なし。`WorldMapView` は view build queue と layer root lifecycle を扱い、Application event の発行や floor 生成判断には踏み込んでいない。

原因:

今回の対応は View 内部の pending request 整合性だけを扱っており、Application / Domain の責務を移動していない。

解決案:

追加対応なし。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapLayerViewRegistry.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldViewRoot.cs`

完了条件:

- [x] Application は View lifecycle を知らない
- [x] View は layer 表示構築と破棄だけを扱う

### 4. コーディングルール

重大度: なし

問題:

新規問題なし。追加コードは block namespace / Allman style / explicit public API / field naming / null guard 方針に沿っている。`DestroyImmediate` は Editor 非 PlayMode cleanup のための Unity lifecycle 分岐であり、Runtime の通常破棄では `Destroy` を使う。

原因:

既存の `WorldActorViewPool` / `WorldDebugMaterialFactory` と同じ Editor cleanup パターンに揃えた。

解決案:

追加対応なし。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapLayerViewRegistry.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldViewRoot.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/WorldMapLayerViewDataTests.cs`

完了条件:

- [x] `uloop.cmd compile --project-path Client` が成功している
- [x] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している

## 検証結果

- `uloop.cmd compile --project-path Client`: 成功、Error 0 / Warning 0
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功、253 passed / 0 failed / 0 skipped
- `uloop.cmd control-play-mode --project-path Client --action Play` -> 30秒待機 -> Stop -> `uloop.cmd get-logs --project-path Client`: `[World] GameWorldState initialized. Facilities=3 DungeonFloors=1 Actors=0` を確認。Error ログ 0 件。Warning は TextTable / Font asset の既存警告 2 件。

## 最終判定

Review 6 で新規指摘なし。ユーザー指定の停止条件「セルフレビューの問題がなくなった時」に到達したため、インデックス6で反復を停止する。

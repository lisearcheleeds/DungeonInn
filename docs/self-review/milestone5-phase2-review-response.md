# Milestone 5 Phase 2 Review Response

## 目的

`docs/self-review/` に追加された Milestone 5 現状レビューを、実装経緯と現在のロードマップに照らして分類する。

この文書では、次の Phase 3 に入る前に対応するもの、Milestone 5 内の後続 Phase で扱うもの、Milestone 6 以降または別リファクタで扱うものを明確にする。

## 対応方針サマリ

| 区分 | 方針 |
|---|---|
| Phase 3 前に対応 | 表示基盤の小さな設計負債・低リスクな性能改善を先に潰す |
| Milestone 5 後続 Phase で対応 | タイルメッシュ、表示専用クエリ、デバッグ可視化の撤去など、ロードマップ上の目的に沿って扱う |
| 将来対応 | Domain / Application の大きな責務整理、戦闘探索最適化、既存 AssetLoader 方針確認などは別枠で扱う |
| 対応不要 / 指摘を再解釈 | 実装経緯上、既に Phase 2 で吸収済みのもの、または現状では違反とは見なさないものを明記する |

## Phase 3 前に対応する項目

### 1. `LayerPositionViewMapper` の固定値を設定クラスへ移動

対象レビュー:

- `milestone5-general-review.md` [総合-6]

現在、`LayerHeightOffset = -240f` と `ActorHeightOffset = 1.5f` が `LayerPositionViewMapper` に直接定義されている。Phase 3 はレイヤー座標と表示座標の扱いが中心になるため、この固定値を残したまま進めると、地上 / ダンジョン / Actor 表示の調整点が散る。

対応:

- `WorldViewSettings` もしくは `LayerPositionViewSettings` を追加する。
- `LayerHeightOffset` と `ActorHeightOffset` を設定クラスへ移す。
- `LayerPositionViewMapper` は設定を注入して座標変換だけを担当する。
- `WorldLifetimeScope` に設定インスタンスを登録する。

期待効果:

- Phase 3 以降の高さ調整、カメラ角度調整、地上 / 地下レイヤー表示の変更が局所化される。

### 2. `WorldActorPresenter` の毎フレーム `HashSet<Guid>` 生成をやめる

対象レビュー:

- `milestone5-performance-review.md` [パフォーマンス-5]

現在、`UpdateVisuals()` のたびに `HashSet<Guid>` を生成している。Actor 数が少ない現在は問題になりにくいが、表示更新は毎フレーム呼ばれるため、Phase 3 前に低リスクで修正しておく。

対応:

- `readonly HashSet<Guid> activeActorIds = new();` をフィールド化する。
- `UpdateVisuals()` 冒頭で `Clear()` して再利用する。

期待効果:

- 不要な GC 発生を減らす。
- 後続の Actor 表示更新処理を増やす前に、Presenter の基本形を整える。

### 3. `WorldDebugMaterialFactory` の Shader フォールバックを安全化する

対象レビュー:

- `milestone5-general-review.md` [総合-1]

Phase 2 で `MapTileVisualConfig` / `ActorSpriteVisualConfig` を導入し、プレースホルダー依存は緩和済み。ただし `WorldDebugMaterialFactory.Create()` は URP Lit と Standard の両方が見つからない場合の扱いが曖昧。

対応:

- `Shader.Find("Universal Render Pipeline/Lit")`、`Shader.Find("Standard")` の順に探索する方針は維持する。
- 両方見つからない場合は明示的に例外を出す、または Unity の環境で利用可能な最小フォールバックを追加する。
- どちらにするかは、実装前に現行 Unity 環境で標準 Shader の取得可否を確認して決める。

期待効果:

- プレースホルダー表示の失敗原因が silent failure にならない。

### 4. Milestone 5 ロードマップへレビュー対応メモを追記

対象レビュー:

- `milestone5-consistency-review.md` [整合-6]
- `milestone5-general-review.md` [総合-4]

`WorldActorDebugVisualizer` は現在、新しい表示実装へ処理を委譲する互換 facade として残している。Feature Flag で切り替える設計にはしていないため、レビュー指摘は「実装漏れ」ではなく「移行期間の扱いを明文化する必要がある」と捉える。

対応:

- Milestone 5 ロードマップに、`WorldActorDebugVisualizer` は互換 facade として保持し、旧 debug 表示の完全撤去は Phase 7 で扱う旨を追記する。

期待効果:

- レビュー時に「旧実装が残っている」ことを未完了と誤認しにくくする。

## Milestone 5 後続 Phase で対応する項目

### 1. `WorldMapView` のタイル生成方式

対象レビュー:

- `milestone5-design-review.md` [設計-7]
- `milestone5-performance-review.md` [パフォーマンス-3]

現在の `GameObject.CreatePrimitive(PrimitiveType.Plane)` によるタイル生成は、Phase 1 / Phase 2 のデバッグ表示置き換え段階として許容している。最終形ではない。

対応:

- Phase 4 でチャンク単位のメッシュ生成へ移行する。
- Material / Prefab 差し替え前提の設定は維持しつつ、描画単位はタイルごとの GameObject ではなく、バッチ効率の良い構造へ寄せる。

### 2. 表示専用の Query / Provider 分離

対象レビュー:

- `milestone5-design-review.md` [設計-2]
- `milestone5-general-review.md` [総合-8]

`IGameWorldStateReader` は Application 層の読み取り境界として導入したもので、UI 表示の全てを集約する意図ではない。Milestone 5 ロードマップにも、View が複雑な集計や整形を直接行わない方針を記載済み。

現時点の `WorldMapView` / `WorldActorPresenter` は、地形と Actor の現在状態を読むだけなので許容範囲。ただし、表示都合の整形や集計が増えた時点で分離する。

対応:

- Phase 3 から Phase 5 の間で、必要になった場合に `IMapViewDataProvider` / `IActorViewDataProvider` のような表示専用 Query を追加する。
- 追加条件は「View が Domain Entity 全体を見て判定・集計し始めた場合」とする。

### 3. Actor 表示種別の解決

対象レビュー:

- `milestone5-design-review.md` [設計-6]

Phase 2 で `ActorSpriteVisualConfig` を導入し、Actor の見た目選択を専用クラスへ移した。ただし内部ではまだ `actor.Behavior.GetType()` に依存している。

対応:

- Phase 5 または戦闘 / 被弾 / 死亡アニメーションに入る前に、`ActorVisualRole` のような表示用分類を導入するか検討する。
- Domain の Behavior 型を View が直接知る形が増える場合は、`ActorVisualRoleResolver` などへ切り出す。

## 将来対応・別枠で扱う項目

### 1. `Resources.LoadAsync` の既存利用

対象レビュー:

- `milestone5-consistency-review.md` [整合-1]

`ProductAssetLoader` の `Resources.LoadAsync` は禁止 API に該当する。ただし Milestone 5 Phase 1 / Phase 2 で追加したものではなく、Lighthouse の初期画面ロード経路と関係する既存実装である。

対応:

- 次フェーズ前の小修正では扱わない。
- Lighthouse の推奨 AssetLoader / ProductAssetLoader 方針を確認したうえで、別リファクタタスクとして扱う。
- Milestone 5 の View 実装では、この経路への依存を増やさない。

### 2. Domain `Actor` の static catalog 依存

対象レビュー:

- `milestone5-design-review.md` [設計-1]

`Actor` が `WeaponTypeCombatMasterCatalog` に依存している点は、Domain 純度の観点では整理余地がある。ただし戦闘計算・装備・Actor 生成にまたがる変更になるため、Milestone 5 の表示置き換え前に破壊的変更として扱う範囲ではない。

対応:

- 戦闘 Phase または Domain 整理 Phase で、`CombatProfile` / `WeaponType` 解決責務の移動を検討する。

### 3. Application 層の UseCase / Service / Orchestrator 整理

対象レビュー:

- `milestone5-design-review.md` [設計-3]
- `milestone5-design-review.md` [設計-4]
- `milestone5-design-review.md` [設計-5]
- `milestone5-design-review.md` [設計-8]
- `milestone5-design-review.md` [設計-9]
- `milestone5-general-review.md` [総合-2]
- `milestone5-general-review.md` [総合-5]

`WorldGameLoopEntryPoint` の依存数、UseCase / Service の命名、宿屋回復処理の責務などは整理余地がある。ただし、Milestone 5 の目的は Sphere / Plane デバッグ表示の置き換えであり、Application の大規模再編は主目的ではない。

対応:

- Milestone 5 中は悪化させない。
- 表示実装のために必要な境界だけ調整する。
- 大きな整理は Milestone 5 完了後、または戦闘表示に入る前の専用リファクタとして扱う。

### 4. 戦闘探索・ターゲット探索の空間分割

対象レビュー:

- `milestone5-performance-review.md` [パフォーマンス-1]
- `milestone5-performance-review.md` [パフォーマンス-2]

O(n^2) 探索は将来的に問題になり得るが、現在の Milestone 5 Phase 3 の直接ブロッカーではない。

対応:

- Actor 数が増える前、または Milestone 6 の戦闘表現に入る前に、Spatial Hash / Grid Index の導入を検討する。

### 5. 毎フレーム LINQ / スナップショット生成 / ログ蓄積

対象レビュー:

- `milestone5-performance-review.md` [パフォーマンス-4]
- `milestone5-performance-review.md` [パフォーマンス-6]
- `milestone5-performance-review.md` [パフォーマンス-7]
- `milestone5-performance-review.md` [パフォーマンス-8]
- `milestone5-performance-review.md` [パフォーマンス-9]
- `milestone5-performance-review.md` [パフォーマンス-10]
- `milestone5-performance-review.md` [パフォーマンス-11]

現時点ではゲーム規模が小さく、Phase 3 前にまとめて最適化するより、表示実装が固まったあとに測定して対応する方が安全。

対応:

- Milestone 5 では新規表示処理で同種の GC を増やさない。
- 大規模マップ、Actor 数増加、戦闘ログ UI 実装時に測定して優先度を決める。

## 対応不要、または指摘を再解釈する項目

### 1. `AdvanceActorAiOrchestrator.MarkEventAsync` の `async` 指摘

対象レビュー:

- `milestone5-consistency-review.md` [整合-3]

現実装は `async` キーワードを使っておらず、`UniTask.CompletedTask` を返している。メソッド名の `Async` は UniTask を返す非同期互換 API としての命名であり、現時点では修正不要。

ただし、処理が完全に同期であり続けることが確定した場合は、将来 `MarkEvent` へリネームする余地はある。

### 2. Phase 2 のプレースホルダー設定不足

対象レビュー:

- `milestone5-general-review.md` [総合-1]

Phase 2 で `MapTileVisualConfig` / `MapMaterialSet` / `ActorSpriteVisualConfig` を追加済み。差し替え前提の入口は作ったため、レビュー指摘の中心部分は対応済み。

残る対応は前述の Shader フォールバック安全化のみ。

### 3. `FirstSceneScene` など既存命名

対象レビュー:

- `milestone5-consistency-review.md` [整合-5]

Lighthouse の Scene 命名や既存構成に関わるため、Milestone 5 Phase 3 前には扱わない。

## Phase 3 前の実施順

1. `LayerPositionViewMapper` の設定クラス化
2. `WorldActorPresenter` の `HashSet<Guid>` 再利用
3. `WorldDebugMaterialFactory` の Shader フォールバック安全化
4. Milestone 5 ロードマップへ `WorldActorDebugVisualizer` の移行扱いを追記
5. コンパイル、EditMode テスト、禁止 API 差分確認

## 完了条件

- Phase 3 に入る前に、表示座標の調整点が設定クラスへ分離されている。
- 毎フレーム Actor 表示更新で不要な `HashSet<Guid>` 生成が残っていない。
- デバッグ Material 生成失敗時の挙動が明確になっている。
- `WorldActorDebugVisualizer` が互換 facade として残っている理由がロードマップ上で説明されている。
- 上記対応後に `uloop.cmd compile --project-path Client` が成功している。

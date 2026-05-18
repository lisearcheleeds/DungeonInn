# Milestone 6 Roadmap

このドキュメントは Milestone 6 の実装計画をまとめる。

## ゴール

- キャラクタースプライトと背景アセットを実際に導入し、単色プレースホルダーから本来の 2.5D RPG 表現に移行する。
- Actor の View 管理を `MonoBehaviour` + Prefab ベースに移行し、後のアセット差し替えを Editor 操作で完結できる運用体制を作る。
- 壁・階段の 3D トポロジーを実装し、2.5D らしいマップ表現にする。
- NavMesh 連携を追加し、Actor が障害物を回避して移動できるようにする。

## 対象範囲

### 対象

- `MapTileVisualConfig` / `MapMaterialSet` / `ActorSpriteVisualConfig` の ScriptableObject 化
- Addressables による Material / Sprite / Prefab のロード（`lighthouse-patterns.md` P5 に従う）
- `ActorView : MonoBehaviour` の導入と Prefab ベースの Actor 生成
- `ActorSpriteAnimator` による idle / walk スプライトアニメーション
- `TileMeshShapeKind.Block` / `Ramp` の実 geometry 実装（壁・階段の立体化）
- テクスチャマテリアルの差し替え基盤
- 環境オブジェクト（小型プロップ）の Prefab 配置基盤
- NavMesh 連携（`INavigationPathProvider` / `UnityNavMeshPathProvider` / NavMesh build）
- `WorldLifetimeScope` DI 登録の整理

### 対象外

- 戦闘演出（combat / hit / dead アニメーション、プロジェクタイル、エリアエフェクト正式表示）
- 宿屋 / 店舗の詳細 UI
- マルチシーン追加
- DropItem 正式表示
- 音声・BGM

---

## アーキテクチャ方針

### Visual Config の ScriptableObject 化

Milestone 5 では `MapTileVisualConfig` / `MapMaterialSet` / `ActorSpriteVisualConfig` がランタイムで `new Material()` / `new Texture2D()` を生成し、DI でインスタンスとして受け渡していた。Milestone 6 ではこれらを ScriptableObject に移行し、Editor 上でアセット参照を設定できる形にする。

- Material / Sprite の参照は Addressables に載せ、ScriptableObject がアドレスを持つ。
- ロード未完了時は既存のプレースホルダーカラー生成コードを fallback として維持する。
- ランタイム生成 Texture / Material の `Dispose()` 経路は、fallback が発動した場合にのみ残す。

### Actor MonoBehaviour 化

現在の `WorldActorView`（plain C# クラス）は `GameObject` と `SpriteRenderer` をラップするだけで、アニメーション制御や Prefab 設定を Inspector から行う手段がない。Milestone 6 では `ActorView : MonoBehaviour` を導入し、Prefab のルートコンポーネントとする。

- `WorldActorViewPool` が `new GameObject()` + `AddComponent<SpriteRenderer>()` をやめ、Prefab を `Instantiate` する。
- `ActorView` MonoBehaviour は Application / Domain の型を知らない（`ActorBehaviorType` 等は引数で渡す）。
- `WorldActorPresenter` が `ActorView` の公開メソッドを通じて操作する（直接 `SpriteRenderer` を触らない）。

### NavMesh と Application 境界

- Application 層に `INavigationPathProvider` インターフェースを置き、`LayerPosition` 配列でパスを受け取る。
- `UnityNavMeshPathProvider` を View / Infrastructure 側に置き、Unity の NavMesh API を呼ぶ。
- Application は UnityEngine に依存しない。

---

## Phase 1: Visual Config の ScriptableObject 化と Addressables 対応

Milestone 5 のランタイム生成 Config を ScriptableObject + Addressables に移行し、アセット差し替えを Editor 操作で完結できる基盤を作る。

### 作るもの

| クラス / アセット | 種別 | 役割 |
|---|---|---|
| `MapTileVisualConfig` | ScriptableObject に移行 | tile ごとの material / shape 設定を Inspector で編集できる |
| `MapMaterialSet` | ScriptableObject に移行 | `TileVisualKind` ごとの Material 参照を保持 |
| `ActorSpriteVisualConfig` | ScriptableObject に移行 | Actor 種別ごとの Sprite / Prefab 参照を保持 |
| `VisualConfigLoader` | Application/View 境界 | ScriptableObject を Addressables 経由でロードし、DI に渡す |

### 対応内容

- `MapMaterialSet` / `ActorSpriteVisualConfig` の `new Material()` / `new Texture2D()` 生成をやめ、ScriptableObject が Inspector で設定した Addressables アドレスを保持する。
- `VisualConfigLoader` が Addressables からロードし、Lighthouse の `IAssetScope` / `IAssetManager` を通じて管理する（`lighthouse-patterns.md` P5 に従う）。
- ロード未完了時・アセット未設定時の fallback は既存のプレースホルダーカラー生成コードを維持する。
- `MapTileVisualConfig` の `TileVisualDefinition` はコンストラクタ引数から ScriptableObject のフィールドに変わるが、外部インターフェース（`Get(TileVisualKind)` → `TileVisualDefinition`）は変えない。

### 完了条件

- [ ] Inspector で Material / Sprite を差し替えるだけで見た目が変わる
- [ ] Addressables ロード経路が `lighthouse-patterns.md` の禁止 API に違反していない（`Resources.Load` 禁止）
- [ ] アセット未設定・ロード未完了でも PlayMode が落ちず fallback 表示できる
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## Phase 2: Actor MonoBehaviour Prefab 化

Actor の View 管理を `MonoBehaviour` + Prefab ベースに移行する。

### 作るもの

| クラス / アセット | 種別 | 役割 |
|---|---|---|
| `ActorView` | MonoBehaviour | Prefab ルートコンポーネント。SpriteRenderer 操作・flipX・アニメーション状態を管理する |
| Actor Prefab | `.prefab` | `SpriteRenderer` + `ActorView` MonoBehaviour を持つ Prefab |
| `WorldActorViewPool` | 更新 | `new GameObject()` → Prefab `Instantiate` に変更 |
| `WorldActorPresenter` | 更新 | `actorView.SpriteRenderer` への直接アクセスをやめ、`ActorView` の公開メソッド経由に変更 |

### 対応内容

- `ActorView : MonoBehaviour` は以下のインターフェースを持つ：
  - `SetSprite(Sprite sprite)` — 表示 sprite を切り替える
  - `SetFlip(bool flipX)` — 左右反転を設定する
  - `SetRotationY(float degrees)` — カメラ yaw 対応の Y 回転を設定する
  - `Reset(Sprite sprite)` — Pool に返却したとき初期化する
- `WorldActorViewPool` は Phase 1 で ScriptableObject 化した `ActorSpriteVisualConfig` に登録された Prefab を `Instantiate` して `ActorView` MonoBehaviour を返す。
- `WorldActorView`（plain C# クラス）は段階的に廃止する。`ActorView` MonoBehaviour が同等の責務を持つ。
- Pool 返却時は `SetActive(false)` して pool に戻す（現在の挙動を維持）。

### 設計制約

- `ActorView` MonoBehaviour は `ActorBehaviorType` / `Guid` など Application / Domain の型を知らない。
- MonoBehaviour への DI は `[Inject]` ではなく `WorldActorViewPool.Rent()` 経由で `WorldActorPresenter` に渡す。
- `WorldActorPresenter` が `ActorView` の公開メソッドだけを呼ぶ（SpriteRenderer / Transform の直接操作は `ActorView` 内部に閉じる）。

### 完了条件

- [ ] Actor GameObject が Prefab `Instantiate` で生成されている
- [ ] `WorldActorViewPool` が `new GameObject()` / `AddComponent<SpriteRenderer>()` を持たない
- [ ] `WorldActorPresenter` が `SpriteRenderer` を直接参照していない
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## Phase 3: Actor スプライトアニメーション

idle / walk のスプライトアニメーションを実装する。

### 作るもの

| クラス / アセット | 種別 | 役割 |
|---|---|---|
| `ActorSpriteAnimator` | MonoBehaviour または plain class | フレーム配列 + FPS でスプライトを切り替える |
| `ActorAnimationState` | enum | Idle / Walk（Milestone 7 以降: Combat / Hit / Dead） |
| `ActorSpriteAnimationClip` | ScriptableObject | フレーム配列・FPS・ループ設定を保持する |
| Actor 種別ごとのアニメーション定義 | ScriptableObject | Adventurer / Monster それぞれの Idle/Walk clip 参照 |

### 対応内容

- `ActorSpriteAnimator` はフレーム配列を受け取り、`Update` で経過時間を加算してフレームを切り替える。
- `ActorView` MonoBehaviour が `ActorSpriteAnimator` を保持し、`SetAnimationState(ActorAnimationState state)` で状態を渡す。
- `WorldActorPresenter` が Actor の移動状態（facing 更新あり → Walk、なし → Idle）を判定し `SetAnimationState()` を呼ぶ。この判定は View 層に閉じる。
- Sprite の方向選択は NE / NW / SE / SW の 4 方向を Milestone 6 で実装する。8 方向への拡張入口として `ActorSpriteAnimationClip` に方向フレーム配列を持てる設計にする。
- アニメーションフレームは Phase 1 で Addressables からロードした Sprite 配列を使う。

### 実装前に確認すること

- idle / walk 各方向のフレーム数とアセット形式（スプライトシート or 個別スプライト）。
- アニメーション FPS の初期値（仮: 8fps）。
- Unity の `Animator` コンポーネントを使うか、フレーム手動切替にするか。

### 完了条件

- [ ] Actor が静止中は idle フレームが再生され、移動中は walk フレームが再生される
- [ ] カメラ回転時にアニメーションが破綻しない
- [ ] フレーム配列・FPS を差し替えると別のアニメーションが再生できる
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## Phase 4: Map 3D トポロジーと背景テクスチャ

Block / Ramp のメッシュトポロジーを実装し、テクスチャ付きマテリアルを適用する。

### 対応内容

**メッシュトポロジー:**

- `MapMeshBuildService` の `AddPlane()` に加え、`AddBlock()` / `AddRamp()` を実装する。
- `TileMeshShapeKind.Block` → 上面 + 4 側面のボックスジオメトリを生成する（壁が立体的に見える）。
- `TileMeshShapeKind.Ramp` → 傾斜面のジオメトリを生成する（階段が傾斜形状に見える）。
- 頂点バッファの容量見積もりを Block / Ramp 追加に合わせて更新する。

**テクスチャ:**

- Phase 1 で ScriptableObject 化した `MapMaterialSet` から Addressables ロードした Material を使う。
- 床・壁・階段・施設予定地それぞれに独自テクスチャを設定できる。
- 1 chunk 内の submesh を material ごとに分ける既存方針を維持する。

**環境オブジェクト（小型プロップ）:**

- `EnvironmentObjectPlacer` を追加し、特定 `TileVisualKind` に対応した Prefab を tile 位置に配置する。
- 配置は chunk 生成時に行い、chunk 削除時に対応オブジェクトも削除する。
- 配置量は 1 tile に 1 オブジェクト以内に留め、大量配置時は GPU Instancing 対応 Material を検討する。
- 初期実装は柱・松明・木などの小型プロップを想定する（3D モデル Prefab）。

### 完了条件

- [ ] 壁タイルが平面ではなくボックスジオメトリで表示される（側面が見える）
- [ ] 階段タイルが傾斜形状で表示される
- [ ] テクスチャ付き Material を差し替えると見た目が変わる
- [ ] プロップ Prefab が tile 位置に配置される
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## Phase 5: NavMesh 連携

Actor が NavMesh を使って障害物を回避しながら移動する。

### 作るもの

| クラス | 層 | 役割 |
|---|---|---|
| `INavigationPathProvider` | Application | `LayerPosition start/goal` を受け取り `LayerPosition[]` のパスを返す |
| `UnityNavMeshPathProvider` | View / Infrastructure | Unity の `NavMesh.CalculatePath` を呼んで Application 座標に変換して返す |
| `NavMeshBuildService` | View | chunk mesh 生成後に対象 layer の NavMesh を bake する |

### 対応内容

- `INavigationPathProvider` は Application 層に置き、Domain / Application は UnityEngine に依存しない。
- `UnityNavMeshPathProvider` が NavMesh Agent を使わず `NavMesh.CalculatePath` を直接呼ぶ（Actor ごとに Agent を持たない）。
- `NavMeshBuildService` が `WorldMapView` の chunk 生成後に `NavMeshSurface`（または NavMesh バッチ API）を呼ぶ。
- Actor の移動は Application 側の移動計算（`ActorMoveUseCase` 相当）を経由し続ける。NavMesh パスは移動先 grid 列の解決に使う。
- NavMesh がない layer（生成前・対応外）では Application の直線移動 fallback を使い、ゲームが落ちない。
- ダンジョン再生成時は対象 layer の NavMesh を rebuild する（`WorldMapView` の chunk 再生成トリガーと連動させる）。

### 実装前に確認すること

- NavMesh Agent サイズ設定（Actor の collider 幅に相当する bake パラメータ）。
- NavMesh Surface の範囲（Ground 全域 / Dungeon 各 floor 独立 / 動的 bake）。
- Application 側の移動計算が NavMesh パスを受け取る形になっているか確認する。

### 完了条件

- [ ] Actor が壁タイルを貫通せず NavMesh 経路に沿って移動する
- [ ] ダンジョン再生成後も NavMesh が正しく更新される
- [ ] NavMesh なし layer でゲームが落ちない（fallback 移動が機能する）
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## Phase 6: WorldLifetimeScope DI 整理

Milestone 5 完了時点で 60+ に達した `WorldLifetimeScope` の DI 登録を、機能グループ別に整理する。Milestone 6 の新規追加クラスも含めて整理する。

### 対応内容

- 登録クラスを以下の大グループに分類し、SubScope または partial 抽出を検討する：
  - World シミュレーション（UseCase / Orchestrator / Service）
  - World ビュー共通（Presenter / Mapper / View Config）
  - Actor 管理（ViewRegistry / Pool / Animator）
  - Map 表示（MeshBuildService / LayerViewRegistry / NavMeshBuildService）
- `lifetime-scope-game-loop-design.md` に分割方針とグループ定義を追記する。
- Milestone 6 で追加する ScriptableObject ロード / MonoBehaviour 登録も同文書に記載する。

### 完了条件

- [ ] `WorldLifetimeScope` の直接登録数が適切な粒度になっている
- [ ] Milestone 6 の新規登録クラスが適切な scope に登録されている
- [ ] `lifetime-scope-game-loop-design.md` に分割方針が追記されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## 推奨実装順

1. Phase 1: Visual Config ScriptableObject 化（Phase 2〜4 の前提）
2. Phase 2: Actor MonoBehaviour Prefab 化（Phase 3 の前提）
3. Phase 3: Actor スプライトアニメーション（Phase 2 が完了してから）
4. Phase 4: Map 3D トポロジーと背景テクスチャ（Phase 1 完了後、Phase 5 と並行可）
5. Phase 5: NavMesh 連携（Phase 4 の chunk mesh が完成してから）
6. Phase 6: WorldLifetimeScope DI 整理（全 Phase 完了後）

Phase 4 と Phase 5 は依存しないが、NavMesh の bake 対象となる walkable mesh が Phase 4 で確定するため、Phase 4 完了後に Phase 5 に進む。

---

## Milestone 7 へ移動する項目

以下は Milestone 6 のスコープ外と判断した項目。

- 4 方向スプライトは Milestone 6 で実装済み
- 8 方向スプライト化（Milestone 7 以降）
- 戦闘アニメーション（combat / hit / dead）
- 正式 Walk フレーム差し替え（現在は 2 フレームの仮実装）
- DropItem 正式表示
- 音声・BGM

---

## Milestone 6 完了条件サマリ

- [ ] Phase 1: Inspector 上でアセットを差し替えると見た目が変わる。Addressables 経路で Material / Sprite をロードできる
- [ ] Phase 2: Actor が Prefab Instantiate で生成され、`ActorView : MonoBehaviour` が SpriteRenderer を管理している
- [ ] Phase 3: Actor に idle / walk アニメーションが適用されている
- [ ] Phase 4: 壁・階段が立体的に表示され、テクスチャマテリアルが適用されている
- [ ] Phase 5: Actor が NavMesh に沿って壁を回避して移動している
- [ ] Phase 6: `WorldLifetimeScope` の DI 登録が整理されている
- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している
- [ ] 一定時間の PlayMode 実行で Error / Warning が出ていない

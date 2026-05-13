# Milestone 5 Roadmap

このドキュメントは Milestone 5 の実装計画をまとめる。

Milestone 5 では、Milestone 4 までに作った内部シミュレーションを Unity の正式な View 表現へ接続する。
ゴールは、現在 `WorldActorDebugVisualizer` が表示している debug 用の Sphere / Plane を、差し替え可能な実ゲーム向け表現へ置き換えること。

UI、デザイン済みボタン、詳細な操作パネル、NavMesh は Milestone 5 の主対象にしない。

## ゴール

- Ground map と Dungeon map が debug Plane ではなく、3D メッシュの 2.5D RPG 表現として表示される。
- Actor が debug Sphere ではなく、SpriteRenderer ベースの 2D SpriteAnimation として表示される。
- Actor の生成、削除、移動、向きが `GameWorldState` と同期して画面上で確認できる。
- マップ表示はタイル単位 GameObject の大量生成を避け、バッチが効く構成にする。
- View は Domain / Application の状態を反映するだけで、ゲーム判定の主体にならない。
- Milestone 5 完了時点で `WorldActorDebugVisualizer` の Sphere / Plane 表示を撤去できる。

## 対象範囲

対象:

- Ground map の表示
- Dungeon floor の表示
- Actor の SpriteRenderer 表示
- Actor の移動・向き・基本状態アニメーション
- Orthographic camera の移動・回転
- 表示用 prefab / material / sprite asset 設定の差し替え基盤
- DebugVisualizer から正式 View への置き換え

対象外:

- NavMesh 連携
- 本格 UI / ボタンデザイン / 管理画面
- Projectile / AreaEffect / DropItem の正式表示
- 宿屋・店舗の詳細 UI
- 3D Actor モデル
- 戦闘判定や経営判定を Unity Collider / GameObject 検索へ移すこと

## 表現方針

### マップ

- 表現ジャンルは 2.5D RPG。
- カメラは orthographic。
- マップ自体は 3D メッシュで表示する。
- タイル状に敷き詰めた見た目にするが、タイルごとに GameObject を生成しない。
- 実行時は、一定範囲ごとの chunk mesh を生成し、material ごとに結合する。
- 床、壁、階段、施設などの見た目は `TileVisualDefinition` のような設定データで差し替え可能にする。
- tile prefab を実体として大量配置するのではなく、設定データから chunk mesh を生成する。
- 高パフォーマンスを優先するため、tile prefab の大量配置ではなく、chunk mesh + shared material + 必要に応じた GPU instancing を基本方針にする。

### Actor

- Actor は 3D モデルではなく、SpriteRenderer による SpriteAnimation を基本方針にする。
- ただし、実装前に改めて確認すること:
  - idle / walk / combat / hit / dead を Milestone 5 でどこまで入れるか。
  - 4方向、8方向、または少ない方向数 + 反転で表現するか。
- カメラが回転できるため、Actor の見た目方向は「Actor のワールド向き」と「カメラ yaw」の相対角で選ぶ。
- 例えば Actor が東を向いている場合、カメラが南側から見ている時と北側から見ている時で、必要に応じて sprite を反転または別方向 sprite に切り替える。
- Actor は GameObject / SpriteRenderer 参照を Domain に持たせない。
- View 側が `ActorId` をキーに GameObject を管理する。

### Camera

- Orthographic camera を使う。
- WASD でカメラ平面移動できる。
- マウス操作でカメラを回転できる。
- カメラ操作は表示操作であり、Domain / Application のゲーム進行には影響させない。
- カメラ回転後も Actor sprite の向きが破綻しないように、Actor visual 側で camera-relative な sprite selection を行う。

### Asset

- 現時点で正式アセットはない。
- Milestone 5 では placeholder sprite / material / mesh を用意し、後から差し替え可能な構成にする。
- placeholder はゲーム進行確認に十分な視認性を優先する。
- アセットロードは Lighthouse の asset loading 方針に従い、禁止 API を使わない。

## アーキテクチャ方針

- Domain / Application は UnityEngine、GameObject、SpriteRenderer、Mesh、Material、Camera に依存しない。
- View は GameObject の生成・削除・描画・補間・カメラ操作だけを担当する。
- ゲーム上の位置、移動結果、戦闘結果、宿泊結果は引き続き Domain / Application が決める。
- 表示の補間は View 側で行ってよいが、補間結果を Domain の正としない。
- Scene 上の手動配置が必要な root / camera / installer は最小限に留め、生成される map / actor object は View 側の管理下に置く。

### 表示情報の取得経路

すべての表示情報を `IGameWorldStateReader` に集約しない。
表示の種類ごとに、以下の経路を使い分ける。

World GameObject 表示:

- `IGameWorldStateReader` を主な現在状態の参照元にする。
- 対象は map、dungeon floor、Actor の現在位置、Actor の現在状態、施設の現在状態など。
- 「今そこに何があるか」を同期する用途に限定する。
- View が `IGameWorldStateReader` から複雑な経済集計や UI 用整形を行わない。

瞬間演出:

- GameEvent 購読を使う。
- 対象は攻撃、被弾、死亡、アイテム取得、売却、宿泊料支払い、レベルアップなど。
- `GameWorldState` には最終状態しか残らないため、「いつ何が起きたか」が必要な表示は Event を起点にする。
- Milestone 5 では Projectile / AreaEffect / DropItem の正式表示は対象外だが、Actor animation の combat / hit / dead を扱う場合は Event 起点を基本にする。

UI 表示:

- UI は専用の Query UseCase / ReadModel / Status DTO を使う。
- 既存の例として `GetInnEconomyStatusUseCase`、`GetInnEconomyReportUseCase`、`GetGameEventHistoryUseCase`、`GetGameTimeStateUseCase` がある。
- UI が `IGameWorldStateReader` を直接読んで集計・整形し始めないようにする。
- Milestone 5 では本格 UI は対象外だが、将来 UI が必要になった場合は表示目的ごとの Query を追加する。

入力・操作:

- 入力は Command UseCase に渡す。
- View / UI が Domain Entity を直接変更しない。

## Phase 1: View 表現基盤と DebugVisualizer 分解

目的:

- 現在の `WorldActorDebugVisualizer` を正式 View へ移行できる単位に分解する。
- Sphere / Plane の debug 表現に閉じた責務を取り除き、map / actor / camera の独立した presenter に分ける。

作るもの:

- `WorldViewRoot`
- `WorldMapView`
- `WorldActorViewRegistry`
- `WorldActorPresenter`
- `WorldCameraController`
- View 用の設定クラスまたは ScriptableObject
- `LayerPositionViewMapper`

対応内容:

- DebugVisualizer が持っている root 生成、tile 表示、actor 表示を分離する。
- `WorldGameLoopEntryPoint` から毎 frame 更新する対象を正式 View presenter へ置き換える準備をする。
- 既存の debug 表示をすぐ削除せず、移行中は feature flag または明確な差し替え手順で併存できるようにする。
- `LayerPosition` から Unity world position への変換責務を `WorldActorDebugVisualizer` から分離する。

完了条件:

- map / actor / camera の View 責務が分離されている。
- 座標変換責務が独立し、map mesh と Actor 表示の両方から再利用できる。
- Domain / Application に Unity 表示責務が漏れていない。
- 既存 PlayMode 起動が壊れていない。

実装状況:

- 2026-05-13 完了。
- `WorldActorDebugVisualizer` は互換用ファサードとして残し、map 表示を `WorldMapView`、Actor 表示を `WorldActorPresenter` / `WorldActorViewRegistry`、root 管理を `WorldViewRoot`、座標変換を `LayerPositionViewMapper` へ分離した。
- `WorldCameraController` を登録し、Phase 5 の camera control 実装先を確保した。
- 既存の debug Sphere / Plane 表示は維持しており、Phase 4 / Phase 6 で正式表示へ置き換える。
- `uloop.cmd compile --project-path Client` 成功。
- `uloop.cmd run-tests --project-path Client --test-mode EditMode` 成功。215 件 passed。
- PlayMode を短時間起動し、`WorldGameLoop` 初期化、Actor spawn、AI ログを確認。Error 0 件。

## Phase 2: Placeholder Asset / Visual Config 最小基盤

目的:

- 正式アセットがない状態でも、Map / Actor 表示の実装を進められる最小の placeholder と設定基盤を用意する。
- Phase 3 以降が仮の素材参照や直書き material に依存しないようにする。

作るもの:

- `MapTileVisualConfig`
- `MapMaterialSet`
- `TileVisualDefinition`
- `ActorSpriteVisualConfig`
- placeholder sprite
- placeholder material

対応内容:

- 床、壁、階段、施設予定地の placeholder material を用意する。
- Actor 種別ごとの placeholder sprite を用意する。
- map tile の見た目は tile prefab 実体ではなく、mesh 生成に使う `TileVisualDefinition` として定義する。
- `TileVisualDefinition` には material、mesh 形状種別、uv / color など、chunk mesh 生成に必要な情報を持たせる。
- config から sprite / material / mesh 定義を参照できるようにする。
- asset 未設定時の fallback 表示方針を決める。
- アセットの direct load 禁止ルールに違反しない。

完了条件:

- コードを書き換えずに placeholder material / sprite を差し替えられる。
- map mesh 生成と Actor sprite 表示が同じ config 基盤を参照できる。
- asset 未設定でも PlayMode が落ちず、fallback 表示できる。

実装状況:

- 2026-05-13 完了。
- `TileVisualKind` / `TileMeshShapeKind` / `TileVisualDefinition` を追加し、map tile の見た目を実体 prefab ではなく mesh 生成向け定義として扱う入口を作った。
- `MapMaterialSet` / `MapTileVisualConfig` を追加し、床、壁、階段、施設予定地の placeholder material と tile 定義を DI で参照できるようにした。
- `ActorSpriteVisualConfig` を追加し、Actor 種別ごとの placeholder 表示定義を DI で参照できるようにした。現時点では既存 Sphere 表示用 material を返し、Phase 6 で SpriteRenderer / sprite 参照へ拡張する。
- `WorldMapView` と `WorldActorPresenter` は直書き material 生成をやめ、VisualConfig 経由で placeholder 表示を取得するようにした。
- `uloop.cmd compile --project-path Client` 成功。
- `uloop.cmd run-tests --project-path Client --test-mode EditMode` 成功。215 件 passed。
- PlayMode を短時間起動し、VisualConfig の DI 解決と初期化ログを確認。Error 0 件。

## Phase 3: 座標変換 / 表示 Layer 管理

目的:

- Ground と Dungeon の表示座標、表示 root、表示対象 layer の扱いを先に確定する。
- Map mesh 生成と Actor 表示が同じ座標変換を使うようにする。

作るもの:

- `LayerPositionViewMapper`
- `MapLayerViewRegistry`
- 表示中 layer の切り替え処理
- layer root 管理

対応内容:

- `MapLayerId.Ground` と dungeon floor layer を Unity world 上でどう配置するか決める。
- layer ごとの root GameObject を作り、map mesh と Actor sprite を同じ layer root 配下に置けるようにする。
- 表示対象 layer の map mesh を表示し、対象外 layer は非表示または別 root に分離する。
- Actor の所属 layer に応じて表示 root を切り替える。
- Dungeon floor が増えた場合も、必要な layer だけ mesh を生成できるようにする。
- 表示切り替えはゲーム進行に影響させない。

完了条件:

- Ground 上の Actor と Dungeon 上の Actor が不正な位置に重なって表示されない設計になっている。
- map mesh と Actor sprite が同じ座標変換を使う。
- ダンジョン階層の表示切り替え方針が実装可能な粒度で決まっている。

## Phase 4: Map Chunk Mesh 生成基盤

目的:

- Ground map と Dungeon floor を 3D メッシュとして表示する。
- タイルごとの GameObject 生成を避け、chunk mesh で高パフォーマンスな表示基盤を作る。

作るもの:

- `MapMeshBuildService`
- `MapChunkMesh`
- `GroundMapPresenter`
- `DungeonMapPresenter`

対応内容:

- Domain の `MapLayer` / `DungeonFloor` / cell 情報から表示用 mesh data を生成する。
- chunk サイズを定義する。
- material ごとに submesh または chunk を分け、描画バッチが効くようにする。
- 床、壁、階段、施設予定地などの tile visual を placeholder material で表示する。
- `TileVisualDefinition` を使って mesh 形状と material を決める。
- タイルごと collider は作らない。必要な場合も表示確認用に限定し、ゲーム判定には使わない。

完了条件:

- Ground map が debug Plane なしで表示される。
- Dungeon floor が debug Plane なしで表示される。
- 表示 GameObject 数が map cell 数に比例して大量増加しない。
- Material / mesh 設定を差し替えれば見た目を変更できる。

実装前に確認すること:

- chunk サイズ。
- material 分割単位。
- 壁、階段、施設予定地を最初の mesh topology でどこまで表現するか。

## Phase 5: Orthographic Camera 操作

目的:

- 2.5D RPG 表示を確認しやすい camera control を作る。

作るもの:

- `WorldCameraController`
- `WorldCameraSettings`

対応内容:

- Orthographic camera を使う。
- WASD で平面移動する。
- マウス操作で yaw 回転する。
- zoom は必要に応じて mouse wheel で追加する。
- カメラ回転角を Actor sprite selection に渡せるようにする。
- UI 実装は行わない。操作確認用の最低限の入力のみ扱う。

完了条件:

- WASD で表示範囲を移動できる。
- マウスでカメラを回転できる。
- カメラ回転後も map / actor の表示関係が読める。

## Phase 6: Actor SpriteRenderer 表示

目的:

- Actor を debug Sphere ではなく SpriteRenderer で表示する。
- Actor の生成、削除、移動、向きが画面上で追える状態にする。

作るもの:

- `ActorSpriteView`
- `ActorSpriteAnimator`
- `ActorSpriteVisualConfig`
- `ActorFacingResolver`
- `ActorViewRegistry`

対応内容:

- `ActorId` と Actor GameObject を対応付ける。
- Actor 生成時に SpriteRenderer GameObject を生成する。
- Actor 削除時に GameObject を破棄する。
- Actor の `LayerPosition` を Unity world position に変換して同期する。
- Actor の移動は Domain/Application の位置を正とし、View 側で必要に応じて補間する。
- Actor の向きは actor movement vector または将来の facing state から解決する。
- カメラ yaw と Actor facing の相対角から sprite direction / flip を選ぶ。

実装前に再確認すること:

- Actor の向きを Domain 状態として追加するか、View 側で直近移動方向を保持するか。
- Milestone 5 で idle / walk / combat / hit / dead のどこまでを実装するか。
- 4方向 sprite、8方向 sprite、または左右反転中心の少数 direction で始めるか。
- Actor 種別ごとの placeholder sprite をどの程度分けるか。

完了条件:

- Actor 数と Actor GameObject 数が一致する。
- Spawn / despawn / movement が表示上で追える。
- カメラ回転時に sprite の向きが大きく破綻しない。
- Domain Actor が GameObject / SpriteRenderer 参照を持っていない。

## Phase 7: Debug Sphere / Plane の撤去

目的:

- Milestone 5 のゴールとして、debug primitive 表示を正式 View に置き換える。

対応内容:

- `WorldActorDebugVisualizer` の Sphere / Plane 生成を停止する。
- 新しい map mesh view / actor sprite view を標準表示にする。
- 古い debug 表示が必要な場合は、明示的な debug option として分離する。

完了条件:

- PlayMode で debug Sphere / Plane が通常表示されない。
- Ground map / Dungeon map / Actor が新 View で表示される。
- Error / Warning なしで一定時間 PlayMode 実行できる。
- `uloop.cmd compile --project-path Client` が成功する。
- `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する。

## Milestone 6 へ移動する項目

NavMesh 連携は Milestone 5 から外し、Milestone 6 の主対象候補にする。

Milestone 6 候補:

- `INavigationPathProvider` / `UnityNavMeshPathProvider`
- 表示中 map chunk からの NavMesh build
- Actor の NavMesh 経路追従
- NavMesh がない layer での Domain/Application 移動 fallback
- ダンジョン再生成時の NavMesh rebuild

理由:

- Milestone 5 の主目的は debug 表示の置き換えであり、移動判定の高度化ではない。
- 先に map mesh と Actor sprite 表示を固めることで、NavMesh の bake 対象と可視確認環境が明確になる。
- NavMesh を先に入れると、表示基盤・移動品質・判定責務の問題が同時に混ざる。

## 推奨実装順

1. Phase 1: View 表現基盤と DebugVisualizer 分解
2. Phase 2: Placeholder Asset / Visual Config 最小基盤
3. Phase 3: 座標変換 / 表示 Layer 管理
4. Phase 4: Map Chunk Mesh 生成基盤
5. Phase 5: Orthographic Camera 操作
6. Phase 6: Actor SpriteRenderer 表示
7. Phase 7: Debug Sphere / Plane の撤去

Actor の向きの扱い、Actor animation の詳細、戦闘 / 被弾 / 死亡 sprite の対応範囲は、Phase 6 の作業開始前にユーザーへ確認する。

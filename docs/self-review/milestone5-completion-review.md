# Milestone 5 完了前レビュー

作成日: 2026-05-13

## 目的

Milestone 5 の完了前チェックとして、現在の実装を以下の4観点でレビューする。

- 設計
- 整合性
- パフォーマンス
- 総合

Milestone 5 の主目的である「debug Sphere / Plane を、World 用 Camera / 3D mesh map / SpriteRenderer actor 表示へ置き換える」点は概ね達成している。ただし、Milestone 6 に進む前に整理した方がよい構造上の課題が複数残っている。

## 結論

Milestone 5 は表示置き換えとしては完了可能な水準にある。一方で、Milestone 6 で NavMesh、移動品質、戦闘表示、イベント表示を積み増す前に、以下を優先して対応するのが妥当。

1. World 入力を Lighthouse の InputLayer 経由に戻す。
2. `WorldGameLoopEntryPoint` からゲーム進行順序を Application 層へ移す。
3. `WorldActorDebugVisualizer` を削除し、正式な View 更新経路へ移す。
4. Actor SpriteAnimation の完了条件を「Milestone 5では表示・向き・反転まで」と明文化する、または最小アニメーションを追加する。
5. PlayMode smoke test の証跡を残す。

## 設計レビュー

### 1. View 層がゲーム進行の順序を握っている

重大度: 高

問題:

`WorldGameLoopEntryPoint` が多数の UseCase / Orchestrator を直接注入し、spawn、lifecycle、combat、projectile、area effect、item、inn recovery、report 発行までの順序を View 層で固定している。Unity の `Update()` 起点ではあるが、実質的にはゲーム進行パイプラインの中核になっている。

原因:

Unity のフレーム更新と Application 層のシミュレーション進行が分離されていない。Milestone 5 では表示置き換えを優先したため、既存の一時的な呼び出し順序が `WorldGameLoopEntryPoint` に集約された。

解決案:

Application 層に `WorldSimulationOrchestrator` または `AdvanceWorldFrameUseCase` を追加し、UseCase の実行順序をそこへ移す。`WorldGameLoopEntryPoint` は起動、停止、delta time の受け渡し、View 更新の呼び出しに限定する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `docs/guidelines/usecase-boundary-guidelines.md`

### 2. カメラ / レイヤー切替入力が Lighthouse の InputLayer を迂回している

重大度: 高

問題:

`WorldCameraController` と `WorldLayerViewController` が `new InputAction(...)` を生成して常時 `Enable()` している。これにより、Lighthouse の `IInputLayer` / InputActionMap stack / modal 入力制御の外側で入力を拾う。

原因:

Milestone 5 の操作確認を優先し、カメラ移動、マウス回転、ズーム、Q/E 階層切替を Controller 内に閉じ込めた。

解決案:

`InputActions.Scene` に World camera / layer 操作用 Action を定義し、`WorldSceneInputLayer` から入力状態または command を Controller へ渡す。将来 UI や modal が重なった時に入力遮断できる経路へ戻す。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldCameraController.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLayerViewController.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Input/Layer/WorldSceneInputLayer.cs`
- `docs/guidelines/lighthouse-patterns.md`

### 3. 戦闘死亡処理の依存方向とイベント順序に不安がある

重大度: 高

問題:

`CombatDamageResolver` が `ActorDefeatOrchestrator` に依存しており、Resolver から上位 Orchestrator へ処理が逆流している。死亡時の状態変更、経験値付与、ドロップ、イベント発行順序が読み取りにくい。

原因:

Milestone 4 の戦闘整理で Orchestrator / Resolver / Service を導入したが、「ダメージ適用」「死亡確定」「報酬・ドロップ」「イベント発行」の境界がまだ完全には分離されていない。

解決案:

`CombatDamageResolver` はダメージ適用結果または死亡候補を返すだけにする。死亡時の削除、経験値、ドロップ、イベント発行順は上位 Application Orchestrator で一括制御する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatDamageResolver.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/ActorDefeatOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatDefeatResolver.cs`
- `docs/guidelines/usecase-boundary-guidelines.md`

### 4. WorldCamera の契約が手動設定に依存している

重大度: 中

問題:

シーン上に serialized `worldCamera` がある通常経路では、orthographic、culling mask、初期位置、初期角度は Inspector 設定を正とする。これはユーザー意図には合っているが、`WorldCameraSettings` が初期値も持つため、どちらが正なのか読み取りにくい。

原因:

「Camera を Inspector で調整したい」という要求に合わせ、シーン設定を尊重するよう変更した一方で、fallback camera 用の設定値も残っている。

解決案:

方針を明文化する。推奨は、Scene の `WorldCamera` がある場合は Inspector 設定を正とし、`WorldCameraSettings` は操作速度、回転感度、ズーム範囲など runtime control の設定に限定すること。最低限 `cullingMask = World` と `orthographic = true` は起動時検証または `OnValidate` で保証する。

ユーザー確認後方針:

`WorldCameraSettings` は、ユーザーがゲーム中の設定画面から変更できる操作感・ズーム範囲などに限定する。ゲーム中に設定できない初期位置、初期角度、projection、culling mask などは Inspector / Scene 設定を正とする。ただし、Lighthouse / URP camera stack の接続や fallback camera 生成など、実行時に動的変更が必要な契約はコード側で扱う。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldScene.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldCameraController.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldCameraSettings.cs`
- `Client/Assets/DungeonInn/Runtime/Scene/MainScene/World.unity`

## 整合性レビュー

### 1. Actor SpriteAnimation の完了条件と実装がずれている

重大度: 高

問題:

ロードマップ上は Actor を SpriteAnimation で表示する想定だが、現状は単色 placeholder sprite の生成、位置同期、向き更新、カメラ相対の左右反転までで、idle / walk のアニメーション実体はない。

原因:

Milestone 5 の主目的が debug Sphere の置き換えだったため、SpriteRenderer 表示基盤を優先した。Phase 6 の記述には `ActorSpriteAnimator` などの名前が残っているが、実装は `WorldActorPresenter` / `WorldActorView` に集約されている。

解決案:

Milestone 5 の完了条件を「SpriteRenderer による placeholder 表示、位置同期、向き、カメラ相対反転まで」と修正する。idle / walk / combat / hit / dead の animation は Milestone 6 以降へ移す。もし Milestone 5 で完全一致させるなら、最小の idle / walk frame 切替を追加する。

根拠:

- `docs/roadmap/milestone5-roadmap.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorView.cs`

### 2. `WorldActorDebugVisualizer` が正式表示更新の名前として残っている（対応済み）

重大度: 中

問題:

Phase 7 後も `WorldActorDebugVisualizer` が DI 登録され、毎フレームの正式表示更新経路として使われている。旧 debug primitive 生成は消えているが、名前が現在の責務と一致していない。

原因:

移行用 facade として残したクラスが、正式な View 更新入口に変化した。

解決案:

`WorldViewUpdater`、`WorldViewPresenter`、`WorldVisualPresenter` などへリネームする。Debug 表示が将来必要なら、別の debug 専用クラスとして分離する。

対応:

移行用 facade 自体が不要になっていたため、`WorldActorDebugVisualizer` を削除した。毎フレームの正式表示更新は `WorldGameLoopEntryPoint` から `WorldMapView` / `WorldActorPresenter` を直接呼び出す。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorDebugVisualizer.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`

### 3. self-review の古い指摘が解決済みか未解決か追いにくい（対応済み）

重大度: 低

問題:

`docs/self-review/` の Milestone 5 関連レビューには、Phase 1 時点の古い指摘が残っている。たとえば `WorldMapView` の tile GameObject 大量生成や `HashSet` 毎フレーム生成は後続対応で一部解消済みだが、最終状態が読み取りにくい。

原因:

レビュー履歴は追記型で残っているが、最終ステータスの `resolved / deferred / still-open` がまとまっていない。

解決案:

本ファイルを Milestone 5 完了前レビューのサマリとし、今後のレビューでは項目ごとに `対応済み`、`Milestone 6へ延期`、`要対応` を明示する。

対応:

本ファイルを Milestone 5 self-review の最終ステータス一覧として扱う。古い個別レビューは履歴として残し、現在の判断は下表を正とする。

| 元レビュー | 指摘 | 最終ステータス |
|---|---|---|
| `milestone5-design-review.md` | Domain static catalog 依存 | 別タスクとして追跡 |
| `milestone5-design-review.md` / `milestone5-general-review.md` | `WorldGameLoopEntryPoint` の責務過多 | Milestone 6へ延期 |
| `milestone5-design-review.md` | Service / UseCase / Orchestrator の責務整理 | 一部対応済み、残りは Milestone 6へ延期 |
| `milestone5-design-review.md` | `WorldMapView` の GameObject 大量生成 | 対応済み |
| `milestone5-design-review.md` | 戦闘死亡処理の依存方向とイベント順序 | 対応済み |
| `milestone5-consistency-review.md` | 既存 `Resources.LoadAsync` / `SceneManager.LoadSceneAsync` | 別タスクとして追跡 |
| `milestone5-consistency-review.md` | `AdvanceActorAiUseCase` 旧名 | 対応済み |
| `milestone5-consistency-review.md` / `milestone5-general-review.md` | `WorldActorDebugVisualizer` の互換 facade | 対応済み |
| `milestone5-performance-review.md` | 戦闘探索 O(n^2) / Line of Sight サンプリング | 対応済み |
| `milestone5-performance-review.md` | Area target 全走査 | Milestone 6へ延期 |
| `milestone5-performance-review.md` | Combat / Projectile / AreaEffect の毎フレーム List 複製 | 要対応 |
| `milestone5-performance-review.md` | `WorldActorPresenter` の毎フレーム `HashSet` 生成 | 対応済み |
| `milestone5-performance-review.md` | Actor 表示の全 Actor 毎フレーム更新 | Milestone 6へ延期 |
| `milestone5-performance-review.md` | Chunk mesh 同期生成スパイク | Milestone 6へ延期 |
| `milestone5-general-review.md` | 表示アセット差し替え基盤 | Milestone 6へ延期 |
| `milestone5-general-review.md` | 暫定 TODO / Master data 整理 | Milestone 6へ延期 |

根拠:

- `docs/self-review/milestone5-design-review.md`
- `docs/self-review/milestone5-consistency-review.md`
- `docs/self-review/milestone5-performance-review.md`
- `docs/self-review/milestone5-general-review.md`
- `docs/self-review/milestone5-phase2-review-response.md`

### 4. 設計 docs に古い実装名が残っている（対応済み）

重大度: 中

問題:

AI 関連 docs に `AdvanceActorAiUseCase` の記述が残っている一方、実装は `AdvanceActorAiOrchestrator` になっている。Milestone 6 で AI / 移動 / NavMesh を扱う際に参照元として混乱しやすい。

原因:

実装整理後に設計 docs 側の名称更新が追いついていない。

解決案:

`docs/design/actor-ai-desing.md` と `docs/design/lifetime-scope-game-loop-design.md` を現行実装名へ更新する。旧名は履歴として残す場合も、現在の正は `AdvanceActorAiOrchestrator` であることを明記する。

対応:

`docs/design/actor-ai-desing.md` と `docs/design/lifetime-scope-game-loop-design.md` の `AdvanceActorAiUseCase` 記述を `AdvanceActorAiOrchestrator` へ更新した。現在の AI 評価入口は `AdvanceActorAiOrchestrator`、Decision 適用は `ApplyActorAiDecisionUseCase` を正とする。

根拠:

- `docs/design/actor-ai-desing.md`
- `docs/design/lifetime-scope-game-loop-design.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/AdvanceActorAiOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`

## パフォーマンスレビュー

### 1. 毎フレームでゲーム進行全体が走る（一部対応済み）

重大度: 高

問題:

`Update()` ごとに表示更新だけでなく、戦闘検出、戦闘進行、Projectile、AreaEffect、Item処理、回復、帰還判定まで直列実行される。Actor 数や Effect 数が増えるとフレーム時間が直接悪化する。

原因:

`WorldGameLoopEntryPoint.Update()` が毎フレーム `TickAsync()` を起動し、`TickAsync()` 内でスケジュール tick に依存しない処理も毎回呼んでいる。pause 時も多くの処理呼び出し自体は残る。

解決案:

表示更新、リアルタイム戦闘、スケジュール tick、イベント駆動処理を分離する。pause 時は camera 以外を止める。戦闘・Projectile・AreaEffect は対象が存在する時だけ回す。

対応:

pause 中も UI 操作や非時間依存の自動判断は許可する方針とし、`GameLoopUseCase` と View 更新は従来通り毎フレーム実行する。`WorldGameLoopEntryPoint` では、pause 中に Actor 移動 / 戦闘進行 / Projectile / AreaEffect / ActorEffect / 宿回復のような時間進行系のみをスキップする。あわせて、Projectile / AreaEffect / Item が存在しない場合の明確に副作用がない呼び出しを省く。ゲーム進行パイプラインを Application 層へ移す大きな責務整理は Milestone 6 へ残す。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/GameLoopTickRequest.cs`

### 2. 戦闘検出が O(n^2) + Line of Sight サンプリングになっている（対応済み）

重大度: 高

問題:

Actor 数が増えると、毎フレーム全 Actor 同士を走査し、候補ごとに視線判定も行うため急激に重くなる。

原因:

`DetectCombatEncounterUseCase` が全 Actor を外側で回し、`FindNearestHostile()` で再度全 Actor を走査している。近距離候補には `HasLineOfSight()` の距離計算と複数セルチェックも発生する。

解決案:

Layer 別・セル別の Spatial Index を導入し、近傍セルだけ探索する。戦闘候補は dirty / event / 一定間隔で再評価し、全 Actor 毎フレーム検出を避ける。Line of Sight 結果は短時間キャッシュする。

対応:

`DetectCombatEncounterUseCase` の全 Actor 総当たり探索を `CombatEncounterTargetResolver` へ分離し、毎回の検出開始時に Layer 別・セル別の Spatial Index を一度だけ再構築して、探索対象を同一 Layer の近傍セルに限定する。Line of Sight は成功した Actor ペアのみ `IGameClock.CurrentScheduleTick` が進むまでキャッシュし、同一 schedule tick 内の再サンプリングを避ける。戦闘検出自体の dirty / event 化はゲーム進行パイプライン整理と合わせて Milestone 6 へ残す。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/DetectCombatEncounterUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatEncounterTargetResolver.cs`

### 3. 毎フレーム GC Alloc と O(n) 削除が複数ある

重大度: 高

問題:

`new List<T>(...)`、`new List<Actor>()`、`new List<Guid>()`、`new GameLoopTickRequest(...)` がフレーム中に発生する箇所があり、GC spike の原因になる。削除も `FindIndex()` で二重に線形探索している。

原因:

コレクション変更回避のために都度コピーしている。`GameWorldState.RemoveXxx()` は Dictionary 削除後に List を再検索している。

解決案:

再利用バッファ、削除 ID queue、末尾 swap-remove、ID to index 管理を使う。Projectile / AreaEffect は逆順 loop か削除 list 再利用にする。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceCombatUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceProjectileUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceAreaEffectUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/GameWorldState.cs`

### 4. Actor 表示が全 Actor を毎フレーム更新する

重大度: 中

問題:

Actor 数に比例して、毎フレーム `SpriteRenderer.sprite`、parent確認、localPosition、rotation、flip更新、欠損Actor走査が発生する。

原因:

`WorldActorDebugVisualizer.UpdateVisuals()` が毎フレーム `WorldActorPresenter.UpdateVisuals()` を呼び、Presenter が全 Actor を無条件更新している。

解決案:

spawn / despawn / layer移動 / 位置変更 / camera yaw変更を dirty 化し、変更 Actor だけ更新する。sprite は変化時のみ設定し、削除バッファは Registry の field で再利用する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorDebugVisualizer.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorViewRegistry.cs`

### 5. Chunk mesh 生成がメインスレッド同期でスパイク化する

重大度: 中

問題:

新しい Dungeon floor が増えたタイミングで、chunk GameObject、Mesh、Material 配列、Normals / Bounds 再計算が一括実行され、ロードや探索中に hitch が出る可能性がある。

原因:

`WorldMapView.UpdateVisuals()` が毎フレーム不足 layer を確認し、未構築 layer を見つけると全 chunk を同期生成する。`MapMeshBuildService` は chunk ごとに List / Dictionary / Mesh を新規生成する。

解決案:

floor 追加イベントで生成 queue に入れ、1 frame あたりの chunk 生成数を制限する。MeshData / バッファを再利用し、静的平面なら法線や Bounds を明示して `RecalculateNormals()` を避ける。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMeshBuildService.cs`

### 6. World 描画に対して URP 設定が重い

重大度: 中

問題:

現在の World は flat な chunk mesh と SpriteRenderer が中心だが、HDR、影、Depth / Opaque texture、SSAO など高コスト機能が有効な設定になっている可能性がある。

原因:

プロジェクト共通の URP 設定をそのまま使っている。

解決案:

World 用の軽量 Renderer / Quality を用意し、不要な Depth / Opaque / HDR / Shadows / SSAO を切る。map material は Unlit / shared 前提に寄せる。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scene/MainScene/World.unity`
- `Client/Assets/Settings/PC_RPAsset.asset`
- `Client/Assets/Settings/PC_Renderer.asset`

## 総合レビュー

### 1. Lighthouse 禁止 API が既存コードに残っている

重大度: 重大

問題:

`Resources.LoadAsync` と `SceneManager.LoadSceneAsync` の直接利用が残っている。今回の Milestone 5 作業で追加されたものではないが、完了前ゲートとしては明確に例外扱いする必要がある。

原因:

`ProductAssetLoader` と `Launcher` が Lighthouse 移行前提の暫定経路として残っている。

解決案:

Milestone 6 前または Milestone 6 の最初に、別リファクタタスクとして扱う。ScreenStack 生成は Lighthouse / IAssetScope 系へ、reboot 経路は Lighthouse scene 遷移経路へ寄せる。すぐ直さない場合は「既存違反・今回追加なし」と明記してトラッキングする。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/Launcher.cs`
- `docs/guidelines/lighthouse-patterns.md`

### 2. Phase 7 の PlayMode 検証証跡が不足している

重大度: 高

問題:

Phase 7 完了条件には「PlayMode で debug Sphere / Plane なし」「Error / Warning なし」があるが、ロードマップ上の実装状況には最終 PlayMode 検証の記録が十分に残っていない。

原因:

compile / EditMode test 中心の検証で、World 表示の統合・視覚確認が自動化されていない。

解決案:

PlayMode smoke test または uLoop PlayMode 確認ログを追加する。確認項目は、World scene 起動、WorldCamera / UI Camera 分離、chunk mesh 生成、Actor Sprite 生成、debug primitive 不在、Error 0 件とする。

根拠:

- `docs/roadmap/milestone5-roadmap.md`
- `docs/guidelines/review-policy-guideline.md`

### 3. 表示アセット差し替え基盤が実運用には弱い

重大度: 中

問題:

ロードマップでは「コードを書き換えずに差し替え」としているが、現状は色、Texture、Sprite、Material を C# 側で生成している。アーティストや調整担当が Unity Inspector 上で差し替えにくい。

原因:

Milestone 5 では placeholder 表示を優先し、ScriptableObject や serialized asset 参照ではなく scoped 登録された C# 設定クラスで生成している。

解決案:

`MapMaterialSet` / `ActorSpriteVisualConfig` を serialized asset または scene serialized config へ寄せ、未設定時だけコード生成 fallback にする。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMaterialSet.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSpriteVisualConfig.cs`
- `docs/roadmap/milestone5-roadmap.md`

### 4. Milestone 6 前に片付けるべき暫定 TODO が残っている

重大度: 中

問題:

Spawn 間隔、上限、Faction、移動速度などの TODO が残っている。NavMesh へ進むと、移動品質の問題と master data 未整備の問題が混ざる。

原因:

Master data 整備が Milestone 6 の前提作業としてまだ明示されていない。

解決案:

Milestone 6 開始前に「暫定定数の正式 Master 化」または「Milestone 6 では触らない TODO 一覧」を作る。NavMesh の評価対象に混ぜるものと混ぜないものを明確に分ける。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/SpawnScheduledMonsterOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/SpawnScheduledAdventurerOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/MoveActorTowardDestinationUseCase.cs`

## 推奨対応順

### Milestone 5 完了前に対応推奨

1. `WorldActorDebugVisualizer` を削除し、正式な View 更新経路へ移す。（対応済み）
2. `docs/roadmap/milestone5-roadmap.md` の完了条件を現状に合わせて更新する。（対応済み）
3. Phase 7 の PlayMode smoke test 証跡を追記する。
4. `WorldCamera` の culling mask / orthographic 契約をコードまたは OnValidate で保証する。

### Milestone 6 の最初に対応推奨

1. World camera / layer 操作を `WorldSceneInputLayer` 経由へ移す。
2. `WorldGameLoopEntryPoint` からゲーム進行パイプラインを Application 層へ移す。
3. 毎フレーム GC Alloc と Area target 全走査を改善する。
4. Asset 差し替え基盤を serialized config / ScriptableObject 化する。

### 別タスクとして追跡

1. 既存の `Resources.LoadAsync` / `SceneManager.LoadSceneAsync` の置き換え。
2. AI docs の旧名更新。（対応済み）
3. Spawn / Faction / Move speed などの TODO 整理。
4. URP 軽量 Renderer / Quality の検討。

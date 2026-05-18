# Milestone 6 完了レビュー 第2回 Codex

作成日: 2026-05-18

## レビュー範囲

第1回統合レビュー `docs/self-review/milestone6-completion-review-1-total.md` とは別に、確認範囲を広げて再レビューした。

確認した範囲:

- `git diff HEAD` のステージ済み差分
- `docs/guidelines/` 配下のレビュー必読 guideline
- `docs/roadmap/milestone6-roadmap.md`
- `docs/design/` 配下の関連設計資料
- `docs/self-review/` 配下の既存レビュー
- `Client/Assets/DungeonInn/Runtime/Scripts` 配下の現行実装
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `Client/Assets/AddressableAssetsData/`
- `Client/Assets/DungeonInn/Runtime/StaticResources/Visual/`
- `Client/Assets/DungeonInn/Tests/`

## 既存レビューとの関係

第1回統合レビューの [A] NavMesh 再生成、[B] ログ/一時ファイル staging、[C] EnvironmentObjectPlacer の Prefab 化、[F] roadmap の方向 sprite 記述矛盾は妥当。ここでは同じ内容を主指摘として再掲せず、今回の追加確認で新しく根拠が取れた問題を中心に記録する。

## 検証結果

```text
uloop.cmd compile --project-path Client
```

結果:

```text
Success: true
ErrorCount: 0
WarningCount: 0
```

```text
uloop.cmd run-tests --project-path Client --test-mode EditMode
```

結果:

```text
Success: true
TestCount: 245
PassedCount: 245
FailedCount: 0
SkippedCount: 0
```

注: 1回目の test 実行は compile 中扱いで失敗し、再実行で成功した。

## 差分分類

production 契約変更として扱う差分:

- `INavigationPathProvider` の追加
- `ActorNavigationService` constructor の変更
- `WorldLifetimeScope` の DI 登録変更
- `ActorView`, `ActorPrefabSource`, `ActorSpriteSet`, `ActorSpriteVisualConfigSO`, `MapMaterialSetSO`, `VisualConfigLoader`, `VisualConfigSettings`, `NavMeshBuildService`, `UnityNavMeshPathProvider`, `EnvironmentObjectPlacer` の追加
- `WorldMapView`, `WorldActorPresenter`, `WorldActorViewPool`, `WorldActorViewRegistry`, `MapMeshBuildService` の公開挙動変更
- Addressables 設定、StaticResources Visual asset の追加

許可理由:

- Milestone 6 roadmap に Actor Prefab 化、Visual Config ScriptableObject 化、Addressables、NavMesh 連携、Map 3D 表示が明記されている。
- Unity 依存実装は View / Editor 側に置かれ、Domain は UnityEngine / NavMesh に依存していない。
- `INavigationPathProvider` は Application 側の能力 interface として配置され、Unity 実装は `UnityNavMeshPathProvider` に閉じている。

不許可または要修正と判断する差分:

- Map Material / texture の Addressables 設定が未完了のまま、Milestone 6 の「テクスチャマテリアル適用」完了条件を満たしているように見える差分。
- Addressables group schema / default group が未整備のまま、Addressables 経路の完了条件を満たしているように見える差分。
- roadmap の animation config 方針と現行実装の乖離が、task 側の変更として十分に同期されていない差分。

## 新規概念追加ゲート

### 追加された主な型

- `INavigationPathProvider`
- `UnityNavMeshPathProvider`
- `NavMeshBuildService`
- `VisualConfigLoader`
- `VisualConfigSettings`
- `MapMaterialSetSO`
- `ActorSpriteVisualConfigSO`
- `ActorSpriteSet`
- `ActorAnimationDirection`
- `ActorView`
- `ActorPrefabSource`
- `EnvironmentObjectPlacer`

### 既存の類似概念

- `IActorNavigationService` / `ActorNavigationService`
- `AStarPathfinder`
- `MapMaterialSet`
- `ActorSpriteVisualConfig`
- `MapTileVisualConfig`
- 旧 `WorldActorView`
- `WorldActorViewPool`
- `WorldActorViewRegistry`
- `WorldMapView`

### 意味差分

- `INavigationPathProvider` は Application 移動処理から外部経路探索能力を呼ぶための境界であり、`IActorNavigationService` は Actor ごとの path state 管理を持つ。
- `UnityNavMeshPathProvider` は Unity NavMesh API と座標変換だけを扱い、Application の path state を持たない。
- `NavMeshBuildService` は View の生成済み mesh から NavMesh を bake する責務で、移動判断や Actor 状態は持たない。
- `VisualConfigLoader` は Addressables load と asset lifetime を扱い、`MapMaterialSet` / `ActorSpriteVisualConfig` はロード済み結果を表示設定として解決する。
- `ActorView` は MonoBehaviour と SpriteRenderer 操作を閉じ込め、旧 `WorldActorView` の plain C# wrapper を置き換える。

### 代替しなかった理由

- `AStarPathfinder` だけでは Unity NavMesh による表示中 layer の障害物回避品質を表現できない。
- `MapMaterialSet` / `ActorSpriteVisualConfig` に直接 Addressables load を持たせると、表示設定解決と async asset lifetime が混ざる。
- 旧 `WorldActorView` では Prefab / Inspector / MonoBehaviour 経由の sprite 表示設定に対応しにくい。

### 統合・削除条件

- NavMesh をゲームルールに使わず表示品質向上だけに閉じる方針が維持される限り、`INavigationPathProvider` と `IActorNavigationService` は分離する。
- 将来、Actor / Item / Projectile などの Visual Config が共通 asset load service に統合される場合、`VisualConfigLoader` の責務を分割または統合する。
- Actor animation が正式な `ActorSpriteAnimator` / `ActorSpriteAnimationClip` へ移行した場合、`ActorSpriteSet` の固定 4方向/2フレーム配列は統合または削除する。

---

### 1. Map texture assets が Addressables / MapMaterialSetSO に接続されていない

重大度: 高

問題:

Milestone 6 roadmap は、Phase 1 / Phase 4 で「Addressables 経由で Material / Sprite をロードできる」「テクスチャ付き Material を差し替えると見た目が変わる」ことを完了条件にしている。差分には `Client/Assets/DungeonInn/Runtime/Art/Textures/Map/*.png` が追加されているが、`MapMaterialSet.asset` の `entries` は空で、Addressables group にも `Textures/Map` や map material の address が存在しない。

この状態では、map 表示は `MapMaterialSet` の fallback debug material に戻り、追加された map texture assets は runtime 表示に接続されない。`MapMeshBuildService` は `mapMaterialSet.Get(visualDefinition.Kind)` を呼んでいるが、`VisualConfigLoader.GetMaterial(kind)` は `MapMaterialSetSO.Entries` が空のため何も返せない。

原因:

`VisualAssetSetup.CreateOrLoadMapMaterialSO()` が `MapMaterialSetSO` を作成するだけで、`TileVisualKind` ごとの `MaterialAddress` を登録していない。`EnsureAddressablesGroup()` も Adventurer / Goblin の sprite だけを `RegisterAnimationSet()` しており、map texture / material を Addressables に登録していない。

解決案:

暫定対応:

- `MapMaterialSet.asset` に `TileVisualKind` ごとの entry を追加する。
- map 表示で使う Material asset を作成し、`GroundWalkable`, `GroundBlocked`, `DungeonWalkable`, `DungeonBlocked`, `StairUp`, `StairDown`, `Facility` の address を設定する。
- `VisualAssetSetup` に map material の作成/登録を追加する。

根治対応:

- texture png を直接 Material として扱わず、専用 Material asset を作成して Addressables に登録する。
- `MapMaterialSetSO.Entry.FallbackColor` を実際に fallback 生成へ反映するか、不要なら削除する。
- `MapMaterialSetSO` の entries が空の場合に warning を出し、PlayMode で「texture 適用完了」と誤判定しないようにする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/StaticResources/Visual/MapMaterialSet.asset`
- `Client/Assets/DungeonInn/Runtime/Art/Textures/Map/DungeonBlocked.png`
- `Client/Assets/DungeonInn/Runtime/Art/Textures/Map/DungeonWalkable.png`
- `Client/Assets/DungeonInn/Runtime/Art/Textures/Map/GroundBlocked.png`
- `Client/Assets/DungeonInn/Runtime/Art/Textures/Map/GroundWalkable.png`
- `Client/Assets/DungeonInn/Runtime/Art/Textures/Map/StairDown.png`
- `Client/Assets/DungeonInn/Runtime/Art/Textures/Map/StairUp.png`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/VisualConfigLoader.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMaterialSet.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMeshBuildService.cs`
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `docs/roadmap/milestone6-roadmap.md`

完了条件:

- [ ] `MapMaterialSet.asset` の `entries` に全 `TileVisualKind` の Material address が登録されている
- [ ] `Client/Assets/AddressableAssetsData/AssetGroups/DungeonInn Visual.asset` に map material address が含まれている
- [ ] `VisualConfigLoader.LoadMaterialsAsync()` が少なくとも 1 件以上の Material をロードする PlayMode / EditMode 検証がある
- [ ] `MapMeshBuildService.BuildChunk()` が fallback debug material ではなく Addressables 由来 Material を使うケースのテストまたは PlayMode 証跡がある
- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している

---

### 2. Addressables group が schema なしで作成されている

重大度: 高

問題:

`Client/Assets/AddressableAssetsData/AssetGroups/DungeonInn Visual.asset` は sprite entries を持っているが、`m_SchemaSet.m_Schemas` が空になっている。また `AddressableAssetSettings.asset` の `m_DefaultGroup` も空である。

Addressables の group は通常、BundledAssetGroupSchema / ContentUpdateGroupSchema などの schema を持ち、build path / load path / bundle mode を定義する。現状は entries があっても group の build/load 設定がなく、Packed mode や player build で asset load が成立する証跡がない。

原因:

`VisualAssetSetup.EnsureAddressablesGroup()` が `settings.CreateGroup(AddressablesGroupName, false, false, false, null)` を呼んでおり、schema template や default schema を渡していない。`Packed Assets.asset` の template は存在するが、`DungeonInn Visual.asset` には適用されていない。

解決案:

暫定対応:

- `DungeonInn Visual` group に BundledAssetGroupSchema / ContentUpdateGroupSchema を追加する。
- 必要なら `AddressableAssetSettings.DefaultGroup` を設定する。
- Editor 上で Addressables Analyze / Build または PlayMode Script 設定に沿ったロード確認を行う。

根治対応:

- `VisualAssetSetup.EnsureAddressablesGroup()` が既存の packed group template を使って group を作るようにする。
- schema が空の group を検出した場合は自動修復するか、warning ではなく setup failure として扱う。

根拠となるファイルリスト:

- `Client/Assets/AddressableAssetsData/AddressableAssetSettings.asset`
- `Client/Assets/AddressableAssetsData/AssetGroups/DungeonInn Visual.asset`
- `Client/Assets/AddressableAssetsData/AssetGroupTemplates/Packed Assets.asset`
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `docs/guidelines/lighthouse-patterns.md`
- `docs/roadmap/milestone6-roadmap.md`

完了条件:

- [ ] `DungeonInn Visual.asset` の `m_SchemaSet.m_Schemas` が空ではない
- [ ] BundledAssetGroupSchema の build/load path が Local profile に接続されている
- [ ] Addressables group 作成処理が schema なし group を生成しない
- [ ] Addressables 経由で Adventurer / Goblin sprite をロードする検証がある
- [ ] Addressables 経由で map material をロードする検証がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 3. Actor animation の roadmap 契約と実装契約が同期されていない

重大度: 中

問題:

Milestone 6 roadmap は `ActorSpriteAnimator`, `ActorAnimationState`, `ActorSpriteAnimationClip` を作るものとして挙げ、フレーム配列・FPS・ループ設定を差し替え可能にする完了条件を持っている。一方、現行実装は `WorldActorPresenter` の `WalkFrameRate = 4f` と `ActorSpriteSet` の固定 4方向 / 2 walk frame 配列で sprite を選択している。

この実装は task_0003 の「仮素材の 4方向 idle/walk」には合っているが、roadmap の「Animator / Clip / FPS 差し替え」契約とは一致しない。第1回統合レビューでは方向 sprite の scope 矛盾を指摘しているが、今回確認した範囲では animation 設定そのものの契約差分も残っている。

原因:

ユーザー提供素材に合わせて task_0003 が具体実装へ寄ったが、roadmap の抽象設計が更新されていない。結果として、実装が正しいのか、roadmap が古いのか、完了判定時に判断が割れる状態になっている。

解決案:

短期対応:

- Milestone 6 の完了条件を「固定 4方向 / 2 frame の仮 animation まで」と明記する。
- `ActorSpriteAnimator`, `ActorAnimationState`, `ActorSpriteAnimationClip` は Milestone 7 以降の正式 animation 差し替え task に移す。
- `WalkFrameRate` が固定値であることを task / roadmap に明記する。

根治対応:

- roadmap 通りに `ActorSpriteAnimationClip` を ScriptableObject として追加し、FPS / loop / direction frames を asset 側で管理する。
- `WorldActorPresenter` は移動/停止の状態判定だけを行い、frame 更新は `ActorSpriteAnimator` または `ActorView` 側に閉じる。

根拠となるファイルリスト:

- `docs/roadmap/milestone6-roadmap.md`
- `tasks/task_0003.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSpriteSet.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSpriteVisualConfigSO.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorAnimationDirection.cs`

完了条件:

- [ ] roadmap と task の animation 完了条件が一致している
- [ ] Milestone 6 で固定 4方向/2 frame を正式完了とする場合、`ActorSpriteAnimator` / `ActorSpriteAnimationClip` が Milestone 7 以降へ延期として記録されている
- [ ] roadmap 通りに対応する場合、FPS / frame 配列を Inspector から差し替えられる
- [ ] `WorldActorPresenter` が animation frame の長期責務を持つかどうかが docs に記録されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 4. Milestone 6 の中核 View 挙動に対するテスト証跡が不足している

重大度: 中

問題:

EditMode test は 245 件成功しているが、今回の中核差分である以下の挙動を直接検証するテストがほぼない。

- `VisualConfigLoader` が Addressables から sprite / material をロードする
- `MapMaterialSetSO` の entries と Addressables group の address が一致する
- `MapMeshBuildService` が Block / Ramp geometry を期待頂点数・submesh 数で生成する
- `WorldMapView` が chunk build 完了後に NavMesh bake を呼ぶ
- dungeon floor 再生成時に chunk / NavMesh が rebuild される
- `WorldActorPresenter` が camera yaw と facing から方向 sprite を選択する

そのため、compile / tests が成功しても、今回見つかった Map texture 未接続や Addressables group schema 欠落を検出できていない。

原因:

既存テストは Application orchestration や view data snapshot の検証が中心で、Unity View asset / Editor setup / Addressables config の検証が不足している。Milestone 6 は asset pipeline と View 表示を大きく触っているが、テスト粒度がそれに追従していない。

解決案:

短期対応:

- YAML / asset 設定を読む EditMode test を追加し、`MapMaterialSet.asset` の entries が空でないこと、Addressables group に必要 address があることを検証する。
- `MapMeshBuildService` は UnityEngine `Mesh` を生成するだけなので、EditMode test で頂点数・submesh 数・material 数を検証する。
- `ActorSpriteSet` / direction 判定は pure に近い形へ切り出すか、少なくとも Presenter 周辺の PlayMode test で検証する。

根治対応:

- Milestone 完了レビューの必須証跡に、compile / EditMode tests だけでなく「asset 設定整合 test」または「Addressables load smoke test」を追加する。
- `VisualAssetSetup` が生成する asset を検証する Editor test を追加する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Tests/EditMode/WorldMapLayerViewDataTests.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/GameLoopTests.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/WorldGameLoopEntryPointArchitectureTests.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/VisualConfigLoader.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMeshBuildService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `docs/guidelines/self-review-preset.md`

完了条件:

- [ ] `MapMaterialSet.asset` の entries が空でないことを検証するテストがある
- [ ] Addressables group に sprite / map material address があることを検証するテストがある
- [ ] `MapMeshBuildService` の Plane / Block / Ramp の geometry を検証するテストがある
- [ ] NavMesh bake trigger の検証がある
- [ ] dungeon floor 再生成時の rebuild を検証するテストまたは PlayMode 証跡がある
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している

---

### 5. Editor 自動セットアップが domain reload ごとに asset 設定へ副作用を持つ

重大度: 低

問題:

`VisualAssetSetup.AutoSetup()` は `[InitializeOnLoadMethod]` で実行され、必要 asset が存在する場合でも毎回 `SetupAddressables()` を呼ぶ。`SetupAddressables()` は group 作成/entry 移動の可能性を持ち、最後に `AssetDatabase.SaveAssets()` と log 出力を行う。

Editor 起動や domain reload ごとに asset database へ副作用を持つ処理が走るため、意図しない Addressables 差分やログノイズを生む可能性がある。今回の Addressables group schema 欠落のように setup が不完全な場合、reload のたびに不完全設定を維持・再生成する経路にもなる。

原因:

初期導入の利便性を優先し、Editor menu 実行と domain reload 自動実行の責務が同じ `RunSetup()` / `SetupAddressables()` に近い形で置かれている。自動実行時に「差分が必要か」を判定する guard が弱い。

解決案:

短期対応:

- `AutoSetup()` は asset 不足時だけ `RunSetup()` を呼び、Addressables の再設定は menu か明示的な validation に寄せる。
- 自動実行を残す場合は、schema / entries / settings が不足しているときだけ保存する。

根治対応:

- `Validate Visual Assets` と `Setup Visual Assets` を分け、validation は副作用なし、setup は menu 実行のみとする。
- CI / EditMode test で validation を実行し、Editor reload では asset を変更しない。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `Client/Assets/AddressableAssetsData/AssetGroups/DungeonInn Visual.asset`
- `Client/Assets/AddressableAssetsData/AddressableAssetSettings.asset`

完了条件:

- [ ] domain reload だけでは `AddressableAssetsData` に差分が出ない
- [ ] `VisualAssetSetup` に副作用なし validation 経路がある
- [ ] setup が必要な場合だけ `AssetDatabase.SaveAssets()` が呼ばれる
- [ ] Addressables group schema 欠落を validation で検出できる

---

## 最終判定

第1回統合レビューの要修正 [A][B] に加えて、今回の追加レビューでは以下を Milestone 6 完了前の要修正として扱うべきと判断する。

- [ ] 指摘 1: Map texture assets を `MapMaterialSetSO` / Addressables / Material asset に接続する
- [ ] 指摘 2: `DungeonInn Visual` Addressables group に schema を設定し、Addressables load 経路の完了条件を満たす

指摘 3 は、roadmap と task のどちらを正とするかの整理が必要。実装を task_0003 基準で完了とするなら、roadmap を更新して正式 animation config を Milestone 7 以降へ延期する。

指摘 4 / 5 は Milestone 6 完了前に一部対応が望ましいが、少なくとも未対応項目として次タスクへ追跡する必要がある。

## レビュー完了チェック

- [x] レビュー前に必読資料を確認した
- [x] 差分許可モデルで production 契約変更に該当する差分を分類した
- [x] 許可した production 契約変更について、責務・境界・寿命・依存方向の理由を記録した
- [x] 新規概念追加ゲート対象について、既存類似概念・意味差分・代替不可理由・統合削除条件を記録した
- [x] 設計・整合性・パフォーマンス・重複・総合の観点を確認した
- [x] 各レビュー項目に問題・原因・解決案・根拠となるファイルリスト・完了条件を書いた
- [x] 完了条件が機械的に確認できる形になっている
- [x] 未対応 / 一部対応 / 延期 / ユーザー判断待ち / 別タスク化済みを区別して記録した
- [x] レビュー結果を `docs/self-review/` 配下に保存した
- [x] `uloop.cmd compile --project-path Client` が成功した
- [x] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功した

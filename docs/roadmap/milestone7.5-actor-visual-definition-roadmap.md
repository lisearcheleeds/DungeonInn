# Milestone 7.5 Roadmap - Actor Visual Definition

## Goal

Actor の見た目を `ActorView.prefab` に固定せず、見た目スキン単位の Visual Definition として Addressable から逐次ロードできる構造へ移行する。

これにより、同じ `ActorView` prefab を使い回しながら、剣士・魔法使い・ドラゴン・ミミックなどの見た目ごとに、アニメーション枚数、FPS、表示サイズ、方向差分を自由に変えられるようにする。

## Background

現在の `ActorView.prefab` は `ActorSpriteAnimationClip` を Idle / Walk / Combat / Hit / Dead のような動作単位で直接参照している。

この構造では、Prefab が「汎用 Actor 表示器」ではなく、特定の見た目のアニメーション断片を知る形になっている。Actor 種別やスキンごとに別のアニメーションを持たせたい場合、Prefab 参照や `ActorBehaviorType` ベースの Sprite 解決が増え、見た目差し替えの責務が分散する。

Milestone 7.5 では、Actor の見た目を `ActorVisualDefinitionSO` に集約し、`ActorArchetypeMaster.visualId` と `ActorVisualMaster(visualId, skinId)` から Addressable address を解決する。

## Scope

### Included

- `ActorVisualDefinitionSO` の追加
- `ActorVisualMaster` の追加
- `ActorArchetypeMaster` への `visualId` 追加
- `skinId = 0` 固定解決の導入
- `ActorAnimationState` の名称整理
  - `Combat` -> `Attack`
  - `Hit` -> `Damage`
- `Idle / Walk / Work / Attack / Damage / Dead` のアニメーションキー対応
- `NE / NW / SE / SW` の方向差分対応
- Actor visual の Addressable 逐次ロード
- `ActorView` / `ActorSpriteAnimator` / `WorldActorPresenter` の Visual Definition ベース化
- Pool 再利用時に visual / state / sprite が正しくリセットされることの確認
- Sprite が null の場合に Warning を出し、表示は透明のまま継続する

### Excluded

- 課金スキン選択 UI
- Actor 個体ごとの `skinId` 保存
- SpriteAtlas への全面移行
- 装備差し替えによる見た目合成
- AnimationClip / Animator Controller への移行
- 全 Actor visual の起動時プリロード

## Confirmed Decisions

- `ActorView.prefab` は 1 つの Actor を表示する汎用 Prefab とする。
- 見た目はスキン単位の `ActorVisualDefinitionSO` で表す。
- `ActorArchetypeMaster` は `visualId` だけを持つ。
- `ActorVisualMaster` は `(visualId, skinId)` で引く。
- 現在は `skinId = 0` を固定で渡して解決する。
- `ActorVisualMaster` は `ActorVisualDefinitionSO` の Addressable address を持つ。
- `ActorVisualDefinitionSO` は Sprite を直接参照する。
- 将来、複数キャラクターの Sprite を 1 つの SpriteAtlas にまとめる可能性はあるが、Milestone 7.5 では Sprite 直接参照で実装する。
- Animation state は `Idle / Walk / Work / Attack / Damage / Dead` とする。
- 方向は isometric view 用に `NE / NW / SE / SW` とし、全 animation key で必要とする。
- Visual 定義は逐次ロードとする。GameObject 生成直後に一時的に透明表示になることは許容する。
- Sprite が null の場合は Warning を出す。代替白画像や placeholder sprite は作らない。
- `ActorView` は将来 pool される前提で、見た目変更と再利用に耐える API にする。

## Required Guidelines

実装タスク作成前に以下を再確認する。

- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/guidelines/debugging-policy.md`
- `docs/guidelines/self-review-guidelines.md`

AGENTS.md には `docs/guidelines/self-review-preset.md` も必須として記載されているが、2026-05-23 時点で同ファイルは存在しない。実装時に存在しないままであれば、その事実を作業ログに記録する。

## Architecture Rules

- Domain は `ScriptableObject` / `Sprite` / Addressables API / View 型を知らない。
- `ActorArchetypeMaster.visualId` はデータ値として扱い、Unity asset 参照を持たない。
- `ActorVisualMaster` は Master 層の不変データとして `visualId` / `skinId` / `visualDefinitionAddress` を持つ。
- Addressable ロードは Lighthouse の `IAssetScope` 経由に限定する。
- `Addressables.LoadAssetAsync` / `Resources.Load` を直接使わない。
- `ActorView` は Addressable を直接ロードしない。
- Visual Definition のロード責務は World 表示側の loader / repository / provider に閉じる。
- `WorldLifetimeScope` は Prefab / Visual Definition の catalog にならない。
- `ActorView.prefab` から見た目固有の `ActorSpriteAnimationClip` 参照を外す。
- Frame loop では LINQ chain、`ToList()`、`ToArray()`、毎フレームの全 Master 走査を避ける。

## Proposed Data Model

### ActorArchetypeMaster

`ActorArchetypeMaster` に `VisualId` を追加する。

責務:

- Actor 種別のデフォルト見た目を指定する。
- `skinId` は持たない。
- 実際の Addressable address は持たない。

完了条件:

- [ ] `ActorArchetypeMaster` に `VisualId` が追加されている。
- [ ] 既存 hardcoded archetype 全件に `visualId` が設定されている。
- [ ] `ActorArchetypeMaster` が `ScriptableObject` / `Sprite` / Addressable 型に依存していない。

### ActorVisualMaster

新規 Master として追加する。

想定フィールド:

- `string VisualId`
- `int SkinId`
- `string VisualDefinitionAddress`

責務:

- `(visualId, skinId)` から `ActorVisualDefinitionSO` の Addressable address を解決する。
- skin 差し替え時の置換点になる。

完了条件:

- [ ] `ActorVisualMaster` が追加されている。
- [ ] `IMasterRepository` から `(visualId, skinId)` で取得できる。
- [ ] `HardcodedMasterRepository` に `skinId = 0` の初期 visual master が登録されている。
- [ ] `visualId` と `skinId` の重複登録を検出できる。
- [ ] `visualDefinitionAddress` が空の場合は master validation で検出できる。

### SkinId Resolution

Milestone 7.5 では `skinId = 0` 固定で解決する。

将来の拡張:

- Actor 個体、プレイヤー設定、課金スキン設定などから `skinId` を決定する。
- `visualId` は Actor archetype のデフォルト見た目、`skinId` は同じ visualId の差し替え版として扱う。

完了条件:

- [ ] `skinId = 0` がコード上の意味ある定数として表現されている。
- [ ] `0` のマジックナンバーが複数箇所に散らばっていない。
- [ ] 将来 `skinId` の解決元を差し替えられる境界がある。

## Proposed Visual Asset Model

### ActorVisualDefinitionSO

新規 ScriptableObject として追加する。

想定フィールド:

- `string visualId`
- `ActorVisualSizeTier visualSizeTier`
- `AnimationEntry[] entries`

`AnimationEntry`:

- `ActorAnimationKey animationKey`
- `ActorAnimationDirection direction`
- `float fps`
- `bool loop`
- `Sprite[] sprites`

対象 animation key:

- `Idle`
- `Walk`
- `Work`
- `Attack`
- `Damage`
- `Dead`

対象 direction:

- `NE`
- `NW`
- `SE`
- `SW`

責務:

- 1 つの見た目スキンに必要な表示設定をまとめる。
- FPS とフレーム枚数を animation key / direction ごとに変えられるようにする。
- SpriteAtlas 利用時も、SO は最終的に参照する Sprite 配列を持つ。

完了条件:

- [ ] `ActorVisualDefinitionSO` が追加されている。
- [ ] 全 animation key / direction の組み合わせを表現できる。
- [ ] FPS / loop / sprites が entry ごとに設定できる。
- [ ] `visualSizeTier` が Visual Definition 側に移っている。
- [ ] SO は runtime state / cache / dirty flag を持たない。

### Runtime Representation

SO をそのまま `ActorView` に渡すのではなく、必要に応じて runtime 用の不変データへ変換する。

候補:

- `ActorVisualDefinition`
- `ActorVisualAnimationSet`
- `ActorVisualAnimationClip`

責務:

- `ActorAnimationKey + ActorAnimationDirection` から clip を O(1) または低コストで取得する。
- `ActorView` / `ActorSpriteAnimator` が SO の null 配列や重複 entry を直接扱わないようにする。

完了条件:

- [ ] Runtime 表示処理が SO の `Entries` を毎フレーム走査していない。
- [ ] `ActorVisualDefinitionSO` の検証と runtime 変換の責務が分離されている。
- [ ] 同じ visual definition を複数 Actor が共有しても state が混ざらない。

## Animation State Migration

### Rename

既存の `ActorAnimationState` を以下へ置き換える。

| Before | After |
|---|---|
| `Combat` | `Attack` |
| `Hit` | `Damage` |

新規追加:

- `Work`

最終状態:

- `Idle`
- `Walk`
- `Work`
- `Attack`
- `Damage`
- `Dead`

完了条件:

- [ ] `ActorAnimationState.Combat` が残っていない。
- [ ] `ActorAnimationState.Hit` が残っていない。
- [ ] 戦闘イベントによる表示切り替えが `Attack` / `Damage` に更新されている。
- [ ] 既存 Idle / Walk 表示が維持されている。

### Animation Key

`ActorAnimationState` をそのまま Visual Definition の key に使うか、別途 `ActorAnimationKey` を作るかは実装前にレビューする。

推奨:

- View 表示用のキーとして `ActorAnimationKey` を追加する。
- `ActorAnimationState` は現在の表示状態として使う。
- 両者が完全一致する間は薄い変換でよい。

完了条件:

- [ ] 状態と設定キーの責務が混ざっていない。
- [ ] 将来、状態名と設定キーが分かれても変更箇所が局所化される。

## Loading Strategy

### Sequential Loading

Actor visual は起動時に全件プリロードしない。Actor が必要になった時点で `visualId + skinId` を解決し、未ロードなら Addressable から逐次ロードする。

許容挙動:

- ActorView 生成直後、Visual Definition のロード完了までは透明表示になる。

禁止:

- `ActorView` が Addressable を直接ロードする。
- 毎フレーム `LoadAsync` を呼ぶ。
- ロード失敗時に無限リトライする。

完了条件:

- [ ] 未ロード visual は初回要求時に 1 回だけロード要求される。
- [ ] 同じ visual の同時要求が重複ロードを起こさない。
- [ ] ロード済み visual は World 表示中キャッシュされる。
- [ ] `IAssetScope` は World 表示の lifetime に紐づいている。
- [ ] World 終了時に `IAssetScope.Dispose()` で解放される。

### Existing AssetScope

既存の `VisualConfigLoader` は `IAssetManager.CreateScope()` で `IAssetScope` を作り、World スコープの間保持している。

Milestone 7.5 ではこの構造を確認した上で、以下のいずれかを選ぶ。

- 既存 `VisualConfigLoader` に Actor Visual Definition の逐次ロードを追加する。
- `ActorVisualDefinitionLoader` / `ActorVisualRepository` を分離し、同じ World scoped `IAssetScope` を保持する。

推奨:

- 既存 `VisualConfigLoader` の責務が Material / Actor Sprite / Visual Definition で肥大化する場合は、Actor Visual 専用 loader を分離する。

完了条件:

- [ ] `VisualConfigLoader` の責務が過剰に広がっていない。
- [ ] `IAssetScope` の lifetime が World lifetime と一致している。
- [ ] Addressable の直接呼び出しが追加されていない。

## View Integration

### ActorView

変更方針:

- Prefab 上の `idleAnimationClip` / `walkAnimationClip` / `combatAnimationClip` / `hitAnimationClip` / `deadAnimationClip` 参照を廃止する。
- `ApplyVisual(ActorVisualDefinition visualDefinition)` 相当の API を追加する。
- `Reset()` で visual / sprite / state / frame / warning state を初期化する。
- Sprite が null の場合は Warning を出し、`SpriteRenderer.sprite = null` のままにする。

完了条件:

- [ ] `ActorView.prefab` が見た目固有 ScriptableObject を直接参照していない。
- [ ] `ActorView` は visual definition の差し替えを受け付けられる。
- [ ] Pool 返却後に前の Actor の Sprite / visual / animation state が残らない。
- [ ] null Sprite 遭遇時に ActorId / visualId / animation key / direction / frame が追跡できる Warning が出る。

### ActorSpriteAnimator

変更方針:

- 状態別固定フィールドをやめる。
- 現在の `ActorAnimationState` と clip frame index を管理する。
- 現在の state / direction に対応する clip を visual definition から解決する。
- one-shot の扱いは `Damage` と必要に応じて `Attack` に対応する。

完了条件:

- [ ] `idleClip` / `walkClip` / `combatClip` / `hitClip` / `deadClip` の固定フィールドが残っていない。
- [ ] `Work` を含む任意の animation key を扱える。
- [ ] loop / one-shot の挙動が clip 定義に従う。
- [ ] フレーム計算が per Actor state として独立している。

### WorldActorPresenter

変更方針:

- `ActorBehaviorType` ベースの Sprite 解決をやめる。
- `ActorViewData` から `visualId` を受け取り、`skinId = 0` で visual definition を要求する。
- ロード未完了なら Sprite 設定を行わず、ActorView は透明のままにする。
- ロード完了後に `ActorView.ApplyVisual()` を呼ぶ。

完了条件:

- [ ] `ActorSpriteVisualConfig.GetSprite(behaviorType, ...)` 依存が削除されている。
- [ ] `ActorViewData` または View 用 provider から `visualId` が取得できる。
- [ ] 未ロード中の Actor がエラーなく透明表示になる。
- [ ] ロード完了後、同じ ActorView に visual が適用される。

## Application Boundary

### ActorViewData

`ActorViewData` は View 表示に必要な `visualId` を含むようにする。

選択肢:

- `ActorViewData` に `VisualId` を追加する。
- `ActorViewData` は `ArchetypeId` のままにし、Presenter / Query が master から `visualId` を解決する。

推奨:

- `ActorViewData` に `VisualId` を追加する。
- View 層が Actor archetype master を広く読まないようにする。

完了条件:

- [ ] View が `IGameWorldStateReader` や master 全体を直接走査して `visualId` を補完していない。
- [ ] `ActorViewDataStore` が actor spawn / archetype 情報から `visualId` を設定している。
- [ ] `ActorViewData` は Sprite / SO / Addressable address を持たない。

## Task Plan

### task_0001: Master と ViewData の visualId 導入

利用する Lighthouse パターン: なし

作るもの:

- `ActorArchetypeMaster.VisualId`
- `ActorVisualMaster`
- `IMasterRepository.GetActorVisualMaster(string visualId, int skinId)`
- `HardcodedMasterRepository` の visual master 登録
- `ActorViewData.VisualId`
- `ActorViewDataStore` の visualId 設定

完了条件:

- [ ] 既存 Actor archetype 全件が `visualId` を持つ。
- [ ] `skinId = 0` の visual master が登録されている。
- [ ] `ActorViewData` から `visualId` が取得できる。
- [ ] Domain に Unity asset 依存が追加されていない。
- [ ] `uloop.cmd compile --project-path Client` が成功している。

### task_0002: ActorVisualDefinitionSO と runtime definition 追加

利用する Lighthouse パターン: なし

作るもの:

- `ActorVisualDefinitionSO`
- `ActorVisualAnimationEntry`
- `ActorAnimationKey`
- Runtime 用 `ActorVisualDefinition`
- Runtime 用 `ActorVisualAnimationClip`
- SO validation / conversion helper

完了条件:

- [ ] `Idle / Walk / Work / Attack / Damage / Dead` を定義できる。
- [ ] `NE / NW / SE / SW` を定義できる。
- [ ] entry ごとに FPS / loop / sprites が設定できる。
- [ ] runtime 変換後、毎フレーム SO entry 全走査をしない。
- [ ] null Sprite は許容し、runtime 表示時の Warning 対象として扱える。

### task_0003: Actor visual 逐次ロード

利用する Lighthouse パターン: [P5] アセット非同期ロード（IAssetScope）

作るもの:

- `ActorVisualDefinitionLoader` または `VisualConfigLoader` 拡張
- `(visualId, skinId)` -> `ActorVisualMaster` -> Addressable load
- ロード中キャッシュ
- ロード済みキャッシュ
- ロード失敗 Warning

完了条件:

- [ ] `IAssetScope.LoadAsync<ActorVisualDefinitionSO>()` 経由でロードしている。
- [ ] 同じ visual の同時要求が重複ロードにならない。
- [ ] ロード失敗時に無限リトライしない。
- [ ] `Addressables.LoadAssetAsync` の直接使用がない。
- [ ] World 終了時に scope が破棄される。

### task_0004: AnimationState 名称整理と animator 汎用化

利用する Lighthouse パターン: なし

作るもの:

- `ActorAnimationState.Combat` -> `Attack`
- `ActorAnimationState.Hit` -> `Damage`
- `ActorAnimationState.Work`
- `ActorSpriteAnimator` の Visual Definition ベース化
- `ActorView.IsDamageOneShotComplete` 相当の API
- 既存 combat animation presenter の参照更新

完了条件:

- [ ] `Combat` / `Hit` の旧 enum 値が残っていない。
- [ ] 攻撃イベントは `Attack`、被弾イベントは `Damage` を使う。
- [ ] `Work` が設定可能な状態として存在する。
- [ ] 既存 Idle / Walk が動作する。
- [ ] `uloop.cmd compile --project-path Client` が成功している。

### task_0005: ActorView の Visual Definition 適用

利用する Lighthouse パターン: なし

作るもの:

- `ActorView.ApplyVisual(...)`
- `ActorView.ClearVisual()` または `Reset()` 強化
- Prefab から animation clip 直接参照を削除
- null Sprite Warning
- Pool 再利用確認

完了条件:

- [ ] `ActorView.prefab` が `ActorSpriteAnimationClip` を直接参照していない。
- [ ] Visual Definition 差し替え時に frame / state / sprite が破綻しない。
- [ ] Pool 返却後の再利用で前 Actor の visual が残らない。
- [ ] null Sprite 時に Warning が出る。
- [ ] null Sprite でも PlayMode が落ちない。

### task_0006: WorldActorPresenter の visualId 解決対応

利用する Lighthouse パターン: [P5] アセット非同期ロード（IAssetScope）

作るもの:

- `WorldActorPresenter` の `ActorSpriteVisualConfig` 依存削除または縮小
- `visualId + skinId=0` で visual definition を要求する処理
- ロード未完了中の透明表示
- ロード完了後の `ActorView.ApplyVisual()`
- 表示サイズを Visual Definition から取得する処理

完了条件:

- [ ] `ActorBehaviorType` ではなく `visualId` で見た目を解決している。
- [ ] `skinId = 0` 固定解決が 1 箇所にまとまっている。
- [ ] ロード未完了 Actor がエラーなく透明表示される。
- [ ] Visual Definition の `visualSizeTier` に基づき表示高さが設定される。
- [ ] `ActorVisualSizeTier` は Domain / Application の判定に使われていない。

### task_0007: Asset / Prefab / Editor 設定更新

利用する Lighthouse パターン: [P5] アセット非同期ロード（IAssetScope）

作るもの:

- 既存 placeholder / dummy 用 `ActorVisualDefinitionSO`
- Addressable 登録
- `ActorView.prefab` の SerializedField 整理
- 既存 `ActorSpriteVisualConfigSO` の廃止または互換期間の扱い決定

完了条件:

- [ ] PlayMode で最低 1 種類の Actor visual が Addressable 経由で表示される。
- [ ] `ActorView.prefab` に見た目固有 SO 参照が残っていない。
- [ ] 不要になった `ActorSpriteVisualConfigSO` の扱いが明確になっている。
- [ ] Addressable group の schema が壊れていない。

### task_0008: 統合確認とレビュー

追加実装なし。

確認内容:

- compile
- EditMode tests
- 30 秒 PlayMode
- `[World] GameWorldState initialized`
- エラーログなし
- Actor が逐次ロード後に表示される
- ロード前透明表示で落ちない
- null Sprite Warning が追跡可能
- Pool 再利用で前 visual が残らない

完了条件:

- [ ] `uloop.cmd compile --project-path Client` が成功している。
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している。
- [ ] Play 30 秒確認で `[World] GameWorldState initialized` が出ている。
- [ ] Play 30 秒確認で Error ログが存在しない。
- [ ] 作業ログに guideline チェック結果が記録されている。

## Migration Notes

### ActorSpriteVisualConfig

既存の `ActorSpriteVisualConfig` / `ActorSpriteVisualConfigSO` / `ActorSpriteSet` / `VisualConfigLoader.LoadSpritesAsync()` は、Milestone 7.5 の移行対象。

対応方針:

- 新 Visual Definition 経路が完成したら、旧 `ActorBehaviorType` ベースの Sprite 解決を削除する。
- 互換 API を残す場合は、残す理由と削除条件を task log に記録する。
- 「最小差分」を理由に旧経路を恒久的に残さない。

### ActorSpriteAnimationClip

既存 `ActorSpriteAnimationClip` は、見た目スキン単位の Visual Definition に統合する。

対応方針:

- `ActorVisualDefinitionSO.AnimationEntry` が FPS / loop / sprites を持つため、`ActorSpriteAnimationClip` は削除候補。
- 互換期間で残す場合は、`ActorView.prefab` からの直接参照を必ず外す。

## Risks

- 逐次ロード中に Actor が削除された場合、ロード完了 callback が返却済み ActorView に適用されるリスクがある。
- 同じ ActorView が pool 再利用された後に古い visual load 結果が適用されるリスクがある。
- `visualId` の設定漏れにより Actor が透明のままになるリスクがある。
- null Sprite Warning が毎フレーム大量に出るリスクがある。

対策:

- ActorView へ visual を適用する前に ActorId / visual request token / current visualId を確認する。
- null Sprite Warning は visualId + animation key + direction + frame ごとに一度だけ出す。
- master validation で visualId / visual master / address の欠落を検出する。
- ロード中・ロード失敗・ロード済みの状態を分けて管理する。

## Verification

Milestone 7.5 完了前に以下を実行する。

- `uloop.cmd compile --project-path Client`
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
- `uloop.cmd control-play-mode --project-path Client --action Play`
- 30 秒待機
- `uloop.cmd control-play-mode --project-path Client --action Stop`
- `uloop.cmd get-logs --project-path Client`

確認項目:

- [ ] compile 成功
- [ ] EditMode test 成功
- [ ] `[World] GameWorldState initialized` が出力されている
- [ ] Error ログが存在しない
- [ ] Actor visual が逐次ロード後に表示される
- [ ] ロード前透明表示が許容範囲で動作する
- [ ] null Sprite Warning が必要情報を含む
- [ ] Warning が毎フレーム連続出力されない

## Task Dependency

| Task | Depends on | Notes |
|---|---|---|
| task_0001 | none | Master / ViewData の基盤 |
| task_0002 | none | Visual Definition 型追加 |
| task_0003 | task_0001, task_0002 | Addressable 逐次ロード |
| task_0004 | task_0002 | Animator 汎用化 |
| task_0005 | task_0002, task_0004 | ActorView 適用 |
| task_0006 | task_0001, task_0003, task_0005 | Presenter 統合 |
| task_0007 | task_0006 | Asset / Prefab 更新 |
| task_0008 | all | 統合確認 |

## Handoff Notes

Claude Code は task 作成時に、外部契約・責務・依存方向を task file に明記する。

Codex は実装前に以下を確認する。

- `ActorArchetypeMaster.visualId` が View asset 参照ではなくデータ値であること
- `ActorVisualMaster` が `(visualId, skinId)` の address 解決責務に限定されていること
- `ActorView` が Addressable を直接ロードしないこと
- `ActorView.prefab` が見た目固有 SO を参照しないこと
- 逐次ロード完了時に pool 再利用済み View へ古い結果を適用しないこと

仕様にない判断が必要になった場合、実装を止めて `review/{task_id}_question.md` で確認する。

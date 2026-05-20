# Milestone 7 Roadmap — 戦闘表現とステータス表示

## ゴール

ゲームで起きていることをプレイヤーが視覚的に読み取れる状態にする。
戦闘が画面上の出来事として追えるようになり、各 Actor の状態をひと目で確認できるようにする。

## 対象範囲

### Actor アニメーション拡張

**経緯**: Milestone 6 では `ActorAnimationState`（enum）、`ActorSpriteAnimator`（plain class）、`ActorSpriteAnimationClip`（ScriptableObject）を追加し、idle / walk のフレーム配列・FPS・loop 設定を `ActorView` prefab 経由で差し替えられるようにした。`SetAnimationState()` / `Tick()` / `CurrentFrameIndex` はすべてスプライト選択に接続済み。

**M7 で拡張する理由**: Milestone 6 の animation は idle / walk の基礎表示に限定する。combat / hit / dead や正式なアニメーション差し替え UI は戦闘表現と合わせて Milestone 7 で扱う。

**M7 での対応内容**:
- combat / hit / dead の `ActorAnimationState` を追加
- 状態ごとの `ActorSpriteAnimationClip` を追加
- 戦闘イベントに応じて `WorldActorPresenter` から animation state を切り替える
- PlayMode で Actor が戦闘中に combat / hit / dead 表現へ切り替わることを確認

### 戦闘演出

- 被弾・死亡スプライトアニメーション（combat / hit / dead アニメーション状態の追加）
- Projectile（矢・魔法弾）の View 表現
  - Projectile の発射・飛翔・着弾を GameObject で表示する
  - Projectile Prefab ベースで、種別ごとにビジュアルを差し替えられる
- Area Effect（爆発・毒沼など）の View 表現
  - Area Effect の範囲と持続を視覚化する

### ステータス表示

- Actor の頭上 HP バー（SpriteRenderer または UI Canvas World Space で実装）
- 状態異常アイコン（毒・負傷など有効な `ActiveStatusEffect` に対応）
- Actor 選択時の詳細パネル（Stats、HP/MP/疲労、装備、所持金の簡易表示）

### プレイヤー向けイベントログ

- `WorldDebugGameLogPresenter`（デバッグ専用）とは別に、プレイヤー向けの Game Event ログ UI を用意する
- 戦闘結果・宿泊・売買などの主要イベントを簡潔な文で表示する
- ゲームイベントの Application 層クエリ（`GetGameEventHistoryUseCase`）を View が利用する

## Milestone 8 へ移動する項目

- 施設管理 / スタッフ管理の UI
- HUD（時間・資金表示）
- ゲームループの完成（勝敗条件、セーブ/ロード）

---

## 詳細実装計画

### M7 の位置づけ

Milestone 7 は、Milestone 4 までに成立した内部シミュレーションと、Milestone 5 / 6 で整えた World View 基盤を接続し、プレイヤーが戦闘・Actor 状態・主要イベントを画面上で追えるようにする段階とする。

Milestone 8 は常時 HUD と経営 UI 基盤、Milestone 9 はギルド経営 UI を扱うため、M7 では「戦闘と Actor 状態の可視化」に範囲を絞る。時間・資金の常時 HUD、施設管理、スタッフ管理、勝敗条件、セーブ / ロードは M7 に含めない。

### 実装前の必須確認

M7 の各タスク作成前に、以下を再確認する。

- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/guidelines/debugging-policy.md`
- `docs/design/combat-domain-design.md`
- `docs/design/game-event-design.md`
- `docs/design/actor-effect-status-effect-design.md`
- `docs/design/actor-visual-size-tier-design.md`
- `docs/design/lifetime-scope-game-loop-design.md`
- `docs/self-review/milestone6-completion-review-6-codex.md`

### M7 で守る設計境界

- View は Projectile / Area / HP バー / 状態アイコン / ログを表示するだけで、命中判定・ダメージ・死亡判定・状態異常の効果計算を行わない。
- 戦闘の事実は既存の `IGameEventBus` / `IEventSubscriber` 経由で受け取る。購読者は Domain State / `GameWorldState` を変更しない。
- Player 向け UI は `WorldDebugGameLogPresenter` を流用しない。Debug 専用 Presenter と Runtime UI Presenter を分離する。
- UI 表示用の派生値は Application 層の narrow query / read model / DTO で用意し、View が広い `IGameWorldStateReader` を直接読んで集計しない。
- Actor のビルボード、HP バー表示位置、状態アイコン、表示サイズは View 層の責務とし、Domain の Actor サイズ・当たり判定・移動範囲には使わない。
- Frame Loop に追加する処理では LINQ chain、`ToList()`、`ToArray()`、毎フレームのラムダクロージャ、毎フレームの全件差分 polling を避ける。
- アセット差し替えは Lighthouse の `IAssetManager` / `IAssetScope` 経由に限定し、`Addressables.LoadAssetAsync` / `Resources.Load` を直接使わない。

### ユーザー確認ゲート

以下は実装開始前または該当 Phase の設計時にユーザー確認を行う。

- Actor 選択操作の入力仕様: クリック選択でよいか、キーボードや一覧選択を併用するか。
- Actor 詳細パネルの表示項目: M7 では Stats / HP / MP / 疲労 / 装備 / 所持金の簡易表示までに限定してよいか。
- HP バーと状態アイコンの表現方式: World Space Canvas、SpriteRenderer、または `ActorView` 子 GameObject のどれを採用するか。
- Projectile / Area Effect の見た目: M7 は placeholder prefab / simple mesh 表示でよいか、最低限必要な正式アセットがあるか。
- プレイヤー向けイベントログの表示位置と行数: 画面下部の一時ログでよいか、固定パネルにするか。

これらの確認が完了していない場合、実装タスクでは暫定実装を進めず、`review/{task_id}_question.md` で確認を返す。

---

## Phase 0: M6 からの View 基盤フォローアップ

### 目的

M7 の戦闘表示を積む前に、Milestone 6 完了レビューで「次マイルストーン以降」とされた View / Asset 基盤のうち、M7 の表示品質と検証に直接影響する項目を片付ける。

### 対応内容

- `DungeonInn Visual` Addressables group の schema 設定を確認し、Packed Mode build 前に schema なし group が残らないようにする。
- `VisualAssetSetup` の自動セットアップを、副作用なし validation と明示 setup に分ける方針を確認する。
- `VisualConfigLoader` と `ActorSpriteVisualConfig` に重複している placeholder sprite 生成定数・ロジックを共通 Factory に集約する。
- `UnityNavMeshPathProvider` の DI 登録グループ分類を View 側に整理する。
- `INavigationPathProvider.TryFindPath()` の返却リスト寿命契約を doc comment または API 名で明確化する。
- Destroy helper 重複は、M7 で新規 View pool / effect pool を作る前に共通 utility 化するか、既存パターンとして意図を記録する。
- M7 表示系テストの前提として、`MapMaterialSet.asset` / Addressables group / `MapMeshBuildService` の最低限の asset 整合テストを追加するか、タスク内の検証項目へ明記する。

### 完了条件

- [ ] M7 で追加する Projectile / Area / UI 用 asset 差し替え口が、schema なし Addressables group に依存していない。
- [ ] Placeholder Sprite / Texture 生成ロジックが複数クラスへ追加で増えていない。
- [ ] View pool / effect pool の破棄処理が既存 Destroy helper 重複を悪化させていない。
- [ ] `uloop.cmd compile --project-path Client` が成功している。
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している。

---

## Phase 1: Actor フルビルボード回転

### 目的

カメラ yaw だけに追従する表示から、カメラ回転全体へ正対するフルビルボード表示へ変更する。Actor の見た目が isometric camera で安定して読める状態にする。

### 対応内容

- `WorldCameraController` から現在の camera rotation を View 層に公開する。
- `ActorView` に `SetBillboardRotation(Quaternion cameraRotation)` 相当の API を追加する。
- `ActorView.SetRotationY(float degrees)` は互換維持が必要かを確認し、不要であれば呼び出し元を置き換える。
- `WorldActorPresenter` は camera rotation を `ActorView` へ渡す。
- アニメーション方向選択は従来通り camera yaw と Actor 移動方向から決め、フルビルボード rotation とは分離する。
- `ActorVisualSizeTier` と ground anchor offset の計算は維持し、Domain に表示サイズ情報を追加しない。

### 完了条件

- [ ] カメラを 90 度単位で回転しても Actor が画面上でカメラに正対して見える。
- [ ] `CurrentYawDegrees` は animation direction 選択用として引き続き利用されている。
- [ ] Domain / Application に `Quaternion` / `Camera` / `ActorVisualSizeTier` 依存が追加されていない。
- [ ] PlayMode で camera rotation、Actor 移動、idle / walk animation が同時に破綻しないことを確認している。

---

## Phase 2: 戦闘 Actor アニメーション拡張

### 目的

既存の idle / walk animation に、戦闘中・被弾・死亡の表示状態を追加する。戦闘イベントを画面上の Actor 表現へ接続し、内部ログを読まなくても戦闘が起きていることを理解できるようにする。

### 対応内容

- `ActorAnimationState` に `Combat` / `Hit` / `Dead` を追加する。
- `ActorSpriteAnimationClip` に `Combat` / `Hit` / `Dead` 用 clip を設定できるようにする。
- `ActorSpriteAnimator` は one-shot clip と loop clip の切り替え契約を明確にする。
- `ActorView` は状態別 clip が未設定の場合に fallback clip を使い、Warning を出す条件を整理する。
- `WorldActorPresenter` または専用 Presenter が、以下のイベントを購読して animation state を切り替える。
  - `CombatAttackOccurred`: 攻撃者を `Combat`、対象を `Hit`
  - `ProjectileHit`: 対象を `Hit`
  - `AreaEffectHit`: 対象を `Hit`
  - `ActorDefeated`: 対象を `Dead`
- one-shot `Hit` 終了後は、Actor が生存していれば idle / walk / combat の基礎状態へ戻す。
- `Dead` 表示は Actor が View から削除されるまで維持する。削除タイミングと死亡 one-shot の見え方が競合する場合は、設計確認を行う。

### 完了条件

- [ ] 戦闘中の Actor が `Combat` 表示へ切り替わる。
- [ ] 被弾時に対象 Actor が `Hit` 表示へ切り替わり、one-shot 後に適切な基礎状態へ戻る。
- [ ] 死亡時に対象 Actor が `Dead` 表示へ切り替わる。
- [ ] 未設定 clip があっても PlayMode が落ちず、fallback と Warning で追跡できる。
- [ ] イベント購読者は Domain State / `GameWorldState` を変更していない。
- [ ] `uloop.cmd compile --project-path Client` と EditMode test が成功している。

---

## Phase 3: Projectile View 表現

### 目的

`ProjectileFired` / `ProjectileHit` と `GameWorldState.Projectiles` を View に反映し、矢・魔法弾などの発射、飛翔、着弾が画面上で追えるようにする。

### 対応内容

- `ProjectileView` MonoBehaviour を追加し、SpriteRenderer / MeshRenderer / Trail などの表示を内部に閉じる。
- `ProjectileViewPool` または `ProjectileViewRegistry` を追加し、ProjectileId と View GameObject を対応付ける。
- Projectile prefab の差し替え口を `VisualConfigSettings` または専用 ScriptableObject に用意する。
- prefab 未設定時は simple sprite / primitive mesh の fallback 表示を用意する。
- `ProjectileFired` で表示を生成し、`AdvanceProjectileUseCase` によって更新された `GameWorldState.Projectiles` の位置へ同期する。
- `ProjectileHit` または projectile 消滅検知で View を返却 / 破棄する。
- Projectile の移動・命中判定は Application の結果だけを反映し、View は独自判定を持たない。
- 毎フレーム更新は registry 内の active projectile のみを対象にし、全 actor / 全 event history を走査しない。

### 完了条件

- [ ] Projectile 発射時に View GameObject が生成される。
- [ ] Projectile 飛翔中に Domain / Application の projectile position と View 表示が同期する。
- [ ] 命中または消滅時に Projectile View が残留しない。
- [ ] prefab を差し替えるだけで projectile の見た目を変更できる。
- [ ] prefab 未設定時の fallback で PlayMode が落ちない。
- [ ] View は Unity Collider による命中判定を追加していない。

---

## Phase 4: Area Effect View 表現

### 目的

`AreaEffectCreated` / `AreaEffectHit` と `GameWorldState.AreaEffects` を View に反映し、爆発・毒沼などの範囲と持続が画面上で分かるようにする。

### 対応内容

- `AreaEffectView` MonoBehaviour を追加し、範囲 shape / radius / duration を表示する。
- `AreaEffectViewRegistry` を追加し、AreaEffectId と View GameObject を対応付ける。
- `AttackAreaShape.Circle` を最初の表示対象とする。Rectangle / Fan が既に runtime に存在する場合は placeholder 表示を行うか、タスク内で対応範囲を明記する。
- `AreaEffectCreated` で表示を生成し、AreaEffect の duration に応じて alpha / scale / color を変化させる。
- `AreaEffectHit` は hit pulse や色変化などの一時演出に限定し、ダメージ計算は行わない。
- AreaEffect が expired になったら View を返却 / 破棄する。
- 範囲表示は View 層の visual scale であり、Domain の当たり判定半径を変更しない。

### 完了条件

- [ ] AreaEffect 発生時に範囲が画面上に表示される。
- [ ] duration area は持続時間中だけ表示され、expired 後に残留しない。
- [ ] `AreaEffectHit` 発生時に対象または範囲表示へ hit feedback が出る。
- [ ] View は `AttackAreaTargetResolver` 相当の対象抽出を再実装していない。
- [ ] active area effect 数に比例する更新に限定され、全 actor 走査を追加していない。

---

## Phase 5: Actor 頭上ステータス表示

### 目的

Actor の現在 HP と有効な ActorEffect / StatusEffect を、World 上でひと目で確認できるようにする。

### 対応内容

- `ActorStatusView` を `ActorView` の子要素として追加する。
- HP バーは `ActorStatusView` が表示のみを担当し、HP 比率は Application の Actor view data / status DTO から受け取る。
- `ActorViewData` または専用 `ActorStatusViewData` に HP / MaxHP / ActorEffect summary を含めるかを設計する。
- ActorEffect / StatusEffect 表示は ActorEffect 単位を基本とし、StatusEffect は内部計算単位として扱う。
- アイコン sprite は placeholder を用意し、ActorEffectMasterId / StatusEffectType に応じて差し替えられる構成にする。
- 状態アイコンは View が master 表示名を直接解決しない。必要な表示用 summary は Query / DTO 境界で用意する。
- HP バーとアイコンの位置は `ActorVisualSizeTier` の ground anchor / canvas height から View 層で計算する。

### 完了条件

- [ ] Actor 頭上に HP バーが表示され、HP 変化に追従する。
- [ ] ActorEffect / StatusEffect が有効な Actor に placeholder アイコンが表示される。
- [ ] ActorEffect が expired になったらアイコンが消える。
- [ ] View が `ActiveStatusEffect` を直接変更していない。
- [ ] Domain に UI 用表示名・アイコン・表示高さのフィールドを追加していない。

---

## Phase 6: Actor 選択と詳細パネル

### 目的

プレイヤーが Actor を選択し、基本ステータスを確認できるようにする。M8 / M9 の本格 UI へ進む前に、World 上の対象確認 UI と Query 境界を確立する。

### 対応内容

- Actor 選択用 input は Lighthouse の `IInputLayer` 経由で扱う。旧 Input System の直接 polling は追加しない。
- クリック選択を行う場合、camera ray と View registry の表示 bounds / selection proxy を使い、Domain 判定にしない。
- `SelectedActorStateService` 相当の View scoped state を追加する場合は、所有者・寿命・クリア条件を記録する。
- `GetActorStatusUseCase` または同等の narrow query を Application 層に追加し、選択 Actor の表示 DTO を返す。
- DTO には Stats、HP / MP、疲労、装備、所持金、ActorEffect summary を含める。
- Presenter は DTO を表示するだけにし、`Actor` / `Inventory` / `Equipment` を直接変更しない。
- UI は World Scene 内の panel として扱い、ScreenStack Dialog にする場合は Lighthouse ScreenStack の P3 パターンに従う。

### 完了条件

- [ ] Actor を選択すると詳細パネルが表示される。
- [ ] 選択解除または Actor despawn 時に詳細パネルが閉じる、または空状態になる。
- [ ] 詳細パネルは narrow query / DTO を使い、広い `IGameWorldStateReader` を Presenter が直接読んでいない。
- [ ] 入力処理が `IInputLayer` 経由で実装されている。
- [ ] `UnityEngine.UI.Button` を使わず、必要なボタンがある場合は `LHButton` を使っている。

---

## Phase 7: プレイヤー向けイベントログ UI

### 目的

Debug ログではなく、プレイヤーがゲーム内で主要イベントを追える簡潔なログ UI を用意する。

### 対応内容

- `WorldDebugGameLogPresenter` は DEBUG 専用のまま維持し、Runtime UI には流用しない。
- `GameEventHistoryService` / `GetGameEventHistoryUseCase` を使う player-facing Presenter を追加する。
- Event DTO には表示文字列を持たせず、Application の Query / formatter / read model 境界で表示用 DTO に変換する。
- 対象イベントは M7 では以下に限定する。
  - `CombatAttackOccurred`
  - `ProjectileFired`
  - `ProjectileHit`
  - `AreaEffectCreated`
  - `AreaEffectHit`
  - `ActorDefeated`
  - `ItemDropped`
  - `ItemPickedUp`
  - `ActorLeveledUp`
  - `ActorRecoveringAtInn`
  - `ActorFullyRecovered`
- ログ UI は直近 N 件を表示し、全履歴を毎フレーム再取得しない。イベント履歴の revision / dirty / 明示 refresh のいずれかで更新する。
- 表示文のために View が `GameWorldState` から Actor / Item / Master を広く参照しない。

### 完了条件

- [ ] Debug Presenter と Player Log Presenter が別クラスである。
- [ ] Player Log Presenter が `WorldDebugGameLogPresenter` の DEBUG 専用依存を流用していない。
- [ ] 主要イベントが画面上の短文ログとして表示される。
- [ ] ログ表示の更新が毎フレーム全履歴 polling になっていない。
- [ ] イベント DTO に View 用表示文字列を追加していない。

---

## Phase 8: M7 統合確認とセルフレビュー

### 目的

M7 の表示機能がゲーム進行と矛盾せず、ガイドライン違反や View / Application 境界崩れを起こしていないことを確認する。

### 確認内容

- Projectile / Area / Actor animation / HP bar / status icon / actor details / player log が同じ PlayMode で共存する。
- View 表示がない、または prefab / clip / icon が未設定でも fallback と Warning で追跡でき、ゲーム進行が止まらない。
- Actor / Projectile / Area despawn 後に View GameObject が残留しない。
- Event 購読者が `Dispose()` / `CompositeDisposable` で購読解除される。
- Frame Loop に全件 polling / LINQ / 毎フレーム allocation が増えていない。
- Domain / Application に UnityEngine 依存が増えていない。
- M8 へ進むための UI 基盤上の残課題を、M8 roadmap または self-review に記録する。

### 完了条件

- [ ] `uloop.cmd compile --project-path Client` が成功している。
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が全件 pass している。
- [ ] `uloop.cmd control-play-mode --project-path Client --action Play` で 30 秒以上実行し、`[World] GameWorldState initialized` が出力され、Error ログがない。
- [ ] Projectile / Area / combat / hit / dead / HP bar / player log の PlayMode 確認結果を task 作業ログに記録している。
- [ ] `docs/guidelines/` の各 guideline 本文と完了前チェックリストを確認している。
- [ ] M7 完了レビューを `docs/self-review/` 配下に保存している。

---

## 推奨実装順

1. Phase 0: M6 からの View 基盤フォローアップ
2. Phase 1: Actor フルビルボード回転
3. Phase 2: 戦闘 Actor アニメーション拡張
4. Phase 3: Projectile View 表現
5. Phase 4: Area Effect View 表現
6. Phase 5: Actor 頭上ステータス表示
7. Phase 6: Actor 選択と詳細パネル
8. Phase 7: プレイヤー向けイベントログ UI
9. Phase 8: M7 統合確認とセルフレビュー

Phase 3 と Phase 4 は、共通の View pool / effect visual config を設計した後であれば並行可能。ただし prefab 差し替え口、Destroy helper、fallback asset 生成の方針は Phase 0 で揃えてから着手する。

Phase 5 と Phase 6 は依存関係がある。Actor 詳細パネルは Actor の status DTO / summary を使うため、頭上ステータス表示で必要な status data 境界を先に確定する。

---

## M7 完了判定サマリ

Milestone 7 は以下を満たした時点で完了とする。

- Actor がカメラに正対し、idle / walk / combat / hit / dead の状態が画面上で判別できる。
- Projectile の発射・飛翔・着弾が GameObject として表示され、消滅時に残留しない。
- Area Effect の範囲・持続・命中 feedback が表示され、expired 後に残留しない。
- Actor 頭上に HP バーと有効 ActorEffect / StatusEffect の placeholder アイコンが表示される。
- Actor を選択すると、Stats / HP / MP / 疲労 / 装備 / 所持金の簡易詳細を確認できる。
- Player 向けイベントログ UI で、戦闘・宿泊・売買などの主要イベントを追える。
- Debug Presenter と Player UI が分離されている。
- View / Presenter がゲーム進行や Domain 判定を握っていない。
- `uloop.cmd compile --project-path Client`、EditMode test、30 秒 PlayMode ログ確認が成功している。

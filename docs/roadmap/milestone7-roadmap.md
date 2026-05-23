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

### 確認済み仕様

ユーザーへの確認が完了した設計決定事項。実装タスクはこれらを仕様として扱う。

**Actor 選択操作（入力）**
- マウスクリックで Actor を選択する
- 選択状態のとき、矢印キーで隣の Actor へ切り替える（**未選択時に矢印キーを押しても選択状態にはならない**）
- キーパッド入力は将来対応。M7 では `IInputLayer` の拡張口のみ設計考慮し、実装はしない

**Actor 選択解除条件**（以下のいずれか）
- ESC キー → 未選択状態に戻る
- 別の Actor をクリック → その Actor の選択状態に切り替わる
- 詳細 Popup を閉じる → 未選択状態に戻る

**カメラ追従・拡大**
- 選択時: 選択 Actor をカメラが追従し、Orthographic Size を通常時の 20% に縮小する（定数 `ActorSelectionZoomRatio = 0.20f`。実装後に調整可）
- 解除時: 追従を停止し、Orthographic Size を元の値に復元する

**HP バーと状態アイコンの表示方式**
- `WorldUIModuleScene`（M7 で新規作成する World 専用 Module Scene）上の Screen Space Overlay Canvas に表示する
- Actor の画面位置は `Camera.WorldToScreenPoint` で変換し、Canvas RectTransform に適用して Actor に追従させる
- `ActorView` の子 GameObject としては実装しない

**Projectile / Area Effect とその他 placeholder アセット**
- M7 の全 placeholder Sprite は `Assets/DungeonInn/Runtime/Art/Sprites/Effect/Dummy.png` を使う
- 後で正式アセットに入れ替える前提

**Actor 詳細パネルの表示形式**
- M7 実装完了時点では、ScreenStack Popup ではなく **WorldUI HUD パネル** として実装する
  - 変更理由: Actor 詳細はモーダル遷移ではなく、選択中 Actor の画面座標に毎フレーム追従する非ブロッキング HUD 要素であるため
  - Prefab は Addressable から `WorldAddressableViewFactory` がロードし、Presenter は `ActorDetailPopup` の表示/非表示・内容・位置更新だけを扱う
  - `ActorDetailPopup` は LifetimeScope に `SerializedField` で直接保持しない
- Actor の画面座標の横に追従する（`Camera.WorldToScreenPoint` → Canvas RectTransform 位置設定）
- 選択時に HUD パネルを表示し、非選択時に非表示にする
- M7 では表示・確認のみ。編集・指示操作は M8 以降

**プレイヤー向けイベントログ**
- 画面下部に一時フェードログとして表示する（初期値: 表示後 3 秒でフェード開始、1 秒でフェードアウト完了。コード内定数で管理）
- 通常時は最新ログのみ表示。マウスオーバーで直近 10 件を固定表示する
- 10 行分の固定 TextView（`ScrollRect` 不使用）の Text だけを更新する 1 行単位 CLI スクロール
- マウスホイールでスクロール。マウスが離れたらスクロール位置をリセット（最新ログへ戻る）
- 新ログが来たとき、古いものを上に押し出して最新を下に追加する

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

- `WorldUIModuleScene` を M7 で新規作成する。World シーン専用の Module Scene とし、Screen Space Overlay Canvas を持つ。
- `ActorStatusView` は `WorldUIModuleScene` の Canvas 上に置く。`ActorView` の子 GameObject としては実装しない。
- HP バーは `ActorStatusView` が表示のみを担当し、HP 比率は Application の Actor view data / status DTO から受け取る。
- Actor の画面位置は `Camera.WorldToScreenPoint` で変換し、`ActorStatusView` の RectTransform に毎フレーム適用して追従させる。
- `ActorViewData` または専用 `ActorStatusViewData` に HP / MaxHP / ActorEffect summary を含めるかを設計する。
- ActorEffect / StatusEffect 表示は ActorEffect 単位を基本とし、StatusEffect は内部計算単位として扱う。
- アイコン sprite は `Dummy.png` を placeholder として使い、ActorEffectMasterId に応じて差し替えられる構成にする。
- 状態アイコンは View が master 表示名を直接解決しない。必要な表示用 summary は Query / DTO 境界で用意する。

### 完了条件

- [ ] `WorldUIModuleScene` が World シーンのアクティベート / デアクティベートに連動して動作する。
- [ ] HP バーが `WorldUIModuleScene` Canvas 上で Actor の画面位置に追従している。
- [ ] ActorEffect / StatusEffect が有効な Actor に `Dummy.png` placeholder アイコンが表示される。
- [ ] ActorEffect が expired になったらアイコンが消える。
- [ ] View が `ActiveStatusEffect` を直接変更していない。
- [ ] Domain に UI 用表示名・アイコン・表示高さのフィールドを追加していない。

---

## Phase 6: Actor 選択と詳細パネル

### 目的

プレイヤーが Actor を選択し、基本ステータスを確認できるようにする。M8 / M9 の本格 UI へ進む前に、World 上の対象確認 UI と Query 境界を確立する。

### 対応内容

#### 入力とカメラ制御（Phase 6a / task_0007）

- Actor 選択は `IInputLayer` 経由のマウスクリックで行う。カメラ ray と selection proxy を使い、Domain 判定にしない。
- 選択状態のとき、矢印キーで隣の Actor へ切り替える。**未選択時に矢印キーを押しても選択状態にはならない。**
- 選択解除: ESC キー / 別 Actor クリック / Popup 閉じる（いずれかで未選択状態に戻る）。
- Actor 選択時: カメラが選択 Actor を追従し、Orthographic Size を `ActorSelectionZoomRatio = 0.20f` に縮小する。
- Actor 選択解除時: 追従を停止し、Orthographic Size を元の値に復元する。
- キーパッド入力は将来対応。M7 では `IInputLayer` の拡張口のみ設計考慮し実装しない。

#### 詳細 Popup（Phase 6b / task_0008）

- Actor 選択時に ScreenStack **Popup** を push する。非選択時に pop する（Dialog ではない）。
- Popup の位置は `Camera.WorldToScreenPoint(actorWorldPos)` で Actor の画面座標の横に追従させる。
- `GetActorDetailQuery` または同等の narrow query を Application 層に追加し、選択 Actor の表示 DTO を返す。
- DTO には Stats、HP / MP、疲労、装備、所持金、ActorEffect summary を含める。
- Presenter は DTO を表示するだけにし、`Actor` / `Inventory` / `Equipment` を直接変更しない。

### 完了条件

- [ ] Actor を選択するとカメラが追従し Orthographic Size が縮小する。
- [ ] ESC キー / 別 Actor クリック / Popup 閉じる で選択解除でき、カメラが元の Size に戻る。
- [ ] 矢印キーで選択中の Actor を切り替えられる。未選択時は矢印キーが作用しない。
- [ ] 選択時に詳細 Popup が Actor の画面座標の横に追従して表示される。
- [ ] 選択解除または Actor despawn 時に Popup が閉じる。
- [ ] 詳細 Popup は narrow query / DTO を使い、広い `IGameWorldStateReader` を Presenter が直接読んでいない。
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

---

## タスク分解と実装詳細

このセクションは各 Phase を具体的なタスクと作成物に分解する。
`tasks/task_{番号}.md` の作成時に参照する。

### タスク一覧

| Task ID | Phase | タスク名 | 前提タスク |
|---|---|---|---|
| task_0001 | Phase 0 | M6 View 基盤フォローアップ | なし |
| task_0002 | Phase 1 | Actor フルビルボード回転 | task_0001 |
| task_0003 | Phase 2 | 戦闘 Actor アニメーション拡張 | task_0002 |
| task_0004 | Phase 3 | Projectile View 表現 | task_0001 |
| task_0005 | Phase 4 | Area Effect View 表現 | task_0001 |
| task_0006 | Phase 5 | Actor 頭上ステータス表示 | task_0003 |
| task_0007 | Phase 6a | Actor 選択入力とカメラ制御 | task_0002, task_0006 |
| task_0008 | Phase 6b | Actor 詳細 Popup | task_0007 |
| task_0009 | Phase 7 | プレイヤー向けイベントログ UI | task_0001 |
| task_0010 | Phase 8 | M7 統合確認とセルフレビュー | task_0003, task_0004, task_0005, task_0006, task_0007, task_0008, task_0009 |

並行可能な組み合わせ:

- task_0004 / task_0005 は task_0001 完了後、task_0002 / task_0003 と並行可能。ただし prefab pool / Destroy helper / fallback asset 生成方針は Phase 0 で揃えてから着手する。
- task_0009 は task_0001 完了後であれば他タスクと並行可能。
- task_0006 は task_0003 の status DTO 設計が確定してから着手する。
- task_0007 は task_0002 と task_0006 の完了後に着手する。
- task_0008 は task_0007 完了後に着手する。

---

### task_0001: M6 View 基盤フォローアップ

**利用する Lighthouse パターン:** [P5] アセット非同期ロード（IAssetScope）

#### 作るもの

| 変更対象 | 種別 | 内容 |
|---|---|---|
| `DungeonInn Visual` Addressables Group | Editor 設定修正 | Packed Assets schema を追加し schema なし状態を解消する |
| `VisualAssetSetup` | 既存クラス修正 | 副作用なし validation と明示 setup メソッドに分離する。Awake / OnEnable での暗黙実行を排除する |
| `PlaceholderAssetFactory`（仮称） | 共通 utility 追加 | `VisualConfigLoader` / `ActorSpriteVisualConfig` に重複する placeholder Sprite / Texture 生成定数・ロジックを集約する。重複元のコードを削除する |
| `UnityNavMeshPathProvider` DI 登録 | `WorldLifetimeScope` 修正 | View: ナビゲーション / 空間グループへ移動する（現在の登録グループを確認の上修正） |
| `INavigationPathProvider.TryFindPath()` | doc comment 追加 | 返却 `LayerPosition[]` の寿命（フレームを跨いで保持してよいか）を明記する |

---

### task_0002: Actor フルビルボード回転

**利用する Lighthouse パターン:** なし（View 層内完結）

#### 作るもの

| クラス / API | 種別 | 内容 |
|---|---|---|
| `WorldCameraController.CurrentCameraRotation` | プロパティ追加 | `Quaternion` 型。現在のカメラ rotation を返す |
| `ActorView.SetBillboardRotation(Quaternion cameraRotation)` | メソッド追加 | スプライト平面をカメラ回転に正対させる Transform 操作を内部で実行する |
| `WorldActorPresenter` | 更新 | フレームごとに `WorldCameraController.CurrentCameraRotation` を取得し `ActorView.SetBillboardRotation()` へ渡す |

#### 設計メモ

- `SetRotationY(float degrees)` はアニメーション方向選択専用として維持し、フルビルボード回転と共存させる。
- ビルボード実装は `transform.rotation = cameraRotation` の直接代入か LookAt 変形で実現する。`Camera.main` は禁止。カメラ参照は `WorldCameraController` 経由で取得する。
- `ActorVisualSizeTier` の ground anchor offset と `Transform.localPosition` は変更しない。

---

### task_0003: 戦闘 Actor アニメーション拡張

**利用する Lighthouse パターン:** なし（View 層内完結、IGameEventBus 購読）

#### 作るもの

| クラス / API | 種別 | 内容 |
|---|---|---|
| `ActorAnimationState.Combat` / `.Hit` / `.Dead` | enum 値追加 | 戦闘中・被弾・死亡状態を追加する |
| `ActorSpriteAnimator` one-shot 機能 | 機能追加 | one-shot clip 再生後にコールバックを呼び基礎状態へ自動復帰する契約を追加する |
| `ActorView.SetBaseAnimationState(ActorAnimationState)` | 新メソッド追加 | one-shot 終了後の復帰先基礎状態（Idle / Walk / Combat）を設定する |
| `ActorSpriteAnimationClip` Combat / Hit / Dead 設定 | ScriptableObject 拡張 | `ActorView` Prefab の Inspector で Combat / Hit / Dead clip を設定できるスロットを追加する |
| `WorldActorCombatAnimationPresenter` | 新規 Presenter | `IGameEventBus` イベント購読と `ActorView` アニメーション状態切り替えを担当する。`WorldActorPresenter` から分離する |

#### イベント → アニメーション状態マッピング

| イベント | 対象 Actor フィールド | 設定状態 |
|---|---|---|
| `CombatAttackOccurred` | `AttackerActorId` | `Combat`（loop） |
| `CombatAttackOccurred` | `TargetActorId` | `Hit`（one-shot → 復帰） |
| `ProjectileHit` | `TargetActorId` | `Hit`（one-shot → 復帰） |
| `AreaEffectHit` | `TargetActorId` | `Hit`（one-shot → 復帰） |
| `ActorDefeated` | `ActorId` | `Dead`（despawn まで維持） |

#### 設計メモ

- `WorldActorCombatAnimationPresenter` は `IInitializable` / `IDisposable` を実装する。`Initialize()` で購読開始、`Dispose()` で `CompositeDisposable.Dispose()`。
- `WorldLifetimeScope` 登録グループ: View: アクター描画

---

### task_0004: Projectile View 表現

**利用する Lighthouse パターン:** [P5] アセット非同期ロード（IAssetScope）

#### 作るもの

| クラス / アセット | 種別 | 内容 |
|---|---|---|
| `ProjectileView` | MonoBehaviour | `SetPosition(Vector3)` / `SetDirection(Vector3)` / `Deactivate()` を公開する。SpriteRenderer または簡易 Mesh で表示する |
| `ProjectileViewRegistry` | Service | `ProjectileId` → `ProjectileView` の対応管理と Pool（`Queue<ProjectileView>`）を持つ |
| `ProjectileViewVisualConfig` | ScriptableObject | Projectile 種別ごとの Prefab アドレスと fallback primitive 設定を持つ |
| `WorldProjectilePresenter` | Presenter | `ProjectileFired` で View を生成、フレームごとに `GameWorldState.Projectiles` から位置同期、`ProjectileHit` / 消滅で View を返却する |

#### 設計メモ

フレームループ更新:
- `WorldProjectilePresenter` は `ITickable` にしない。`WorldActorPresenter` と同じ View 更新フェーズで `UpdateProjectileViews()` を呼ぶ。
- active Projectile のみ走査する。`GameWorldState.Projectiles` が空なら即 return する。全 Actor / 全 event history の走査は禁止。LINQ は使わない。

Pool:
- `WorldActorViewPool` の実装パターンに揃える（`Queue<ProjectileView>` ベース）。
- Prefab 未設定時は Quad Primitive + Material の fallback を使い PlayMode が落ちないようにする。

`WorldLifetimeScope` 登録グループ: View: アクター描画

---

### task_0005: Area Effect View 表現

**利用する Lighthouse パターン:** [P5] アセット非同期ロード（IAssetScope）

#### 作るもの

| クラス / アセット | 種別 | 内容 |
|---|---|---|
| `AreaEffectView` | MonoBehaviour | `SetNormalizedProgress(float t)` / `SetShape(AttackAreaShape, float radius)` / `TriggerHitPulse()` / `Deactivate()` を公開する |
| `AreaEffectViewRegistry` | Service | `AreaEffectId` → `AreaEffectView` の対応管理と Pool を持つ |
| `WorldAreaEffectPresenter` | Presenter | `AreaEffectCreated` で View を生成、フレームごとに残り duration を同期、expired で View を返却する |

#### 形状対応方針

- M7 では `AttackAreaShape.Circle` を正式表示対象とする。
- `Rectangle` / `Fan` は M7 では placeholder（Circle と同じ表示）とし、TODO コメントで記録する。

#### alpha / scale アニメーション仕様

- `SetNormalizedProgress(t)` で `t = 1` が生成直後、`t = 0` が消滅直前。
- Duration Area は `残り時間 / 最大時間` を t として渡す。Instant Area は t=1 で表示後、1 フレームで Deactivate する。

`WorldLifetimeScope` 登録グループ: View: アクター描画

---

### task_0006: WorldUIModuleScene 作成と Actor 頭上ステータス表示

**利用する Lighthouse パターン:** [P2] ModuleScene 作成と登録、[P5] アセット非同期ロード（IAssetScope）

#### 作るもの

| クラス / アセット | 種別 | 内容 |
|---|---|---|
| `WorldUIModuleScene` | 新規 ModuleScene | World 専用 HUD 表示用 Module Scene。Screen Space Overlay Canvas を持つ。World シーン以外では使わない |
| `WorldUILifetimeScope` | 新規 LifetimeScope | `WorldUIModuleScene` 専用の VContainer LifetimeScope |
| `ActorStatusView` | UI MonoBehaviour（Canvas 上） | `SetHpRatio(float)` / `SetStatusIcons(IReadOnlyList<ActorEffectIconData>)` / `SetScreenPosition(Vector2)` を公開する。HP バーと状態アイコンスロットを持つ |
| `ActorHUDViewPool` | Service | `ActorStatusView` の Pool を管理する（`WorldActorViewPool` と同様の `Queue<>` パターン） |
| `ActorStatusViewData` | DTO | `ActorId` / `float HpRatio` / `IReadOnlyList<ActorEffectIconData> ActiveEffects` を持つ |
| `ActorEffectIconData` | DTO | `ActorEffectMasterId` / `string DisplayName` / `float RemainingSeconds` を持つ |
| `GetActorStatusSummaryQuery` | Application Query | `ActorId` を受け取り `ActorStatusViewData` を返す narrow query |
| `WorldActorStatusPresenter` | Presenter | フレームごとに active Actor の world 位置を `Camera.WorldToScreenPoint` で変換し `ActorStatusView` の位置と HP / Effect を更新する |
| HP バー / アイコン用 Sprite | アセット | `Dummy.png` を使う |

#### 設計メモ

ModuleScene 作成:
- `WorldUIModuleScene` を新規 Unity Scene として作成し、Screen Space Overlay Canvas を配置する。
- Lighthouse の ModuleScene パターン（P2）に従い、World シーン起動時にアクティベート、World シーン終了時にデアクティベートする。

`ActorStatusView` の位置追従:
- `WorldActorStatusPresenter` が毎フレーム `Camera.WorldToScreenPoint(actor.WorldPosition)` を取得し、`ActorStatusView.SetScreenPosition(screenPos)` を呼ぶ。
- `SetScreenPosition` は `RectTransform.position = screenPos`（または `anchoredPosition` 換算）を設定する。
- `Camera.main` は禁止。`WorldCameraController` 経由でカメラ参照を取得する。

更新頻度:
- 画面位置更新（`WorldToScreenPoint`）は毎フレーム全 Actor に適用する（位置は常に変化しうるため）。
- HP / Effect 更新は `ActorViewDataStore` の変化通知を利用し、変化した Actor のみに絞る。

`WorldLifetimeScope` 登録グループ: View: アクター描画（`WorldActorStatusPresenter`、`ActorHUDViewPool`）
`WorldUILifetimeScope` 登録グループ: HUD シーン固有の初期化処理

---

### task_0007: Actor 選択入力とカメラ制御

**利用する Lighthouse パターン:** [P10] Input Layer

#### 作るもの

| クラス / API | 種別 | 内容 |
|---|---|---|
| `ActorSelectionService` | View-scoped Service | 選択中 `ActorId?` を `ReactiveProperty<ActorId?>` で保持する。`Select(ActorId)` / `Deselect()` / `SelectNext()` / `SelectPrevious()` を公開する |
| `WorldActorSelectionInputHandler` | View Component | `IInputLayer` 経由のマウスクリック・矢印キー・ESC キー入力を処理し `ActorSelectionService` を更新する。キーパッド入力の拡張口のみ設計考慮する（M7 では未実装） |
| `WorldActorCameraFollowController` | View Component | `ActorSelectionService.SelectedActorId` を購読し、選択時にカメラ追従 + Orthographic Size 縮小、解除時に元の値へ復元する |

#### 設計メモ

入力処理:
- `WorldActorSelectionInputHandler` は `IInputLayer` 経由でマウスクリック・矢印キー・ESC キーを取得する。`Mouse.current` / `Keyboard.current` の直接ポーリングは禁止。
- クリック選択はカメラ ray + selection proxy で行い Domain 判定にしない。Actor の selection proxy サイズは `ActorVisualSizeTier` の canvas height から View 層で計算する。
- 矢印キーは **選択状態のときのみ** 作用する。`ActorSelectionService.SelectedActorId == null` の場合は何もしない。
- 矢印キーで選択する次の Actor の決定（例: 画面上の近傍 Actor、または登録順）は `WorldActorSelectionInputHandler` または `ActorSelectionService.SelectNext()` に閉じる。

カメラ追従・拡大:
- `WorldActorCameraFollowController` は `WorldCameraController` を通じてカメラ追従と Orthographic Size 変更を行う。
- `ActorSelectionZoomRatio = 0.20f`（通常 Orthographic Size の 20% へ縮小）。コード内定数として管理し、実装後に調整可。

`WorldLifetimeScope` 登録グループ: View: アクター描画

---

### task_0008: Actor 詳細 HUD パネル

**利用する Lighthouse パターン:** [P5] アセット非同期ロード（AssetScope）

#### 作るもの

| クラス / アセット | 種別 | 内容 |
|---|---|---|
| `ActorDetailDto` | DTO | `ActorId` / `Position` / `Name` / `Stats` / `CurrentHp` / `MaxHp` / `CurrentMp` / `MaxMp` / `FatigueLevel` / `EquipmentSummary` / `Gold` / `IReadOnlyList<ActorEffectIconData> ActiveEffects` を持つ |
| `GetActorDetailQuery` | Application Query | `ActorId` を受け取り `ActorDetailDto` を返す narrow query |
| `ActorDetailPopupPresenter` | Presenter | `ActorSelectionService.SelectedActorId` を購読し、選択時に HUD パネル表示 + 位置設定 + DTO 更新、非選択時に非表示を行う |
| `ActorDetailPopup` | HUD Panel MonoBehaviour | Stats / HP / MP / 疲労 / 装備 / 所持金 / ActorEffect を表示する。Presenter 以外から直接操作しない |

#### 設計メモ

Popup の位置追従:
- `ActorDetailPopupPresenter` が毎フレーム `Camera.WorldToScreenPoint(actor.WorldPosition)` を取得し、Popup の RectTransform 位置を Actor 画面座標の横に設定する。
- `Camera.main` は禁止。`WorldCameraController` 経由でカメラ参照を取得する。

HUD Panel の開閉:
- `ActorSelectionService.SelectedActorId` が non-null に変化 → HUD パネル表示。
- `ActorSelectionService.SelectedActorId` が null に変化 → HUD パネル非表示。
- Prefab は Addressable から取得し、`WorldLifetimeScope` / `WorldUILifetimeScope` の `SerializedField` では保持しない。
- `ActorDetailPopup` の生成責務は `WorldAddressableViewFactory` に閉じ、`ActorDetailPopupPresenter` は `ActorSelectionService` と `GetActorDetailQuery` を使った表示制御だけを行う。

M7 の Popup コンテンツは表示・確認のみ。編集・指示操作は M8 以降。

`WorldLifetimeScope` 登録グループ: View: UI

---

### task_0009: プレイヤー向けイベントログ UI

**利用する Lighthouse パターン:** なし

#### 作るもの

| クラス / アセット | 種別 | 内容 |
|---|---|---|
| `PlayerEventLogEntry` | DTO | `float Timestamp` / `string Text` / `ActorId? RelatedActorId` を持つ表示エントリ |
| `PlayerEventLogStore` | Application Service | 直近 N 件（デフォルト 30）の `PlayerEventLogEntry` をインメモリ保持する。`IObservable<PlayerEventLogEntry> OnEntryAdded` を公開する |
| `PlayerEventLogFormatter` | Application Service | `IGameEvent` → 表示文字列の変換を担当する。`IActorProfileRegistry` で Actor 名を解決する。View 用文字列を `IGameEvent` に持たせない |
| `PlayerGameEventLogPresenter` | Presenter | 対象イベントを購読し `PlayerEventLogFormatter` → `PlayerEventLogStore.Add()` → View 更新のフローを実装する。`WorldDebugGameLogPresenter` とは完全別クラス |
| `PlayerEventLogView` | UI Component | 10 行固定の `Text[]` 配列を持つ。`ScrollRect` は使わない。1 行単位スクロールと一時フェードを管理する |

#### フェード仕様

- ログ表示後 **3 秒**でフェードアウト開始、**1 秒**でフェードアウト完了（コード内定数 `LogDisplaySeconds = 3f` / `LogFadeSeconds = 1f` で管理）
- マウスオーバー中はフェードを停止し 10 行すべてを表示する
- マウスが離れたらフェードを再開し、スクロール位置を最新ログへリセットする

#### スクロール仕様

- `ScrollRect` は使わない。10 行分の `Text[]` の `.text` だけを更新する
- マウスホイール上でログが古い方向へスクロール。スクロールは 1 行単位（CLI 表示と同じ動作）
- 新ログ追加時: 配列を 1 段シフトして最新ログを末尾に追加する

#### M7 対象イベントと表示例

| イベント | 表示例（日本語）|
|---|---|
| `CombatAttackOccurred` | `スライム が 冒険者A に 10 ダメージ` |
| `ProjectileFired` | `冒険者B が 矢 を発射した` |
| `ProjectileHit` | `矢 が スライム に命中した` |
| `AreaEffectCreated` | `毒沼 が発生した` |
| `AreaEffectHit` | `スライム が 毒沼 の影響を受けた` |
| `ActorDefeated` | `スライム が倒された` |
| `ItemDropped` | `スライム が アイテム を落とした` |
| `ItemPickedUp` | `冒険者A が アイテム を拾った` |
| `ActorLeveledUp` | `冒険者A がレベルアップした` |
| `ActorRecoveringAtInn` | `冒険者A が宿屋で回復中` |
| `ActorFullyRecovered` | `冒険者A が回復完了した` |

#### 設計メモ

- `PlayerEventLogFormatter` は `IActorProfileRegistry` 経由で Actor 名を取得する。`GameWorldState` を広く参照しない。
- `PlayerEventLogStore` は `GameEventHistoryService` とは独立したインメモリ store とする。全履歴への参照は持たない。
- View 更新は event-driven（`OnEntryAdded` 購読）とし、毎フレーム全履歴 polling は禁止。

`WorldLifetimeScope` 登録グループ: Application: イベント / アクター状態（`PlayerEventLogStore` / `PlayerEventLogFormatter`）、View: UI（`PlayerGameEventLogPresenter` / `PlayerEventLogView`）

---

### task_0010: M7 統合確認とセルフレビュー

追加実装なし。Phase 0〜7 の全表示物が同一 PlayMode で共存することを確認し、M8 に持ち越す残課題を `docs/self-review/milestone7-completion-review-1-claude.md` に記録する。

---

### WorldLifetimeScope / WorldUILifetimeScope M7 追加登録まとめ

M7 で追加する DI 登録のグループ別まとめ。

**View: アクター描画（WorldLifetimeScope）**
- `WorldActorCombatAnimationPresenter`（task_0003）
- `ProjectileViewRegistry`（task_0004）
- `WorldProjectilePresenter`（task_0004）
- `AreaEffectViewRegistry`（task_0005）
- `WorldAreaEffectPresenter`（task_0005）
- `WorldActorStatusPresenter`（task_0006）
- `ActorHUDViewPool`（task_0006）
- `ActorSelectionService`（task_0007）
- `WorldActorSelectionInputHandler`（task_0007）
- `WorldActorCameraFollowController`（task_0007）

**View: UI（WorldLifetimeScope）**
- `ActorDetailPopupPresenter`（task_0008）
- `PlayerGameEventLogPresenter`（task_0009）

**WorldUILifetimeScope（新規 Module Scene 専用）**
- `WorldUIModuleScene` 固有の初期化処理（task_0006）

**Application: イベント / アクター状態（WorldLifetimeScope）**
- `PlayerEventLogStore`（task_0009）
- `PlayerEventLogFormatter`（task_0009）
- `GetActorStatusSummaryQuery`（task_0006）
- `GetActorDetailQuery`（task_0008）

`ProjectileViewVisualConfig` / `AreaEffectViewVisualConfig` は ScriptableObject として `VisualConfigSettings` 経由でロードするか `WorldLifetimeScope` の Inspector にアサインするかを Phase 0 の方針確定後に決定する。


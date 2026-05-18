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

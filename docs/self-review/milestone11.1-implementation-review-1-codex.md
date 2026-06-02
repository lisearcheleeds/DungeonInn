# Milestone 11.1 Implementation Review 1 - Codex

## 対象

- GameHUD から Canvas 固定 UI を GameUI に移行
- GameHUD を World 追従 HUD 専用 Scene として再構成
- ActorStatusView / DamageNumberView を SpriteRenderer + pool 表示へ移行
- 状態異常 icon の Sprite 差し替え経路を追加

## セルフレビュー結果

- GameUI は Screen Space Canvas 上の固定 UI、Popup、ScreenStack、EventLog、Minimap、WorldHud を所有する構成になっている。
- GameHUD は ActorStatusView、DamageNumberView、Presenter、Pool、Factory のみを所有し、Canvas / RectTransform / Image に依存していない。
- World MainScene は具体 View を GameHUD に渡さず、`IActorWorldAnchorProvider` と `IWorldHudCameraProvider` を公開するだけの境界になっている。
- 旧 `IActorScreenPositionProvider` / `WorldActorScreenPositionProvider` / `ActorHUDViewPool` / `GameHud*` 実装名は Runtime / Editor / Tests から削除した。
- `docs/design/scene-design.md` と `docs/design/lifetime-scope-game-loop-design.md` は GameUI / GameHUD の現行責務に合わせて更新した。

## 完了前チェック

- `uloop.cmd compile --project-path Client`: 成功
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 356 / 356 pass
- Play mode 通常 UI 経路:
  - `NewGameButton` と `StartButton` を押下
  - `[World] GameWorldState initialized. Facilities=4 DungeonFloors=1 Actors=0` を確認
  - 初期化後 30 秒待機
  - `uloop.cmd get-logs --project-path Client --log-type Error`: 0 件
- guideline 本文とハードゲート:
  - `lighthouse-patterns.md`
  - `coding-rules.md`
  - `domain-design-guidelines.md`
  - `application-boundary-guidelines.md`
  - `implementation-quality-guidelines.md`
  - `debugging-policy.md`
  - `self-review-guidelines.md`

## 残リスク

- Play 確認では Error は 0 件だが、既存の fallback material / TextTable / TMP font warning は残っている。

## 追記: ActorStatus 表示確認

- 一時診断ログで `WorldActorStatusPresenter` の tick を確認したところ、初回は診断ログが出ず、`GameHUDLifetimeScope` が GameHUD 用 presenter / pool / factory / entry point を登録していないことが原因だった。
- `GameHUDLifetimeScope` を GameHUD 専用 composition root として復元し、`GameHUDEntryPoint` / `GameHUDViewFactory` / `ActorStatusViewPool` / `DamageNumberViewPool` / `WorldActorStatusPresenter` / `DamageNumberPresenter` を登録した。
- 修正後の一時診断ログで `ActorStatusView` の show と summary を確認した。
- `WorldActorWorldAnchorProvider` が visibility 判定で anchor 提供を失敗扱いにしていたため、座標提供の責務に限定して常に anchor を返すよう修正した。
- 一時診断ログは削除済み。
- 動的確認ログ: `[CodexValidation] ActorStatusView count=4 active=2`
- `uloop.cmd compile --project-path Client`: 成功
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 357 / 357 pass
- Play mode 通常 UI 経路で World 初期化後 30 秒動作し、Error ログ 0 件を確認した。

# Milestone 11.1 Review 3 Fix Log - Codex

## 2026-06-02 Codex Follow-up: DamageNumber Target Position

- GameHUD の DamageNumber が表示されない件を、`CombatAttackOccurred` 受信、表示レイヤー判定、Spawn 到達の順に一時診断ログで確認した。
- 確認結果として、イベント自体は受信していたが、`DamageNumberPresenter` が `IActorStatusViewDataProvider` から対象 Actor の表示データを逆引きしていたため、撃破直後や ViewData 同期順によって target view data が見つからず Spawn されないケースがあった。
- ActiveLayer が Ground のまま DungeonFloor の戦闘イベントを受けた場合は、現在表示していない階層なので Spawn しないことを確認した。このレイヤー判定は残した。
- 根本対応として、`CombatAttackOccurred` にダメージ発生時点の `TargetPosition` を追加し、`DamageNumberPresenter` は ActorStatus 用 ViewData へ依存せず、イベントの座標を `IActorWorldAnchorProvider` で World 座標へ変換するだけに変更した。
- これにより、撃破・ViewData 削除・HUD status 同期順に依存せず、表示中レイヤーのダメージ表示が生成される。
- 一時診断ログは削除済み。

### Verification

- `uloop.cmd compile --project-path Client`
  - Success: true
  - ErrorCount: 0
  - WarningCount: 0
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
  - Success: true
  - TestCount: 368
  - PassedCount: 368
  - FailedCount: 0
- Play verification
  - `Launcher.unity` から Play を開始し、`NewGameButton` と `StartButton` を `LHButton.onClick` 経路で実行した。
  - `[World] GameWorldState initialized. Facilities=4 DungeonFloors=1 Actors=0` を確認した。
  - 確認用に World の表示レイヤーを `DungeonFloor(1)` へ切り替え、戦闘発生後に `DamageNumberView` が 4 個生成されていることを確認した。
  - 追加で 30 秒 World を動作させて Stop した。
  - `uloop.cmd get-logs --project-path Client` の表示範囲に Error は出ていない。Fallback material / TextTable など既存 Warning のみ。

## 対応範囲

User 指示により、`milestone11.1-implementation-review-3-codex.md` の finding 3 はレビュー対象から除外した。

対応した finding:

- Finding 1: `VisualAssetSetup` の旧 Canvas GameHUD prefab 生成経路
- Finding 2: GameHUD pool の不可視 fallback view
- Finding 4: `scene-design.md` / `milestone11.1-roadmap.md` の設計矛盾

## 修正内容

- `VisualAssetSetup` から旧 `ActorStatusView` / `DamageNumberView` の Canvas 生成処理を削除した。
- `VisualAssetSetup` の GameHUD world-space prefab 生成は `SetupGameHUDWorldSpaceAssets.Run()` へ統一した。
- `ActorStatusViewPool` / `DamageNumberViewPool` の不可視 fallback 生成を削除し、prefab 未ロード時は設定エラーとして `InvalidOperationException` を投げるようにした。
- `GameHUDViewFactory.LoadAsync()` は必須 GameHUD prefab がロードできなければ初期化失敗として扱うようにした。
- `WorldMapLayerViewDataTests` は `ThrowingAssetManager` による fallback 成功前提をやめ、実体の `ActorStatusView` prefab 相当を渡す形にした。
- prefab 未ロード時に fallback object が作られないことを EditMode test で確認するようにした。
- `scene-design.md` の GameUI / GameHUD 責務、Addressable key、配置判断を最終設計に合わせた。
- `milestone11.1-roadmap.md` の DamageNumber digit sprite 方針を、digit 個別 Addressable ではなく `DamageDigits.png` sub-asset を `DamageNumberView.prefab` に serialized reference する設計へ統一した。

## 静的確認

- `VisualAssetSetup` から以下の旧経路が消えていることを確認した:
  - `CreateOrLoadActorStatusViewPrefab`
  - `CreateOrLoadDamageNumberViewPrefab`
  - `ConfigureDamageNumberViewPrefab`
  - `rootRectTransform`
  - `canvasGroup`
  - `digitAtlas`
- Runtime `GameHUD` に `UnityEngine.UI` / `Canvas` / `RectTransform` / `Image` / `TextMeshProUGUI` / `LHButton` がないことを確認した。
- `scene-design.md` / `milestone11.1-roadmap.md` に、今回問題にした以下の曖昧な旧記述が残っていないことを確認した:
  - `ActorStatus3DView`
  - `DamageNumber3DView`
  - `DamageNumberDigitSpriteCatalog`
  - `DamageNumbers`
  - `DamageNumber_0`
  - `GameHUD/UI/DamageNumberView`
  - `GameHUDModuleScene.HUDCanvas`
  - `画面固定なら GameHUD`

## Verification

- `uloop.cmd compile --project-path Client`
  - Success: true
  - ErrorCount: 0
  - WarningCount: 0
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
  - Success: true
  - TestCount: 359
  - PassedCount: 359
  - FailedCount: 0
- Play verification
  - Title scene を開いた状態で Play を開始。
  - `TitleView` の `NewGameButton` と `StartButton` を `LHButton.onClick` 経由で実行。
  - `[World] GameWorldState initialized. Facilities=4 DungeonFloors=1 Actors=0` を確認。
  - World 初期化後 30 秒待機して Stop。
  - `uloop.cmd get-logs --project-path Client --log-type Error --max-count 20` は 0 件。

## Notes

- Play 検証中に一度 `control-play-mode --action Status` を試し、uLoop 側の未対応 action として Error が記録された。最終確認では Error log 0 件であることを確認済み。

## 2026-06-02 Codex Follow-up: ActorStatusView HP Bar

- `ActorStatusView` の HP 表示が Circle sprite 由来で丸く見えていたため、HP background/fill だけ Unity built-in の矩形 UI sprite を使うように変更した。
- 状態アイコンは既存の placeholder sprite を維持し、HP bar と status icon の sprite 割り当て責務を `SetupGameHUDWorldSpaceAssets` 内で分離した。
- `ActorStatusView.SetHpRatio()` が fill の高さと Y offset を上書きしていたため、Prefab 側で設定した棒状レイアウトを維持するように修正した。
- Runtime で layer を補正する処理は追加していない。Prefab/Scene 生成側で World layer を設定する方針を維持した。

### Verification

- `uloop.cmd compile --project-path Client`
  - Success: true
  - ErrorCount: 0
  - WarningCount: 0
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
  - Success: true
  - TestCount: 366
  - PassedCount: 366
  - FailedCount: 0
- Play verification
  - `Launcher.unity` を開き、Play 開始後に `NewGameButton` と `StartButton` を `LHButton.onClick` 経路で実行した。
  - `[World] GameWorldState initialized. Facilities=4 DungeonFloors=1 Actors=0` を確認した。
  - World 初期化ログ確認後、35 秒待機して Stop した。
  - `uloop.cmd get-logs --project-path Client` に Error は出ていない。fallback material / text table など既存 Warning のみ。

## 2026-06-02 Codex Follow-up: Shader Driven ActorStatus HP Bar

- `ActorStatusView` の HP 表示を `HpBackground` / `HpFill` の 2 SpriteRenderer 重ね合わせから、単一 `HpBar` SpriteRenderer + 専用 shader 方式へ変更した。
- `HpBar` は既存 `BaseSprite.png` を土台にし、`DungeonInn/GameHUD/HpBar` shader と `Runtime/Art/Materials/GameHUD/HpBar.mat` で fill / background / border を描画する。
- HP 比率は Transform scale 変更ではなく、`MaterialPropertyBlock` の `_FillRatio` で renderer instance ごとに反映する。
- GameHUD 用 shader は `Runtime/Shaders/GameHUD/HpBar.shader`、material は `Runtime/Art/Materials/GameHUD/HpBar.mat` に配置した。

### Verification

- `uloop.cmd compile --project-path Client`
  - Success: true
  - ErrorCount: 0
  - WarningCount: 0
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
  - Success: true
  - TestCount: 366
  - PassedCount: 366
  - FailedCount: 0
- Play verification
  - `Launcher.unity` を開き、Play 開始後に `NewGameButton` と `StartButton` を `LHButton.onClick` 経路で実行した。
  - `[World] GameWorldState initialized. Facilities=4 DungeonFloors=1 Actors=0` を確認した。
  - World 初期化ログ確認後、35 秒待機して Stop した。
  - `uloop.cmd get-logs --project-path Client` に Error は出ていない。fallback material / text table など既存 Warning のみ。

## 2026-06-02 Codex Follow-up: HP Bar Size And Batching

- `MaterialPropertyBlock` による `_FillRatio` 更新を削除した。Unity の SRP Batcher は `MaterialPropertyBlock` 使用 Renderer を対象外にするため、HPBar は共有 Material を維持する。
- HP 比率は `SpriteRenderer.color.a` に保持し、shader 側で頂点カラー alpha を fill ratio として読む。
- HPBar の表示サイズは Transform scale ではなく `SpriteRenderer.drawMode = Sliced` と `SpriteRenderer.size` で指定する。
- HPBar の表示矩形は 120px x 28px 相当とし、shader 側で上下 10px 相当を透明 margin として扱う。
- `ActorStatusView.SetScreenScale()` は View 全体の Transform scale を変更せず、HPBar と status icon の renderer size / local position を `worldUnitsPerPixel` から更新する。

### Verification

- `uloop.cmd compile --project-path Client`
  - Success: true
  - ErrorCount: 0
  - WarningCount: 0
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
  - Success: true
  - TestCount: 366
  - PassedCount: 366
  - FailedCount: 0
- Play verification
  - `Launcher.unity` を開き、Play 開始後に `NewGameButton` と `StartButton` を `LHButton.onClick` 経路で実行した。
  - `[World] GameWorldState initialized. Facilities=4 DungeonFloors=1 Actors=0` を確認した。
  - World 初期化ログ確認後、35 秒待機して Stop した。
  - `uloop.cmd get-logs --project-path Client` に Error は出ていない。fallback material / text table など既存 Warning のみ。

## 2026-06-02 Codex Follow-up: HP Bar Pixel-based Shape

- HPBar が横に潰した正方形のように見えていた原因は、shader の border thickness が UV 比率指定で、上下 margin 後の本体高さに対して枠が太くなりすぎていたこと。
- `HpBar.shader` をピクセル基準に変更し、120px x 28px の参照サイズ、上下 10px margin、1px border、1px highlight として描画するようにした。
- BaseSprite の形状や伸縮結果ではなく、shader 内の pixel rect で HPBar の形を決める。
- `MaterialPropertyBlock` は引き続き使わず、HP 比率は `SpriteRenderer.color.a` で渡すため、共有 material 方針を維持する。

### Verification

- `uloop.cmd compile --project-path Client`
  - Success: true
  - ErrorCount: 0
  - WarningCount: 0
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
  - Success: true
  - TestCount: 366
  - PassedCount: 366
  - FailedCount: 0
- Play verification
  - `Launcher.unity` を開き、Play 開始後に `NewGameButton` と `StartButton` を `LHButton.onClick` 経路で実行した。
  - `[World] GameWorldState initialized. Facilities=4 DungeonFloors=1 Actors=0` を確認した。
  - World 初期化ログ確認後、35 秒待機して Stop した。
  - `uloop.cmd get-logs --project-path Client` に Error は出ていない。fallback material / text table など既存 Warning のみ。

## 2026-06-02 Codex Follow-up: GameHUD3D Sorting And Depth

- `World` / `WorldEffect` / `GameHUD3D` / `GameHUD3DOverlay` の SortingLayer を Editor OneShot で定義するようにした。
- ActorStatus の HP bar と status icon は `GameHUD3D` sorting layer に配置した。
- DamageNumber は `GameHUD3DOverlay` sorting layer に配置し、ActorStatus より前に描画されるようにした。
- `HpBar.shader` に `ZTest Always` / `ZWrite Off` を設定し、World geometry の depth によって壁へ埋まらないようにした。
- 状態アイコンとダメージ数字用に `SpriteOverlay.shader` / `SpriteOverlay.mat` を追加し、こちらも `ZTest Always` / `ZWrite Off` で描画する。

### Verification

- `uloop.cmd compile --project-path Client`
  - Success: true
  - ErrorCount: 0
  - WarningCount: 0
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
  - Success: true
  - TestCount: 368
  - PassedCount: 368
  - FailedCount: 0
- Play verification
  - `Launcher.unity` を開き、Play 開始後に `NewGameButton` と `StartButton` を `LHButton.onClick` 経路で実行した。
  - `[World] GameWorldState initialized. Facilities=4 DungeonFloors=1 Actors=0` を確認した。
  - World 初期化ログ確認後、35 秒待機して Stop した。
  - `uloop.cmd get-logs --project-path Client` に Error は出ていない。fallback material / text table など既存 Warning のみ。

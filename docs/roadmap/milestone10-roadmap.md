# Milestone 10 Roadmap - 永続運用ゲームループ / Save Load

## ゴール

Milestone 10 では、プレイヤーがタイトル画面からゲームを開始し、セッションを保存し、保存済みセッションを再開し、飽きるまで運用を続けられる状態にする。

このゲームには現時点で勝利条件と敗北条件を置かない。
ギルド資金がマイナスになってもゲームは終了せず、数値が上下する運営シミュレーションとして継続する。

Milestone 10 はコンテンツ量を増やすマイルストーンではなく、既存の Dungeon / Adventurer / Guild / Market / Facility / GameHUD を、永続的なプレイセッションとして成立させることを目的とする。

## 対象範囲

- タイトル / トップメニュー
- New Game 開始フロー
- Seed 入力
- 3 スロット Save / Load
- Continue による最新保存スロット再開
- GameHUD から開く System Menu
- System Menu 経由の Save / Load / Option / Title
- 日付サイクルの可視化
- 初回チュートリアル状態の保存
- 統合テストと完了レビュー

## 対象外

以下は Milestone 10 では実装しない。

- 勝利条件
- 敗北条件
- 勝利 / 敗北イベント
- Result / End Screen
- Auto Save
- Option の中身
- ギルド名入力
- 難易度入力
- 複数プロファイル
- セーブデータのバージョン移行 UI
- クラウド保存

## 確定仕様

### ゲーム終了条件

- 勝利条件はない。
- 敗北条件はない。
- ギルド資金が 0 未満、またはマイナスになってもゲームは継続する。
- M10 の完了条件に Win / Lose / Result 到達は含めない。

### タイトルメニュー

タイトル画面には以下の 5 ボタンを表示する。

- `NewGame`
- `Continue`
- `LoadGame`
- `Option`
- `Exit`

挙動:

- `NewGame`: Seed 入力を使って新規セッションを開始する。
- `Continue`: 最後に Save したスロットを自動選択してロードする。
- `LoadGame`: 3 スロット選択ウィンドウを開き、任意の保存済みスロットをロードする。
- `Option`: M10 ではボタンだけ置き、イベント設定は行わない。
- `Exit`: Unity Editor / Build で破綻しない終了処理を行う。

Continue は保存済みスロットが 1 つもない場合は無効にする。
LoadGame は保存済みスロットが 1 つもない場合でも開いてよいが、空スロットは `Empty` 表示で選択不可にする。

### New Game Seed 入力

New Game では Seed 値を入力できるようにする。

- 入力欄の placeholder は `"12345"` とする。
- 未入力または不正値の場合は `12345` を使う。
- 入力値は `DungeonSeed` と `GameRandomSeed` の両方に使う。
- ギルド名と難易度は M10 では実装しない。

### Save / Load スロット

セーブスロットは 3 つとする。

- Slot 1
- Slot 2
- Slot 3

NewGame で始めたセッションで Save するときは、どのスロットに上書きするかを選択するウィンドウを表示する。
LoadGame / Continue でロードしたセッションでも、Save 時は常に 3 スロット選択ウィンドウを表示する。
ロード元スロットには `"*"` を表示し、現在のセッションがどのスロットからロードされたか分かるようにする。

空スロット:

- LoadGame では `Empty` 表示にする。
- LoadGame では空スロットを選択不可にする。
- Save では空スロットも選択可能にする。

Continue:

- 最後に Save したスロットを自動的に選択してロードする。
- 最後に Save したスロット情報は Save metadata として保持する。

Auto Save:

- M10 では実装しない。
- Save はユーザー操作による手動保存のみとする。

### GameHUD System Menu

システムとコンテンツを切り分けるため、GameHUD に設定ボタンを表示する。
設定ボタンを押すと System Menu を表示する。

System Menu には以下の 4 ボタンを表示する。

- `Save`
- `Load`
- `Option`
- `Title`

挙動:

- `Save`: 3 スロット選択ウィンドウを開く。空スロットも選択可能。
- `Load`: 3 スロット選択ウィンドウを開く。空スロットは `Empty` 表示で選択不可。
- `Option`: M10 ではボタンだけ置き、イベント設定は行わない。
- `Title`: タイトルへ戻る。戻る前に自動保存はしない。

Title へ戻る場合、`IProductSceneManager` の通常遷移を使い、既存の session exit 処理で現在の GameSession scope を破棄する。
未保存変更の確認ダイアログは M10 では実装しない。

### 初回チュートリアル

初回チュートリアル状態は SaveData に含める。
Continue / LoadGame 後に表示済み案内が過剰に繰り返されないようにする。

## Phase 一覧

### Phase 1: 仕様反映とロードマップ確定

作業内容:

- 勝利 / 敗北 / Result / Auto Save を M10 対象外として整理する
- New Game Seed 入力仕様を docs に明記する
- 3 スロット Save / Load 仕様を docs に明記する
- Title menu の 5 ボタン仕様を docs に明記する
- GameHUD System Menu の 4 ボタン仕様を docs に明記する
- Tutorial state を SaveData に含めることを docs に明記する

完了条件:

- [ ] M10 完了条件から Win / Lose / Result / Auto Save が削除されている
- [ ] Seed 入力、3 スロット、Continue、System Menu の仕様が docs に明記されている
- [ ] Option はボタンのみでイベント未設定と明記されている

### Phase 2: タイトル / トップメニュー

タイトル画面を正式なゲーム開始入口として整える。

作業内容:

- 既存の単一 Start ボタン導線を破壊的に置き換える
- NewGame / Continue / LoadGame / Option / Exit の 5 ボタンを表示する
- Continue は最新保存スロットが存在する場合だけ有効にする
- LoadGame は 3 スロット選択ウィンドウを開く
- 空スロットは `Empty` 表示で選択不可にする
- Option はボタンのみ配置し、イベントを設定しない
- Exit の扱いを Unity Editor / Build で分ける

完了条件:

- [ ] Title に NewGame / Continue / LoadGame / Option / Exit が表示される
- [ ] Continue が最新保存スロットの有無で有効 / 無効になる
- [ ] LoadGame で 3 スロット選択ウィンドウが開く
- [ ] 空スロットが `Empty` 表示で選択不可になる
- [ ] Option ボタンは存在するがイベント未設定である
- [ ] Exit が Editor / Build で破綻しない

### Phase 3: New Game / Seed 入力

新規プレイセッションの初期化を明示的なフローとして定義する。

作業内容:

- NewGame 選択時に Seed 入力 UI を表示する
- Seed placeholder を `"12345"` にする
- 未入力 / 不正値の場合は `12345` にフォールバックする
- 入力 Seed を `DungeonSeed` と `GameRandomSeed` の両方に使う
- GameSession scope を開始してから World へ遷移する
- World 初期化時に New Game 用 settings を使う

完了条件:

- [ ] NewGame から Seed 入力を経由して World 初期化まで進む
- [ ] Seed 未入力 / 不正値で `12345` が使われる
- [ ] 入力 Seed が DungeonSeed と GameRandomSeed の両方に反映される
- [ ] `[World] GameWorldState initialized` が出力される

### Phase 4: Save / Load 基盤

ゲーム進行状態を 3 スロットに保存し、タイトルまたは GameHUD から復元できるようにする。

作業内容:

- SaveData DTO を設計する
- Save metadata を設計する
  - slot id
  - saved at
  - display day
  - seed
  - latest saved slot marker
- 保存対象を確定する
  - GameClock
  - Guild
  - Facilities
  - Adventurer / GuildStaff の継続状態
  - Inventory
  - Dungeon
  - Economy / Transaction
  - Tutorial state
  - New Game seed
- 保存対象外を確定する
  - View instance
  - Presenter state
  - Addressable handle
  - Scene-owned object
  - ScreenStack 開閉状態
  - Actor 座標
  - 探索中の一時状態
  - Monster
  - Projectile / AreaEffect
- JSON または独自形式で保存 / 読込を実装する
- Slot 1〜3 の path / metadata 管理を実装する
- Continue から latest saved slot を復元する
- LoadGame から任意の保存済み slot を復元する

完了条件:

- [ ] Slot 1〜3 に手動保存できる
- [ ] latest saved slot が metadata として保存される
- [ ] Continue が latest saved slot をロードする
- [ ] LoadGame が選択した slot をロードする
- [ ] 保存対象に View / Unity instance が混入していない
- [ ] Tutorial state が SaveData に含まれている

### Phase 5: GameHUD System Menu

ゲーム中のシステム操作を GameHUD の設定ボタンから開けるようにする。

作業内容:

- GameHUD に設定ボタンを追加する
- 設定ボタンから System Menu を開く
- System Menu に Save / Load / Option / Title を表示する
- Save から 3 スロット選択ウィンドウを開く
- Load から 3 スロット選択ウィンドウを開く
- Option はボタンのみ配置し、イベントを設定しない
- Title で `IProductSceneManager` 経由でタイトルへ戻り、既存の session exit 処理で GameSession scope を破棄する

完了条件:

- [ ] GameHUD に設定ボタンが表示される
- [ ] 設定ボタンから System Menu が開く
- [ ] System Menu に Save / Load / Option / Title が表示される
- [ ] Save で空スロットを含む 3 スロット選択が表示される
- [ ] Load で空スロットが選択不可になる
- [ ] Option ボタンは存在するがイベント未設定である
- [ ] Title で Title scene へ戻り、既存の session exit 処理で GameSession scope が破棄される

### Phase 6: Slot 選択 UI

Title と GameHUD の両方から使える 3 スロット選択 UI を整える。

作業内容:

- Save 用 slot selection と Load 用 slot selection を同じ設計で扱う
- 空スロットは Save では選択可能、Load では選択不可にする
- ロード元スロットに `"*"` を表示する
- slot ごとに保存日時、日数、seed を表示する
- 上書き保存時の確認は M10 では必須にしない

完了条件:

- [ ] 3 スロットが常に表示される
- [ ] Save と Load で空スロットの可否が切り替わる
- [ ] ロード元 slot に `"*"` が表示される
- [ ] 保存済み slot に保存日時、日数、seed が表示される
- [ ] 空 slot は `Empty` と表示される

### Phase 7: 日付サイクルの可視化

ゲーム内時間の進行をプレイヤーが理解できるようにする。

作業内容:

- 既存の HUD 日数 / 時刻表示を正式な日付サイクル表示として整理する
- 時間帯を HUD に追加表示する
- 朝 / 昼 / 夕 / 夜の時間帯変化を表示する
- 日付切替時の通知またはログを表示する
- Auto Save とは接続しない

完了条件:

- [ ] HUD で現在日数、時刻、時間帯が確認できる
- [ ] 日付切替が視覚的またはログで確認できる
- [ ] Auto Save 用の処理が追加されていない

### Phase 8: 初回チュートリアル

最初の数日間で、既存の主要画面と操作を自然に案内する。

作業内容:

- チュートリアル対象を決める
  - DungeonInfo
  - GuildManagement
  - Market
  - System Menu
  - Save / Load
- チュートリアル進行状態を Application state として管理する
- 表示済み guide を SaveData に含める
- UI 操作を邪魔しない軽量な表示を実装する

完了条件:

- [ ] NewGame 後に最初の案内が表示される
- [ ] 主要画面と System Menu へ誘導できる
- [ ] 表示済みチュートリアルが繰り返し過ぎない
- [ ] Save / Load 後のチュートリアル状態が破綻しない

### Phase 9: 統合テスト / Play 確認

NewGame から Save / Load / Continue までの流れを機械的に検証できるようにする。

作業内容:

- NewGame から World 開始までのテストを追加する
- Seed 入力の EditMode test を追加する
- Save / Load の EditMode test を追加する
- Continue 復元テストを追加する
- Slot metadata test を追加する
- Tutorial state 保存 / 復元 test を追加する
- Launcher scene から Play し、Title の NewGame ボタンを押して World へ入る通常導線を確認する

完了条件:

- [ ] `uloop.cmd compile --project-path Client` が成功する
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する
- [ ] Launcher scene から Play し、Title の NewGame 操作後に World が起動する
- [ ] Play 30秒確認で Error ログがない
- [ ] `[World] GameWorldState initialized` が出力される
- [ ] NewGame / Save / LoadGame / Continue / System Menu / Title return の主要導線が確認済み

### Phase 10: Milestone 10 完了レビュー

永続運用ゲームループとして、設計・実装・動作確認を横断レビューする。

作業内容:

- セルフレビューを実施する
- Lighthouse guideline 適合性を確認する
- Application boundary guideline 適合性を確認する
- Save / Load の責務境界を確認する
- Slot metadata と latest slot の整合性を確認する
- GameHUD System Menu の Scene / Module 所有境界を確認する
- Milestone 11 へ送る残課題を整理する

完了条件:

- [ ] `docs/self-review/` に Milestone 10 完了レビューが作成されている
- [ ] 未対応項目と M11 送り項目が区別されている
- [ ] ハードゲート違反がない
- [ ] コンパイル、EditMode test、Play 確認が完了している

## Milestone 11 へ移動する項目

Milestone 10 では、永続運用のために必要な最小範囲に集中する。
以下は Milestone 11 以降で扱う。

- 勝利 / 敗北条件
- Result / End Screen
- Auto Save
- Option の中身
- ギルド名入力
- 難易度入力
- 未保存変更確認ダイアログ
- セーブデータのバージョン移行 UI
- コンテンツ拡充
  - モンスター種類
  - 装備種類
  - アイテム種類
  - ダンジョン深層
- Pet システム
- Faction 勢力管理 UI
- 本格的な価格設定 UI
- GuildStaff / スタッフ管理
- 演出強化
  - オーディオ
  - パーティクル
  - ビジュアルポリッシュ

## Milestone 10 完了条件

- [ ] タイトル画面に NewGame / Continue / LoadGame / Option / Exit が表示される
- [ ] NewGame で Seed 入力後に新規セッションを開始できる
- [ ] Seed placeholder が `"12345"` で、未入力 / 不正値でも `12345` で開始できる
- [ ] 入力 Seed が DungeonSeed と GameRandomSeed に反映される
- [ ] 3 スロットへ手動保存できる
- [ ] LoadGame で保存済み slot を選んで再開できる
- [ ] Continue で最後に Save した slot を自動再開できる
- [ ] GameHUD の設定ボタンから System Menu を開ける
- [ ] System Menu から Save / Load / Title が動作する
- [ ] System Menu の Option はボタンのみ存在し、イベント未設定である
- [ ] 空 slot は `Empty` 表示で、Load では選択不可になる
- [ ] ロード元 slot に `"*"` が表示される
- [ ] Save / Load 後も Guild / Facility / Actor / Inventory / Dungeon / Clock / Tutorial state が復元される
- [ ] HUD で現在日数、時刻、時間帯が確認できる
- [ ] 初回チュートリアルで主要画面と System Menu へ誘導できる
- [ ] Launcher scene から Play し、Title の NewGame 操作後に World が起動する
- [ ] `uloop.cmd compile --project-path Client` が成功する
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する
- [ ] Play 30秒確認で Error ログがない

## 現行コード確認レビュー

2026-05-27 時点の `Client/Assets/DungeonInn/Runtime/Scripts` を確認した結果、Milestone 10 は「既存の Title -> World 直行導線と GameSession scope を、正式な NewGame / Continue / LoadGame / Save / System Menu flow へ置き換える」作業として扱う。

### Phase 1: 仕様反映

現行コード:

- `WorldGameSettingsSO` に初期 dungeon seed、random seed、初期資金、初期施設値、初期ギルド inventory が既にある。
- 勝利 / 敗北 / Result / Auto Save は存在しない。
- セーブスロット、latest slot、Tutorial SaveData は存在しない。

修正方針:

- 勝利 / 敗北 / Result / Auto Save は M10 対象外として扱う。
- `WorldGameSettingsSO` の既存初期値を使いつつ、NewGame Seed 入力で dungeon seed / random seed を上書きできるようにする。

### Phase 2: タイトル / トップメニュー

現行コード:

- `TitleView` は `startGameButton` 1 つだけを持つ。
- `TitlePresenter` は `StartNewGameAsync()` で `GameSessionLifecycle.BeginSession()` を呼び、直接 `WorldScene.WorldTransitionData` へ遷移している。
- Continue / LoadGame / Option / Exit / Save metadata 判定は存在しない。

修正方針:

- 単一 Start 導線は 5 ボタンの正式メニューへ破壊的に置き換える。
- `TitlePresenter` は SaveData IO や GameWorldState 初期化を持たず、開始 request と scene transition の調整に留める。
- Continue 可否は Save metadata query から取得する。
- Title UI は 1280x720 を基準に、タイトル領域とメニュー領域を明確に分離する。タイトルテキストは上部中央、5 ボタンは中央縦並びにし、Seed / Load slot panel はメニューと重ならない位置または ScreenStack window として表示する。
- TitleView は既存 Scene の Canvas / RectTransform / CanvasScaler に合わせ、ボタンやタイトルを viewport 中央からの固定値だけで積むのではなく、上部タイトル領域・中央メニュー領域・補助パネル領域の親 RectTransform を分けて配置する。
- 1280x720 Play 確認で、Title text / NewGame / Continue / LoadGame / Option / Exit / Seed panel / Load slot panel が重ならないことを完了条件に含める。

### Phase 3: New Game / Seed

現行コード:

- `GameSessionLifecycle` と `GameSessionLifetimeScope` は既に存在する。
- `WorldSimulationOrchestrator.InitializeAsync()` は常に `WorldGameSettingsRepository.GetInitialWorldSettings()` から新規 World を初期化する。
- NewGame Seed 入力は存在しない。

修正方針:

- `WorldSimulationOrchestrator.InitializeAsync()` に NewGame / Load の区別がない状態を正式フローへ直す。
- NewGame では seed 入力を `DungeonSeed` と `GameRandomSeed` に反映する。
- Load / Continue では新規初期化を走らせず、復元済み state を検証する。

### Phase 4: Save / Load 基盤

現行コード:

- Runtime に GameSaveData / IGameSaveRepository / SaveGameUseCase / LoadGameUseCase 相当は存在しない。
- `ProductTextTableLoader` などの infrastructure loader はあるが、ゲーム状態保存用 repository はない。
- `GameClock` は現在 tick / day / elapsed time を持つが、復元用 contract はない。

修正方針:

- Save / Load は新規 Application + Infrastructure 機能として追加する。
- `GameClock` や `GameWorldState` に安易な public setter を追加するのではなく、復元責務を明確にした usecase / state restoration 経路を設計する。
- SaveData には View / Presenter / Addressable handle / scene-owned object を含めない。
- Actor 座標、探索中の一時状態、Monster、Projectile、AreaEffect は SaveData に含めない。再開時、冒険者は地上の開始位置へ戻し、Monster は spawn table で再生成する。

### Phase 5: GameHUD System Menu

現行コード:

- GameHUD は WorldHud、DungeonInfo、GuildManagement、Market の導線を持つ。
- System Menu / Save / Load / Title 導線は存在しない。

修正方針:

- GameHUD に設定ボタンを追加し、System Menu を GameHUD-owned ScreenStack window として開く。
- GameHUD の設定ボタン自体は HUD 上に常時表示してよいが、押下後に表示される System Menu 本体は `WorldHudView` 配下の動的 Panel ではなく ScreenStack window として表現する。
- Save / Load slot selection は Title と GameHUD の両方から使うため、GameHUD 専有ではなく共通 ScreenStack window として追加する。
- Title へ戻る時は `IProductSceneManager` の通常遷移を使い、`ProductSceneManager.EndSessionIfSessionExit()` に GameSession scope 破棄を任せる。

### Phase 6: Slot 選択 UI

現行コード:

- GameHUD-owned ScreenStack window の基盤は存在する。
- Slot selection window は存在しない。

修正方針:

- Slot selection は Title と GameHUD の両方から開ける共通 ScreenStack window として追加する。
- Save 用と Load 用で同じ View を使い、mode によって空 slot の選択可否を変える。
- ロード元 slot の `"*"` は Save metadata / session state から ViewData に変換する。

### Phase 7: 日付サイクル

現行コード:

- `GameClock` は `CurrentDay`、`CurrentTickOfDay`、pause、time scale を持つ。
- `WorldHudPresenter` は `WorldHudScreenService.GetTimeState()` を使い、`Day {n} HH:MM` と Gold を表示している。
- `WorldHudPresenter` は `MinutesPerDay = 1440` を自前で持ち、時刻文字列を Presenter 内で組み立てている。
- 朝 / 昼 / 夕 / 夜の時間帯表示、日付切替通知は存在しない。

修正方針:

- 現在日数 / 時刻表示は既存 HUD 実装を活かす。
- 追加対象は時間帯表示と日付切替通知に絞る。
- 時刻 / 時間帯の表示用変換は View や Presenter の magic number ではなく、Application 側の ViewData 生成に寄せる。
- Auto Save とは接続しない。

### Phase 8: 初回チュートリアル

現行コード:

- TutorialProgressService / TutorialGuidePresenter / Tutorial SaveData は存在しない。
- DungeonInfo / GuildManagement / Market の window open service は存在する。

修正方針:

- Tutorial は既存 GameHUD window open 導線と System Menu を案内する軽量 guide として設計する。
- チュートリアル進行状態は Presenter の一時 field ではなく Application state に置く。
- Tutorial state は SaveData に含める。

### Phase 9: 統合テスト / Play 確認

現行コード:

- 既存 EditMode tests は Milestone 9 までの GameLoop / UI / Market / Facility を中心に存在する。
- NewGame / Save / LoadGame / Continue / System Menu を通す統合テストはまだない。

修正方針:

- M10 の完了判定は compile と既存 tests だけでは不可とする。
- NewGame / Save / LoadGame / Continue / System Menu / Title return を個別 test と Play 確認に含める。
- Play 確認では Launcher scene から起動し、Title の NewGame 操作後に World へ入る通常導線を必須にする。

### Phase 10: 完了レビュー

現行コード:

- M10 は未実装のため完了レビュー対象ではない。
- `docs/self-review/` には Milestone 9 までのレビューがある。

修正方針:

- M10 完了時は Save / Load の保存対象、slot metadata、latest slot、System Menu の Scene / Module 所有境界を重点レビューする。
- 完了レビューでは「現行コード確認レビュー」の各 Phase 修正方針が実装で満たされているか照合する。

## 実装前の理想設計方針

### レビュー判断

Milestone 10 は、既存の World 実行基盤に「NewGame Seed 開始」「3 スロット Save / Load」「Continue」「GameHUD System Menu」「Tutorial state 保存」を追加するマイルストーンである。
実装の中心は View を増やすことではなく、1 回のゲームセッションの寿命、保存対象、復元経路、UI 所有境界を Application / GameSession / View 境界で明確にすることである。

勝利 / 敗北 / Result / Auto Save は M10 の対象外である。

### 全体原則

- Product / bootstrap は最初の入口 MainScene へ遷移するだけに留め、実ゲームセッションの開始判断を持たない。
- Title / Menu は NewGame / Continue / LoadGame / Option / Exit の意思決定を持つが、GameWorldState の中身を直接生成・変更しない。
- GameSessionLifetimeScope は 1 プレイセッションで共有する Application / Domain state の正しい所有者とする。
- World MainScene は開始済みセッションを表示・操作する場であり、セッション生成や SaveData の IO を持たない。
- GameHUD / ScreenStack ModuleScene は UI 表現を所有する。Save / Load / NewGame 初期化を UI 側へ逃がさない。
- SaveData は復元可能な snapshot であり、Domain live object、UnityEngine.Object、Addressable handle、View state を含めない。
- Save / Load は手動操作のみ。Auto Save は追加しない。
- UseCase はステートレス、長期状態は Service / StateService に分ける。
- 複数 UseCase の順序制御は Orchestrator / Coordinator が持つ。UseCase から UseCase を直接呼ばない。
- 既存互換より理想設計を優先し、単一 Start 導線や NewGame 専用初期化を正式なセッション開始契約へ破壊的に置き換える。

### 望ましい大枠

```text
Core / Product
  EntrySceneTransitionService
  ProductSceneManager
  GameSessionLifecycle
  IProductSceneManager

GameSession
  GameSessionLifetimeScope
  GameSessionStartRequest
  NewGameSessionRequest
  LoadGameSessionRequest
  GameSessionStartCoordinator
  ActiveSaveSlotService

Application
  NewGame
    InitializeNewGameSessionUseCase
    NewGameInitialSettings
    NewGameSeedParser
  SaveLoad
    GameSaveData
    GameSaveMetadata
    GameSaveSlotId
    GameSaveSlotSummary
    IGameSaveRepository
    CreateGameSaveSnapshotUseCase
    RestoreGameSaveSnapshotUseCase
    SaveGameUseCase
    LoadGameUseCase
    GetSaveSlotSummariesUseCase
    GetLatestSaveSlotUseCase
  Tutorial
    TutorialProgressService
    GetTutorialGuidanceUseCase
    MarkTutorialStepSeenUseCase
  HUD
    GetGameClockViewDataUseCase

Infrastructure
  SaveLoad
    JsonGameSaveRepository
    GameSaveFilePathProvider

View
  MainScene/Title
    TitleView
    TitlePresenter
    TitleLifetimeScope
    SeedInputView
  ModuleScene/ScreenStack
    SaveSlotSelectionWindow
    SaveSlotSelectionWindowData
    SaveSlotSelectionWindowViewData
  ModuleScene/GameHUD
    DateCycleHudPresenter
    TutorialGuidePresenter
  ModuleScene/GameHUD/ScreenStack
    SystemMenuWindow
    SystemMenuWindowData
```

### Task 1: 仕様反映の設計

目的:

M10 を勝敗到達型ではなく、永続運用 Save / Load 型の milestone として固定する。

理想構成:

- docs 上で勝利 / 敗北 / Result / Auto Save を対象外にする。
- `GameSessionStatusService` や `GameSessionEnded` は M10 では追加しない。
- SaveData には Tutorial state と NewGame seed を含める。

禁止:

- 勝敗条件を暫定実装すること。
- Auto Save を「ついで」に追加すること。
- Result window を M10 に混ぜること。

完了条件:

- [ ] docs から Win / Lose / Result / Auto Save 完了条件が消えている
- [ ] M10 の主目的が Save / Load / Continue / System Menu になっている

### Task 2: Title / Top Menu の設計

目的:

Title を正式なゲーム開始入口にし、単一 Start ボタンを 5 ボタン menu へ置き換える。

理想構成:

- `TitleView`
  - NewGame / Continue / LoadGame / Option / Exit の `LHButton`
  - Seed 入力表示の入口
  - Save slot window 表示の入口
  - 1280x720 基準の安定した Title layout root
  - タイトル領域、メニュー領域、補助パネル領域を分けた RectTransform
- `TitlePresenter`
  - Save metadata query から Continue 可否を取得
  - NewGame flow / Continue flow / LoadGame flow を開始
  - Option は listener 未設定
  - Exit を Editor / Build で分岐
- `GameSessionStartCoordinator`
  - BeginSession
  - NewGame / LoadGame request を GameSession 側へ渡す
  - World transition
  - 失敗時 EndSession

所有者:

- Title MainScene / TitleLifetimeScope

配置:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/Title/`
- Save metadata query は Application / SaveLoad

UI レイアウト:

- Title は 1280x720 を基準にする。
- 画面上部に `Dungeon Inn` のタイトル領域を固定し、ボタン列と重ならない高さを確保する。
- 5 ボタンは中央の menu column に縦並びで配置し、ボタン間隔は固定しつつ最下部が画面外へ出ないようにする。
- Seed 入力 panel / Load slot panel は menu column と重ならない補助領域へ配置する。画面幅が足りない場合は ScreenStack window として開く設計を優先する。
- `TitleText` と動的に生成するタイトルテキストを二重表示しない。既存 Scene 上の title object を使うか、新規 root を使う場合は旧 title object を明示的に非表示にする。

破壊的に直す点:

- `TitleView.startGameButton` 前提を廃止する。
- `TitlePresenter.StartNewGameAsync()` の直接 World 遷移を開始 request flow へ置き換える。
- 既存 `TitleText` / `StartGameButton` と新規生成 UI が混在して位置崩れを起こす状態を解消する。

禁止:

- `TitleView` が SaveData を読むこと。
- `TitlePresenter` が JSON / file IO を直接呼ぶこと。
- `TitlePresenter` が GameWorldState を直接初期化すること。
- タイトルテキストとボタンを同じ座標系へ場当たり的に積み、1280x720 で重なりを起こすこと。
- 旧 `TitleText` と新規 `Dungeon Inn` テキストを同時表示すること。

完了条件:

- [ ] 5 ボタン menu になっている
- [ ] Continue 可否が save metadata 由来である
- [ ] Option はイベント未設定である
- [ ] `IProductSceneManager` で World / Title 遷移している
- [ ] 1280x720 で Title text と 5 ボタンが重ならない
- [ ] 1280x720 で Seed panel / Load slot panel が menu column を覆って操作不能にしない

### Task 3: NewGame / Seed の設計

目的:

NewGame 開始時に Seed を入力し、DungeonSeed と GameRandomSeed に反映する。

理想構成:

- `NewGameSeedParser`
  - 文字列を int に変換
  - 未入力 / 不正値は `12345`
- `NewGameSessionRequest`
  - `DungeonSeed`
  - `GameRandomSeed`
- `InitializeNewGameSessionUseCase`
  - NewGame request を受ける
  - `WorldGameSettingsSO` 由来の初期値に seed だけを上書きする
  - GameRandom 初期化と World 初期化へ渡す

所有者:

- Seed UI は Title MainScene
- NewGame 初期化は GameSessionLifetimeScope / Application

配置:

- Seed UI: `View/Scene/MainScene/Title/`
- Parser / Request / UseCase: `Application/NewGame/` または `Application/SaveLoad/SessionStart/`

禁止:

- Seed を View の field に保持したまま World 初期化で直接読むこと。
- `WorldSimulationOrchestrator` に magic number として `12345` を置くこと。
- Load / Continue 時に NewGame 初期化を再実行すること。

完了条件:

- [ ] placeholder が `"12345"` である
- [ ] 未入力 / 不正値で `12345` になる
- [ ] 入力 seed が dungeon seed / random seed に反映される
- [ ] Load / Continue では NewGame 初期化が走らない

### Task 4: Save / Load 基盤の設計

目的:

GameSession の状態を 3 スロットに保存し、Title / GameHUD から復元できるようにする。

理想構成:

- `GameSaveData`
  - version
  - metadata
  - clock snapshot
  - guild snapshot
  - facility snapshot
  - adventurer / staff snapshot
  - inventory snapshot
  - dungeon snapshot
  - economy / transaction snapshot
  - tutorial snapshot
  - new game seed
  - actor position, monster, projectile, area effect は含めない
- `GameSaveMetadata`
  - slot id
  - saved at
  - display day
  - seed
  - is latest
- `ActiveSaveSlotService`
  - 現在のセッションがどの slot からロードされたかを GameSession 中だけ保持する
  - NewGame 直後は未設定
  - Save 成功時に保存先 slot を active slot として更新する
- `IGameSaveRepository`
  - `Save(slotId, data)`
  - `TryLoad(slotId)`
  - `GetSlotSummaries()`
  - `GetLatestSlot()`
- `JsonGameSaveRepository`
  - file / JSON / persistentDataPath を所有する

所有者:

- Application: DTO / UseCase / Repository interface
- Infrastructure: file implementation
- View: slot summary 表示のみ

配置:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/SaveLoad/`
- `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/SaveLoad/`

破壊的に直す点:

- `WorldSimulationOrchestrator.InitializeAsync()` の「常に新規初期化」を Load 復元対応へ直す。
- `GameClock` / `GameWorldState` の復元経路を正式契約として追加する。
- latest slot は永続 metadata、active loaded slot は GameSession state として分離する。

禁止:

- Domain live object をそのまま JSON serializer に渡すこと。
- SaveData に `UnityEngine.Object` を含めること。
- Actor 座標、探索中の一時状態、Monster、Projectile、AreaEffect を SaveData に含めること。
- View / Presenter が `IGameSaveRepository` を直接呼ぶこと。
- Auto Save を追加すること。

完了条件:

- [ ] 3 スロット保存 / 読込ができる
- [ ] latest slot が保存される
- [ ] Tutorial state が保存される
- [ ] Save / Load roundtrip test がある

### Task 5: GameHUD System Menu の設計

目的:

ゲーム中のシステム操作をコンテンツ UI から切り分ける。

理想構成:

- GameHUD に設定ボタンを追加する。
- `WorldHudView` に設定ボタンを追加し、`WorldHudPresenter` は `IGameHudWindowOpenService.OpenSystemMenu()` だけを呼ぶ。
- `GameHudWindowOpenService` は ScreenStack module に `SystemMenuWindowData` を渡す。
- 設定ボタンは `SystemMenuWindow` を開く。
- `SystemMenuWindow`
  - Save
  - Load
  - Option
  - Title
- Save / Load は共通 ScreenStack の `SaveSlotSelectionWindow` を開く。
- Title は `IProductSceneManager` で Title scene へ戻る。GameSession scope 破棄は `ProductSceneManager.EndSessionIfSessionExit()` に任せる。

ScreenStack window としての理想クラス・リレーション:

```text
WorldHudView
  -> settings button click を WorldHudPresenter へ通知する
  -> System Menu 本体は持たない

WorldHudPresenter
  -> IGameHudWindowOpenService.OpenSystemMenu() を呼ぶ
  -> Save / Load / Title の実処理は直接持たない

IGameHudWindowOpenService / GameHudWindowOpenService
  -> GameHUD 側から ScreenStack を開くための境界
  -> SystemMenuWindowData を生成または受け取り、ScreenStack へ渡す
  -> Window prefab / View 実体には依存しない

SystemMenuWindowData
  -> Save / Load / Option / Title の選択時 callback または command entry を持つ
  -> 表示文言・選択可否などの ViewData を持つ
  -> file IO / GameWorldState / Domain live object は持たない

SystemMenuWindow
  -> ScreenStack window として表示される View
  -> Save / Load / Option / Title button を描画する
  -> button click を SystemMenuWindowData の command へ通知する
  -> SaveData repository や scene transition を直接呼ばない

SystemMenuActionHandler または SystemMenuPresenter
  -> SystemMenuWindowData の command 実体を組み立てる
  -> Save 選択時は SaveSlotSelectionWindow を開く
  -> Load 選択時は SaveSlotSelectionWindow を開く
  -> Title 選択時は IProductSceneManager へ遷移依頼する
  -> Option は M10 では no-op / 未設定に留める

SaveSlotSelectionWindow
  -> Title / GameHUD から共通利用される ScreenStack window
  -> slot summary ViewData の表示と slot 選択通知だけを持つ
  -> Save / Load の実行は呼び出し元から渡された command に委譲する
```

依存方向:

- `WorldHudView -> WorldHudPresenter -> IGameHudWindowOpenService -> ScreenStack`
- `SystemMenuWindow -> SystemMenuWindowData -> command`
- `SystemMenuWindow` から Application / Infrastructure へ直接依存しない。
- `SaveGameUseCase` / `GetSaveSlotSummariesUseCase` / `GameSessionStartCoordinator` などの Application / GameSession 処理は、Window ではなく Presenter / ActionHandler / WindowOpenService 側で接続する。

Lifetime:

- `WorldHudView` / `WorldHudPresenter` / `GameHudWindowOpenService` は GameHUD ModuleScene / GameHUDLifetimeScope 所有。
- `SystemMenuWindow` は GameHUD-owned ScreenStack window として ScreenStack ModuleScene 上に表示されるが、開く責務は GameHUD 側 service が持つ。
- `SaveSlotSelectionWindow` は Title / GameHUD 共通の ScreenStack window として、どちらの scene からも window data 経由で開ける。

所有者:

- GameHUD ModuleScene / GameHUDLifetimeScope
- `SystemMenuWindow` は GameHUD-owned ScreenStack 配下
- `SaveSlotSelectionWindow` は Title / GameHUD 共用の ScreenStack 配下
- `WorldHudView` は設定ボタンの表示とクリック通知だけを所有し、System Menu の GameObject / Panel / button 群を所有しない

配置:

- `View/Scene/ModuleScene/GameHUD/`
- `View/Scene/ModuleScene/GameHUD/ScreenStack/`
- `View/Scene/ModuleScene/ScreenStack/SaveLoad/`

禁止:

- World MainScene に System Menu Canvas を直置きすること。
- `WorldHudView` が `SystemMenuPanel` や Save / Load slot panel を動的生成して保持すること。
- `WorldHudPresenter` が System Menu のボタン単位の表示制御を直接持つこと。
- System Menu が file IO を直接行うこと。
- Title へ戻る時に SceneManager を直接呼ぶこと。
- Title へ戻る前に View / Window から `GameSessionLifecycle.EndSession()` を直接呼ぶこと。
- Option の中身を M10 で実装すること。

完了条件:

- [ ] GameHUD 設定ボタンから System Menu が開く
- [ ] System Menu は ScreenStack window として開く
- [ ] `WorldHudView` 配下に System Menu 本体の動的 Panel を持たない
- [ ] Save / Load / Option / Title が表示される
- [ ] Option はイベント未設定である
- [ ] Title で session が破棄される

### Task 6: Slot 選択 UI の設計

目的:

Title と GameHUD の両方で使える 3 スロット選択 UI を作る。

理想構成:

- `SaveSlotSelectionWindowData`
  - mode: Save / Load
  - slot summaries
  - active loaded slot id
  - callbacks
- `SaveSlotSelectionWindowViewData`
  - slot label
  - saved at
  - day
  - seed
  - is empty
  - is selectable
  - is active loaded slot

所有者:

- ScreenStack ModuleScene / ScreenStackLifetimeScope
- Title / GameHUD は window data と callback を渡すだけで、window 実体を所有しない

配置:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/ScreenStack/SaveLoad/`

表示:

- 空 slot: `Empty`
- ロード元 slot: `"*"` 表示
- Save mode: 空 slot 選択可
- Load mode: 空 slot 選択不可

禁止:

- Save mode と Load mode で別々の重複 window を作ること。
- Window が repository を直接読むこと。
- GameHUD 専有 namespace / folder に置き、Title から GameHUD UI へ依存させること。
- 空 slot を Load 可能にすること。

完了条件:

- [ ] 3 スロットが常に表示される
- [ ] Save / Load mode で選択可否が変わる
- [ ] ロード元 slot に `"*"` が表示される
- [ ] 空 slot は `Empty` 表示である

### Task 7: 日付サイクル HUD の設計

目的:

既存 HUD の日数 / 時刻表示を活かし、時間帯表示を追加する。

理想構成:

- `GetGameClockViewDataUseCase` または既存 `GetGameTimeStateUseCase` 拡張
  - current day
  - current time
  - time period
  - pause
  - time scale
- `WorldHudPresenter`
  - ViewData を表示する
  - 時間進行は呼ばない
- `GameClockViewData`
  - 表示文字列または表示に必要な値を持つ
  - 時間帯判定を View / Presenter へ漏らさない

禁止:

- HUD が `GameClock.Advance` を呼ぶこと。
- 時間帯判定や `1440` などの magic number を View / Presenter に置くこと。
- Auto Save へ接続すること。

完了条件:

- [ ] HUD に日数 / 時刻 / 時間帯が表示される
- [ ] 日付切替通知またはログが確認できる
- [ ] Auto Save 処理が追加されていない

### Task 8: 初回チュートリアルの設計

目的:

既存の主要画面と System Menu / SaveLoad を自然に案内する。

理想構成:

- `TutorialProgressService`
  - 表示済み step
  - 現在 step
  - SaveData へ含める値
- `GetTutorialGuidanceUseCase`
- `MarkTutorialStepSeenUseCase`
- `TutorialGuidePresenter`

対象:

- DungeonInfo
- GuildManagement
- Market
- System Menu
- Save / Load

禁止:

- Tutorial 状態を Presenter の field だけに置くこと。
- UI window の開閉状態を SaveData に含めること。
- Tutorial が gameplay state を変更すること。

完了条件:

- [ ] NewGame 後に guide が表示される
- [ ] Tutorial state が SaveData に含まれる
- [ ] Load / Continue 後に重複表示されすぎない

### Task 9: 統合テスト / Play 確認の設計

目的:

Launcher 起動から Title 操作、World 起動、Save / Load / Continue までを検証する。

EditMode test:

- Seed parser test
- NewGame 初期化 test
- SaveData snapshot 対象 / 対象外 test
- Save / Load roundtrip test
- Latest slot metadata test
- Slot selection ViewData test
- Tutorial state 保存 / 復元 test

Play / uLoop:

- Launcher scene から Play
- Title で NewGame を押す
- Seed 入力で World へ入る
- `[World] GameWorldState initialized` を確認
- GameHUD 設定ボタンで System Menu を開く
- Save で Slot 1 に保存
- Title へ戻る
- Continue で Slot 1 をロード
- LoadGame で Slot 1 / Empty slot 表示を確認
- 30 秒 Error なし

完了条件:

- [ ] compile が成功する
- [ ] EditMode test が成功する
- [ ] Launcher -> Title -> NewGame -> World が確認済み
- [ ] Save / Continue / LoadGame が確認済み
- [ ] Play 30 秒で Error がない

### 実装開始前ゲート

各 Task の実装前に、Codex は以下を確認し、問題があれば実装せずユーザーに確認する。

- docs にない仕様判断を必要としていないか。
- 新規 public / internal API が production 契約として必要か。
- 既存類似 DTO / Service / StateService / Repository / Event と責務が重複していないか。
- Product / GameSession / MainScene / ModuleScene のどこが正しい所有者か。
- UseCase が他 UseCase を呼ぶ構造になっていないか。
- View が `IGameWorldStateReader` や Domain 集約を直接広く読んでいないか。
- LifetimeScope が Prefab / View 実体 / Content catalog を持つ構造になっていないか。
- SaveData に Runtime live object や Unity object を含めていないか。
- 破壊的変更を避けるためだけに旧 API / alias / optional constructor を残していないか。
- テスト都合だけで Runtime surface を増やしていないか。

# Milestone 10 Roadmap - ゲームループの完成

## ゴール

「ゲームを始めて、進めて、終わる」という一連のプレイセッションを成立させる。
タイトル画面から New Game / Continue を選び、World に入り、日付と経済が進行し、勝利または敗北で結果画面へ到達できる状態にする。

Milestone 10 はコンテンツ量を増やすマイルストーンではなく、既存の Dungeon / Adventurer / Guild / Market / Facility の要素を、1本のゲーム体験として閉じることを目的とする。

## 対象範囲

- タイトル / トップメニュー
- New Game 開始フロー
- セーブ / ロード
- 勝利 / 敗北判定
- エンドスクリーン / リザルト
- 日付サイクルの可視化
- 初回チュートリアル
- 統合テストと完了レビュー

## Phase 一覧

### Phase 1: Milestone 10 仕様確定

勝利条件、敗北条件、初期状態、1プレイの想定長を確定する。

作業内容:
- 勝利条件を確定する
  - 例: 評判レベル到達、ギルド資産目標達成、施設総レベル到達
- 敗北条件を確定する
  - 例: ギルド資金が0未満、一定日数以内に目標未達、支払い不能
- 初期ギルド資金、初期施設、初期冒険者、初期日付を確定する
- New Game 時にユーザーが選べる項目を確定する
  - ギルド名
  - 難易度
  - 将来拡張用の設定項目
- セーブ方式を確定する
  - 1 スロット固定 / 複数スロット
  - JSON / 独自形式
  - 終了済みセッションを Continue 対象にするか
- 初回チュートリアル進行状態を SaveData に含めるか確定する
- Milestone 11 以降へ送る内容を確定する

完了条件:
- [ ] 勝利条件と敗北条件が docs に明記されている
- [ ] 初期状態が docs に明記されている
- [ ] セーブ方式と Continue 対象が docs に明記されている
- [ ] New Game 入力項目を M10 で実装するか固定値にするかが明記されている
- [ ] M10 対象外の項目が明確になっている

### Phase 2: タイトル / トップメニュー

タイトル画面を正式なゲーム開始入口として整える。

作業内容:
- New Game / Continue / Quit の UI を実装する
- セーブデータがない場合は Continue を無効化する
- タイトルロゴまたはタイトル表示を整える
- 既存の開始ボタン導線を正式なトップメニュー導線へ整理する
- Quit の扱いを Unity Editor / Build で分ける

完了条件:
- [ ] New Game から新規ゲーム開始フローへ進める
- [ ] Continue がセーブデータの有無に応じて有効 / 無効になる
- [ ] Quit が Editor / Build で破綻しない
- [ ] タイトル画面から World への遷移が Lighthouse の Scene 遷移ルールに沿っている

### Phase 3: New Game 開始フロー

新規プレイセッションの初期化を明示的なフローとして定義する。

作業内容:
- Phase 1 で New Game 入力を M10 対象にした場合のみ、New Game 選択後の初期設定画面を追加する
- Phase 1 で固定値開始にした場合は、初期設定画面を追加せず `NewGameInitialSettings` に固定初期値を渡す
- ギルド名、難易度、初期条件を採用する場合は `GameWorldState` 初期化へ反映する
- GameWorldState の初期化入力を整理する
- 初期施設、初期資金、初期冒険者、初期日付を生成する
- World 初期化完了後に HUD / ScreenStack が正常に使えることを確認する

完了条件:
- [ ] New Game から World 初期化まで一貫して進む
- [ ] 初期設定が GameWorldState に反映される
- [ ] 初期施設と初期資金が仕様どおり生成される
- [ ] `[World] GameWorldState initialized` が出力される

### Phase 4: セーブ / ロード基盤

ゲーム進行状態を保存し、タイトルから復元できるようにする。

作業内容:
- SaveData DTO を設計する
- 保存対象を確定する
  - GameClock
  - Guild
  - Facilities
  - Actors
  - Inventory
  - Dungeon
  - Economy / Transaction / Tutorial state
- 保存対象外を確定する
  - View instance
  - Presenter state
  - Addressable handle
  - Scene-owned object
- JSON または独自形式で保存 / 読込を実装する
- Phase 1 で確定した方針に従い、1 スロット固定または複数スロット管理を実装する
- 日付切替時の Auto Save を追加する
- Continue から最新セーブを復元する

完了条件:
- [ ] 手動または自動でセーブファイルが作成される
- [ ] タイトルの Continue から同じ状態を復元できる
- [ ] 保存対象に View / Unity instance が混入していない
- [ ] セーブが存在しない場合に Continue が無効化される

### Phase 5: 勝利 / 敗北判定

プレイセッションの終了条件をゲーム進行ループに組み込む。

作業内容:
- 勝利条件判定 UseCase / Service を追加する
- 敗北条件判定 UseCase / Service を追加する
- WorldSimulationOrchestrator の適切なタイミングで判定する
- 勝利 / 敗北イベントを発行する
- 勝敗確定後にゲーム進行を停止または結果画面へ遷移する

完了条件:
- [ ] 勝利条件到達時に勝利イベントが発行される
- [ ] 敗北条件到達時に敗北イベントが発行される
- [ ] 勝敗後に通常のゲーム進行が継続しない
- [ ] 判定処理が View 層に漏れていない

### Phase 6: エンドスクリーン / リザルト

勝利 / 敗北後に、プレイ結果を確認できる画面を追加する。

作業内容:
- 勝利画面を実装する
- 敗北画面を実装する
- リザルト項目を表示する
  - 経過日数
  - 最終資金
  - 評判
  - 施設レベル
  - 冒険者数
  - 売上 / 取引数
- Restart / Back to Title の導線を実装する
- 終了済みセッションを Continue できるかどうかの扱いを決める

完了条件:
- [ ] 勝利時に勝利画面へ遷移する
- [ ] 敗北時に敗北画面へ遷移する
- [ ] 結果画面からタイトルへ戻れる
- [ ] 結果表示が Application の summary / ViewData を通じて作られている

### Phase 7: 日付サイクルの可視化

ゲーム内時間の進行をプレイヤーが理解できるようにする。

作業内容:
- 既存の HUD 日数 / 時刻表示を正式な日付サイクル表示として整理する
- 時間帯を HUD に追加表示する
- 朝 / 昼 / 夕 / 夜の時間帯変化を表示する
- 日付切替時の通知またはログを表示する
- 施設の営業時間に応じた表示切替を検討する
- Auto Save と日付切替のタイミングを接続する

完了条件:
- [ ] HUD で現在日数、時刻、時間帯が確認できる
- [ ] 日付切替が視覚的またはログで確認できる
- [ ] Auto Save が日付切替と矛盾しない

### Phase 8: 初回チュートリアル

最初の数日間で、既存の主要画面と操作を自然に案内する。

作業内容:
- チュートリアル対象を決める
  - DungeonInfo
  - GuildManagement
  - Market
  - Facility upgrade
- チュートリアル進行状態を管理する
- 表示済みガイドをセーブ対象に含めるか決める
- UI 操作を邪魔しない軽量な表示を実装する

完了条件:
- [ ] New Game 後に最初の案内が表示される
- [ ] 主要画面へ誘導できる
- [ ] 表示済みチュートリアルが繰り返し過ぎない
- [ ] セーブ / ロード後のチュートリアル状態が破綻しない

### Phase 9: 統合テスト / Play 確認

New Game から勝敗までの流れを機械的に検証できるようにする。

作業内容:
- New Game から World 開始までのテストを追加する
- Save / Load の EditMode test を追加する
- 勝利条件到達テストを追加する
- 敗北条件到達テストを追加する
- Continue 復元テストを追加する
- Play 30秒以上の確認を行う

完了条件:
- [ ] `uloop.cmd compile --project-path Client` が成功する
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する
- [ ] Play 30秒確認で Error ログがない
- [ ] `[World] GameWorldState initialized` が出力される
- [ ] New Game / Save / Continue / Win / Lose の主要導線が確認済み

### Phase 10: Milestone 10 完了レビュー

ゲームループ完成として、設計・実装・動作確認を横断レビューする。

作業内容:
- セルフレビューを実施する
- Lighthouse guideline 適合性を確認する
- Application boundary guideline 適合性を確認する
- Save / Load の責務境界を確認する
- 勝敗判定と結果画面の依存方向を確認する
- Milestone 11 へ送る残課題を整理する

完了条件:
- [ ] `docs/self-review/` に Milestone 10 完了レビューが作成されている
- [ ] 未対応項目と M11 送り項目が区別されている
- [ ] ハードゲート違反がない
- [ ] コンパイル、EditMode test、Play 確認が完了している

## Milestone 11 へ移動する項目

Milestone 10 では、ゲームセッションを閉じるために必要な最小範囲に集中する。
以下は Milestone 11 以降で扱う。

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

- [ ] タイトル画面から New Game を開始できる
- [ ] タイトル画面から Continue で保存済みゲームを再開できる
- [ ] ゲーム内日付と経済が進行する
- [ ] 勝利条件でゲームが終了する
- [ ] 敗北条件でゲームが終了する
- [ ] 結果画面でプレイ結果を確認できる
- [ ] セーブ / ロード後も Guild / Facility / Actor / Inventory / Dungeon / Clock が復元される
- [ ] 初回チュートリアルで主要画面へ誘導できる
- [ ] `uloop.cmd compile --project-path Client` が成功する
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する
- [ ] Play 30秒確認で Error ログがない

## 現行コード確認レビュー

2026-05-27 時点の `Client/Assets/DungeonInn/Runtime/Scripts` を確認した結果、Milestone 10 は「既存の Title -> World 直行導線と GameSession scope を、正式な New Game / Continue / Save / End flow へ置き換える」作業として扱う。

### Phase 1: 仕様確定

現行コード:
- `WorldGameSettingsSO` に初期 dungeon seed、random seed、初期資金、初期施設値、初期ギルド inventory が既にある。
- 勝利条件、敗北条件、終了済み session の扱い、セーブスロット方針、チュートリアル状態の保存方針は未実装であり、仕様も未確定。

修正方針:
- 初期状態は `WorldGameSettingsSO` の既存値を正典候補として扱う。
- 勝敗条件と Save / Continue 方針は Phase 1 のユーザー判断事項として明記し、実装側で仮決めしない。
- New Game 入力画面は必須前提にせず、Phase 1 で「固定値開始」か「入力あり開始」かを決めてから実装する。

### Phase 2: タイトル / トップメニュー

現行コード:
- `TitleView` は `startGameButton` 1 つだけを持つ。
- `TitlePresenter` は `StartNewGameAsync()` で `GameSessionLifecycle.BeginSession()` を呼び、直接 `WorldScene.WorldTransitionData` へ遷移している。
- Continue / Quit / Save metadata 判定は存在しない。

修正方針:
- 単一 StartGame 導線は New Game / Continue / Quit の正式メニューへ破壊的に置き換える。
- `TitlePresenter` は SaveData IO や GameWorldState 初期化を持たず、開始 request と scene transition の調整に留める。
- Continue 可否は Save metadata query から取得する。

### Phase 3: New Game 開始フロー

現行コード:
- `GameSessionLifecycle` と `GameSessionLifetimeScope` は既に存在する。
- `WorldSimulationOrchestrator.InitializeAsync()` は常に `WorldGameSettingsRepository.GetInitialWorldSettings()` から新規 World を初期化する。
- Continue 復元と New Game 初期化の分岐はない。

修正方針:
- `WorldSimulationOrchestrator.InitializeAsync()` に New Game / Continue の区別がない状態を正式フローへ直す。
- New Game の初期値は `WorldGameSettingsSO` 由来の settings を利用しつつ、将来の難易度 / seed / guild name 入力に拡張できる request として扱う。
- Continue では新規初期化を走らせず、復元済み state を検証する。

### Phase 4: セーブ / ロード基盤

現行コード:
- Runtime に GameSaveData / IGameSaveRepository / SaveGameUseCase / LoadGameUseCase 相当は存在しない。
- `ProductTextTableLoader` などの infrastructure loader はあるが、ゲーム状態保存用 repository はない。
- `GameClock` は現在 tick / day / elapsed time を持つが、復元用 setter / snapshot contract はない。

修正方針:
- Save / Load は新規 Application + Infrastructure 機能として追加する。
- `GameClock` や `GameWorldState` に安易な public setter を追加するのではなく、復元責務を明確にした usecase / state restoration 経路を設計する。
- SaveData には View / Presenter / Addressable handle / scene-owned object を含めない。

### Phase 5: 勝利 / 敗北判定

現行コード:
- Actor の `ActorDefeated` は存在するが、ゲームセッション全体の勝利 / 敗北イベントは存在しない。
- `WorldSimulationOrchestrator.AdvanceFrameAsync()` は session end state を見ずにゲーム進行を続ける。
- 勝敗判定 UseCase / StateService は存在しない。

修正方針:
- Actor defeat と GameSession defeat を混同しない。
- `GameSessionStatusService` のような session-level state を追加し、Running でない場合は World simulation を進めない。
- 勝敗 event は状態変更後に 1 回だけ発行する。

### Phase 6: エンドスクリーン / リザルト

現行コード:
- GameHUD / ScreenStack window 基盤は存在する。
- `DungeonInfoWindow`、`GuildManagementWindow`、`MarketWindow` は GameHUD-owned ScreenStack として実装済み。
- Result / End Screen 用の window、data、summary usecase は存在しない。

修正方針:
- Result は既存 GameHUD / ScreenStack 配下の window として追加する。
- Result window は GameWorldState を直接集計せず、Application summary / ViewData を表示する。
- Back to Title / Restart は `IProductSceneManager` と `GameSessionLifecycle.EndSession()` の順序を明確にする。

### Phase 7: 日付サイクルの可視化

現行コード:
- `GameClock` は `CurrentDay`、`CurrentTickOfDay`、pause、time scale を持つ。
- `WorldHudPresenter` は `WorldHudScreenService.GetTimeState()` を使い、`Day {n} HH:MM` と Gold を表示している。
- 朝 / 昼 / 夕 / 夜の時間帯表示、日付切替通知、Auto Save 連携は存在しない。

修正方針:
- 現在日数 / 時刻表示は既存 HUD 実装を活かす。
- 追加対象は時間帯表示、日付切替通知、Auto Save 連携に絞る。
- 時間帯判定は View の magic number ではなく Application / clock settings 側へ寄せる。

### Phase 8: 初回チュートリアル

現行コード:
- TutorialProgressService / TutorialGuidePresenter / Tutorial SaveData は存在しない。
- DungeonInfo / GuildManagement / Market の window open service は存在する。

修正方針:
- Tutorial は既存 GameHUD window open 導線を案内する軽量 guide として設計する。
- チュートリアル進行状態は Presenter の一時 field ではなく Application state に置く。
- SaveData に含めるかどうかは Phase 1 の決定に従う。

### Phase 9: 統合テスト / Play 確認

現行コード:
- 既存 EditMode tests は Milestone 9 までの GameLoop / UI / Market / Facility を中心に存在する。
- New Game / Save / Continue / Win / Lose / Result を通す統合テストはまだない。

修正方針:
- M10 の完了判定は compile と既存 tests だけでは不可とする。
- New Game / Save / Continue / Win / Lose / Result / Back to Title を個別 test と Play 確認に含める。
- Play 確認では `Title -> World` の通常導線、`[World] GameWorldState initialized`、Error 0 を必須にする。

### Phase 10: 完了レビュー

現行コード:
- M10 は未実装のため完了レビュー対象ではない。
- `docs/self-review/` には Milestone 9 までのレビューがある。

修正方針:
- M10 完了時は Save / Load の保存対象、勝敗判定の責務、Result 表示の依存方向、GameSession scope の破棄順序を重点レビューする。
- 完了レビューでは「現行コード確認レビュー」の各 Phase 修正方針が実装で満たされているか照合する。

## 実装前の理想設計方針

### レビュー判断

Milestone 10 は、既存の World 実行基盤に「タイトル開始」「New Game 初期化」「Continue 復元」「勝敗による終了」「結果表示」を追加するマイルストーンである。
実装の中心は View を増やすことではなく、1 回のゲームセッションの寿命と状態遷移を Application / GameSession 境界で明確にすることである。

現時点では、勝利条件、敗北条件、New Game の入力項目、セーブスロット数、終了済みセッションを Continue 可能にするかが未確定である。
これらはゲーム仕様そのものなので、実装前にユーザー判断が必要である。

### 本来の責務境界

- Title MainScene は、New Game / Continue / Quit の入力受付と、開始意思の確定だけを持つ。
- GameSessionLifetimeScope は、1 プレイセッションで共有される Application state、Domain state、Repository、UseCase、Orchestrator の所有者になる。
- World MainScene は、開始済みセッションの 3D / World 表示、カメラ、マップ、Actor 表示を持つ。
- GameHUD ModuleScene は、World に付随する HUD、Window、ScreenStack 用 View / Presenter / Pool を持つ。
- ScreenStack ModuleScene は、ScreenStack の生成・破棄と入力レイヤーを持つ。
- Save / Load は View から直接 Domain を触らず、Application の snapshot 生成 / 復元 UseCase と Infrastructure の保存 Repository を経由する。
- 勝利 / 敗北判定は View や Window ではなく、WorldSimulationOrchestrator から呼ばれる Application の終了条件判定 UseCase / Service が持つ。

### 各クラス・オブジェクトが持つもの / 持たないもの

`TitleView` が持つもの:
- New Game / Continue / Quit の LHButton 参照
- ボタンの interactable 表示
- プレイヤー入力を Presenter に通知する口

`TitleView` が持たないもの:
- GameWorldState の生成
- SaveData の読込
- 勝敗条件や初期値
- Scene 遷移判断

`TitlePresenter` が持つもの:
- TitleView からの入力を受け、開始フロー UseCase / Service へ渡す責務
- New Game / Continue / Quit の UI 状態更新
- IProductSceneManager による Lighthouse Scene 遷移の呼び出し

`TitlePresenter` が持たないもの:
- GameWorldState の直接変更
- JSON / ファイル IO
- 保存データ DTO の手作業組み立て
- World の View / HUD / ScreenStack 参照

`GameSessionLifecycle` が持つもの:
- GameSessionLifetimeScope の生成と破棄
- セッション用 Scope が存在するかの管理
- World がセッション Scene であるという寿命判断

`GameSessionLifecycle` が持たないもの:
- New Game の初期条件
- Load 済み SaveData の中身
- 勝敗判定
- 結果画面の ViewData

`GameSessionLifetimeScope` が持つもの:
- GameWorldState、GameClock、GameRandom
- GameSession 内で共有する Application Service / UseCase / Orchestrator
- Save / Load、勝敗判定、チュートリアル状態管理など、1 セッション単位で寿命を持つサービス

`GameSessionLifetimeScope` が持たないもの:
- TitleView / HUD View / Popup View など Scene-owned component
- UI View Prefab や Popup Prefab の直接 SerializedField catalog
- Product 全体で共有される root service

`GameWorldState` が持つもの:
- 復元可能な現在状態の正典
- Guild / Facility / Actor / Inventory / Dungeon / Clock などゲーム進行の状態

`GameWorldState` が持たないもの:
- UnityEngine.Object
- View / Presenter / Canvas / Addressable handle
- ファイルパスや保存形式
- UI 表示用に整形済みの文字列

`SaveData DTO` が持つもの:
- 保存対象の値だけを含む versioned snapshot
- GameClock、Guild、Facilities、Actors、Inventory、Dungeon、Economy / Transaction summary、Tutorial state

`SaveData DTO` が持たないもの:
- Domain Entity の live instance 参照
- View instance / Presenter state
- Addressable handle / Material / Sprite / GameObject
- Runtime の一時 cache、dirty flag、選択中 UI 状態

`WorldSimulationOrchestrator` が持つもの:
- 1 フレーム / Schedule tick の Application 実行順序
- 日付切替、経済処理、Actor 処理の後に終了条件判定を呼ぶ接続点
- 勝敗確定後に以降のゲーム進行を止めるための Application 状態確認

`WorldSimulationOrchestrator` が持たないもの:
- 結果画面の生成
- UI 遷移演出
- 勝利 / 敗北の具体的な表示文言
- Save file の直接 IO

`Result Screen / End Screen` が持つもの:
- ResultViewData の表示
- Restart / Back to Title の入力

`Result Screen / End Screen` が持たないもの:
- 勝敗判定そのもの
- GameWorldState の直接集計
- セッション破棄の具体処理

### 依存方向

依存方向は以下に固定する。

```text
View
  -> Application Query / UseCase / Service interface
  -> Core scene service interface

Application
  -> Domain
  -> Application interface

Infrastructure
  -> Application interface implementation
  -> file / JSON / Unity persistentDataPath

Domain
  -> no Unity / no View / no Infrastructure
```

Title / HUD / Result などの View は、Application の広い state を直接読まず、用途ごとの Query / ScreenService / ViewDataFactory を通す。
UseCase から別 UseCase を直接呼ばず、複数処理の順序制御は Orchestrator が持つ。
イベントは状態変更完了後に発行し、イベント購読 callback 内で Domain state を直接変更しない。

### 所有する Scene / Module / LifetimeScope

既存実装に合わせ、Scene / LifetimeScope の所有者は以下にする。

- ProductLifetimeScope: Product 全体の基盤、IProductSceneManager、Reboot、TextTable、ScreenStack proxy など。
- Title MainScene / TitleLifetimeScope: タイトル画面 UI と TitlePresenter。
- GameSessionLifetimeScope: 1 プレイセッションの Application / Domain state と Save / Load / Goal / Tutorial service。
- World MainScene / WorldLifetimeScope: World 表示、カメラ、マップ、Actor / Projectile / AreaEffect presenter。
- GameHUD ModuleScene / GameHUDLifetimeScope: HUD、Minimap、InnStatus、ActorStatus、GameHUD window open service。
- ScreenStack ModuleScene / ScreenStackLifetimeScope: ScreenStack entity factory、ScreenStack instance factory。

Title から World へ遷移する際は、TitlePresenter が GameSessionLifecycle にセッション開始を要求し、IProductSceneManager で World へ遷移する。
Continue では、セッション Scope を開始した後に Load request を GameSession 側へ渡し、World 初期化時に復元済み state を使う。

### 配置場所

Runtime の配置:
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/`: 勝敗状態、日付進行、ゲーム終了状態の Application model / UseCase。
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/SaveLoad/`: SaveData 変換、snapshot 生成、復元 UseCase、Repository interface。
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Tutorial/`: チュートリアル進行状態と表示判定 service。
- `Client/Assets/DungeonInn/Runtime/Scripts/GameSession/`: GameSession 開始 request、New Game / Continue の session-level coordinator、GameSessionLifetimeScope 登録。
- `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/SaveLoad/`: JSON / file repository 実装。
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/Title/`: TitleView / TitlePresenter / TitleLifetimeScope の拡張。
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/`: 日付 HUD、軽量 tutorial 表示、result window を HUD 内で扱う場合の View / Presenter。
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ScreenStack/`: Result Screen を ScreenStack として実装する場合の Data / ViewData / Window。

Editor の配置:
- 自動 asset 配線や一時生成が必要な場合のみ `Client/Assets/DungeonInn/Editor/OneShot/` に置く。
- Runtime の通常経路から OneShot を呼ばない。

Prefab / Addressable の配置:
- 既存の GameHUD window と同様に、HUD / ScreenStack View は Addressable Factory / ScreenStack 経由で生成する。
- LifetimeScope に UI View Prefab を SerializedField catalog として増やさない。
- Result / Tutorial / Title 用の prefab が必要な場合は、View 所有の Addressable 定義または既存 Scene 上の scene-owned component として扱う。

### 既存実装に合わせる点

- Title は既存の `TitleScene` / `TitleView` / `TitlePresenter` / `TitleLifetimeScope` を拡張し、新しいタイトル導線を別系統で増やさない。
- Scene 遷移は `IProductSceneManager.TransitionScene` を使い、`SceneManager.LoadScene` を通常遷移に使わない。
- セッション寿命は既存の `GameSessionLifecycle.BeginSession()` / `EndSession()` と `GameSessionLifetimeScope` に寄せる。
- World 初期化は既存の `WorldGameLoopEntryPoint.InitializeAsync()` と `WorldSimulationOrchestrator.InitializeAsync()` の流れに接続する。
- HUD / Window は既存の GameHUD ModuleScene と ScreenStack の構成に合わせる。
- UI の Button は `LHButton` を使う。
- Addressable は `IAssetScope` / `IAssetManager` 経由にする。
- ViewData は既存の `GameHudScreenStackViewDataFactory` のような factory / screen service 経由で作る。

### 破壊的に直す点

Milestone 10 では、既存の暫定実装を正式導線へ置き換える必要がある。

- Title の単一 StartGame ボタン導線は、New Game / Continue / Quit のトップメニューへ置き換える。
- `TitlePresenter.StartNewGameAsync()` が直接 `BeginSession()` と World 遷移だけを行う状態は、New Game / Continue の開始 request を扱える flow に分割する。
- `WorldSimulationOrchestrator.InitializeAsync()` が常に初期ワールド生成だけを行う形は、New Game 初期化と Continue 復元の入力を区別できる形に直す。
- Save / Load のために Domain live object や View state をそのまま serialize する実装は作らない。
- 勝敗判定を Result View や HUD Presenter に置く実装は不可とする。
- 勝敗確定後も `AdvanceFrameAsync()` が通常通り進む状態は不可とし、Application 側で終了済み session state を持って止める。

これらは既存 API の追加 / 変更を伴う production 契約変更であるため、実装時は task の作業ログに「追加した型」「既存類似概念」「意味差分」「代替しなかった理由」「統合・削除条件」を記録する。

### 実装前にユーザー判断が必要な項目

- 勝利条件: 評判、資金、施設総レベル、日数など、どれを正式条件にするか。
- 敗北条件: 資金 0 未満、支払い不能、期限切れなど、どれを正式条件にするか。
- New Game 入力: ギルド名 / 難易度を M10 で実装するか、固定初期値で開始するか。
- セーブスロット: 1 スロット固定か、複数スロットか。
- Auto Save のタイミング: 日付切替時のみか、重要イベント後も保存するか。
- 終了済みセッション: Continue 可能にするか、結果確認のみ可能にするか、Continue 無効にするか。
- チュートリアル状態: SaveData に含めるか、New Game ごとの一時状態にするか。

### 全体原則

- Product / bootstrap は最初の入口 MainScene へ遷移するだけに留め、実ゲームセッションの開始判断を持たない。
- Title / Menu は New Game / Continue / Quit の意思決定を持つが、GameWorldState の中身を直接生成・変更しない。
- GameSessionLifetimeScope は 1 プレイセッションで共有する Application / Domain state の正しい所有者とする。
- World MainScene は開始済みセッションを表示・操作する場であり、セッション生成や SaveData の IO を持たない。
- GameHUD / ScreenStack ModuleScene は UI 表現を所有する。勝敗判定、Save / Load、New Game 初期化を UI 側へ逃がさない。
- SaveData は復元可能な snapshot であり、Domain live object、UnityEngine.Object、Addressable handle、View state を含めない。
- 勝敗判定は Application の終了条件判定として扱い、Result Screen は判定結果を表示するだけにする。
- UseCase はステートレス、長期状態は Service / StateService に分ける。
- 複数 UseCase の順序制御は Orchestrator が持つ。UseCase から UseCase を直接呼ばない。
- 新規概念を追加する場合は、既存類似概念、意味差分、代替不可理由、統合・削除条件を作業ログに記録する。
- 破壊的変更を避けるためだけの旧 API、互換 alias、optional constructor、テスト都合の Runtime surface は残さない。

### 望ましい大枠

```text
Core / Product
  EntrySceneTransitionService
  GameSessionLifecycle
  IProductSceneManager

GameSession
  GameSessionLifetimeScope
  GameSessionStartRequest
  NewGameSessionRequest
  ContinueGameSessionRequest
  GameSessionStartCoordinator

Application
  NewGame
    InitializeNewGameSessionUseCase
    NewGameInitialSettings
    NewGameInitialSettingsRepository
  SaveLoad
    GameSaveData
    GameSaveMetadata
    IGameSaveRepository
    CreateGameSaveSnapshotUseCase
    RestoreGameSaveSnapshotUseCase
    SaveGameUseCase
    LoadGameUseCase
    GetLatestSaveMetadataUseCase
  SessionEnd
    GameSessionStatusService
    EvaluateGameSessionEndUseCase
    GameSessionEndResult
    GameSessionEnded
    GetGameResultSummaryUseCase
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
  MainScene/World
    WorldGameLoopEntryPoint
    WorldLifetimeScope
  ModuleScene/GameHUD
    DateCycleHudPresenter
    TutorialGuidePresenter
    ResultWindow open service
  ModuleScene/GameHUD/ScreenStack
    GameResultWindow
    GameResultWindowData
    GameResultWindowViewData
```

`GameSessionStartCoordinator` は Title からの開始要求を受けて、セッション Scope の開始、New Game / Continue の準備、World への遷移をつなぐ調整役にする。
ただし、GameWorldState の初期化や復元そのものは Application UseCase 側が持つ。

`GameSessionStatusService` は現在セッションが Running / Won / Lost / Ended のどれかを持つ StateService とする。
`WorldSimulationOrchestrator` は毎 frame / schedule tick の終端でこの状態を確認し、終了済みなら以降の時間進行・経済進行・AI 進行を止める。

### Task 1: 仕様確定 / GameSession 契約の設計

Milestone 10 の最初の作業は、勝敗条件や初期状態をコードへ埋めることではなく、以後の実装が参照する契約を確定することである。

確定する仕様:

- 勝利条件
  - 評判到達、資金到達、施設総レベル到達、期限内達成などのうち正式条件を選ぶ
  - 複数条件にする場合は AND / OR を明記する
- 敗北条件
  - 資金不足、支払い不能、期限切れ、目標未達などのうち正式条件を選ぶ
  - 「0 未満」「0 以下」「支払い実行時に不足」のどれで負けるかを明記する
- 初期状態
  - 初期日付
  - 初期資金
  - 初期施設レベル
  - 初期冒険者
  - 初期 dungeon seed / random seed の扱い
- New Game 入力
  - M10 でギルド名 / 難易度を実装するか、固定値にするか
- Save / Continue
  - 1 スロット固定か、複数スロットか
  - 終了済み save を Continue 対象にするか
- Tutorial
  - 進行状態を SaveData に含めるか

理想構成:

- `GameSessionStartRequest`
  - New Game と Continue の共通開始情報
- `NewGameSessionRequest`
  - ギルド名、難易度、初期 seed など
- `ContinueGameSessionRequest`
  - slot id または latest save 指定
- `GameSessionStartMode`
  - `NewGame`
  - `Continue`

禁止:

- 未確定仕様を magic number として Runtime に書くこと。
- 勝敗条件を UI 文言や Presenter の if 文として実装すること。
- 難易度や初期条件を後から変えられない static constant に直書きすること。

完了条件:

- [ ] 勝利条件、敗北条件、初期状態、Save / Continue 方針が docs に明記されている
- [ ] New Game / Continue の開始 request が区別できる
- [ ] GameSession 開始契約が Title / World / SaveLoad のいずれか一層に偏っていない

### Task 2: Title / Top Menu の設計

Title はゲーム開始の正式入口であり、Product 起動処理の延長ではない。
現在の単一 StartGame 導線は New Game / Continue / Quit のトップメニューへ置き換える。

View 側:

- `TitleView`
  - `LHButton` の New Game / Continue / Quit 参照
  - Continue の interactable 切替
  - transition 中の入力抑制
  - 保存データ有無の表示

Presenter 側:

- `TitlePresenter`
  - 起動時に `GetLatestSaveMetadataUseCase` または Title 用 service で Continue 可否を取得する
  - New Game 選択時は New Game flow を開始する
  - Continue 選択時は Continue flow を開始する
  - Quit は Editor / Build で挙動を分ける
  - Scene 遷移は `IProductSceneManager` を使う

Application / Core 側:

- `GameSessionStartCoordinator`
  - `GameSessionLifecycle.BeginSession()` を呼ぶ
  - New Game / Continue の準備処理を GameSession scope 側へ委譲する
  - 成功時のみ World へ遷移する
  - 失敗時は `GameSessionLifecycle.EndSession()` で Scope を破棄する

禁止:

- `TitleView` が SaveData を読むこと。
- `TitlePresenter` が JSON / file IO を直接呼ぶこと。
- `TitlePresenter` が GameWorldState に初期値を詰めること。
- Product bootstrap から直接 World へ遷移すること。

完了条件:

- [ ] Title に New Game / Continue / Quit が存在する
- [ ] セーブがない場合 Continue が無効になる
- [ ] New Game / Continue の処理中は重複入力できない
- [ ] Scene 遷移に `IProductSceneManager` を使っている
- [ ] `TitlePresenter` が `GameWorldState` / SaveData DTO を直接操作していない

### Task 3: New Game 開始フローの設計

New Game は「新しい GameSession scope を作る」「初期条件を決める」「World 初期化で新規 state を作る」を分ける。
World 表示の初期化処理に New Game 用の設定画面やギルド名入力の責務を混ぜない。

Application 側:

- `InitializeNewGameSessionUseCase`
  - `NewGameInitialSettings` を受け取る
  - `GameRandom` を初期化する
  - `GameClock` を初期日付へ設定する
  - `InitializeGameWorldOrchestrator` に初期 dungeon / guild / facility 情報を渡す
  - 初期チュートリアル状態を作る
- `NewGameInitialSettingsRepository`
  - 難易度や初期値を設定データから解決する
  - Addressable が必要な場合は Repository / Settings loader に閉じる

GameSession 側:

- `GameSessionStartCoordinator`
  - New Game request を保存し、World 初期化時に Application usecase へ渡せるようにする
  - または BeginSession 直後に GameSession scope 内の initializer を実行する

World 側:

- `WorldSimulationOrchestrator.InitializeAsync()`
  - New Game mode なら新規初期化
  - Continue mode なら復元済み state の検証
  - どちらの場合も `[World] GameWorldState initialized` のログを出す

禁止:

- `WorldGameLoopEntryPoint` が New Game 設定を直接持つこと。
- 初期状態を `WorldSimulationOrchestrator` 内の magic number にすること。
- New Game 初期化と Continue 復元を同じ「とりあえず初期化」処理で潰すこと。

完了条件:

- [ ] New Game request から初期 GameWorldState が作られる
- [ ] 初期値は docs / settings と一致する
- [ ] Continue 復元時に New Game 初期化が二重に走らない
- [ ] 初期化処理が View 層にない

### Task 4: Save / Load 基盤の設計

Save / Load は「現在状態の snapshot 化」「保存媒体への IO」「復元」の 3 つを分ける。
Domain live object をそのまま serialize しない。

Application DTO:

- `GameSaveData`
  - `Version`
  - `SavedAtUtc`
  - `SessionStatus`
  - `Clock`
  - `Guild`
  - `Facilities`
  - `Actors`
  - `Inventory`
  - `Dungeon`
  - `Economy`
  - `Tutorial`
- `GameSaveMetadata`
  - slot id
  - saved at
  - display name
  - session status
  - elapsed day
- `GameSaveSlotId`
  - M10 で 1 スロット固定にする場合でも、将来複数化できる stable id として持つ

Application UseCase:

- `CreateGameSaveSnapshotUseCase`
  - `IGameWorldStateReader`、`IGameClock`、各 StateService から保存値を集める
  - View / Presenter / Addressable / scene-owned object を含めない
- `RestoreGameSaveSnapshotUseCase`
  - `GameSaveData` を検証する
  - GameWorldState、GameClock、各 StateService を復元する
  - 復元できない version は失敗 result を返す
- `SaveGameUseCase`
  - snapshot 作成後、`IGameSaveRepository` へ渡す
- `LoadGameUseCase`
  - repository から読み、restore usecase へ渡す
- `GetLatestSaveMetadataUseCase`
  - Title の Continue 可否判定に使う

Infrastructure:

- `IGameSaveRepository`
  - Application 側 interface
- `JsonGameSaveRepository`
  - Infrastructure 実装
  - `Application.persistentDataPath` 等の Unity API はここに閉じる
- `GameSaveFilePathProvider`
  - 保存パス生成
  - slot id とファイル名の対応を持つ

保存対象外:

- `MonoBehaviour`
- `GameObject`
- `Sprite`
- `Material`
- `Addressable handle`
- `Presenter` の選択状態
- `ScreenStack` の開閉状態
- `WorldActorPresenter` の cache

禁止:

- View / Presenter が `IGameSaveRepository` を直接呼ぶこと。
- Domain Entity を JSON serializer へそのまま渡すこと。
- SaveData DTO に UnityEngine.Object 派生型を含めること。
- テスト都合で public setter だらけの Runtime DTO を増やすこと。

完了条件:

- [ ] SaveData DTO が versioned snapshot になっている
- [ ] Repository interface は Application 側、file 実装は Infrastructure 側にある
- [ ] SaveData に UnityEngine.Object が含まれていない
- [ ] Save / Load roundtrip の EditMode test がある
- [ ] Title の Continue 可否が metadata query から判断される

### Task 5: Auto Save / 日付切替接続の設計

Auto Save は frame loop の中で毎フレーム条件判定するのではなく、GameClock の日付切替結果を起点にする。
日付切替時の経済処理と Save の順序を固定し、保存される状態が中途半端にならないようにする。

Application 側:

- `GameLoopUseCase`
  - 既存の completed day 結果を返す責務を維持する
- `WorldSimulationOrchestrator`
  - completed day ごとに日次レポート / 経済処理を完了する
  - その後に Auto Save usecase を呼ぶ
- `AutoSaveOnDayChangedUseCase`
  - Auto Save 有効判定
  - slot id 解決
  - `SaveGameUseCase` への委譲

実行順序:

```text
GameLoopUseCase.ExecuteAsync
  -> completed days を取得
  -> 日次経済処理
  -> 日次レポート発行
  -> 勝敗判定
  -> Running の場合 Auto Save
```

勝敗確定した日の保存方針はユーザー判断に従う。
終了済みセッションを保存する場合は `SessionStatus = Won / Lost` と Result summary に必要な値を保存する。

禁止:

- `Update()` で毎フレーム SaveData を作ること。
- 日次処理の途中で Auto Save し、中途半端な経済状態を保存すること。
- Save 失敗時にゲーム進行を silent failure で続けること。最低限 warning / result を残す。

完了条件:

- [ ] 日付切替後に Auto Save が 1 回だけ走る
- [ ] 日次経済処理後の状態が保存される
- [ ] Auto Save 失敗時のログ / result がある
- [ ] Auto Save が勝敗確定後の扱いと矛盾しない

### Task 6: 勝利 / 敗北判定の設計

勝敗判定はゲーム進行ループの Application 処理として扱う。
UI は勝敗状態を表示するだけで、条件判定を持たない。

Application:

- `EvaluateGameSessionEndUseCase`
  - `IGameWorldStateReader`
  - `IGameClock`
  - `GuildProgressService`
  - 経済状態 summary
  - 確定済み仕様に基づき `GameSessionEndResult` を返す
- `GameSessionStatusService`
  - `Running`
  - `Won`
  - `Lost`
  - `Ended`
  - 終了理由
  - 終了日
- `GameSessionEnded`
  - Domain / Application event
  - result type、reason、day、summary id など事実だけを持つ

`WorldSimulationOrchestrator` の接続:

- frame 開始時に `GameSessionStatusService` が Running でなければ時間進行しない
- schedule tick / 日次処理後に `EvaluateGameSessionEndUseCase` を呼ぶ
- 勝敗が確定したら `GameSessionStatusService` を更新し、状態変更後に event を発行する
- 終了確定後は以降の AI / combat / economy を進めない

判定タイミング:

- 資金不足のような即時条件は、該当 UseCase の状態変更後または schedule tick 終端で判定する
- 期限切れ / 目標達成は日付切替後に判定する
- 同時に勝利 / 敗北を満たす場合の優先順位は仕様で決める

禁止:

- Result Screen が勝敗条件を再計算すること。
- HUD Presenter が資金や日数を見て勝敗を決めること。
- イベント購読 callback 内で GameWorldState を変更して勝敗処理を進めること。
- 勝敗確定後も `AdvanceFrameAsync()` が通常通り処理を続けること。

完了条件:

- [ ] 勝利 / 敗北判定が Application UseCase にある
- [ ] Session status を持つ StateService がある
- [ ] 勝敗 event は状態変更後に 1 回だけ発行される
- [ ] 勝敗確定後に時間 / 経済 / AI が進まない
- [ ] 勝敗同時成立時の優先順位が docs にある

### Task 7: Result / End Screen の設計

Result Screen は勝敗判定の所有者ではなく、確定済み結果と summary を表示する UI である。
既存の GameHUD / ScreenStack 構成に合わせ、World から結果を表示する場合は ScreenStack window として扱う。

Application:

- `GetGameResultSummaryUseCase`
  - `GameSessionStatusService`
  - `IGameWorldStateReader`
  - 経済 / transaction / guild progress service
  - 結果表示に必要な summary を作る
- `GameResultSummary`
  - result type
  - 終了理由
  - 経過日数
  - 最終資金
  - 評判
  - 施設レベル
  - 冒険者数
  - 売上 / 取引数

View:

- `GameResultWindowData`
- `GameResultWindowViewData`
- `GameResultWindow`
- `GameResultScreenService` または GameHUD 側 open service

導線:

- 勝敗 event を GameHUD / result presenter が購読し、Result window を開く
- Back to Title は `IProductSceneManager` で Title へ戻り、必要に応じて `GameSessionLifecycle.EndSession()` を呼ぶ
- Restart は古い session を破棄して New Game flow を再実行する

禁止:

- Result window が `GameWorldState` を直接集計すること。
- Result window が `SceneManager.LoadScene` を直接呼ぶこと。
- Result UI を World MainScene の Canvas として直置きすること。
- 終了済み session の破棄と Scene 遷移順序が曖昧なままにすること。

完了条件:

- [ ] 勝利 / 敗北で Result window が開く
- [ ] Result 表示値は Application summary から作られる
- [ ] Back to Title で session scope が破棄される
- [ ] Restart で古い session state が混ざらない
- [ ] Result UI が GameHUD / ScreenStack 所有になっている

### Task 8: 日付サイクル HUD の設計

日付サイクル HUD は GameClock の表示であり、時間進行そのものを所有しない。
表示専用 DTO / ViewData を通し、HUD Presenter が GameClock や WorldState を広く読む状態にしない。

Application:

- `GetGameClockViewDataUseCase` または `GetGameTimeStateUseCase` の拡張
  - 現在日
  - 時刻
  - 時間帯
  - time scale
  - pause 状態
  - 次の日付切替までの表示用進捗

View:

- `DateCycleHudView`
- `DateCycleHudPresenter`
- `DateCycleHudViewData`

更新方針:

- HUD は毎フレーム表示更新してよいが、Domain / Master の重い集計はしない
- 日付切替通知は event / history / log を使う
- Auto Save 表示が必要な場合は Save result を通知として受ける

禁止:

- HUD が `GameClock.Advance` 相当の時間進行を呼ぶこと。
- HUD が日付切替を検出して経済処理や Auto Save を呼ぶこと。
- 時間帯判定の magic number を View に持つこと。

完了条件:

- [ ] HUD に日数 / 時刻 / 時間帯が表示される
- [ ] 時間帯判定は Application / clock settings 側にある
- [ ] 日付切替通知が表示またはログで確認できる
- [ ] HUD からゲーム進行 UseCase を直接呼んでいない

### Task 9: 初回チュートリアルの設計

チュートリアルは UI 操作を強制的に奪う実装ではなく、既存の主要画面へ誘導する軽量 guide とする。
進行状態は Application の Tutorial state が持ち、View は表示だけを行う。

Application:

- `TutorialProgressService`
  - 表示済み step
  - 現在 step
  - guide の完了状態
  - SaveData へ含める値
- `GetTutorialGuidanceUseCase`
  - 現在の session 状態から表示すべき guide を返す
- `MarkTutorialStepSeenUseCase`
  - 表示済み / 完了を記録する

Tutorial step 候補:

- DungeonInfo を開く
- GuildManagement を開く
- Market を開く
- Facility upgrade を確認する
- 日付進行を見る

View:

- `TutorialGuidePresenter`
- `TutorialGuideView`
- GameHUD 上の軽量 overlay / toast / pointer

Save / Load:

- 表示済み step を SaveData に含める方針を基本とする
- Continue 後に同じ guide が過剰に再表示されないようにする

禁止:

- Tutorial 進行状態を Presenter の field だけに置くこと。
- UI window の開閉状態を SaveData に含めること。
- Tutorial が ScreenStack / HUD の具象 window を直接操作し続けること。
- ガイド表示のために gameplay state を変更すること。

完了条件:

- [ ] New Game 後に最初の guide が表示される
- [ ] 表示済み step が Application state に記録される
- [ ] Save / Load 後に tutorial state が復元される
- [ ] Tutorial View は guide 表示だけを持つ

### Task 10: 統合テスト / 完了レビューの設計

Milestone 10 は複数の導線がつながって初めて完了する。
単体機能ごとの compile 成功だけで完了とせず、New Game / Save / Continue / Win / Lose / Result / Title return を通す。

EditMode test:

- New Game 初期化 test
- SaveData snapshot の保存対象 / 対象外 test
- Save / Load roundtrip test
- Continue metadata 判定 test
- 勝利条件到達 test
- 敗北条件到達 test
- 勝敗確定後に進行停止する test
- Result summary 作成 test
- Tutorial state 保存 / 復元 test

Play / uLoop:

- Title 表示
- Continue disabled
- New Game -> World
- `[World] GameWorldState initialized` ログ確認
- 日付表示確認
- Auto Save file 作成確認
- Stop -> Play または Title へ戻って Continue
- テスト用条件で勝利 / 敗北到達
- Result -> Back to Title
- 30 秒 Error なし

静的確認:

- `rg -n "SceneManager.LoadScene|Addressables.LoadAssetAsync|Resources.Load|UnityEngine.UI.Button" Client/Assets/DungeonInn/Runtime/Scripts`
- SaveData DTO に `UnityEngine.Object` が含まれていない
- Runtime public / internal API の追加理由が作業ログにある
- LifetimeScope に UI prefab catalog が増えていない
- View / Presenter が scene 境界を跨いで具象 View / Canvas / Presenter を直接参照していない

完了条件:

- [ ] compile が成功する
- [ ] EditMode test が成功する
- [ ] Play 30 秒で Error がない
- [ ] New Game / Save / Continue / Win / Lose / Result / Back to Title が確認済み
- [ ] `docs/self-review/` に Milestone 10 完了レビューがある

### 実装開始前ゲート

各 Task の実装前に、Codex は以下を確認し、問題があれば実装せず `review/{task_id}_question.md` で Claude Code に返す。

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

### テストで確認すること

EditMode test:
- New Game request から初期 GameWorldState が生成され、初期資金 / 施設 / Actor / Clock が仕様値になる。
- SaveData snapshot に View / Unity instance / Addressable handle が含まれない。
- SaveData を復元すると Guild / Facility / Actor / Inventory / Dungeon / Clock が一致する。
- セーブがない場合、Continue 判定が false になる。
- 勝利条件到達時に勝利状態とイベントが 1 回だけ発生する。
- 敗北条件到達時に敗北状態とイベントが 1 回だけ発生する。
- 勝敗確定後、WorldSimulationOrchestrator が通常の時間進行 / 経済進行を継続しない。
- Result summary が Application の summary / ViewData factory から作られる。
- Tutorial state が Save / Load 後に重複表示されない。

コード検索 / 静的確認:
- `SceneManager.LoadScene` / `Addressables.LoadAssetAsync` / `Resources.Load` を通常経路に追加していない。
- `UnityEngine.UI.Button` ではなく `LHButton` を使っている。
- LifetimeScope に UI View Prefab / Popup Prefab の catalog を追加していない。
- View / Presenter が `GameWorldState` を直接広く読まず、Query / ScreenService / ViewData を通している。
- SaveData DTO に `UnityEngine.Object` 派生型が含まれていない。

PlayMode / uLoop:
- `uloop.cmd compile --project-path Client` が成功する。
- `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する。
- Title で SaveData なしの場合 Continue が無効になる。
- New Game から World に入り、`[World] GameWorldState initialized` が出る。
- 日付表示が進み、日付切替時に Auto Save が走る。
- Continue で保存済み状態を復元できる。
- テスト用条件で勝利 / 敗北画面へ到達できる。
- Result から Back to Title / Restart が動作する。
- 30 秒 Play して Error ログがない。

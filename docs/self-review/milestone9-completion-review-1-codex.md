# Milestone 9 Completion Self Review 1 - Codex

レビュー日時: 2026-05-26

対象:
- `docs/roadmap/milestone9-roadmap.md`
- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/guidelines/debugging-policy.md`
- `docs/guidelines/self-review-guidelines.md`
- `Client/Assets/DungeonInn/Runtime/Scripts` の Milestone 9 変更範囲

## 総合判定

Milestone 9 は compile / EditMode test は通るが、ロードマップの完了条件を満たしていない項目が残っているため、完了扱いにはできない。

特に「施設アップグレードを実行できる」「宿屋アップグレード効果が回復速度へ反映される」「雑貨屋 / 装備屋ラインナップがレベルに応じて増える」は、実装済みログでは完了扱いになっているが、現行コード上では未達成。

## 1. 施設アップグレードを UI から実行できない

重大度: 高

問題:

Milestone 9 のゴールは「施設アップグレードと Market 取引を行えるようにする」ことだが、`GuildManagementWindow` は `GuildManagementWindowViewData` を文字列表示するだけで、施設別の実行ボタンや `UpgradeFacilityUseCase.Execute` への導線を持たない。`UpgradeFacilityUseCase` 自体は登録されているが、現行 UI から到達できない。

原因:

Task 5 の Application 実装と Task 4 の表示実装が接続されていない。Market 側は `MarketWindowData` に refresh / fulfill callback を持たせている一方、GuildManagement 側は `GuildManagementWindowData` が `ViewData` だけを持つ構造で止まっている。

解決案:

`GuildManagementWindowData` に refresh callback と facility upgrade callback を持たせ、`GuildManagementWindow` に施設ごとの Upgrade ボタンを生成する。実行後は `UpgradeFacilityUseCase.Execute(facilityId)` を呼び、`CreateGuildManagement()` で再取得して refresh する。実行不可の施設は button disabled にし、不可理由を表示する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GuildManagementWindow/GuildManagementWindow.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GuildManagementWindow/GuildManagementWindowData.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ScreenStack/GameHudWindowOpenService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/UpgradeFacilityUseCase.cs`
- `docs/roadmap/milestone9-roadmap.md`

完了条件:

- [ ] Guild Management window に施設ごとの Upgrade ボタンがある
- [ ] Button click で `UpgradeFacilityUseCase.Execute(facilityId)` が呼ばれる
- [ ] 実行後に施設レベル、消費済み inventory、取引履歴、preview が refresh される
- [ ] UI 操作を通した EditMode test または uLoop 操作ログで確認できる

## 2. 施設アップグレード効果が仕様に接続されていない

重大度: 高

問題:

ロードマップでは、宿屋はアップグレードで回復速度を増加、雑貨屋 / 装備屋は固定ラインナップをレベルに応じて増やす必要がある。現行コードでは `Facility.UpgradeTo` が `Level` / `Quality` / `Capacity` を更新するだけで、宿屋回復速度は `WorldGameSettingsSO` 由来の固定値を使い続けている。また `FacilityLineupMaster` / `FacilityLineupItemMaster` / `GetFacilityLineupUseCase` は存在しない。

原因:

`FacilityUpgradeMaster` に quality / capacity はあるが、施設効果を実行系が参照する契約が不足している。ラインナップ系はロードマップに想定型として残っているだけで、Master / UseCase / View のいずれにも実装されていない。

解決案:

宿屋回復速度は、`RecoverAdventurerAtInnUseCase` が予約中の inn facility を解決し、facility level / quality または upgrade master 由来の効果値を使って回復量を計算する。雑貨屋 / 装備屋は `FacilityLineupMaster` / `FacilityLineupItemMaster` と `GetFacilityLineupUseCase` を追加し、Guild Management の施設表示に現在ラインナップを含める。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/RecoverAdventurerAtInnUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Facility/Facility.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/FacilityUpgradeMaster.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `docs/roadmap/milestone9-roadmap.md`

完了条件:

- [ ] Inn upgrade 後の回復速度が upgrade 前より増えることをテストで検証する
- [ ] `FacilityLineupMaster` / `FacilityLineupItemMaster` 相当の固定ラインナップ定義がある
- [ ] `GetFacilityLineupUseCase` 相当の Application Query がある
- [ ] GeneralStore / EquipmentShop の表示ラインナップが level に応じて増える

## 3. ダンジョン情報に地上階層が含まれていない

重大度: 中

問題:

Task 2 の完了条件は「地上を含む階層リストが表示される」だが、`GetDungeonLayerInfoUseCase.Execute()` は `DungeonFloorExplorationMasters.Values` だけを列挙している。`HardcodedMasterRepository.CreateDungeonFloorExplorationMasters()` は floor 1〜3 のみで、`MapLayerId.Ground` の 0 は含まれていない。

原因:

Dungeon floor master をそのまま画面リストの正典として扱ったため、地上というダンジョン探索 master を持たない layer が漏れている。

解決案:

`GetDungeonLayerInfoUseCase` の先頭に ground summary を明示追加する。地上は spawn / drop なし、または地上用 master があるならそれを参照する。actor count は `MapLayerId.Ground.Value` で集計する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Dungeons/GetDungeonLayerInfoUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Map/MapLayerId.cs`
- `docs/roadmap/milestone9-roadmap.md`

完了条件:

- [ ] Dungeon Info window に Ground / 地上が表示される
- [ ] 地上の冒険者数が `MapLayerId.Ground` から集計される
- [ ] 地上の popup 表示仕様が明確で、空の場合は `None` と表示される

## 4. MarketOffer の解放条件を無視している

重大度: 中

問題:

`MarketOfferMaster` は `RequiredGuildTotalLevel` を持つが、`GetMarketOffersUseCase.Execute()` は `DisplayPriority` 順に先頭 3 件を取るだけで、解放条件を見ていない。`FulfillMarketOfferUseCase.Execute()` も offer ID を再解決するだけで、現在のギルドトータルレベルで解放済みか再判定しない。

現在の hardcoded offer は全て required level 0 のため実害は出にくいが、ロードマップは「解放済み候補から 3 件」「実行時も offer が解放済みか確認」としている。Master に契約がある以上、今のままだとデータ追加時に未解放 offer が表示・納品可能になる。

原因:

表示件数の固定 3 件だけが実装され、解放条件の判定が抜けている。

解決案:

施設レベル合計を計算する Application helper を追加し、`GetMarketOffersUseCase` で `RequiredGuildTotalLevel <= totalLevel` を満たす offer だけを表示する。`FulfillMarketOfferUseCase` でも同じ条件を再判定する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/GetMarketOffersUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/FulfillMarketOfferUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/MarketOfferMaster.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `docs/roadmap/milestone9-roadmap.md`

完了条件:

- [ ] total facility level 未満の offer が Market window に出ない
- [ ] 未解放 offer ID を直接実行しても状態変更されない
- [ ] 解放条件の EditMode test がある

## 5. Play 30 秒確認の必須ログを今回の再確認では取れていない

重大度: 中

問題:

今回のレビューで `uloop.cmd control-play-mode --action Play` を 30 秒実行しただけでは Title 止まりで、`[World] GameWorldState initialized` は出なかった。StartGameButton 呼び出しを試したが、今回のログ取得では必須ログを確認できていない。compile と EditMode test は成功しているが、AGENTS のハードゲートである通常ルート Play 確認は、今回のセルフレビューでは未達。

原因:

Play mode 起動だけでは World へ遷移しない。Milestone 9 実装ログには通常ルート確認済みとあるが、レビュー担当として現行状態で再現確認できていない。

解決案:

uLoop で Title の `TitleView` / `startGameButton` を確実に操作できる dynamic code または UI simulation を整備し、World 遷移後に 30 秒待機して logs を確認する。`[World] GameWorldState initialized` と error 0 をレビュー文書に残す。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/Title/TitleView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/Title/TitlePresenter.cs`
- `docs/roadmap/milestone9-roadmap.md`
- `AGENTS.md`

完了条件:

- [ ] Title -> World の通常ルートで Play 30 秒確認を再実行する
- [ ] `[World] GameWorldState initialized` がログに出る
- [ ] Error log が 0 件である
- [ ] 確認手順を再利用可能な形で作業ログに残す

## 確認結果

- `uloop.cmd compile --project-path Client`: 成功。Error 0 / Warning 0。
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功。308 / 308 pass。
- Play 30 秒確認: 単純 Play では Warning 2 件のみで、必須ログ `[World] GameWorldState initialized` は未確認。通常ルートの World 遷移確認は今回未達。

## guideline チェック

- Lighthouse: ScreenStack は Lighthouse 経由で開く構造だが、Guild Management の操作導線が未達。MainScene 具象直接参照は今回確認範囲では見つからなかった。
- Coding Rules: 今回の主要変更範囲で明示的 `private`、file-scoped namespace、Unity UI Button API の追加は見つからなかった。`UnityEngine.UI.Image` は表示部品として使用されており、Button API 違反ではない。
- Domain Design: Domain inventory 統合は行われていない。合算 inventory は Application snapshot として実装されている。
- Application Boundary: `UpgradeFacilityUseCase` と `FulfillMarketOfferUseCase` は他 UseCase 呼び出しをしていない。ただし Market 解放条件の再判定が不足している。
- Implementation Quality: Runtime 配置の `Milestone9` 名は修正済み。ただしロードマップの完了条件を満たさないまま実装ログで完了扱いになっている。
- Debugging / Self Review: compile / tests は再確認済み。Play 必須ログは今回未確認のため未達として扱う。


# Milestone 8 Roadmap — UI 基盤と HUD

## ゴール

プレイヤーがゲームの状態を一目で把握できる UI 基盤を構築する。
「今何時か」「資金はいくらか」「シミュレーション速度はどうか」を常に確認でき、
宿に何人いるかを把握できる状態にする。

## 対象範囲

### HUD（常時表示）

- ゲーム内時間の表示（時刻・日付）
- ギルド資金の表示
- シミュレーション速度コントロール（一時停止・等速・倍速）
- 簡易アラート表示（重要イベント通知のポップ）

### Inn ステータスパネル

- 現在宿屋に宿泊中の冒険者リスト（名前・回復状態・残滞在時間）
- 当日の収入・支出サマリ

### ミニマップ / 俯瞰表示

- Ground / Dungeon の 2D 俯瞰表示（簡易マップ）
- 現在表示中の layer と Actor の配置を概略で確認できる
- 詳細なマップ UI は Milestone 9 の施設管理と合わせて整理する

### UI アーキテクチャ基盤

- World シーンの UI Canvas 配置方針の確定
- HUD 専用 LifetimeScope の整理
- Application 層のクエリ UseCase（`GetGameTimeStateUseCase` など）を UI が利用する経路の確立
- View が `IGameWorldStateReader` を直接読んで集計しない方針の徹底

### World シーングループ LifetimeScope 分離

Milestone 7 で World の UI Canvas を `WorldUI` ModuleScene に分離した。
ただし、World のゲームロジックが `WorldLifetimeScope` に残ったままだと、`WorldLifetimeScope` の子ではない `WorldUILifetimeScope` が同じゲーム状態や UseCase を自然に受け取れない。
Milestone 8 では、World の 3D 表現と Canvas UI を兄弟 Scene として扱い、その共通親にゲームロジック用 LifetimeScope を置く構成へ整理する。

想定構造:

```text
RootLifetimeScope
  ProductLifetimeScope
    WorldGameLifetimeScope
      WorldLifetimeScope
      WorldUILifetimeScope
```

- `WorldGameLifetimeScope`
  - World シーングループの親 LifetimeScope として追加する
  - World のゲーム状態、ゲームループ、Application UseCase、EventBus、World 全体で共有する Service を登録する
  - 3D View / Canvas View / UI View Prefab の実体は保持しない
- `WorldLifetimeScope`
  - World MainScene 固有の 3D 表現に責務を限定する
  - Actor View、Map View、Projectile View、AreaEffect View、Environment Prop など 3D View 系 Presenter / Factory / Pool を登録する
  - Canvas / HUD / Popup / UI View の登録や直接参照を持たない
- `WorldUILifetimeScope`
  - `WorldUI` ModuleScene 固有の Canvas / HUD / Popup / UI Presenter / UI Factory を登録する
  - ゲーム状態は親の `WorldGameLifetimeScope` から、Application Query / 表示専用 DTO / Event 購読経由で受け取る
  - `IGameWorldStateReader` のような広い Reader を UI Presenter が直接読んで集計しない

実装方針:

1. `ProductEntryPoint.StartAsync` で行っている `mainSceneManager.SetEnqueueParentLifetimeScope` / `moduleSceneManager.SetEnqueueParentLifetimeScope` は、デフォルト親を `ProductLifetimeScope` にする初期設定として維持する。
2. `ProductSceneManager` から利用する小さな協調クラス（例: `SceneGroupLifetimeScopeManager`）を追加し、遷移先 `MainSceneId` に応じて SceneGroup 用親 LifetimeScope を生成・保持・破棄する。
3. World へ遷移する前に `WorldGameLifetimeScope` を `ProductLifetimeScope` の子として生成し、MainScene / ModuleScene の enqueue parent を `WorldGameLifetimeScope` に差し替える。
4. Lighthouse の `LoadSceneGroupStep` は MainScene → ModuleScene の順で `Load` するため、World MainScene と `WorldUI` ModuleScene の両方が同じ `WorldGameLifetimeScope` を親にできる。
5. World 以外へ遷移する場合は enqueue parent を `ProductLifetimeScope` に戻し、World シーングループの unload 完了後に `WorldGameLifetimeScope` を破棄する。
6. Reboot / BackScene / 例外復旧時に `WorldGameLifetimeScope` が残留しないよう、`PreReboot` と遷移完了時の cleanup 経路を定義する。

完了条件:

- [ ] `WorldGameLifetimeScope` が追加され、World のゲームロジック共有依存がそこに移動している
- [ ] `WorldLifetimeScope` は 3D 表現固有の登録に限定され、Canvas / HUD / Popup の責務を持っていない
- [ ] `WorldUILifetimeScope` は `WorldGameLifetimeScope` の子として生成され、HUD / Popup が親 scope の Query / Event / DTO 経由で World 状態を表示できる
- [ ] `ProductSceneManager` またはその協調クラスが、World 遷移時だけ SceneGroup 用親 LifetimeScope を enqueue parent に設定している
- [ ] World から別 MainScene へ離脱した後、`WorldGameLifetimeScope` とその scoped disposable が破棄される
- [ ] `WorldLifetimeScope` / `WorldUILifetimeScope` にゲームコンテンツ Prefab / UI View Prefab / Popup View 実体の `SerializedField` がない
- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している
- [ ] Play モードで World へ遷移し、`[World] GameWorldState initialized` が出力され、エラーログが存在しない

## Milestone 9 へ移動する項目

- 施設アップグレード画面
- スタッフ管理 / 雇用画面
- 経済レポート詳細

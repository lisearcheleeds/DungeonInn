# Milestone 7 Completion Review 6 Codex 2

作成日: 2026-05-22
対象: staged diff 全体（Milestone 7 実装差分 + self-review-6-codex-1 対応差分）
レビュー担当: Codex

## 事前確認

レビュー直前に以下を読み直した。

- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/guidelines/debugging-policy.md`
- `docs/guidelines/self-review-preset.md`

あわせて以下を確認した。

- `docs/roadmap/milestone7-roadmap.md`
- `tasks/task_0008.md`
- `docs/self-review/milestone7-completion-review-6-codex-1.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/*`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/*`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/WorldUI/*`

## 前回レビュー対応状況

- `ActorDetailPopupPresenter` / `WorldActorStatusPresenter` から広い `IGameWorldStateReader` 依存が外れ、Application の narrow query / read model から必要値を取得する形になった。
- `ActorDetailPopup` の Prefab は `WorldAddressableViewFactory` 経由で生成され、LifetimeScope の `SerializedField` には戻っていない。
- `WorldHudCanvasProvider` の fallback は Warning を出し、`DontDestroyOnLoad` による永続化をやめた。
- `uloop.cmd compile --project-path Client` は成功（ErrorCount 0 / WarningCount 0）。

## レビュー結果

### 1. task_0008 の本文が古い ScreenStack / 直接参照設計のまま残っている

重大度: 高

問題:

`tasks/task_0008.md` の作業ログでは ScreenStack から HUD パネル方式への設計変更が補足されたが、タスク本文の目的・利用パターン・Presenter 依存・登録例にはまだ `ScreenStack Popup`、`IScreenStackModule`、`IGameWorldStateReader`、`ActorDetailPopup` の直接登録が残っている。レビュー時に本文だけを読むと、現行実装と逆の設計を正と誤読する。

原因:

レビュー対応で変更理由を追記しただけで、タスク本文の正典記述を更新していない。self-review-preset の「docs の仕様、設計、roadmap、self-review と現行実装が一致しているか」に対して不十分。

解決案:

`tasks/task_0008.md` の本文を現行設計へ更新する。Actor 詳細は ScreenStack ではなく WorldUI HUD パネルであり、Prefab は Addressable Factory が生成、Presenter は narrow query DTO による表示制御のみを行う、と明記する。古い `IScreenStackModule` / `RegisterComponentInHierarchy<ActorDetailPopup>` / Presenter の `IGameWorldStateReader` 依存は削除する。

根拠となるファイルリスト:

- `tasks/task_0008.md`
- `docs/roadmap/milestone7-roadmap.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorDetailPopupPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldAddressableViewFactory.cs`

完了条件:

- [ ] `tasks/task_0008.md` の目的・利用パターン・Presenter 依存・登録例から `ScreenStack Popup` / `IScreenStackModule` / `RegisterComponentInHierarchy<ActorDetailPopup>` が消えている
- [ ] `ActorDetailDto` に `Position` が含まれることがタスク本文に反映されている
- [ ] `ActorDetailPopupPresenter` が `IGameWorldStateReader` を直接受けない設計としてタスク本文に反映されている
- [ ] Addressable Factory 生成と Presenter 経由操作がタスク本文に明記されている

### 2. WorldUI 実装に対して roadmap / task の名称が WorldHUDModuleScene のまま混在している

重大度: 中

問題:

現行実装は `WorldUI.unity` / `WorldUIModuleScene` / `WorldUILifetimeScope` だが、`docs/roadmap/milestone7-roadmap.md` と `tasks/task_0006.md` には `WorldHUDModuleScene` / `WorldHUDLifetimeScope` が正典のように残っている。ユーザー指示は WorldUI ModuleScene への分離であり、M8 の親 LifetimeScope 設計も WorldUI 前提になっているため、名称混在は後続タスクの誤実装につながる。

原因:

WorldHUD から WorldUI へ設計名が変わった後、コードと一部ロードマップだけが更新され、M7 の詳細タスクと古い章見出しが追従していない。

解決案:

M7 実装完了時点の名称を `WorldUI` に統一する。過去ログとして WorldHUD 名を残す必要がある箇所は「旧称」として扱い、現行仕様・完了条件・登録グループでは `WorldUIModuleScene` / `WorldUILifetimeScope` を使う。

根拠となるファイルリスト:

- `docs/roadmap/milestone7-roadmap.md`
- `tasks/task_0006.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/WorldUI/WorldUIModuleScene.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/WorldUI/WorldUILifetimeScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scene/ModuleScene/WorldUI.unity`

完了条件:

- [ ] 現行仕様・完了条件・登録グループの名称が `WorldUI` に統一されている
- [ ] `WorldHUDModuleScene` が残る場合は旧称または過去ログとして明示されている
- [ ] M7 roadmap と task_0006 の Canvas ModuleScene 名が現行コードと一致している

### 3. ActorStatusViewData の Effect cache が RemainingSeconds を古い値のまま返しうる

重大度: 中

問題:

`GetActorStatusSummaryQuery` は `ActorEffectIconData` の配列を cache するようになったが、cache signature は active な `ActorEffectMasterId` だけを見ている。Effect の種類が同じまま `ElapsedSeconds` だけが進む場合、`ActorEffectIconData.RemainingSeconds` が最初に生成された値のまま返る。現行 `ActorStatusView` は RemainingSeconds を表示していないが、DTO 契約としては stale な値を返すため、後続で残り秒表示を足した時に不具合になる。

原因:

Frame Loop allocation を避ける目的で cache したが、DTO が持つ全フィールドの更新契機を signature に含めていない。表示で未使用という現行 View 都合と、Application DTO の契約が分離されていない。

解決案:

M7 では毎フレーム allocation を避けつつ、残り秒が変わる単位で cache を更新する。具体的には signature に `InstanceId` と丸めた残り秒（例: ceiling seconds）を含める。もしくは `ActorStatusViewData` から RemainingSeconds を外し、残り秒が必要な詳細表示は `ActorDetailDto` だけで扱う。ただし後者は DTO 契約変更が大きいため、今回は signature 修正が適切。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetActorStatusSummaryQuery.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/ActorStatusViewData.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/WorldUI/ActorStatusView.cs`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `GetActorStatusSummaryQuery` の cache signature が `ActorEffectMasterId` だけに依存していない
- [ ] 同じ Effect が継続中でも、DTO が返す `RemainingSeconds` が長期間 stale にならない
- [ ] Effect 配列の再生成が毎フレームではなく、表示上意味のある変化単位に抑えられている
- [ ] `uloop.cmd compile --project-path Client` が成功している

## 総合判定

前回の重大なコード境界違反は大半が解消された。ただし、M7 の正典資料に古い ScreenStack / WorldHUD 設計が残っているため、このままでは次マイルストーンに進める状態とは判断しない。上記 1〜3 を修正した上で、再度同じ観点でレビューする。

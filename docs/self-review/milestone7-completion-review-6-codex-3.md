# Milestone 7 Completion Review 6 Codex 3

作成日: 2026-05-22
対象: staged diff 全体（Milestone 7 実装差分 + self-review-6-codex-1/2 対応差分）
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
- `tasks/task_0006.md`
- `tasks/task_0008.md`
- `docs/self-review/milestone7-completion-review-6-codex-1.md`
- `docs/self-review/milestone7-completion-review-6-codex-2.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/*`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/*`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/WorldUI/*`

## 前回レビュー対応状況

- task_0008 の現行仕様は `WorldUI HUD パネル` / Addressable Factory / Presenter 経由操作へ更新済み。
- task_0006 と M7 roadmap の現行仕様名称は `WorldUI` / `WorldUIModuleScene` / `WorldUILifetimeScope` に統一済み。
- `GetActorStatusSummaryQuery` の Effect cache signature は `ActorEffectMasterId` だけでなく `InstanceId` と丸めた残り秒を含むようになり、同一 Effect 継続中も `RemainingSeconds` が長期間 stale にならない。
- `GetActorStatusSummaryQueryRefreshesCachedEffectRemainingSeconds` を追加し、cache 更新漏れを EditMode test で検出できるようになった。
- `uloop.cmd compile --project-path Client` は成功（ErrorCount 0 / WarningCount 0）。
- `uloop.cmd run-tests --project-path Client --test-mode EditMode` は成功（284/284 pass）。

## 差分分類と許可判断

production 契約変更として確認した差分:

- `ActorDetailDto.Position`
  - 許可理由: Presenter が `IGameWorldStateReader` を直接読まず、Popup 位置追従に必要な表示用 DTO から位置を受け取るため。
- `IActorStatusViewDataProvider.CopyActiveActorsTo(List<ActorViewData>)`
  - 許可理由: View が broad world state を走査せず、Application の表示用 read model から active actor snapshot を受け取るため。呼び出し側バッファに copy する API で、毎フレーム allocation を避ける意図も名前に出ている。
- `GetActorStatusSummaryQuery` の cache
  - 許可理由: Frame Loop から呼ばれる status summary の Effect 配列再生成を、表示上意味のある変化単位へ抑えるため。cache の correctness は EditMode test で検証済み。
- `WorldAddressableViewFactory.CreateActorDetailPopup(Transform)`
  - 許可理由: UI View Prefab 生成を Presenter 直書きから Addressable Factory に寄せ、LifetimeScope と Presenter が View 実体を直接持つ範囲を狭めるため。

新規概念追加ゲート:

- `ActorStatusViewData` / `ActorEffectIconData`
  - 類似概念: `ActorViewData`, `ActorDetailDto`
  - 意味差分: 頭上ステータス表示用の HP 比率と Effect icon summary。`ActorViewData` は位置/振る舞い、`ActorDetailDto` は選択 Actor 詳細。
  - 代替不可理由: 位置差分と status content は更新頻度・利用者が異なり、Detail DTO で代替すると常時 HUD が不要な詳細情報まで要求する。
  - 統合条件: Actor UI read model が共通化され、頭上 HUD と詳細パネルの更新契機を安全に分離できる場合。
- `WorldAddressableViewFactory`
  - 類似概念: `VisualConfigLoader`, `ProjectilePrefabSource`, `AreaEffectPrefabSource`
  - 意味差分: Addressable から Prefab をロードし、Pool / Presenter に提供する View asset factory。
  - 代替不可理由: `VisualConfigLoader` は Sprite/Material 設定、PrefabSource は生成済み Prefab の選択であり、Addressable scope 寿命を所有しない。
  - 統合条件: WorldUI / World3D の View asset repository が M8 以降で共通化される場合。

## レビュー結果

今回のレビューで、次マイルストーンへ進行を止める新規指摘はなし。

確認した主要観点:

- `WorldLifetimeScope` にゲームコンテンツ Prefab / UI View Prefab / Popup View 実体の `SerializedField` は残っていない。
- Projectile / AreaEffect / Prop の Prefab address は `WeaponTypeCombatMaster` / `EnvironmentPropVisualMaster` など発生元データから解決されている。
- `ActorDetailPopupPresenter` と `WorldActorStatusPresenter` は `IGameWorldStateReader` を直接受け取っていない。
- `ActorDetailPopup` は DI で他クラスへ直接公開されず、Presenter と Factory の内側で扱われている。
- World の Screen Space Overlay Canvas は `WorldUI` ModuleScene 側へ分離されている。
- fallback Canvas は Warning 付きの開発保険に留まり、`DontDestroyOnLoad` で残留しない。
- M7 roadmap / task_0006 / task_0008 の現行仕様は WorldUI / Addressable HUD パネル方式と一致している。
- `GetActorStatusSummaryQuery` の Effect 配列生成は毎フレーム固定 allocation ではなく、Effect 構成または丸め秒の変化時に限定されている。
- `uloop compile` と EditMode test の証跡がある。

## 総合判定

Milestone 7 の実装差分は、self-review-6-codex-1/2 の指摘対応後、少なくとも今回レビュー観点では次のマイルストーンへ進める状態と判断する。

残る構造課題:

- `WorldHudCanvasProvider` が `Object.FindFirstObjectByType<WorldUIModuleScene>()` に依存している点は、M8 の `WorldGameLifetimeScope` / World と WorldUI の共通親 scope 化で根治する前提。
- `DungeonInnModuleSceneId.g.cs` は staged diff に含まれているため、最終完了前に uLoop / Unity Editor 経由の生成または compile 時再生成結果として扱えることを最終報告に残す。

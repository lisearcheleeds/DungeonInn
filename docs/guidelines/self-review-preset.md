# セルフレビュープリセット

このドキュメントは、マイルストーン完了確認やプロジェクト全体レビューを行うときの標準プロンプトと出力ルールを定義する。

レビューの目的は「問題を列挙すること」ではなく、「指摘の意図、根本原因、解決後の完了状態を明文化し、実装対応時にゴールを取り違えないようにすること」である。

## レビュープロンプト

以下をベースプロンプトとして使う。

```text
このプロジェクトはUnityプロジェクトです。まずはdocsの下を確認してください。
現在は{milestone_name}の確認作業中です。
あなたにはこのプロジェクト全体のレビューをお願いしたいです。
観点は設計、整合性、パフォーマンス、重複した機能を持つクラスやデータクラス、その他総合の4種類で、それぞれ専門家として詳細にレビューをお願いします。
精度を上げるためにエージェントをわける必要があれば許可します。
レビューの内容は項目ごとに問題、原因、解決案、根拠となるファイルリスト、完了条件をdocsの下にmdファイルで記載してください。
ファイル名は `{output_file_name}`。
```

`{milestone_name}` には `マイルストーン5` のような確認対象を入れる。
`{output_file_name}` には `milestone5-completion-review-3-codex.md` のような出力ファイル名を入れる。

## レビュー前に必ず確認する資料

レビュー担当者は、レビュー開始前に以下を確認する。

- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- 対象マイルストーンの `docs/roadmap/*.md`
- 対象マイルストーンに関連する `docs/design/*.md`
- 既存の `docs/self-review/*.md`
- `Client/Assets/DungeonInn/Runtime/Scripts` 配下の現行実装

既存レビューを読むときは、本文の指摘と末尾の対応ログを両方確認する。
本文に残っていても対応ログと現行コードで解消済みの項目は、未解決問題として再掲しない。
再掲する場合は「現行コードでも未解決である根拠」を必ず示す。

## レビュー観点

### 設計

- Clean Architecture の依存方向が守られているか
- Domain / Application / Infrastructure / View の責務が混ざっていないか
- UseCase / Service / StateService / Orchestrator の境界が明確か
- View 層がゲーム進行、Domain 判定、Application orchestration を握っていないか
- DI で解決すべき依存を `new` / static / singleton / 手動検索で生成していないか
- Lighthouse のパターンに反していないか

### 整合性

- docs の仕様、設計、roadmap、self-review と現行実装が一致しているか
- 既存レビューで「対応済み」とされた項目が本当に完了条件を満たしているか
- 設計資料にあるイベント、状態遷移、マスタ値、画面責務が実装に反映されているか
- レビュー文書内で「未対応」と「対応済み」が混在し、完了判断を誤らせる状態になっていないか

### パフォーマンス

- 毎フレーム処理、schedule tick、event-driven 処理が分離されているか
- Frame Loop 内で LINQ、`ToList()`、`ToArray()`、コレクション生成、全件走査が発生していないか
- Actor 数、Item 数、Floor 数、AreaEffect 数に比例して急激に重くなる処理がないか
- Mesh / GameObject / Material / Sprite / Log の生成が高頻度に発生していないか

### 重複した機能を持つクラスやデータクラス

- 似た役割の DTO / ValueObject / Request / Factory / Repository / Service が並列に存在していないか
- 共通化済みの下位実装に対して、上位 interface / facade / request が冗長に残っていないか
- UI 用 DTO、履歴用 DTO、Domain summary の正典が分散していないか
- 同じ Actor-keyed state 管理が複数 Service に散らばっていないか

### その他総合

- Lighthouse 禁止 API が追加または残存していないか
- LifetimeScope / Installer の DI 登録漏れがないか
- テスト不足により完了判定が主観的になっていないか
- PlayMode / uLoop / EditMode test の証跡が不足していないか
- 命名、フォルダ、namespace が責務と一致しているか

## レビュー項目の必須フォーマット

レビュー項目は、必ず以下の5項目をすべて書く。
1つでも欠ける場合、そのレビュー項目は未完成とする。

```md
### {番号}. {指摘タイトル}

重大度: 重大 / 高 / 中 / 低

問題:

何が問題なのかを書く。
症状だけでなく、設計意図や仕様と何が食い違っているかを明記する。

原因:

なぜその問題が起きているのかを書く。
「依存が多い」「処理が重い」のような表層ではなく、所有者・境界・実行タイミング・データ正典などの根本原因を書く。

解決案:

どう直すべきかを書く。
暫定対応と根治対応が違う場合は分けて書く。
既存コードを残す場合は、例外扱いにする理由と追跡方法を書く。

根拠となるファイルリスト:

- `path/to/file.cs`
- `docs/path/to/design.md`

完了条件:

- [ ] 何がコード上から消えていれば完了か
- [ ] 何が新しい責務の所有者になっていれば完了か
- [ ] どのテスト、検索、compile、PlayMode 確認で完了を検証するか
- [ ] ドキュメント更新が必要な場合、どのファイルが更新されていれば完了か
```

## 完了条件の書き方

完了条件は、実装者やレビュー担当者が機械的に確認できる形にする。

悪い例:

```md
完了条件:
- 責務が整理されている
- パフォーマンスが改善されている
```

良い例:

```md
完了条件:
- [ ] `WorldGameLoopEntryPoint` が個別 UseCase / Orchestrator を直接注入していない
- [ ] `WorldGameLoopEntryPoint.TickAsync()` に spawn / AI / combat / item / inn の実行順序が残っていない
- [ ] Application 層に `AdvanceWorldFrameUseCase` または `WorldSimulationOrchestrator` が存在し、1フレームのゲーム進行順序を所有している
- [ ] `WorldGameLoopEntryPoint` は delta time、キャンセル、camera 更新、view 更新の呼び出しだけを担当している
- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] 追加または変更した Application orchestrator の EditMode test がある
```

## レビュー対応完了の判定ルール

レビューで指摘された項目は、完了条件をすべて満たすまで「対応済み」と書いてはならない。

以下は対応済みとして扱ってはいけない。

- 指摘の一部だけを直した
- 関連する別問題だけを直した
- 依存数を減らしたが、責務の所有者は変わっていない
- docs の文言だけを更新し、現行コードが完了条件を満たしていない
- 「Milestone 6で対応」と書いたが、未対応項目として追跡していない

対応が完了していない場合は、必ず以下のいずれかで記録する。

- `未対応`
- `一部対応`
- `Milestone X へ延期`
- `ユーザー判断待ち`
- `別タスク化済み`

## 再発防止ルール

同じ指摘が3回以上レビューに出た場合、以降は通常レビュー項目ではなく「再発項目」として扱う。

再発項目には、通常の5項目に加えて以下を書く。

```md
再発理由:

なぜ過去の対応で根治しなかったのか。
対応済み判定を誤った場合は、どの完了条件が不足していたのか。

再発防止策:

レビューゲート、テスト、検索コマンド、アーキテクチャテスト、タスクテンプレート修正など、再発を機械的に防ぐ方法を書く。
```

再発項目は、完了条件に再発防止策の実装を含める。

## 出力先

レビュー結果は `docs/self-review/` 配下に保存する。

例:

- `docs/self-review/milestone5-completion-review-3-codex.md`
- `docs/self-review/milestone6-design-review-claude.md`
- `docs/self-review/milestone6-completion-review-total.md`

## 最終チェック

レビュー文書を完了する前に、以下を確認する。

- [ ] 各レビュー項目に「問題」がある
- [ ] 各レビュー項目に「原因」がある
- [ ] 各レビュー項目に「解決案」がある
- [ ] 各レビュー項目に「根拠となるファイルリスト」がある
- [ ] 各レビュー項目に「完了条件」がある
- [ ] 既存レビューの対応ログを確認した
- [ ] 解消済み項目を未解決として再掲していない
- [ ] 未解決項目を対応済みとして扱っていない
- [ ] 同じ指摘が3回以上出ている場合、再発理由と再発防止策を書いた

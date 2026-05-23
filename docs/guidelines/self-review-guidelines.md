# セルフレビュープリセット

このドキュメントは、マイルストーン完了確認やプロジェクト全体レビューを行うときの標準プロンプトと出力ルールを定義する。

レビューの目的は「問題を列挙すること」ではなく、「指摘の意図、根本原因、解決後の完了状態を明文化し、実装対応時にゴールを取り違えないようにすること」である。

## ハードゲート

この節は即時停止・修正が必要な禁止事項を列挙する。
この節に載っていないレビューや対応が自動的に許可されるわけではなく、本文の設計方針・判断基準に反する場合もレビュー指摘または作業停止対象とする。

- [ ] レビュー開始前に必読資料を確認せず、レビューを開始していない
- [ ] 既存レビューの対応ログを確認せず、解消済み項目を未解決として再掲していない
- [ ] 現行コードで未解決である根拠なしに、過去指摘を再掲していない
- [ ] Runtime public / internal API、constructor、interface、DTO、Request、Event の追加・変更を、production 契約変更として分類せず許可していない
- [ ] テスト都合だけの Runtime surface 追加を許可していない
- [ ] DI 管理対象を Composition Root / Installer / LifetimeScope 以外で `new` する差分を許可していない
- [ ] 新規概念追加ゲートの記録なしに、新しい DTO / Result / Request / Store / Service / Calculator / Factory / Event / State を許可していない
- [ ] レビュー項目の必須5項目（問題・原因・解決案・根拠となるファイルリスト・完了条件）が欠けたままレビュー文書を完了していない
- [ ] 完了条件を満たしていない項目を「対応済み」として扱っていない
- [ ] 同じ指摘が3回以上出ているのに、再発理由と再発防止策を書かずに通常指摘として処理していない
- [ ] 専任エージェントのレビュー結果だけで milestone 完了可否を判断せず、最終統合レビューで判断している

## 完了前チェックリスト

このチェックリストは本文のレビュー方針を省略するためのものではない。
レビュー時は本文を確認したうえで、最後に確認漏れを防ぐ目的で使用する。

- [ ] レビュー前に必読資料を確認した
- [ ] 差分許可モデルで production 契約変更に該当する差分を分類した
- [ ] 許可した production 契約変更について、責務・境界・寿命・依存方向の理由を記録した
- [ ] 新規概念追加ゲート対象について、既存類似概念・意味差分・代替不可理由・統合削除条件を記録した
- [ ] guideline ごとの専任レビューを一次レビュー軸とし、設計・整合性・パフォーマンス・重複・総合は統合レビューの横断確認観点として扱っている
- [ ] マイルストーン完了レビュー / 全体レビューでは guideline ごとの専任エージェントレビューを行い、各専任レビューを最終統合レビューで重複排除・重大度調整・完了可否判定している
- [ ] 各レビュー項目に問題・原因・解決案・根拠となるファイルリスト・完了条件を書いた
- [ ] 完了条件が機械的に確認できる形になっている
- [ ] 未対応 / 一部対応 / 延期 / ユーザー判断待ち / 別タスク化済みを区別して記録した
- [ ] 再発項目には再発理由・再発防止策・再発防止策の完了条件を含めた
- [ ] レビュー結果を `docs/self-review/` 配下に保存した

## レビュープロンプト

以下をベースプロンプトとして使う。

```text
このプロジェクトはUnityプロジェクトです。まずはdocsの下を確認してください。
現在は{milestone_name}の確認作業中です。
あなたにはこのプロジェクト全体のレビューをお願いしたいです。
一次レビューの軸は guideline ごとの専任レビューとしてください。設計、整合性、パフォーマンス、重複した機能を持つクラスやデータクラス、その他総合は、専任レビューの担当軸ではなく、最後の統合レビューで抜け漏れを確認する横断観点として扱ってください。
精度を上げるため、guideline ごとに専任エージェントを必ず分けてレビューしてください。最後に必ず統合レビューで重複排除、重大度調整、完了可否判定を行ってください。
レビューの内容は項目ごとに問題、原因、解決案、根拠となるファイルリスト、完了条件をdocsの下にmdファイルで記載してください。
ファイル名は `{output_file_name}`。
```

`{milestone_name}` には `マイルストーン5` のような確認対象を入れる。
`{output_file_name}` には `milestone5-completion-review-3-codex.md` のような出力ファイル名を入れる。
ファイル名フォーマットは `milestone{マイルストーン番号}-{review-phase}-review-{increment-index}-{ai-type}.md` とする。
`review-phase` は `before` / `after` のいずれか、`ai-type` は `codex` / `claude` / `total` などレビュー主体を表す値を使う。
`increment-index` は同一 milestone / review-phase / ai-type のレビュー回数として 1 から増やす。

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

## 精密レビュー時の専任エージェント運用

マイルストーン完了レビューや全体レビューでは、レビュー前に必ず確認する guideline ごとに専任エージェントを立てる。
専任エージェントは担当 guideline に強くバイアスを掛け、担当外の観点を主目的にしない。

**Coding Rules 専任は他の専任エージェントと並行せず、先行フェーズとして独立させること。**
理由: コーディング規約違反は実動作に影響しないため、設計問題を探す認知負荷の高いフェーズでは自然言語の読み取りで検知しにくい。`coding-rules.md` 記載の「コーディング規約スキャン」節の grep パターンを先に実行し、候補を機械的に抽出してから判定すること。

専任レビューの推奨分担:

- **[先行フェーズ] Coding Rules 専任**: `docs/guidelines/coding-rules.md` の「コーディング規約スキャン」節に記載の grep パターンを実行し、出力を根拠に命名、prefix / suffix、namespace、`[Inject]` 明示、C# スタイルの違反を判定する。自然言語での設計読み取りは補助的な役割とする。
- Lighthouse 専任: `docs/guidelines/lighthouse-patterns.md` を根拠に、Lighthouse パターン、DI、LifetimeScope、Addressable、AssetScope、禁止 API を確認する。
- Domain Design 専任: `docs/guidelines/domain-design-guidelines.md` を根拠に、Domain / Entity / ValueObject / Calculator / DTO の責務と依存方向を確認する。
- Application Boundary 専任: `docs/guidelines/application-boundary-guidelines.md` を根拠に、UseCase / Orchestrator / Service / Event / Aggregate / ゲームループ境界を確認する。
- Implementation Quality 専任: `docs/guidelines/implementation-quality-guidelines.md` を根拠に、テスト都合 API、DI 手動生成、重複概念、定数 / ScriptableObject、TODO、cache、長期状態を確認する。

専任レビューの出力ルール:

- 各指摘には、担当 guideline のどの方針・ハードゲート・完了前チェックに関係するかを書く。
- 担当外 guideline の判断が必要な場合は、断定せず「統合レビューで確認」として渡す。
- 同じ問題の別表現を増やさず、担当 guideline から見た根本原因と完了条件に絞る。
- 設計、整合性、パフォーマンス、重複、総合の分類を専任レビュー内の独立カテゴリとして増やさない。
- 専任レビュー単体で milestone 完了可否を判断しない。

専任レビューの完了後、最後に必ず統合レビューを行う。
統合レビュー担当は、各専任レビューを横断して以下を実施する。

- 重複指摘を統合し、同じ問題を複数項目として残さない。
- guideline 間で評価が割れた場合、どの guideline を優先するかと理由を書く。
- 設計、整合性、パフォーマンス、重複、総合の横断観点で、専任レビューの抜け漏れがないか確認する。
- 重大度を揃え、milestone 完了を止める問題と次 milestone へ送る問題を分ける。
- 各指摘の完了条件が機械的に確認できる形になっているか確認する。
- 最終レビュー文書に、専任レビューを使ったこと、担当分担、統合判断の結果を短く記録する。

## 差分許可モデル

レビュー担当者は、docs を「読んだ資料」として扱うだけでなく、差分を許可する条件として使う。
実装差分またはレビュー対応差分を確認するときは、指摘項目ごとのチェックリストを増やす前に、差分そのものを以下の分類へ通す。

### 差分分類

差分に以下が含まれる場合、その変更は通常の実装修正ではなく、production 契約変更として扱う。

- Runtime 側の `public` / `internal` API、constructor、interface、DTO、Request、Event を追加・変更している
- DI 解決対象クラスの constructor、LifetimeScope 登録、注入型を追加・変更している
- Runtime 側で `new XxxService()`、`new XxxUseCase()`、`new XxxRepository()`、static / singleton / 手動検索による依存解決を追加している
- テストを通す、既存テスト修正量を減らす、互換を保つ、という理由で Runtime API や constructor を追加している
- adjustable value、cache、dirty flag、index、registry、Actor-keyed state などの長期状態を追加している

### 許可条件

分類に該当した差分は、以下をすべて満たす場合だけ許可する。

- production 側の責務として、その API / constructor / 依存生成が必要な理由を説明できる
- 該当ガイドライン上で許可される所有者、境界、寿命、依存方向に置かれている
- テスト都合だけで Runtime surface を増やしていない
- DI 管理対象を Composition Root / Installer / LifetimeScope 以外で `new` していない
- テスト補助は Runtime ではなく Tests 側の helper / fixture / test double に閉じている
- docs にない設計判断が必要な場合は、実装やレビュー完了判定を止めてユーザーに確認している

上記を満たせない差分は、compile や run-tests が成功していてもレビュー上は不許可とする。

### 差分分類から見る例

```text
GameWorldState() を追加する
→ Runtime public constructor の追加
→ production caller と production 責務がない
→ 既存テスト互換だけが理由
→ implementation-quality-guidelines / AGENTS の方針に反する
→ 不許可。Tests 側で GameWorldState(new ActorSpatialIndexService()) または helper を使う。
```

```text
Runtime 内で new ActorSpatialIndexService() を追加する
→ DI 管理対象 Service の手動生成
→ LifetimeScope の寿命管理と一致しない
→ AGENTS の DI 生成禁止に反する
→ 不許可。LifetimeScope 登録と constructor injection に寄せる。
```

```text
ActorSpatialIndexService.Revision を追加する
→ cache invalidation 用の長期状態
→ Application Service が spatial index と cache invalidation の所有者であることを説明できる
→ 名前が用途を表し、テストで invalidation を検証している
→ 許可。ただし Version のような広すぎる名前は避ける。
```

### 差分分類の記録

レビュー文書または対応ログには、必要に応じて「差分分類」と「許可理由 / 不許可理由」を短く残す。
これは個別チェック項目を増やすためではなく、docs の既存ルールを差分へ適用した判断過程を残すためである。

## 新規概念追加ゲート

レビュー担当者・実装担当者は、新しい DTO / Result / Request / Store / Service / Calculator / Factory / Event / State を追加した場合、レビュー本文または対応ログに以下を必ず記載する。

- 追加した型
- 既存の類似概念
- 既存概念との意味差分
- 既存概念で代替しなかった理由
- 将来統合・削除される条件

このゲートの目的は、新規型の追加を禁止することではない。新しい型が必要な場合は、既存の形に無理に寄せず、概念として独立している理由を説明する。

### 判定基準

新規概念を許可する条件:

- 既存型と所有者、寿命、更新契機、利用者、含むデータのいずれかが意味上異なる
- 既存型で代替すると、呼び出し元が追加検索、再解決、状態推測、consume 競合を持つ
- 新規型の名前が表す概念が、呼び出し元都合ではなく Application / Domain / View 境界上の安定した責務である
- 将来、より大きい共通概念へ統合される条件を説明できる

新規概念を不許可にする条件:

- 既存型と所有者、寿命、更新契機、利用者、含むデータが同じで、名前だけが違う
- 既存概念との差分を「便利」「扱いやすい」「今だけ必要」以外で説明できない
- テスト、暫定実装、局所的な呼び出し元都合だけで Runtime の public / internal surface を増やしている
- 既存概念を確認せず、完全一致がないことだけを理由に新規型を追加している

禁止する判断:

- 「既存パターンと違う」だけで不要と判断する
- 「新しい形だから」だけで削除・統合する
- 「既存に完全一致がない」だけで新規型を正当化する
- 名前、戻り値、形が似ている / 違うだけで統合可否を判断する
- 用途、所有者、寿命、更新契機、含むデータを比較せずに削除・統合する

### 記載例

```md
新規概念追加ゲート:

追加した型:
- ActorViewDataChangeSet

既存の類似概念:
- ActorViewData
- WorldMapLayerViewData
- ActorSpatialIndexService の dirty consume 経路

意味差分:
- ActorViewData は単一 Actor の表示スナップショット
- ActorViewDataChangeSet は 1 回の View 更新で反映する Actor 表示差分
- ActorSpatialIndexService の dirty id は戦闘検出用で、表示 DTO と削除通知を持たない

代替しなかった理由:
- ActorViewData の List だけでは removed actor を表現できない
- dirty id のみでは Presenter が position / behavior type を再解決する必要がある
- spatial index の dirty は consume 競合が起きるため共有できない

統合・削除条件:
- Actor / Item / Projectile などの View diff が共通 interface に統合された場合
- View 差分通知の共通型が導入された場合
```

## 統合レビューの横断確認観点

以下は専任エージェントごとの担当軸ではない。
guideline ごとの専任レビューを一次レビューとして完了した後、統合レビュー担当が抜け漏れを確認するために使う。
この観点ごとに別エージェントを立てたり、専任レビュー内で追加の分類軸を作ったりしない。

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
- 今回のレビュー指摘のうち、他 task / 他プロジェクトでも再利用できる判断基準を guideline に抽象化できないか

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

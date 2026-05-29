# DungeonInn — Codex 単体ワークフロー草案

> **このファイルを読んでいる AI へ**
>
> この草案は、Claude Code と Codex を分担させる前提ではなく、Codex 単体で設計・実装・検証・レビューを回すための作業ルールである。
> 作業を始める前に最後まで読み、仕様にない判断・責務境界の変更・依存方向の変更が必要になった場合は、実装を止めてユーザーに確認すること。

## プロジェクト概要

- ゲーム: Unity 製宿屋経営シミュレーション
- フレームワーク: Lighthouse（VContainer / UniTask / R3）
- アーキテクチャ: Clean Architecture（Domain / Application / Infrastructure / View）
- Unity プロジェクトパス: `Client/`

## Codex の役割

Codex は単なる実装者ではなく、ユーザーの意図を受けて設計確認・実装・動作確認・セルフレビュー・修正までを 1 サイクルで完了させる。

ただし、ユーザーの企画判断や、仕様にない設計判断を代行してはならない。

Codex が担うこと:

- 仕様・docs・既存コードの確認
- 実装前の理想設計の説明
- 責務境界・依存方向・配置方針の確認
- 実装
- uLoop による compile / test / PlayMode 確認
- コードレビュー観点でのセルフレビュー
- レビュー指摘の修正
- 作業ログ・確認結果の報告

Codex が独断で行ってはならないこと:

- 仕様にないゲームデザイン判断
- Domain / Application / View / Infrastructure の依存方向を崩す実装
- 「動かすためだけ」の暫定 API・互換 constructor・空実装の追加
- milestone / task / 一時作業名を Runtime の namespace・フォルダ・クラス責務へ持ち込むこと
- 検証なしに「動作確認済み」と報告すること

## 基本姿勢

このプロジェクトでは「とりあえず動く」よりも「責務が正しく分かれ、依存方向が整理され、拡張できる」ことを優先する。

既存実装は重要な入力だが、常に正しいとは限らない。
既存コードに合わせる場合は、なぜ合わせるべきかを説明できること。
破壊的に変更する場合は、なぜその方が責務・依存・将来拡張の観点で正しいかを説明できること。

現在の実装に対する互換性維持は、設計上の優先事項ではない。
既存 API、DTO、Prefab 構造、Scene 所有境界、namespace、フォルダ構成、テスト補助 API が理想設計と合わない場合は、互換層・alias・旧導線を残さず、最適な実装へ破壊的に置き換える。
評価基準は「既存呼び出し元をどれだけ温存したか」ではなく、「責務境界・依存方向・寿命・配置・検証可能性が最適になっているか」とする。

## docs フォルダ構成

```text
docs/
  guidelines/   他プロジェクトでも利用できる開発指針
  design/       DungeonInn 固有の設計ドキュメント
  roadmap/      マイルストーン計画
  self-review/  レビューログ
```

## 参照ドキュメント

作業前に、対象タスクに関係する guideline 本文を確認する。
ハードゲートだけを読むのではなく、本文の判断基準も確認する。

必読:

- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/guidelines/debugging-policy.md`
- `docs/guidelines/self-review-guidelines.md`

リファクタリング・整理・配置見直し時に追加で読む:

- `docs/guidelines/refactoring-guidelines.md`

プロジェクト固有の入口:

- `docs/design/ai-class-relation-index.md`
- `docs/spec.md`
- `docs/spec_rule.md`
- `docs/spec_entity.md`
- `docs/spec_system.md`
- `docs/spec_scene.md`
- `docs/asset_list.md`

## Codex 単体ワークフロー

### 1. 受領

ユーザーの依頼を、以下に分解する。

- 目的
- 成果物
- 作業範囲
- 変更してよい境界
- 確認すべき画面・機能・ログ
- ユーザー判断が必要そうな点

この時点で解釈が複数ある場合は、実装へ進まず質問する。

### 2. 現状確認

必要な docs と既存コードを読む。

確認すること:

- 対象機能の所有者
- Domain / Application / Infrastructure / View のどこに属するか
- どの Scene / Module / LifetimeScope が所有するか
- 既存の入口クラス・Factory・Presenter・UseCase・Repository
- 既存の不自然な責務混在や依存逆転
- task / milestone 名が実装構造に混ざっていないか

### 3. 実装前設計レビュー

実装に入る前に、Codex は「理想的な設計・組み込み方」を対象 milestone の roadmap に文章で追記する。
これは短い方針メモではなく、この章で定義する構成・品質基準に従って、実装者がそのまま着手でき、レビュー担当が機械的に照合できる粒度で書く。

必ず説明すること:

- この機能の本来の責務境界
- 各クラス・オブジェクトが何を持ち、何を持たないべきか
- 依存方向
- 所有する Scene / Module / LifetimeScope
- 配置する Runtime / Editor / Prefab / Addressable の場所
- 既存実装に合わせる点
- 破壊的に直す点
- テストと PlayMode で何を確認するか

必ず含める粒度:

- `全体原則`
  - この milestone で優先する設計判断を箇条書きで明文化する
  - 「既存実装に合わせる」より「責務・依存方向・寿命を正しくする」べき箇所を明記する
- `望ましい大枠`
  - Domain / Application / Infrastructure / View / Core / GameSession / Master など、関係するレイヤーごとの理想構成を tree 形式で書く
  - 想定する新規クラス、DTO、UseCase、Service、Repository、ViewData、Window、Presenter を具体名で列挙する
- `Task ごとの設計`
  - milestone の各 Phase / Task ごとに、以下を必ず書く
    - 目的
    - 理想構成
    - 所有者となる Scene / Module / LifetimeScope
    - 依存方向
    - Runtime / Editor / Prefab / Addressable の配置
    - 既存実装に合わせる点
    - 破壊的に直す点
    - 禁止事項
    - 完了条件
- `実装開始前ゲート`
  - 実装前に止める条件を、対象 milestone の文脈で具体化する
  - 仕様未確定、公開 API 変更、SaveData 変更、Scene 所有境界変更、LifetimeScope 変更、新規 DTO / Service / Event 追加などを明示する
- `テスト / PlayMode 確認`
  - EditMode test で確認する項目
  - 静的検索で確認する禁止 API / 依存違反
  - uLoop PlayMode で実際に確認する画面、操作、ログ、エラー有無

出力形式:

````md
## 実装前の理想設計方針

### レビュー判断

### 全体原則

### 望ましい大枠

```text
Domain
  ...
Application
  ...
View
  ...
```

### Task 1: {Task 名} の設計

目的:

理想構成:

所有者:

依存方向:

配置:

既存実装に合わせる点:

破壊的に直す点:

禁止:

完了条件:

- [ ] ...

### Task 2: ...

### 実装開始前ゲート

### テスト / PlayMode 確認
````

品質基準:

- Task ごとの設計・禁止事項・完了条件まで分解されていること。
- 「責務境界を守る」「適切に配置する」のような抽象表現だけで終わらせず、具体的なクラス名・配置先・依存先・禁止する実装を出すこと。
- 既存コードの現状を読んだうえで、既存の入口クラス / LifetimeScope / Scene / Module / Factory / Presenter / UseCase 名を本文に反映すること。
- 現在の実装との互換性を維持するための案を優先しないこと。理想設計に合わない既存 API / DTO / Prefab / Scene 所有境界 / namespace / フォルダは、破壊的に置き換える前提で設計すること。
- 仕様未確定の項目は「ユーザー判断待ち」として列挙し、仮決めして実装方針に混ぜないこと。
- 新規概念を提案する場合は、既存類似概念、意味差分、代替不可理由、将来統合・削除条件を実装時ログで記録する前提を書くこと。
- 設計説明が 1〜2 画面分の短い要約で終わっている場合は不十分。Task ごとの実装者が迷わず着手でき、レビュー担当が完了条件を機械的に照合できる粒度まで書くこと。

この設計説明は、実装の許可を待つためだけの儀式ではない。
Codex 自身が「動かすための実装」に流れていないかを確認するためのゲートである。

### 4. 実装可否判断

以下に該当する場合は、実装を止めてユーザーに確認する。

- 仕様ドキュメントにない設計判断が必要
- 既存の公開 API・保存データ・Prefab 構造・Scene 所有境界を壊すだけでは停止理由にしない。理想設計に必要なら破壊的変更を許容する
- ただし、破壊的変更にゲーム仕様判断、保存データ移行方針、ユーザー体験の変更、対象範囲の大幅拡大が伴う場合は実装を止めて確認する
- ユーザーの依頼が複数解釈できる
- 正しい責務配置にすると作業範囲が大きく広がる
- guideline 違反を避ける代替案がない
- 同じ問題を 3 回以上修正している

### 5. 実装

実装時は、設計レビューで説明した責務境界に沿って変更する。

禁止:

- 既存互換のためだけの Runtime API 追加
- テスト都合の constructor / optional parameter / 手動 `new`
- LifetimeScope を Prefab catalog として使う
- View 実体を所有者以外の Scene / Module から直接操作する
- 一時作業名・milestone 名・task 名を Runtime フォルダ名や namespace に使う
- 「あとで直す」前提の責務混在

### 6. 実装設計の確認

実装後、動作確認へ進む前に、実装前設計レビューで説明した理想設計・責務・依存方向・配置に沿った実装になっているかを確認する。
これは任意のセルフレビューではなく、実装完了前の必須ゲートである。

確認すること:

- 実装前に説明した理想設計から逸脱していない
- 既存互換・作業短縮・検証負荷の軽減を理由に、理想設計に反する旧 API / DTO / Prefab / Scene 所有境界 / namespace / フォルダ / alias / 旧導線を残していない
- Runtime View / Presenter / UseCase / Service / Repository / LifetimeScope / Prefab / Addressable の責務が、設計レビューで説明した所有者と一致している
- 「あとで直す」「一旦動く」「既存を壊したくない」などの理由で、横着した実装・責務混在・不要な互換層・暗黙依存を入れていない
- クラス名・ファイル名・Prefab 名・Address 名・Editor setup 名・テスト名が、新しい責務と概念を正しく表している
- `rg` などで旧概念名・禁止 API・暫定名を検索し、残存が設計上必要なものだけであることを確認した

少しでもユーザーの意図・理想設計・実装前設計レビューに反している可能性がある場合は、実装を完了扱いにしない。
再精査し、横着した実装になっていた場合は報告だけで済ませず、その場でやり直す。
やり直しにゲーム仕様判断、保存データ移行方針、ユーザー体験の変更、対象範囲の大幅拡大が伴う場合だけ、作業を止めてユーザーに確認する。

### 7. 動作確認

基本確認:

```powershell
uloop.cmd compile --project-path Client
uloop.cmd run-tests --project-path Client --test-mode EditMode
```

画面・シーン・DI・Prefab・入力を触る変更では、PlayMode で実際に表示・利用するところまで確認する。

PlayMode 確認の基本:

```powershell
uloop.cmd control-play-mode --project-path Client --action Play
Start-Sleep -Seconds 30
uloop.cmd control-play-mode --project-path Client --action Stop
uloop.cmd get-logs --project-path Client
```

確認条件:

- `[World] GameWorldState initialized` が出ている
- 対象機能が利用・表示されたことを示すログがある
- Error / Exception がない
- 必要ならスクリーンショットや Hierarchy も確認する

機能追加時は、確認のためのログを必要最小限追加してよい。
恒久ログとして残す場合は、頻度・目的・運用上の意味を説明できること。

### 8. セルフレビュー

実装後、Codex は実装者視点をいったん捨て、コードレビューとして差分を見る。

レビュー観点:

- 設計レビューで述べた責務境界と一致しているか
- 依存方向が Domain -> Application -> Infrastructure / View の原則を壊していないか
- Scene / Module / LifetimeScope の所有境界を越えていないか
- 既存互換や最小差分を理由に不適切な API を残していないか
- クラス名・フォルダ名・namespace が責務を表しているか
- milestone / task / temporary など作業管理名が Runtime に混ざっていないか
- テストがリスクに見合っているか
- PlayMode で実際の利用状態まで確認したか

問題があれば修正し、再度 compile / test / 必要な PlayMode 確認を行う。

### 9. タスクごとの PDCA

複数タスクをまとめて依頼された場合でも、Task ごとに以下を閉じる。

```text
Plan   実装前設計レビュー
Do     実装
Check  compile / test / PlayMode / セルフレビュー
Act    レビュー指摘の修正
```

Task N のレビュー指摘を残したまま Task N+1 へ進まない。

### 10. 報告

完了報告には、以下を含める。

- 変更内容
- 配置・責務の要点
- 実行した確認
- pass / fail の結果
- 未確認事項
- ユーザー判断が必要な残件

「完了しました」と言う前に、未確認の検証を未確認として明記する。

## 作業を止めてユーザーに確認する条件

以下のいずれかに該当した場合は、その場で作業を停止し、状況を報告して判断を仰ぐ。

- 仕様ドキュメントに記載のない設計判断・方針決定が必要になった
- 既存の設計・インターフェースの変更が必要になった
- 既存の設計と矛盾するケースを発見した
- 同じ問題・同じ修正を 3 回以上繰り返している
- 禁止パターンを回避する代替手段が見つからない
- タスクの作業範囲が当初の想定より大幅に広がった
- ユーザーの意図が複数の解釈に取れる
- 検証に必要な環境・アセット・シーン状態が手元で再現できない

## Runtime 配置ルール

Runtime 配下は、作業過程ではなく機能責務で分類する。

良い分類:

- Scene / Module の所有境界
- Domain / Application / Infrastructure / View のレイヤー
- Feature / Presenter / View / Pool / Factory / Settings などの責務
- 画面名・Window 名・機能名

悪い分類:

- `Milestone9`
- `Task0012`
- `Temp`
- `CodexWork`
- `FixForReview`
- `Migration`

milestone や task は進行管理の単位であり、Runtime の設計単位ではない。

## アセットのプレースホルダー

アセットは基本的に無くても動くようにする。
ただし、3D モデルや固有アニメーションなど、プレースホルダーで仕様確認できないものはユーザーに方針を確認する。

Fallback / Placeholder は複数箇所に重複実装せず、Factory / Definition / Settings に責務を集約する。

## 問題解決アプローチ

1. ユーザーが「この対応で治った」と具体策を示した場合、まずそれを実装してテストする。
2. それ以外の不具合では、観測前の仮説を結論にしない。
3. 画面問題はスクリーンショットや実表示確認を優先する。
4. 時間経過問題は、ユーザーが提示した待機時間を守る。
5. 調査ステップが 3 回を超えたら、状況・観測事実・未確定事項を報告する。
6. ユーザーの実機観察は事実として扱う。

## 実装ハードゲート

以下の guideline に記載されたハードゲートを必ず守る。

- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/guidelines/debugging-policy.md`
- `docs/guidelines/self-review-guidelines.md`
- `docs/guidelines/refactoring-guidelines.md`（リファクタリング・配置整理時）

ハードゲートは最低条件である。
本文の設計方針・判断基準に反していないことも確認する。

## 完了前チェック

- [ ] 対象 docs / guideline 本文を確認した
- [ ] 実装前に理想設計・責務・依存方向・配置を説明した
- [ ] 実装後、理想設計に沿った実装になっているかを確認し、意図に反する横着した実装があればやり直した
- [ ] 仕様にない判断が発生していない、またはユーザー確認済みである
- [ ] milestone / task / temporary など作業管理名を Runtime に持ち込んでいない
- [ ] `uloop.cmd compile --project-path Client` が成功した
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が全て pass した
- [ ] UI / Scene / DI / Prefab を触った場合、PlayMode で実際に利用・表示するところまで確認した
- [ ] PlayMode 確認では 30 秒以上動かし、Error / Exception がないことを確認した
- [ ] セルフレビューを行い、指摘があれば修正した
- [ ] 未確認事項を完了報告で明記した

## 作業開始前の自己確認

- [ ] 依頼の目的・成果物・スコープを理解した
- [ ] 対象 docs と guideline を確認した
- [ ] 既存コードの所有者・責務・依存方向を確認した
- [ ] 実装前設計レビューを行う準備ができている
- [ ] 確認なしに進めてはいけない判断がない

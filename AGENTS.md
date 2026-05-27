# DungeonInn — プロジェクト共通情報

> **このファイルを読んでいる AI へ**
>
> 作業を始める前に、このファイルを最後まで読むこと。
> 迷いが生じた場合・仕様に記載のない判断が必要になった場合は、**実装を止めてユーザーに確認する**。
> 確認なしに独自判断で進めることは禁止する。
> ファイル末尾に「作業開始前の自己確認リスト」があるので、作業開始前に必ず確認すること。

このファイルは Claude Code と Codex の両方が参照する共通情報。

## プロジェクト概要

- ゲーム: Unity 製宿屋経営シミュレーション
- フレームワーク: Lighthouse（VContainer / UniTask / R3）
- アーキテクチャ: Clean Architecture（Domain / Application / Infrastructure / View）
- Unityプロジェクトパス: `Client/`

## 作業手順について

- このプロジェクトでは下記に記載されている作業フローで作業を行う
- ユーザーはプロジェクト全体の管理を行うため、任意のタイミングで任意の作業をClaudeCodeやCodexに依頼出来るとする


## 役割分担

| 担当 | 役割 |
|---|---|
| **ユーザー** |ゲームの企画、問題が発生した時の方針決定|
| **Claude Code** | 設計（仕様整理、責務設計、依存方向、公開インターフェース）、レビュー |
| **Codex** | 実装（実装詳細、メソッド本体、Unity API 利用、コンパイル修正、局所的なリファクタ 等） |

- ユーザールール
  - ユーザーはClaude Codeに対してゲームの企画プロンプトを渡す
  - ユーザーはClaude Codeから上がってきた質問やアラート、ヘルプに対して応える
- Claude Codeルール
  - ユーザーから受け取った企画書・指示に少しでも曖昧な点・矛盾している点・一般的ではないパターンがあった場合はまとめてユーザーに確認する
  - Claude Code はコードレビュー前にコード実装の詳細を想定せず、外部契約・責務・依存などを定義し、具体的な実装はCodexに任せる
  - Claude Code はClaude CodeとCodexで方針がぶつかったり、同じ問題を繰り返し作業し続けるようであればユーザーにアラート・ヘルプを求め作業を中断する
  - Claude Code はCodexから報告された問題の原因を調査し、その場で判明しない場合はユーザーにアラート・ヘルプを求め作業を中断する
  - Claude Code はuLoopコマンドを利用してUnityの動作確認が出来る
- Codexルール
  - Codex はClaude Code から指示された命令・方針が適切かどうか判断する。判断に必要なコンテクストが足りなければClaude Codeに意図を確認する
  - Codex は詰まっても禁止パターンに逃げず、`review/{task_id}_question.md` で Claude Code に確認をする
  - Codex はただ実装するのではなく、タスクの作業前にインターフェース設計が適切かレビューし、問題があればClaude Codeに確認する
  - Codex はuLoopコマンドを利用してUnityの動作確認が出来る

## docs フォルダ構成

```
docs/
  guidelines/   ← 他プロジェクトでも利用できる開発指針（必読）
  design/       ← DungeonInn 固有の設計ドキュメント
  roadmap/      ← マイルストーン計画
  self-review/  ← レビューログ
```

## 参照ドキュメント（実装前に必ず確認）

### 開発指針（他プロジェクト共通・必読）

| ファイル | 内容 |
|---|---|
| `docs/guidelines/lighthouse-patterns.md` | Lighthouse ルール・禁止事項・実装パターン集 |
| `docs/guidelines/coding-rules.md` | C# コーディング規約 |
| `docs/guidelines/domain-design-guidelines.md` | Domain / Entity / Calculator / DTO の設計判断基準 |
| `docs/guidelines/application-boundary-guidelines.md` | UseCase / Service / Event / Aggregate / ゲームループの境界ルール |
| `docs/guidelines/implementation-quality-guidelines.md` | 命名・DI・テスト・定数・TODO 管理の判断基準 |
| `docs/guidelines/debugging-policy.md` | 不具合調査・ログ計測・再現確認・一時診断コードの扱い |
| `docs/guidelines/self-review-guidelines.md` | マイルストーン完了レビュー・全体レビューの標準プロンプトと出力ルール |

### リファクタリング依頼時に参照するドキュメント

`docs/guidelines/refactoring-guidelines.md` は、ユーザーが明示的にリファクタリング、整理、構造見直し、定数・ScriptableObject・Master・Prefab 配置・フォルダ分けの見直しを求めた場合に参照する。

このガイドラインは「通常の実装タスクやレビュー完了後に毎回リファクタリングを実行する」ためのものではない。  
任意のタイミングでユーザーがリファクタリングを依頼したときに、後から整理するための判断基準として提示・適用する。

| ファイル | 内容 |
|---|---|
| `docs/guidelines/refactoring-guidelines.md` | 後から整理するための判断基準。`GameConstants` / ScriptableObject / Settings class / Master / Spec / Addressable / LifetimeScope / フォルダ分けの使い分け |

### プロジェクト固有ドキュメント

| ファイル | 内容 | 生成タイミング |
|---|---|---|
| `/AGENTS.md` | プロジェクト共通情報・役割分担・タスクフロー | 最初から |
| `docs/design/ai-class-relation-index.md` | AI が検索前にプロジェクト全体像・機能別の入口クラス・主要な関係を把握するための索引 | プロジェクト進行中に随時更新 |
| `docs/spec_prompt.md` | ユーザーからのゲーム仕様となる初期プロンプト | プロジェクトフェーズ1 |
| `docs/spec.md` | 企画書 | プロジェクトフェーズ1 |
| `docs/spec_rule.md` | ゲームルールに関して詳細に記載したドキュメント | プロジェクトフェーズ2 |
| `docs/spec_entity.md` | エンティティに関して詳細に記載したドキュメント | プロジェクトフェーズ2 |
| `docs/spec_system.md` | システムに関して詳細に記載したドキュメント | プロジェクトフェーズ2 |
| `docs/spec_scene.md` | 画面に関して詳細に記載したドキュメント | プロジェクトフェーズ2 |
| `docs/asset_list.md` | 画像/サウンド/3Dモデル/エフェクトなどのアセット一覧 | プロジェクトフェーズ2 |


## 作業の監視

Claude CodeとCodexのリクエスト・レスポンスは全て.txtに記載する。
**`claude-codex-communication.log` へのストリームは必須。** ユーザーが `tail -f` でリアルタイム監視している。

```bash
echo "=== [$(date '+%Y-%m-%d %H:%M')] Claude→Codex: task_XXX ===" >> claude-codex-communication.log
codex exec --dangerously-bypass-approvals-and-sandbox -s danger-full-access - < tasks/task_XXX.md >> claude-codex-communication.log 2>&1 &
```

## ワークフロー

### プロジェクトフェーズ1: 企画書の作成

1. ユーザーはClaudeCodeに対して企画となるプロンプトを送信する。
2. ClaudeCodeは`docs/spec_prompt.md`に内容を保存する。
3. ClaudeCodeはAAAタイトルのプロのゲームデベロッパーとして的確なレビューとFBを行う。
4. 企画の内容が固まり、ゲームの全体像・設計が見えるまでこれを繰り返す。特に2D/3D/UIの見え方は部分は必ず確認する。
5. 曖昧な点・明文化されていない点が0になったらClaudeCodeは企画書を作成する
  - `docs/spec.md` に保存する
6. 完了

### プロジェクトフェーズ2: 要素の定義

1. ClaudeCodeは企画書の内容から要素を抜き出して詳細な振る舞いを記載する（=要素から仕様を逆引きする資料の作成）
  - ゲーム全体のルール：初期状態、ゲームの勝利条件
    - `docs/spec_rule.md` に保存する
  - ゲームのエンティティ：キャラクター、モンスター、アイテム
    - `docs/spec_entity.md` に保存する
  - ゲームシステム：戦闘システム、お金システム
    - `docs/spec_system.md` に保存する
  - 画面：画面設計、画面一覧、画面ごとのUI要素
    - `docs/spec_scene.md` に保存する
2. ユーザーはその資料を確認してレビューとFBを行う
3. ClaudeCodeは企画書の内容と矛盾点がないか確認する
4. ClaudeCodeはゲームに必要なアセット(画像/サウンド/3Dモデル/エフェクトなどのルック)を整理し、一覧化する
  - サイズや再生時間の規模感と、準備優先度を添えたリスト形式
  - 3Dモデルの場合は利用するアニメーションやポーズも記載する
5. ClaudeCodeはユーザーにアセットの準備を依頼する
  - ただし、用意に時間がかかるため、フェーズ自体は先に進める

### プロジェクトフェーズ3: Taskの作成

1. ユーザーはマイルストーンを設定し、このマイルストーンでどこまで実装を進めるか定める。これはまずClaudeCodeから適切だと思われる範囲を提案する
2. ClaudeCodeは企画書の内容とマイルストーンの範囲を実現するために作業を分解し、Taskファイルを作成・記載する
  - フォーマットは後述
3. ユーザーはTaskの内容をレビューする。実装にあたりTaskの分解の粒度や項目の内容を確認する。必要があれば更に分解もしくは統合を行う
4. CodexはTaskの内容をレビューする。実装にあたりTaskの分解の粒度や項目の内容を確認する。必要があれば粒度の確認と不明点の質問を行う
  - Codexへの指示はClaudeCodeが行う
5. ClaudeCodeはレビュー内容を確認し、必要があれば修正を行う
  - ただしレビュー内容をそのまま受け入れるのではなく、ClaudeCodeの判断が適切だと思われれば意図を提示して再度レビューを依頼する

- Taskファイル
  - プロジェクトの進捗に必要なTaskの1つを管理するファイル。タスクに関して作業を行う時Claude CodeとCodexそれぞれが最初に参照する
  - ファイル名は `tasks/task_{0000:task_index}.md`
  - ファイルの内容
    - タスクの状態の記載(ClaudeCodeが記載)
      - タスクの状態遷移は 設計待ち -> 設計レビュー待ち -> 実装待ち -> 実装レビュー待ち -> 完了 の5段階とする
    - タスクの目的の記載(ClaudeCodeが記載)
    - 利用するLighthouseパターンの記載（ClaudeCodeが記載）
      - 実装に必要なLighthouseパターンを `docs/guidelines/lighthouse-patterns.md` のラベル（P1〜P10）で列挙する
      - 例: `[P5] アセット非同期ロード（IAssetScope）`, `[P6] Config Repository`, `[P8] LifetimeScope`
      - コードの記載は不要。パターン名と用途を端的に記載
      - Codexはここに列挙されたパターンを実装前に必ず `docs/guidelines/lighthouse-patterns.md` で確認すること
    - 作業ログの記載（ClaudeCode, Codexが記載）
    - レビューログの記載（ClaudeCodeが記載）

タスクの作成・管理ルール
- タスクの状態の初期値はファイルが作られた時に「設計待ち」とする
- タスクは適切なタイミングで全体レビュータスクを挿入する
  - （例）キャラクター関連のタスクの完了し、次にアイテム関連のタスクが控えている場合は、そこにキャラクター関連の全体レビュータスクを挿入する
- タスクの作業ログやレビューログは追記で行い、削除や上書きは行わない
  - また、タスクの変更があった場合も作業ログに仕様変更のBefore/Afterを記載して、タスクの内容そのものを更新する

### プロジェクトフェーズ4: Taskの実行

1. ClaudeCodeは現在のタスクを実行する
  - もしそのタスクがユーザー判断を伴うものであれば、ユーザーに確認をする
  - もしそのタスクが実装タスクであれば以下の順番で作業を進める
    - ClaudeCodeが外部契約・責務・依存方向の定義などを行い、Taskファイルに記載する
      - タスクの状態は「設計レビュー待ち」にする
    - CodexはTaskファイルに記載された内容をレビューし、問題なければ内部実装・メソッド構成・具体的な Unity/API 呼び出しを含む実装を開始する
      - タスクの状態を「実装待ち」にしてから実装を開始する
    - Codex が設計矛盾・禁止パターン・依存方向違反・仕様不明・コンパイル不能を予見した場合は、実装を停止してTaskファイルに追記し、ClaudeCodeに返す
      - ClaudeCode はそれを受けて実装方針を確認し、妥当性があれば修正する
        - ただし、このやりとりが3回を超えたら実装を中断し、ユーザーに判断を確認する
    - Codexは実装完了後に必ず動作確認する。 `uloop.cmd compile --project-path Client` を基本必須
      - 失敗した場合はエラーログを確認し原因を調査して対応を行う
2. ClaudeCodeはタスクの動作確認を行う
  - ClaudeCodeは動作確認が必要なタスクであれば動作確認を行う
  - ClaudeCodeは必要に応じてuloopコマンドで画面/統合確認を行う
  - 完了後タスクの状態は「実装レビュー待ち」にする
3. ClaudeCodeは実装のレビューを行う
  - ClaudeCodeは実装のレビューが必要なタスクであればレビューを行う
  - レビューした結果、問題が見つかればTaskファイルに追記し、Codexにレビュー内容を確認させ、修正もしくは反論させる
  - 問題なく実装が完了したらタスクの状態は「完了」にする

### プロジェクトフェーズ5: 実装の全体レビュー

1. ユーザーはタスクが全て完了した後、ゲーム全体が動くかどうか・マイルストーンで定めた目標を達成しているか確認する
  - 問題があればClaudeにフィードバック・指摘を行う
  - 問題が無ければプロジェクトフェーズ3に戻り、次のマイルストーンを定める

## 作業を止めてユーザーに確認する条件

以下のいずれかに該当した場合、**その場で作業を停止し**、ユーザーに状況を報告して判断を仰ぐ。
「たぶん大丈夫」と自己判断して続行しないこと。

- 仕様ドキュメントに記載のない設計判断・方針決定が必要になった
- 既存の設計・インターフェースの変更（追加・削除・リネーム）が必要になった
- 既存の設計と矛盾するケースを発見した
- 同じ問題・同じ修正を 3 回以上繰り返している
- 禁止パターン（実装ハードゲート参照）を回避するための代替手段が見つからない
- タスクの作業範囲が当初の想定より大幅に広がっていると気づいた
- ユーザーの意図が複数の解釈に取れる

## アセットのプレースホルダー

アセットは基本的に無くても動くようにする。3Dモデルなどプレースホルダーではどうにも出来ない場合はユーザーに方針を確認する

## 問題解決アプローチ

1. ユーザーが「〇〇したら治った」→ まずそれを実装してテスト（調査より実証優先）
2. スクリーンショットを先に取り、視覚的現状を確認してからコードを読む
3. 調査ステップが 3 回を超えたらユーザーに報告して確認する
4. ユーザーの実機観察（「Hierarchy に見える」等）はそのまま事実として扱う

## ClaudeCode/Codex必須条件

- Lighthouseを利用してゲームを開発すること
  - 実装時、レビュー時に `docs/guidelines/lighthouse-patterns.md` を参照し、違反していた場合は必ず修正する
- uLoopを利用して動作確認すること
- コーディングルールを必ず守ること
  - 実装時、レビュー時に `docs/guidelines/coding-rules.md` を参照すること
- 実装品質ルールを必ず守ること
  - 実装時、レビュー時に `docs/guidelines/implementation-quality-guidelines.md` を参照すること
- UseCase / Event / Aggregate 境界ルールを必ず守ること
  - 次回以降の新規実装では `docs/guidelines/application-boundary-guidelines.md` を参照すること
  - UseCase のステートレス性、UseCase 間呼び出し、イベント購読による状態変更、Aggregate 境界、Publisher/Subscriber 分離方針を確認すること
  - 既存コードに違反がある場合は作業範囲内で悪化させず、必要に応じてリファクタタスクとして扱うこと

## 実装ハードゲート

以下の guideline に記載されたハードゲートを必ず守ること。

- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/guidelines/debugging-policy.md`
- `docs/guidelines/self-review-guidelines.md`

- 既存互換維持だけを理由に、理想設計上不要な旧実装・旧API・旧Scene/Prefab要素を残すことは禁止する。

ハードゲート違反を発見した場合、Codex は実装を停止して `review/{task_id}_question.md` に報告する。

ハードゲートは即時停止・修正が必要な最低条件であり、本文の設計方針・判断基準を省略してよいという意味ではない。
実装・レビュー時は、ハードゲートだけでなく該当 guideline 本文の方針に反していないことを確認する。

## Codex 完了前チェック

> **以下を全てチェックするまで「完了しました」と報告してはならない。**
> チェックできていない項目が 1 つでもあれば、作業を継続するか、Claude Code に確認を取ること。

- [ ] `uloop.cmd compile --project-path Client` が成功した
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が全て pass した
- [ ] **[ハードゲート] 以下のコマンドで 30 秒間 Unity を Play モードで動作させた。`[World] GameWorldState initialized` のログが出力され、エラーログが存在しないことを確認した**
  ```
  uloop.cmd control-play-mode --project-path Client --action Play
  （30秒待機）
  uloop.cmd control-play-mode --project-path Client --action Stop
  uloop.cmd get-logs --project-path Client
  ```
- [ ] `docs/guidelines/` 配下の guideline 本文を確認した
- [ ] 各 guideline のハードゲートに違反していない
- [ ] 各 guideline の完了前チェックリストを確認した
- [ ] ハードゲートだけでなく、本文の設計方針・判断基準に反していないことを確認した
- [ ] 作業ログに上記チェック結果を記載した
- [ ] 完了報告時に、上記チェック結果をユーザーへ提示した

## Claude Code レビュー必須チェック

> **以下を全てチェックするまでタスクを「完了」にしてはならない。**
> Codex の作業ログの自己申告だけを信用せず、必ず自分で差分と guideline 適合性を確認すること。

- [ ] `docs/guidelines/` 配下の guideline 本文を確認した
- [ ] 各 guideline のハードゲートに違反していないことを確認した
- [ ] 各 guideline の完了前チェックリストを確認した
- [ ] ハードゲートだけでなく、本文の設計方針・判断基準に反していないことを確認した
- [ ] コンパイルが通っている（uloop compile または Codex ログで確認）
- [ ] **[ハードゲート] `uloop.cmd control-play-mode --action Play` → 30 秒後 Stop → `uloop.cmd get-logs` で `[World] GameWorldState initialized` ログが出力され、エラーログが存在しないことを確認した**

---

## 作業開始前の自己確認リスト

> **このファイルを読み終えた AI は、作業を始める前に以下を自問すること。**
> 全て「YES」でなければ作業を開始してはならない。

- [ ] `docs/guidelines/` 配下の guideline を全て確認した
- [ ] タスクの目的・スコープを理解した
- [ ] 仕様ドキュメントに記載のない設計判断が発生していないことを確認した
- [ ] 「作業を止めてユーザーに確認する条件」に該当する状況がないことを確認した

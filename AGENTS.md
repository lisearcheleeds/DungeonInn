# DungeonInn — プロジェクト共通情報

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

## 参照ドキュメント（実装前に必ず確認）

| ファイル | 内容 | 生成タイミング |
|---|---|---|
| `/AGENTS.md` | プロジェクト共通情報・役割分担・タスクフロー | 最初から |
| `docs/lighthouse-patterns.md` | Lighthouse ルール・禁止事項・実装パターン集 | 最初から |
| `docs/coding-rules.md` | C# コーディング規約 | 最初から |
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
      - 実装に必要なLighthouseパターンを `docs/lighthouse-patterns.md` のラベル（P1〜P10）で列挙する
      - 例: `[P5] アセット非同期ロード（IAssetScope）`, `[P6] Config Repository`, `[P8] LifetimeScope`
      - コードの記載は不要。パターン名と用途を端的に記載
      - Codexはここに列挙されたパターンを実装前に必ず `docs/lighthouse-patterns.md` で確認すること
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

## アセットのプレースホルダー

アセットは基本的に無くても動くようにする。3Dモデルなどプレースホルダーではどうにも出来ない場合はユーザーに方針を確認する

## 問題解決アプローチ

1. ユーザーが「〇〇したら治った」→ まずそれを実装してテスト（調査より実証優先）
2. スクリーンショットを先に取り、視覚的現状を確認してからコードを読む
3. 調査ステップが 3 回を超えたらユーザーに報告して確認する
4. ユーザーの実機観察（「Hierarchy に見える」等）はそのまま事実として扱う

## ClaudeCode/Codex必須条件

- Lighthouseを利用してゲームを開発すること
  - 実装時、レビュー時にドキュメントを参照し、違反していた場合は必ず修正する
- uLoopを利用して動作確認すること
- コーディングルールを必ず守ること
  - 実装時、レビュー時にドキュメントを参照すること

## 実装ハードゲート

以下を含む実装は禁止。発見した場合、Codex は実装を停止して `review/{task_id}_question.md` に報告する。

- `Addressables.LoadAssetAsync` の直接使用
- `Resources.Load` / `Resource.Load` の使用
- `SceneManager.LoadScene` の直接使用
- `Task` / `ValueTask` の使用
- `UnityEngine.UI.Button` の使用
- Lighthouse / VContainer / 既存フレームワークコードの複製
- DI で解決すべき依存を `new` / static / singleton / 手動検索で生成すること
- 自動生成ファイル `.g.cs` の手動編集

## Codex 完了前チェック

Codex は完了前に以下を確認し、結果を作業ログに記載する。

- `uloop.cmd compile --project-path Client`
- `Addressables.LoadAssetAsync` が追加されていないこと
- `Resources.Load` / `Resource.Load` が追加されていないこと
- `Task` / `ValueTask` が追加されていないこと
- DI 登録が必要なクラスは LifetimeScope / Installer に登録されていること
- LighthouseGenerated 以下を編集していないこと

## Claude Code レビュー必須チェック

Claude Code は実装レビュー時に、Codex の作業ログだけを信用せず、禁止 API 検索と差分確認を行う。
例
```powershell
  rg "Addressables\.LoadAssetAsync|Resources\.Load|Resource\.Load|SceneManager\.LoadScene|UnityEngine\.UI\.Button|Task<|
  ValueTask<|WaitForCompletion|\.Result" Client/Assets
```
# オーケストレーター手順

## Codexへのタスク委託
1. tasks/task_XXX.md を作成
2. `codex -q --approval-mode full-auto "$(cat tasks/task_XXX.md)"` を実行
3. review/{task_XXX}_done.md の生成を待つ
4. 変更ファイルを読みレビューを実施
5. /uloop-compile でコンパイル確認

## Codex 実行コマンド（正しい構文）
```
codex exec --dangerously-bypass-approvals-and-sandbox -s danger-full-access - < tasks/task_XXX.md
```

## レビュー→反論ループ
- レビュー結果を review/task_XXX_review.md に書き出す
- `codex exec --dangerously-bypass-approvals-and-sandbox -s danger-full-access - < review/task_XXX_review.md` で反論を求める
- 最大3往復。超えた場合は「ユーザー判断が必要です」と報告して停止

## フェーズ終了前の動作確認（必須）
各フェーズの全タスク完了後、ユーザーへ報告する前に以下を実施する:

1. uloop でゲームを起動して数秒待機
   ```
   uloop control-play-mode --project-path Client --action play
   ```
2. コンソールログを取得してエラーを確認
   ```
   uloop get-logs --project-path Client
   ```
3. 想定外のエラーがあれば:
   a. ClaudeCode がエラーを分析して原因を特定
   b. fix タスク（task_XXX_fix.md）を作成して Codex に修正させる
   c. 修正後に再度起動確認
   d. エラーなしを確認してからフェーズ完了を報告
4. 起動を停止
   ```
   uloop control-play-mode --project-path Client --action stop
   ```

## タスク順序への異議
- Codexが task_XXX.md を受け取った際、順序に問題があると判断した場合
  → review/task_XXX_order_objection.md を生成
- Claude Codeはこれを読み、妥当なら順序を変更、不当なら却下してユーザーに報告
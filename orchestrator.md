# オーケストレーター手順

## Codexへのタスク委託
1. tasks/task_XXX.md を作成
2. `codex -q --approval-mode full-auto "$(cat tasks/task_XXX.md)"` を実行
3. review/{task_XXX}_done.md の生成を待つ
4. 変更ファイルを読みレビューを実施
5. /uloop-compile でコンパイル確認

## レビュー→反論ループ
- レビュー結果を review/task_XXX_review.md に書き出す
- `codex -q --approval-mode full-auto "$(cat review/task_XXX_review.md)"` で反論を求める
- 最大3往復。超えた場合は「ユーザー判断が必要です」と報告して停止

## タスク順序への異議
- Codexが task_XXX.md を受け取った際、順序に問題があると判断した場合
  → review/task_XXX_order_objection.md を生成
- Claude Codeはこれを読み、妥当なら順序を変更、不当なら却下してユーザーに報告
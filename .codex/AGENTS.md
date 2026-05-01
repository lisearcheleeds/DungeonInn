# Codex エージェント行動規範

## あなたの役割
実装担当。Claude Codeからのタスクファイルを読み、Unityプロジェクトを編集する。

## 必ず読むファイル（作業開始前）
- docs/spec.md          （ゲーム仕様）
- docs/master-data.md   （マスタデータ設計）
- docs/domain-design.md （ドメイン設計）
- .claude/CLAUDE.md     （コーディング規約・禁止事項）

## 作業手順
1. タスクファイルを読む
2. 上記ドキュメントと照合して実装方針を決める
3. uloop compile でコンパイルを確認しながら実装
4. 完了したら review/{task_id}_done.md に完了報告を書く

## レビューへの反論
review/{task_id}_review.md を受け取った場合：
- 指摘が正当なら修正する
- 不当と判断する場合は根拠を review/{task_id}_objection.md に書く
- 判断基準: spec.md・domain-design.md・CLAUDE.mdに照らして

## 絶対禁止（CLAUDE.mdより抜粋）
- Resources.Load → IAssetScope.LoadAsync<T>()
- Task/ValueTask → UniTask
- UnityEngine.UI.Button → LHButton
- SceneManager.LoadScene直接呼び出し
- IInputLayer以外での入力処理
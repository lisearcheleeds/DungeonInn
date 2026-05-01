# DungeonInn プロジェクト固有設定

グローバルの Lighthouse ワークフロー（`~/.claude/CLAUDE.md`）に加えて、このプロジェクト固有の設定を以下に記載する。

---

## プロジェクト情報
- Unityプロジェクトパス: `Client/`
- uloop コマンドには常に `--project-path Client` を付ける

## 作業ログ
`ClaudeCodeWorkingLog.txt`（プロジェクトルート）

---

## 既知の回避策（Lighthouse 制限由来）

### Camera Base depth 問題
`SceneCameraManager` が Base camera の depth を `0` にハードコードしているが、
このプロジェクトでは depth=1 が必要なため `Camera3DDepthWrapper`（ISceneCamera デコレータ）で上書きしている。
→ `GameScene.cs` の `Camera3DDepthWrapper` を参照。

---

## 既知の技術的負債（要修正）

### FreeCamera InputLayer 違反
`FreeCamera.cs` が `Keyboard.current` / `Mouse.current` を直接ポーリングしている。
修正方針: `InputActions.inputactions` に `Game` ActionMap（Move / Elevate / Look）を追加し、
`GameInputLayer : IInputLayer` を実装して `GameScene.CreateInputLayer()` で登録する。
修正タイミング: ユーザーと設計確認後に実施すること。

---

## Phase 4 引き継ぎ（未完了）

### FloorSwitcher ボタンが反応しない
- 有力仮説: HUDCanvas `SortingOrder=0` が ScreenStack Canvas（600 / 1100）より低く入力が横取りされている
- 次回最初に実行する確認コード:
```csharp
var btn = GameObject.Find("Btn_Ground")?.GetComponent<UnityEngine.UI.Button>();
if (btn != null) btn.onClick.Invoke();
else Debug.Log("Btn_Ground not found");
```
  - フロアが切り替わる → 入力の問題（Raycaster / SortingOrder を調査）
  - 切り替わらない → `FloorSwitcherView.Start()` の `WorldRenderer` 取得失敗を疑う

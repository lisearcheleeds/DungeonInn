# PlayMode Automation

PlayMode の基本動作確認では、`#if DEBUG` で有効になる `DungeonInn.Debugging.PlayModeAutomation` を `uloop.cmd execute-dynamic-code` から呼び出す。

この API はテスト・手動確認補助専用で、通常の製品導線には使わない。

## 基本動作確認シナリオ

1. PlayMode を開始する。
   ```powershell
   uloop.cmd control-play-mode --project-path Client --action Play
   ```

2. タイトル画面で NewGame を押す。
   ```powershell
   uloop.cmd execute-dynamic-code --project-path Client --code "DungeonInn.Debugging.PlayModeAutomation.ClickTitleNewGame();"
   ```

3. 5 秒待つ。
   ```powershell
   Start-Sleep -Seconds 5
   ```

4. Seed 値画面で決定ボタンを押す。
   ```powershell
   uloop.cmd execute-dynamic-code --project-path Client --code "DungeonInn.Debugging.PlayModeAutomation.ClickTitleSeedStart();"
   ```

5. 10 秒待つ。
   ```powershell
   Start-Sleep -Seconds 10
   ```

6. ランダムな Actor を選択状態にする。
   ```powershell
   uloop.cmd execute-dynamic-code --project-path Client --code "DungeonInn.Debugging.PlayModeAutomation.SelectRandomActor();"
   ```

7. `[World] GameWorldState initialized` を確認後、必要な秒数 World を動作させる。

8. PlayMode を停止し、ログに Error がないことを確認する。
   ```powershell
   uloop.cmd control-play-mode --project-path Client --action Stop
   uloop.cmd get-logs --project-path Client
   ```

## 注意

- `ClickTitleNewGame` は Title scene の `TitlePresenter.OnEnter` 後に登録される。
- `ClickTitleSeedStart` は Seed パネルの active 状態に依存せず、TitlePresenter の既存開始処理を呼び出す。基本シナリオでは必ず `ClickTitleNewGame` の 5 秒後に呼ぶ。
- `SelectRandomActor` は World scene の automation entry point 登録後に有効になる。Actor 候補が存在しない場合は選択せず、Warning を出して `false` を返す。

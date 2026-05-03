# task_010: Launcher の TransitionNextScene 失敗時に Reboot を呼ぶ

## 問題

`Launcher.TransitionNextScene()` の例外ハンドラがエラーをログ出力するだけで
`Reboot()` を呼ばない。遷移に失敗してもゲームが壊れた状態のまま止まる。

```csharp
// 現在の NG 状態
catch (System.Exception e)
{
    UnityEngine.Debug.LogError($"[Launcher] TransitionScene failed: {e}");
    // Reboot が呼ばれていない
}
```

## 修正対象

```
Client/Assets/DungeonInn/Runtime/Scripts/Core/Launcher.cs
```

## 修正内容

catch ブロックに `Reboot()` を追加する。

```csharp
catch (System.Exception e)
{
    UnityEngine.Debug.LogError($"[Launcher] TransitionScene failed: {e}");
    Reboot();
}
```

**注意**:
- `OperationCanceledException` は Reboot せず再スローする（キャンセルは正常フロー）
- `Reboot()` が無限ループしないよう、既存の `Reboot()` 実装は変更しない

変更後の `TransitionNextScene()` 全体:

```csharp
void TransitionNextScene()
{
    UniTask.Void(async () =>
    {
        try
        {
            await sceneManager.TransitionScene(new WorldScene.WorldTransitionData());

            if (!string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetSceneByName(LauncherSceneName).name))
            {
                await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(LauncherSceneName).ToUniTask();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[Launcher] TransitionScene failed: {e}");
            Reboot();
        }
    });
}
```

## 完了条件

- [ ] `uloop.cmd compile --project-path Client` エラーゼロ
- [ ] `review/task_010_done.md` に完了報告

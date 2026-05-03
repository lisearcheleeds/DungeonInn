# task_004_debug: World シーンが起動後に表示されない問題の調査・修正

## 現状

World.unity と HUD.unity に必要なコンポーネント（WorldScene, LHSceneTransitionAnimatorManager, CanvasGroup, SceneCanvasInitializer）を追加済み。

しかし起動後のランタイム状態を確認すると:

```
[SceneCheck] count=1 scenes=HUD[1]
```

HUD (モジュールシーン) のみロードされており、World (メインシーン) と ScreenStack (モジュールシーン) がロードされていない。

## 既知の情報

- Launcher.cs の `TransitionNextScene()` は `UniTask.Void` を使っているため、例外が発生しても無音で握りつぶされる
- uloop get-logs でエラーが表示されないのはこれが原因の可能性が高い
- Lighthouse の `FindSceneBase()` が World.unity 内で `MainSceneBase` を見つけられない場合、例外を投げて遷移が中断される

## 調査手順

### ステップ 1: 例外を可視化する

`Launcher.cs` の `TransitionNextScene()` を修正して、例外をログに出力するようにする:

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
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[Launcher] TransitionScene failed: {e}");
        }
    });
}
```

### ステップ 2: プレイモードでログ確認

```
uloop control-play-mode --project-path Client --action play
# 10秒待機
uloop get-logs --project-path Client --log-type Error
```

### ステップ 3: エラー内容に応じて修正

エラーが "MainSceneBase NotFound" なら → World.unity の WorldScene コンポーネントが正しく認識されていない

エラーが別のものなら → その内容に応じて対処する

## 修正完了条件

1. `uloop get-logs` でエラーゼロ
2. `uloop execute-dynamic-code` でシーン確認:
   ```csharp
   var count = UnityEngine.SceneManagement.SceneManager.sceneCount;
   var sb = new System.Text.StringBuilder();
   for(int i=0;i<count;i++){var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i); sb.Append(s.name+"["+s.rootCount+"] ");}
   Debug.Log("[SceneCheck] count="+count+" scenes="+sb);
   ```
   結果に `World[2]`（WorldScene + WorldLifetimeScope）と `ScreenStack[1]` と `HUD[1]` が含まれること

3. コンパイルエラーゼロ
4. `review/task_004_debug_done.md` に完了報告（エラーの原因と修正内容を含む）

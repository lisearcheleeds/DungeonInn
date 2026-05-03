# task_004_debug レビュー

## 承認項目

- `VContainerSettings.asset` の RootLifetimeScope を null クリア → 二重起動解消 ✓
- `Launcher.unity` に RootLifetimeScope MonoBehaviour 追加 ✓
- `Launcher.cs` への try/catch 追加 ✓

## 却下・修正要求

### 1. Lighthouse 違反: SceneManager.LoadSceneAsync の直接使用禁止

現在の `TransitionNextScene()` は Unity の `SceneManager.LoadSceneAsync()` を直接呼んでいる。
CLAUDE.md に明記された禁止事項に抵触する:

```
SceneManager.LoadScene / LoadSceneAsync 直接呼び出し禁止
→ ISceneManager.TransitionScene() を使う
```

この実装では Lighthouse の OnLoad / OnEnter / InAnimation / OutAnimation / SceneTransitionDiff が一切動かない。将来のフェーズで致命的な問題になる。

**修正**: `TransitionNextScene()` を `sceneManager.TransitionScene(new WorldScene.WorldTransitionData())` に戻すこと。VContainerSettings の二重起動が解消されたので TransitionScene は正常動作するはず。

### 2. LaunchProcess() が空になっている

Config Repository の Addressables ロードが失除されている。
これを復元すること。

Addressables ロードが失敗していた真の原因は:
- `m_ActivePlayerDataBuilderIndex: 2` (Packed mode) でビルドが未実施
- エディタ開発時は index 0 (Use Asset Database / Fast Mode) に変更する必要がある

**修正**:
1. `AddressableAssetSettings.asset` の `m_ActivePlayerDataBuilderIndex` を `0` に変更
2. `LaunchProcess()` に 5 リポジトリの WhenAll ロードを復元:

```csharp
async UniTask LaunchProcess()
{
    await UniTask.WhenAll(
        worldConfigRepository.LoadAsync(),
        innConfigRepository.LoadAsync(),
        adventurerConfigRepository.LoadAsync(),
        monsterConfigRepository.LoadAsync(),
        dungeonConfigRepository.LoadAsync()
    );
}
```

3. コンストラクタの注入を元に戻す（IWorldConfigRepository 等 5 つを復元）

## 修正後の確認条件

1. コンパイルエラーゼロ
2. uloop get-logs エラーゼロ
3. execute-dynamic-code で `World[2] ScreenStack[1] HUD[1]` 確認
4. `review/task_004_debug_fix_done.md` に完了報告

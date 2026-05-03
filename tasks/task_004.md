# task_004: シーン GameObject セットアップ修正

## 問題
起動時に以下のエラーが発生する:
```
Lighthouse: [SceneGroup] MainSceneBase NotFound
To add a scene, you need to add the scene to UnityEditor
and place a GameObject that inherits MainSceneBase at the root of the added scene.
```

## 原因
task_002 でシーンファイルを作成した際、空の GameObject を置いただけで
WorldScene / HudModuleScene コンポーネントをアタッチしていない。
Lighthouse はシーンロード時にルート GameObject の MainSceneBase / ModuleSceneBase を検索するが、
見つからずエラーになっている。

## 必要な対応

### World.unity の構成
```
[Root] WorldScene        ← WorldScene + LHSceneTransitionAnimatorManager
[Root] WorldLifetimeScope ← WorldLifetimeScope (VContainer LifetimeScope)
```

### HUD.unity の構成
```
[Root] HudModuleScene    ← HudModuleScene + LHSceneTransitionAnimatorManager
```

---

## 作業 1: World.unity のセットアップ

以下の C# コードを `uloop execute-dynamic-code` で実行する:

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DungeonInn.Runtime.Scripts.View.Scene.MainScene.World;
using LighthouseExtends.Animation;
using VContainer.Unity;

// World シーンを開く
var worldScene = EditorSceneManager.OpenScene(
    "Assets/DungeonInn/Runtime/Scene/MainScene/World.unity",
    OpenSceneMode.Single);

// 既存の GameObject を取得（task_002 で "WorldScene" という名前で作成済み）
var rootGOs = worldScene.GetRootGameObjects();
var worldRoot = rootGOs.Length > 0 ? rootGOs[0] : new GameObject("WorldScene");
worldRoot.name = "WorldScene";

// WorldScene コンポーネントをアタッチ（RequireComponent で LHSceneTransitionAnimatorManager も自動追加）
var worldSceneComp = worldRoot.GetComponent<WorldScene>() ?? worldRoot.AddComponent<WorldScene>();

// LHSceneTransitionAnimatorManager の serialized フィールドを設定
var animManager = worldRoot.GetComponent<LHSceneTransitionAnimatorManager>();
var so = new SerializedObject(worldSceneComp);
var prop = so.FindProperty("sceneTransitionAnimatorManager");
prop.objectReferenceValue = animManager;
so.ApplyModifiedProperties();

// WorldLifetimeScope 用 GameObject を追加
var lifetimeScopeGO = new GameObject("WorldLifetimeScope");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lifetimeScopeGO, worldScene);
lifetimeScopeGO.AddComponent<WorldLifetimeScope>();

EditorSceneManager.SaveScene(worldScene);
AssetDatabase.Refresh();
Debug.Log("[task_004] World.unity setup complete.");
```

---

## 作業 2: HUD.unity のセットアップ

以下の C# コードを `uloop execute-dynamic-code` で実行する:

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DungeonInn.Runtime.Scripts.View.Scene.ModuleScene.HUD;
using LighthouseExtends.Animation;

// HUD シーンを開く
var hudScene = EditorSceneManager.OpenScene(
    "Assets/DungeonInn/Runtime/Scene/ModuleScene/HUD.unity",
    OpenSceneMode.Single);

var rootGOs = hudScene.GetRootGameObjects();
var hudRoot = rootGOs.Length > 0 ? rootGOs[0] : new GameObject("HudModuleScene");
hudRoot.name = "HudModuleScene";

// HudModuleScene コンポーネントをアタッチ
var hudSceneComp = hudRoot.GetComponent<HudModuleScene>() ?? hudRoot.AddComponent<HudModuleScene>();

// LHSceneTransitionAnimatorManager の serialized フィールドを設定
var animManager = hudRoot.GetComponent<LHSceneTransitionAnimatorManager>();
var so = new SerializedObject(hudSceneComp);
var prop = so.FindProperty("sceneTransitionAnimatorManager");
prop.objectReferenceValue = animManager;
so.ApplyModifiedProperties();

EditorSceneManager.SaveScene(hudScene);
AssetDatabase.Refresh();
Debug.Log("[task_004] HUD.unity setup complete.");
```

---

## 作業 3: コンパイル確認

`uloop.cmd compile --project-path Client` でエラーゼロを確認する。

---

## 完了条件

1. コンパイルエラーゼロ
2. `review/task_004_done.md` に完了報告（実行ログ・シーン構成確認結果を含む）

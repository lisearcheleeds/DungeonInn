# task_006: SceneGroupProvider KeyNotFoundException バグ修正

## 問題

`SceneGroupProvider.CreateSceneGroup` で Dictionary に存在しないキーに `[]` アクセスすると
`KeyNotFoundException` が発生する。`??` は Dictionary アクセス失敗時に効かない。

```csharp
// NG: SceneModuleMap に mainSceneKey がなければ例外
mainSceneKey => RequireSceneModuleIds
    .Concat(SceneModuleMap[mainSceneKey] ?? Array.Empty<ModuleSceneId>()).ToArray()
```

## 修正対象

```
Client/Assets/DungeonInn/Runtime/Scripts/Core/SceneGroupProvider.cs
```

## 修正内容

`SceneModuleMap[mainSceneKey] ?? ...` を `TryGetValue` パターンに変える。

```csharp
// OK
mainSceneKey => RequireSceneModuleIds
    .Concat(SceneModuleMap.TryGetValue(mainSceneKey, out var modules)
        ? modules
        : Array.Empty<ModuleSceneId>())
    .ToArray()
```

## 完了条件

- [ ] `uloop.cmd compile --project-path Client` エラーゼロ
- [ ] `review/task_006_done.md` に完了報告

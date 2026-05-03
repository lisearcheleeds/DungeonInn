# task_008: ProductMainSceneManager / ProductModuleSceneManager を削除して Lighthouse 本体に戻す

## 問題

デバッグ中に追加された `ProductMainSceneManager` と `ProductModuleSceneManager` は
Lighthouse の `MainSceneManager` / `ModuleSceneManager` の内部実装をほぼ丸ごとコピーしたクラス。
根本原因（VContainerSettings 二重初期化）はすでに修正済みのため、これらは不要。

Lighthouse 内部クラスを複製することは、フレームワークのバージョンアップに追随できなくなる
設計上の問題であるため、削除する。

## 修正内容

### 1. ProductLifetimeScope.cs の登録を元に戻す

```
Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductLifetimeScope.cs
```

変更前:
```csharp
builder.Register<ProductMainSceneManager>(Lifetime.Singleton).AsImplementedInterfaces();
builder.Register<ProductModuleSceneManager>(Lifetime.Singleton).AsImplementedInterfaces();
```

変更後:
```csharp
builder.Register<MainSceneManager>(Lifetime.Singleton).AsImplementedInterfaces();
builder.Register<ModuleSceneManager>(Lifetime.Singleton).AsImplementedInterfaces();
```

`MainSceneManager` と `ModuleSceneManager` は `Lighthouse.Scene` 名前空間にある。
`using Lighthouse.Scene;` は既に追加されているはずだが、なければ追加する。

### 2. 不要ファイルの削除

以下の2ファイルを削除する:

```
Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductMainSceneManager.cs
Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductModuleSceneManager.cs
```

対応する `.meta` ファイルも削除すること:
```
Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductMainSceneManager.cs.meta
Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductModuleSceneManager.cs.meta
```

## 完了条件

- [ ] `uloop.cmd compile --project-path Client` エラーゼロ
- [ ] `ProductMainSceneManager.cs` と `ProductModuleSceneManager.cs` が存在しない
- [ ] `review/task_008_done.md` に完了報告

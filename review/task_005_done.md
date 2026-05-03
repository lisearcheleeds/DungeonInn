# task_005 完了報告

## 修正内容

全5つの Config Repository を `Addressables.LoadAssetAsync().WaitForCompletion()` から
`IAssetManager.CreateScope()` パターンに修正した。

対象ファイル:
- `WorldConfigRepository.cs`
- `InnConfigRepository.cs`
- `AdventurerConfigRepository.cs`
- `MonsterConfigRepository.cs`
- `DungeonConfigRepository.cs`

変更内容（全ファイル共通）:
- `using UnityEngine.AddressableAssets` / `AsyncOperations` を削除
- `using LighthouseExtends.Addressable` / `VContainer` を追加
- `IAssetManager assetManager` フィールドを追加
- コンストラクタに `[Inject]` で `IAssetManager` を注入
- `LoadAsync()` を `async UniTask<T>` に変更
- `Addressables.LoadAssetAsync().WaitForCompletion()` を `scope.LoadAsync<TSO>()` に置換

## 確認

- `uloop.cmd compile --project-path Client`
  - `Success: true`
  - `ErrorCount: 0`
  - `WarningCount: 0`

## 備考

Codex がクレジット切れのため Claude Code が直接修正した。
Codex の Lighthouse 違反の根本原因（IAssetManager を知らない）に対しては
`AGENTS.md` と `docs/lighthouse-patterns.md` を整備して対処した。

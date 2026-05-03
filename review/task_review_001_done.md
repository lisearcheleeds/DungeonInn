# task_review_001 完了報告

## レビュー対象

- `Client/Assets/DungeonInn/Runtime/Scripts/`

## 発見した問題

### 1. Domain 層が UniTask / Cysharp.Threading.Tasks に依存している

- 重大度: 高
- 内容: `docs/domain-design.md` の依存ルールでは Domain 層は Unity / Lighthouse / UniTask 禁止だが、Repository interface が `Cysharp.Threading.Tasks` と `UniTask` に依存している。
- 該当箇所:
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/World/IWorldConfigRepository.cs:1` `using Cysharp.Threading.Tasks;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/World/IWorldConfigRepository.cs:7` `UniTask<WorldConfigData> LoadAsync();`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Inn/IInnConfigRepository.cs:1` `using Cysharp.Threading.Tasks;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Inn/IInnConfigRepository.cs:7` `UniTask<InnConfigData> LoadAsync();`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Character/IAdventurerConfigRepository.cs:1` `using Cysharp.Threading.Tasks;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Character/IAdventurerConfigRepository.cs:7` `UniTask<AdventurerConfigData> LoadAsync();`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Character/IMonsterConfigRepository.cs:1` `using Cysharp.Threading.Tasks;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Character/IMonsterConfigRepository.cs:7` `UniTask<MonsterConfigData> LoadAsync();`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Dungeon/IDungeonConfigRepository.cs:1` `using Cysharp.Threading.Tasks;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Dungeon/IDungeonConfigRepository.cs:7` `UniTask<DungeonConfigData> LoadAsync();`

### 2. Infrastructure 層が IAssetScope 以外の LighthouseExtends 型に依存している

- 重大度: 中
- 内容: 依存ルールでは Infrastructure 層の Lighthouse 依存は `IAssetScope` のみ許可とされているが、Repository 群が `IAssetManager` を直接保持・注入している。また `ProductAssetLoader` は Addressable 以外の LighthouseExtends サービスにも依存している。
- 該当箇所:
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/AdventurerConfigRepository.cs:3` `using LighthouseExtends.Addressable;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/AdventurerConfigRepository.cs:10` `readonly IAssetManager assetManager;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/DungeonConfigRepository.cs:10` `readonly IAssetManager assetManager;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/InnConfigRepository.cs:10` `readonly IAssetManager assetManager;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/MonsterConfigRepository.cs:10` `readonly IAssetManager assetManager;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/WorldConfigRepository.cs:10` `readonly IAssetManager assetManager;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:8` `using LighthouseExtends.Font;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:9` `using LighthouseExtends.ScreenStack;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:10` `using LighthouseExtends.TextTable;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:17` `IScreenStackInstanceFactory, ITextTableLoader`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:22` `readonly IFontService fontService;`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:23` `readonly IAssetManager assetManager;`

### 3. `InnLand.GetRoomContaining` のシグネチャが domain-design.md と一致していない

- 重大度: 中
- 内容: domain-design.md では `public Room? GetRoomContaining(GridPosition worldPos);` と定義されているが、実装は `Room` 戻り値になっている。見つからない場合は `FirstOrDefault` により null が返るため、nullable 契約が型に表現されていない。
- 該当箇所:
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Inn/InnLand.cs:47` `public Room GetRoomContaining(GridPosition worldPos)`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Inn/InnLand.cs:49` `return rooms.FirstOrDefault(r => r.Contains(worldPos));`

### 4. domain-design.md に定義された Application UseCase が未実装

- 重大度: 高
- 内容: `docs/domain-design.md` の Application 層に定義されている UseCase クラスが存在しない。`Application/UseCase/` 配下に `.cs` ファイルがなく、検索でも対象クラス・`ExecuteAsync` シグネチャは見つからなかった。
- 該当箇所:
  - `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/` `.cs` ファイルなし
- 未実装:
  - `PurchaseInnParcelUseCase`
  - `PlaceBedUseCase`
  - `RemoveBedUseCase`
  - `AdventurerCheckInUseCase`
  - `AdventurerCheckOutUseCase`
  - `GenerateDungeonUseCase`

### 5. domain-design.md に定義された Application AI が未実装

- 重大度: 高
- 内容: `docs/domain-design.md` の AI アーキテクチャに定義されている `IAdventurerAI` / `DefaultAdventurerAI` / AI state 群が存在しない。`Application/AI/` 配下に `.cs` ファイルがなく、検索でも対象型は見つからなかった。
- 該当箇所:
  - `Client/Assets/DungeonInn/Runtime/Scripts/Application/AI/` `.cs` ファイルなし
- 未実装:
  - `IAdventurerAI`
  - `DefaultAdventurerAI`
  - `IAdventurerAIState`
  - `RestingState`
  - `TravelingState`
  - `ExploringState`
  - `ReturningState`

### 6. ファイルパスと namespace が一致していない

- 重大度: 低
- 内容: `docs/domain-design.md` のフォルダ・namespace 方針ではパスと namespace を対応させる前提だが、一部で `Runtime/Scripts` を含む namespace と含まない namespace が混在している。
- 該当箇所:
  - `Client/Assets/DungeonInn/Runtime/Scripts/Input/InputActions.cs:18` パス上は `.../Scripts/Input` だが namespace は `DungeonInn.Input`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/WorldConfigSO.cs:4` パス上は `.../Scripts/Infrastructure/Repository` だが namespace は `DungeonInn.Infrastructure.Repository`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/WorldConfigRepository.cs:6` 同上
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/MonsterConfigSO.cs:4` 同上
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/MonsterConfigRepository.cs:6` 同上
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/InnConfigSO.cs:4` 同上
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/InnConfigRepository.cs:6` 同上
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/DungeonConfigSO.cs:4` 同上
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/DungeonConfigRepository.cs:6` 同上
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/AdventurerConfigSO.cs:4` 同上
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/AdventurerConfigRepository.cs:6` 同上
- 補足: Domain 層は `DungeonInn.Domain.*` で domain-design.md と一致している。View/Core/Input では `DungeonInn.Runtime.Scripts.*` 系と `DungeonInn.Input` が混在している。

### 7. WebGL ビルド時に `UnityWebRequest` の using 不足でコンパイルエラーになる可能性がある

- 重大度: 中
- 内容: `UNITY_WEBGL && !UNITY_EDITOR` ブロック内で `UnityWebRequest` を使用しているが、ファイル先頭に `using UnityEngine.Networking;` がない。現状の通常 Editor コンパイルでは露出しないが、WebGL player build では未解決型になる可能性が高い。
- 該当箇所:
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:67` `UnityWebRequest.Get(manifestUrl)`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:70` `UnityWebRequest.Result.Success`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:188` `UnityWebRequest.Get(url)`
  - `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:190` `UnityWebRequest.Result.Success`

## AGENTS.md 禁止事項チェック結果

- `Addressables.LoadAssetAsync`: 問題なし
- `Resources.Load`: 問題なし
- `SceneManager.LoadScene` / `LoadSceneAsync`: `Client/Assets/DungeonInn/Runtime/Scripts/Core/Launcher.cs:48` の `Reboot()` 例外のみ
- `Task` / `ValueTask`: 問題なし
- `UnityEngine.UI.Button` の直接使用: 問題なし
  - `InputActions.cs` 内の `"type": "Button"` は Input System の action type 文字列であり対象外
  - `ButtonRxExtensions.cs` は `LHButton` 系の拡張であり対象外
- `Keyboard.current` / `Mouse.current`: 問題なし
- `Input.GetKey` / `Input.GetAxis`: 問題なし
- `WaitForCompletion()`: 問題なし

## アーキテクチャ依存ルールチェック結果

- Domain 層の Unity / Lighthouse using: 問題なし
- Domain 層の MonoBehaviour: 問題なし
- Domain 層の UniTask: 問題あり（問題 1）
- Application 層の Unity / Lighthouse / Task / ValueTask: `.cs` ファイルが存在しないため違反なし。ただし UseCase / AI 未実装（問題 4, 5）
- Infrastructure 層の Lighthouse 依存: 問題あり（問題 2）

## namespace チェック結果

- グローバル namespace のクラス: 問題なし
- ファイルパスと namespace の一致: 問題あり（問題 6）

## domain-design.md 主要シグネチャ確認

- `InnLand`: `GetRoomContaining` 以外は対象メソッドあり。`GetRoomContaining` は nullable 契約不一致（問題 3）
- `Room`: `OpenDoor`, `CanPlaceBedAt`, `PlaceBed`, `RemoveBed`, `IsWall`, `Contains`, `Density` あり
- `CharacterBase`: `TakeDamage`, `Heal`, `IsAlive` あり
- `AdventurerCharacter`: `UpdateSatisfaction`, `TransitionState` あり
- `InnFeeCalculator`: `Calculate(Satisfaction satisfaction, Money baseFee, Money maxTip, float tipThreshold)` あり
- `SatisfactionCalculator`: `Calculate(float density, bool isPrivateRoom)` あり

# task_011 完了報告

## 実装内容

- `Application/UseCase` 配下 6 クラスの `ExecuteAsync` を実装
  - `PurchaseInnParcelUseCase`
  - `PlaceBedUseCase`
  - `RemoveBedUseCase`
  - `AdventurerCheckInUseCase`
  - `AdventurerCheckOutUseCase`
  - `GenerateDungeonUseCase`
- `throw new NotImplementedException()` は対象 UseCase から除去済み
- Application 層の Unity / Lighthouse 依存は追加していない
- コンパイル確認中に見つかった既存ブロッカーを最小修正
  - `IAdventurerAIState` のアクセシビリティを public に変更
  - Unity の `Application` 型参照を `UnityEngine.Application` に明示

## 確認

`uloop.cmd compile --project-path Client`

- Success: true
- ErrorCount: 0
- WarningCount: 0

## 備考

UseCase 実装自体の問題はなし。

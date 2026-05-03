# task_011: UseCase 実装

## 概要

Application/UseCase/ 配下の 6 クラスのメソッド本体を実装する。
現在はすべて `throw new NotImplementedException()` になっている。

---

## AGENTS.md を必ず読むこと

このリポジトリの AGENTS.md に Lighthouse / UniTask / Addressables の禁止事項と正しいパターンが書いてある。
作業前に読むこと。特に **Application 層は Unity 禁止・UniTask 必須** のルールに注意する。

---

## 対象ファイル

```
Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/PurchaseInnParcelUseCase.cs
Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/PlaceBedUseCase.cs
Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/RemoveBedUseCase.cs
Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerCheckInUseCase.cs
Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerCheckOutUseCase.cs
Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/GenerateDungeonUseCase.cs
```

---

## 実装仕様

### PurchaseInnParcelUseCase.ExecuteAsync

```csharp
// 処理:
// 1. land.CanPurchaseAt(origin, cost) が false なら InvalidOperationException をスロー
// 2. var room = land.PurchaseParcel(origin, cost)
// 3. return UniTask.FromResult(room)
```

### PlaceBedUseCase.ExecuteAsync

```csharp
// 処理:
// 1. land.UnplacedBedCount <= 0 なら InvalidOperationException("No unplaced beds.")
// 2. room.CanPlaceBedAt(worldPos) が false なら InvalidOperationException("Cannot place bed.")
// 3. var bed = new Bed(Guid.NewGuid(), worldPos)
// 4. room.PlaceBed(bed)
// 5. return UniTask.CompletedTask
//
// Bed のコンストラクタ: Bed(Guid id, GridPosition position)
```

### RemoveBedUseCase.ExecuteAsync

```csharp
// 処理:
// 1. room.RemoveBed(bedId)
// 2. return UniTask.CompletedTask
```

### AdventurerCheckInUseCase.ExecuteAsync

```csharp
// 戻り値: 空きベッドが見つかれば Bed、なければ null（Bed は class なので null 返却可能）
//
// 処理:
// 1. land.Rooms を LINQ で走査
// 2. 各 Room の Beds から !IsOccupied のベッドを FirstOrDefault で探す
// 3. 見つかれば bed.CheckIn() を呼び、return UniTask.FromResult(bed)
// 4. 見つからなければ return UniTask.FromResult<Bed>(null)
//
// Bed.CheckIn() は引数なし
```

### AdventurerCheckOutUseCase.ExecuteAsync

```csharp
// 処理:
// 1. isPrivateRoom = (room.Beds.Count == 1)
// 2. satisfaction = satisfactionCalculator.Calculate(room.Density, isPrivateRoom)
// 3. adventurer.UpdateSatisfaction(satisfaction)
// 4. fee = feeCalculator.Calculate(
//        satisfaction,
//        new Money(config.BaseFee),
//        new Money(config.MaxTip),
//        config.TipThreshold)
// 5. land.AddFunds(fee)
// 6. bed.CheckOut()
// 7. return UniTask.FromResult(fee)
//
// Money は struct: new Money(int value)
// Bed.CheckOut() は引数なし
```

### GenerateDungeonUseCase.ExecuteAsync

```csharp
// 処理:
// 1. var map = generator.Generate(
//        config.FloorSizeX,
//        config.FloorSizeZ,
//        worldConfig.DungeonFloorCount,
//        worldConfig.DungeonFloorHeight,
//        config.Seed,
//        config.MinStairsCount)
// 2. return UniTask.FromResult(map)
```

---

## 制約

- Application 層: Unity 依存禁止（UnityEngine.* using 不可）
- Lighthouse 依存禁止（LighthouseExtends.* using 不可）
- Task / ValueTask 禁止 → UniTask.FromResult / UniTask.CompletedTask を使う
- コメントは WHY が非自明な場合のみ（WHAT を説明するコメント禁止）
- グローバル namespace 禁止 → namespace は `DungeonInn.Application.UseCase`

---

## 完了条件

- [ ] 6 ファイルすべての ExecuteAsync が実装済み（throw NotImplementedException なし）
- [ ] `uloop.cmd compile --project-path Client` エラーゼロ
- [ ] `review/task_011_done.md` に完了報告（問題があれば記載）

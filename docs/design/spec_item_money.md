# アイテム・お金システム設計

## 概要

アイテムは「インベントリに所持する状態」と「ワールドにドロップされた状態」の2つの存在形態を持つ。
お金（Gold）はアイテムの一種として統一的に扱われる。

---

## データ構造

### ItemStack（Domain/Item）

```csharp
readonly struct ItemStack { int ItemId; int Count; }
```

**「アイテムID ＋ 個数」を表す値型。**  
インベントリ操作・マスタデータ・取引など、アイテムが「どこかに存在する」文脈全般で使用する。
位置情報もインスタンスIDも持たない。

### ItemInstance（Domain/Item）

```csharp
sealed class ItemInstance { Guid InstanceId; ItemStack Stack; LayerPosition Position; }
```

**ワールドに落ちているアイテムのエンティティ。**  
`ItemStack`（何が何個）に、ワールド上の固有IDと座標を付加したもの。  
`GameWorldState.Items` で管理される。

> **設計の意図**：アイテム自体は座標を持たない。座標は「ドロップされた状態」に固有の情報であるため、`ItemInstance` が保持する。

### Inventory（Domain/Item）

```
Dictionary<ItemId, Count>
```

アクターが所持するアイテムの集合。`ItemStack` 単位で Add / Remove を行う。  
同一アイテムIDは自動的にスタックされる。

| メソッド | 説明 |
|---|---|
| `Add(ItemStack)` | 指定アイテムをスタックに加算 |
| `Remove(ItemStack)` | 指定アイテムをスタックから減算（不足時は例外） |
| `Has(ItemStack)` | 所持確認 |
| `AddGold(int)` / `TrySpendGold(int)` | Gold 専用の便利メソッド |

### ItemMaster（Master）

アイテムの静的定義。

| フィールド | 説明 |
|---|---|
| `Id` | アイテムID |
| `Name` | 表示名 |
| `Category` | Material / Consumable / Equipment |
| `BasePrice` | 基本価格 |
| `CanTrade` | 取引可能フラグ |

---

## お金（Gold）の扱い

Gold はアイテムの一種として統一的に扱う。

| 定数 | 値 |
|---|---|
| `SpecialItemIds.Money` | `1` |

- `Inventory.Gold` → `itemCounts[SpecialItemIds.Money]` のショートカット
- `Inventory.AddGold(amount)` / `TrySpendGold(amount)` で操作
- マスタ定義：`ItemMaster(1, "Gold", Material, 1, 1, false)` （取引不可）

---

## アイテムドロップ

### ActorDropEntry（Domain/Item）

モンスター・冒険者が死亡時に落とすアイテムのドロップ定義。

| フィールド | 型 | 説明 |
|---|---|---|
| `ItemId` | `int` | ドロップするアイテムID |
| `Probability` | `float` | 確率（0.0〜1.0） |
| `MinCount` | `int` | 最小個数 |
| `MaxCount` | `int` | 最大個数 |

### IActorDropSource（Domain/Actor）

```csharp
interface IActorDropSource { IReadOnlyList<ActorDropEntry> DropTable; }
```

`MonsterBehavior` と `AdventurerBehavior` の両方が実装する。
冒険者は現在 `DropTable` が空。将来的に冒険者ドロップを追加する場合は `DropTable` にエントリを追加するだけでよい。

### DropItemUseCase（Application/UseCase）

```
actor.Behavior is IActorDropSource → 各エントリを確率ロール → パスしたものを ItemInstance として WorldState に追加 → ItemDropped イベント発行
```

1. `IGameRandom.Next(0, 10000) / 10000f` で確率ロール
2. `roll < entry.Probability` ならドロップ成立
3. `MinCount == MaxCount` なら固定個数、異なれば `IGameRandom.Next(min, max+1)` で乱択
4. `new ItemInstance(Guid.NewGuid(), new ItemStack(itemId, count), position)` を生成
5. `GameWorldState.AddItem(instance)` でワールド登録
6. `ItemDropped` イベント発行

#### 現在のドロップテーブル（HardcodedMasterRepository）

| モンスター | アイテム | 確率 | 個数 |
|---|---|---|---|
| ゴブリン | Goblin Ear (1002) | 70% | 1 |
| ゴブリン | Gold (1) | 50% | 1〜3 |

---

## ワールド上のアイテム（GameWorldState）

| メソッド | 説明 |
|---|---|
| `AddItem(ItemInstance)` | アイテムをワールドに追加 |
| `RemoveItem(Guid instanceId)` | アイテムをワールドから除去 |
| `Items` | 現在ワールドに存在する全アイテム一覧 |

---

## イベント

| イベント | 発行タイミング | 主なフィールド |
|---|---|---|
| `ItemDropped` | アイテムがドロップされた時 | `ActorId`（ドロップ元）, `ItemInstance` |

---

## 取引・交換

`ExchangeOffer` は施設が提示する「RequestedItems を渡せば RewardItems を受け取れる」オファー。
いずれも `IReadOnlyList<ItemStack>` で表現される。

---

## 型の使い分けまとめ

| 型 | 使う場面 | 位置情報 | 固有ID |
|---|---|---|---|
| `ItemStack` | インベントリ操作・マスタ初期値・取引 | なし | なし |
| `ItemInstance` | ワールドに落ちているアイテム | あり | あり |

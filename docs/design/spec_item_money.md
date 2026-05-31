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

### InventorySlot（Domain/Item）

```csharp
readonly struct InventorySlot { int ItemId; int Count; }
```

`Inventory` の内部スロット表現。`ItemStack` と同じ構造だが、`Inventory` の内部リスト専用の型として分離されている。  
外部から `InventorySlot` を直接操作する必要はなく、外部 API は常に `ItemStack` を受け取る。

### Inventory（Domain/Item）

アクターが所持するアイテムの集合。内部的に `List<InventorySlot>` でスロットを管理し、外部 API は `ItemStack` 単位で操作する。

| メソッド | 説明 |
|---|---|
| `Add(ItemStack)` | 指定アイテムをスタックに加算 |
| `Remove(ItemStack)` | 指定アイテムをスタックから減算（不足時は例外） |
| `CanAdd(ItemStack)` / `CanAddAll` | スロット容量・スタック上限を考慮した事前チェック |
| `Has(ItemStack)` | 所持確認 |
| `AddGold(int)` / `TrySpendGold(int)` | Gold 専用の便利メソッド |

#### インベントリのスロット管理

インベントリは最大 `DefaultInventorySlotCapacity = 10` スロットを持つ。  
各スロットは 1 種類のアイテムを最大 `MaxStackCount` 個まで積める。

**スタック挙動**（`Add` 時）:
1. 既存スロットに同 ItemId があり、かつ `MaxStackCount` に余裕があれば、そのスロットに積む
2. 既存スロットが全て上限に達していれば、新規スロットを追加する
3. `IsFull`（`UsedSlotCount >= MaxSlotCount`）の場合は `Add` が例外を投げる（事前に `CanAdd` で確認すること）

**スタック上限の設計意図**：
- `MaxStackCount = 100000`（Gold）: 実質無制限。Gold はお金なので個数制限が不自然なため
- `MaxStackCount = 10`（素材系）: スロット節約とインベントリ管理の適度なゲーム性
- `MaxStackCount = 1`（装備品）: 装備は同種でも別インスタンスとして扱うため

> **当初想定との差異**：インベントリのスロット制・スタック制は企画書に明記されていなかったが、アイテム取得フローの実装時に必要になり導入した。ゲーム性（持てるアイテム数の制限）とデータ管理（メモリ効率）の両観点から採用。

#### IItemStackLimitResolver（Domain/Item）

```csharp
interface IItemStackLimitResolver { int GetMaxStackCount(int itemId); }
```

アイテムIDごとの「1スロットに積める最大個数」を返すインターフェース。  
`HardcodedMasterRepository`（Master 層）が実装し、VContainer 経由で解決される。

**Domain 層に置く理由**：`Inventory.CanAdd` / `Add` がスタック上限を把握するために必要。インターフェースを Domain に置くことで `Inventory`（Domain エンティティ）が外部層に依存せず参照できる。

**`Inventory` がリゾルバを保持する理由**：`CanAdd`・`CanAddAll`・`Add` でスタック上限チェックが必要になる。リゾルバを外部（Use Case 等）が毎回渡すと、スタック制御ロジックが `Inventory` の外に漏れる。`Inventory` コンストラクタで受け取ることで「インベントリがスタック管理の責務を持つ」設計を維持する。

**生成時の流れ**：VContainer が `IItemStackLimitResolver` を解決 → `ActorFactoryCore`（Application）がコンストラクタ引数として受け取り → `new Inventory(stackLimitResolver)` で Domain エンティティを生成。Domain エンティティは VContainer では解決できないため、Application ファクトリがブリッジ役を担う。

### ItemMaster（Master）

アイテムの静的定義。

| フィールド | 説明 |
|---|---|
| `Id` | アイテムID |
| `Name` | 表示名 |
| `Tags` | `ItemTag` flags（Material / Recovery / Weapon / Armor / Accessory などを複数指定可） |
| `BasePrice` | 基本価格 |
| `CanTrade` | 取引可能フラグ |
| `MaxStackCount` | 1スロットに積める最大個数 |

---

## お金（Gold）の扱い

Gold はアイテムの一種として統一的に扱う。

| 定数 | 値 |
|---|---|
| `SpecialItemIds.Money` | `1` |

- `Inventory.Gold` → `itemCounts[SpecialItemIds.Money]` のショートカット
- `Inventory.AddGold(amount)` / `TrySpendGold(amount)` で操作
- マスタ定義：`ItemMaster(1, "Gold", ItemTag.Currency, 1, 1, false)` （取引不可）

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

### SpeciesMaster.SpeciesDrops（Master）

通常撃破時の種族由来ドロップは `SpeciesMaster.SpeciesDrops` に定義する。

`MonsterBehavior` / `AdventurerBehavior` は `DropTable` を持たない。`DropItemUseCase` は撃破対象が Monster の場合のみ、撃破対象の `ActorArchetypeMaster.SpeciesId` から `SpeciesMaster` を解決し、`SpeciesDrops` を使ってドロップを生成する。

冒険者の死亡時ロストは通常ドロップとは別の復活フローで扱う。`AdventurerDeathRevivalService` が、インベントリ内の全アイテム、装備中の武器 / 防具 / アクセサリ、`SpecialItemIds.Money` を死亡位置へ `ItemInstance` としてドロップする。

### DropItemUseCase（Application/UseCase）

```
defeatedActor.Behavior is MonsterBehavior → ActorArchetypeMaster から SpeciesMaster を解決 → SpeciesDrops の各エントリを確率ロール → パスしたものを ItemInstance として WorldState に追加 → ItemDropped イベント発行
```

1. `IGameRandom.Next(0, 10000) / 10000f` で確率ロール
   - 浮動小数点の直接比較を避けるため整数（0〜9999）で乱数を引いてから float に変換
2. `roll < entry.Probability` ならドロップ成立
3. `MinCount == MaxCount` なら固定個数、異なれば `IGameRandom.Next(min, max+1)` で乱択
4. `new ItemInstance(Guid.NewGuid(), new ItemStack(itemId, count), position)` を生成
5. `GameWorldState.AddItem(instance)` でワールド登録
6. `ItemDropped` イベント発行

冒険者の死亡復活時に落とす所持品 / 装備 / 所持金は、`DropItemUseCase` ではなく `AdventurerDeathRevivalService` が担当する。

---

## ワールド上のアイテム（GameWorldState）

| メソッド | 説明 |
|---|---|
| `AddItem(ItemInstance)` | アイテムをワールドに追加 |
| `RemoveItem(Guid instanceId)` | アイテムをワールドから除去 |
| `Items` | 現在ワールドに存在する全アイテム一覧 |

---

## アイテムピックアップ

### PickUpItemUseCase（Application/UseCase）

```
全アクターを走査 → AdventurerBehavior かつ Exploring 状態のアクターのみ対象
  └─ ワールド上のアイテムを走査
       1. 同一レイヤーかチェック
       2. 距離チェック（GameConstants.AdventurerItemPickupRadiusMeters の範囲内か）
       3. Inventory.CanAdd でスロット空き確認
       4. Inventory.Add → WorldState.RemoveItem → ItemPickedUp イベント発行
```

- モンスターはピックアップ対象外（`AdventurerBehavior` チェックで除外）
- インベントリが満杯の場合はそのアイテムをスキップ（例外なし）

## イベント

| イベント | 発行タイミング | 主なフィールド |
|---|---|---|
| `ItemDropped` | アイテムがドロップされた時 | `ActorId`（ドロップ元）, `ItemInstance` |
| `ItemPickedUp` | アイテムが拾われた時 | `ActorId`（拾ったアクター）, `ItemInstance` |

### Gold ピックアップ時のウォレット表示

`ItemPickedUp` イベントには所持金残高を持たせていない。  
Debug ログ表示（`WorldDebugGameLogPresenter`）では Gold ピックアップ時に `worldState.FindActor()` でアクターを引いてから `actor.Inventory.Gold` を読む。
**理由**：ピックアップ後の残高はすでに `Inventory` に反映されているため、イベントに冗長なスナップショットを持たせるより状態を直接参照する方がシンプル。

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

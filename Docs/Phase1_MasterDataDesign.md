# Phase 1 — マスタデータ構造定義書

## 概要

本ドキュメントは「Inn Before the Dungeon」のマスタデータ構造を定義する。  
ScriptableObjectで管理し、Runtime時はScriptableObjectから読み込む。  
将来的にTSV/JSON対応が必要になった場合は対応するローダーをInfrastructure層に追加する。

---

## 1. キャラクタースペック

### 1-1. CharacterBaseSpec（キャラクター基底スペック）

すべてのキャラクタースペックの基底となるScriptableObject。

**ファイル:** `Assets/DungeonInn/Runtime/Resources/MasterData/Character/CharacterBaseSpec.asset`  
**ScriptableObject型:** `CharacterBaseSpec : ScriptableObject`

| フィールド名 | 型 | 説明 |
|---|---|---|
| `characterId` | `string` | 一意識別子（例: `"adventurer_001"`） |
| `displayName` | `string` | 表示名（例: `"ガルム"`) |
| `characterType` | `CharacterType` | `InnKeeper / Adventurer / InnStaff / Monster` |
| `maxHp` | `int` | 最大HP |
| `attackPower` | `int` | 攻撃力 |
| `defense` | `int` | 防御力 |
| `moveSpeed` | `float` | 移動速度（m/s） |
| `attackSpeed` | `float` | 攻撃速度（回/s） |
| `personalityType` | `PersonalityType` | 性格タイプ（後述） |

```
enum CharacterType { InnKeeper, Adventurer, InnStaff, Monster }

enum PersonalityType
{
    Cautious,    // 慎重（安全重視）
    Aggressive,  // 積極的（ダンジョン深部を好む）
    Social,      // 社交的（プライバシー感度低）
    Solitary,    // 孤独好き（プライバシー感度高）
    Balanced,    // バランス型
}
```

---

### 1-2. AdventurerSpec（冒険者スペック）

`CharacterBaseSpec` を参照し、冒険者固有のパラメータを追加するScriptableObject。

**ScriptableObject型:** `AdventurerSpec : ScriptableObject`

| フィールド名 | 型 | 説明 |
|---|---|---|
| `baseSpec` | `CharacterBaseSpec` | 基底スペック参照 |
| `initialGold` | `int` | 初期所持金（Gold） |
| `sightRange` | `float` | 視界範囲（m）。Privacy判定に使用 |
| `sightAngle` | `float` | 視野角（度）。Privacy判定に使用 |
| `privacySensitivity` | `float` | プライバシー感度 `0.0〜1.0`。高いほど視線による満足度減少が大きい |
| `preferredFloorDepth` | `int` | 好む最大ダンジョン深度（1〜5）。Aggressiveは5、Cautiousは1など |
| `baseDailyRate` | `int` | この冒険者の宿泊基本料金（Gold）|

---

### 1-3. InnKeeperSpec（宿屋店長スペック）

**ScriptableObject型:** `InnKeeperSpec : ScriptableObject`

| フィールド名 | 型 | 説明 |
|---|---|---|
| `baseSpec` | `CharacterBaseSpec` | 基底スペック参照 |
| `initialGold` | `int` | 宿屋の初期資金 |

---

### 1-4. InnStaffSpec（店員スペック、将来拡張用）

**ScriptableObject型:** `InnStaffSpec : ScriptableObject`

| フィールド名 | 型 | 説明 |
|---|---|---|
| `baseSpec` | `CharacterBaseSpec` | 基底スペック参照 |
| `staffRole` | `StaffRoleType` | 役職（`Receptionist / Cleaner` など） |
| `hireGold` | `int` | 雇用コスト（Gold/日） |

```
enum StaffRoleType { Receptionist, Cleaner }
```

---

## 2. モンスタースペック

### 2-1. MonsterSpec（モンスタースペック）

**ScriptableObject型:** `MonsterSpec : ScriptableObject`

| フィールド名 | 型 | 説明 |
|---|---|---|
| `monsterId` | `string` | 一意識別子（例: `"monster_slime_001"`） |
| `displayName` | `string` | 表示名 |
| `baseSpec` | `CharacterBaseSpec` | 基底スペック参照 |
| `minFloorDepth` | `int` | 出現最小階層（1〜5） |
| `maxFloorDepth` | `int` | 出現最大階層（1〜5） |
| `behaviorPattern` | `MonsterBehaviorPatternType` | 行動パターン種別（後述） |
| `dropGoldMin` | `int` | ドロップ最低Gold |
| `dropGoldMax` | `int` | ドロップ最高Gold |
| `wanderRadius` | `float` | 徘徊半径（m） |

```
enum MonsterBehaviorPatternType
{
    Wander,        // ランダム徘徊
    Patrol,        // 固定ルートパトロール
    Aggressive,    // 視野に入った敵を即追跡
    Territorial,   // 縄張り内のみ追跡
}
```

---

## 3. 宿屋設備マスタ

### 3-1. BedSpec（ベッドスペック）

**ScriptableObject型:** `BedSpec : ScriptableObject`

| フィールド名 | 型 | 説明 |
|---|---|---|
| `bedId` | `string` | 一意識別子（例: `"bed_straw_001"`） |
| `displayName` | `string` | 表示名（例: `"麦わらベッド"`） |
| `purchaseCost` | `int` | 購入コスト（Gold） |
| `comfortBonus` | `float` | 快適度ボーナス（満足度計算に加算）`0.0〜1.0` |
| `sizeX` | `int` | 占有グリッドX幅（通常 1） |
| `sizeZ` | `int` | 占有グリッドZ幅（通常 1） |
| `tier` | `BedTier` | ランク（解放条件に使用） |

```
enum BedTier { Basic, Standard, Comfort, Luxury }
```

---

### 3-2. RoomTypeSpec（部屋タイプスペック、将来拡張用）

**ScriptableObject型:** `RoomTypeSpec : ScriptableObject`

| フィールド名 | 型 | 説明 |
|---|---|---|
| `roomTypeId` | `string` | 一意識別子 |
| `displayName` | `string` | 表示名（例: `"大部屋"`, `"個室"`） |
| `maxBedCount` | `int` | 配置可能最大ベッド数 |
| `buildCost` | `int` | 建設コスト（Gold） |
| `privacyModifier` | `float` | 部屋タイプによるプライバシー係数（個室なら侵害減少） |

---

## 4. ダンジョン階層マスタ

### 4-1. DungeonFloorSpec（ダンジョン階層スペック）

**ScriptableObject型:** `DungeonFloorSpec : ScriptableObject`

| フィールド名 | 型 | 説明 |
|---|---|---|
| `floorDepth` | `int` | 階層番号（1〜5、B1F〜B5F） |
| `displayName` | `string` | 表示名（例: `"B1F"`) |
| `difficultyCoefficient` | `float` | 難易度係数（1.0〜5.0）。ダメージ・報酬の倍率 |
| `mapWidth` | `int` | マップX幅（グリッド数） |
| `mapHeight` | `int` | マップZ幅（グリッド数） |
| `monsterEntries` | `MonsterSpawnEntry[]` | 出現モンスター一覧（後述） |
| `maxMonsterCount` | `int` | 同時存在モンスター最大数 |
| `monsterRespawnIntervalSec` | `float` | モンスター再出現間隔（ゲーム内秒） |

```csharp
[Serializable]
struct MonsterSpawnEntry
{
    string monsterId;   // MonsterSpec.monsterId への参照
    int spawnWeight;    // 抽選重み（相対値）
}
```

---

## 5. 土地拡張マスタ

### 5-1. LandExpansionSpec（土地拡張スペック）

**ScriptableObject型:** `LandExpansionSpec : ScriptableObject`

土地は 5m×5m 単位で購入・拡張する。
初期土地は 10m×10m（2×2ブロック相当）。

| フィールド名 | 型 | 説明 |
|---|---|---|
| `expansionId` | `string` | 一意識別子（例: `"expansion_001"`） |
| `displayName` | `string` | 表示名（例: `"北側拡張 Lv1"`） |
| `offsetX` | `int` | 初期土地左上を基点とした拡張先X座標（5m単位） |
| `offsetZ` | `int` | 初期土地左上を基点とした拡張先Z座標（5m単位） |
| `sizeX` | `int` | 拡張ブロックX幅（5m単位、通常 1） |
| `sizeZ` | `int` | 拡張ブロックZ幅（5m単位、通常 1） |
| `purchaseCost` | `int` | 購入コスト（Gold） |
| `prerequisiteExpansionIds` | `string[]` | 購入に必要な先行拡張IDリスト（隣接制限） |

---

## 8. マスタデータ管理方針

### ScriptableObject配置ルール

```
Assets/DungeonInn/Runtime/Resources/MasterData/
├── Character/
│   ├── CharacterBaseSpec/     # CharacterBaseSpec assets
│   ├── AdventurerSpec/        # AdventurerSpec assets
│   ├── InnKeeperSpec/         # InnKeeperSpec asset（1体）
│   └── InnStaffSpec/          # InnStaffSpec assets（将来）
├── Monster/
│   └── MonsterSpec/           # MonsterSpec assets
├── Inn/
│   ├── BedSpec/               # BedSpec assets
│   └── RoomTypeSpec/          # RoomTypeSpec assets（将来）
├── Dungeon/
│   └── DungeonFloorSpec/      # DungeonFloorSpec assets（B1F〜B5F の5つ）
├── Land/
│   └── LandExpansionSpec/     # LandExpansionSpec assets
└── Config/
    └── GameConfigSpec/        # GameConfigSpec asset（1つ）
```

---

## 6. ゲーム設定マスタ

### 6-1. GameConfigSpec（ゲーム設定スペック）

ゲーム全体に関わる定数・パラメータを一元管理するScriptableObject（1アセットのみ）。

**ScriptableObject型:** `GameConfigSpec : ScriptableObject`

| フィールド名 | 型 | 初期値 | 説明 |
|---|---|---|---|
| `maxAdventurerCount` | `int` | `20` | ゲーム中に同時存在できる冒険者の最大数 |
| `adventurerSpawnIntervalDay` | `int` | `1` | 新規冒険者が到着する間隔（ゲーム内日数）|
| `initialAdventurerCount` | `int` | `5` | ゲーム開始時の初期冒険者数 |

> 冒険者がダンジョンで死亡した場合は復活せず完全に消滅する。総数が `maxAdventurerCount` を下回った場合、`adventurerSpawnIntervalDay` ごとに新規冒険者が1人到着して補充される（Phase 5で実装）。

---

## 7. マスタデータ管理方針

### ロード方針

- Addressablesでロードする
- アセットグループ: `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset`
- ロード実装はInfrastructure層の `ProductAssetLoader` に集約する
- Domain層はScriptableObjectに直接依存しない。Infrastructureがデータを変換してDomain層のValueObjectとして渡す

### 視覚アセットマスタについて

キャラクター・モンスター・設備の見た目に関するAddressableアドレス（モデルPrefab・ポートレートSprite等）は、本ドキュメントのスペックとは**別マスタ**（例: `CharacterVisualSpec`）でPresentation/Infrastructure層が管理する。`characterId` 等をキーに対応付ける。視覚アセットマスタの設計は **Phase 4（ワールド・カメラ実装）時点で別途行う**。

### データ整合性ルール

1. `monsterId / characterId / bedId / expansionId` はアプリ内で一意であること
2. `DungeonFloorSpec.floorDepth` は 1〜5 の範囲であること
3. `LandExpansionSpec.prerequisiteExpansionIds` に循環参照を含めないこと
4. `MonsterSpec.minFloorDepth <= maxFloorDepth` であること

---

*作成日: 2026-04-29*  
*フェーズ: Phase 1 — マスタデータ構造設計*

# Actor / Species Master Design

## 目的

Actor の生成単位と種族固有情報を分離し、冒険者・モンスターのどちらも同じマスタ構造で扱えるようにする。

この整理は Milestone 3 Phase 11.5 の差し込みフェーズとして行う。Phase 11 で探索目的・討伐目標が導入され、Actor が削除された後も「何を倒したか」を参照する必要が出たため、Actor 識別情報と種族情報の責務を明確にする。

## 現状の問題

現状は、冒険者とモンスターで参照するマスタ構造が非対称になっている。

- 冒険者は主に `ActorArchetypeMaster` から生成される
- モンスターは `MonsterSpeciesMaster` を入口にし、そこから `ActorArchetypeMaster` を参照して生成される
- `MonsterSpeciesMaster` は `ActorArchetypeMaster` と生成単位の責務が重なっている
- `Species` という概念がモンスター専用になっている

種族はモンスター専用ではない。冒険者にも人間、エルフ、ドワーフなどの種族を持たせられるため、種族は Actor 共通の属性として扱う。

## 方針

生成単位は `ActorArchetypeMaster` に統一する。

種族固有情報は `SpeciesMaster` に分離する。

```text
SpeciesMaster
- Id
- Name
- SpeciesDrops

ActorArchetypeMaster
- Id
- Name
- BehaviorType
- SpeciesId
- DefaultWeaponType
- BaseStats
- InitialLevel
- LevelTableId
- InitialEquipmentItemIds
- InitialInventoryItemIds

AdventurerSpawnMaster
- Id
- DisplayName
- ActorArchetypeId
- SpawnOnce
```

## SpeciesMaster

`SpeciesMaster` は種族固有情報を表す。

持つ情報:

- `Id`
- `Name`
- `SpeciesDrops`

`SpeciesDrops` は種族由来のドロップとして扱う。現在の `MonsterSpeciesMaster.SpeciesDrops` はここへ移す。

`SpeciesMaster` は Actor の生成単位ではない。どのステータスで、どの行動種別で、どの初期装備を持つかは `ActorArchetypeMaster` が決める。

## ActorArchetypeMaster

`ActorArchetypeMaster` は実際に生成される Actor のテンプレートを表す。

追加する情報:

- `SpeciesId`
- `DefaultWeaponType`

`DefaultWeaponType` はモンスター専用ではなく、Actor が装備を持たない場合の自然武器・素手・初期武器種として扱う。冒険者にも設定可能とする。

`ActorArchetypeMaster` はテンプレートであり、固有キャラクターそのものではない。同じ `ActorArchetypeMaster` を複数のスポーン定義から参照できる。

## AdventurerSpawnMaster

`AdventurerSpawnMaster` は冒険者の来訪・スポーン文脈で使う定義を表す。

持つ情報:

- `Id`
- `DisplayName`
- `ActorArchetypeId`
- `SpawnOnce`

冒険者の固有名は `ActorArchetypeMaster` ではなく `AdventurerSpawnMaster.DisplayName` に持たせる。これにより、同じ能力テンプレートを使う `Alice` / `Bob` のような固有名付き冒険者を表現できる。

`SpawnOnce` は、そのスポーン定義がゲーム中に一度だけ出現するかを表す。汎用冒険者を複数生成したい場合は、別のスポーン定義または後続のランダム名生成設計で扱う。

## MonsterSpeciesMaster の廃止

`MonsterSpeciesMaster` は廃止する。

現在の対応関係:

```text
MonsterSpeciesMaster.Id
  -> ActorArchetypeMaster.Id に統合、または SpawnTableEntry.TargetMasterId が ActorArchetypeId を直接参照する

MonsterSpeciesMaster.Name
  -> SpeciesMaster.Name または ActorArchetypeMaster.Name

MonsterSpeciesMaster.ActorArchetypeId
  -> 不要

MonsterSpeciesMaster.DefaultWeaponType
  -> ActorArchetypeMaster.DefaultWeaponType

MonsterSpeciesMaster.CanScavenge
  -> Phase 11.5 では廃止する。必要になった時点で Behavior / AI / Policy として再設計する

MonsterSpeciesMaster.SpeciesDrops
  -> SpeciesMaster.SpeciesDrops
```

`CanScavenge` は種族固有か行動ルーチン固有かがまだ確定していない。今回の主目的は重複マスタの整理なので、Phase 11.5 ではマスタ項目としては引き継がない。

## SpawnTable

冒険者の SpawnTable は `AdventurerSpawnMaster` を生成対象として参照する。

モンスターの SpawnTable は `ActorArchetypeMaster` を生成対象として参照する。

`SpawnTableTargetType.MonsterSpecies` は廃止する。冒険者 SpawnTable の TargetMasterId は AdventurerSpawnId を指し、モンスター SpawnTable の TargetMasterId は ActorArchetypeId を指す。

## ActorProfileRegistry

`ActorProfileRegistry` はゲーム中に追加され続ける Actor 識別データベースとして扱う。

Actor が `GameWorldState` から削除された後も、討伐目標などの判定で必要な識別情報を参照できる必要がある。

保持すべき情報:

- `ActorId`
- `DisplayName`
- `ArchetypeId`
- `SpeciesId`
- `BehaviorType`

討伐目標は削除済み Actor の `ActorId` から `ActorProfileRegistry` を参照し、`SpeciesId` または `ArchetypeId` によって対象判定を行う。

`DisplayName` は表示用の個体名として扱う。冒険者は `AdventurerSpawnMaster.DisplayName`、固有名を持たない Actor は `ActorArchetypeMaster.Name` を初期値として登録する。

`ActorArchetypeMaster.Name` はテンプレート名、`AdventurerSpawnMaster.DisplayName` は初期スポーン定義上の名前であり、生成後の Actor 個体の現在表示名ではない。

将来、ユーザー作成キャラクターや名前変更機能を追加する場合は、生成済み Actor の現在表示名として `ActorProfile.DisplayName` を更新する。これにより、Actor が `GameWorldState` から削除された後も、ログ・履歴・討伐結果などで変更後の名前を参照できる。

## 実装時の注意

- Domain Entity から Master Repository を参照しない
- Actor 削除後に必要な識別情報は、削除前に `ActorProfileRegistry` へ登録しておく
- `ActorArchetypeMaster` と `SpeciesMaster` の責務を混ぜない
- 冒険者専用、モンスター専用という現在用途を理由に、共通化できる概念を片側専用名にしない
- `MonsterSpeciesMaster` を残した互換レイヤーは原則作らない。破壊的変更で参照元を整理する

## 完了条件

- `MonsterSpeciesMaster` が削除されている
- 冒険者・モンスターの生成が `ActorArchetypeMaster` を生成単位としている
- 種族固有ドロップが `SpeciesMaster.SpeciesDrops` にある
- `ActorProfileRegistry` が削除済み Actor の種族・アーキタイプ情報を保持している
- 討伐目標判定が `ActorProfileRegistry` の情報を使っている
- `uloop.cmd compile --project-path Client` が成功する
- EditMode テストが成功する

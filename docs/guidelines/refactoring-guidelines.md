# リファクタリングガイドライン

このドキュメントは、実装後にコードとデータ配置を整理するときの判断基準を定義する。
セルフレビューが「完了状態の妥当性」を確認するためのものだとすれば、リファクタリングレビューは「既存の置き場所・責務・依存・設定方法が、時間経過や機能追加に耐えられる形になっているか」を確認するためのものとする。

特に、`GameConstants` / ScriptableObject / Master / Spec / Addressable / LifetimeScope / Settings class の使い分けを明確にする。

---

## ハードゲート

以下に該当する場合は、リファクタリング対象として扱い、放置してはならない。

- [ ] ゲームバランス値、初期所持金、生成サイズ、出現数、速度、範囲、しきい値など、調整対象の値が `GameConstants` やインライン数値に残っている
- [ ] コンテンツ種別が増えるたびに `LifetimeScope` の `SerializedField` が増える設計になっている
- [ ] Projectile / AreaEffect / Prop / UI View Prefab などのコンテンツ Prefab が、発生元の Master / Spec / Definition ではなく、World 横断の一覧や LifetimeScope に集約されている
- [ ] ScriptableObject を Domain / Application の通常ロジックへ直接注入している
- [ ] ScriptableObject が runtime state、cache、dirty flag、entity instance、現在値を保持している
- [ ] Master から O(1) で引ける不変値を Runtime Instance / State / Entity にコピーして保持している
- [ ] 「テストしやすい」「差分が小さい」だけを理由に、本来の責務と異なる場所へ値・Prefab・Factory・設定を置いている
- [ ] 既存 guideline に判定基準があるのに、「最小差分」を理由に古い命名・古い配置・互換用 API を残している
- [ ] 1 フォルダ直下のクラス数が 40 個以上あるのに、責務別のサブフォルダ分割を検討・実施していない
- [ ] System / bootstrap 用フォルダに、Content / GameSession / MainScene 固有の実装が置かれている
- [ ] 入口画面が存在するのに、Launcher / bootstrap が実ゲームシーンへ直接遷移している
- [ ] 別 Scene / 別 LifetimeScope が所有する Canvas / View / Presenter / Pool / scene-owned component を直接参照・操作している

---

## 基本方針

リファクタリングでは、まず「どこを変えるか」ではなく「その情報の所有者は誰か」を決める。

判断の順序:

1. その値や参照は、コンテンツ定義か、ゲーム全体設定か、コード不変条件かを分類する
2. 変更頻度、編集者、寿命、参照方向、実行時状態の有無を確認する
3. Domain / Application / View / Infrastructure の依存方向に反しない置き場所を選ぶ
4. System / bootstrap と Content / GameSession のどちらが所有者かを確認する
5. 既存コードの都合で暫定配置する場合は、完了条件と撤去条件を明記する

---

## System と Content / GameSession の分離

`Core`、`System`、`Bootstrap`、`Product` などの名前を持つ領域は、アプリケーションを起動・維持する基盤のための場所である。
ここにゲームセッションや具体コンテンツの実装を置くと、仮の遷移・仮の初期化が恒久設計に見え、依存方向が分かりにくくなる。

### 分類

| 分類 | 例 | 所有するもの |
|---|---|---|
| System / Bootstrap | Root, Product, Launcher, SceneGroupProvider | アプリ全体の起動、再起動、共通 service、最初の入口画面への遷移 |
| GameSession | MainGame, GameSession, RunSession | NewGame / Continue / Load で生成され、セッション終了で破棄される state / Application service |
| Content MainScene | World, Battle, Dungeon, Inn | 開始済みセッションを表示・操作する scene object / presenter / scene adapter |
| Module / UI | HUD, Popup, ScreenStack, Audio | MainScene と分離して管理する補助表示・入力・UI |

### 移動判断

- Product 起動前から必要なものは System / Bootstrap に置く。
- NewGame / Continue / Load 後に初めて必要になるものは GameSession に置く。
- 特定 MainScene の GameObject、Camera、Input、View adapter はその MainScene 配下に置く。
- HUD / Popup / EventLog / Minimap など Canvas や補助 UI は Module / UI 配下に置く。
- `WorldCameraSettingsRepository` のような View 固有設定の repository は System ではなく、その View / MainScene の設定領域に置く。
- `WorldGameSettingsRepository` のようなゲームセッション設定の repository は System ではなく GameSession 側に置く。

### 禁止

- System / Bootstrap フォルダに `World` / `Battle` / `Dungeon` / `HUD` などコンテンツ固有名の実装を置く。
- Launcher から実ゲームシーンへ直接遷移する仮実装を、入口画面実装後も残す。
- 「ProductLifetimeScope から登録しているから」という理由で、GameSession / Content 固有クラスを System 配下に置く。
- System 層の controller が、GameSession scope 生成と Content scene 遷移の両方を恒久責務として持つ。

### レビュー観点

- そのクラスは Product 起動だけで必要か、ゲームセッション開始後に必要か
- そのクラスの名前に具体コンテンツ名が含まれていないか
- 登録される LifetimeScope とファイル配置の所有者が一致しているか
- System 層から Content 層への参照が「入口画面への遷移」以上に広がっていないか
- 暫定配置なら TODO に削除条件と移動先が書かれているか

---

## Scene / LifetimeScope 境界を跨ぐ直接参照の撤去

Scene / ModuleScene / LifetimeScope の分離は、所有者と寿命を分けるために行う。
分離後に別 Scene の Canvas / View / Presenter / Pool / scene-owned component を直接参照している場合、見た目だけ分離して責務は分離できていない。

### 撤去対象

- MainScene から ModuleScene の Canvas / View / Presenter / Pool を参照している
- ModuleScene から MainScene の concrete controller / registry / camera / scene object を参照している
- 親 scope が子 scope の scene-owned component を inject している
- `FindObjectOfType` / hierarchy 探索 / serialized reference で別 Scene の所有物を取得している
- 別 Scene の View 実体を更新するための Provider / Controller が存在している

### 移動先

- View / Canvas / Pool / Presenter の操作は、それを所有する Scene / ModuleScene 側へ移す。
- MainScene 固有情報を UI が必要とする場合は、MainScene 側に抽象 interface を登録し、UI 側は抽象のみ参照する。
- 複数 Scene が同じゲーム状態を読む場合は、GameSession / Application scope に state / query / store を置く。
- 画面横断の補助機能は、専用 ModuleScene または親 scope の service として設計し、どちらか一方の Scene 所有物に寄せない。

### 完了条件

- [ ] 他 Scene 所有の Canvas / View / Presenter / Pool を参照する field / constructor parameter / serialized field が消えている
- [ ] UI 更新の起点が UI 所有 ModuleScene 側にある
- [ ] MainScene と ModuleScene の連携は抽象 interface または GameSession / Application service 経由になっている
- [ ] hierarchy 探索で他 Scene の所有物を探していない
- [ ] compile / test / PlayMode で、表示と破棄順が成立している

---

## 配置先の判定表

| 置き場所 | 置いてよいもの | 置いてはいけないもの |
|---|---|---|
| System / Bootstrap | Root / Product 起動、共通 service、Launcher、SceneGroupProvider、最初の入口画面への遷移 | GameSession state、Content MainScene 固有実装、HUD / Popup、実ゲームシーンへの直遷移 |
| GameSession | NewGame / Continue / Load で生成される state、Application service、ゲームセッション設定 Repository | Product 全体の常駐基盤、Scene-owned component、View 実体 |
| `GameConstants` | コード不変条件、アルゴリズム係数、ドメイン共通の最低値、単位変換の基準値 | 調整されるゲームバランス、初期状態、マップサイズ、速度、Prefab address、表示設定 |
| ScriptableObject | Designer / Inspector で調整する設定値、Visual 設定、Scene 単位の設定入力 | Domain / Application へ直接渡す runtime dependency、状態、cache、コンテンツ Prefab 一覧 |
| Settings class | SO から変換された runtime 用の不変設定、DI で Application / View に渡す値 | UnityEngine.Object、Addressable handle、実行時に変わる状態 |
| Master / Spec / Definition | コンテンツ発生元が所有する不変データ、Prefab address、visual id、攻撃仕様、施設種別設定 | シーン横断の一括 Prefab catalog、現在 HP、現在位置、cache |
| Addressable | Prefab / Material / Sprite / View などロード対象アセット | ゲームバランス判定そのもの、Domain Entity |
| LifetimeScope | Composition Root、Scene-owned component、設定 SO、DI 登録 | コンテンツ Prefab 一覧、Popup / HUD / View 実体の直接操作口、ゲームロジックの分岐 |
| Runtime State / Entity | 現在値、状態遷移、HP、位置、予約、進行状況 | Master から引ける不変値のコピー、表示専用の調整値 |

---

## `GameConstants` に残してよいもの

`GameConstants` は「ゲームを調整する場所」ではない。コードが成立するための不変条件、単位、アルゴリズムの基準だけを残す。

残してよい例:

- `MapCellWidthMeters`: グリッド座標とワールド座標の変換基準
- `GameScheduleTicksPerDay`: 1 日を構成する tick 数のような時間単位の基準
- `DungeonFloorSeedMultiplier`: 乱数 seed 分散用のアルゴリズム係数
- `KillExperienceRewardMinimum`: 報酬計算式の下限など、計算式の意味を表す値
- `DefaultInventorySlotCapacity`: ドメインとしての安全なデフォルト容量

移動すべき例:

- 初期所持金、初期施設価格、初期施設容量
- 地上マップサイズ、ダンジョンフロアサイズ、部屋サイズ
- Actor 移動速度、到着距離、拾得半径
- Spawn 間隔、最大出現数
- 戦闘範囲、Projectile hit radius
- 宿泊料金、満足度増減、回復速度

移動先:

- ゲーム全体で調整するなら `WorldGameSettingsSO` -> runtime `Settings class`
- コンテンツごとに異なるなら Master / Spec / Definition
- 表示・カメラ・レイヤーなど View 固有なら View settings SO

---

## ScriptableObject の使い方

ScriptableObject は「Unity Editor で設定を入力する器」として使う。
Domain / Application のロジックは SO を知らず、通常の settings class を受け取る。

推奨:

```csharp
public sealed class WorldGameSettingsSO : ScriptableObject
{
    [Header("Spawn Balance")]
    [SerializeField] int adventurerSpawnIntervalTicks = 5;

    public SpawnBalanceSettings ToSpawnBalanceSettings()
    {
        return new SpawnBalanceSettings(adventurerSpawnIntervalTicks);
    }
}
```

```csharp
builder.RegisterInstance(worldGameSettingsSO.ToSpawnBalanceSettings()).AsSelf();
```

禁止:

```csharp
public sealed class SpawnScheduledAdventurerOrchestrator
{
    public SpawnScheduledAdventurerOrchestrator(WorldGameSettingsSO settingsSO)
    {
    }
}
```

理由:

- Application が Unity asset に依存する
- EditMode test が Unity asset の存在に引きずられる
- runtime state と editor setting の境界が曖昧になる

---

## Master / Spec に置くべきもの

コンテンツによって変わる値は、そのコンテンツを発生させる Master / Spec / Definition に置く。

例:

- 銃の Projectile Prefab address -> 銃または WeaponType の Master / Spec
- 弓の矢 Prefab address -> 弓または ProjectileSpec
- スキルの AreaEffect Prefab address -> SkillMaster / AttackAreaSpec
- 階段・施設・環境 Prop の Prefab address -> Cell / Facility / EnvironmentProp の Master
- 武器ごとの projectile speed -> WeaponTypeCombatMaster / ProjectileSpec
- 攻撃範囲や持続時間 -> AttackAreaSpec / SkillMaster

禁止:

```csharp
public sealed class WorldContentPrefabConfigSO : ScriptableObject
{
    [SerializeField] string arrowProjectileAddress;
    [SerializeField] string bulletProjectileAddress;
    [SerializeField] string scytheAreaEffectAddress;
}
```

理由:

- World が全コンテンツ種別を知ることになる
- コンテンツ追加のたびに横断設定が増える
- 発生元から見た「なぜその Prefab なのか」が追跡できない

---

## LifetimeScope のリファクタリング基準

LifetimeScope は DI の組み立て場所であり、コンテンツ catalog ではない。

置いてよいもの:

- Scene-owned component
- Settings SO
- Scene / ModuleScene entry point
- Composition Root として必要な登録処理

置いてはいけないもの:

- Projectile / AreaEffect / Prop / UI View Prefab の個別参照
- Popup / HUD / View 実体を他クラスが直接操作するための登録
- Master / Spec で解決できるアドレス一覧
- ゲームループ内の実行分岐

判定:

- コンテンツ種別が 1 つ増えたときに LifetimeScope の field が増えるなら設計ミス
- Prefab の種類が増えたときは Master / Spec / Addressable Factory / Pool 側だけが増えるべき
- Popup / HUD は Presenter / Pool / Factory を通して操作し、View 実体を広く Inject しない

---

## ScriptableObject の Inspector 整理

SO の項目が 5 個を超える、または調整意図が複数カテゴリに分かれる場合は `[Header]` でグルーピングする。

推奨グループ例:

- `Ground Map Generation`
- `Dungeon Map Generation`
- `Initial World`
- `Initial Facilities`
- `Initial Guild Inventory`
- `Inn Balance`
- `Actor Simulation`
- `Spawn Balance`
- `Combat Balance`
- `Camera`
- `Actor Focus`
- `Visual Entries`

ルール:

- Header 名は「何を調整するか」を表す
- 実装都合の名前にしない
- 同じカテゴリの値を離して置かない
- SO から runtime settings へ変換するメソッドもカテゴリ単位で分ける

---

## フォルダ細分化の判断基準

クラス数が増えたフォルダは、責務境界が見えにくくなり、レビュー時に「どのクラス群が同じ変更理由を持つのか」を追いづらくなる。
リファクタリングレビューでは、ファイル数だけでなく「同じ階層に複数の責務が混在しているか」を確認する。

目安:

- 1 フォルダ直下にクラスが 20 個以上ある場合は、サブフォルダ分割を検討する
- 1 フォルダ直下にクラスが 40 個以上ある場合は、原則として何らかのフォルダ分割を行う
- 40 個以上でも分割しない場合は、分割しない理由と次に見直す条件を review / roadmap に記録する

分割単位の候補:

- 機能単位: `Actor`, `Camera`, `Combat`, `Map`, `HUD`, `Popup`, `Projectile`, `AreaEffect`
- レイヤー内責務単位: `Presenter`, `View`, `Pool`, `Factory`, `Settings`, `Input`, `Debug`
- 寿命単位: scene-owned component、runtime service、addressable view、temporary view object
- 依存方向単位: View 実体、Presenter、設定 SO、純粋な変換・Mapper

例:

```text
Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World
  Actor/
  Camera/
  Combat/
  Map/
  HUD/
  Popup/
  Projectile/
  AreaEffect/
  Settings/
  Debug/
```

分割時の注意:

- namespace をフォルダ名だけに機械的に合わせるのではなく、既存プロジェクトの namespace 方針に従う
- Unity の `.meta`、Prefab / Scene 参照、asmdef、生成コードへの影響を確認する
- ファイル移動だけで責務が整理されたことにしない。分割後も依存方向と命名が責務を表していることを確認する
- 循環参照や相互依存が強いクラス群は、フォルダ分割より先に責務分離を検討する
- 分割によって参照切れが起きやすい Unity asset / scene / prefab がある場合は、移動後に compile / EditMode test / PlayMode を確認する

確認コマンド例:

```powershell
Get-ChildItem Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World -Filter *.cs | Measure-Object
```

---

## リファクタリングレビュー手順

1. 対象領域の `GameConstants` 参照を列挙する
2. 対象領域の `SerializedField` を列挙する
3. 対象領域の Master / Spec / Settings / State を列挙する
4. 対象フォルダ直下のクラス数を確認し、20 個以上なら分割要否を判断する
5. 各値について、以下を記録する

```md
### {値または参照名}

現在の置き場所:

本来の所有者:

分類:
- [ ] コード不変条件
- [ ] ゲーム全体設定
- [ ] View 設定
- [ ] コンテンツ定義
- [ ] runtime state
- [ ] test fixture

移動先:

理由:

完了条件:
- [ ] runtime 参照が移動先を使っている
- [ ] 古い定数 / field / 互換 API が削除されている、または残す理由が明記されている
- [ ] compile / test / PlayMode で確認されている
```

---

## 完了条件

リファクタリング完了前に以下を確認する。

- [ ] 調整対象の値が `GameConstants` やインライン数値に残っていない
- [ ] `GameConstants` に残した値はコード不変条件として説明できる
- [ ] SO は runtime settings class へ変換され、Domain / Application が SO に直接依存していない
- [ ] コンテンツ Prefab address は発生元 Master / Spec / Definition から解決されている
- [ ] LifetimeScope にコンテンツ Prefab 一覧や UI View 実体が残っていない
- [ ] 別 Scene / 別 LifetimeScope 所有の Canvas / View / Presenter / Pool / scene-owned component を直接参照・操作していない
- [ ] SO の Inspector 項目が `[Header]` で調整しやすく整理されている
- [ ] 1 フォルダ直下のクラス数が 20 個以上の箇所について、分割要否を判断した
- [ ] 1 フォルダ直下のクラス数が 40 個以上の箇所について、フォルダ分割済み、または分割しない理由と見直し条件を記録した
- [ ] System / Bootstrap と GameSession / Content の配置境界が守られている
- [ ] 入口画面が存在する場合、Launcher / bootstrap から実ゲームシーンへ直遷移していない
- [ ] 互換 constructor / 旧 API / 旧定数を残した場合、残す理由と削除条件が記録されている
- [ ] `rg "GameConstants\\."` や `rg "SerializedField"` などで残存箇所を確認した
- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] 変更リスクに応じて EditMode test / PlayMode 確認を実行している

---

## 記録先

リファクタリングレビュー結果は、対象に応じて以下のいずれかへ記録する。

- マイルストーン完了後の整理: `docs/self-review/`
- 次マイルストーンへ送る整理項目: `docs/roadmap/`
- 設計として恒久化する判断: `docs/guidelines/`
- タスク中に発見した設計確認: `review/{task_id}_question.md`

同じ種類の指摘が 2 回以上出た場合は、個別対応ではなく guideline 化またはハードゲート化を検討する。

# Scene Design

## 目的

このドキュメントは DungeonInn の Scene / ModuleScene の責務境界を定義する。
特に、World 空間に追従する表示と画面固定 UI を分離し、World MainScene に View 補助表示の関心を置かないための基準を明文化する。

## 基本方針

- MainScene はゲーム内容そのものの表示と操作の入口を持つ。
- ModuleScene は MainScene から分離された補助表示、HUD、Popup、入力補助、共通 UI を持つ。
- World MainScene は Screen Space Canvas、HUD、Popup、ActorStatusView、DamageNumberView を直接所有しない。
- World MainScene が持ってよいのは、World の 3D 表示、カメラ、入力、NavMesh、World 側 View Registry、ModuleScene へ公開する読み取り専用 Provider である。
- ModuleScene が World の具体 View / Registry / Camera Controller を直接参照してはならない。
- MainScene と ModuleScene の連携は、親 scope または MainScene scope に登録された抽象 interface / Application service / DataProvider 経由に限定する。

## Scene 構成

```text
Product
  -> Title MainScene
  -> GameSessionLifetimeScope
      -> World MainScene
      -> GameUI ModuleScene
      -> GameHUD ModuleScene
      -> ScreenStack ModuleScene
```

## World MainScene

World MainScene は、ゲーム世界そのものを 3D 空間に描画する MainScene である。

責務:

- Ground / Dungeon の Map 表示
- Actor / Monster / Projectile / AreaEffect / Facility / Prop の World 表示
- World Camera と camera follow
- World 入力処理
- NavMesh / A* など World 表示・移動に必要な scene adapter
- ActorView / MapLayerView / ProjectileView など World 固有 View の registry
- ModuleScene が必要とする読み取り専用 Provider の実装

持たないもの:

- Screen Space Canvas
- ActorStatusView
- DamageNumberView
- Popup
- Button / menu / log などの画面固定 UI
- GameUI / GameHUD の Presenter / Pool / View

World MainScene が ModuleScene に公開してよい情報は、具体 View ではなく抽象化された読み取り専用情報に限る。

例:

- `IActorWorldAnchorProvider`: ActorId から頭上アンカー座標を返す
- `IActiveLayerProvider`: 現在表示中の layer を返す
- `IWorldHudCameraProvider`: billboard 等に必要な camera 情報を返す

## GameUI ModuleScene

GameUI は Screen Space Canvas 上の画面固定 UI 専用 ModuleScene とする。

責務:

- 所持金、日数、時刻、ゲーム速度などの固定 HUD
- メニュー、ボタン、設定 UI
- 選択中 Actor / Facility の詳細 Popup
- Event log / notification log
- ScreenStack と組み合わせる画面 UI

持たないもの:

- Actor に常時追従する status view
- World 座標から発生する damage number
- Actor の頭上 icon / speech bubble / state marker
- World 空間上の SpriteRenderer / MeshRenderer 表示

GameUI は Canvas rebuild の影響を受けるため、Actor 数に比例して毎フレーム位置更新される UI を置かない。

## GameHUD ModuleScene

GameHUD は World 空間に重ねて表示される 3D HUD 専用 ModuleScene とする。
World MainScene の中に UI 関心を置かず、かつ Canvas の大量 rebuild を避けるために分離する。

責務:

- ActorStatusView
- DamageNumberView
- Actor 頭上 icon
- 状態異常 icon
- speech bubble / thought bubble
- interact marker
- 将来の selection ring や target marker のうち、World 空間に追従するもの

表示方式:

- `SpriteRenderer`
- `MeshRenderer`
- `TextMeshPro 3D`
- MaterialPropertyBlock
- pooled GameObject

GameHUD の View は World 空間に存在するが、World MainScene の所有物ではない。
GameHUD LifetimeScope が Presenter / Pool / Factory / View prefab を所有する。

## ActorStatusView の配置方針

ActorStatusView は Actor に追従し、Actor 数に比例して増え、毎フレーム座標更新される。
そのため Canvas GameUI ではなく GameHUD に配置する。

理想構成:

```text
WorldActorStatusPresenter
  -> IActorStatusViewDataProvider
  -> IActorWorldAnchorProvider
  -> IActiveLayerProvider
  -> ActorStatusViewPool
      -> ActorStatusView
```

責務:

- Presenter は表示対象 Actor、表示/非表示、ViewData の適用を判断する。
- Presenter は World の具体 ActorView を参照しない。
- Pool は ActorId ごとの View 割り当てと解放を管理する。
- View は HP bar、名前、状態 icon、billboard、scale 補正だけを扱う。

完了条件:

- Actor 数が増えても Canvas の rebuild 対象が増えない。
- Actor が despawn されたら対応する status view が release される。
- Actor が active layer 外にいる場合は非表示または pool release される。

## DamageNumberView の配置方針

DamageNumberView は一時的な World 空間エフェクトであり、大量に発生する可能性がある。
Canvas ではなく GameHUD の pooled SpriteRenderer 表示とする。

理想構成:

```text
DamageNumberPresenter
  -> IEventSubscriber
  -> IActorWorldAnchorProvider
  -> IActiveLayerProvider
  -> IDamageNumberViewSpawner
      -> DamageNumberViewPool
          -> DamageNumberView
```

責務:

- Presenter は `CombatAttackOccurred` などの event を購読し、表示すべき damage だけを spawn する。
- Presenter は damage の見た目や animation を持たない。
- Pool は上限数を持ち、同時表示数が上限を超えた場合は古い表示の再利用または新規 spawn の drop を行う。
- View は数字 atlas の桁分割、桁 Sprite の配置、上昇、fade、scale、billboard を扱う。

表示位置:

- Actor の頭上 anchor を基準にする。
- 同一 actor に短時間で複数 damage が発生した場合は、spawn index や乱数ではなく deterministic な offset queue で重なりを避ける。
- 座標計算は Provider が返す World anchor と View 側 offset の合成に限定する。

## GameHUD と World の連携

GameHUD は World の具体 object を探さない。

禁止:

- `FindObjectOfType` / `FindObjectsOfType` で World の View を探す
- GameHUD から `WorldActorViewRegistry` を直接 inject する
- GameHUD から `WorldCameraController` を直接 inject する
- World MainScene から GameHUD の Pool / Presenter / View を操作する
- World の ActorView に damage number / status view の child object を生やす

許可:

- `IActorWorldAnchorProvider` などの抽象 interface を読む
- Application の ViewDataProvider を読む
- `IEventSubscriber` で domain/application event を購読する
- GameHUD 内の Pool / Presenter / View を GameHUD LifetimeScope が所有する

## Provider 設計

World 追従 HUD の Provider は、ModuleScene が具体 World 実装を知らないための境界である。

候補:

```csharp
public interface IActorWorldAnchorProvider
{
    bool TryGetAnchorPosition(Guid actorId, WorldHudAnchorKind anchorKind, out Vector3 position);
}
```

```csharp
public enum WorldHudAnchorKind
{
    Feet,
    Center,
    Head,
    DamageOrigin
}
```

方針:

- Provider は読み取り専用とする。
- Provider は View 表示に必要な最小情報だけを返す。
- Provider の戻り値に View 実体、Transform、GameObject を含めない。
- World 側実装は ActorViewRegistry を使ってよいが、それは Provider の内部実装に閉じ込める。
- GameHUD 側は Provider の interface だけを見る。

## LifetimeScope 方針

```text
GameSessionLifetimeScope
  -> Application state / event / ViewDataProvider

WorldLifetimeScope
  -> World scene-owned View / Camera / Registry
  -> IActorWorldAnchorProvider implementation

GameUILifetimeScope
  -> Canvas UI Presenter / ViewFactory

GameHUDLifetimeScope
  -> 3D HUD Presenter / Pool / ViewFactory
```

依存方向:

- GameHUD は GameSession scope の Application state / event を読める。
- GameHUD は World scope が公開する抽象 Provider を読める。
- GameHUD は World の concrete class を読まない。
- World は GameHUD の concrete class を読まない。

## Addressables / Asset 方針

GameHUD の View prefab は GameHUD 専用 Factory / Pool が Addressables 経由でロードする。

例:

- `GameHUD/ActorStatusView`
- `GameHUD/DamageNumberView`

禁止:

- LifetimeScope に View prefab を `SerializedField` で直接持たせる
- World の prefab catalog に GameHUD 用 View prefab を混ぜる
- Canvas GameUI の Factory に 3D HUD prefab を混ぜる

## Performance 方針

Canvas GameUI:

- 画面固定 UI に限定する。
- Actor 数や damage 発生数に比例して毎フレーム移動する UI を置かない。

GameHUD:

- pooled object を前提にする。
- 頻繁な `Instantiate` / `Destroy` を避ける。
- 同時表示数の上限を設定する。
- per-frame update は active view のみ行う。
- Material / Sprite / Mesh の runtime 生成は初期化時または asset load 時に限定する。
- 大量表示が想定されるものは MaterialPropertyBlock または shared material の property 更新で扱う。

## 移行方針

Canvas GameUI に存在していた World 追従 UI は、以下の方針で GameHUD に移す。

1. DamageNumberView を GameHUD の pooled SpriteRenderer 表示に移す。
2. ActorStatusView を GameHUD に移す。
3. Canvas GameUI から Actor 追従座標更新処理を削除する。
4. World 追従表示は `IActorWorldAnchorProvider` と `IWorldHudCameraProvider` に依存させる。
5. GameUI は固定 HUD / Popup 専用として整理する。

互換性維持のために旧 Canvas 表示を残さない。
移行対象の責務が GameHUD に移ったら、旧 Presenter / Pool / prefab / Addressable entry は削除する。

## 判断基準

表示物をどの Scene に置くかは、以下で判断する。

| 表示物 | 配置先 | 理由 |
|---|---|---|
| 所持金 / 日数 / 速度 UI | GameUI | 画面固定 UI |
| Actor 詳細 Popup | GameUI / ScreenStack | 画面固定または modal |
| ActorStatusView | GameHUD | Actor に追従し、Actor 数に比例する |
| DamageNumberView | GameHUD | World 座標で発生し、大量表示される |
| 状態異常 icon | GameHUD | Actor に追従する |
| 施設内表示 / 建物 / Prop | World | World 実体の表示 |
| Projectile / AreaEffect | World | ゲーム世界の実体 |
| Selection ring | GameHUD または World | UI feedback なら GameHUD、実体表現なら World |

迷う場合は、次の基準を優先する。

- World 座標に追従するだけの補助表示なら GameHUD。
- ゲーム世界の実体そのものなら World。
- 画面固定なら GameUI。
- modal / stack 管理が必要なら ScreenStack。

## 完了前チェック

- [ ] World MainScene が GameUI / GameHUD の View / Pool / Presenter を直接参照していない。
- [ ] GameUI / GameHUD が World の concrete View / Registry / Controller を直接参照していない。
- [ ] World 追従 UI が Canvas GameUI に残っていない。
- [ ] GameHUD の View は pooled object として生成・再利用されている。
- [ ] GameHUD の View prefab は GameHUD 専用 Factory / Pool が Addressables 経由でロードしている。
- [ ] Actor 数や damage 数に比例する表示が Canvas rebuild を発生させない。
- [ ] `uloop.cmd compile --project-path Client` が成功している。


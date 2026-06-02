# Milestone 11.1 Roadmap - GameUI / GameHUD Responsibility Split

## ゴール

画面固定 UI と World 内オブジェクトに追従する HUD の責務を分離し、名称と Scene 構成を実装責務に合わせる。

- 現在 `GameHUD` と呼ばれている Screen Space Canvas の UI Scene は `GameUI` にリネームする。
- `GameHUD` は World 座標に追従する HUD Scene の名称として使う。
- `GameHUD3D` は仮称として扱い、最終実装・Scene 名・Namespace・Addressable key には残さない。
- `ActorStatusView` と `DamageNumberView` は Canvas UI ではなく、`GameHUD` Scene が管理する World-space 表現へ移動する。
- 互換性維持だけを目的とした旧 API、旧 Scene 名、旧 Prefab、旧 Addressable key は残さない。

## 背景

現在の `GameHUD` は実態として Screen Space Canvas の固定 UI を管理している。  
一方で、Actor の頭上表示やダメージ数値のような「World 内対象に追従する表示」こそが HUD の責務に近い。

このまま `GameHUD` が Canvas UI と World 追従表示の両方を持つと、Scene 名・責務・依存方向が曖昧になる。  
Milestone 11.1 では、今後の表示追加に耐えるように以下の命名へ整理する。

| 名称 | 責務 |
|---|---|
| `GameUI` | Screen Space Canvas の固定 UI。操作ボタン、ログ、詳細パネル、Popup、Window、画面固定ステータスを管理する。 |
| `GameHUD` | World-space HUD。ActorStatus、DamageNumber、状態アイコン、吹き出し、ターゲットマーカーなど、World 内対象に追従する表示を管理する。 |
| `GameHUD3D` | 仮称。実装完了時点で残さない。 |
| `World` | Actor、Facility、Map、Camera などゲーム世界そのものを表示する。HUD View の生成・プール・演出責務を持たない。 |

## 対象範囲

- `GameHUD` から `GameUI` への Scene / LifetimeScope / EntryPoint / Namespace / Folder / Addressable key のリネーム。
- World-space HUD 用の `GameHUD` ModuleScene の追加。
- `DamageNumberView` を `GameHUD` の SpriteRenderer ベースの pooled object に移動。
- `ActorStatusView` を `GameHUD` の World-space 表示に移動。
- `docs/design/scene-design.md` と関連する設計索引の名称更新。
- Unity Scene、Prefab、Addressable、Lighthouse Scene 定義の整合性修正。
- `SceneGroupProvider` の `World -> GameUI + GameHUD` ModuleScene 登録更新。
- コンパイル、EditMode test、通常 UI 経路での Play 動作確認。

## 対象外

- ダメージ数値や ActorStatus のアート品質改善。
- 新しい戦闘ロジック、AI ロジック、施設ロジックの追加。
- SaveData 形式変更。ただし Scene 参照名の変更に伴う設定更新は対象に含める。
- 旧 `GameHUD` 名を互換目的で残す暫定ブリッジ。
- `GameHUD3D` 名での Scene / Namespace / Folder / Prefab の確定。

## 現行コード調査結果

Milestone 11.1 は新規追加ではなく、現行 `GameHUD` の責務を分解して再配置する作業である。

### 現在の `GameHUD`

現行 `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/` には、以下が混在している。

Canvas 固定 UI 側:

- `GameHUDModuleScene`
  - `ProductCanvasModuleSceneBase` を継承し、`Canvas HUDCanvas` を所有している。
- `WorldHudView` / `WorldHudPresenter`
  - day/time、gold、pause、speed、DungeonInfo / GuildManagement / Market / Settings button を表示する。
- `SelectedActorInspectorView` / `SelectedActorInspectorPresenter`
  - 選択 Actor の詳細パネルを Canvas 上に表示する。
- `PlayerEventLogView` / `PlayerGameEventLogPresenter`
  - Player event log を Canvas 上に表示する。
- `InnStatusPanelView` / `InnStatusPanelPresenter`
  - 宿屋ステータス系の固定パネルを Canvas 上に表示する。
- `MinimapView` / `MinimapPresenter`
  - Minimap を Canvas 上に表示する。
- `ScreenStack/`
  - Milestone 9 の ScreenStack window 用 ViewData factory / open service / window data を持つ。

World 追従 HUD 側:

- `WorldActorStatusPresenter`
  - `IActorStatusViewDataProvider`、`GetActorStatusSummaryQuery`、`IActorScreenPositionProvider`、`IActiveLayerProvider` を使い、Actor ごとの status view を screen position に置く。
- `ActorStatusView`
  - `UnityEngine.UI.Image` と `RectTransform` を使う Canvas View。
- 旧 `ActorHUDViewPool`
  - 旧 Canvas root の子として `ActorStatusView` を生成・プールしていた。
- `DamageNumberPresenter`
  - `CombatAttackOccurred` を購読し、target Actor の screen position へ DamageNumber を出す。
- `DamageNumberView`
  - `UnityEngine.UI.Image`、`CanvasGroup`、`RectTransform`、Runtime `Sprite.Create` による Canvas View。
- 旧 `DamageNumberViewPool`
  - 旧 Canvas root の子として `DamageNumberView` を生成・プールしていた。

この状態では `GameHUD` が「Canvas 固定 UI」と「World 追従 HUD」の両方を持っている。  
Milestone 11.1 では、Canvas 固定 UI を `GameUI` へ移動し、`GameHUD` は World-space HUD 専用に作り直す。

### 現在の Scene / LifetimeScope

- `SceneGroupProvider` は常駐 ModuleScene として `ScreenStack` をロードし、`World` MainScene では追加 ModuleScene として `GameHUD` をロードしている。
- `DungeonInnModuleSceneId.g.cs` には `GameHUD` が生成済みである。`GameUI` は未生成のため、Lighthouse の Scene 定義側を更新して生成し直す必要がある。
- `WorldLifetimeScope` は `WorldActorScreenPositionProvider` と `WorldActorScreenPositionProviderEntryPoint` を登録し、proxy registry 経由で `GameHUD` へ screen position provider を渡している。
- `WorldLifetimeScope` は `WorldActiveLayerProvider` と `WorldActiveLayerProviderEntryPoint` も登録し、active layer を proxy registry 経由で渡している。
- `GameHUDLifetimeScope` は固定 UI presenter と World 追従 presenter を同じ scope に登録している。

### 現在の bridge

現行 bridge は screen-space 前提である。

- `IActorScreenPositionProvider`
  - `LayerPosition` から `Vector2 screenPosition` を返す。
- `ActorScreenPositionProviderProxy`
  - World 側 provider を proxy として保持する。
- `IActiveLayerProvider`
  - active layer id を返す。
- `ActiveLayerProviderProxy`
  - World 側 provider を proxy として保持する。

Milestone 11.1 では `IActorScreenPositionProvider` を `GameHUD` の主経路として使わない。  
新しく `IActorWorldAnchorProvider` / `IWorldHudCameraProvider` / `IWorldLayerStateProvider` を追加し、World-space HUD は world anchor と camera を使って配置する。

### 現在の Damage event

`CombatAttackOccurred` は以下を持つ。

- `AttackerActorId`
- `TargetActorId`
- `Damage`
- `TargetRemainingHp`

View 用 world position / screen position / offset / sorting 情報は持っていない。  
したがって event DTO の破壊的変更は不要。`DamageNumberPresenter` 側が `TargetActorId` から world anchor を解決するように変更する。

### 現在の ActorStatus

`ActorStatusView` は Canvas 用の `Image` / `RectTransform` 実装である。  
Milestone 11.1 ではこの class を延命せず、SpriteRenderer ベースの `ActorStatusView` として置き換える。互換用 `CanvasActorStatusView` は作らない。

### 現在の DamageNumber

`DamageNumberView` は Canvas 用の `Image` / `CanvasGroup` / `RectTransform` 実装であり、`Texture2D digitAtlas` を Runtime で `Sprite.Create` している。  
Milestone 11.1 では Runtime slicing を削除し、digit sprite は `DamageNumberView.prefab` の serialized reference として配線する。

## Phase 一覧

| Phase | 内容 | 成果物 |
|---|---|---|
| 1 | 設計ドキュメント更新 | Scene 責務と最終名称が docs に反映されている |
| 2 | 既存 `GameHUD` を `GameUI` へリネーム | Canvas UI Scene が `GameUI` として動作する |
| 3 | 新 `GameHUD` ModuleScene 作成 | World-space HUD 専用 Scene が存在する |
| 4 | DamageNumber を `GameHUD` へ移動 | ダメージ数値が SpriteRenderer pool で表示される |
| 5 | ActorStatus を `GameHUD` へ移動 | Actor 追従ステータスが Canvas から外れる |
| 6 | 旧名称・旧責務の削除 | `GameHUD3D` と旧 Canvas `GameHUD` が残っていない |
| 7 | 検証・セルフレビュー | compile/test/Play 確認とレビュー記録が完了している |

## Phase 1 - 設計ドキュメント更新

### 目的

実装前に Scene の責務と名称を確定し、後続タスクが仮称に引きずられない状態にする。

### 作業

- `docs/design/scene-design.md` の `GameHUD` / `GameHUD3D` 記述を以下へ更新する。
  - 旧 `GameHUD`: `GameUI`
  - 旧 `GameHUD3D`: `GameHUD`
- `docs/design/ai-class-relation-index.md` に Scene / View の入口がある場合は名称を更新する。
- 関連 roadmap や task で今後参照される設計説明は final name を使う。
- 過去の作業ログとしての記録は無理に書き換えない。ただし、現行設計として読まれる文書に旧名称を残さない。

### 完了条件

- `GameUI` が Canvas UI、`GameHUD` が World-space HUD として説明されている。
- `GameHUD3D` は仮称または削除対象としてのみ登場し、現行設計名として登場しない。

## Phase 2 - 既存 `GameHUD` を `GameUI` へリネーム

### 目的

Screen Space Canvas の固定 UI を、責務に合う `GameUI` 名へ移行する。

### 理想構成

- `View/Scene/ModuleScene/GameUI/`
- `GameUILifetimeScope`
- `GameUIEntryPoint`
- `GameUIViewFactory`
- `GameUIPresenter`
- Addressable group / key は `GameUI/...`

### 作業

- 既存 `GameHUD` 配下の Canvas UI 実装を `GameUI` 配下へ移動する。
- Class / Namespace / asmdef reference / Scene asset / Prefab reference / Addressable key を `GameUI` に統一する。
- Lighthouse の generated file を手編集せず、Scene 設定側を更新して再生成または既存生成フローに従う。
- Title / World / ScreenStack などから Canvas UI Scene を参照する箇所を `GameUI` へ更新する。

### 禁止

- `GameHUD` という旧名の Canvas UI wrapper を互換目的で残さない。
- `GameHUD` Namespace から `GameUI` 実装を re-export しない。
- Addressable key の旧名 alias を残さない。

### 完了条件

- Screen Space Canvas の固定 UI は `GameUI` Scene としてロードされる。
- `GameUI` は World 内 Actor 追従表示を生成しない。

## Phase 3 - 新 `GameHUD` ModuleScene 作成

### 目的

World 内対象に追従する HUD 表示を `World` から分離し、View の関心を `GameHUD` Scene に閉じる。

### 理想構成

- `View/Scene/ModuleScene/GameHUD/`
- `GameHUDLifetimeScope`
- `GameHUDEntryPoint`
- `GameHUDPresenter`
- `DamageNumberHudPresenter`
- `WorldActorStatusPresenter`
- `GameHUDViewFactory`
- `GameHUDPool<TView>`

### 依存方向

- `GameHUD` は Application Event / Provider interface を購読して表示を更新する。
- `World` は HUD View の生成、プール、演出を知らない。
- `GameHUD` は World の具体 View class に直接依存しない。
- Actor の追従位置は interface で取得する。
- `WorldLifetimeScope` は World が所有する情報を provider interface として scoped 登録する。
- `GameHUDLifetimeScope` は provider interface のみを inject し、World の具象 View / Controller / Registry を inject しない。
- `SceneGroupProvider` は `World` MainScene の ModuleScene として `GameHUD` をロードする。
- `GameHUD` は `World` と同じ SceneGroup でのみロードされる。provider が解決できない状態は通常到達しない構成不備として扱い、fallback object を生成しない。

### 必要な境界

- `IActorWorldAnchorProvider`
  - ActorId から World-space anchor position を返す。
  - Actor View の具体型を公開しない。
- `IWorldCameraProvider`
  - HUD の billboarding / screen-facing に必要な camera を返す。
- `IWorldLayerStateProvider`
  - Ground / Dungeon など active layer を返す。
- Damage event subscriber
  - 対象 ActorId、damage amount、必要であれば Domain / Application として意味を持つ hit source id または layer position を受け取る。
  - Unity world position、screen position、表示 offset、sorting 情報など View 表示都合の値は event DTO に含めない。
  - DamageNumber の表示位置は `GameHUD` が provider interface から解決する。

### 完了条件

- `GameHUD` Scene 単体の責務は World-space HUD に限定されている。
- `World` から `GameHUD` concrete class への参照がない。
- `GameHUD` から World concrete class への参照がない。
- `SceneGroupProvider` で `World -> GameHUD` の ModuleScene 登録が行われている。

## Phase 4 - DamageNumber を `GameHUD` へ移動

### 目的

ダメージ発生時の数値表示を Canvas ではなく SpriteRenderer ベースの pooled object として表示する。

### 理想構成

- `DamageNumberHudPresenter`
  - damage event を購読する。
  - 表示位置を決定する。
  - pool から `DamageNumberView` を取得して再生する。
- `DamageNumberView`
  - 数字 sprite を桁ごとに SpriteRenderer で表示する。
  - world position、scale、alpha、上昇量、寿命を自分の表示状態として持つ。
  - 再生完了時に pool へ返却される。
- `DamageNumberView.prefab`
  - `DamageDigits.png` から分割した 0-9 の sprite 参照を serialized reference として保持する。
  - `GameHUDViewFactory` は digit sprite を個別ロードせず、`DamageNumberView.prefab` だけを Addressable から読み込む。

### 数字アセット設定

- 元画像は `Client/Assets/DungeonInn/Runtime/Art/Sprites/UI/DamageDigits.png` として配置する。
- Sprite import は Multiple Sprite とし、0-9 の各数字 sprite として分割する。
- Pivot は Center、FilterMode は Point、透明背景あり、pixels per unit は 32 に統一する。
- Sprite 分割、import 設定、Addressable 登録は Editor / OneShot / setup script で行い、Runtime で Texture2D を切り出して Sprite を生成しない。
- Addressable key は `GameHUD/DamageNumberView` を設定する。digit sprite は prefab の serialized reference で配線し、個別 Addressable にはしない。

### 表示仕様

- Actor に紐づく DamageNumber は、damage event の target ActorId から `IActorWorldAnchorProvider` で取得した anchor 座標を基準に表示する。
- Actor に紐づかない DamageNumber が必要になった場合は、この milestone では扱わず、Domain / Application として意味を持つ layer position から HUD anchor へ変換する provider を別途設計する。
- 複数同時発生時は deterministic な小さな offset を加えて重なりを軽減する。
- Camera に対して読みやすい向きを維持する。
- Pool 上限を持ち、上限超過時は古い表示を再利用する。

### 禁止

- `Canvas`, `RectTransform`, `CanvasGroup`, `Image` を DamageNumber の実表示に使わない。
- `World` 側で DamageNumber prefab を instantiate しない。
- DamageNumber 表示のために Domain state を変更しない。
- Damage event DTO に Unity world position、screen position、表示 offset、sorting 情報を入れない。
- Runtime で数字画像を Texture2D slicing して Sprite 生成しない。

### 完了条件

- ダメージイベント発生時、`GameHUD` Scene 内に pooled `DamageNumberView` が表示される。
- DamageNumber の表示終了後、object が破棄されず pool に戻る。
- 数字 sprite は Addressable 経由でロードされ、Runtime 生成ではない。
- DamageNumber の表示位置は event DTO の View 座標ではなく provider interface から解決される。

## Phase 5 - ActorStatus を `GameHUD` へ移動

### 目的

Actor に追従する status 表示を Canvas から外し、Actor 数に比例して増える表示を `GameHUD` Scene の責務にする。

### 理想構成

- `WorldActorStatusPresenter`
  - Actor spawn / despawn / status change / active layer change を購読する。
  - `ActorStatusViewPool` から view を取得し、active actor ごとの lease を actor-keyed registry で管理する。
- `ActorStatusView`
  - HP bar、background、state icon は SpriteRenderer で表示する。
  - name label は Milestone 11.1 の必須対象外とし、必要になった場合は bitmap font SpriteRenderer または TextMeshPro 3D の採用判断を別途設計する。
  - TextMeshPro 3D を使う場合は、Canvas rebuild 回避のための例外として扱い、sorting、material、camera-facing の制約を実装前に明記する。
  - Actor anchor が存在しない、または非 active layer の場合は非表示にする。

### 表示仕様

- Actor の head anchor に追従する。
- Actor が Dungeon / Ground を移動した場合、active layer に応じて表示を切り替える。
- Actor despawn 時に view を release する。
- Camera に対して読みやすい向きを維持する。

### 禁止

- `GameUI` の Canvas で Actor ごとの追従 status を持たない。
- `ActorStatusView` が Actor の Domain object を直接変更しない。
- Actor View の child として HUD View をぶら下げない。

### 完了条件

- ActorStatus は `GameHUD` Scene 内に生成される。
- Actor 数が増えても Canvas の rebuild 対象にならない。
- HP bar、background、state icon が SpriteRenderer で構成されている。
- name label は実装しないか、別途設計された描画方式と制約に従っている。

## Phase 6 - 旧名称・旧責務の削除

### 目的

互換目的の残骸を消し、以後の実装が古い名称に戻らない状態にする。

### 作業

- `GameHUD3D` という Scene / Folder / Namespace / Class / Prefab / Addressable key を残さない。
- Canvas UI の旧 `GameHUD` 名を残さない。
- `GameHUD` 配下に Screen Space Canvas 固定 UI を置かない。
- `GameUI` 配下に Actor 追従 HUD を置かない。
- 旧 API alias、obsolete wrapper、migration-only bridge を削除する。

### 静的確認

以下の検索を実行し、意図しない残存がないことを確認する。

```powershell
rg -n "GameHUD3D" Client/Assets/DungeonInn
rg -n "GameHUD" Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameUI
rg -n "GameUI" Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD
rg -n "Canvas|RectTransform|CanvasGroup|Image" Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD
rg -n "WorldCameraController|MapLayerViewRegistry|WorldActor.*View|ActorView" Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD
```

`GameHUD` 内の `Canvas|RectTransform|CanvasGroup|Image` は原則 0 件にする。  
やむを得ず editor-only utility などで出る場合は、実表示責務ではないことをレビューで説明する。

`GameHUD3D` は実装成果物では 0 件にする。  
docs 内では、この milestone の削除対象説明として登場する場合のみ許容する。

World concrete class 名の検索は 0 件を原則とする。  
出る場合は provider interface の実装側ではなく `GameHUD` 側に具象依存が入り込んでいないか確認する。

## Phase 7 - 検証・セルフレビュー

### 必須確認

- `uloop.cmd compile --project-path Client`
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
- 通常 UI 経路で StartGame を実行し、`[World] GameWorldState initialized` 後 30 秒以上 Play mode でエラーログがないことを確認する。

### 追加確認

- DamageNumber がダメージ発生位置付近に表示される。
- ActorStatus が Actor の移動に追従する。
- Ground / Dungeon の active layer 切り替えで不要な HUD が残らない。
- Pool release 漏れで表示 object が増え続けない。
- `GameUI` の固定 UI がリネーム前と同等に表示される。

## 実装タスク案

以下の Task 1〜10 は実装順序と完了条件の一覧である。  
既存互換維持より、`GameUI` / `GameHUD` の最終責務を正とする。

### Task 1: Scene 名と docs の正典更新

目的:

`GameHUD` の名称を World-space HUD に使うため、Canvas 固定 UI の現行 Scene を `GameUI` として再定義する。

現行対象:

- `docs/design/scene-design.md`
- `docs/design/ai-class-relation-index.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/LighthouseGenerated/DungeonInnModuleSceneId.g.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/SceneGroupProvider.cs`
- 現行 `GameHUD` ModuleScene asset / scene 定義

対応:

- `docs/design/scene-design.md` の `GameHUD` / `GameHUD3D` 記述を final name に更新する。
- Lighthouse の Scene 定義側で `GameUI` ModuleScene を追加し、生成フローで `DungeonInnModuleSceneId.GameUI` を生成する。
- `.g.cs` は手編集しない。
- `SceneGroupProvider` は `ScreenStack` を常駐 ModuleScene とし、`World` MainScene で `GameUI` と `GameHUD` の両方をロードする形にする。

理想構成:

```text
RequireSceneModuleIds:
  ScreenStack

World additional ModuleScenes:
  GameUI
  GameHUD
```

完了条件:

- [ ] `DungeonInnModuleSceneId.GameUI` が生成済みである
- [ ] `DungeonInnModuleSceneId.GameHUD` は World-space HUD の ID として残っている
- [ ] `SceneGroupProvider` の `World` module list に `GameUI` と `GameHUD` が含まれている
- [ ] `.g.cs` を手編集していない
- [ ] docs で `GameUI` = Canvas 固定 UI、`GameHUD` = World-space HUD と説明されている

### Task 2: 現行 Canvas `GameHUD` を `GameUI` へ移動

目的:

Canvas 固定 UI を `GameUI` Scene / namespace / folder / Addressable key へ移す。

移動対象:

- `GameHUDModuleScene` -> `GameUIModuleScene`
- `GameHUDLifetimeScope` の Canvas 固定 UI 登録 -> `GameUILifetimeScope`
- `GameHUDEntryPoint` の Canvas 固定 UI 初期化 -> `GameUIEntryPoint`
- `GameHUDAddressableViewFactory` の Canvas 固定 UI prefab load -> `GameUIAddressableViewFactory`
- `WorldHudView` / `WorldHudPresenter`
- `SelectedActorInspectorView` / `SelectedActorInspectorPresenter`
- `PlayerEventLogView` / `PlayerGameEventLogPresenter`
- `InnStatusPanelView` / `InnStatusPanelPresenter`
- `MinimapView` / `MinimapPresenter`
- `ScreenStack/` 配下の GameUI から開く window open service / ViewData factory

移動先:

```text
Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameUI/
Client/Assets/DungeonInn/Runtime/Prefab/GameUI/
Addressable key: GameUI/...
```

対応:

- `GameUI` は `ProductCanvasModuleSceneBase` を継承し、`Canvas UICanvas` を所有する。
- 旧 `GameHUDModuleScene` の Canvas root 参照は `GameUIModuleScene.UICanvas` へ置き換える。
- `WorldHudViewAddress` などの Canvas UI Addressable key は `GameHUD/UI/...` から `GameUI/...` へ変更する。
- `GameHUDRenderingLayer` が Canvas UI 専用の名前なら `GameUIRenderingLayer` へ移動・リネームする。World-space `GameHUD` では使わない。

禁止:

- `GameHUD` namespace から `GameUI` 実装を re-export しない。
- 旧 `GameHUD/UI/...` Addressable key を alias として残さない。
- `GameUI` に `ActorStatusView` / `DamageNumberView` / World 追従 HUD pool を残さない。

完了条件:

- [ ] Canvas 固定 UI の scripts が `View/Scene/ModuleScene/GameUI/` 配下へ移動している
- [ ] Canvas 固定 UI prefab の Addressable key が `GameUI/...` へ移行している
- [ ] `GameUI` は Actor ごとの追従 status / damage number を生成しない
- [ ] `rg -n "ActorStatusView|DamageNumberView|DamageNumberPresenter|ActorHUDViewPool" Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameUI` が 0 件である

### Task 3: World-space `GameHUD` の scope を作り直す

目的:

`GameHUD` を World-space HUD 専用 ModuleScene として再構成する。

残す / 新設する対象:

- `GameHUDModuleScene`
- `GameHUDLifetimeScope`
- `GameHUDEntryPoint`
- `GameHUDViewFactory`
- `WorldActorStatusPresenter`
- `ActorStatusViewPool`
- `DamageNumberPresenter`
- `DamageNumberViewPool`

対応:

- `GameHUDModuleScene` は `ProductCanvasModuleSceneBase` を継承しない。
- `GameHUDModuleScene` は `Transform hudRoot` だけを scene-owned root として持つ。
- `GameHUDEntryPoint` は `IAsyncStartable` / `ITickable` とし、factory load、presenter initialize、per-frame world-space HUD 更新だけを行う。
- `GameHUDLifetimeScope` は Canvas 固定 UI presenter を登録しない。
- `GameHUDViewFactory` は `ActorStatusView` prefab と `DamageNumberView` prefab を `IAssetScope` 経由で読み込む。digit sprite は `DamageNumberView.prefab` の serialized reference として配線する。

禁止:

- `GameHUDModuleScene` に `Canvas` を持たせない。
- `GameHUDLifetimeScope` に `WorldHudPresenter` / `SelectedActorInspectorPresenter` / `PlayerGameEventLogPresenter` / `InnStatusPanelPresenter` / `MinimapPresenter` を登録しない。
- `GameHUD` から World 具象型を inject しない。

完了条件:

- [ ] `GameHUDModuleScene` が Canvas を所有していない
- [ ] `GameHUDLifetimeScope` が World-space HUD 関連型だけを登録している
- [ ] `GameHUDEntryPoint.Tick()` が ActorStatus / DamageNumber の更新だけを呼ぶ
- [ ] `rg -n "Canvas|RectTransform|CanvasGroup|Image|TextMeshProUGUI|LHButton" Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD` が実表示責務では 0 件である

### Task 4: Screen-position bridge を world-anchor bridge へ置き換える

目的:

Canvas 用 screen position provider を World-space HUD 用 anchor provider に置き換える。

現行対象:

- `IActorScreenPositionProvider`
- `ActorScreenPositionProviderProxy`
- `WorldActorScreenPositionProvider`
- `WorldActorScreenPositionProviderEntryPoint`
- `WorldActorStatusPresenter`
- `DamageNumberPresenter`

新規 / 置換対象:

- `IActorWorldAnchorProvider`
- `ActorWorldAnchorProviderProxy`
- `WorldActorWorldAnchorProvider`
- `WorldActorWorldAnchorProviderEntryPoint`
- `IWorldHudCameraProvider`
- `WorldHudCameraProvider`
- `IWorldLayerStateProvider`

対応:

- `WorldActorWorldAnchorProvider` は `ActorId` または `LayerPosition` から World-space anchor を返す。
- ActorStatus / DamageNumber の基本経路は `ActorId` から anchor を解決する。
- `WorldActorWorldAnchorProvider` の内部では `ActorViewDataStore` / `WorldActorViewRegistry` / `MapLayerViewRegistry` / `LayerPositionViewMapper` など World 側具象を使ってよい。これは World 側実装であり、GameHUD 側には公開しない。
- `IWorldHudCameraProvider` は HUD が向くべき camera / rotation 情報を返す。`GameHUD` から `WorldCameraController` を直接 inject しない。
- `IWorldLayerStateProvider` は `IActiveLayerProvider` の置き換えとして active layer を返す。
- 既存 `IActorScreenPositionProvider` は Canvas 固定 UI から使用されていないことを確認し、不要なら削除する。必要なら GameUI 専用名にリネームする。

完了条件:

- [ ] `GameHUD` 側 presenter が `IActorWorldAnchorProvider` を使っている
- [ ] `GameHUD` 側 presenter が `IActorScreenPositionProvider` を使っていない
- [ ] `GameHUD` 側に `WorldCameraController` / `MapLayerViewRegistry` / `WorldActorViewRegistry` の具象依存がない
- [ ] `WorldLifetimeScope` が world-anchor provider 実装を登録し、entry point が proxy へ登録・解除している

### Task 5: DamageNumber を SpriteRenderer pool へ置き換える

目的:

現行 Canvas `DamageNumberView` を削除し、SpriteRenderer digit object の pooled view に置き換える。

現行対象:

- `DamageNumberPresenter`
- `DamageNumberView`
- `DamageNumberViewPool`
- `IDamageNumberViewSpawner`
- `DamageNumberAnimation`
- Addressable `GameHUD/DamageNumberView`
- Runtime での `Texture2D digitAtlas` slicing

対応:

- `DamageNumberAnimation` は world-space 用 sample を返す形へ変更する。
  - `Vector3 WorldPosition`
  - `float Scale`
  - `float Alpha`
- `DamageNumberView` は `SpriteRenderer[]` を使って digit を表示する。
- `DamageNumberView` は 0-9 の sprite を prefab serialized reference として保持する。
- `DamageNumberView.Show(int damage, Vector3 worldPosition)` のように、screen / anchored position を受け取らない API にする。
- `DamageNumberViewPool` は `GameHUDModuleScene.HudRoot` の子として view を生成する。
- `DamageNumberPresenter` は `CombatAttackOccurred.TargetActorId` から `IActorWorldAnchorProvider` で anchor を取得し、DamageNumber を spawn する。
- `DamageNumberPresenter` は active layer と Actor visibility を provider / `IActorStatusViewDataProvider` で確認する。

アセット:

- `Client/Assets/DungeonInn/Runtime/Art/Sprites/UI/DamageDigits.png`
- Sprite sub-assets: 0-9 の digit sprite
- Digit sprite は個別 Addressable にせず、`DamageNumberView.prefab` の serialized reference として配線する。
- Prefab:
  - `Client/Assets/DungeonInn/Runtime/Prefab/GameHUD/DamageNumberView.prefab`
  - Addressable: `GameHUD/DamageNumberView`

禁止:

- `Texture2D digitAtlas` を Runtime slicing しない。
- `Image` / `CanvasGroup` / `RectTransform` を使わない。
- `GameUI` Canvas の子として生成しない。

完了条件:

- [ ] `DamageNumberView` が `SpriteRenderer` で digit を表示している
- [ ] `DamageNumberViewPool` が `HudRoot` の子に pooled object を生成している
- [ ] `DamageNumberPresenter` が `CombatAttackOccurred` の `TargetActorId` から world anchor を解決している
- [ ] `rg -n "Sprite.Create|digitAtlas|CanvasGroup|RectTransform|Image" Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/DamageNumber*` が 0 件である

### Task 6: ActorStatus を SpriteRenderer pool へ置き換える

目的:

現行 Canvas `ActorStatusView` を削除し、SpriteRenderer ベースの World-space ActorStatus に置き換える。

現行対象:

- `WorldActorStatusPresenter`
- `ActorStatusView`
- `ActorHUDViewPool`
- Addressable `GameHUD/ActorStatusView`

新規 / 置換対象:

- `WorldActorStatusPresenter`
- `ActorStatusView`
- `ActorStatusViewPool`

対応:

- `ActorStatusView` は HP background、HP fill、state icon slots を `SpriteRenderer` で持つ。
- HP fill は `localScale.x` または sprite mask ではなく、まずは fill bar child の scale で表現する。
- state icon は Milestone 11.1 では active / inactive の表示だけを維持し、icon sprite の詳細差し替えは既存 `ActorEffectIconViewData` に合わせる。
- name label は実装しない。
- `WorldActorStatusPresenter` は `IActorStatusViewDataProvider.CopyActiveActorsTo` と `GetActorStatusSummaryQuery` を使う。
- `WorldActorStatusPresenter` は `IActorWorldAnchorProvider` から head anchor を取得する。
- `ActorStatusViewPool` は `ActorId -> lease` を Dictionary で持ち、despawn / inactive layer / invisible actor で return する。

Prefab:

- `Client/Assets/DungeonInn/Runtime/Prefab/GameHUD/ActorStatusView.prefab`
- Addressable: `GameHUD/ActorStatusView`

禁止:

- `UnityEngine.UI.Image` を使わない。
- `RectTransform` を使わない。
- Actor View の child にしない。
- World 側 presenter から status view pool を操作しない。

完了条件:

- [ ] `ActorStatusView` が SpriteRenderer 構成になっている
- [ ] `WorldActorStatusPresenter` が `IActorWorldAnchorProvider` から anchor を取得している
- [ ] `WorldActorStatusPresenter` / `ActorHUDViewPool` の旧 Canvas 経路が削除されている
- [ ] `rg -n "UnityEngine.UI|Image|RectTransform|SetScreenPosition|IActorScreenPositionProvider|ActorHUDViewPool" Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD` が 0 件である

### Task 7: Canvas 固定 UI の Addressable / prefab を `GameUI` へ移動

目的:

コードだけでなく prefab / Addressable key / asset group も責務名に合わせる。

対象:

- `WorldHudView.prefab`
- `SelectedActorInspectorView.prefab`
- `PlayerEventLogView.prefab`
- `InnStatusPanelView.prefab`
- `MinimapView.prefab`
- ScreenStack window prefab / data address が `GameHUD` 名を含む場合

対応:

- Canvas 固定 UI prefab は `Runtime/Prefab/GameUI/` 配下へ移動する。
- Addressable key は `GameUI/...` に変更する。
- `GameHUDAddressableViewFactory` から Canvas UI load address を削除し、`GameUIAddressableViewFactory` へ移す。
- `GameHUD` 用 factory は World-space HUD の prefab / sprite のみを扱う。

完了条件:

- [ ] Canvas 固定 UI prefab が `Runtime/Prefab/GameUI/` 配下にある
- [ ] Canvas 固定 UI Addressable key が `GameUI/...` である
- [ ] `GameHUDViewFactory` が Canvas 固定 UI prefab を読み込まない
- [ ] `GameUIAddressableViewFactory` が Canvas 固定 UI prefab を読み込む

### Task 8: Editor / OneShot で数字 sprite と prefab を配線する

目的:

手作業で壊れやすい Sprite import / prefab wiring / Addressable 登録を Editor / OneShot で再現可能にする。

対象:

- `ImageAsset/Number.png` またはユーザー提供の数字画像
- `Client/Assets/DungeonInn/Runtime/Art/Sprites/UI/DamageDigits.png`
- `DamageNumberView.prefab`
- `ActorStatusView.prefab`
- Addressable settings

対応:

- OneShot script で数字画像を所定フォルダへコピー済みであることを前提に import 設定を行う。
- SpriteRect を 10 分割し、0-9 の digit sprite として import する。
- Addressable key は `GameHUD/DamageNumberView` を設定し、digit sprite は prefab に serialized reference として配線する。
- `DamageNumberView.prefab` は digit root と SpriteRenderer children を serialized reference で配線する。
- `ActorStatusView.prefab` は HP background / fill / icon slots の SpriteRenderer を serialized reference で配線する。

禁止:

- Runtime View が `GetComponentInChildren` で必須 child を探す標準実装にしない。
- View prefab の配線漏れを fallback 生成で隠さない。

完了条件:

- [ ] OneShot または Editor setup で数字 sprite import と `GameHUD/DamageNumberView` Addressable key が再現できる
- [ ] `DamageNumberView` / `ActorStatusView` の必須 child は serialized reference で配線されている
- [ ] prefab 選択または Play 起動で missing reference error が出ない

### Task 9: 旧名称・旧経路の削除

目的:

実装後に旧 `GameHUD` Canvas 経路や `GameHUD3D` 仮称が残らないようにする。

削除対象:

- `GameHUD/UI/...` Canvas UI Addressable key
- 旧 `GameHUDModuleScene` の Canvas root / HUDCanvas field
- 旧 Canvas ActorStatus presenter
- `ActorHUDViewPool`
- `IActorScreenPositionProvider` / proxy / registry のうち使用箇所がなくなったもの
- `WorldActorScreenPositionProvider` / entry point のうち使用箇所がなくなったもの
- `DamageNumberView` の Canvas / Runtime slicing 実装
- `GameHUD3D` 名の実装成果物

残してよいもの:

- `CombatAttackOccurred`
  - event DTO として既に View 座標を持たないため変更不要。
- `IActorStatusViewDataProvider`
  - Actor status の表示対象一覧と removed actor ids の正典として使う。
- `GetActorStatusSummaryQuery`
  - HP ratio / effects を返す Application query として使う。

完了条件:

- [ ] 実装成果物に `GameHUD3D` がない
- [ ] `GameHUD` namespace に Canvas 固定 UI class がない
- [ ] `GameUI` namespace に World-space HUD class がない
- [ ] 使用されない screen-position bridge が削除されている、または GameUI 専用として名前と用途が明確になっている

### Task 10: テスト / 検証

目的:

リネーム、Scene 分割、World-space HUD 移行が機械的に検証できるようにする。

EditMode test:

- `SceneGroupProvider` が `World` に `GameUI` と `GameHUD` を含むこと。
- `GameHUDViewFactory` が `GameHUD/ActorStatusView` と `GameHUD/DamageNumberView` を読むこと。
- `DamageNumberAnimation` が world-space sample を返すこと。
- `DamageNumberPresenter` が `CombatAttackOccurred` の target ActorId から anchor provider を呼ぶこと。
- `WorldActorStatusPresenter` が inactive layer actor の view を return すること。
- `ActorStatusViewPool` が despawn / removed actor id で lease を release すること。

静的確認:

```powershell
rg -n "GameHUD3D" Client/Assets/DungeonInn
rg -n "Canvas|RectTransform|CanvasGroup|Image|TextMeshProUGUI|LHButton" Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD
rg -n "ActorStatusView|DamageNumberView|DamageNumberPresenter|WorldActorStatusPresenter|ActorStatusViewPool|DamageNumberViewPool" Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameUI
rg -n "WorldCameraController|MapLayerViewRegistry|WorldActorViewRegistry|ActorView" Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD
rg -n "GameHUD/UI" Client/Assets/DungeonInn
```

Play 確認:

- Title の StartGame を通常 UI 経路で実行する。
- `[World] GameWorldState initialized` 後 30 秒以上 Error なし。
- Actor が表示される状況で ActorStatus が Actor に追従する。
- Damage が発生する状況で DamageNumber が target actor anchor 付近に表示される。
- pause / speed / DungeonInfo / GuildManagement / Market / Settings など固定 UI は `GameUI` 側で従来通り動作する。

完了条件:

- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している
- [ ] Play 30 秒確認で error が出ていない
- [ ] 上記静的確認で意図しない旧責務・旧名称が残っていない

## 実装ハードゲート

- `World` は HUD View の生成・プール・演出を持たない。
- `GameHUD` は World の具体 View class に依存しない。
- `GameUI` は Actor ごとの World 追従表示を持たない。
- `GameHUD3D` 名を最終成果物に残さない。
- 互換性維持だけを目的とした旧 `GameHUD` Canvas API を残さない。
- Lighthouse の generated file を手編集しない。
- Addressable の直接ロードを行わず、既存の asset scope / factory pattern に従う。
- Domain / Application state を View 表示都合で変更しない。
- Application event DTO に View 表示都合の Unity world position / screen position / sorting / offset を含めない。
- `GameHUD` から World 具象 View / Controller / Registry を inject しない。
- Runtime で数字画像を切り出して Sprite を生成しない。

## 完了条件

- `docs/design/scene-design.md` が `GameUI` / `GameHUD` の最終責務で更新されている。
- 既存 Canvas UI Scene は `GameUI` としてロードされる。
- `GameHUD` Scene が World-space HUD を管理している。
- `SceneGroupProvider` で `World` MainScene の ModuleScene として `GameUI` と `GameHUD` が登録されている。
- `WorldLifetimeScope` が HUD 用 provider interface の実装を登録し、`GameHUDLifetimeScope` は interface のみへ依存している。
- `DamageNumberView` は SpriteRenderer pool として `GameHUD` 内で表示される。
- DamageNumber の数字 sprite は `Client/Assets/DungeonInn/Runtime/Art/Sprites/UI/DamageDigits.png` の sub-asset を `DamageNumberView.prefab` の serialized reference として参照する。
- `ActorStatusView` は `GameHUD` 内で Actor に追従する。
- ActorStatus の HP bar、background、state icon は SpriteRenderer で構成されている。
- `GameHUD3D` と旧 Canvas `GameHUD` の実装名が残っていない。
- compile / EditMode test / Play 30 秒確認が成功している。
- セルフレビューでハードゲート違反がないことを確認している。

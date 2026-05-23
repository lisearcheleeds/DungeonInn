# Milestone 7 完了レビュー #7 — Claude Code
作成日: 2026-05-23
対象コミット: 59a592c milestone7
使用したガイドライン: lighthouse-patterns / coding-rules / domain-design / application-boundary / implementation-quality

---

## 前置き — 本レビューの位置づけ

本レビューは既存レビュー群（6-codex-1/2/3、6-claude-1）に続く最終確認レビューである。  
既存レビューで指摘・解決済みの問題は原則再掲しない。  
既存レビューで「M8 対応」と記録された既知キャリーオーバーは末尾にまとめて再確認のみ行う。  
新たに発見した問題を主軸に記述する。

---

## 差分許可モデル（新規追加の契約変更確認）

### 新規公開インターフェース・型

| 分類 | 型名 | 追加ファイル | 判定 |
|---|---|---|---|
| 新規 struct | `ActorDetailViewData` | Application/World/ActorDetailViewData.cs | PERMIT — ViewData 命名規則に従い View 専用 DTO として正当 |
| 新規 struct | `ActorStatusViewData` | Application/World/ActorStatusViewData.cs | PERMIT |
| 新規 struct | `ActorEffectIconViewData` | Application/World/ActorStatusViewData.cs | PERMIT |
| 新規 class | `GetActorDetailQuery` | Application/World/GetActorDetailQuery.cs | PERMIT — Application 層 narrow query として正当 |
| 新規 class | `GetActorStatusSummaryQuery` | Application/World/GetActorStatusSummaryQuery.cs | PERMIT |
| 新規 class | `ActorSelectionService` | View/Scene/MainScene/World/ActorSelectionService.cs | PERMIT — View スコープ選択状態管理として正当 |
| 新規 class | `ActorDetailPopupPresenter` | View/Scene/MainScene/World/ActorDetailPopupPresenter.cs | PERMIT |
| 新規 class | `PlayerGameEventLogPresenter` | View/Scene/MainScene/World/PlayerGameEventLogPresenter.cs | PERMIT |
| 新規 class | `PlayerEventLogStore` | Application/World/PlayerEventLogStore.cs | PERMIT — IInitializable + IDisposable Service として正当 |
| 新規 class | `WorldHudCanvasProvider` | View/Scene/MainScene/World/WorldHudCanvasProvider.cs | PERMIT（FindFirstObjectByType は M8 対応として記録済み） |
| 新規 static class | `PlaceholderAssetFactory` | View/Scene/MainScene/World/PlaceholderAssetFactory.cs | PERMIT — プレースホルダー生成の統一ファクトリとして正当 |
| 新規 class | `WorldActorSelectionInputHandler` | Input/Layer/WorldActorSelectionInputHandler.cs | PERMIT（[Inject] 欠損は本レビューで指摘） |

---

## 新規概念追加ゲート

### NG-1: PlayerEventLogStore — Application Service として公開される Add()

- 分類: 新規公開メソッド
- 新規性: `PlayerEventLogStore.Add(string text)` が `public` で公開されている
- M7 で意図的に選択したか: 6-claude-1 レビューにて P7-1 として記録済み（M8 見直し）
- ゲート判定: RECORD（M8 で可視性の絞り込みを再検討）

### NG-2: GetActorDetailQuery — キャッシュなし複数回呼び出し設計

- 分類: 新規 Query クラスの動作特性
- 新規性: `GetActorStatusSummaryQuery` はキャッシュ機構を持つが `GetActorDetailQuery` は持たない
- M7 で意図的に選択したか: 記録なし
- ゲート判定: WARN（本レビュー AB-1 で詳述）

---

## 専任レビュー 1 — Lighthouse パターン適合性

### 結果: WARN（新規発見 1 件）

---

### LH-1: PlayerGameEventLogPresenter が Object.Instantiate を直接呼び出している

**問題**

`PlayerGameEventLogPresenter.EnsureLogView()` が `UnityEngine.Object.Instantiate(prefab, hudCanvasProvider.HUDCanvas.transform)` を直接呼び出している。Lighthouse の View Factory パターン（P5相当）では、Prefab のインスタンス化はアセットスコープ管理の Factory を通じて行うことが原則であり、Presenter が直接 Instantiate を呼ぶことは Factory パターンの迂回になる。

**原因**

`WorldAddressableViewFactory` には `CreateActorDetailPopup(Transform parent)` のような生成メソッドが存在するが、`PlayerEventLogView` 用の対応する `CreatePlayerEventLogView(Transform parent)` メソッドが Factory に存在せず、Presenter が直接 Prefab プロパティ (`viewFactory.PlayerEventLogViewPrefab`) にアクセスして Instantiate している。

```csharp
// PlayerGameEventLogPresenter.cs L77
logView = UnityEngine.Object.Instantiate(prefab, hudCanvasProvider.HUDCanvas.transform);
```

**解決案**

`WorldAddressableViewFactory` に `CreatePlayerEventLogView(Transform parent)` メソッドを追加し、Presenter からは Factory メソッド経由で生成する。これにより Presenter が Prefab への直接参照を持つ必要がなくなる。

**根拠となるファイルリスト**

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/PlayerGameEventLogPresenter.cs` (L64-78)
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldAddressableViewFactory.cs`
- `docs/guidelines/lighthouse-patterns.md` (P5: View Factory パターン)

**完了条件**

`PlayerGameEventLogPresenter` から `Object.Instantiate` の直接呼び出しが消え、`WorldAddressableViewFactory.CreatePlayerEventLogView(Transform)` 経由になること。`WorldAddressableViewFactory.PlayerEventLogViewPrefab` プロパティの `public` 公開が不要になること。

---

## 専任レビュー 2 — コーディング規約適合性

### 結果: FAIL（新規発見 1 件）

---

### CR-1: WorldActorSelectionInputHandler コンストラクタに [Inject] がない

**問題**

`WorldActorSelectionInputHandler` のコンストラクタに `[Inject]` 属性が付与されていない。coding-rules §15-4 は「DI コンテナから注入を受ける全コンストラクタには `[Inject]` を付与すること」と定めている。

```csharp
// WorldActorSelectionInputHandler.cs L25-42
public WorldActorSelectionInputHandler(
    InputActions inputActions,
    ActorSelectionService actorSelectionService,
    WorldCameraController worldCameraController,
    LayerPositionViewMapper positionMapper,
    MapLayerViewRegistry layerViewRegistry,
    IGameWorldStateReader worldState)
{
    // [Inject] 属性なし
```

VContainer はコンストラクタが1つの場合は属性なしで動作するが、規約統一と可読性のために全 DI コンストラクタへの付与が求められている。

**原因**

実装時に `[Inject]` 属性の付与が漏れた。

**解決案**

```csharp
[Inject]
public WorldActorSelectionInputHandler(
    InputActions inputActions,
    ...
```

**根拠となるファイルリスト**

- `Client/Assets/DungeonInn/Runtime/Scripts/Input/Layer/WorldActorSelectionInputHandler.cs` (L25)
- `docs/guidelines/coding-rules.md` (§15-4: [Inject] 属性)

**完了条件**

`WorldActorSelectionInputHandler` のコンストラクタ宣言直前に `[Inject]` 属性が付与されていること。

---

## 専任レビュー 3 — Domain 設計適合性

### 結果: PASS

M7 で追加された Domain 層の変更を確認した。  
`PlayerEventLogEntry` は純粋な `readonly struct` であり View 依存なし。  
`ActorDetailViewData`・`ActorStatusViewData`・`ActorEffectIconViewData` は Application 層に配置されており、Domain 層を汚染していない。  
Domain の Entity (`Actor`, `ActorEffectInstance` 等) が直接 View に渡される経路は確認されなかった。

新たな Domain ルール違反は検出されなかった。

---

## 専任レビュー 4 — Application 境界適合性

### 結果: FAIL（新規発見 2 件）

---

### AB-1: ActorDetailPopupPresenter がフレームごとに GetActorDetailQuery を 2 回呼び出しており、毎回 ToArray() 割り当てが発生する

**問題**

`WorldGameLoopEntryPoint.Update()` は毎フレーム `actorDetailPopupPresenter.UpdatePopupPosition()` と `actorDetailPopupPresenter.UpdatePopupContent()` を順番に呼び出す。両メソッドはそれぞれ独立して `actorDetailQuery.Query(selectedId.Value)` を呼び出す。

```csharp
// ActorDetailPopupPresenter.cs L67
var dto = actorDetailQuery.Query(selectedId.Value); // UpdatePopupPosition 内

// ActorDetailPopupPresenter.cs L94
var dto = actorDetailQuery.Query(selectedId.Value); // UpdatePopupContent 内
```

`GetActorDetailQuery.Query()` の実装は毎回 `equipmentNameBuffer.ToArray()` と `effectBuffer.ToArray()` を呼び出してヒープ割り当てを行う。キャッシュ機構がないため、俳優が選択されている間は毎フレーム 2 回のクエリ実行 × 2 回の配列割り当てが発生する。

`GetActorStatusSummaryQuery` が signature ベースのキャッシュを持つのに対し、`GetActorDetailQuery` が同等のキャッシュを持たない設計の非対称性がある。application-boundary-guidelines 「フレームループ禁止事項」は ToArray()/ToList() をフレームループ内で呼ぶことを禁じている。

**原因**

`UpdatePopupPosition` と `UpdatePopupContent` がそれぞれ独立して Query を呼び出す構造になっており、同一フレーム内で結果を共有する仕組みが設計されていない。また `GetActorDetailQuery` にキャッシュが実装されていない。

**解決案**

以下のいずれかの方針で対応する。

案A（Presenter で結果を1回取得して共有）: `ActorDetailPopupPresenter` に `lastDto` フィールドを設け、フレームの先頭でクエリを1回実行して両メソッドで共有する。メソッドを `UpdatePopup()` 1本に統合する方法も有効。

案B（Query にキャッシュを追加）: `GetActorDetailQuery` に `GetActorStatusSummaryQuery` と同様の signature-based キャッシュを実装する。装備・エフェクトの変化がない限りは前回結果を返す。

いずれの場合もフレームあたりの配列割り当てをゼロにすることが目標。

**根拠となるファイルリスト**

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorDetailPopupPresenter.cs` (L53-101)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetActorDetailQuery.cs` (L79-98)
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetActorStatusSummaryQuery.cs` (キャッシュ実装の参考)
- `docs/guidelines/application-boundary-guidelines.md` (フレームループ禁止事項: ToArray/ToList)

**完了条件**

俳優選択中の1フレームあたり `GetActorDetailQuery.Query()` 呼び出し回数が 1 回以下になること。且つ `GetActorDetailQuery` の ToArray() 呼び出しがフレームごとの割り当てを発生させないこと（キャッシュ or 戻り値型の変更）。

---

### AB-2: View 層クラスが IGameWorldStateReader を直接注入して Actors を列挙している

**問題**

`WorldActorSelectionInputHandler`（Input 層）と `ActorSelectionService`（View 層）の 2 クラスが `IGameWorldStateReader` を直接注入し、`worldState.Actors` を列挙している。

```csharp
// WorldActorSelectionInputHandler.cs L100-118
foreach (var actor in worldState.Actors)
{
    // ワールド座標→スクリーン座標変換してクリック判定
}

// ActorSelectionService.cs L91-99
void RefreshSortedBuffer()
{
    sortedActorIdBuffer.Clear();
    foreach (var actor in worldState.Actors)
    {
        sortedActorIdBuffer.Add(actor.Id);
    }
    sortedActorIdBuffer.Sort();
}
```

application-boundary-guidelines は「View/Presenter は IGameWorldStateReader を直接注入せず、Application 層の narrow query から ViewData を受け取ること」と定めている。既存レビュー 6-codex-1 で Presenter レベルの違反は修正されたが、Input ハンドラと Selection Service レベルの同様の違反が残存している。

**原因**

クリック判定のためにはアクターのワールド座標が必要であり、Actor エンティティの `Position` フィールドへのアクセスが必要となる。Application 層に「クリック位置から最近傍アクターIDを返す narrow query」が存在しないため、View 層が直接 `IGameWorldStateReader` に依存せざるを得ない状況になっている。

**解決案**

Application 層に `GetActorScreenPositionsQuery` または `FindNearestActorQuery(Vector2 screenPos)` を追加し、View 層が Domain エンティティを直接参照しない形にする。ただしこの修正はスクリーン座標変換（カメラ・マッピング）を Application 層が知る必要が生じ、レイヤー境界の再検討が必要。M7 完了条件として即時修正するか M8 タスクとするかはユーザーの方針判断が必要。

**根拠となるファイルリスト**

- `Client/Assets/DungeonInn/Runtime/Scripts/Input/Layer/WorldActorSelectionInputHandler.cs` (L21, L100)
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSelectionService.cs` (L11, L94)
- `docs/guidelines/application-boundary-guidelines.md` (View/Presenter の境界ルール)

**完了条件**

`WorldActorSelectionInputHandler` と `ActorSelectionService` が `IGameWorldStateReader` を直接注入しなくなること。または、この依存を許容する設計判断がドキュメントに明文化されること。

---

## 専任レビュー 5 — 実装品質適合性

### 結果: WARN（新規発見 1 件）

---

### IQ-1: ActorDetailPopup.PopupOffsetX が説明なしのマジックナンバー定数

**問題**

`ActorDetailPopup` に `const float PopupOffsetX = 120f` が定義されているが、この値がなぜ 120 ピクセルなのかの根拠がコード上に存在しない。implementation-quality-guidelines はマジックナンバーをコード中に直接置くことを禁じており、定数化されていても説明コメントがない場合は意図が伝わらない。

```csharp
// ActorDetailPopup.cs L9
const float PopupOffsetX = 120f;
```

**原因**

UI 配置の調整値をハードコードした際にコメントが付与されなかった。

**解決案**

定数に意図を示すコメントを追加する。または、Inspector から調整できるよう `[SerializeField] float popupOffsetX = 120f;` として設定可能にする。将来的にはポップアップ幅の半分などから算出する計算式として表現することで、定数の意味を自明にできる。

**根拠となるファイルリスト**

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorDetailPopup.cs` (L9, L52)
- `docs/guidelines/implementation-quality-guidelines.md` (マジックナンバー禁止)

**完了条件**

`PopupOffsetX` の値の根拠を示すコメントが付与されるか、Inspector 調整可能な SerializedField に変更されること。

---

## M8 キャリーオーバー確認（既存記録の現行コード照合）

以下は既存レビュー（6-codex-3、6-claude-1）で「M8 対応」と記録された項目の現行コード照合。

| ID | 問題 | 現行コード状態 | 判定 |
|---|---|---|---|
| S5-1 | `WorldHudCanvasProvider.Initialize()` が `FindFirstObjectByType<WorldUIModuleScene>()` を使用 | 現行コードで確認済み（WorldHudCanvasProvider.cs） | M8 未対応・記録維持 |
| P3-1 | `WorldProjectileViewPool`/`WorldAreaEffectViewPool` が Stack を使用（ロードマップ指定は Queue） | 本レビューでコード未確認（既存レビューの記録を採用） | M8 未対応・記録維持 |
| P7-1 | `PlayerEventLogStore.Add()` が public 公開 | 現行コードで確認済み（PlayerEventLogStore.cs L? public Add） | M8 未対応・記録維持 |

---

## 統合レビュー

### 全体サマリー

| レビュー領域 | 判定 | 新規発見数 |
|---|---|---|
| Lighthouse パターン | WARN | 1 (LH-1) |
| コーディング規約 | FAIL | 1 (CR-1) |
| Domain 設計 | PASS | 0 |
| Application 境界 | FAIL | 2 (AB-1, AB-2) |
| 実装品質 | WARN | 1 (IQ-1) |

### 重大度分類

| ID | 問題 | 重大度 | 分類根拠 |
|---|---|---|---|
| CR-1 | [Inject] 欠損 | **MUST** | コーディング規約ハードゲート §15-4 |
| AB-1 | フレームごと 2 回クエリ + ToArray 割り当て | **MUST** | application-boundary フレームループ禁止事項 |
| AB-2 | View 層が IGameWorldStateReader を直接列挙 | SHOULD | レイヤー境界の方針依存。即時修正 or 設計記録のいずれかが必要 |
| LH-1 | Presenter から Object.Instantiate 直接呼び出し | SHOULD | Factory パターン迂回。即時修正が望ましい |
| IQ-1 | PopupOffsetX 説明なし定数 | MAY | コメント追記で対応可能 |

### M7 完了可否判定

**判定: 条件付き完了**

CR-1（[Inject] 欠損）と AB-1（フレームごと 2 回 ToArray 割り当て）は規約上の MUST 指摘であり、M7 完了前に修正することを推奨する。

AB-2（View 層の IGameWorldStateReader 直接依存）は、Input/Selection の設計変更が広範囲に及ぶため、ユーザーの方針確認の上で M8 タスクとすることも許容される。ただし、その場合は「View 層が Domain 状態読み取りインターフェースを直接参照することを Input ハンドラに限り許容する」旨をドキュメントに明文化すること。

LH-1（Presenter の直接 Instantiate）は Factory メソッドの追加のみで対応でき、影響範囲が限定的なため M7 内修正が望ましい。

IQ-1（マジックナンバー）はコメント追記で完結するため M7 内修正を推奨する。

**M8 移行条件（最低ライン）**

- [ ] CR-1: `WorldActorSelectionInputHandler` コンストラクタに `[Inject]` を付与
- [ ] AB-1: `ActorDetailPopupPresenter` のフレームごとクエリを 1 回に統合し ToArray 割り当てを排除
- [ ] AB-2: View 層の `IGameWorldStateReader` 直接依存について、M8 タスク化 or 設計記録のいずれかを実施
- [ ] LH-1: `WorldAddressableViewFactory` に `CreatePlayerEventLogView` を追加し Presenter の直接 Instantiate を除去
- [ ] IQ-1: `PopupOffsetX` にコメント追記または SerializedField 化

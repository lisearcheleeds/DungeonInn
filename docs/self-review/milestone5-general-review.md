# 総合品質レビュー報告（Milestone 5 Phase 1）

## 概要

DungeonInnプロジェクト（Milestone 5 Phase 1）の総合品質評価は、**全体的に高い水準を維持しており、アーキテクチャ設計は良好**です。しかし、以下の観点で改善が必要な問題が検出されました。

**品質スコア概算: 7.5 / 10**
- アーキテクチャ設計: 8/10
- コード品質: 7/10
- テスト・保守性: 7.5/10
- 将来拡張性への対応: 7/10
- リスク管理: 6.5/10

---

## 問題一覧

### [総合-1] Placeholder資産に強く依存した設計でアセット欠落時の堅牢性が不足

**重要度**: 高  
**カテゴリ**: リスク / 将来拡張性  
**場所**: `WorldMapView.cs`, `WorldActorPresenter.cs`, `WorldDebugMaterialFactory.cs`

**問題**:
- `WorldMapView::CreateTile()` は `GameObject.CreatePrimitive(PrimitiveType.Plane)` をハードコードしており、placeholder素材への依存が実装レベルで固定化されている。
- `WorldDebugMaterialFactory.Create()` は `Shader.Find("Universal Render Pipeline/Lit")` のフォールバックのみで、URP非搭載環境では失敗の可能性がある。
- Phase 2 の「Placeholder Asset / Visual Config 最小基盤」がまだ実装されていないため、将来の正式アセット切り替えが困難になるリスク。

**原因**:
- Milestone 5 Roadmapが「Phase 2で最小基盤を実装する」計画であるにもかかわらず、Phase 1ですでに具体的な表現方式（`CreatePrimitive`）がコード内に埋め込まれている。

**解決案**:
- `TileVisualDefinition` / `ActorSpriteVisualConfig` の設定基盤を Phase 1 で先行実装し、`WorldMapView` / `WorldActorPresenter` は設定から tile 表現を読む設計に変更する。
- `WorldDebugMaterialFactory.Create()` が null shader を返した場合のエラーハンドリングを追加。

---

### [総合-2] WorldGameLoopEntryPoint の依存注入が過剰（責務範囲が不明確）

**重要度**: 中  
**カテゴリ**: コード品質 / テスト性 / 保守性  
**場所**: `WorldGameLoopEntryPoint.cs:42-82`

**問題**:
- Construct メソッドが 20個以上の UseCase / Orchestrator を注入受け取っており、このクラスの責務が不明瞭。
- 新しい UseCase が追加されるたびに Construct を修正する必要がある（Open/Closed原則違反）。
- テスト時にすべてをモック化する必要があり、テスト設定が複雑。

**原因**: ゲームループの実行順序が複雑であることと、Unity MonoBehaviour の DI パターンの組み合わせで、EntryPoint が「オーケストレータ中枢」になってしまっている。

**解決案**:
- `GameLoopExecutor` のような中間レイヤーを導入し、「全 UseCase の呼び出し順序」を一元管理する。
- EntryPoint は `GameLoopExecutor` だけに依存し、個別の UseCase には依存しない設計にする。

---

### [総合-3] TODO コメントが 10 件以上残存し、完了条件と乖離

**重要度**: 中  
**カテゴリ**: ドキュメント / 設計の一貫性  
**場所**: `SpawnScheduledMonsterOrchestrator.cs:39,47,61,66` など複数箇所

**問題**:
- `// TODO: 間隔をSpawnTableMasterから取得する` など「マスタデータから値を取得する」タイプの TODO が複数残存している。
- `DetectCombatEncounterUseCase:181` の `// TODO: FactionMasterから取得する。` も同様。
- これらの TODO が実装されないと、Monster / Adventurer のスポーン動作がマスタデータと乖離し続ける。

**原因**: マスタデータ体系が明確に定義されておらず、各 Orchestrator が独立して TODO を残した状態で実装された。

**解決案**:
- 各 TODO に対応する Master クラス / リポジトリを定義し、実装スケジュールを明確にする。
- 短期的には GameConstants に仮の値を配置し、TODO のある箇所を明示的に「暫定定数使用」とコメント化する。
- Roadmap に「マスタデータシステム構築」を Milestone 6 の前フェーズとして追加する。

---

### [総合-4] WorldActorDebugVisualizer が「互換ファサード」のまま保守負債化リスク

**重要度**: 中  
**カテゴリ**: コード品質 / 保守性  
**場所**: `WorldGameLoopEntryPoint.cs:34, 103`

**問題**:
- `worldActorDebugVisualizer.UpdateVisuals()` が毎フレーム呼ばれており、古い debug Sphere 表示と新しい SpriteRenderer 表示が **二重表示される可能性** がある。
- feature flag や conditional rendering がないため、デバッグ表示の on/off ができない。
- Phase 7「Debug Sphere / Plane の撤去」が遅延するリスクがある。

**原因**: 「互換ファサード」として残す決定が、debug ビジュアルを表示し続ける設計に変わったまま固定化されている。

**解決案**:
- `#if DUNGEON_INN_DEBUG_VISUALIZATION` などのコンパイルディレクティブで debug 表示を条件付きにする。
- または `IDebugVisualizationEnabled` フラグを設定から読み込み、off の場合は呼び出さない。

---

### [総合-5] RecoverAdventurerAtInnUseCase に複数の責務が混在

**重要度**: 中  
**カテゴリ**: 単一責任の原則（SRP）  
**場所**: `Application/UseCase/RecoverAdventurerAtInnUseCase.cs`

**問題**:
- このクラスが以下の責務を持っている：
  1. 宿泊予約確保（EnsureInnReservation）
  2. 回復ティック処理（TickRecovery）
  3. 宿泊待機状態管理（ChangeToWaitingForInn）
  4. イベント発行（複数の eventPublisher.Publish 呼び出し）
- 回復アルゴリズムの複雑さ（累積値 → int 変換 → イベント発行）が同じメソッドに混在している。

**解決案**:
- `RecoveryTickCalculator` のような値計算用クラスを分離。
- `InnReservationOrchestrator` を導入し、予約確保のロジック（fee charge, facility selection など）を別クラスに移す。

---

### [総合-6] 座標変換にハードコードされた定数が複数存在

**重要度**: 中  
**カテゴリ**: 将来拡張性 / マジックナンバー  
**場所**: `LayerPositionViewMapper.cs:9-10`

**問題**:
- `const float LayerHeightOffset = -240f;` と `const float ActorHeightOffset = 1.5f;` が定数として定義されているが、マップサイズやレイヤー数に依存する可能性がある。
- Milestone 6 の NavMesh 連携時に高さ設定を動的に変更したい場合、このクラスの修正が必須になる。

**解決案**:
- `LayerHeightOffset` / `ActorHeightOffset` を `LayerPositionViewMapperSettings` ScriptableObject に移動する。
- または設定クラスに持たせ、コードを書き換えずに調整できるようにする。

---

### [総合-7] Null チェックの inconsistency

**重要度**: 低  
**カテゴリ**: コード品質 / 一貫性  
**場所**: `WorldActorViewRegistry.cs`, `WorldMapView.cs`, `WorldDebugMaterialFactory.cs`

**問題**:
- Dispose() 内の `if (material != null)` チェックと、`ApplyMaterial()` 内の `if (renderer != null)` チェックの混在。
- coding-rules.md では `?? throw` パターンが推奨されているが、getter での null check は混在している。

**解決案**:
- View 層の static helper メソッドの null チェックを統一する。
- nullable annotation を有効にして null safety を型レベルで強制することを検討する。

---

### [総合-8] WorldActorPresenter / WorldMapView が IGameWorldStateReader に強く依存

**重要度**: 中  
**カテゴリ**: テスト性 / 依存性  
**場所**: `WorldMapView.cs:13`, `WorldActorPresenter.cs:19`

**問題**:
- 両クラスが `IGameWorldStateReader` を注入受け取り、毎フレーム `gameWorldState.Actors`, `gameWorldState.Dungeon` などを読み込んでいる。
- テスト時にはフルサイズの `IGameWorldStateReader` 実装が必要。
- review-policy-guideline.md:89-98 では「表示情報の取得経路を使い分ける」と明記されているが、実装では全て IGameWorldStateReader に統一されている。

**解決案**:
- `IMapViewDataProvider` / `IActorViewDataProvider` のような読み取り専用インターフェースを分離する。
- または GameEvent ベースで「floor が生成された」「Actor が移動した」という変化を購読する設計に変更する。

---

### [総合-9] Delta time 計算の参照が混在している

**重要度**: 低  
**カテゴリ**: コード品質 / 一貫性  
**場所**: `WorldGameLoopEntryPoint.cs:140-143`

**問題**:
- `unscaledDeltaTime` を gameLoopUseCase に渡し、その戻り値の計算済み値を使いつつ、ローカルでも再計算している。
- 複数の UseCase が `frameDeltaGameSeconds` と `result.AdvancedScheduleTicks` の両方を受け取る場面があり、どちらを使うべきか混在している。

**解決案**:
- `GameLoopTickResult` に `FrameDeltaGameSeconds` フィールドを追加し、一元計算する。
- または `IGameClock` から frame delta を読む仕組みに統一する。

---

### [総合-10] > 演算子の使用（コーディング規約違反）

**重要度**: 低  
**カテゴリ**: コーディング規約遵守  
**場所**: 複数箇所の可能性

**問題**:
- coding-rules.md では「`<` を使い `>` は使わない」と明記されている（11-1）。
- 実装コードにいくつかの箇所で `>` 演算子が使われている可能性がある。

**解決案**:
```powershell
rg ' > ' Client/Assets/DungeonInn/Runtime/Scripts/
```
で全箇所を抽出し、`<` に統一する。

---

## 良い実装の例

### [1] Clean Architecture の層分離が明確

Domain / Application / Infrastructure / View の責務分離が明確で、違反が少ない。UseCase 層が「コマンド型」で命名規則が統一されている（`ExecuteAsync`, `Execute`）。

### [2] Event Bus パターンの適切な活用

`IEventPublisher` / `IEventSubscriber` に分離され、Publishing と Subscribing の責務が明確化されている。Presenter が Event を購読し、UseCase が Publish のみ行う疎結合な設計になっている。

### [3] 設計ドキュメントの充実（review-policy-guideline.md）

単なるコーディング規約ではなく、「型設計」「イベント設計」「DI設計」などの判断基準が明文化されている。事例ベースで「Before/After」が示されており、実装者が判断しやすい。

### [4] Milestone 単位での Self-Review 実施

Milestone 2, 4 での自己レビューが詳細で、「問題 → 原因 → 解決策」が明確に記述されている。パフォーマンス・整合性・設計など観点が網羅的。

### [5] Orchestrator パターンで UseCase 間の依存を制御

`ActorDefeatOrchestrator`, `InitializeGameWorldOrchestrator` など、複数の UseCase を組み合わせる責務が明確で、UseCase 間の直接呼び出しを避ける設計が採用されている。

---

## 総評

### 品質の総括

DungeonInn プロジェクトは **アーキテクチャ設計の方向性は正しく、層分離と責務分割が明確**です。Event Bus、Orchestrator、Clean Architecture が機能しており、基盤は堅実です。

#### 強み
- Clean Architecture に基づいた層分離が貫徹されている
- Event Bus による疎結合設計
- Orchestrator パターンで複雑な流れを管理
- 設計ドキュメント（review-policy-guideline.md）が充実
- Self-Review ルーチンが機能している

#### 弱み
1. **Placeholder 素材への依存が深い** — Phase 1 時点で CreatePrimitive がコード化されている
2. **EntryPoint が責務過剰** — 20個以上の UseCase 注入
3. **TODO が 10 件以上残存** — 完了条件との乖離
4. **SRP 違反が複数** — RecoverAdventurerAtInnUseCase など
5. **パフォーマンスリスク** — タイルごと GameObject 生成

### 最優先改善項目

| 優先度 | 項目 | 影響範囲 | 作業量 |
|---|---|---|---|
| 1 | Placeholder asset 設定基盤の先行実装（Phase 2 前倒し） | Map/Actor 表示 | 中 |
| 2 | WorldGameLoopEntryPoint の責務削減（Executor パターン導入） | ゲームループ全体 | 大 |
| 3 | TODO コメントの実装スケジュール明確化 | Spawn, Combat, Faction | 中 |
| 4 | WorldActorDebugVisualizer の feature flag 化 | Debug 表示 | 小 |
| 5 | RecoverAdventurerAtInnUseCase の責務分割 | 宿泊システム | 中 |

### Milestone 6 への課題

- NavMesh 連携を想定して、`LayerPositionViewMapper` の高さ設定を動的化。
- UI 層追加時に Presenter 階層を再整理（WorldPresenter / WorldGameLogPresenter の責務明確化）。
- マスタデータシステムの正式実装（TODO の解消）。

段階的な改善により品質を 8.5/10 以上に引き上げることは十分可能です。

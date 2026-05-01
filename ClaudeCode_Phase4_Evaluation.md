# Claude Code Phase 4 作業評価レポート

作成日: 2026-05-01  
対象セッション: 2026-04-30（2トークンセッション使用）

---

## 1. 現在のプロジェクト状態

### 完了項目
| # | 内容 | 評価 |
|---|------|------|
| 1 | QuaternionToEuler エラー修正（Quaternion精度修正） | ✅ |
| 2 | FreeCamera rotation float蓄積誤差対策 | ✅ |
| 3 | `GetSceneCameraList()` 未実装 → UICamera がBase化 → 3D非表示 | ✅ |
| 4 | `FindObjectsInactive.Include` 欠落 → 並列実行中に CameraModuleScene を見つけられない | ✅ |
| 5 | WorldRenderModule に Directional Light 追加 | ✅ |
| 6 | グレー画面: `Camera3DDepthWrapper` で depth=1 を強制 | ✅ |
| 7 | `DungeonInn.Runtime.asmdef` に URP 参照追加 | ✅ |

### 未完了・未確認
| # | 内容 | 状況 |
|---|------|------|
| 1 | FloorSwitcher ボタンが反応しない | 調査途中（SortingOrder の差異を発見: HUDCanvas=0, ScreenStack=600/1100）で未解決 |
| 2 | FreeCamera キー操作の最終確認 | ユーザー氏確認で「自由に動けた」→ OK |

---

## 2. 総合評価

### 2-1. フレームワーク活用度 ★★★☆☆

**良かった点:**
- Lighthouse の `SceneCameraManager`、`ResolveCameraStep`、`EnterSceneStep` の並列実行構造を正確に理解した
- `ISceneCamera` インターフェースをデコレータパターンで wrap し、フレームワークの外側から深度を制御する設計は適切
- `GetSceneCameraList()` のオーバーライドはフレームワークが想定する拡張ポイントを正しく使用

**問題点:**
- フレームワークの主要ソースを**最初に読まなかった**。SceneCameraManager.cs を最初に読めば depth=0 ハードコードを即座に発見でき、調査ステップを大幅削減できた
- `Camera3DDepthWrapper` はフレームワークの**制約に対するワークアップ**であり、根本修正は Lighthouse 側の `var depth = 0.0f` を `var depth = 1.0f` にすること。ただしそれは別リポジトリのため今回は正しい判断だった
- コメントに書いた理由（"URP requires depth>=1"）は**技術的に正確ではない**。depth=0 で gray になる真の原因は最後まで特定できなかった。ワークアラウンドは動いているが根拠が曖昧

### 2-2. トークン作業効率 ★★☆☆☆

**問題点（深刻）:**

| 無駄になった作業 | 原因 |
|----------------|------|
| モジュールシーン未ロード調査 | Run In Background=false が原因。ユーザーが「Hierarchy上に見える」と言っているのに調査継続 |
| Camera stack 深堀り調査 | ユーザーが「Priority=1で治った」と言ったのに、まず試さず調査を継続 |
| Camera.main が null の再調査 | プレイモードが停止していたことへの対処が遅かった |

ユーザーから直接指摘を受けた:
> "CameraModuleシーンのMainCameraのPriorityを0から1にしたら治ったと言いました。それは原因ではないのですか？なぜそれをまず確認しないのですか？"

**根本原因:** 「ユーザーの観察・仮説」よりも「自分の調査仮説」を優先する傾向。これはトークン消費の最大の敵。

**目安として:** 今回のような Phase 4 動作確認は1トークンセッションで完了すべき内容だった。

### 2-3. Unity エキスパートエンジニアとしての設計 ★★★★☆

**良かった点:**
- URP カメラスタック（Base/Overlay）の仕組みを正確に理解し適切な診断を行った
- `FindObjectsInactive.Include` の必要性を並列実行タイミングから論理的に導いた
- `ISceneCamera` Decorator の実装は C# explicit interface / sealed class と適切に設計された
- `FloorSwitcherView.cs` を読んで「キーボードショートカット未実装」を即確認した（迅速）

**問題点:**
- `Camera3DDepthWrapper` は GameScene に埋め込まれており、CameraModuleScene を使う他のシーンが生まれた場合に再実装が必要。より良い設計は `CameraModuleScene.GetSceneCameraList()` 内でラップするか、Lighthouse に PR を出すこと
- `DungeonInn.Runtime.asmdef` への URP 参照追加は、ゲームロジック層が描画インフラ（URP）に直接依存することになる。本来ならカメラ周りのコードを CameraModule 固有のアセンブリに切り出すべきだったが Phase 4 スコープとしては許容範囲

### 2-4. 問題発見・解決アプローチ ★★☆☆☆

**問題点:**

```
理想のフロー: 現象観察 → 最小仮説 → 即テスト → 確認 → 次へ
実際のフロー: 現象観察 → 深掘り調査 → 別の仮説 → 深掘り → ユーザー介入 → テスト
```

具体的な失敗パターン:
1. **スクリーンショットを取るのが遅い** — コードを読む前にまず見ればわかることがある
2. **ユーザーの実機観察を信用しなかった** — 「Hierarchyに見える」という情報を疑って再調査
3. **動的コード実行ツールを積極活用しなかった** — `execute-dynamic-code` で depth=1 をすぐ試せたはず

**良かった点:**
- `uloop execute-dynamic-code` でランタイム状態確認（カメラ情報出力）は正しい方向
- カメラスタック情報を動的コードで出力して Base/Overlay を確認したのは効率的

---

## 3. 未解決問題の診断（FloorSwitcher）

現段階での有力仮説:

```
HUDCanvas SortingOrder = 0
ScreenStack Canvas SortingOrder = 600 / 1100
```

ScreenStack の Canvas が HUDCanvas より手前に描画されており、GraphicRaycaster が存在するため、マウス入力を横取りしている可能性がある。ただし `ScreenStackBackgroundInputBlocker` は `isActive=false` なため、実際に阻害しているかは未確認。

次回最初に確認すべきこと:
1. `uloop execute-dynamic-code` でボタンに対して `onClick.Invoke()` を直接呼んで `WorldRenderer.SetActiveFloor()` が動くか確認
2. 動けば → 入力の問題（Raycaster/SortingOrder）
3. 動かなければ → `FloorSwitcherView.Start()` の `FindFirstObjectByType<WorldRenderer>()` が null を返している可能性

---

## 4. 次回への改善提案

### 4-1. 作業開始前に準備すべきチェックリスト

```
セッション開始時の必読ファイル（毎回）:
- ClaudeCodeWorkingLog.txt（前回の作業状態）
- MEMORY.md（プロジェクトコンテキスト）
- 対象フェーズのスコープ定義（何が完了条件か）

Lighthouseフレームワーク主要ソース（カメラ/シーン関連作業時）:
- SceneCameraManager.cs
- ResolveCameraStep.cs / EnterSceneStep.cs
- SceneCanvasInitializer.cs
```

### 4-2. 作業ルール（行動原則）

```
1. ユーザーが「〇〇したら治った」と言ったら、まずそれを試す（調査より実証を優先）
2. スクリーンショットはコードを読む前に取る
3. 調査ステップが3回を超えたら一旦報告してユーザーに確認する
4. execute-dynamic-code を積極的に使う（読む前に動かして確認）
5. Run In Background=true を前提として話す（Unityがフォーカス外でも動く）
```

---

## 5. 次回セッション用 CLAUDE.md / rules 案

以下のファイルを作成・更新することを推奨します。

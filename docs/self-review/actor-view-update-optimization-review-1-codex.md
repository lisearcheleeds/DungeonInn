# Actor View Update Optimization Review 1 - Codex

対象: Actor の SpriteRenderer / Transform 差分適用、カメラ外 ActorView 更新スキップ対応

観点: 設計、整合性、パフォーマンス、重複した機能を持つクラス/データクラス、その他総合

## 差分分類

production 契約変更:

- `ActorView.SetVisible(bool)` を追加
- `ActorView.IsVisible` を追加
- `WorldCameraController.IsWorldPositionVisible(Vector3, float)` を追加

長期状態追加:

- `ActorView` に `currentSprite` / `currentVisualScale` / `currentFlipX` / `currentRotationY` / `visible` を追加

許可理由:

- `SetVisible` と `IsWorldPositionVisible` は View 層の表示更新最適化に必要な production API であり、Application / Domain へ依存を漏らしていない。
- `ActorView` のキャッシュ状態は Unity component への重複適用を避けるための View 層内部状態であり、Actor のゲーム状態とは別責務である。

不許可または要修正:

- `ActorView.IsVisible` は現行コードで未使用の public API であり、テスト都合でも production 都合でも必要性が確認できない。

## 1. カメラ外判定の余白が ScriptableObject 設定ではなくコード定数になっている

重大度: 中

問題:

`WorldActorPresenter` の `ActorViewportMargin = 0.08f` は、カメラ外 Actor の表示/非表示切り替えタイミングを決める調整値である。画面端でのちらつき、巨大 Actor の見切れ、モバイル/デスクトップの見え方に影響するため、実質的には View 設定である。

以前の方針として、Settings 系はコード内に固定せず ScriptableObject に置くことになっている。現状では `WorldCameraSettingsSO` などから調整できず、調整のたびにコード変更が必要になる。

原因:

最適化実装時に、カメラ外判定の余白を「小さな実装定数」として扱ったため。実際には表示品質とパフォーマンスのトレードオフを決める調整可能値であり、WorldCamera または ActorView 表示設定の所有物として扱うべきだった。

解決案:

`ActorViewportMargin` を ScriptableObject 管理の設定へ移す。既存の `WorldCameraSettingsSO` に含めるか、Actor 表示最適化専用の設定が増える見込みがあるなら `WorldActorViewSettingsSO` を新設する。現時点では追加項目が1つなので、既存の `WorldCameraSettingsSO` / `WorldCameraSettings` に `ActorViewportMargin` を追加するのが最小で整合的。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldCameraSettings.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldCameraSettingsSO.cs`

完了条件:

- [ ] `WorldActorPresenter` から `ActorViewportMargin` 定数が削除されている
- [ ] カメラ外判定の余白が `WorldCameraSettingsSO` から設定できる
- [ ] fallback settings にも同じ設定値が定義されている
- [ ] `uloop.cmd compile --project-path Client` が成功する
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する

## 2. `ActorView.IsVisible` が未使用の public API として残っている

重大度: 低

問題:

`ActorView.IsVisible` は現行 Runtime / Tests のどこからも参照されていない。表示状態を外部から読む契約として公開されているが、利用者が存在しないため、今後のコードが `SpriteRenderer.enabled` と `ActorView.IsVisible` のどちらを正典として扱うべきか迷う余地を作っている。

原因:

`SetVisible` 実装時に、内部状態の確認用として property を公開したが、実際の production caller は visibility を設定するだけで読み取りを必要としていない。

解決案:

`ActorView.IsVisible` を削除し、visibility の正典を `ActorView` 内部に閉じる。テストで必要になった場合も、Runtime public API を増やすのではなく、挙動を `SpriteRenderer.enabled` や Presenter 経由の結果で確認する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorView.cs`

完了条件:

- [ ] `ActorView.IsVisible` が削除されている
- [ ] `rg "IsVisible" Client/Assets/DungeonInn/Runtime/Scripts Client/Assets/DungeonInn/Tests/EditMode` で不要な参照が残っていない
- [ ] `uloop.cmd compile --project-path Client` が成功する

## 3. ActorView 差分適用とカメラ外スキップの EditMode テストがない

重大度: 中

問題:

今回の変更は performance optimization だが、`ActorView` のキャッシュ状態と `WorldActorPresenter` のカメラ外 early return に依存している。現状は compile / PlayMode ログで統合確認はできているが、以下の挙動を機械的に保証するテストがない。

- 同じ sprite / height / flip / rotation を再設定しても不要な Transform / SpriteRenderer 更新が起きない
- カメラ外 Actor は `SpriteRenderer.enabled = false` になり、画面内へ戻ると再び表示される
- カメラ外の間に位置が変わっても、画面内復帰時に最新位置・向きで表示される

原因:

最適化を View component の内部キャッシュとして実装したが、既存テストは Application / GameLoop 側が中心で、View 層の低レベルな差分適用を検証する fixture が不足している。

解決案:

まず `ActorView` 単体の EditMode test を追加し、`SetSprite` / `SetVisualCanvasHeight` / `SetVisible` / `SetRotationY` の基本契約を検証する。`WorldActorPresenter` のカメラ外判定は Camera を含む fixture が必要になるため、可能なら別テストで「offscreen -> hidden」「onscreen -> enabled」を確認する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldCameraController.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/`

完了条件:

- [ ] `ActorView` の差分適用を検証する EditMode test が追加されている
- [ ] カメラ外 Actor の `SpriteRenderer.enabled` が false になることを検証する EditMode test が追加されている、または PlayMode/統合テストで明示的に確認されている
- [ ] 画面内復帰時に `SpriteRenderer.enabled` が true に戻ることを検証している
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する

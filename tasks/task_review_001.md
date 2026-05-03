# task_review_001: コードベース全体レビュー

## 目的

現時点のコードベースを以下の観点でレビューし、問題点をすべて列挙してください。
実装作業は行わない。問題の報告のみ。

## レビュー対象ディレクトリ

```
Client/Assets/DungeonInn/Runtime/Scripts/
```

## レビュー観点

### 1. AGENTS.md の禁止事項チェック

以下をコード全体で grep して違反を探す:

- `Addressables.LoadAssetAsync` の直接呼び出し
- `Resources.Load` の使用
- `SceneManager.LoadScene` / `LoadSceneAsync` の直接呼び出し（Launcher.cs の Reboot() は意図的な例外として許容）
- `Task` / `ValueTask` の使用（`UniTask` 以外の非同期）
- `UnityEngine.UI.Button` の直接使用（`LHButton` 以外）
- `Keyboard.current` / `Mouse.current` の直接ポーリング
- `Input.GetKey` / `Input.GetAxis` の使用
- `WaitForCompletion()` の使用

### 2. アーキテクチャ依存ルールチェック（docs/domain-design.md 参照）

```
Domain 層   → Unity 依存禁止、Lighthouse 依存禁止、UniTask 禁止
Application 層 → Unity 禁止、Lighthouse 禁止、UniTask 必須
Infrastructure 層 → IAssetScope のみ許可
View 層     → 制限なし
```

- Domain 層のファイル（Scripts/Domain/）に Unity / Lighthouse の using がないか
- MonoBehaviour が Domain 層にないか

### 3. namespace チェック

- グローバル namespace のクラスがないか（すべてのクラスは namespace を持つべき）
- ファイルパスと namespace が一致しているか

### 4. domain-design.md との整合性チェック

docs/domain-design.md に定義されたクラス・インターフェース・メソッドシグネチャと
実際の実装が一致しているか確認する。

主な確認対象:
- InnLand: PurchaseParcel, CanPurchaseAt, GetRoomContaining, AddFunds, TrySpendFunds, AddBeds
- Room: OpenDoor, CanPlaceBedAt, PlaceBed, RemoveBed, IsWall, Contains, Density
- CharacterBase: TakeDamage, Heal, IsAlive
- AdventurerCharacter: UpdateSatisfaction, TransitionState
- InnFeeCalculator: Calculate シグネチャ
- SatisfactionCalculator: Calculate シグネチャ

### 5. その他気になる問題

上記以外で気になった設計上・実装上の問題があれば報告する。

## 完了条件

- `review/task_review_001_done.md` に以下を記載:
  - 発見した問題の一覧（問題なければ「問題なし」）
  - 各問題のファイルパス・行番号・内容
  - 重大度（高/中/低）

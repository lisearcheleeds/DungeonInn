# 整合性レビュー報告（Milestone 5 Phase 1）

## 概要

DungeonInnプロジェクト（Milestone 5 Phase 1完了時点）の整合性は概ね良好です。コンパイルに成功し、テストも通過しています（Compilation 0 errors, Tests 215 passed）。ただし、以下のマイナー改善が必要です。

**整合性スコア**: 7.5 / 10

主な所見：
- **禁止API検出**: `Resources.LoadAsync` の使用（ProductAssetLoader）
- **コーディングルール違反**: lambda変数の単一文字名、不要なasync修飾子
- **設計ドキュメントとの差異**: `AdvanceActorAiUseCase` → `AdvanceActorAiOrchestrator` への変更が文書化されていない
- **クラス名の冗長性**: `FirstSceneScene` の命名
- **DI登録**: 問題なし、全て整合している

---

## 問題一覧

### [整合-1] Resources.LoadAsync の直接使用（禁止API）

**重要度**: 高  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:32`

**問題**:
```csharp
var request = Resources.LoadAsync<GameObject>(screenStackAddress);
```
Milestone 5ロードマップにも「アセットロードは Lighthouse の asset loading 方針に従い、禁止 API を使わない」と明記されている禁止APIが使用されている。

**原因**: ProductAssetLoaderがスクリーンスタック生成のための基盤実装で、禁止APIの使用を避ける設計がまだ完成していない。

**解決案**:
- Lighthouse/LighthouseExtendsの公式アセットローディング仕組みを確認し、それに従うよう変更する。
- アドレッサブルス対応など別の資産管理パターンへ変更する。

---

### [整合-2] Lambda変数の単一文字命名（コーディング規約違反）

**重要度**: 中  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/GameWorldState.cs:71,110,143,176`

**問題**:
```csharp
// 現在（line 71）
var index = actors.FindIndex(x => x.Id.Equals(actorId));
```
コーディング規約 3-4 で「1文字名を使わない、コレクション名から自然に導かれる単数形を使う」と定義されている。

**原因**: List<>.FindIndex() メソッドの用法で、慣習的に `x` が使われることが多い。

**解決案**:
```csharp
// 推奨
var index = actors.FindIndex(actor => actor.Id.Equals(actorId));
```

---

### [整合-3] 不要な async 修飾子の使用（コーディング規約違反）

**重要度**: 中  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/AdvanceActorAiOrchestrator.cs:51`

**問題**:
```csharp
public UniTask MarkEventAsync(Guid actorId, ActorAiEventType eventType)
{
    scheduler.MarkEvent(actorId, eventType);
    return UniTask.CompletedTask;
}
```
メソッド内で `await` を使用せず `UniTask.CompletedTask` を返しているのに `async` サフィックスがついている。コーディング規約 13-2 で「async が不要な場合は async を付けない」と定義されている。

**原因**: メソッド命名が `Async` サフィックスになっているため、実装時に意識が混在した。

**解決案**: `async` キーワードを削除する（メソッド名の `Async` サフィックスは非同期インターフェースへの適合のため維持）。

---

### [整合-4] 設計ドキュメントと実装の名前乖離（actor-ai-desing.md）

**重要度**: 低  
**場所**: `docs/design/actor-ai-desing.md:40` vs 実装

**問題**:
設計ドキュメントでは以下のように記載されている：
```
UseCase/
  AdvanceActorAiUseCase
  ApplyActorAiDecisionUseCase
```
しかし実装には `AdvanceActorAiUseCase` が存在せず、代わりに `AdvanceActorAiOrchestrator` が実装されている。

**原因**: 設計段階の計画からの変更が、ドキュメントに反映されなかった。

**解決案**: `docs/design/actor-ai-desing.md` を更新し、`AdvanceActorAiUseCase` を `AdvanceActorAiOrchestrator` に修正する。

---

### [整合-5] クラス名の冗長性（FirstSceneScene）

**重要度**: 低  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/QuickFirst/QuickFirstScene.cs:9`

**問題**:
```csharp
public class FirstSceneScene : ProductCanvasMainSceneBase<FirstSceneScene.QuickFirstTransitionData>
```
クラス名が `FirstSceneScene` となっており、「Scene」が2回繰り返されている。

**原因**: ファイル名 `QuickFirstScene.cs` とクラス名が異なることから発生。

**解決案**:
- クラス名を `QuickFirstScene` に変更しファイル名と一致させる。

---

### [整合-6] WorldActorDebugVisualizer ファサードの機能分岐機構が未実装

**重要度**: 低  
**場所**: `View/Scene/MainScene/World/WorldActorDebugVisualizer.cs`

**問題**:
Milestone 5ロードマップでは「既存のdebug表示をすぐ削除せず、移行中は feature flag または明確な差し替え手順で併存できるようにする」と記載されているが、機能分岐機構が実装されていない。

**原因**: Phase 1では分解が完了しただけで、古い表現と新表現の使い分けメカニズムは未実装。

**解決案**:
- デバッグビジュアライザー ON/OFF の切り替え手段を実装する（設定フラグ または コンパイルディレクティブ）。
- または、既存 PlayMode がこのファサードを使い続ける設計であることを文書化する。

---

## 禁止API検索結果

以下の検索コマンドで確認すべき項目：
```powershell
rg "Addressables\.LoadAssetAsync|Resources\.Load|Resource\.Load|SceneManager\.LoadScene|UnityEngine\.UI\.Button|Task<|ValueTask<|WaitForCompletion|\.Result" Client/Assets
```

検出済み違反：
- `Resources.LoadAsync` → `Infrastructure/AssetLoader/ProductAssetLoader.cs:32`

未検出（問題なし）：
- `Addressables.LoadAssetAsync` の直接使用
- `UnityEngine.UI.Button` の使用
- `Task<` / `ValueTask<`

---

## 総評

### 良い点

1. **Clean Architecture の整合性が保たれている** — Domain層は UnityEngine への依存がない。
2. **DI登録が完全で漏れがない** — WorldLifetimeScope の登録と実装クラスが一致している。
3. **イベント・リアクティブ購読が適切に管理されている** — R3 の `Subscribe().AddTo(ref bag)` パターンで自動破棄が保証されている。
4. **コンパイル成功・テスト通過** — Milestone 5 Phase 1 の完了条件をほぼ満たしている（0 errors, 215 passed）。

### 改善が必要な点（優先順位順）

| 優先度 | 問題 | 作業量 |
|---|---|---|
| 高 | Resources.LoadAsync の禁止API排除 | 中（Lighthouse 方針確認が必要） |
| 中 | GameWorldState.cs の lambda 変数修正 | 小 |
| 中 | AdvanceActorAiOrchestrator の async 削除 | 小 |
| 低 | actor-ai-desing.md の更新 | 小 |
| 低 | QuickFirstScene のクラス名修正 | 小 |

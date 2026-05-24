# 実装品質・レビューガイドライン

このドキュメントは、命名・DI・テスト・定数・TODO 管理など、コードを書く際・レビューする際の判断基準をまとめる。
特定プロジェクト専用のルールではなく、別プロジェクトでも同じ判断を再現するための指針とする。

「動く」ことと「正しい設計である」ことは別である。
動作の確認前に、型・名前・責務・依存が設計の意図と一致しているかを見る。
レビューは「バグを見つける場」ではなく「設計の意図を明文化し、コードと一致させる場」である。

---

## ハードゲート

この節は即時停止・修正が必要な禁止事項を列挙する。
この節に載っていない実装が自動的に許可されるわけではなく、本文の設計方針・判断基準に反する場合もレビュー指摘または作業停止対象とする。

- [ ] docs にない将来拡張判断を、ユーザー確認なしに実装していない
- [ ] DI で解決すべき依存を Runtime constructor / Runtime 経路で手動 `new` していない
- [ ] Runtime コードにテスト用 / 互換用 / デバッグ用 constructor を追加していない
- [ ] Runtime constructor に optional parameter（`= null` 等）でデフォルト依存を生成する経路を追加していない
- [ ] DI 注入型と LifetimeScope / Installer の登録型が一致している
- [ ] テスト都合で Runtime public / internal API、constructor、interface、DTO、Request、Event を増やしていない
- [ ] 空の `IDisposable.Dispose()` を追加していない
- [ ] 実挙動・性能に影響する TODO を milestone / task に紐付けずに残していない
- [ ] 内部バッファ参照を、コントラクト不明な API として返していない
- [ ] 一般パターンに反する意図的設計を、根拠記録なしに追加していない
- [ ] DI constructor（非MonoBehaviour）内で UnityEngine.Object（Texture2D / Sprite / Material / Mesh / GameObject 等）を生成していない
- [ ] LifetimeScope / Installer にゲームコンテンツ Prefab、UI View Prefab、Popup View 実体を `SerializedField` していない
- [ ] View 実体を直接 DI 登録せず、Presenter / Pool / Factory の責務境界を通して操作している
- [ ] 別 Scene / 別 LifetimeScope が所有する Canvas / View / Presenter / Pool / scene-owned component を直接参照・操作していない
- [ ] 既存 guideline 上で適切な命名・責務・設定配置が判断できるのに、「最小差分」を理由に曖昧な旧名・不適切な責務・互換用 API を残していない
- [ ] System / bootstrap 層のクラスに、Content / GameSession 固有の開始処理・状態生成・コンテンツ遷移を混ぜていない

## 完了前チェックリスト

このチェックリストは本文の設計方針を省略するためのものではない。
実装・レビュー時は本文を確認したうえで、最後に確認漏れを防ぐ目的で使用する。

- [ ] 型・クラス名・テストダブル名が実際の振る舞いと一致している
- [ ] インターフェース名が現在の実装者ではなく能力・役割を表している
- [ ] 将来拡張が予見できる空実装は、ユーザー確認済みで意図が明確である
- [ ] 調整可能な値・重複する数値は名前付き定数になっている
- [ ] テストダブルのスコープが必要最小限で、依存の広さを隠していない
- [ ] 変更リスクに応じて EditMode test / run-tests / Play mode の確認順序を選んだ
- [ ] キャッシュ値・Dictionary key・constructor 代入はテストで検出できる
- [ ] DI 注入された依存が実際に使われ、不要依存を残していない
- [ ] `IReadOnlyList<T>` property や `All` / `Values` 系 property の裏で allocation が隠れていない
- [ ] TODO は責務・milestone・task のいずれかに紐付いている
- [ ] 内部バッファを返す API は `Consume` / `Drain` / `Flush` 等の名前またはコメントで契約を明示している
- [ ] 一般パターンに反する設計は docs/design または task ログに根拠を記録している
- [ ] UnityEngine.Object を保持するコレクションで、スコープ無効化（InvalidateXxx）と Dispose のクリーンアップパスが対称に実装されているか確認した
- [ ] Fallback / Placeholder アセット生成の定数・ロジックが複数クラスに重複していないか確認した
- [ ] LifetimeScope がコンテンツ catalog 化していないか確認した
- [ ] Popup / HUD / View の操作入口が Presenter / Pool / Factory に限定されているか確認した
- [ ] Scene / LifetimeScope 境界を跨ぐ参照が、具象 View ではなく抽象 interface / Application service / 所有者側 Presenter 経由になっている
- [ ] 最小差分を理由に、本文ルールに沿ったリネーム・責務移動・不要 API 削除を省略していない
- [ ] System と Content の境界が名前・namespace・配置・依存方向から読み取れる

---

## 1. 型・クラス名は実際の振る舞いと一致させる

名前は読者との契約である。
名前から期待される振る舞いと実際の振る舞いが食い違うと、コードを読んだ人が誤解し、バグや誤用の温床になる。
テストダブルは特に「何をする・しないクラスか」が名前に表れていなければならない。

「最小差分」を理由に、実際の意味と合わない既存名を別名・互換名として残してはならない。
たとえば高さと幅を分離する変更で、旧名 `Size` が幅にも高さにも読めるなら、旧名を残さず `Width` / `Height` などの正しい名前へ置き換える。
互換維持が本当に必要な場合は、外部契約・保存データ・公開 API などの具体的な理由を task または review ログに記録する。

### 最小差分を理由に省略してはいけない対応

- 既存 guideline に正しい命名・責務・配置の判断基準がある場合、その対応を省略しない
- 曖昧な旧名を alias として残さない
- 互換目的だけの Runtime public / internal API を残さない
- 正しい責務の所有者が明確な場合、呼び出し元都合で不適切なクラスに処理を残さない
- テスト修正量や差分量を減らすために、設計上の完了条件を弱めない

### Before

```csharp
sealed class NoOpMasterRepository : IMasterRepository
{
    public ItemMaster GetItemMaster(int itemId) => throw new NotSupportedException();
    // NoOp（何もしない）ではなく、例外を投げる
}
```

### After

```csharp
sealed class ThrowingMasterRepository : IMasterRepository
{
    public ItemMaster GetItemMaster(int itemId) => throw new NotSupportedException();
    // 呼ばれたら必ず例外を投げる、という事実が名前に表れている
}
```

### テストダブルの命名目安

| 振る舞い | 適切な名前の例 |
|---|---|
| 呼ばれたら例外を投げる | `ThrowingXxx`, `AlwaysFailingXxx` |
| 固定値を返す | `StubXxx`, `FakeXxx`, `FixedXxx` |
| 呼び出しを記録する | `SpyXxx`, `CapturingXxx`, `CollectingXxx` |
| 何もせず正常終了する | `NoOpXxx`, `SilentXxx` |
| 実際の実装に近い軽量版 | `FakeXxx` |

プロダクションコードでも同様に、「例外を投げる可能性があるか」「状態を変える副作用があるか」が名前から読み取れることが望ましい。

---

## 2. インターフェース名は「現在の実装者」ではなく「能力・役割」を表す

インターフェースに実装クラスの名前を入れると、「現在それを実装しているのが誰か」という実装の都合が型に固定される。
インターフェースが表すべきものは「何ができるか（能力）」である。

### Before

```csharp
public interface IMonsterDropSource
{
    IReadOnlyList<ActorDropEntry> DropTable { get; }
}
```

モンスターしか実装していない現時点では不自然に見えないが、将来冒険者やトラップなどがドロップ機能を持つと、
`IMonsterDropSource` を実装するという矛盾が生まれる。

### After

```csharp
public interface IActorDropSource
{
    IReadOnlyList<ActorDropEntry> DropTable { get; }
}
```

「アイテムをドロップする能力を持つ Actor」という意味になる。

### 適用基準

- インターフェース名に現在の実装クラス名（`Monster`, `Player`, `Admin` 等）を含めない
- 命名の目安：「このインターフェースを全く別のクラスが実装するとしたら、名前は依然として適切か」を確認する

---

## 3. 将来の拡張が予見できる場合、現時点でインターフェースを定義し空実装を置く

拡張の可能性が予見できる場合、将来の実装者になりうる型に対して現時点でインターフェースを実装させ、
今は空実装にしておく。こうすることで将来の追加が「既存コードの変更」ではなく「既存コードの充填」になる。

**ただし、実装者（Claude Code / Codex）が独断でこの判断を行わず、拡張の可能性が見えた場合はユーザーに提案・確認してから実装する。**

### Before

```csharp
// MonsterBehavior だけが実装。冒険者ドロップが必要になったとき、
// 呼び出し側の is MonsterBehavior チェックを書き換える必要がある
if (actor.Behavior is MonsterBehavior monster)
{
    foreach (var entry in monster.DropTable) { ... }
}
```

### After

```csharp
// AdventurerBehavior は現在ドロップしないが、インターフェースを実装しておく
public sealed class AdventurerBehavior : IActorBehavior, IActorDropSource
{
    public IReadOnlyList<ActorDropEntry> DropTable => Array.Empty<ActorDropEntry>();
}

// 呼び出し側は IActorDropSource だけを知ればよい
if (actor.Behavior is IActorDropSource dropSource)
{
    foreach (var entry in dropSource.DropTable) { ... }
}
```

冒険者がドロップするようになっても、`AdventurerBehavior.DropTable` を返すだけで呼び出し側は変わらない。

### 適用基準

- 空実装は「手抜き」ではなく「意図的な拡張の受け口」として扱う
- 「将来これを実装する可能性があるか」が不明な場合は、確認なしに空実装を追加しない

---

## 4. 調整可能な値はマジックナンバーにしない。定数に名前をつける

ゲームバランス値・リトライ上限・タイムアウト秒数など、後から調整が想定される数値を式にインラインで書かない。

### Before

```csharp
var xpReward = Math.Max(1, defeated.Experience / 10);
//                      ↑              ↑ 何の意味か名前がない
```

### After

```csharp
// GameConstants.Combat.cs
public const int KillExperienceRewardNumerator   = 1;
public const int KillExperienceRewardDenominator = 10;
public const int KillExperienceRewardMinimum     = 1;

var xpReward = Math.Max(
    GameConstants.KillExperienceRewardMinimum,
    defeated.Experience * GameConstants.KillExperienceRewardNumerator
        / GameConstants.KillExperienceRewardDenominator);
```

### 定数に抽出すべき値 / しなくてよい値

| 抽出すべき | しなくてよい |
|---|---|
| ゲームバランスに関わる数値（報酬倍率、確率、しきい値） | ドメインの不変条件として変わりえない値（`if (count < 1)` の `1` など） |
| 試行回数・タイムアウト・ページサイズなど運用調整が想定されるもの | 意味が自明な `0` や `true` |
| 同じ数値が複数箇所に出てきているもの | |

定数ファイルの分割は関心ごとに行う。`GameConstants.Combat`、`GameConstants.Inn`、`GameConstants.Map` のように文脈で分け、巨大な1ファイルにしない。

---

## 5. DI に注入する型は、コンテナへの登録型と一致させる

コンストラクタで要求するインターフェースが、DI コンテナに登録されている型と一致していなければ実行時エラーになる。
インターフェースの継承関係があっても、コンテナは自動的に親インターフェースへのマッピングを行わない場合がある。

### Before

```csharp
public sealed class DropItemUseCase
{
    [Inject]
    public DropItemUseCase(IItemMasterRepository itemMasterRepository, ...)
    // IItemMasterRepository は DI コンテナに登録されていない
}

// LifetimeScope
builder.Register<HardcodedMasterRepository>(Lifetime.Scoped).As<IMasterRepository>();
// IMasterRepository : IItemMasterRepository だが、IItemMasterRepository は別途登録していない
```

### After

```csharp
public sealed class DropItemUseCase
{
    [Inject]
    public DropItemUseCase(IMasterRepository masterRepository, ...)
    // 実際に登録されている型を使う
}
```

### 適用基準

- 新しい UseCase を作るとき、コンストラクタで要求する型が LifetimeScope に登録されているか確認する
- インターフェースを分割したい場合は、LifetimeScope に `.As<IItemMasterRepository>()` を追加するか、設計を見直す
- レビュー時は「コンストラクタの型一覧」と「LifetimeScope の登録一覧」を照合する

---

## 6. テストダブルのスコープは必要最小限にする

テストダブルは「そのテストが必要とする振る舞いだけ」を実装する。
過剰な実装はテストの意図を曇らせ、テスト自体のメンテナンスコストを上げる。

### Before

```csharp
// DropItemUseCase のテストなのに、全メソッドを実装したフル IMasterRepository スタブ
sealed class StubItemMasterRepository : IMasterRepository
{
    public ItemMaster GetItemMaster(int itemId) => masters[itemId];
    public EquipmentMaster GetEquipmentMaster(int itemId) => throw new NotSupportedException();
    public WeaponMaster GetWeaponMaster(int itemId) => throw new NotSupportedException();
    // ... 多数のメソッド
}
```

`DropItemUseCase` が実際に呼ぶのは `GetItemMaster` だけ。

### After

```csharp
// DropItemUseCase はマスタリポジトリ不要になったため、スタブ自体が消えた
var useCase = new DropItemUseCase(new FixedGameRandom(0), new CollectingEventBus());
```

### 適用基準

- テストで使わないメソッドが多いスタブは「このクラスの依存が広すぎないか」を疑う
- テストダブルに `throw new NotSupportedException()` が多い場合、インターフェースを分割できないか検討する

---

## 7. 実装後の確認順序：テスト → run-tests → Play モード

動作確認は「広い網」から「狭い観察」へ順番に行う。

```
1. 新機能をカバーするテストを書く（EditMode テスト）
2. uloop run-tests で全テストパスを確認
3. uloop control-play-mode で Play モード起動、ログで実際の挙動を確認
```

Play モード確認だけでは「どのケースが壊れているか」の特定が難しく、原因の調査に時間がかかる。
テストが書けない設計（依存が多すぎる、構築が複雑すぎる）は設計を見直すシグナルである。

### Before（不十分な確認）

Play モードだけで確認 → `LevelTable.Id = 0` 問題が後回しになる。どのユースケースが壊れているか絞り込めない。

### After（正しい順序）

```
テストを書く
  ↓ GetLevel / GetExperienceForLevel / RecalculateLevel を単体でカバー
run-tests
  ↓ LevelTable.Id 代入漏れが即座に検出される
Play モード確認
  ↓ テストで保証された状態からの統合確認
```

### 適用基準

- 「動くかどうか」より「何が保証されているか」を先に確保する
- Play モード確認は「統合シナリオとして成立するか」を確認する場であり、ロジック正しさの確認はテストで行う

---

## 8. キャッシュ値の更新漏れはテストで検出する

キャッシュを持つ設計では、キャッシュと元データの不整合がバグになる。
「構築後にキャッシュが正しいこと」を必ずテストで保証する。

### Before（テストなし → 実行時に発覚）

```csharp
public LevelTable(int id, int[] cumulativeXp)
{
    // Id = id; を書き忘れ → Id が 0 のまま
    this.cumulativeXp = cumulativeXp;
}
```

`ToDictionary(x => x.Id)` で全エントリが `Id = 0` になりキー重複例外が発生。Play モード起動まで発覚しなかった。

### After

```csharp
[Test]
public void LevelTableIdIsCorrectlyAssigned()
{
    var table = new LevelTable(2, new int[101]);
    Assert.That(table.Id, Is.EqualTo(2));
}
```

### 適用基準

- 新しいマスタデータクラスやエンティティを作ったら、コンストラクタのプロパティ代入を検証するテストを最初に書く
- キャッシュ値は「変更を引き起こした操作後に更新されること」もテストする
- Dictionary のキーになるプロパティ（`Id` 等）の代入漏れは特に検出困難なため、優先的にテストする

---

## 9. TODO を責務で分類しマイルストーンに紐付ける

TODO が「将来の改善」ではなく現在のゲーム挙動に影響しているケースで、
仕様不一致として扱われずにマイルストーンが完了とみなされてしまうことがある。

TODO を以下の4種類に分類し、それぞれ対応を決める。

| 種別 | 扱い |
|---|---|
| 実挙動に影響する（仕様と差異がある） | 不具合・仕様不一致として task 化し、milestone に紐付ける |
| 性能に影響する | milestone の前提条件として task 化する |
| 将来拡張 | backlog へ記録。`// TODO(backlog):` で明示 |
| コメントのみ（設計 docs に移すべき内容） | 削除または設計 docs へ移す |

実挙動・性能に影響する TODO は `// TODO(milestone:X): 理由` 形式で記録し、task ファイルを作成する。

```csharp
// Before: 理由も対応予定も不明な TODO
// TODO: 重み付き抽選に変更する
return candidates[Random.Range(0, candidates.Count)];

// After: milestone 番号と理由を明示し、task ファイルを作成する
// TODO(milestone:6): 重み付き抽選に変更する。現在は等確率。
// 理由: milestone:5 では重みデータが未準備のため省略
return candidates[Random.Range(0, candidates.Count)];
```

マイルストーン完了レビューのチェックリストに「そのマイルストーン番号の TODO が残っていないか」を追加する。

### レビュー観点

- TODO が現在の挙動を仕様と違うものにしていないか
- milestone 番号のない TODO が実挙動に影響していないか
- マイルストーン完了時に、そのマイルストーン番号の TODO が残っていないか

### DungeonInn Example

`SpawnTableUseCase` が重みを無視して等確率選択している状態でマイルストーンが完了とみなされ、
次のレビューで「仕様差分」として発覚した。

---

## 10. System 層と Content / GameSession 層を混ぜない

System 層は、アプリケーションを起動・維持するための基盤である。
Content / GameSession 層は、ユーザーが開始した 1 回のゲームセッションや、その中で表示・操作されるコンテンツを扱う。
両者を混ぜると、仮遷移や暫定初期化が恒久設計に見え、責務と依存方向が歪む。

### 判断基準

| 分類 | 置いてよいもの | 置いてはいけないもの |
|---|---|---|
| System / Bootstrap | Root / Product 起動、共通 service 登録、言語・入力・Asset 管理、最初の入口画面への遷移 | 実ゲームセッション生成、World / Battle / Dungeon などコンテンツ固有の開始判断 |
| GameSession | NewGame / Continue / Load で生成される状態、Application service、ゲーム進行 state | Root / Product 起動、アプリ全体の常駐 service |
| Content / MainScene | 開始済みセッションを表示・操作する scene object / presenter / input | セッション生成そのもの、Product 初期化 |
| Module / UI | HUD / Popup / ScreenStack / 補助 UI の表示と入力 | MainScene 具象型の直接操作、System 起動処理 |

### ルール

- `Launcher` / `Bootstrap` / `ProductEntryPoint` という名前のクラスは、特定ゲームコンテンツを直接開始しない。
- `World` / `Battle` / `Dungeon` / `Inn` などのコンテンツ名を持つ型は、System 層へ置かない。
- `MainGame` / `GameSession` などのセッション名を持つ scope や controller は、Product 起動処理ではなくセッション開始フローから呼ぶ。
- 一時的に System 層から Content を呼ぶ場合は、TODO に「なぜ一時的か」「正式フローでどこへ移すか」「いつ削除するか」を書く。
- 入口画面やメニューが既に存在するなら、System から Content へ直行する仮実装を残さず、先に正式フローへ寄せる。

### Controller / Manager の配置

`Controller` / `Manager` は名前だけでは責務が分からないため、配置と依存方向で所有者を明確にする。

- Product / System に置く controller は、Product 全体の起動・終了・再起動だけを扱う。
- GameSession に置く controller は、セッションの生成・破棄・保存・復元だけを扱う。
- MainScene / ModuleScene に置く controller は、その scene / module が所有する View・Input・Presenter だけを扱う。

次のようなクラスは配置を見直す。

```csharp
// NG: System 層の controller が game session scope の生成と content scene 遷移を両方扱う
public sealed class MainGameLifetimeScopeController
{
    public void CreateGameScope() { ... }
    public UniTask TransitionToWorld() { ... }
}
```

```csharp
// OK: 入口画面のユーザー操作が session 開始を明示し、その後 content scene へ遷移する
public sealed class TitlePresenter
{
    public void StartNewGame()
    {
        gameSessionController.CreateSession();
        sceneManager.TransitionScene(new WorldTransitionData()).Forget();
    }
}
```

### レビュー観点

- System 層のクラスが Content 固有 namespace を参照していないか
- Product 起動時に GameSession scope / state が作られていないか
- ユーザー操作なしに NewGame / Continue 相当の処理が走っていないか
- 一時実装 TODO が正式フロー実装後も残っていないか
- クラス名、namespace、登録 scope が同じ所有者を指しているか

---

## 11. 内部バッファを返す API はコントラクトを明示する

メソッドが内部バッファへの参照を返す場合（例: `List<T>` の実体を返す）、「呼び出し後に内部状態が変化する」「呼び出し側はコピーを保持してはならない」などのコントラクトをメソッド名またはコメントで明示する。

代替案として、配列コピー・`ReadOnlyCollection<T>`・immutable snapshot を返す、または callback 形式で同期消費させる設計を検討する。
`IReadOnlyList<T>` として返すだけでは内部の `List<T>` 実体への参照は同じままであるため、内部バッファ参照問題は解決しない。

### Before

```csharp
public List<ActorViewData> ConsumeChanges()
{
    var result = changedActors;  // NG: 内部バッファをそのまま返している
    changedActors = new List<ActorViewData>();
    return result;
}
```

### After

```csharp
// コントラクトを名前で明示: Consume = 呼び出し後に内部バッファがクリアされる
public ActorViewData[] ConsumeChangesAndClear()
{
    var snapshot = changedActors.ToArray();  // コピーを返す
    changedActors.Clear();
    return snapshot;
}
```

### 適用基準

- メソッド名に「Consume」「Drain」「Flush」など副作用を示す語を含める
- 返した後も内部バッファが変化する場合はコメントで明記する
- `IReadOnlyList<T>` ラッピングだけで内部バッファを「安全に」返したと思わない

---

## 12. テスト用コンストラクタを Runtime コードに含めない

DI コンテナによって解決されるクラスに、テスト・互換目的で「DI 管理対象の依存を手動 `new` する互換コンストラクタ」を追加してはならない。
テストでは DI コンテナ上でテスト用バインディングを行う、またはテスト専用のファクトリ / fixture を用意する。

**理由:** Runtime コードに互換コンストラクタがあると、誤って本番コードから呼ばれるリスクがあり、DI の恩恵（依存の可視化・ライフタイム管理）が崩れる。また、手動 `new` された依存はライフタイムやスコープが DI の管理外になる。

### Before

```csharp
public sealed class SpawnAdventurerUseCase
{
    // DI 経由の正規コンストラクタ
    public SpawnAdventurerUseCase(IProfileRegistry profileRegistry, IEventPublisher eventBus,
        CompleteActorSpawnUseCase completeSpawnUseCase) { ... }

    // NG: テスト用に追加した互換コンストラクタ。手動 new で依存を生成している
    public SpawnAdventurerUseCase(IProfileRegistry profileRegistry, IEventPublisher eventBus)
    {
        this.completeSpawnUseCase = new CompleteActorSpawnUseCase(profileRegistry, eventBus);
    }
}
```

### After

```csharp
// Runtime コードには正規コンストラクタのみ残す
public sealed class SpawnAdventurerUseCase
{
    public SpawnAdventurerUseCase(IProfileRegistry profileRegistry, IEventPublisher eventBus,
        CompleteActorSpawnUseCase completeSpawnUseCase) { ... }
}

// テスト側でバインディングを構成する
container.Register<CompleteActorSpawnUseCase>(Lifetime.Scoped);
container.Register<SpawnAdventurerUseCase>(Lifetime.Scoped);
```

### 適用基準

- コンストラクタ内で `new` している依存が DI で解決すべきクラスでないか確認する
- optional パラメータ（`= null`）でデフォルト依存を生成するパターンも同じ問題
- テスト用 setup は TestLifetimeScope / fixture / test helper に分離する

---

## 13. IDisposable 実装は空にしない

`IDisposable.Dispose()` を空実装（`{ }`）で放置しない。
購読解除・バッファクリア・ネイティブリソース解放など、実際のクリーンアップが不要であることを確認した上で、その理由をコメントで明示するか、インターフェースの実装そのものを外す。

### Before

```csharp
public sealed class WorldActorPresenter : IDisposable
{
    public void Dispose() { }  // NG: なぜ空なのか不明。実装忘れの可能性がある
}
```

### After

```csharp
// ケース1: 購読解除が必要な場合は実装する
public sealed class WorldActorPresenter : IDisposable
{
    readonly IDisposable subscription;
    public void Dispose() => subscription.Dispose();
}

// ケース2: 本当にクリーンアップが不要な場合はインターフェース自体を外す
public sealed class WorldActorPresenter  // IDisposable を実装しない
{
}
```

### 適用基準

- `Dispose()` が空の場合、「本当に解放すべきリソースがないか」をコードで確認する
- 購読（`Subscribe`）・バッファ（`List`・`Dictionary`）・非同期トークンを保持しているなら Dispose が必要
- 「将来の拡張のため」に空 `IDisposable` を置くのは禁止。必要になったときに追加する

---

## 14. 一般パターンに反する設計は設計ドキュメントに根拠を記録する

「一般的なパターンに反するが意図的にそう設計した」箇所（例: 意図的に空のマーカーインターフェース）は、その設計意図と根拠を設計ドキュメントに記録する。

記録がない場合、後続の開発者・レビュアーが「バグ」または「改善余地」と判断して不必要な変更を加えるリスクがある。
また、レビュー時にも「ドキュメントに記載あり」を根拠として意図的設計を示せるようにする。

**注意:** 「意図的な設計」の記録は、その設計決定の影響範囲を明確にするものでもある。例えば「空マーカーインターフェースは意図的」という記録は「インターフェースを空にしてよい」という意味ではなく、「空であることによって Domain コア側で具体型へのキャストが必要になる問題は別途解決が必要」という認識も合わせて記録する。設計の一側面が意図的であっても、そこから派生する別の問題が免除されるわけではない。

### 適用基準

- コードレビューで「なぜこうなっているのか」の質問が想定されるパターンはドキュメントに記録する
- 設計意図の記録先: 関連する設計ドキュメント（`docs/design/`）または task ファイルのレビューログ
- 記録する内容: 「何を意図しているか」「なぜ一般パターンに従わなかったか」「派生する問題と対処方針」

---

## 15. DI 依存は使う責務だけを注入する

コンストラクタで注入した依存がクラス内で使われていない場合、そのクラスの責務か依存関係のどちらかが古くなっている。
未使用依存は compile には影響しないが、LifetimeScope 登録、テストダブル、レビュー観点を広げ、実際の責務を読みにくくする。

### Before

```csharp
public sealed class ActorCombatPowerCalculator
{
    readonly IMasterRepository masterRepository;

    public ActorCombatPowerCalculator(IMasterRepository masterRepository)
    {
        this.masterRepository = masterRepository;
    }

    public int Calculate(Actor actor)
    {
        return actor.Stats.Strength + actor.Stats.Dexterity;
    }
}
```

### After

```csharp
public sealed class ActorCombatPowerCalculator
{
    public int Calculate(Actor actor)
    {
        return actor.Stats.Strength + actor.Stats.Dexterity;
    }
}
```

### 適用基準

- constructor parameter / field が実処理で使われていない場合は削除する
- 将来使う予定の依存は追加しない。必要になった task で追加する
- 依存削除時は LifetimeScope 登録、テスト fixture、stub / fake も合わせて狭める
- 使っているように見えるが nullable guard だけで終わる依存も未使用として扱う

---

## 16. Property に allocation を隠さない

`IReadOnlyList<T>` や `IReadOnlyDictionary<TKey, TValue>` を返す property は、呼び出し側から見ると軽量な参照取得に見える。
その裏で `ToArray()`、`ToList()`、`new List<T>()`、LINQ chain を実行すると、呼び出し頻度の高い経路で allocation が見えにくくなる。

### Before

```csharp
public IReadOnlyList<EquipmentMaster> All => equippedMasters.Values.ToArray();

public IReadOnlyList<StatBonus> AllStatBonuses
{
    get
    {
        var result = new List<StatBonus>();
        foreach (var equipment in equippedMasters.Values)
        {
            result.AddRange(equipment.StatBonuses);
        }

        return result;
    }
}
```

### After

```csharp
public IReadOnlyDictionary<EquipmentSlot, EquipmentMaster> EquippedMasters => equippedMasters;

public void CopyAllStatBonusesTo(List<StatBonus> results)
{
    results.Clear();
    foreach (var equipment in equippedMasters.Values)
    {
        results.AddRange(equipment.StatBonuses);
    }
}
```

### 適用基準

- property getter は原則として O(1) 参照取得か、明確に安い計算に限定する
- allocation を伴う場合は `CreateSnapshot` / `CopyTo` / `DrainTo` など、名前でコストと契約を表す
- Frame Loop / Entity Loop / AI 評価 / 戦闘評価から呼ばれる property では、`ToArray()` / `ToList()` / LINQ chain を禁止する
- `IReadOnlyList<T>` にしても、生成済み配列や内部 list の寿命問題は解決しない。snapshot なのか一時 buffer なのかを明示する

---

## 17. DI コンストラクタで UnityEngine.Object を生成しない

`[Inject]` コンストラクタ（非MonoBehaviour）内で `new Texture2D()`・`Sprite.Create()`・`new Material()`・`new Mesh()`・`GameObject.CreatePrimitive()` などを呼び出さない。

DI コンテナが依存を解決するフェーズ（Scene起動時）で予測しにくい副作用が生じる。
EditMode テストでインスタンス化すると UnityEngine.Object が残留し、手動の `DestroyImmediate` 管理が必要になる。
一見 Pure クラスに見えるが Unity MainThread 依存になるため、テスト・再利用コストが上がる。

初期化が必要な場合は `LoadAsync` / `Initialize()` / Factory / EditorSetup 経由に分離すること。

### Before

```csharp
public sealed class ActorSpriteVisualConfig
{
    [Inject]
    public ActorSpriteVisualConfig(VisualConfigLoader loader)
    {
        // NG: DI コンストラクタ内で UnityEngine.Object を生成している
        var texture = new Texture2D(32, 48, TextureFormat.RGBA32, false);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 48), new Vector2(0.5f, 0f), 16f);
        placeholderSprites.Add(ActorBehaviorType.Adventurer, sprite);
    }
}
```

### After

```csharp
public sealed class ActorSpriteVisualConfig
{
    [Inject]
    public ActorSpriteVisualConfig(VisualConfigLoader loader)
    {
        // OK: コンストラクタは依存の保持のみ
        this.loader = loader;
    }

    public void Initialize()
    {
        // UnityEngine.Object 生成はここで行う
        var sprite = ActorSpritePlaceholderFactory.Create(color, out var texture);
        placeholderTextures.Add(texture);
        placeholderSprites.Add(ActorBehaviorType.Adventurer, sprite);
    }
}
```

### 適用基準

- コンストラクタ内の `new Texture2D` / `Sprite.Create` / `new Material` / `new Mesh` は即座にレビュー対象とする
- 例外的に許容するケース: `Initialize()` / `LoadAsync()` / Factory メソッド / EditorSetup 経由での生成
- `MonoBehaviour.Awake()` / `Start()` 内での生成は許容する（MonoBehaviour ライフサイクルの範囲内）

---

## 18. LifetimeScope をコンテンツ Catalog にしない

`LifetimeScope` / Installer は DI の composition root であり、ゲームコンテンツや UI View の一覧を保持する場所ではない。
コンテンツ種別が増えるたびに `SerializedField` が増える `LifetimeScope` は、責務が膨らみ、Prefab 選択の発生元が読めなくなる。

### 禁止

```csharp
public sealed class WorldLifetimeScope : LifetimeScope
{
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] GameObject areaEffectPrefab;
    [SerializeField] ActorStatusView actorStatusViewPrefab;
    [SerializeField] ActorDetailPopup actorDetailPopup;
}
```

### 推奨

- Projectile / AreaEffect / Prop などのゲームコンテンツ Prefab は、発生元 Master / Spec / Definition から visual id / Addressable address を解決する。
- UI View Prefab は UI ModuleScene 用 Factory / Pool が Addressable 経由で生成する。
- Popup View は Presenter の内部実装として保持し、他クラスが View 実体を直接 DI できる登録を避ける。
- `LifetimeScope` に置いてよい `SerializedField` は、Scene root、設定 ScriptableObject、composition に必要な scene-owned component に限定する。

```csharp
public sealed class WorldLifetimeScope : LifetimeScope
{
    [SerializeField] WorldScene worldScene;
    [SerializeField] WorldCameraSettingsSO worldCameraSettingsSO;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(worldScene);
        builder.Register<WorldViewFactory>(Lifetime.Scoped).AsSelf();
        builder.Register<WorldProjectileViewPool>(Lifetime.Scoped);
    }
}
```

### レビュー観点

- `LifetimeScope` の `SerializedField` が増えた場合、Scene root / 設定 SO / scene-owned component のいずれかか確認する。
- Prefab や View 実体であれば、Addressable Factory / Pool / Presenter へ移す。
- 「暫定」「最小差分」を理由に直接参照を残さない。コンテンツ増加時に必ず破綻するため、ハードゲート違反として扱う。

---

## 19. Scene / LifetimeScope 所有物を他 Scene から直接操作しない

Scene-owned component、Canvas、View、Presenter、Pool、Factory、EntryPoint は、その Scene / ModuleScene / LifetimeScope が所有する。
別 Scene がそれらを直接参照して操作すると、所有者、初期化順、破棄順、描画責務、テスト境界が同時に壊れる。

### 禁止

```csharp
// NG: MainScene が ModuleScene の Canvas / View / Pool を保持して操作する
public sealed class WorldHudController
{
    readonly Canvas hudCanvas;
    readonly ActorStatusViewPool actorStatusViewPool;

    public void UpdateHud()
    {
        actorStatusViewPool.UpdateAll(hudCanvas);
    }
}
```

```csharp
// NG: hierarchy 探索や serialized reference で別 Scene の所有物を掴む
var hudCanvas = Object.FindFirstObjectByType<Canvas>();
```

### 推奨

- HUD / Popup / EventLog / Minimap などの UI は UI ModuleScene が所有し、ModuleScene 側の Presenter / EntryPoint が初期化・更新する。
- MainScene 固有情報が必要な場合、MainScene は抽象 interface を登録し、ModuleScene はその interface だけを読む。
- 共有ゲーム状態は GameSession / Application scope に置き、MainScene と ModuleScene は同じ state / query / store を読む。
- View 実体の生成と破棄は、所有する ModuleScene の Factory / Pool / Presenter に閉じる。

```csharp
// OK: ModuleScene 側が HUD 更新を所有する
public sealed class HudEntryPoint
{
    readonly ActorHudPresenter actorHudPresenter;

    void Update()
    {
        actorHudPresenter.UpdatePositions();
    }
}
```

```csharp
// OK: MainScene 固有情報は抽象 interface として渡す
public sealed class ActorHudPresenter
{
    readonly IActorScreenPositionProvider screenPositionProvider;
}
```

### レビュー観点

- `Canvas` / `RectTransform` / `View` / `Presenter` / `Pool` が、所有 Scene 以外の namespace から参照されていないか
- MainScene が ModuleScene の型を inject していないか
- ModuleScene が MainScene の具象 controller / registry / camera を inject していないか
- `FindObjectOfType` / hierarchy 探索 / scene serialized reference で他 Scene の所有物を取得していないか
- 更新ループが所有者側にあるか。HUD の更新なら HUD ModuleScene 側、World 3D 表示なら World MainScene 側にあるか

---

## 20. UnityEngine.Object を保持するクラスは invalidation 単位と Dispose 単位を揃える

スコープ（Layer・Floor・Actor等）をキーとした UnityEngine.Object のコレクションを持つクラスは、そのスコープが無効化された際に対応するリソースをクリーンアップするメソッドを別途実装すること。`Dispose` のみに依存しない。

「親 GameObject を Destroy したから子の参照リストも大丈夫」とみなさない。
Unity のオブジェクト破棄後も C# 側の参照は List / Dictionary に残留し続ける。

### Before

```csharp
public sealed class EnvironmentObjectPlacer : IDisposable
{
    readonly List<GameObject> placedObjects = new();

    public void PlaceChunkProps(Transform parent, ...) { ... }

    public void Dispose()
    {
        // NG: Dispose 時にのみ破棄。スコープ無効化に対応した破棄パスがない
        foreach (var obj in placedObjects)
            if (obj != null) Object.Destroy(obj);
        placedObjects.Clear();
    }
}
```

TileRoot が `DestroyLayerRoot` で破棄されると子 GameObject も破棄されるが、
`placedObjects` に破棄済み参照が残り続け、再生成のたびに蓄積する。

### After

```csharp
public sealed class EnvironmentObjectPlacer : IDisposable
{
    readonly Dictionary<int, List<GameObject>> propsByLayer = new();

    public void PlaceChunkProps(Transform parent, WorldMapLayerViewData layerData, ...)
    {
        if (!propsByLayer.TryGetValue(layerData.LayerId.Value, out var list))
        {
            list = new List<GameObject>();
            propsByLayer[layerData.LayerId.Value] = list;
        }
        // props を生成して list に追加する
    }

    // スコープ無効化と対称のクリーンアップパス
    public void InvalidateLayer(MapLayerId layerId)
    {
        if (!propsByLayer.TryGetValue(layerId.Value, out var list)) return;
        foreach (var obj in list)
            if (obj != null) Object.Destroy(obj);
        list.Clear();
        propsByLayer.Remove(layerId.Value);
    }

    public void Dispose()
    {
        foreach (var list in propsByLayer.Values)
            foreach (var obj in list)
                if (obj != null) Object.Destroy(obj);
        propsByLayer.Clear();
    }
}
```

### 適用基準

- Layer / Floor / Actor / Chunk など生成単位ごとに `InvalidateXxx(id)` を実装する
- 既存の無効化パス（`NavMeshBuildService.InvalidateLayer`・`WorldMapViewDataProvider.InvalidateLayer` 等）と対称に設計する
- 「親 GameObject が破棄されれば C# の参照リストも整理される」という前提で設計しない

---

## 21. Fallback / Placeholder アセット生成ロジックを複数クラスに持たない

UnityEngine.Object の Fallback（色・サイズ・PPU・ピボット・フィルターモード等）を生成するコードは単一の Factory クラスに集約し、複数クラスに同一ロジックを置かない。

特に `pixelsPerUnit`・サイズ・ピボット・フィルターモードは一箇所でズレると視覚的な不具合（スケールのズレ・ぼやけ等）になりやすいため、定数含めて一元管理する。

### Before

```csharp
// VisualConfigLoader
const int SpriteWidth = 32;
const int SpriteHeight = 48;
const float PixelsPerUnit = 16f;
Sprite CreatePlaceholderSprite(Color color) { /* Texture2D 生成ロジック */ }

// ActorSpriteVisualConfig（同一定数・同一ロジックが重複）
const int SpriteWidth = 32;
const int SpriteHeight = 48;
const float PixelsPerUnit = 16f;
Sprite CreateFallbackSprite(Color color) { /* 実質同一コード */ }
```

### After

```csharp
// ActorSpritePlaceholderFactory（一元管理）
public static class ActorSpritePlaceholderFactory
{
    const int SpriteWidth = 32;
    const int SpriteHeight = 48;
    const float PixelsPerUnit = 16f;

    public static Sprite Create(Color color, out Texture2D texture) { ... }
}

// 各クラスは Factory を呼ぶだけ
var sprite = ActorSpritePlaceholderFactory.Create(color, out var texture);
```

### 適用基準

- Fallback Texture / Sprite / Material などのデフォルトビジュアル生成が複数クラスに現れたら Factory に集約する
- 同一定数（サイズ・PPU・ピボット）が複数箇所にコピーされていないか確認する
- 状態を持たない生成ロジックは `static class` にする

---

## レビュー用チェックリスト

### 命名

- [ ] テストダブルの名前は実際の振る舞いと一致しているか（NoOp, Throwing, Stub, Fake, Spy 等）
- [ ] インターフェース名に現在の実装クラス名が入り込んでいないか
- [ ] 「別のクラスがこのインターフェースを実装するとしても、名前は適切か」を確認したか

### 定数・マジックナンバー

- [ ] ゲームバランスに関わる数値がインラインのマジックナンバーになっていないか
- [ ] 同じ数値が複数箇所に散在していないか
- [ ] 定数が適切な名前で意味を伝えているか

### DI・依存

- [ ] コンストラクタで要求する型が LifetimeScope に登録されているか
- [ ] 注入された依存が実際に使われているか
- [ ] 将来用・過去実装の名残の依存が constructor / field に残っていないか
- [ ] インターフェース分割により「必要最小限の依存」になっているか
- [ ] テストダブルのスコープが必要最小限か（使わないメソッドが多すぎないか）
- [ ] テストダブルに `throw new NotSupportedException()` が多い場合、インターフェース分割を検討したか
- [ ] DI constructor（非MonoBehaviour）内で UnityEngine.Object を生成していないか
- [ ] LifetimeScope / Installer が Prefab や View 実体の置き場になっていないか
- [ ] Popup / HUD / View 実体が直接 DI されず、Presenter / Pool / Factory 経由で操作されているか

### インターフェース設計

- [ ] 将来の拡張が予見できる場合、空実装を置くかどうかをユーザーに提案・確認したか
- [ ] 空実装が「手抜き」ではなく「意図的な拡張受け口」として設計されているか

### 確認プロセス

- [ ] テストを書いてから run-tests でグリーンを確認したか
- [ ] Play モード確認はテスト通過後に行ったか
- [ ] 新しいマスタクラスのコンストラクタ代入をテストで検証したか

### マスタ参照と Runtime Instance

- [ ] Runtime Instance / State / Entity が、マスタから O(1) で引ける不変値をコピー保持していないか
- [ ] 表示や判定に必要なマスタ値は、UseCase / Presenter / Policy 側で Repository から解決しているか
- [ ] 例外的に保持する値は、履歴・ログ・セーブ互換・個体固有値・現在値のいずれかであり、明示されているか

### TODO 管理

- [ ] TODO が現在の挙動を仕様と違うものにしていないか
- [ ] 実挙動・性能に影響する TODO に `TODO(milestone:X):` 形式でマイルストーンを明記したか
- [ ] マイルストーン完了時に、そのマイルストーン番号の TODO が残っていないか

### API コントラクト・コンストラクタ・IDisposable

- [ ] property getter の裏で `ToArray()` / `ToList()` / `new List<T>()` / LINQ chain による allocation が発生していないか
- [ ] allocation が必要な取得処理は `CreateSnapshot` / `CopyTo` / `DrainTo` など、コストと寿命が名前で分かる API になっているか
- [ ] 内部バッファを返すメソッドのコントラクトが名前またはコメントで明示されているか
- [ ] `IReadOnlyList<T>` ラッピングだけで内部バッファ参照問題を解決したと思っていないか
- [ ] DI で解決すべき依存を手動 `new` する互換コンストラクタが Runtime コードに含まれていないか
- [ ] `IDisposable.Dispose()` が空の場合、クリーンアップ不要であることをコードで確認したか
- [ ] `Dispose()` が空なら、インターフェース自体を外すか理由をコメントで明記しているか
- [ ] UnityEngine.Object を保持するコレクションで、スコープ単位の InvalidateXxx が Dispose と対称に実装されているか
- [ ] Fallback / Placeholder アセット生成ロジックが複数クラスに重複していないか
- [ ] コンテンツ Prefab の Addressable address が発生元 Master / Spec / Definition から解決されているか

### 設計記録

- [ ] 一般パターンに反する意図的設計の根拠が設計ドキュメントに記録されているか
- [ ] 「意図的な設計」の記録が、そこから派生する別の問題への認識も含んでいるか
---

## Visual Definition 分離の原則

### 経緯

DungeonInn の Milestone 7.5 では、`Actor` と `Actor` の見た目を分離した。
それ以前は `ActorView.prefab` や Actor 表示処理が、Actor 種別ごとの Sprite / AnimationClip / 表示サイズを直接知る形になりやすかった。
この構造では、冒険者・モンスター・職員・ペットなどの種類が増えるたびに View / Prefab / Presenter の分岐が増え、ゲーム上の存在と表示資産の責務が混ざる。

Milestone 7.5 では、Actor のゲーム上の定義、見た目の解決表、実際の表示資産定義を分けた。

- `Actor` / `ActorArchetypeMaster`: ゲーム上の存在、種別、能力、初期装備、行動種別を扱う
- `ActorVisualMaster`: `visualId + skinId` から Visual Definition の Addressable address を解決する
- `ActorVisualDefinitionSO`: Sprite、FPS、loop、方向差分、表示サイズなど Unity 表示資産を扱う
- `ActorSpriteAnimator`: Actor ごとの現在 state、方向、frame、経過時間など再生状態を扱う

この分離により、「Goblin という敵」と「Goblin をどう描画するか」を別々に変更できる。

### 基本方針

Domain / Application / Master は Unity asset を直接参照しない。
Prefab、Sprite、Material、AnimationClip、Animator Controller、Addressables API などをゲーム概念に混ぜない。
ゲーム概念側が表示差分を必要とする場合は、`visualId` や `visualMasterId` のような論理 ID のみを持たせ、実体のロード・参照・差し替えは View / Infrastructure 側に閉じる。

Visual Definition は共有可能な不変データとして扱う。
一方で、現在 frame、再生中の animation state、経過時間、one-shot 完了状態、missing warning の出力済み状態などは View インスタンス側に持つ。
「定義」と「再生状態」を同じオブジェクトに混ぜない。

### 適用する場面

以下のような、ゲーム上の意味と表示資産が一対一に見えやすい箇所ほど、この分離を検討する。

- Actor / Character / Enemy / NPC の見た目
- Item / Equipment の icon や world object 表示
- Projectile / AreaEffect / SkillEffect の prefab
- Facility / Prop / Tile の見た目
- UI View prefab の選択

ただし、Domain に Unity asset を持ち込むための抜け道として `Definition` を作ってはならない。
Visual Definition は View / Infrastructure 側の表示定義であり、Domain の仕様値やゲームルールそのものではない。

### ルール

#### 1. ゲーム概念と表示概念を同じデータに混ぜない

Actor、Item、Facility などのゲーム概念は、Prefab、Sprite、Animation、Material を直接持たない。
必要なら `visualId` のような論理 ID だけを持つ。

```csharp
// OK: ゲームデータは論理 ID だけを持つ
public sealed class ActorArchetypeMaster
{
    public string VisualId { get; }
}

// NG: ゲームデータが Unity asset を直接持つ
public sealed class ActorArchetypeMaster
{
    public Sprite IdleSprite { get; }
    public GameObject ActorPrefab { get; }
}
```

#### 2. 見た目差分は Visual Definition に閉じ込める

Actor 種別ごとに prefab や View クラスを増やすのではなく、共通 View に Visual Definition を差し替える。
これにより、見た目の種類が増えても View の構造や Presenter の分岐を増やさずに済む。

```csharp
// OK: View は共通。見た目は Definition で差し替える
actorView.ApplyVisual(actorVisualDefinition);
```

#### 3. Addressable address をゲーム本体に漏らさない

`ActorArchetypeMaster` などのゲームデータが Addressable address を直接持つと、ゲームデータが Unity の asset 配置に依存する。
`visualId -> address` の解決表を挟み、asset 配置変更や skin 差し替えを局所化する。

```csharp
// OK: archetype は visualId のみを持つ
new ActorArchetypeMaster(id, name, visualId, ...);

// OK: visual master が address 解決を担当する
new ActorVisualMaster(visualId, skinId, "World/ActorVisual/AdventurerNovice");

// NG: archetype が直接 Addressable address を持つ
new ActorArchetypeMaster(id, name, "World/ActorVisual/AdventurerNovice", ...);
```

#### 4. Enum 追加時に明示的に壊れる構造を選ぶ

`Idle / Walk / Attack / Damage` のように固定カテゴリとして扱う値は、過度に汎用的なフラット配列にしない。
Enum 追加時に Factory、Editor 生成、テストが明示的に更新対象になる構造の方が安全な場合がある。

Milestone 7.5 では、`ActorVisualAnimationEntry` が `ActorAnimationKey` を持つフラット配列ではなく、`ActorVisualDefinitionSO` が animation key ごとの配列を持つ形にした。

```csharp
public sealed class ActorVisualDefinitionSO : ScriptableObject
{
    [SerializeField] ActorVisualAnimationEntry[] idleEntries;
    [SerializeField] ActorVisualAnimationEntry[] walkEntries;
    [SerializeField] ActorVisualAnimationEntry[] workEntries;
    [SerializeField] ActorVisualAnimationEntry[] attackEntries;
    [SerializeField] ActorVisualAnimationEntry[] damageEntries;
    [SerializeField] ActorVisualAnimationEntry[] deadEntries;
}

public sealed class ActorVisualAnimationEntry
{
    [SerializeField] ActorAnimationDirection direction;
    [SerializeField] float fps;
    [SerializeField] bool loop;
    [SerializeField] Sprite[] sprites;
}
```

この形にすると、`Idle` の配列に `Attack` の key が混ざる不正状態を構造的に作れない。
新しい `ActorAnimationKey` を追加した場合も、SO フィールド、Factory、EditorSetup、テストの不足が見つかりやすい。

#### 5. Visual Definition は共有し、再生状態は View インスタンスに持つ

Visual Definition は複数 Actor で共有してよい。
ただし、現在 frame、state、elapsed time、one-shot 完了状態などは Actor ごとに異なるため、Definition に入れてはならない。

```csharp
// OK: 共有可能な不変データ
public sealed class ActorVisualDefinition
{
    public bool TryGetClip(ActorAnimationKey key, ActorAnimationDirection direction, out ActorVisualAnimationClip clip);
}

// OK: ActorView / Animator インスタンスごとの再生状態
public sealed class ActorSpriteAnimator
{
    ActorAnimationState currentState;
    float elapsed;
    int clipFrameIndex;
}
```

### DungeonInn Milestone 7.5 Example

Milestone 7.5 の Actor visual 分離は、このパターンの具体例である。

- `ActorArchetypeMaster.VisualId` は「この Actor 種別のデフォルト見た目」を表す論理 ID
- `ActorVisualMaster` は `visualId + skinId` を Addressable address に解決する Master
- `ActorVisualDefinitionSO` は Unity asset と表示パラメータを持つ View 側定義
- `ActorVisualDefinitionFactory` は SO を runtime 用の不変定義に変換し、必須 animation key / direction の欠落を検証する
- `WorldActorPresenter` は `ActorViewData.VisualId` を受け取り、未ロードなら `ActorVisualDefinitionLoader` に要求する
- `ActorView` は Addressable を知らず、ロード済み `ActorVisualDefinition` を適用するだけにする
- `ActorSpriteAnimator` は共有 definition を参照しつつ、現在 state / frame / elapsed を Actor ごとに保持する

### レビュー観点

- [ ] Domain / Application / Master が `Sprite` / `Material` / `GameObject` / `ScriptableObject` / Addressables API を直接参照していない
- [ ] ゲーム概念側が持つ表示情報は `visualId` などの論理 ID に留まっている
- [ ] Addressable address は発生源 Master / Spec / Visual Master / Definition 側に閉じている
- [ ] View / Presenter / Loader の責務が、ID 解決、asset load、View 適用に分離されている
- [ ] Visual Definition に現在 frame / elapsed / dirty flag / warning state などの per-instance 状態が入っていない
- [ ] enum や固定カテゴリを追加した時に、Factory / Editor 生成 / asset validation / test の不足が検出できる
- [ ] null asset や missing sprite の警告には、visualId / animation key / direction / frame など追跡に必要な情報が含まれている

## Editor/OneShot Pattern

- Temporary Unity asset generation or wiring editor code belongs under `Editor/OneShot/`.
- Invoke OneShot methods directly with `uloop execute-dynamic-code`.
- OneShot code may remain in the repository for later reference or repair, and may also be deleted when no longer needed.
- Do not call OneShot methods from validation, `[InitializeOnLoadMethod]`, `AutoSetup`, or any other automatic execution path.

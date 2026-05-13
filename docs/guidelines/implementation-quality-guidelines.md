# 実装品質・レビューガイドライン

このドキュメントは、命名・DI・テスト・定数・TODO 管理など、コードを書く際・レビューする際の判断基準をまとめる。
特定プロジェクト専用のルールではなく、別プロジェクトでも同じ判断を再現するための指針とする。

「動く」ことと「正しい設計である」ことは別である。
動作の確認前に、型・名前・責務・依存が設計の意図と一致しているかを見る。
レビューは「バグを見つける場」ではなく「設計の意図を明文化し、コードと一致させる場」である。

---

## 1. 型・クラス名は実際の振る舞いと一致させる

名前は読者との契約である。
名前から期待される振る舞いと実際の振る舞いが食い違うと、コードを読んだ人が誤解し、バグや誤用の温床になる。
テストダブルは特に「何をする・しないクラスか」が名前に表れていなければならない。

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
- [ ] インターフェース分割により「必要最小限の依存」になっているか
- [ ] テストダブルのスコープが必要最小限か（使わないメソッドが多すぎないか）
- [ ] テストダブルに `throw new NotSupportedException()` が多い場合、インターフェース分割を検討したか

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

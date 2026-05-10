# Review Policy Guidelines

このドキュメントは、コードレビューを通じて蓄積された設計・実装上の判断基準をまとめる。
特定プロジェクト専用のルールではなく、別プロジェクトでも同じ判断を再現するための指針とする。

DungeonInn での具体的なレビュー指摘を事例として記載する。

---

## 基本方針

「動く」ことと「正しい設計である」ことは別である。
動作の確認前に、型・名前・責務・依存が設計の意図と一致しているかを見る。
レビューは「バグを見つける場」ではなく「設計の意図を明文化し、コードと一致させる場」である。

---

## 1. イベントは「起きた事実」を運ぶ。View 用の加工済み値を持たせない

ドメインイベントはドメインで何が起きたかの記録である。
表示名・フォーマット済みテキスト・UI 向け集計値など、View 側の関心事を埋め込まない。
イベントを受け取った View 層が必要な値を自分で導出・解決する。

### Before

```csharp
public sealed class ItemDropped : IGameEvent
{
    public Guid ActorId { get; }
    public Guid ItemInstanceId { get; }
    public int ItemId { get; }
    public string ItemName { get; }   // ← 表示用文字列をイベントに持たせている
    public LayerPosition Position { get; }
}
```

`ItemName` はマスタデータから引いた表示用の値であり、ドメインイベントの事実とは関係がない。
イベント生成時にマスタリポジトリへの依存が発生し、UseCase の責務が増える。

### After

```csharp
public sealed class ItemDropped : IGameEvent
{
    public Guid ActorId { get; }
    public ItemInstance ItemInstance { get; }  // ← ドロップされた事実（誰が、何を）をそのまま渡す
}
```

`ItemInstance` はドロップ時点で既に生成済みのドメインオブジェクトである。
アイテム名が必要な View 側が `IMasterRepository` を通じて解決すればよい。

### 適用基準

イベントに含めてよい値：

- ドメインオブジェクト本体（Entity、値型）
- 変化の前後の状態（`PreviousLevel`, `NewLevel`）
- 「誰が何をしたか」を表す ID と量

イベントに含めてはいけない値：

- マスタデータから引いた表示名、説明文
- View のレイアウトやフォーマットに依存する値
- 集計・変換済みの表示用スコア

---

## 2. 概念が重複する型を作らない。既存の値型を再利用する

同じ概念（アイテムID ＋ 個数）が複数の型に分散すると、フィールド名のゆれ（`Count` / `Amount`）や変換ロジックが生まれる。
既存の値型で表現できるものは新しいフィールドを作らず、その型を組み合わせる。

### Before

```csharp
public sealed class ItemInstance
{
    public Guid InstanceId { get; }
    public int ItemId { get; }     // ← ItemStack.ItemId と同じ概念
    public int Amount { get; }     // ← ItemStack.Count と同じ概念（名前もゆれている）
    public LayerPosition Position { get; }
}
```

`ItemId + Amount` は既に `ItemStack { ItemId, Count }` として定義されている。
同じ概念が 2 箇所に分散し、どちらが正典か不明瞭になる。

### After

```csharp
public sealed class ItemInstance
{
    public Guid InstanceId { get; }
    public ItemStack Stack { get; }     // ← 既存の値型をそのまま使う
    public LayerPosition Position { get; }
}
```

`ItemInstance` は「ワールドに置かれた ItemStack のエンティティ」として意味が明確になる。
`Count` か `Amount` かの命名ゆれも解消される。

### 適用基準

- 新しいフィールドを追加する前に「同じ概念を表す値型・構造体が既にないか」を確認する
- 「どちらが正典か」が曖昧な場合は既存の型に一本化する
- 値型（struct）は Identity を持たない「量・仕様・状態のスナップショット」として再利用しやすい

---

## 3. 型・クラス名は実際の振る舞いと一致させる（特にテストダブル）

名前は読者との契約である。
名前から期待される振る舞いと実際の振る舞いが食い違うと、コードを読んだ人が誤解し、バグや誤用の温床になる。
テストダブルは特に「何をする・しないクラスか」が名前に表れていなければならない。

### Before

```csharp
sealed class NoOpMasterRepository : IMasterRepository
{
    public ItemMaster GetItemMaster(int itemId) => throw new NotSupportedException();
    // ← 何もしない（No Operation）ではなく、例外を投げる
}
```

`NoOp` は「何もせずに戻る」という意味合いを持つ。
しかし実際には `NotSupportedException` を投げる。
このクラスを使うテストで「なぜ例外が出たか」のデバッグに余計な時間がかかる。

### After

```csharp
sealed class ThrowingMasterRepository : IMasterRepository
{
    public ItemMaster GetItemMaster(int itemId) => throw new NotSupportedException();
    // ← 呼ばれたら必ず例外を投げる、という事実が名前に表れている
}
```

### 適用基準

| 振る舞い | 適切な名前の例 |
|---|---|
| 呼ばれたら例外を投げる | `ThrowingXxx`, `AlwaysFailingXxx` |
| 固定値を返す | `StubXxx`, `FakeXxx`, `FixedXxx` |
| 呼び出しを記録する | `SpyXxx`, `CapturingXxx`, `CollectingXxx` |
| 何もせず正常終了する | `NoOpXxx`, `SilentXxx` |
| 実際の実装に近い軽量版 | `FakeXxx` |

プロダクションコードでも同様に、「例外を投げる可能性があるか」「状態を変える副作用があるか」が名前から読み取れることが望ましい。

---

## 4. 調整可能な値はマジックナンバーにしない。定数に名前をつける

ゲームバランス値・リトライ上限・タイムアウト秒数など、後から調整が想定される数値を式にインラインで書かない。
名前付き定数として一箇所に集め、変更箇所を明確にする。

### Before

```csharp
var xpReward = Math.Max(1, defeated.Experience / 10);
//                      ↑              ↑ 何の意味か名前がない
//                      最低値が 1 の根拠が不明
```

数値の意味（分母が 10 なのはなぜか、最低値が 1 なのはバランス上の決定か）がコードから読み取れない。
バランス調整のたびにコード内を検索しなければならない。

### After

```csharp
// GameConstants.Combat.cs
public const int KillExperienceRewardNumerator = 1;
public const int KillExperienceRewardDenominator = 10;
public const int KillExperienceRewardMinimum = 1;

// UseCase
var xpReward = Math.Max(
    GameConstants.KillExperienceRewardMinimum,
    defeated.Experience * GameConstants.KillExperienceRewardNumerator
        / GameConstants.KillExperienceRewardDenominator);
```

### 適用基準

定数に抽出すべき値：

- ゲームバランスに関わる数値（報酬倍率、確率、しきい値）
- 試行回数・タイムアウト・ページサイズなど運用調整が想定されるもの
- 同じ数値が複数箇所に出てきているもの

定数に抽出しなくてよい値：

- ドメインの不変条件として変わりえない値（`if (count < 1)` の `1` など）
- 意味が自明な `0` や `true`

定数ファイルの分割は「関心ごとに」行う。
`GameConstants.Combat`, `GameConstants.Inn`, `GameConstants.Map` のように文脈で分け、巨大な 1 ファイルにしない。

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
    // ↑ IItemMasterRepository は DI コンテナに登録されていない
}

// LifetimeScope
builder.Register<HardcodedMasterRepository>(Lifetime.Scoped).As<IMasterRepository>();
// IMasterRepository : IItemMasterRepository だが、IItemMasterRepository は別途登録していない
```

`IMasterRepository` が `IItemMasterRepository` を継承していても、コンテナは `IItemMasterRepository` として解決できない。

### After

```csharp
public sealed class DropItemUseCase
{
    [Inject]
    public DropItemUseCase(IMasterRepository masterRepository, ...)
    // ↑ 実際に登録されている型を使う
}
```

### 適用基準

- 新しい UseCase を作るとき、コンストラクタで要求する型が LifetimeScope に登録されているか確認する
- インターフェースを分割したい（`IItemMasterRepository` だけを知ればよい）場合は、LifetimeScope に `.As<IItemMasterRepository>()` を追加するか、設計を見直す
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
    // ... 10以上のメソッド
}
```

`DropItemUseCase` が実際に呼ぶのは `GetItemMaster` だけ。
それ以外のメソッドを実装する必要がない（→ 後に `DropItemUseCase` が `IMasterRepository` への依存をなくしたとき、このスタブも不要になった）。

### After

```csharp
// DropItemUseCase はマスタリポジトリ不要になったため、スタブ自体が消えた
// 必要なのは IGameRandom と IGameEventBus のみ
var useCase = new DropItemUseCase(new FixedGameRandom(0), new CollectingEventBus());
```

テストに必要なスタブが最小限になることは、そのユースケースの依存が正しく絞られている証拠でもある。

### 適用基準

- テストで使わないメソッドが多いスタブは「このクラスの依存が広すぎないか」を疑う
- テストダブルに `throw new NotSupportedException()` が多い場合、インターフェースを分割できないか検討する
- テストが「DI コンテナの登録型」に縛られるなら、UseCase の依存型設計を見直す機会でもある

---

## 7. 実装後の確認順序：テスト → run-tests → Play モード

動作確認は「広い網」から「狭い観察」へ順番に行う。
Playモード確認だけでは「どのケースが壊れているか」の特定が難しく、原因の調査に時間がかかる。

### 順序

```
1. 新機能をカバーするテストを書く（EditMode テスト）
2. uloop run-tests で全テストパスを確認
3. uloop control-play-mode でPlayモード起動、ログで実際の挙動を確認
```

### Before（不十分な確認）

Playモードだけで確認 → `LevelTable.Id = 0` 問題が後回しになる。
どのユースケースが壊れているか絞り込めない。

### After（正しい順序）

```
テストを書く
  ↓ GetLevel / GetExperienceForLevel / RecalculateLevel を単体でカバー
run-tests
  ↓ LevelTable.Id 代入漏れが即座に検出される
Playモード確認
  ↓ テストで保証された状態からの統合確認
```

### 適用基準

- 「動くかどうか」より「何が保証されているか」を先に確保する
- Playモード確認は「統合シナリオとして成立するか」を確認する場であり、ロジック正しさの確認はテストで行う
- テストが書けない設計（依存が多すぎる、構築が複雑すぎる）は設計を見直すシグナルである

---

## 8. キャッシュ値の更新漏れはテストで検出する水準を目指す

キャッシュを持つ設計（Level は Experience から導出可能だがキャッシュする、等）では、
キャッシュと元データの不整合がバグになる。
「構築後にキャッシュが正しいこと」を必ずテストで保証する。

### Before（テストなし → 実行時に発覚）

```csharp
public LevelTable(int id, int[] cumulativeXp)
{
    // Id = id; を書き忘れ → Id が 0 のまま
    this.cumulativeXp = cumulativeXp;
}
```

`ToDictionary(x => x.Id)` で全エントリが `Id = 0` になりキー重複例外が発生。
Playモード起動まで発覚しなかった。

### After（コンストラクタの正しさをテストで保証）

```csharp
[Test]
public void LevelTableIdIsCorrectlyAssigned()
{
    var table = new LevelTable(2, new int[101]);
    Assert.That(table.Id, Is.EqualTo(2));
}
```

コンストラクタの代入漏れは最も発見しやすいバグの1つ。
初期化後のプロパティ値を検証するテストが1本あれば防げる。

### 適用基準

- 新しいマスタデータクラスやエンティティを作ったら、コンストラクタのプロパティ代入を検証するテストを最初に書く
- キャッシュ値は「変更を引き起こした操作後に更新されること」もテストする（`RecalculateLevel` 後に `Level` が変わるかどうか、等）
- Dictionary のキーになるプロパティ（`Id` 等）の代入漏れは特に検出困難なため、優先的にテストする

---

## 9. インターフェース名は「現在の実装者」ではなく「能力・役割」を表す

インターフェースに実装クラスの名前を入れると、「現在それを実装しているのが誰か」という実装の都合が型に固定される。
インターフェースが表すべきものは「何ができるか（能力）」であり、「誰が実装しているか（現状）」ではない。

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
将来の実装者が増えても、インターフェース名も呼び出し側コードも変える必要がない。

### 適用基準

- インターフェース名に現在の実装クラス名（`Monster`, `Player`, `Admin` 等）を含めない
- 「〜できる」「〜を持つ」「〜として振る舞う」という能力・役割を名前にする
- 命名の目安：「このインターフェースを全く別のクラスが実装するとしたら、名前は依然として適切か」を確認する

---

## 10. 将来の拡張が予見できる場合、現時点でインターフェースを定義し空実装を置く

拡張の可能性が予見できる場合、将来の実装者になりうる型に対して現時点でインターフェースを実装させ、
今は空実装にしておく。こうすることで将来の追加が「既存コードの変更」ではなく「既存コードの充填」になる。

ただし、これは「現状では不要な拡張の受け口を作る」という方針判断を含む。
**実装者（Claude Code / Codex）が独断でこの判断を行わず、拡張の可能性が見えた場合はユーザーに提案・確認してから実装する。**

### Before（拡張を考慮しない実装）

```csharp
// MonsterBehavior だけが実装。冒険者ドロップが必要になったとき、
// 呼び出し側の is MonsterBehavior チェックを書き換える必要がある
if (actor.Behavior is MonsterBehavior monster)
{
    foreach (var entry in monster.DropTable) { ... }
}
```

将来の変更時に呼び出し側を書き換えるコストが発生する。

### After（空実装で拡張の受け口を確保）

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
- **実装者（Claude Code / Codex）の判断ルール：** 拡張の可能性が見えた場合は「〇〇もこのインターフェースを実装しておくことを提案します」とユーザーに確認し、GOサインが出てから実装する

---

## 11. データ設計の疑問は「概念レベル」から問う。コードの前に「この型が本質的に持つべき属性か」を確認する

コードの正しさを問う前に、データモデルの概念的な正しさを問う。
「この属性は、この型が本質的に持つべきものか、それとも文脈依存の属性か」という問いが、
型の設計ミスを早期に発見する。

### 問いの立て方

```
この属性は...
  ├─ この型の本質的な属性か？ → その型に持たせる
  └─ 特定の文脈でのみ意味を持つ属性か？ → 文脈を持つ別の型に持たせる
```

### 例：アイテムと座標

```
「Item は座標を持つか？」
  → Item それ自体は座標を持たない（インベントリにある Item も、ショップにある Item も同じ Item）
  → 座標は「ワールドにドロップされている」という文脈に固有の属性

結論：座標は ItemInstance（ワールドに存在するアイテムのエンティティ）が持つ
```

### 例：ItemStack と ItemInstance の概念整理

```
「ItemInstance に ItemId と Amount を持たせているが、ItemStack と概念が重複していないか？」
  → ItemStack =「アイテムID ＋ 個数」という純粋な値（文脈なし）
  → ItemInstance = 「その ItemStack がワールドに存在している状態」（文脈あり）

結論：ItemInstance は ItemStack を内包する構造にする
```

### 適用基準

- 新しいフィールドを追加する前に「この属性は、この型の本質か、それとも文脈か」を自問する
- 「文脈依存の属性を本質に入れた型」は、文脈が変わるたびにフィールドが増え肥大化する
- 逆に「本質的な属性を別の型に切り出した型」は、再利用性が高まり命名も明確になる
- 設計に迷いがある場合は「この型を、全く別の文脈（別の画面、別のシステム）でも使うとしたら、このフィールドは自然か」で確認する

---

## レビュー用チェックリスト

### イベント設計

- [ ] イベントに View 用の表示文字列・加工済み値が含まれていないか
- [ ] イベントにドメインオブジェクトをそのまま渡せているか
- [ ] イベント生成のためにマスタリポジトリ・View サービスへの依存が増えていないか

### 型設計

- [ ] 既存の値型で表現できる概念を新しいフィールドとして重複定義していないか
- [ ] `Count` / `Amount` / `Quantity` のような命名ゆれが発生していないか
- [ ] 概念的に同じものが 2 つの型に分かれていないか

### 命名

- [ ] テストダブルの名前は実際の振る舞いと一致しているか（NoOp, Throwing, Stub, Fake, Spy 等）
- [ ] クラス名から動作を誤解する可能性がないか

### 定数・マジックナンバー

- [ ] ゲームバランスに関わる数値がインラインのマジックナンバーになっていないか
- [ ] 定数が適切な名前で意味を伝えているか
- [ ] 同じ数値が複数箇所に散在していないか

### DI・依存

- [ ] コンストラクタで要求する型が LifetimeScope に登録されているか
- [ ] インターフェース分割により「必要最小限の依存」になっているか
- [ ] テストダブルのスコープが必要最小限か（使わないメソッドが多すぎないか）

### インターフェース設計

- [ ] インターフェース名に現在の実装クラス名が入り込んでいないか
- [ ] 「別のクラスがこのインターフェースを実装するとしても、名前は適切か」を確認したか
- [ ] 将来の拡張が予見できる場合、空実装を置くかどうかをユーザーに提案・確認したか
- [ ] 空実装が「手抜き」ではなく「意図的な拡張受け口」として設計されているか

### データモデル設計

- [ ] 新しいフィールドを追加する前に「本質的な属性か、文脈依存の属性か」を自問したか
- [ ] 文脈依存の属性が誤って型の本質的フィールドになっていないか
- [ ] 既存の型で表現できる概念を新たなフィールドとして重複定義していないか

### 確認プロセス

- [ ] テストを書いてから run-tests でグリーンを確認したか
- [ ] Play モード確認はテスト通過後に行ったか
- [ ] 新しいマスタクラスのコンストラクタ代入をテストで検証したか

### マスタ参照と Runtime Instance

- [ ] Runtime Instance / State / Entity が、マスタから O(1) で引ける不変値をコピー保持していないか
- [ ] `XxxMasterId` があるのに、同じマスタ由来の `Name` / `DisplayName` / `ReapplyPolicy` / 固定 Duration / 固定 Amount / Category を重複保持していないか
- [ ] 表示や判定に必要なマスタ値は、UseCase / Presenter / Policy 側で Repository から解決しているか
- [ ] 例外的に保持する値は、履歴・ログ・セーブ互換・個体固有値・現在値のいずれかであり、フィールド名やコメントで意図が明示されているか

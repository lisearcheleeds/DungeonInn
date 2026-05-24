# C# コーディング規約

## ハードゲート

この節は即時停止・修正が必要な禁止事項を列挙する。
この節に載っていない実装が自動的に許可されるわけではなく、本文の設計方針・判断基準に反する場合もレビュー指摘または作業停止対象とする。

- [ ] 新規・変更コードに明示的な `private` 修飾子を追加していない（`private set` を除く）
- [ ] `public` / `protected` / `internal` が必要なメンバーにアクセス修飾子を書いている
- [ ] namespace はブロックスコープで書き、ファイルスコープ namespace を追加していない
- [ ] 波括弧は Allman スタイルで書いている
- [ ] 条件分岐・ループは原則ブロック形式で書いている
- [ ] フィールド名に `_` prefix を付けていない
- [ ] メソッド本体を式形式にせず、ブロック形式で書いている
- [ ] 比較式は数直線の向きに合わせ、原則 `<` を使っている
- [ ] DI 注入点の constructor / `Construct` method に `[Inject]` を明示している
- [ ] コメントは WHAT ではなく WHY を説明している
- [ ] View / Presenter が画面表示へ直接適用する class / struct は `ViewData` suffix に統一している

## 完了前チェックリスト

このチェックリストは本文の設計方針を省略するためのものではない。
実装・レビュー時は本文を確認したうえで、最後に確認漏れを防ぐ目的で使用する。

- [ ] アクセス修飾子、namespace、波括弧、空行が本文ルールに沿っている
- [ ] フィールド・ローカル変数・引数・定数の命名が本文ルールに沿っている
- [ ] `var` / `new()` / using 順序が本文ルールに沿っている
- [ ] null チェック、比較式、定数位置が本文ルールに沿っている
- [ ] `UniTask` の同期ラップや不要な `async` が本文ルールに沿っている
- [ ] オーバーロード、デフォルト引数、引数名が本文の判断基準に沿っている
- [ ] 不要 using と WHAT コメントを残していない
- [ ] View / Presenter が画面表示へ直接適用する class / struct に `Dto` / 汎用 `Data` suffix を使っていない

---

## コーディング規約スキャン

Coding Rules のレビューは設計レビューと並行して行うと見落としが生じる。
以下の grep スキャンを**設計レビューとは独立したフェーズとして先行させる**こと。

コーディング規約違反の多くは実動作に影響しないため、設計問題を探す認知負荷の高いフェーズでは自然言語の読み取りでは検知しにくい。スキャンで候補を機械的に抽出してから人が判定する手順を取ること。

スキャン対象: `Client/Assets/DungeonInn/Runtime/Scripts/` 配下の `.cs` ファイル

```bash
# Rule 2-2: ブロック形式なし（最頻出違反）
# if/else/for/foreach/while の後に { がなく同一行にステートメントが続く
grep -rn "if\s*(.*)\s[^{]" --include="*.cs" Client/Assets/DungeonInn/Runtime/Scripts/ | grep -v "//"

# Rule 1-1: 明示的 private 修飾子
grep -rn "\bprivate\b" --include="*.cs" Client/Assets/DungeonInn/Runtime/Scripts/ | grep -v "private set"

# Rule 3-1: _ prefix フィールド
grep -rn "readonly\s_\|static\s_\|^\s*_[a-z]" --include="*.cs" Client/Assets/DungeonInn/Runtime/Scripts/

# Rule 5-1: ファイルスコープ namespace
grep -rn "^namespace.*;" --include="*.cs" Client/Assets/DungeonInn/Runtime/Scripts/

# Rule 8-2: 式形式メソッド（プロパティ以外の => ）
grep -rn ")\s*=>" --include="*.cs" Client/Assets/DungeonInn/Runtime/Scripts/ | grep -v "=>\s*{"

# Rule 11-1: > 比較演算子
grep -rn "[^=!<>] > [^>=]" --include="*.cs" Client/Assets/DungeonInn/Runtime/Scripts/ | grep -v "//"

# Rule 15-4: [Inject] 欠損確認
# public コンストラクタ（クラス名と同名）の前行に [Inject] があるか目視確認する
grep -rn "public [A-Z][a-zA-Z]*\s*(" --include="*.cs" Client/Assets/DungeonInn/Runtime/Scripts/
```

**スキャン後の判定手順:**

1. 各パターンの出力を確認し、コメント行・文字列リテラル・誤検知を除外する
2. 残った候補を本文の該当ルールに照らして違反か判定する
3. 違反箇所をレビュー項目として記録する（通常の5項目フォーマット不要。ファイル名・行番号・違反ルール番号の一覧で足りる）

---

## 1. アクセス修飾子

### 1-1. `private` は書かない

フィールド・メソッド・ネストクラス等、デフォルトが `private` になるメンバーには `private` キーワードを付けない。

```csharp
// NG
private readonly float restDuration;
private float elapsed;
private void Update() { }

// OK
readonly float restDuration;
float elapsed;
void Update() { }
```

### 1-2. `public` / `protected` / `internal` は必ず書く

```csharp
// OK
public int Value { get; }
protected override void Configure(IContainerBuilder builder) { }
internal class Helper { }
```

### 1-3. `private set` はそのまま書く

プロパティのセッターに限り `private set` は使用可能。

```csharp
public bool IsOccupied { get; private set; }  // OK
```

### 1-4. `const` フィールドには `private` を付けない

```csharp
// NG
private const int ParcelSize = 5;

// OK
const int ParcelSize = 5;
```

---

## 2. 波括弧（Allman スタイル）

### 2-1. 開き波括弧は必ず新しい行に

```csharp
// NG（K&R スタイル）
public void Foo() {
    ...
}

// OK（Allman スタイル）
public void Foo()
{
    ...
}
```

### 2-2. if / else / for / foreach / while は原則すべてブロック形式

```csharp
// NG
if (condition)
    DoSomething();

// OK
if (condition)
{
    DoSomething();
}
```

メソッド途中の条件分岐、複数ステートメントを含む場合、else を伴う場合は必ずブロック形式にする。

---

## 3. フィールド・変数の命名

### 3-1. フィールド: camelCase、アンダースコアプレフィックスなし

```csharp
// NG
private float _elapsed;
readonly IAssetManager _assetManager;

// OK
float elapsed;
readonly IAssetManager assetManager;
```

### 3-2. パラメータ・ローカル変数: camelCase

```csharp
// OK
void Foo(int maxHp, string assetAddress)
{
    var cachedValue = Load();
}
```

### 3-3. 定数・static readonly: PascalCase

```csharp
// OK
const int ParcelSize = 5;
static readonly ModuleSceneId[] RequireSceneModuleIds = { ... };
```

### 3-4. foreach の一時変数: 1文字名を使わない

コレクション名から自然に導かれる単数形の名前を使う。
例外として `i`, `t`, `x`, `y`, `z`, `w` は使用可能。

```csharp
// NG
foreach (var a in actors) { }
foreach (var r in rooms) { }
foreach (var e in events) { }

// OK
foreach (var actor in actors) { }
foreach (var room in rooms) { }
foreach (var gameEvent in events) { }

// OK（例外）
for (var i = 0; i < count; i++) { }
var ratio = Mathf.Lerp(0f, 1f, t);
```

### 3-5. 引数名はメソッドが要求する抽象度に合わせる

public / internal メソッドの引数名は、呼び出し元の現在用途ではなく、そのメソッド自身が要求する契約・抽象度に合わせる。

型が汎用的な基底型・共通型・インターフェースであり、メソッド内で特定サブタイプや特定ロールの契約を要求しない場合、引数名も汎用名にする。

```csharp
// NG: 型は汎用なのに、呼び出し元都合の具体ロール名を付けている
public int CalculateScore(Customer premiumCustomer)
{
    return premiumCustomer.Point;
}

// OK: メソッドが要求している抽象度と引数名が一致している
public int CalculateScore(Customer customer)
{
    return customer.Point;
}
```

特定ロール名を使ってよいのは、引数型・メソッド名・ガード条件・内部処理のいずれかで、そのロール固有の契約を明示している場合に限る。

```csharp
// OK: メソッド責務が特定ロール専用であることを名前で明示している
public int CalculatePremiumCustomerScore(Customer premiumCustomer)
{
    if (!premiumCustomer.IsPremium)
    {
        throw new InvalidOperationException("Premium customer is required.");
    }

    return premiumCustomer.Point * 2;
}
```

「今はその呼び出し元からしか呼ばれない」という理由だけで、境界メソッドの引数名に具体ロールを漏らさない。

---

## 4. ローカル変数の型

### 4-1. 右辺から型が明確な場合は `var` を使う

```csharp
// OK
var room = new Room(Guid.NewGuid(), origin, ParcelSize, ParcelSize);
var scope = assetManager.CreateScope();
var handle = await scope.LoadAsync<WorldConfigSO>("Config/WorldConfig");
```

---

## 5. 名前空間

### 5-1. ブロックスコープ（ファイルスコープ禁止）

```csharp
// NG（ファイルスコープ）
namespace DungeonInn.Application.UseCase;

public class Foo { }

// OK（ブロックスコープ）
namespace DungeonInn.Application.UseCase
{
    public class Foo { }
}
```

---

## 6. 空行

### 6-1. フィールド同士の間: 空行なし

```csharp
// NG
readonly IAssetManager assetManager;

InnConfigData cached;

// OK
readonly IAssetManager assetManager;
InnConfigData cached;
```

### 6-2. メソッド・プロパティ間: 1 行空ける

```csharp
public void Foo()
{
    ...
}

public void Bar()
{
    ...
}
```

### 6-3. メソッド内の連続する空行は最大 1 行

```csharp
// NG（2行以上空いている）
var a = GetA();


var b = GetB();

// OK
var a = GetA();

var b = GetB();
```

### 6-4. 単一行コメントの直前に空行を入れる

```csharp
// OK
var result = Compute();

// 結果を保存
Save(result);
```

---

## 7. 行の長さと折り返し

### 7-1. 自動的な行折り返しはしない

### 7-2. 引数・パラメータが長い場合: 1つずつ改行（CHOP_IF_LONG）

```csharp
// NG（途中で折り返す）
var floor = new DungeonFloor(floorIndex, yMin, yMax,
    stairsUp, stairsDown, walkableMap);

// OK（すべての引数を1つずつ改行）
var floor = new DungeonFloor(
    floorIndex,
    yMin,
    yMax,
    stairsUp,
    stairsDown,
    walkableMap);
```

---

## 8. プロパティとメソッドの本体

### 8-1. 単純な読み取り専用プロパティ: 式形式（`=>`）を使う

```csharp
// OK
public bool IsAlive => Hp > 0;
public IReadOnlyList<Bed> Beds => beds;
public float Density => (float)beds.Count / ((Width - 2) * (Depth - 2));
```

### 8-2. メソッドは単一 return であってもブロック形式を使う

```csharp
// NG（式形式メソッド）
public Room GetRoomContaining(GridPosition worldPos) =>
    rooms.FirstOrDefault(r => r.Contains(worldPos));

// OK（ブロック形式）
public Room GetRoomContaining(GridPosition worldPos)
{
    return rooms.FirstOrDefault(r => r.Contains(worldPos));
}
```

---

## 9. オブジェクト生成

### 9-1. 型が左辺から推論できる場合は `new()` を使う

```csharp
// OK
readonly List<Room> rooms = new();
readonly Dictionary<string, (IAssetScope scope, GameObject prefab)> prefabCache = new();

// 型が推論できない場合は型を明示
var rooms = new List<Room>();
```

---

## 10. using ディレクティブ

### 10-1. グループ順序: System → Unity → LighthouseExtends → VContainer → プロジェクト内

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LighthouseExtends.Addressable;
using VContainer;
using DungeonInn.Domain.Inn;
```

### 10-2. 不要な using は削除する

同一 namespace を `using` する self namespace using も不要 using として削除する。
例えば `namespace DungeonInn.Application.GameLoop` のファイル内で `using DungeonInn.Application.GameLoop;` を書かない。
これは compile には影響しないが、依存している外部 namespace と自分自身の境界を読みにくくする。

---

## 11. 比較演算子

### 11-1. `<` を使い `>` は使わない（数直線の向きに合わせる）

小さい値が左、大きい値が右に来るよう `<` に統一する。

```csharp
// NG
if (max > value) { }
if (5 > count) { }

// OK
if (value < max) { }
if (count < 5) { }
```

範囲チェックも左から右へ数値が大きくなる順に書く。

```csharp
// NG
if (index >= 0 && Floors.Count > index) { }

// OK
if (0 <= index && index < Floors.Count) { }
```

### 11-2. 定数・リテラルは右側に置く

`<` 統一ルールと矛盾する場合は `<` 統一を優先する。

```csharp
// OK（定数が右）
if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
if (elapsed >= restDuration) { }

// NG（定数が左: < を使うために定数を左に置かない）
if (0 > count) throw new ArgumentOutOfRangeException(nameof(count));
```

---

## 12. null チェック

### 12-1. コンストラクタ引数の null チェック: `??` 演算子を使う

```csharp
// OK
this.rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
```

### 12-2. パターンマッチング

```csharp
// OK
if (obj is GridPosition other && Equals(other)) { ... }
```

---

## 13. UniTask

### 13-1. 同期処理を UniTask でラップする場合

```csharp
// OK
return UniTask.FromResult(value);
return UniTask.CompletedTask;
```

### 13-2. async が不要な場合は async を付けない

```csharp
// NG（不要な async/await）
public async UniTask<Room> ExecuteAsync(...)
{
    var room = land.PurchaseParcel(origin, cost);
    return await UniTask.FromResult(room);
}

// OK
public UniTask<Room> ExecuteAsync(...)
{
    var room = land.PurchaseParcel(origin, cost);
    return UniTask.FromResult(room);
}
```

---

## 14. メソッドのオーバーロードとデフォルト引数

### 14-1. デフォルト引数よりオーバーロードを推奨

**推奨（絶対ではない）。** デフォルト引数は呼び出し側のバイナリ互換性を損なう・省略意図が不明瞭になる場合があるため、オーバーロードで表現することを優先する。

```csharp
// 非推奨（デフォルト引数）
public DungeonMap Generate(int sizeX, int sizeZ, int seed = 0, int minStairs = 1) { }

// 推奨（オーバーロード）
public DungeonMap Generate(int sizeX, int sizeZ)
{
    return Generate(sizeX, sizeZ, seed: 0, minStairs: 1);
}

public DungeonMap Generate(int sizeX, int sizeZ, int seed, int minStairs) { }
```

デフォルト引数が明らかに適切な場合（省略が自明・オーバーロードが冗長になる場合）は使用してよい。

---

## 15. その他

### 15-1. `var` ループ変数

```csharp
// OK
for (var x = 0; x < width; x++) { }
foreach (var room in rooms) { }
```

### 15-2. 文字列補完

```csharp
// OK
Debug.LogError($"[TextTable] Failed to load: '{filePath}'\n{e}");
```

### 15-3. コメントは WHY のみ

WHAT（コードを読めばわかること）を説明するコメントは書かない。
なぜその実装にしたか、非自明な制約・回避策がある場合のみコメントを書く。

```csharp
// NG
// ベッドを追加する
room.PlaceBed(bed);

// OK
// prefab はダイアログが生きている間ロードされ続ける必要があるため
// アドレスごとにスコープを保持し、ProductAssetLoader 破棄時にまとめて解放する
readonly Dictionary<string, (IAssetScope scope, GameObject prefab)> prefabCache = new();
```

### 15-4. DI 注入点は `[Inject]` を明示する

VContainer で生成・注入されるクラスは、注入に使う constructor または `Construct` method に `[Inject]` を付ける。

```csharp
// OK: 通常の C# class
public sealed class WorldCameraController
{
    [Inject]
    public WorldCameraController(ISceneCameraManager sceneCameraManager)
    {
    }
}

// OK: MonoBehaviour / Unity component
public sealed class WorldGameLoopEntryPoint : MonoBehaviour
{
    [Inject]
    public void Construct(IGameLoopUseCase gameLoopUseCase)
    {
    }
}
```

理由:
- VContainer は public constructor が 1 つなら `[Inject]` なしでも解決できるが、依存解決の入口が読み取りにくくなる。
- constructor が増えた場合の解決先の揺れを防ぐ。
- このプロジェクトでは、DI で解決される依存は明示性を優先する。

---

## 16. Prefix / Suffix の意味を揃える

### 16-1. View に適用する表示用データは `ViewData` suffix に統一する

View / Presenter / UI Component が画面表示へ直接適用する class / struct は `XxxViewData` と命名する。

```csharp
// OK
public readonly struct ActorStatusViewData { }
public readonly struct ActorDetailViewData { }
public readonly struct ActorEffectIconViewData { }

// NG
public readonly struct ActorDetailDto { }
public readonly struct ActorEffectIconData { }
```

判断基準:
- `ViewData`: View / Presenter が表示内容として直接受け取るデータ。
- `Dto`: 外部通信、永続化、レイヤー間転送など、用途が View 表示に限定されないデータ。
- `Data`: Unity / Lighthouse などのフレームワーク契約、画面遷移データ、または既存の抽象名として `ViewData` では意味が狭すぎるデータ。

Application 層の業務結果 DTO（UseCase / Query が返す型）は `Summary` / `Status` suffix を使ってよい。
これらは Presenter が内部で変換して表示に用いるものであり、画面表示へ「直接適用」する型ではないため `ViewData` を強制しない。

例:
- `InnGuestSummary` — GetInnGuestListUseCase が返す Application 層 DTO（Summary 可）
- `InnEconomyStatus` — GetInnEconomyStatusUseCase が返す Application 層 DTO（Status 可）
- `ActorStatusViewData` — Presenter が View に直接渡す型（ViewData 必須）

判断基準: Presenter が何も変換せず View.SetXxx() に渡すなら `ViewData`。Presenter が加工してから渡すなら `Summary` / `Status` を許容する。

ハードゲート:
- View / Presenter が画面表示へ直接適用する class / struct に `Dto` / 汎用 `Data` suffix を使わず、`ViewData` に統一していること。

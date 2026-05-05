# Actor AI Design

このドキュメントは、Actor のAI設計方針をまとめる。
Adventurer、Monster、Pet、GuildStaff は判断内容こそ異なるが、同じAI実行基盤に乗せる。

## 目的

Actor AI は、Actor が現在の状況を見て次の行動を自律的に決定するための仕組みである。

AIは以下の3階層で扱う。

- 長期目標: 何を達成したいか
- 中期計画: 長期目標を達成するためにどの方針で行動するか
- 短期行動: 直近で実行する具体的なAction

例:

- 長期目標: 薬草を10個集める
- 中期計画: 薬草が出るフロアを探索する
- 短期行動: 移動する、アイテムを拾う、敵と戦う、逃げる

## 配置方針

AIはUseCaseそのものではなく、Application層の意思決定サービスとして扱う。

推奨構成:

```text
Application/
  AI/
    ActorDecisionScheduler
    ActorAiContext
    ActorAiRuntimeState
    ActorAiDirtyFlags
    IActorAiPolicy
    AdventurerAiPolicy
    MonsterAiPolicy
    PetAiPolicy
    GuildStaffAiPolicy
  UseCase/
    AdvanceActorAiUseCase
    ApplyActorActionUseCase
```

Domain は、AI判断の結果として成立する状態を持つ。
Application/AI は、AIの評価タイミング、dirty管理、cooldown、判断ロジックを持つ。
UseCase は、AI評価の呼び出しと、決定されたActionの適用を担当する。

## Domain が持つもの

Actor または Actor に紐づくDomain状態として持ってよいもの:

- 現在の長期目標
- 現在の中期計画
- 現在の短期Action
- Actionの実行状態
- AI判断に使う永続的な傾向値
- AI判断に使う保存対象の記憶

Domain に持たせないもの:

- dirty flag
- cooldown
- last evaluated tick
- 1tick内で評価済みかどうか
- 一時的なイベント蓄積キュー
- 再評価優先度

これらは Application/AI の実行管理であり、Actor本体に持たせない。

## AI階層

### LongTermGoal

長期目標は、Actor が何を達成したいかを表す。

例:

- レベルを上げる
- 特定アイテムを集める
- 特定モンスターを討伐する
- 特定フロアへ到達する
- 施設勤務を継続する
- 巡回する

長期目標はセーブ対象にする。

### MidTermPlan

中期計画は、長期目標を達成するための現在の方針を表す。

例:

- 目的アイテムが出るフロアへ向かう
- 目的アイテムが集まるまで現在フロアを探索する
- HPが危険なので回復可能な場所へ戻る
- 勤務施設へ移動する
- 縄張り周辺を巡回する

中期計画は基本的にセーブ対象にする。
ただし、復元が難しい一時的な探索候補リストなどは保存しない。

### ShortTermAction

短期Actionは、直近で実行する具体的な行動を表す。

例:

- 移動する
- アイテムを拾う
- 敵を攻撃する
- 逃げる
- 階段を使う
- 施設を利用する
- 待機する

短期Actionは単なる結果値ではなく、実行状態を持つ。

想定する実行状態:

- NotStarted
- Running
- Completed
- Failed
- Cancelled

短期Actionを保存する場合は、Action種別と対象IDなど最低限に留める。
復元時はGoal/Planから再評価してもよい。

## 再評価の考え方

AIは1tickごとに常時評価しない。
状態変化が起きたときに dirty を立て、別のタイミングで最大1回だけ評価する。

イベント発生時に直接AI判定を行わない。
イベントは dirty flag を立てるだけにする。

### Dirty Flags

dirty は階層別に持つ。

```csharp
[Flags]
public enum ActorAiDirtyFlags
{
    None = 0,
    ShortTerm = 1,
    MidTerm = 2,
    LongTerm = 4
}
```

処理時は、dirtyになっている最上位層から再評価する。

```text
LongTerm dirtyあり -> LongTerm評価 -> MidTerm再構築 -> ShortTerm決定
MidTerm dirtyあり  -> MidTerm評価  -> ShortTerm決定
ShortTerm dirtyあり -> ShortTerm決定
```

短期Actionの結果から中期計画の更新が必要な場合は MidTerm dirty を立てる。
中期計画の結果から長期目標の更新が必要な場合は LongTerm dirty を立てる。

### Event To Dirty Mapping

イベントをそのままAI dirtyに変換しすぎない。
AI用の意味イベントに集約してから dirty を立てる。

例:

| 発生した変化 | AI用イベント | dirty |
|---|---|---|
HPが減った | HealthBandChanged | ShortTerm / MidTerm |
敵と接敵した | EnemyEnteredRange | ShortTerm |
ダンジョンに入った | EnteredDungeon | MidTerm / ShortTerm |
アイテムを入手した | ObjectiveItemCountChanged | MidTerm / LongTerm |
ボスを倒した | ObjectiveMonsterDefeated | LongTerm |
ゲーム内日付が変わった | GameDateChanged | MidTerm / LongTerm |
現在Actionが失敗した | CurrentActionFailed | ShortTerm / MidTerm |
現在Actionが完了した | CurrentActionCompleted | ShortTerm |

どのイベントがどの階層をdirtyにするかは、AI設計上の重要な表として管理する。

## Cooldown

AI評価にはcooldownを設ける。
戦闘中など短時間に多くのイベントが発生する状況でも、一定間隔より短く再評価しない。

初期方針:

- 1tick内でActorごとのAI評価は最大1回
- dirty が立っていても、評価cooldown中は処理しない
- 短期AIの最小評価間隔は 0.5秒相当

AI評価cooldownは、原則として simulation time / simulation tick で扱う。
UIや演出都合のリアル時間ではなく、ゲーム進行に同期する時間を使う。

## ActorAiRuntimeState

Application/AI は、Actorごとの実行管理状態を持つ。

想定フィールド:

```csharp
public sealed class ActorAiRuntimeState
{
    public Guid ActorId { get; }
    public ActorAiDirtyFlags DirtyFlags { get; private set; }
    public int LastEvaluatedTick { get; private set; }
    public int CooldownUntilTick { get; private set; }
    public bool EvaluatedThisTick { get; private set; }
}
```

このRuntimeStateは原則としてセーブ対象にしない。
セーブ復元後は、Goal/Plan/CurrentActionから必要なdirtyを立て直す。

## ActorDecisionScheduler

ActorDecisionScheduler は、どのActorのAIをいつ評価するかを制御する。

責務:

- イベントから dirty を立てる
- tickごとに評価可能なActorを選ぶ
- 1tick最大1回の制限を守る
- cooldownを守る
- dirtyの最上位層を判定する
- 対応するAI Policyを呼び出す

ActorDecisionScheduler はDomain Entityを直接変更しすぎない。
意思決定結果をUseCaseへ渡し、Action適用はUseCaseで行う。

## AI Policy

Actor種別ごとにPolicyを分ける。
共通基盤は共有し、判断内容は分ける。

共通化してよいもの:

- dirty / cooldown / scheduler
- LongTerm / MidTerm / ShortTerm の評価順
- Action実行状態
- Action結果の扱い

共通化しすぎないもの:

- Adventurerの探索目的選択
- Adventurerの施設利用判断
- Monsterの徘徊判断
- Monsterの戦闘継続判断
- Petの追従判断
- GuildStaffの勤務判断

想定インターフェース:

```csharp
public interface IActorAiPolicy
{
    bool CanHandle(Actor actor);
    ActorAiDecision EvaluateLongTerm(ActorAiContext context);
    ActorAiDecision EvaluateMidTerm(ActorAiContext context);
    ActorAiDecision EvaluateShortTerm(ActorAiContext context);
}
```

`CanHandle` は実体型やBehaviorを見て判断する。
Behaviorに分類用enumを重複保持しない。

## ActorAiContext

AI Policyには、判断に必要な情報をContextとして渡す。

想定情報:

- Actor
- 現在地点
- 現在フロア
- 周辺の敵
- 周辺のアイテム
- 利用可能な施設
- 宿屋予約状況
- 交換項目
- 現在時刻 / 現在tick
- 現在のGoal / Plan / Action

Contextは読み取り用にする。
Policyは直接Domainを更新せず、Decisionを返す。

## ActorAiDecision

AI Policy は、Domainを直接変更せず Decision を返す。

例:

```csharp
public sealed class ActorAiDecision
{
    public ActorGoal NextGoal { get; }
    public ActorPlan NextPlan { get; }
    public ActorAction NextAction { get; }
    public ActorAiDirtyFlags AdditionalDirtyFlags { get; }
}
```

UseCase が Decision をActorやWorldへ適用する。

## UseCase

### AdvanceActorAiUseCase

AI評価を進めるUseCase。

責務:

- Schedulerから評価対象Actorを受け取る
- ActorAiContextを構築する
- 対応するPolicyを呼び出す
- Decisionを受け取る
- 必要に応じてApplyActorActionUseCaseへ渡す

### ApplyActorActionUseCase

Decisionの結果をDomainへ適用するUseCase。

責務:

- Goal / Plan / Actionの更新
- Action開始
- Action完了 / 失敗の反映
- Actor位置やInventoryなどDomain状態の更新
- 取引や施設利用など既存UseCaseとの接続

## 永続化方針

保存する:

- LongTermGoal
- MidTermPlan
- 必要ならCurrentActionの種類と対象ID
- AI判断に必要な永続的Memory / Preference

保存しない:

- dirty flag
- cooldown
- last evaluated tick
- evaluated this tick
- 一時的な探索候補
- イベントキュー

復元時は、保存されたGoal/Plan/Actionから再評価に必要なdirtyを立て直す。

## 懸念点と対策

### DomainとApplicationの境界が曖昧になる

対策:

- ActorにはAIの現在意思を持たせる
- dirty、cooldown、評価時刻はApplication/AIへ置く
- PolicyはDecisionを返し、Domain更新はUseCaseで行う

### 再評価ルールが複雑化する

対策:

- Event To Dirty Mapping を表として管理する
- dirtyは階層別に持つ
- 最上位dirtyから再評価する

### DecisionとAction適用が循環する

対策:

- Actionに実行状態を持たせる
- Action開始、進行中、完了、失敗を区別する
- 位置更新など細かい変化をそのままdirtyにしない

### 全Actor共通化しすぎる

対策:

- Schedulerと階層評価の枠組みだけ共通化する
- 判断内容はBehavior別Policyに分ける

## 初期実装スコープ

最初の実装では、以下を優先する。

1. Domainに `ActorGoal` / `ActorPlan` / `ActorAction` の基本型を追加する
2. Application/AIに dirty / cooldown / runtime state を追加する
3. Adventurer用Policyを最初に作る
4. Monster / Pet / GuildStaff は同じインターフェースに乗せられる最小実装にする
5. Event To Dirty Mapping は小さく始め、実装しながら拡張する

## 未決定事項

- 具体的な `ActorGoalType`
- 具体的な `ActorPlanType`
- 具体的な `ActorActionType`
- cooldownをtick数として何tickにするか
- 戦闘中Actionの詳細
- 移動ActionとNavigation UseCaseの接続方法
- セーブ対象にするCurrentActionの粒度

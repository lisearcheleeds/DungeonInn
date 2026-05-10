# ActorEffect / StatusEffect Design

## Purpose

ActorEffect / StatusEffect は、回復薬・バフ・デバフなど、Actor に一定時間作用する効果を扱うための共通基盤とする。

最初の実装対象は体力回復ポーション:

- 体力回復ポーションを使用すると、使用した本人に ActorEffect を付与する
- ActorEffect は StatusEffect を通じて、10秒かけて HP を 30 回復する
- この時間経過処理は、将来のバフ・デバフにも流用する

## Concept

Actor は StatusEffect を直接抱えない。

Actor が保持するのは、ユーザーに見える管理単位である ActorEffect。
ActorEffect が、実際に Actor へ作用する StatusEffect を1つ以上持つ。

```text
Actor
  └─ ActorEffects[]
        ├─ ActorEffectMasterId
        ├─ RemainingSeconds / ElapsedSeconds
        └─ StatusEffectSpecs[]
              ├─ Type
              ├─ Amount
              ├─ DurationSeconds
              ├─ TickIntervalSeconds
              └─ AggregationPolicy
```

## ActorEffect

ActorEffect は「何によって付与された効果か」を表す。

例:

- 体力回復ポーション
- 上級体力回復ポーション
- 移動速度低下の呪い
- 攻撃力上昇ポーション

ActorEffect は UI / ログ / 再付与ルールの単位になる。
同じ StatusEffect を持っていても、ActorEffect が異なれば別の効果として扱う。

Runtime の ActorEffectInstance は `ActorEffectMasterId` と経過時間などの実行時状態だけを持つ。
表示名や再付与ルールは `ActorEffectMaster.Name` / `ActorEffectMaster.ReapplyPolicy` から解決する。
マスタから O(1) で取得できる不変値を ActorEffectInstance に複製してはならない。

例:

- 体力回復ポーション: 10秒で HP を 30 回復
- 上級体力回復ポーション: 10秒で HP を 60 回復

この2つを同時に使用した場合、ActorEffect は別々に保持される。
StatusEffect の合成結果として、10秒で HP を 90 回復する。

## StatusEffect

StatusEffect は Actor に実際に作用する内側の効果を表す。

例:

- HP を時間経過で回復する
- HP を時間経過で減少させる
- 移動速度を低下させる
- 攻撃力を上昇させる

StatusEffect は排他的な状態異常ではない。
Actor には複数の ActorEffect が付与され、それぞれが複数の StatusEffect を持てる。

## Duration

ActorEffect と StatusEffect の Duration は分ける。

ActorEffect の Duration は、外側の効果コンテナが Actor に残る時間。
StatusEffect の Duration は、内側の効果が実際に作用する時間。

これにより、将来以下のような効果を表現できる。

- 10秒間、2秒ごとに毒を付与する
- 30秒間の ActorEffect のうち、最初の10秒だけ移動速度を上げる
- 1つの ActorEffect から複数の StatusEffect を異なるタイミングで発生させる

## Reapply Policy

同じ ActorEffect を再付与した時の振る舞いは ActorEffect 側で定義する。

```csharp
public enum ActorEffectReapplyPolicy
{
    AppendDuration,
    AddStack,
    RefreshDuration
}
```

### AppendDuration

既存 ActorEffect の残り時間に、新しく付与された効果時間を加算する。

例:

- 10秒で HP を 30 回復する体力回復ポーションを使用
- 1秒後に同じ体力回復ポーションを再使用
- 最終的に20秒かけて HP を 60 回復する

### AddStack

同じ ActorEffect を別スタックとして重ねる。
各スタックは個別の残り時間を持つ。

例:

- 10秒間、毎秒 1 Gold を得る効果を使用
- 1秒後に同じ効果を再使用
- 最初の1秒は毎秒 1 Gold
- 2秒目から10秒目までは毎秒 2 Gold
- 11秒目は毎秒 1 Gold

### RefreshDuration

効果量やスタック数は増やさず、ActorEffect の効果時間を再開始する。

例:

- 10秒間、攻撃力 +10 の効果を使用
- 1秒後に同じ効果を再使用
- 攻撃力 +20 にはならず、合計で11秒間、攻撃力 +10 が続く

## Aggregation Policy

複数の ActorEffect が同じ StatusEffectType を持つ場合、StatusEffectType ごとの合成ルールに従って最終値を決める。

```csharp
public enum StatusEffectAggregationPolicy
{
    Sum,
    MostEffective
}
```

### Sum

同じ StatusEffectType の有効な効果を合算する。

例:

- 体力回復ポーション: 10秒で HP を 30 回復
- 上級体力回復ポーション: 10秒で HP を 60 回復
- 同時使用時は、合計して10秒で HP を 90 回復する

### MostEffective

同じ StatusEffectType の有効な効果のうち、最も効果が強いものだけを採用する。

Amount は効果の強さを正の値で表す。
MostEffective は、同じ StatusEffectType の中で Amount が最大のものを採用する。

例:

- 移動速度低下 A: 10秒間、移動速度を 20 低下
- 移動速度低下 B: 5秒間、移動速度を 50 低下
- 同時使用時は、最初の5秒間は 50 低下
- 残りの5秒間は 20 低下

移動速度低下は `-20` / `-50` のような負数ではなく、`MoveSpeedDown amount = 20` / `50` のように正の効果量として扱う。

## Initial Implementation Scope

最初に実装する範囲:

- `ActorEffectMaster`
- `ActorEffectInstance`
- `StatusEffectSpec`
- `ActiveStatusEffect`
- `ActorEffectReapplyPolicy`
- `StatusEffectAggregationPolicy`
- `AdvanceActorEffectsUseCase`
- 体力回復ポーションの使用
- HP を 10秒かけて 30 回復する StatusEffect

初期実装では、体力回復ポーションは `AppendDuration` を使う。
StatusEffectAggregationPolicy は HP 回復では `Sum` を使う。

## Open Notes

- StatusEffect の発生タイミングを最初から持つか、初期実装では Duration と TickInterval のみで扱うかは実装時に確認する
- 回復薬を使用する AI / 行動選択は、ActorEffect 基盤とは別タスクに分ける
- UI 表示は ActorEffect 単位で行い、StatusEffect は内部計算単位として扱う
- StatusEffect によるパラメータ補正は、ActorParams の再計算経路と接続する必要がある

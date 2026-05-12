# Milestone 4 Roadmap

このドキュメントは Milestone 4 の実装計画をまとめる。

## 対象ループ

```text
冒険者が来訪する
-> ダンジョンへ向かう
-> モンスターと戦闘する（Projectile / Area 攻撃を含む）
-> 宿屋へ帰還する
-> 宿泊・施設利用・売上・評判に反映される
-> AI判断理由とイベント履歴からシミュレーションの状態を追える
```

## 到達目標

- 戦闘の CombatEffect 実行が Direct / Projectile / Area で共通化され、撃破処理やダメージ適用の重複が減っている
- 宿屋経営シミュレーションとして、宿泊・施設利用・売上・満足度/評判の最小ループが内部状態として成立している
- AIの主要判断について、なぜその行動を選んだかをログ/イベント履歴から追える
- Pause / 倍速など、シミュレーション観察に必要な時間操作がUseCaseとして用意されている
- Unity/GameObject 表現に移る前に、内部シミュレーションの状態変化をテストとログで確認できる

## 前提

- Milestone 3 のゲームループ、探索、帰還、アイテム経済が動作している
- Milestone 4 Phase 1 の Projectile 攻撃が実装済み
- Milestone 4 Phase 2 の Area 攻撃が実装済み
- NavMesh / GameObject / Unity表示都合の本格実装は Milestone 5 に分離する

---

## Phase 1: Projectile 攻撃

遠距離攻撃を持つモンスター・冒険者が飛び道具を発射する。

作るもの:

- `ProjectileInstance`
- `GameWorldState.Projectiles`
- `AdvanceProjectileUseCase`
- `ProjectileFired` / `ProjectileHit`
- Bow の `Projectile -> DirectDamage` CombatEffect

完了条件:

```text
[Combat] Goblin Archer fired projectile at Adventurer A
[Combat] Projectile hit Adventurer A for 12
```

状態:

- 実装済み

---

## Phase 2: Area 攻撃

爆発や広域魔法など、範囲内の複数ターゲットに効果を与える攻撃を実装する。

作るもの:

- `AreaEffectInstance`
- `GameWorldState.AreaEffects`
- `AttackAreaTargetResolver`
- `AdvanceAreaEffectUseCase`
- `AreaEffectCreated` / `AreaEffectHit`
- Scythe の `Area -> DirectDamage` CombatEffect

初期仕様:

- Instant / Duration の2種類
- ダメージのみ
- Friendly Fire は行わず、敵Factionのみを対象にする

完了条件:

```text
[Combat] Orc created area effect at (15.0, 8.0) radius 3.0m
[Combat] Area effect hit Adventurer A for 20
[Combat] Area effect hit Adventurer B for 20
```

状態:

- 実装済み

---

## Phase 3: CombatEffect 実行共通化

Projectile / Area 実装で分散し始めた攻撃実行、ダメージ適用、撃破解決を共通化する。

作るもの:

- `CombatEffectExecutor`
- `CombatDamageResolver`
- `CombatDefeatResolver`
- `CombatEffectExecutionId` または同等の実行単位ID
- Projectile / Area / DirectDamage から共通Executorを呼ぶ経路

初期仕様:

- DirectDamage、Projectile、Area のNode解釈をUseCase内の個別分岐から段階的にExecutorへ移す
- 撃破時の経験値付与、ドロップ、Actor削除、CombatTarget解除を `CombatDefeatResolver` に集約する
- `Projectile -> Area -> DirectDamage` の連鎖を実行できるようにする
- イベントには表示用加工値ではなく、実行結果として必要な事実だけを含める

完了条件:

```text
Projectile -> Area -> DirectDamage の攻撃定義を実行できる
Direct / Projectile / Area の撃破処理が同じResolverを通る
```

---

## Phase 4: シミュレーション時間操作

経営シミュレーションとして観察と検証をしやすくするため、時間操作のUseCaseを整える。

作るもの:

- Pause / Resume UseCase
- 1x / 2x / 4x などの時間倍率変更
- 現在日・現在時刻・時間倍率を参照するクエリ
- 重要イベント時の自動Pause候補を後で差し込める設計

初期仕様:

- UIはまだ必須にしない
- `GameClock` の状態変更はUseCase経由に寄せる
- PlayMode確認ではログまたはRuntime Queryで状態を確認する

完了条件:

```text
Pause中はゲーム時間が進まない
2x / 4x でスケジュール進行とフレーム進行が倍率通りに進む
```

---

## Phase 5: 宿屋経営ループ拡張

宿屋経営シミュレーションとして、戦闘・探索以外の内部数値ループを成立させる。

作るもの:

- 客室/ベッド稼働率
- 宿泊料金と売上集計
- 施設利用需要
- 満足度または評判
- 在庫消費/補充の最小ループ
- 日次集計

初期仕様:

- まずは内部状態とログで成立させる
- 施設の見た目やGameObject配置は Milestone 5 で扱う
- 満足度/評判は来訪者数や滞在判断に影響する最小モデルから始める

完了条件:

```text
[Daily] Guests=8 Occupancy=75% Sales=240G Reputation=12
[Inn] Adventurer A stayed and satisfaction changed +2
```

---

## Phase 6: AI行動理由ログとイベント履歴

シミュレーションゲームとして「なぜその状態になったか」を追えるようにする。

作るもの:

- AI判断理由イベント
- イベント履歴サービス
- 日次/直近イベントの参照API
- デバッグログ出力

対象にする判断:

- 冒険者がなぜ帰還したか
- なぜ宿を待っているか
- なぜアイテムを使ったか
- なぜ敵を狙ったか
- なぜその階層を選んだか
- なぜ施設を利用したか

完了条件:

```text
[AI] Adventurer A selected ReturnToInn: low HP 18/80
[AI] Adventurer B waits for inn: no vacant room
```

### Phase 6 追加リファクタリング: UseCase / Event / Aggregate 境界整理

`docs/guidelines/usecase-boundary-guidelines.md` を次回以降の実装ルールとして採用する。
既存コードはまだ完全準拠していないため、Milestone 4 Phase 6 の後続リファクタ作業として段階的に対応する。

対象:

- `IEventPublisher` / `IEventSubscriber` を追加し、`IGameEventBus` 直接注入を必要最小限に下げる
- `DecideAdventurerReturnUseCase` からイベント購読・Dirty状態・撃破数履歴をServiceへ分離する
- `RecoverAdventurerAtInnUseCase` から回復蓄積状態とイベント購読をServiceへ分離する
- `AdvanceActorSimpleLifecycleUseCase` の探索目的地状態をServiceへ分離する
- UseCaseからUseCaseを呼ぶ箇所を確認し、通常UseCase呼び出しとOrchestrator責務を明示的に分ける
- 読み取り専用処理向けに `IGameWorldStateReader` / 書き込み向けに `IGameWorldStateWriter` の分離を検討する
- `Actor.Inventory` / `Actor.Equipment` など可変Aggregate内部オブジェクトの公開範囲を見直す
- Domain Entity の static catalog 参照をRepository/Factory注入へ寄せる方針を検討する

完了条件:

- 新規実装が `usecase-boundary-guidelines.md` に準拠している
- UseCaseが長期状態・イベント購読を直接持つ箇所がPhase 6対象範囲で解消されている
- ゲーム状態を変更するイベント購読者が増えていない
- `uloop.cmd compile --project-path Client` と EditMode テストが成功している

状態:

- 実装済み
- 実装ログ: `docs/self-review/milestone4-phase6-boundary-refactor.md`

---

## Phase 7: Unity化前の境界整理

Milestone 5 で NavMesh / GameObject / View 表現に進む前に、Application / Domain の責務境界をもう一段整理する。
Unity 表現が乗った後に副作用の追跡が難しくならないよう、GameObject 化で触れる可能性が高い境界を先に固める。

作るもの / 対応するもの:

- UseCase から UseCase を呼ぶ箇所の棚卸し
- Orchestrator UseCase と通常 UseCase の責務分離
- `IGameWorldStateReader` / `IGameWorldStateWriter` の導入検討と、読み取り専用箇所の段階的置き換え
- GameObject 化で参照される Actor / WorldState 周辺の Aggregate 境界点検
- `Actor.Inventory` / `Actor.Equipment` など可変内部オブジェクトの公開範囲見直し方針
- Domain Entity の static catalog 参照を Repository / Factory 注入へ寄せる方針整理

初期仕様:

- 既存挙動を変えないリファクタリングを優先する
- GameObject / Unity API を使う実装は Milestone 5 に残す
- 影響範囲が広い Domain API 変更は、対象と移行手順を明確にしてから実装する
- すべてを一括で完了させるのではなく、Milestone 5 の実装前にブロッカーになる境界から対応する

完了条件:

- UseCase から UseCase を呼ぶ箇所が一覧化され、Orchestrator 化する対象と維持する対象が明確になっている
- Unity View / Presenter から参照される WorldState 読み取り経路に、読み取り専用契約を適用する方針が決まっている
- GameObject 化で直接触れる Actor / WorldState 周辺に、Aggregate 境界を壊す公開 API が残っていない、または対応タスク化されている
- `uloop.cmd compile --project-path Client` と EditMode テストが成功している

状態:

- 完了
- 実装ログ: `docs/self-review/milestone4-phase7-unity-boundary-review.md`

---

## Phase 8: 時間・日別レポート・イベント履歴の責務整理

Milestone 5 の UI / GameObject 化に入る前に、シミュレーション時間と統計表示の責務を整理する。
現在の `Day` はゲーム進行の正規状態として扱うには副次的であり、`Exp` と `Level` の関係に近い。
正規状態はゲーム開始からの累積 Tick とし、日付や日内時刻は表示・集計のために Tick から導出する。

この整理の目的:

- `CurrentDay` と `CurrentScheduleTick` の二重管理を避け、時間状態のズレを防ぐ
- 「現在状態表示」と「日別レポート」を明確に分ける
- 日別レポートを表示のたびに再集計するものではなく、一定 Tick 境界で保存されるスナップショットにする
- イベント履歴を日別レポートの保存責務から切り離し、発生事実の履歴として独立管理する
- Milestone 5 の UI から、現在状態・日別レポート・イベント履歴をそれぞれ正しい Query で参照できるようにする

時間設計:

```text
正規状態:
  TotalScheduleTick = ゲーム開始から累積した Tick

派生値:
  Day = TotalScheduleTick / GameScheduleTicksPerDay
  TickOfDay = TotalScheduleTick % GameScheduleTicksPerDay
```

初期仕様:

- 1 Tick はゲーム内スケジュールの最小進行単位として扱う
- `GameScheduleTicksPerDay = 1200` は維持する
- 日付は保存状態ではなく、Tick から計算する派生値として扱う
- `IGameClock.CurrentDay` は削除、または互換目的の派生プロパティに限定する
- `GameTimeState` / `GameLoopTickResult` は Tick を正として返し、Day は必要なら計算値として扱う
- 日付変更判定は `TotalScheduleTick` が日境界を跨いだかで判定する
- 高倍率や長い delta で複数日を跨いでも、必要な日別スナップショットを取りこぼさない

日別レポート設計:

- 日別レポートは Tick 境界到達時に保存されるスナップショットとする
- 保存単位は `[day * 1200, (day + 1) * 1200 - 1]` の Tick 範囲
- レポート保存は副作用のある経営処理ではなく、表示・分析用の記録処理とする
- レポート保存時に、評判更新・在庫補充・カウンタリセットなどのゲーム状態変更は行わない
- 評判 `Reputation` は当面利用しないため、既存値のまま表示するだけでよい
- 現在状態表示は `GetInnCurrentStatusUseCase` 相当の Query とし、保存済み日別レポートとは別に扱う
- 日別レポート表示は保存済みスナップショットを読む Query とする

イベント履歴設計:

- イベント履歴は `OccurredAtTick` を正として保持する
- イベント履歴は日別レポート生成のための一次データとして依存しない
- `GetByDay(day)` のような API が必要な場合も、内部では Tick 範囲へ変換して検索する
- 日別レポートはイベント履歴から毎回再集計するのではなく、専用の保存済みレポートから取得する
- イベント履歴は「なぜそうなったか」を追うためのログであり、宿屋統計の永続保存とは責務を分ける

作るもの / 対応するもの:

- `GameClock` の正規時間を累積 Tick に一本化する
- `GameTimeState` / `GameLoopTickResult` / `GameClockAdvanceResult` の時間表現を見直す
- `GameTimeUtility` または同等の Tick -> Day / TickOfDay 変換ロジックを用意する
- 日境界を跨いだ Tick 範囲を列挙する仕組みを用意する
- `InnDailyReport` を保存する Repository / Store / Service を追加する
- 日別レポート保存 UseCase を、イベント履歴再集計ではなくスナップショット保存に変更する
- 現在状態 Query と日別レポート Query を分離する
- `GameEventHistoryEntry.Day` を廃止し、Tick ベースの参照へ寄せる
- `GetGameEventHistoryUseCase` の日別参照は Tick 範囲参照へ置き換える、または日別指定を内部変換に限定する
- `L` キーなどの現在状態表示は、日別レポートではなく現在状態 Query を使い続ける

完了条件:

- `GameClock` の正規状態が累積 Tick になっている
- Day は Tick から導出され、永続的な進行状態として二重管理されていない
- 日付境界を跨いだタイミングで日別レポートのスナップショットが保存される
- 現在状態表示と保存済み日別レポート表示の Query が分かれている
- イベント履歴は Tick ベースで管理され、日別レポート保存と責務が分離されている
- 高倍率時に複数日を跨いでもレポート保存漏れがない
- `uloop.cmd compile --project-path Client` と EditMode テストが成功している

状態:

- 実装レビュー待ち

---

## 推奨実装順

1. Phase 3: CombatEffect 実行共通化
2. Phase 4: シミュレーション時間操作
3. Phase 5: 宿屋経営ループ拡張
4. Phase 6: AI行動理由ログとイベント履歴
5. Phase 7: Unity化前の境界整理
6. Phase 8: 時間・日別レポート・イベント履歴の責務整理

Projectile / Area は実装済みのため、以降はUnity表示に進む前の内部シミュレーション基盤を整える。

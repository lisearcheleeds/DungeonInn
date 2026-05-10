# Milestone 5 Roadmap

このドキュメントは Milestone 5 の実装計画をまとめる。

Milestone 5 では、Milestone 4 までに作った内部シミュレーションを Unity / GameObject / NavMesh / 表示表現へ接続する。

## 対象ループ

```text
内部シミュレーションがActor/施設/戦闘/経済状態を更新する
-> GameObject が状態を視覚化する
-> NavMesh でActorが地形に沿って移動する
-> Projectile / Area / 施設利用 / 宿屋状態が画面上で確認できる
-> UIで時間操作・イベント履歴・経営状態を確認できる
```

## 到達目標

- ダンジョン生成後にNavMeshが構築され、Actorが壁を通り抜けずに移動する
- Actor / Projectile / AreaEffect がGameObjectとして視覚化される
- 宿屋施設、客室、利用状況、Actorの状態が画面上で追える
- 時間操作、イベント履歴、日次集計をUIから確認できる
- GameObject表現はシミュレーションの判定主体にならず、Domain/Applicationの状態を反映するViewとして機能する

## 前提

- Milestone 4 の内部シミュレーション基盤が完了している
- CombatEffect実行、宿屋経営ループ、AI判断理由、時間操作がUseCaseとして存在する
- Unity ColliderやGameObject検索を戦闘判定・経営判定の主ロジックにしない

---

## Phase 1: NavMesh 連携

移動を直線移動から NavMesh 経路探索に切り替える。

作るもの:

- `NavMeshBakeService`
- `NavMeshActorNavigationService`
- `IActorNavigationService` のNavMesh実装
- `MoveActorTowardDestinationUseCase` のNavMesh経路追従
- 経路取得失敗時のフォールバック

初期仕様:

- ダンジョン生成完了後に NavMesh をBakeする
- フロアを跨いだ移動は対象フロアのNavMeshを使う
- 経路が取れない場合は既存の直線移動にフォールバックする
- NavMeshは移動経路にのみ使い、戦闘判定やAI判断の主データにはしない

完了条件:

- 冒険者・モンスターが壁を通り抜けず、廊下や部屋を通って目標へ到達する
- ダンジョン再生成後にNavMeshが再Bakeされる
- 経路取得失敗時にゲーム進行が止まらない

---

## Phase 2: Actor GameObject 表現

Actorの内部状態をGameObjectで表示する。

作るもの:

- Actor表示Presenter / Visualizerの整理
- Actor種類ごとの見た目差分
- HP / 状態 / 行動中ターゲットのデバッグ表示
- Actor生成/削除とGameObject生成/削除の同期

初期仕様:

- まずはプリミティブ/プレースホルダーでよい
- Domain ActorがGameObject参照を持たない
- View側が `ActorId` をキーにGameObjectを管理する

完了条件:

- Spawn / Despawn / Move / Defeat が画面上で追える
- Actor数と表示GameObject数が一致する

---

## Phase 3: Combat Visual 表現

Projectile / Area / DirectDamage を視覚的に確認できるようにする。

作るもの:

- Projectile GameObject表示
- AreaEffect範囲表示
- Damage / Hit / Defeat の簡易表示
- CombatイベントからVisualを再生するPresenter

初期仕様:

- 判定はApplication層の結果を使う
- VisualはイベントとWorldStateを購読して再生する
- 見た目が無くてもシミュレーションは動く

完了条件:

```text
Bow攻撃でProjectileが飛ぶ
Scythe攻撃でArea範囲が表示される
Hit時に対象のHP変化が視覚的に分かる
```

---

## Phase 4: 宿屋施設 GameObject 表現

宿屋経営ループの内部状態を画面上で確認できるようにする。

作るもの:

- 客室/ベッド/施設のGameObject
- 稼働中/空き/待ち状態の表示
- 施設利用中Actorの表示
- 在庫や売上に関係する簡易表示

初期仕様:

- 施設判定はDomain/Application側の状態を使う
- GameObject配置はViewの表現責務に留める
- 施設のクリック操作や詳細UIは後続でもよい

完了条件:

- 宿泊中・待機中・利用中の状態が画面上で確認できる
- 稼働率や売上ログと表示状態が大きく矛盾しない

---

## Phase 5: シミュレーションUI

プレイヤー/開発者がシミュレーション状態を確認・操作できるUIを作る。

作るもの:

- Pause / 1x / 2x / 4x 操作
- 日付/時刻/所持金/評判/稼働率表示
- イベント履歴ログ
- Actor詳細の簡易表示
- 日次集計表示

初期仕様:

- Canvasを使う場合は `ProductCanvasMainSceneBase` 系の設計に従う
- WorldScene本体は非Canvas MainSceneとして維持し、必要なUIはModuleSceneまたは適切なCanvas構成で扱う
- UIはUseCase/Queryを呼び、Domain状態を直接改変しない

完了条件:

- 時間操作がUIから行える
- 直近イベントと日次集計がUIで読める
- 経営状態の主要指標が常時確認できる

---

## 推奨実装順

1. Phase 1: NavMesh 連携
2. Phase 2: Actor GameObject 表現
3. Phase 3: Combat Visual 表現
4. Phase 4: 宿屋施設 GameObject 表現
5. Phase 5: シミュレーションUI

NavMeshはActor表示と強く関係するため、Milestone 5の最初に扱う。UIは内部状態のQueryが揃ってから実装する。

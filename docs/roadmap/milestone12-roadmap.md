# Milestone 12 Roadmap — Faction / Pet / オーディオと演出

## ゴール

Milestone 12 では、ゲーム世界の関係性を広げるために Faction と Pet の初期実装を行う。
あわせて、ゲームの雰囲気と没入感を高める音・演出を追加する。
Faction / Pet はゲームルールと Actor 行動の拡張、オーディオ / 演出は見た目と手触りの拡張として扱い、責務を混ぜない。

## Phase 一覧

### Phase 1: Faction 基盤

- `FactionId` / `FactionRelationType` / `FactionRelation` を定義する。
- 初期 Faction と初期関係を master / config として定義する。
- Faction 定義の読み取り経路を Repository / Service として用意する。
- Actor が現在所属する Faction を、戦闘・AI・表示から参照できる状態にする。

完了条件:

- [ ] 初期 Faction が master / config から取得できる
- [ ] Actor の Faction が Behavior 型に依存せず取得できる
- [ ] Faction 関係を取得する Application 経路が存在する

### Phase 2: 戦闘・AI の Faction 化

- 戦闘開始判定を Adventurer / Monster 固定分岐から Faction 関係参照へ置き換える。
- AI の敵対対象選択を Faction 関係参照へ置き換える。
- `Neutral` / `Hostile` / `Allied` の差が AI と戦闘開始条件に反映されるようにする。

完了条件:

- [ ] Hostile 関係の Actor 同士だけが戦闘候補になる
- [ ] Allied 関係の Actor 同士が戦闘候補にならない
- [ ] AI が Behavior 型名ではなく Faction 関係で敵対対象を選ぶ

### Phase 3: Faction UI

- Faction 関係を確認する UI を追加する。
- UI は Application Query / ViewData から表示し、Repository や Domain object を直接読まない。
- 初期実装では閲覧専用とし、関係編集 UI は必須にしない。

完了条件:

- [ ] Faction 一覧と関係種別を画面で確認できる
- [ ] UI 表示用 DTO / ViewData が用意されている
- [ ] View / Presenter が Faction 判定ロジックを持っていない

### Phase 4: Pet 化フロー

- Pet 化可能な Monster を master / config で定義する。
- Monster Actor を削除せず、同一 Actor の Behavior と Faction を切り替える UseCase を実装する。
- Pet 化後も Actor ID、レベル、経験値、装備、Inventory、PreferenceSeed を維持する。

完了条件:

- [ ] Pet 化可能条件を満たす Monster を Pet 化できる
- [ ] Pet 化後も同一 Actor ID が維持される
- [ ] Pet 化後の Faction が `TamedMonster` になる

### Phase 5: Pet AI / 施設 / Save Load

- Pet 用 AI policy を追加する。
- Pet の宿屋滞在・施設利用の初期挙動を追加する。
- Pet 状態を Save / Load に含める。ただし Actor 座標は M10 方針どおり保存しない。

完了条件:

- [ ] Pet が Guild / Adventurer と敵対しない
- [ ] Pet が Hostile な Monster を敵対対象にできる
- [ ] Load 後も Pet が Monster に戻らない
- [ ] Load 後の Pet が地上または宿屋側の開始位置から再開する

### Phase 6: Audio 基盤

- Lighthouse の Audio モジュールを利用する。
- BGM / SFX の再生経路と音量設定の責務を分離する。
- Title / World / Dungeon / UI 操作の最小音を設定する。

完了条件:

- [ ] BGM と SFX が Lighthouse Audio 経由で再生される
- [ ] 音量設定の入口が用意されている
- [ ] Audio 再生が View / Presenter に直書きされていない

### Phase 7: 演出・ポリッシュ

- 攻撃、被弾、アイテム取得、施設利用の演出を追加する。
- 昼夜切り替え、シーン遷移、軽量なポストプロセスを調整する。
- 演出はゲーム状態を変更せず、Domain / Application の結果を表示するだけにする。

完了条件:

- [ ] 主要な戦闘・取得・施設利用に視覚演出がある
- [ ] 演出が Domain / Application 状態変更を所有していない
- [ ] PlayMode で過剰ログやエラーが出ない

### Phase 8: 統合確認

- Faction / Pet / Audio / 演出を Launcher -> Title -> Start 経由で Play 確認する。
- `uloop.cmd compile --project-path Client` と EditMode test を実行する。
- Milestone 12 完了レビューで guideline 適合性を確認する。

完了条件:

- [ ] Compile が成功している
- [ ] EditMode test が pass している
- [ ] PlayMode で `[World] GameWorldState initialized` が確認できる
- [ ] エラーログが存在しない

## 詳細仕様

### Faction と勢力関係

Actor の敵味方判定を、Adventurer / Monster の固定分岐から Faction 関係へ移行する。
戦闘・AI・表示は Actor のクラス名や Behavior 型ではなく、Actor が所属する Faction と Faction 間の関係を参照する。

実装対象:

- `FactionMaster` または同等の Faction 定義
- `FactionId`
- `FactionRelation`
- `FactionRelationType`
  - `Neutral`
  - `Hostile`
  - `Allied`
- Faction 関係を取得する Repository / Service
- Actor が所属 Faction を持つ経路
- 戦闘開始判定の Faction 化
- AI の敵対対象選択の Faction 化
- Save / Load で runtime relation override を復元する経路
- Faction UI（勢力関係の確認画面）

初期 Faction:

- `Guild`
- `Adventurer`
- `Monster`
- `TamedMonster`
- `Merchant`

初期関係:

| From | To | Relation |
|---|---|---|
| Adventurer | Monster | Hostile |
| Monster | Adventurer | Hostile |
| Guild | Monster | Hostile |
| Monster | Guild | Hostile |
| Guild | Adventurer | Allied |
| Adventurer | Guild | Allied |
| Guild | TamedMonster | Allied |
| TamedMonster | Guild | Allied |
| Adventurer | TamedMonster | Allied |
| TamedMonster | Adventurer | Allied |
| Merchant | Guild | Neutral |
| Merchant | Adventurer | Neutral |
| Merchant | Monster | Neutral |

責務境界:

- Domain は Faction ID、Faction 関係、関係種別を表す。
- Domain は Unity object、Scene、UI、Addressable を持たない。
- Application は「この Actor 同士が敵対しているか」「AI が対象にできるか」を判定する。
- View は Faction relation の ViewData を表示するだけで、敵対判定を直接行わない。
- Infrastructure は Faction 定義の読み込み元を所有する。
- SaveData は runtime relation override だけを保存し、master 由来の初期関係を丸ごと保存しない。

動的変化:

- 初期実装では master / config の関係を正とする。
- 動的変化は runtime override として扱う。
- runtime override が存在する場合は master / config より優先する。
- runtime override は Save / Load の対象にする。
- View / Presenter の一時状態だけで関係を変更した扱いにしない。

禁止:

- `actor.Behavior is MonsterBehavior` のような型判定で敵対判定を続けること。
- `bool IsEnemy` / `bool IsFriendly` のような二値で勢力関係を固定すること。
- Faction UI が Domain object や repository を直接読むこと。
- Faction 関係の変更を View / Presenter の一時状態だけに置くこと。
- 初期関係の master 値を SaveData に複製保存すること。

完了条件:

- [ ] 戦闘開始判定が Faction 関係を参照している
- [ ] AI の敵対対象選択が Faction 関係を参照している
- [ ] 初期 Faction と初期関係が master / config として定義されている
- [ ] runtime relation override が Save / Load で復元される
- [ ] Faction UI で勢力関係を確認できる
- [ ] 型名分岐による敵対判定が残っていない

### Pet システム（初期実装）

Pet は、既存 Actor を維持したまま `MonsterBehavior` から `PetBehavior` へ振る舞いを切り替えた Actor として扱う。
Pet 化によって Actor ID、レベル、経験値、装備、Inventory、PreferenceSeed は維持する。
Pet 化後の所属 Faction は `TamedMonster` とし、Guild / Adventurer と敵対しない。

実装対象:

- `PetBehavior`
- Monster から Pet へ変換する UseCase
- Pet 化可能条件
- Pet の Faction 変更
- Pet AI policy
- Pet の宿屋滞在 / 施設利用の初期挙動
- Pet の表示上の区別
- Pet 状態の Save / Load 対応

Pet 化条件:

- M12 初期実装では、すべての Monster が Pet 化できるわけではない。
- Pet 化可能な Monster は master / config 側で明示する。
- Pet 化の成立条件は初期実装では簡易条件に留める。
  - 対象 Monster が生存している
  - 対象 Monster が Pet 化可能フラグを持つ
  - 対象 Monster が戦闘中でない、または戦闘終了後に処理される
  - ギルド側に最低限の受け入れ余地がある
- 捕獲アイテム、専用施設、好感度などの複雑な条件は M12 では必須にしない。

Pet 化後に維持するもの:

- ActorId
- ActorName
- Level / Experience
- Stats / Current HP / MP
- Equipment
- Inventory
- PreferenceSeed
- Visual definition

Pet 化後に変更するもの:

- Behavior: `MonsterBehavior` -> `PetBehavior`
- Faction: `Monster` -> `TamedMonster`
- AI policy: Monster AI -> Pet AI
- 宿屋 / ギルド側の所属表示

Pet AI 初期方針:

- Pet は Guild / Adventurer / TamedMonster を攻撃しない。
- Pet は Hostile な Monster を敵対対象にできる。
- Pet は基本的に地上または宿屋周辺に滞在する。
- M12 初期実装では、Pet の同行探索は必須にしない。
- Pet は施設利用の対象になれるが、複雑な予約・支払い・報酬計算は簡易化する。

Save / Load:

- SaveData には Pet の Actor 継続状態を含める。
- Actor 座標は M10 方針どおり保存しない。
- Load 後、Pet は地上の開始位置または宿屋側の初期位置から再開する。
- Pet 化済み Actor が Load 後に Monster として復元されないこと。

責務境界:

- Domain は `PetBehavior` と Faction を表す。
- Pet 化 UseCase は Behavior と Faction の切り替えを一貫して行う。
- AI は `PetBehavior` 型だけで敵対判定せず、Faction 関係を見る。
- View は Pet の見た目・ラベルを表示するだけで、Pet 化判定を持たない。

禁止:

- Monster Actor を削除して新しい Pet Actor を生成し直すこと。
- Pet を Adventurer や GuildStaff の特殊ケースとして実装すること。
- Pet 化で Actor ID、経験値、Inventory、PreferenceSeed を失うこと。
- Pet の敵対判定を Behavior 型だけで決めること。

完了条件:

- [ ] Monster から Pet へ Behavior と Faction を切り替えられる
- [ ] Pet 化後も Actor ID と主要状態が維持される
- [ ] Pet は Guild / Adventurer と敵対しない
- [ ] Pet は Hostile な Monster を敵対対象にできる
- [ ] Save / Load 後も Pet として復元される
- [ ] Pet 初期挙動が PlayMode で確認できる

### BGM

- タイトル画面 BGM
- 地上フィールド BGM（昼 / 夜の切り替え）
- ダンジョン BGM（深さ帯で変化）
- 戦闘 BGM（戦闘開始時にクロスフェード）
- 施設管理画面 BGM
- システムメニュー / ロード画面用の低干渉 BGM または環境音

### 効果音（SFX）

- 武器攻撃音（剣・弓・魔法など武器種別ごと）
- 被弾・死亡効果音
- アイテム取得・売買音
- 施設利用音（宿泊チェックイン、食事提供など）
- UI 操作音（ボタン、画面遷移）
- 環境音（アンビエント：ダンジョンの水音・地上の風音）

### Lighthouse Audio 連携

- Lighthouse の Audio モジュール（`AudioModuleScene`）を利用したサウンド管理
- BGM クロスフェード・音量設定・SE 再生経路の確立
- オーディオ設定画面（マスター音量・BGM 音量・SE 音量）

### パーティクルエフェクト

- 攻撃ヒットエフェクト
- 魔法詠唱・着弾エフェクト
- アイテムドロップ・取得エフェクト
- 施設利用時の演出（宿泊時の★エフェクトなど）

### 画面演出・ポリッシュ

- シーン遷移アニメーションの整備
- 昼夜切り替えの空・照明演出
- カメラシェイク（大型攻撃・ボス登場時）
- ポストプロセス設定（ブルーム・被写界深度など、軽量な設定のみ）

## Milestone 13 へ移動する項目

- バランス調整
- パフォーマンス最適化
- リリースビルド準備

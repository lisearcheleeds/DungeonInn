# Milestone 11.1 Design Review 1 - Codex

対象: `docs/roadmap/milestone11.1-roadmap.md`

確認した guideline:

- `docs/guidelines/self-review-guidelines.md`
- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`

## 総合判断

Milestone 11.1 の方向性は妥当。  
`GameUI` を Screen Space Canvas、`GameHUD` を World-space HUD とする責務分離は、Lighthouse の Scene / ModuleScene 境界ルールと一致している。

ただし、実装時に迷う箇所が残っているため、この roadmap のまま実装に入ると、Scene 間依存・Event DTO・Asset 配置で判断がぶれる可能性がある。  
以下の項目を roadmap に追記してから実装に入るべき。

## 指摘

### 1. Provider interface の登録場所と LifetimeScope 可視性が未定義

重大度: 高

問題:

`GameHUD` が `IActorWorldAnchorProvider` / `IWorldCameraProvider` / `IWorldLayerStateProvider` を読む方針は書かれているが、それらをどの LifetimeScope が登録し、`GameHUD` がどの親 scope から解決するのかが明記されていない。  
Lighthouse では MainScene / ModuleScene / LifetimeScope の所有物を別 Scene が直接参照してはいけないため、ここが曖昧なままだと `GameHUD` から `WorldCameraController` や `MapLayerViewRegistry` を直接 inject する実装に流れやすい。

原因:

roadmap は「抽象 interface を使う」ところまでは定義しているが、抽象の実装所有者、登録 scope、SceneGroup の接続、World 非表示時の解決可否を定義していない。

解決案:

Phase 3 に以下を追記する。

- `WorldLifetimeScope` が `IActorWorldAnchorProvider` / `IWorldCameraProvider` / `IWorldLayerStateProvider` の実装を scoped 登録する。
- `GameHUDLifetimeScope` は上記 interface のみを inject し、World 具象 View / Controller / Registry を inject しない。
- `SceneGroupProvider` で `World` MainScene の ModuleScene として新 `GameHUD` をロードする。
- `GameHUD` が `Title` や他 MainScene と同時にロードされない前提、または provider 不在時の扱いを明記する。通常到達しないなら fallback 生成ではなく例外または assert とする。

根拠となるファイルリスト:

- `docs/roadmap/milestone11.1-roadmap.md`
- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] Phase 3 に provider 実装の所有者が `WorldLifetimeScope` であることが書かれている
- [ ] Phase 3 に `GameHUDLifetimeScope` は provider interface のみへ依存することが書かれている
- [ ] Phase 3 または完了条件に `SceneGroupProvider` の `World -> GameHUD` module 登録更新が含まれている
- [ ] `GameHUD` から World 具象型を inject しないことが静的確認項目に含まれている

### 2. Damage event に World 座標を入れる設計が Application / View 境界を曖昧にしている

重大度: 高

問題:

roadmap では `Damage event subscriber` が「Damage 発生位置、対象 ActorId、damage amount」を受け取ると書かれている。  
この「Damage 発生位置」が View 用の World 座標を意味する場合、Application event に View 表示都合の座標を混ぜることになり、Application Boundary のイベント設計に反する。

原因:

DamageNumber の表示位置を決める責務と、ダメージというゲーム上の事実を通知する責務が同じ event に混ざっている。

解決案:

Damage event はゲーム上の事実に限定する。

- event は `targetActorId`、`damageAmount`、必要なら domain layer position / hit source id など、Domain / Application として意味のある値だけを持つ。
- `GameHUD` は `targetActorId` から `IActorWorldAnchorProvider` を使って表示 anchor を取得する。
- Actor に紐づかないダメージ表示が必要な場合は、別途 `IWorldHudAnchorProvider` または domain position から View 座標へ変換する provider を定義する。
- event DTO に表示用 offset、screen position、Unity world position、Sprite sorting 情報を入れない。

根拠となるファイルリスト:

- `docs/roadmap/milestone11.1-roadmap.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/domain-design-guidelines.md`

完了条件:

- [ ] Phase 3 / Phase 4 から「Damage event が View 用 World 座標を直接持つ」と読める記述が消えている
- [ ] DamageNumber の表示位置決定は `GameHUD` + provider の責務として書かれている
- [ ] event DTO に入れてよい値と入れてはいけない値が明記されている

### 3. `GameHUD3D` の静的確認コマンドが roadmap 自身に一致する

重大度: 中

問題:

Phase 6 の静的確認で `rg -n "GameHUD3D" docs Client/Assets/DungeonInn` を実行すると、少なくとも roadmap 自身の「仮称として残さない」という説明に一致する。  
このままだと、実装完了時の確認で常に失敗するコマンドになり、レビュー担当者が「docs の説明なので許容」と手作業判断する必要が出る。

原因:

実装名の残存確認と、設計ドキュメント上の禁止説明の検索範囲が分離されていない。

解決案:

静的確認を「実装成果物」と「現行設計 docs」に分ける。

- 実装成果物確認: `rg -n "GameHUD3D" Client/Assets/DungeonInn`
- docs 確認: `rg -n "GameHUD3D" docs/design docs/roadmap` を実行し、許容されるのは milestone 11.1 の削除対象説明だけ、と明記する。
- もしくは実装完了後に roadmap 内の説明を `旧仮称` などに言い換えて検索対象から外す。

根拠となるファイルリスト:

- `docs/roadmap/milestone11.1-roadmap.md`
- `docs/guidelines/self-review-guidelines.md`

完了条件:

- [ ] Phase 6 の `GameHUD3D` 検索が実装成果物に対して機械的に 0 件判定できる形になっている
- [ ] docs 内に残してよい `GameHUD3D` 記述の条件が明記されている

### 4. DamageNumber の数字画像アセット設定が不足している

重大度: 中

問題:

DamageNumber の理想構成に `DamageNumberSpriteAtlas` または同等の設定 asset とあるが、添付された 0-9 の数字画像をどこへ配置し、どの Sprite import 設定で分割し、どの Addressable key / factory が所有するかが書かれていない。  
実装者が Editor script、手動設定、Runtime slicing のどれを選ぶべきか判断できない。

原因:

View 構成の責務は書かれているが、Unity asset pipeline と Addressable / Factory の所有者が未定義。

解決案:

Phase 4 に以下を追記する。

- 数字画像の配置先を `Client/Assets/DungeonInn/Runtime/Art/Sprites/UI/DamageNumbers/` などに固定する。
- Sprite は Multiple Sprite として 0-9 に分割し、名前を `DamageNumber_0` ... `DamageNumber_9` に統一する。
- PPU、FilterMode、Pivot、透明背景、SpriteAtlas 化の有無を定義する。
- `GameHUDViewFactory` または専用 loader が `IAssetScope` 経由で読み込む。
- Runtime で Texture2D を分割生成しない。Editor / setup script で import 設定を行う。

根拠となるファイルリスト:

- `docs/roadmap/milestone11.1-roadmap.md`
- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] Phase 4 に数字画像の配置先が書かれている
- [ ] Phase 4 に Sprite 分割名と import 設定が書かれている
- [ ] Phase 4 に Addressable key と `IAssetScope` 経由のロード所有者が書かれている
- [ ] Runtime での Texture slicing を禁止する記述がある

### 5. ActorStatus の表示方式が `SpriteRenderer または TextMeshPro 3D` で曖昧

重大度: 中

問題:

ユーザー意図は「World 内オブジェクトに追従する UI を SpriteRenderer 側へ移す」ことにあるが、roadmap では `ActorStatusView` が `SpriteRenderer または TextMeshPro 3D` を使うとされている。  
この書き方だと、実装者が name label だけ TMP 3D、bar は SpriteRenderer、または全体 TMP 3D など複数解釈を取りうる。

原因:

ActorStatus の最終的な表示部品ごとの技術選択が固定されていない。

解決案:

Phase 5 に ActorStatus の構成を固定する。

- HP bar / state icon / background は SpriteRenderer。
- name label が必要な場合は、当面は非表示にするか、専用 bitmap font / TMP 3D のどちらを採用するかを明記する。
- TMP 3D を使う場合は「Canvas rebuild 回避のために許可する例外」であること、sorting / material / camera-facing の扱いを明記する。
- 今回の milestone で name label を必須にしないなら対象外へ出す。

根拠となるファイルリスト:

- `docs/roadmap/milestone11.1-roadmap.md`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] Phase 5 に ActorStatus の部品ごとの描画方式が明記されている
- [ ] `TextMeshPro 3D` を使う場合の理由と制約が書かれている、または今回 milestone の対象外として明記されている
- [ ] ActorStatus の完了条件が「Canvas ではない」だけでなく、採用した描画方式で確認できる形になっている

## 補足

今回の roadmap は「リネームと責務移動」の方向性自体は正しい。  
問題は、実装担当が現場で決めるには大きすぎる Unity / Lighthouse 境界の判断がいくつか残っている点。  
上記 5 件を roadmap に追記すれば、実装時に設計判断で止まりにくくなる。

# Inn Before the Dungeon — Project CLAUDE.md

## Lighthouse Framework

このプロジェクトはLighthouseフレームワークを使用しています。
**作業開始前に必ず以下を確認してください。**

- ローカルドキュメント: `C:\Users\Lise\Documents\UnityProjects\Lighthouse`
- GitHubドキュメント: https://github.com/lisearcheleeds/Lighthouse/blob/master/README.md
- 詳細リファレンス: https://github.com/lisearcheleeds/Lighthouse/blob/master/Docs/en/readme.md

Lighthouseの規約とクリーンアーキテクチャが矛盾する場合は **Lighthouse側に従う** こと。

### Lighthouse依存ライブラリ（既にインストール済み）
- Unity 6000.0以降 / URP
- VContainer >= 1.17.0（DI）
- UniTask >= 2.5.10（非同期）
- R3 >= 1.3.0（Reactive）
- TextMeshPro 3.x
- Input System >= 1.17.0

---

## プロジェクト概要

ダンジョン入口の村にある宿屋を経営するシミュレーションゲーム。
プレイヤーは宿屋の店長として冒険者を泊め、資金を稼ぎ、宿屋を拡張する。
冒険者は自律的にダンジョンへ挑み、宿屋へ帰還するループを繰り返す。

---

## アーキテクチャ方針

### レイヤー構成（クリーンアーキテクチャ）

```
Assets/
└── Scripts/
    ├── Domain/          # エンティティ・値オブジェクト・リポジトリIF・ドメインサービス
    ├── UseCase/         # アプリケーション層（ユースケース・インターフェース）
    ├── Infrastructure/  # リポジトリ実装・外部サービス・Unity依存
    └── Presentation/    # Lighthouseシーン・View・PresenterまたはViewModel
```

- Domain層はUnity・Lighthouse・外部ライブラリに **一切依存しない**
- DIはVContainerで行い、LifetimeScopeはLighthouseのSceneGroupに合わせて構成する
- 非同期処理はUniTaskで統一する
- Reactiveな状態管理はR3を使用する

### 命名規則

| 種別 | 規則 | 例 |
|------|------|-----|
| インターフェース | `I` prefix | `IInnRepository` |
| ユースケース | `〜UseCase` suffix | `CheckInAdventurerUseCase` |
| ドメインサービス | `〜DomainService` suffix | `SatisfactionDomainService` |
| Presenter | `〜Presenter` suffix | `InnPresenter` |
| SceneTransitionData | `〜TransitionData` suffix | `InnTransitionData` |

---

## 実装フェーズとルール

### ⚠️ フェーズ移行ルール（厳守）

各フェーズはオーナー（Lise）のレビューと承認を得るまで次フェーズに進んではならない。
完了したら「レビューをお願いします」と明示的に報告すること。

### Phase 1: マスタデータ構造設計

ScriptableObjectまたはTSV/JSONで管理するマスタデータの **構造定義のみ** を行う。
実装はしない。以下のマスタデータを設計すること。

- キャラクタースペック（冒険者・店長・店員の基底パラメータ）
- モンスタースペック（階層別強度、行動パターン種別）
- 宿屋設備マスタ（ベッド・部屋タイプ等）
- ダンジョン階層マスタ（出現モンスター、難易度係数）
- 土地拡張マスタ（コスト、拡張単位）

**成果物**: マスタデータ構造定義書（Markdown）
**レビュー完了後、Phase 2へ進む**

---

### Phase 2: Domain設計

Domain層の設計書を作成する。実装はしない。

#### エンティティ設計対象

**キャラクター基底（CharacterBase）**
すべてのキャラクターの基底クラス。以下から派生させること。
- `InnKeeper`（プレイヤー／宿屋店長）
- `Adventurer`（宿屋利用者・ダンジョン挑戦者）
- `InnStaff`（店員、将来拡張）

パラメータ:
- HP / MaxHP
- AttackPower / Defense / MoveSpeed / AttackSpeed
- 性格・状態（将来の行動バリエーションに備えた拡張可能な設計）

**Adventurer（冒険者）追加要素**
- 満足度（Satisfaction）: 0〜100
- 所持金
- 現在の状態（ダンジョン挑戦中 / 宿屋滞在中 / 移動中）
- 視界範囲（Privacy判定に使用）
- 現在装備・インベントリ（将来拡張用のプレースホルダー可）

**Inn（宿屋）**
- 土地サイズ（初期10×10m、5mずつ拡張）
- ベッドリスト
- 在籍中の冒険者リスト
- 資金（Gold）

**Bed（ベッド）**
- 配置座標（Vector3Int）
- 使用中の冒険者参照
- 周囲空きブロック数（配置時に計算）

**DungeonFloor（ダンジョン階層）**
- 階層番号（地下1〜5）
- 迷路データ（グリッド）
- 上り階段・下り階段の座標（各最低1つ）
- 在籍モンスターリスト

**Monster（モンスター）**
- CharacterBaseから派生
- 現在の徘徊状態・次のウェイポイント

#### 満足度ドメインサービス（SatisfactionDomainService）

以下の要因から満足度を算出するロジックを設計すること。

1. ベッド間距離スコア: 最寄りの他者ベッドとの距離
2. 周囲空きブロックスコア: ベッド周囲8方向の空きブロック数
3. プライバシー侵害スコア: 休憩中に他の冒険者の視線に入った回数（視野角・距離で判定）

各スコアから満足度（0〜100）を算出し、チップ有無・基本料金のみを決定する。

#### リポジトリインターフェース

- `IInnRepository`
- `IAdventurerRepository`
- `IDungeonRepository`

#### ゲーム内時間ドメインサービス（GameTimeDomainService）

- リアル20分 = ゲーム内1日
- 時刻（0〜24時）の管理
- 朝・夜のイベントトリガー
- ポーズ / 早送り（x2, x4）/ スロー（x0.5）のTimeScale制御インターフェース

**成果物**: Domain設計書（Markdown、クラス図含む）
**レビュー完了後、Phase 3へ進む**

---

### Phase 3: UseCase設計

ユースケース層の設計書を作成する。実装はしない。

設計対象のユースケース（最低限）:

| ユースケース | 概要 |
|---|---|
| `CheckInAdventurerUseCase` | 冒険者を宿屋に受け入れ、ベッドをアサインする |
| `CheckOutAdventurerUseCase` | 冒険者が出発し、満足度に応じて料金を徴収する |
| `ExpandInnLandUseCase` | 土地を5m×5m単位で購入・拡張する |
| `PlaceBedUseCase` | ベッドを指定座標に配置する（満足度スコアのプレビュー含む） |
| `StartDungeonChallengeUseCase` | 冒険者がダンジョンに出発する |
| `ReturnFromDungeonUseCase` | 冒険者がダンジョンから帰還する（HP・所持金変動） |
| `AdvanceGameTimeUseCase` | ゲーム内時間を進め、朝夜イベントをトリガーする |
| `ChangeTimeScaleUseCase` | ポーズ・早送り・スローを切り替える |
| `GenerateDungeonFloorUseCase` | ゲーム開始時にダンジョン全階層をランダム生成する |

各ユースケースの入出力・例外ケースを設計すること。

**成果物**: UseCase設計書（Markdown）
**レビュー完了後、Phase 4へ進む**

---

### Phase 4: ワールド・カメラ実装（Unity）

Lighthouseのシーン構造に従い実装する。

#### グリッドワールド

- 1ブロック = 1m立方体のグリッドワールド
- デフォルトサイズ: 1000×1000×1000（設定から変更可能）
- **チャンク分割による遅延ロードを必須とする**（全ブロックをメモリに持たない）
- 描画は階層（地上 / 地下1〜5階）単位で切り替える
- 地形は静的（ゲーム開始時確定）、キャラクターは動的オブジェクト

レンダリング方式:
- 現段階: 単純なCubeメッシュ配置（プロトタイプ）
- 将来: グリーディメッシュ等の最適化（Phase実装時にリファクタリング）

#### カメラ

- Minecraftのスペクテイターモード相当の自由カメラ
- WASD + QE（上下）で移動、右クリックドラッグで回転
- 階層切り替えボタンUI（地上 / B1F〜B5F）

#### Lighthouseシーン構成（案）

```
SceneGroup: Game
  MainScene: GameScene（ゲームループ管理）
  ModuleScene: WorldRenderModule（グリッド描画）
  ModuleScene: UIModule（HUD・時刻・資金表示）
  ModuleScene: CameraModule（カメラ制御）
```

**Claude Codeが実施する前に現在のプロジェクト状態を確認し、
既存のLighthouseセットアップと競合しないよう調整すること。**

**成果物**: 動作するワールドビューアー（カメラ操作・階層切り替え）
**レビュー完了後、Phase 5へ進む**

---

### Phase 5: コアループ実装

Phase 2〜3の設計に基づきInfrastructure・Presentationを実装する。

- ダンジョンランダム生成（迷路生成アルゴリズム: 再帰的バックトラッキング推奨）
- 冒険者AI（ダンジョン挑戦→帰還→宿泊の1日サイクル）
- モンスターAI（階層内徘徊）
- 宿屋経営（チェックイン・アウト・料金・満足度）
- 土地拡張UI

**AIの拡張性要件**:
将来的にパーティ編成・AI挙動カスタマイズを追加できるよう、
ステートマシンまたはBehavior Treeベースの設計にすること。
現段階では単純な1日サイクルのみ実装すれば良い。

---

## ワールド仕様

| 項目 | 値 |
|---|---|
| グリッド単位 | 1m立方体 |
| デフォルトマップサイズ | 1000×1000×1000 |
| 地上 | Y=0を地表とする |
| ダンジョン | 地下1〜5階（B1F〜B5F） |
| 宿屋初期土地 | 10m×10m |
| 土地拡張単位 | 縦横5m（購入制） |

---

## ゲーム時間仕様

| 項目 | 値 |
|---|---|
| 1ゲーム日 | リアル20分 |
| 1日の時刻 | 0〜24時 |
| 冒険者の基本サイクル | 朝出発・夕帰還・夜宿泊 |
| TimeScale制御 | ポーズ / x0.5 / x1 / x2 / x4 |

---

## 満足度・宿泊料金仕様

| 満足度 | 支払い |
|---|---|
| 低（不満） | 基本料金のみ |
| 高（満足） | 基本料金 + チップ |

満足度決定要因:
1. ベッド間距離（近すぎると減点）
2. ベッド周囲の空きブロック数（少ないと減点）
3. 休憩中に他の冒険者の視線に晒された回数（多いと減点）

---

## 実装してはいけないこと（スコープ外）

- セーブ・ロード（プロトタイプフェーズでは不要）
- グリーディメッシュ最適化（Phase 4では単純Cubeで可）
- パーティ編成（AI拡張性の設計は行うが実装しない）
- 多言語対応（Lighthouseの仕組みは使えるが現段階では不要）

---

## 作業の進め方

1. 各Phaseの **設計書を先に作成し** 、オーナーのレビューを待つ
2. 「レビューをお願いします」と明示的に伝えること
3. レビュー承認を受けるまで次のPhaseに着手しない
4. 実装フェーズでは **コンパイルが通る状態を常に維持** する
5. TODOコメントを残す場合は `// TODO(phase):` の形式で残す

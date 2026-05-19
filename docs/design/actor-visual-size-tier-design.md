# Actor 表示サイズ Tier 設計

## 目的

Actor のスプライトは透明余白を含む場合があり、元画像の解像度もアセットごとに異なる。そのため、Runtime の表示サイズを画像のピクセル高だけから推測しない。

`ActorVisualSizeTier` は、スプライトのキャンバス全体をワールド上で何メートルとして表示するかを定義する。これは Actor スプライト用の View 側表示契約であり、Domain の Actor サイズ・当たり判定・ゲーム上の占有サイズではない。

## ルール

- `ActorVisualSizeTier` は World View 層に属する。
- tier はスプライトキャンバス全体の表示高さを表す。
- スプライト解像度の違いはピクセル密度だけを変える。同じ tier であれば、512px のスプライトと 1024px のスプライトは同じワールド高さで表示する。
- キャンバス内の透明余白はアセット側の責任とする。Runtime は不透明ピクセル範囲ではなくキャンバス全体をスケールする。
- Actor の接地位置はスプライト pivot を使う。現在の Actor スプライトは bottom-center pivot を使うため、既定の Actor Y オフセットは `0f` とする。
- 大型モンスターは Actor ごとの magic Y offset や asset ごとの `localScale` ではなく、大きい tier を使って表現する。

## 現在の Tier

| Tier | キャンバス高さ | 用途 |
|---|---:|---|
| `ActorVisualSizeTier.AdventurerS` | 1.5m | 冒険者サイズの人型スプライト |
| `ActorVisualSizeTier.MonsterS` | 1.2m | ゴブリンや小型ペットなどの小型モンスター |
| `ActorVisualSizeTier.MonsterL` | 5.0m | 将来のドラゴンなどの大型モンスター |

## 実装メモ

- `ActorSpriteVisualConfigSO.Entry` が Actor behavior ごとの visual size tier を保持する。
- `ActorView` は `targetCanvasHeightMeters / sprite.bounds.size.y` で `localScale` を計算する。
- `sprite.bounds.size.y` は透明なキャンバス領域も含む。この設計ではそれを意図的に利用する。
- Domain の `Actor` にはこの値を持たせない。当たり判定・射程・戦闘上の占有範囲・移動経路上のサイズが必要になった場合は、別の Domain 概念として導入する。

## Milestone 7: ビルボード回転

Actor スプライトは isometric camera angle 前提で制作する。Milestone 7 では、`ActorView` をカメラ yaw だけではなくカメラ回転全体に正対させるフルビルボード表示へ変更する。

### 前提

- World camera は isometric pitch 固定とする。
- Camera yaw は 90度単位で回転する。
- Actor スプライトはその isometric view angle に合わせて制作する。
- Actor の当たり判定・移動経路・ゲーム上のサイズは、スプライト平面の回転とは分離する。

### ルール

- ビルボード回転は View 層の責務とする。
- `CurrentYawDegrees` はアニメーション方向の選択に引き続き使用する。
- ビルボード表示用には、カメラの `Quaternion` を yaw とは別に公開する。
- `ActorView` は `SetBillboardRotation(Quaternion cameraRotation)` のような API でカメラ回転を受け取る。
- 固定 isometric camera 前提であれば、スプライト平面がワールド上で完全な直立板でなくなってもフルビルボードを許容する。

### Milestone 7 実装対象

- `WorldCameraController` から現在のカメラ回転を公開する。
- `ActorView.SetRotationY(float degrees)` をフルビルボード回転に置き換える、または併用できる API を追加する。
- アニメーション方向の選択はフルビルボード回転ではなく、camera yaw と Actor の移動方向に基づいて行う。
- PlayMode で、カメラを 90度単位で回転しても Actor が見た目上カメラに正対することを確認する。

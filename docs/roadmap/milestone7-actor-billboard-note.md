# Milestone 7 Actor ビルボードメモ

Milestone 7 では、Actor スプライトをフルビルボード表示にする。

## 前提

- World camera は isometric pitch 固定とする。
- Camera yaw は 90度単位で回転する。
- Actor スプライトはその isometric camera angle に合わせて制作する。
- スプライトのビルボード回転は View 層の表示責務に限定する。

## 実装スコープ

- `WorldCameraController` から現在のカメラ回転を公開する。
- `ActorView` にカメラ回転全体を受け取る API を追加する。例: `SetBillboardRotation(Quaternion cameraRotation)`。
- スプライト平面のビルボード表示にはカメラ回転全体を使う。
- アニメーション方向の選択は camera yaw と Actor の移動方向から決める。
- ビルボード状態、表示サイズ、当たり判定サイズ、移動経路上の占有範囲は Domain の `Actor` に追加しない。

## 参照

詳細は `docs/design/actor-visual-size-tier-design.md` の `Milestone 7: ビルボード回転` を参照する。

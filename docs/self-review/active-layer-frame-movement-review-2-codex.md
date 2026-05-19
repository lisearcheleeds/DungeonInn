# Active Layer Frame Movement Review 2 - Codex

対象: アクティブ階層の Actor 毎フレーム移動対応後のセルフレビュー

観点: 設計、整合性、パフォーマンス、重複した機能を持つクラス/データクラス、その他統合

## 1. リアルタイム移動の消費時間が「実際に動いた Actor」ではなく「表示中レイヤー」に対して記録される

重要度: 高

問題:

`WorldSimulationOrchestrator.AdvanceFrameAsync()` は、表示中の `RealtimeLayerId` があり、かつ `gameWorldState.Actors.Count` が 1 件以上なら、対象レイヤーに Actor が存在するかどうかに関係なく `AddRealtimeMovedSeconds()` を呼び出している。

その後、同じフレームの schedule tick で Actor が spawn された場合、`AdvanceScheduledActorLifecycleAsync()` は spawn 後の Actor を見てレイヤーごとの schedule 移動時間を差し引く。つまり「その Actor が存在する前のリアルタイム移動時間」まで消費済みとして扱われ、spawn 直後の Actor の schedule 移動が不足する可能性がある。

原因:

リアルタイム移動の差し引き単位が Layer 単位になっているが、記録条件が「その Layer に対して移動処理が実際に Actor を進めたか」ではなく「表示中 Layer で移動処理を呼んだか」になっているため。

解決案:

`AddRealtimeMovedSeconds()` は、対象レイヤーに schedule 移動対象の Actor が存在することを確認してから呼ぶ。より堅牢にするなら、`AdvanceActorLifecycleOrchestrator` が実際に処理した Actor 数または対象 Actor の有無を返す API にする。ただし Runtime API 変更を増やすより、まずは `WorldSimulationOrchestrator` 側で対象レイヤーに Actor がいるかを判定するのが妥当。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/WorldGameLoopEntryPointArchitectureTests.cs`

完了条件:

- [ ] `RealtimeLayerId` に Actor がいない場合は `realtimeMovedSecondsByLayer` が増えない
- [ ] schedule tick と同じフレームで spawn された Actor が、spawn 前のリアルタイム移動時間で schedule 移動を減算されない
- [ ] 上記を検証する EditMode test が追加されている
- [ ] `uloop.cmd compile --project-path Client` が成功する
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する

## 2. `ActorLifecycleAdvanceScope.Except()` が現在の実装で未使用の public API になっている

重要度: 中

問題:

`ActorLifecycleAdvanceScope.Except()` は当初の active layer 除外方式では使われていたが、現在の実装では layer ごとの残り schedule 秒数を計算して `Only()` で処理する方式に変わったため、Runtime 側にも Tests 側にも呼び出しがない。未使用の public API が残ると、今後の責務判断や利用範囲が曖昧になる。

原因:

レビュー対応の途中で方式が変わったが、不要になった scope variant が削除されていない。

解決案:

`Except()` と `excludeLayerId` を削除し、`ActorLifecycleAdvanceScope` は `All` と `Only` に絞る。将来、除外指定が必要になった時点で再追加する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/ActorLifecycleAdvanceScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdvanceActorLifecycleOrchestrator.cs`

完了条件:

- [ ] `ActorLifecycleAdvanceScope.Except()` が削除されている
- [ ] `ActorLifecycleAdvanceScope` が現在の production caller に必要な概念だけを公開している
- [ ] `rg "ActorLifecycleAdvanceScope\\.Except|Except\\(" Client/Assets/DungeonInn/Runtime/Scripts Client/Assets/DungeonInn/Tests/EditMode` で不要な呼び出しが残っていない
- [ ] `uloop.cmd compile --project-path Client` が成功する
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する

## 3. レイヤー別リアルタイム消費の異常系テストが不足している

重要度: 中

問題:

既存の追加テストは「同じ Actor がリアルタイム移動と schedule 移動で二重に進まない」ことを確認している。一方で、対象レイヤーに Actor がいない場合、schedule tick と同時に spawn される場合、表示レイヤー切替を挟む場合の検証がない。

原因:

初回修正の主目的が二重移動の再現ケースに寄っており、layer 単位の消費時間管理が持つ境界条件をテスト化できていない。

解決案:

少なくとも「対象レイヤーに Actor がいない状態で realtime 秒が加算されず、同フレーム spawn Actor の schedule 移動が減算されない」ケースを追加する。レイヤー切替ケースは、今回のバグ修正後も不安が残る場合に追加する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Tests/EditMode/WorldGameLoopEntryPointArchitectureTests.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`

完了条件:

- [ ] spawn 前 realtime 秒の誤消費を検出できるテストがある
- [ ] テストが実装の内部 Dictionary ではなく Actor の移動距離で結果を検証している
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功する

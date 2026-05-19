# Active Layer Frame Movement Self Review 1

## 1. [整合-1] schedule tick 実行中に表示中レイヤーの毎フレーム移動が止まる

重大度: 高

問題:

`WorldGameLoopEntryPoint.Update()` は `isExecuting` が true の間、`WorldSimulationOrchestrator.AdvanceFrameAsync()` を呼ばない。今回の表示中レイヤー毎フレーム移動は `AdvanceFrameAsync()` 内の `ActorLifecycleAdvanceScope.Only(activeLayer)` に入っているため、schedule tick 側の処理が `YieldIfBudgetExhaustedAsync()` で複数フレームにまたがると、その間は表示中 Actor の移動 Simulation も止まる。

原因:

表示中レイヤーの高頻度移動と、schedule tick の重い非表示/低頻度処理が同じ `AdvanceFrameAsync()` 実行単位に入っている。`WorldGameLoopEntryPoint` の overlap 防止フラグは正しいが、結果として高頻度更新も同じ lock に巻き込まれている。

解決案:

表示中 Actor の frame movement を schedule tick worker から分離する。例として、`IWorldSimulationOrchestrator` に `AdvanceRealtimeFrameAsync` と `AdvanceScheduledFrameAsync` を分ける、または `WorldSimulationOrchestrator.AdvanceFrameAsync()` 内で schedule work を別の継続状態として持ち、active layer movement だけは毎フレーム必ず先に進める。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`

完了条件:

- [ ] schedule tick work が複数フレームにまたがっても、表示中レイヤーの lifecycle movement が毎フレーム実行される。
- [ ] `isExecuting` または後継の実行制御が schedule work だけを保護し、realtime movement を止めない。
- [ ] EditMode test で「schedule work 実行中でも active layer frame movement が進む」ことを検証する。
- [ ] `uloop.cmd compile --project-path Client` と EditMode test が成功する。

## 2. [整合-2] レイヤー切替時に active 移動時間と schedule 移動時間が二重計上され得る

重大度: 高

問題:

現在は毎フレームの表示中レイヤー移動で `frameDeltaGameSeconds` を使い、schedule tick 到達時に「その時点で active ではないレイヤー」へ `AdvancedScheduleTicks` 分をまとめて渡している。schedule tick の途中で表示レイヤーを切り替えると、以前 active だったレイヤーは既に frame delta 分だけ進んでいるにもかかわらず、tick 到達時に inactive なら `AdvancedScheduleTicks` 分も進むため、同じ時間帯の移動が二重に入る。

原因:

`ActorLifecycleAdvanceScope.Only/Except` は対象レイヤーの選別だけを行い、レイヤーごとの「この schedule interval 内ですでに frame movement として消費した game seconds」を持たない。`AdvancedScheduleTicks` は全レイヤー共通のまとめ時間として扱われている。

解決案:

レイヤー別に realtime 消費済み秒数を追跡し、schedule tick 側では `AdvancedScheduleTicks - consumedRealtimeSecondsForLayer` を渡す。あるいは、visible/invisible を問わず lifecycle movement は毎フレーム進め、schedule tick から lifecycle movement を完全に外す。その場合は非表示階層の負荷対策として actor budget / layer budget を別途設ける。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/ActorLifecycleAdvanceScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldFrameAdvanceRequest.cs`

完了条件:

- [ ] レイヤー切替を挟んでも、同じゲーム時間帯の actor movement が二重に進まない。
- [ ] active/inactive の切替履歴を含む EditMode test が追加されている。
- [ ] schedule tick 到達時の移動量が、レイヤーごとの未消費時間に基づいている。

## 3. [設計-1] Application の request に View 由来の `ActiveLayerId` がそのまま入っている

重大度: 中

問題:

`WorldFrameAdvanceRequest.ActiveLayerId` は現在表示中の階層という View の都合を Application request に直接持ち込んでいる。依存方向として View 型は入っていないため即時の Clean Architecture 違反ではないが、Application 側の意味としては「表示中」ではなく「高頻度更新対象」の方が責務に合う。

原因:

今回の要件が「現在表示しているアクティブな階層」だったため、View の語彙をそのまま request 名に使っている。

解決案:

`ActiveLayerId` を `RealtimeLayerId` / `HighFrequencyLayerId` / `FrameAdvanceLayerId` など Application 側の更新ポリシー名に改める。将来、カメラ外でも高頻度更新したい階層や、戦闘中だけ高頻度にする対象が出ても意味が崩れない名前にする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldFrameAdvanceRequest.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`

完了条件:

- [ ] Application request の名前が View 表示状態ではなく Simulation 更新ポリシーを表す。
- [ ] `MapLayerViewRegistry.ActiveLayerId` から request への変換は View 層または entry point 境界で完結している。

## 4. [テスト-1] 通常移動は検証できているが、切替・schedule overlap ケースが未検証

重大度: 中

問題:

追加テスト `ActorLifecycleAdvanceScopeMovesOnlyIncludedLayer` は `Only(layer)` の選別を検証しているが、今回の実利用で重要な「schedule tick と frame movement の混在」「active layer 切替」「schedule work が複数フレームにまたがる」ケースを検証していない。

原因:

スコープ単体の検証に寄っており、`WorldSimulationOrchestrator` と `WorldGameLoopEntryPoint` の実行タイミングまで含めたテストが不足している。

解決案:

`WorldSimulationOrchestrator` レベルで、同じ actor が active frame movement と schedule movement の両方で同一時間帯に進まないことを検証する。加えて、schedule work が yield している間も realtime movement が進むことを検証できるテストダブルを用意する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Tests/EditMode/GameLoopTests.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/WorldGameLoopEntryPointArchitectureTests.cs`

完了条件:

- [ ] active/inactive 切替を含む移動量テストがある。
- [ ] schedule overlap 中の realtime movement 継続テストがある。
- [ ] 既存の `ActorLifecycleAdvanceScopeMovesOnlyIncludedLayer` が、より上位の挙動検証に置き換わるか補強されている。

## 5. [良好] レイヤー選別の責務分離自体は拡張しやすい

重大度: なし

問題:

なし。

原因:

`ActorLifecycleAdvanceScope` により、`All / Only / Except` の対象選別は `AdvanceActorLifecycleOrchestrator` から独立した値として表現できている。将来、複数レイヤー指定や predicate 化へ拡張する余地もある。

解決案:

上記 1-4 を解決したうえで、`ActorLifecycleAdvanceScope` は維持してよい。ただし名前は「movement scope」なのか「lifecycle scope」なのか、最終設計に合わせて再確認する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/ActorLifecycleAdvanceScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdvanceActorLifecycleOrchestrator.cs`

完了条件:

- [ ] 二重計上と schedule overlap が解決された後も、対象選別ロジックが一箇所に集約されている。

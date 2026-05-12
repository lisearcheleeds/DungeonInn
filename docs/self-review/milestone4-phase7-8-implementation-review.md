# Milestone 4 Phase 7 / 8 Implementation Review

Implementation date: 2026-05-12

## Review Result

- Phase 7 query boundary separation is in place through `IGameWorldStateReader` / `IGameWorldStateWriter`.
- UseCase-to-UseCase direct calls have been reduced to Orchestrator / Service composition.
- Defeat flow is split as intended:
  - `ActorDefeatOrchestrator` handles experience, drops, and processing order.
  - `CombatDefeatResolver` handles actor removal, combat target clearing, and `ActorDefeated` publishing.
- Phase 8 time design uses `GameClock.TotalScheduleTick` as the canonical state. Day and tick-of-day values are derived through `GameTimeUtility`.
- Daily reports are persisted in `InnDailyReportStore`; current status is exposed through `GetInnEconomyStatusUseCase`, and saved reports through `GetInnEconomyReportUseCase`.
- Automated item selling now uses `PricePolicy.CalculatePurchasePrice`, guild inventory, guild gold, trade facilities, and `ExchangeTransaction` records instead of increasing actor gold directly.
- Initial guild creation now includes `GeneralStore` and `EquipmentShop` facilities in addition to the inn.

## Verification

- `uloop.cmd compile --project-path Client`: passed
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: passed, 210 tests
- Hard-gate scan on added diff: no added forbidden API usage

## Notes

- The full hard-gate scan still reports pre-existing framework/runtime matches such as `UniTask<...>` and existing loader/scene code. The added diff does not introduce prohibited APIs.

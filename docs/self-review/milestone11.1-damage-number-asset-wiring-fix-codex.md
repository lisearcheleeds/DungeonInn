# Milestone 11.1 DamageNumber Asset Wiring Fix - Codex

## 2026-06-03

- Cause: `DamageDigits.png` existed, but its importer was not configured as Sprite/Multiple and the generated `DamageNumberView.prefab` did not serialize the 10 digit sub-sprites. `DigitTemplate` also kept the dummy effect sprite, so runtime clones had no real digit sprite source.
- Fix: `SetupGameHUDWorldSpaceAssets.Run()` now configures `DamageDigits.png` as 10 sub-sprites named `DamageDigit_0` through `DamageDigit_9`, each 30x50 pixels from the 300x50 atlas.
- Fix: `DamageNumberView.prefab` now serializes `digitSprites[0..9]` from `DamageDigits.png`, and `DigitTemplate.sprite` is `DamageDigit_0`.
- Fix: `DungeonInn.Editor.asmdef` references `Unity.2D.Sprite.Editor` so the editor setup uses the current Sprite Editor data provider API.
- Test: `VisualAssetPipelineTests.DamageNumberViewPrefabReferencesDigitSprites` now fails if any digit sprite is null, named incorrectly, or sourced from anything other than `DamageDigits.png`.

## Verification

- `uloop.cmd compile --project-path Client`
  - Success: true
  - ErrorCount: 0
  - WarningCount: 0
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
  - Success: true
  - TestCount: 368
  - PassedCount: 368
  - FailedCount: 0
- Play verification
  - Opened `Launcher.unity`, entered Play, invoked `NewGameButton` and `StartButton` via `LHButton.onClick`.
  - Confirmed `[World] GameWorldState initialized. Facilities=4 DungeonFloors=1 Actors=0`.
  - Waited 30 seconds after the World initialization log.
  - Stopped Play and confirmed `uloop.cmd get-logs --project-path Client --log-type Error --max-count 20` returned 0 logs.

# Milestone 11.1 Implementation Self Review 3 - Codex

## Scope

- Reviewed the current uncommitted working tree shown by `git status --short`.
- Focused on whether the GameUI / GameHUD split is the ideal design, whether duplicate data/classes remain, and whether compatibility-only code or obsolete data remains.
- No fixes were applied in this review.

## Guideline Check

- Confirmed the project guidelines under `docs/guidelines/` before review:
  - `lighthouse-patterns.md`
  - `coding-rules.md`
  - `domain-design-guidelines.md`
  - `application-boundary-guidelines.md`
  - `implementation-quality-guidelines.md`
  - `debugging-policy.md`
  - `self-review-guidelines.md`

## Findings

### 1. High - `VisualAssetSetup` still contains obsolete Canvas setup for world-space HUD prefabs

**Evidence**

- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs:163` still calls `CreateOrLoadActorStatusViewPrefab()`.
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs:167` and `:192` still call `CreateOrLoadDamageNumberViewPrefab()`.
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs:348` creates `ActorStatusView` with `RectTransform`.
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs:457` adds/uses `CanvasGroup` for `DamageNumberView`.
- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs:499`, `:500`, and `:503` assign obsolete serialized fields `rootRectTransform`, `canvasGroup`, and `digitAtlas`.

**Why this is a problem**

`GameHUD` is now a world-space `SpriteRenderer` scene. The new `SetupGameHUDWorldSpaceAssets` path creates the correct `ActorStatusView` and `DamageNumberView` prefabs, but `VisualAssetSetup` still contains the old Canvas-era generation path.

This creates two sources of truth. If `VisualAssetSetup.Run()` or the damage-number setup menu is executed after the prefab is missing or recreated, it can rebuild incompatible Canvas-style prefabs or fail when obsolete serialized properties are not found. This is not just stale documentation; it is executable editor code that conflicts with the ideal design.

**Ideal resolution**

Remove the old Canvas setup from `VisualAssetSetup`, or make it delegate to the same world-space setup logic used by `SetupGameHUDWorldSpaceAssets`. There should be exactly one setup source for `GameHUD/ActorStatusView` and `GameHUD/DamageNumberView`, and it must create `SpriteRenderer`-based prefabs only.

**Completion criteria**

- `VisualAssetSetup` no longer creates `ActorStatusView` or `DamageNumberView` using `RectTransform`, `CanvasGroup`, `Image`, or `digitAtlas`.
- Running the visual asset setup path cannot recreate Canvas-style GameHUD prefabs.
- Tests or editor validation verify that generated GameHUD prefabs have the required `SpriteRenderer` references.

### 2. Medium - GameHUD pools still contain invisible fallback views

**Evidence**

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ActorStatusViewPool.cs:109` falls back to `CreateFallbackView()`.
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ActorStatusViewPool.cs:116` creates `ActorStatusView_Fallback`.
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/DamageNumberViewPool.cs:86` falls back to `CreateFallbackView()`.
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/DamageNumberViewPool.cs:93` creates `DamageNumberView_Fallback`.
- `Client/Assets/DungeonInn/Tests/EditMode/WorldMapLayerViewDataTests.cs:556` constructs `GameHUDViewFactory` with `ThrowingAssetManager`, which allows the actor-status presenter test to pass through the fallback route.

**Why this is a problem**

The fallback objects do not wire the required `SpriteRenderer` references, so they are not visually functional. They preserve runtime continuity when Addressables or prefabs are broken, but that is exactly the kind of compatibility-only path the current milestone is trying to remove.

This also weakens tests: a GameHUD presenter test can pass even when prefab loading is impossible.

**Ideal resolution**

Treat missing `GameHUD` prefabs as a configuration error. Remove the invisible fallback path and fail with a clear log/exception, or build a fully functional placeholder with serialized-equivalent renderer wiring. The simpler ideal design is to require the prefab and make setup failures visible.

**Completion criteria**

- `ActorStatusViewPool` and `DamageNumberViewPool` do not create invisible runtime fallback objects.
- Tests no longer rely on `ThrowingAssetManager` for a path that is supposed to render GameHUD views.
- Missing prefab behavior is explicitly verified as either a hard failure or a functional placeholder.

### 3. Medium - Generated `.g.cs` changes are still not proven to be generated output

**Evidence**

- `Client/Assets/DungeonInn/Runtime/Scripts/LighthouseGenerated/DungeonInnModuleSceneId.g.cs` is modified.
- `Client/Assets/DungeonInn/Runtime/Scripts/LighthouseGenerated/ScreenStackEntityFactory.g.cs` is modified.
- The previous review already flagged this, and the current diff still contains these generated files.

**Why this is a problem**

Scene rename and namespace changes naturally require generated code updates, but generated files must be regenerated from the source/generator path. Manual edits to generated files would be a hard-gate violation and would make future generation overwrite the fix.

**Ideal resolution**

Run the Lighthouse generation path and confirm that these `.g.cs` files remain exactly as shown. If they were manually edited, update the generator input/source and regenerate.

**Completion criteria**

- The generation command or Unity generation flow is run.
- The generated file diff is confirmed to be reproducible.
- Review log records that the generated files were not hand-maintained compatibility edits.

### 4. Medium - Design docs still conflict with the implemented GameUI / GameHUD split

**Evidence**

- `docs/design/scene-design.md:226-227` lists `GameHUDLifetimeScope -> Canvas HUD Presenter / Pool / ViewFactory`.
- `docs/design/scene-design.md:246-247` uses `GameHUD/ActorStatus3DView` and `GameHUD/DamageNumber3DView`, while runtime uses `GameHUD/ActorStatusView` and `GameHUD/DamageNumberView`.
- `docs/design/scene-design.md:304` says `画面固定なら GameHUD。`, which should be `GameUI`.
- `docs/roadmap/milestone11.1-roadmap.md:272`, `:276`, `:592`, `:595-597`, and `:820` describe digit-per-address assets under `DamageNumbers`, while the implementation uses one `DamageDigits.png` atlas imported as multiple sprites and serialized into `DamageNumberView.prefab`.

**Why this is a problem**

The code split is mostly correct, but the design documents still contain contradictory names and asset-loading models. Future implementation work could reasonably follow the docs and reintroduce a second digit catalog/addressable path or put fixed UI back under `GameHUD`.

**Ideal resolution**

Choose one ideal asset model and make docs match it. The current implementation's single atlas plus prefab-serialized digit sprites is simpler and avoids unnecessary addressable digit assets, so it is a reasonable ideal design if the docs are corrected to match.

**Completion criteria**

- `scene-design.md` consistently says fixed screen-space UI belongs to `GameUI`.
- GameHUD addressable examples match actual keys: `GameHUD/ActorStatusView` and `GameHUD/DamageNumberView`.
- Roadmap asset guidance either adopts the current atlas-prefab model or the implementation is changed to the documented digit-address model. Both should not coexist.

## Positive Findings

- Runtime `GameHUD` no longer contains Canvas/UI dependencies in the reviewed code path. Search for `UnityEngine.UI`, `Canvas`, `RectTransform`, `Image`, `TextMeshProUGUI`, `LHButton`, `Addressables.Load`, and `Resources.Load` under `Runtime/Scripts/View/Scene/ModuleScene/GameHUD` returned no matches.
- Runtime `GameUI` no longer contains `ActorStatusView`, `DamageNumberView`, `DamageNumberPresenter`, `ActorStatusViewPool`, `DamageNumberViewPool`, or `SpriteRenderer` classes.
- Fixed Canvas UI prefabs were moved to `Runtime/Prefab/GameUI`; `Runtime/Prefab/GameHUD` is now reserved for world-space HUD prefabs.
- `ActorEffectIconSpriteCatalog` is separated from `GameHUDViewFactory`, so the factory no longer owns effect-icon data.
- `ActorStatusViewPool` no longer has the empty `IInitializable` implementation that was previously flagged.
- `DamageNumberAnimation` now uses `LocalOffset`, which matches world/local HUD positioning better than the old anchored-position terminology.
- No duplicate GameUI/GameHUD data class pair was found in the current split. Existing `Data` and `ViewData` classes under GameUI still represent separate window input and rendered view contracts, not duplicate compatibility models.

## Review Conclusion

The main architectural split is moving in the right direction: `GameUI` owns fixed Canvas UI and `GameHUD` owns world-space SpriteRenderer HUD. However, the implementation is not yet clean enough to call ideal. The remaining blockers are not large runtime architecture issues; they are stale executable editor setup, invisible fallback behavior, unverified generated files, and conflicting docs.

The highest-priority fix is `VisualAssetSetup`, because it can actively recreate the wrong prefab shape and undo the GameHUD world-space design.

## Verification

- This was a review-only pass. No code or prefab fixes were applied.
- Compile, EditMode tests, and 30-second Play Mode validation were not rerun during this review because the working tree was not changed by this review.

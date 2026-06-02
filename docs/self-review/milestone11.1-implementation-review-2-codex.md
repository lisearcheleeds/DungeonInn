# Milestone 11.1 Implementation Self Review 2 - Codex

## 1. GameUI prefab files still live under the GameHUD prefab directory

Severity: High

Problem:

The scene/script responsibility split is now `GameUI` for fixed Canvas UI and `GameHUD` for world-space HUD. However, fixed UI prefabs such as `WorldHudView`, `SelectedActorInspectorView`, `PlayerEventLogView`, `InnStatusPanelView`, `MinimapView`, and ScreenStack windows are still generated and stored under `Assets/DungeonInn/Runtime/Prefab/GameHUD`.

Cause:

The rename moved runtime namespaces and scene responsibility, but the asset directory constants were only partially updated. This leaves a compatibility-shaped physical layout where GameUI content is kept in the old GameHUD location while only Addressable addresses use `GameUI/...`.

Resolution:

Move fixed UI prefabs to `Assets/DungeonInn/Runtime/Prefab/GameUI`, keep only world-space HUD prefabs under `Assets/DungeonInn/Runtime/Prefab/GameHUD`, and update editor setup scripts and Addressable setup accordingly.

Evidence:

- `Client/Assets/DungeonInn/Editor/VisualAssetSetup.cs`
- `Client/Assets/DungeonInn/Editor/OneShot/SetupWorldHudViewPrefab.cs`
- `Client/Assets/DungeonInn/Editor/OneShot/SetupSelectedActorInspectorViewPrefab.cs`
- `Client/Assets/DungeonInn/Editor/OneShot/SetupPlayerEventLogViewPrefab.cs`
- `Client/Assets/DungeonInn/Editor/OneShot/SetupInnStatusPanelViewPrefab.cs`
- `Client/Assets/DungeonInn/Editor/OneShot/SetupMinimapViewPrefab.cs`

Completion criteria:

- [ ] `Runtime/Prefab/GameHUD` contains only world-space HUD prefabs such as `ActorStatusView` and `DamageNumberView`.
- [ ] Fixed UI and ScreenStack prefabs are under `Runtime/Prefab/GameUI`.
- [ ] Addressable addresses remain responsibility-based: `GameUI/...` and `GameHUD/...`.
- [ ] Editor one-shot/setup code no longer routes fixed UI prefabs through `GameHUDPrefabDirectory`.

## 2. Actor effect icon catalog is hardcoded and owned by the view factory

Severity: Medium

Problem:

`ActorEffectIconSpriteCatalog` creates `Texture2D` and `Sprite` instances at runtime and resolves visuals by hardcoded `actorEffectMasterId` colors. `GameHUDViewFactory` constructs and owns this catalog, and `WorldActorStatusPresenter` reaches through the factory to use it.

Cause:

The implementation was acceptable as a placeholder for replacing Canvas icons, but it is not an ideal design. Effect visuals should be data/asset driven, and a factory that loads view prefabs should not also be the owner of an icon catalog.

Resolution:

Introduce an asset-driven icon catalog or visual definition for actor effects, register it separately in DI, and inject it directly where icon sprites are applied. Remove hardcoded master-id color mapping and runtime texture generation from production runtime.

Evidence:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ActorEffectIconSpriteCatalog.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GameHUDViewFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/WorldActorStatusPresenter.cs`

Completion criteria:

- [ ] Actor effect icon sprite resolution is asset-driven.
- [ ] `GameHUDViewFactory` only owns GameHUD view prefab loading.
- [ ] `WorldActorStatusPresenter` does not access icon data through the view factory.
- [ ] Production runtime no longer creates placeholder effect textures for known effect IDs.

## 3. Missing GameHUD prefabs produce invisible fallback views

Severity: Medium

Problem:

`ActorStatusViewPool` and `DamageNumberViewPool` create fallback GameObjects when prefab loading fails, but those fallback objects do not wire required `SpriteRenderer` references. This silently produces invisible HUD elements instead of either a working placeholder or a clear failure.

Cause:

The fallback path preserves runtime continuity but does not satisfy the responsibility of the view being spawned. This can hide Addressable or prefab setup failures during verification.

Resolution:

Either build functional placeholder renderers in the fallback path or remove the fallback and fail loudly with traceable logs when required GameHUD prefabs are missing.

Evidence:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ActorStatusViewPool.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/DamageNumberViewPool.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GameHUDViewFactory.cs`

Completion criteria:

- [ ] Missing GameHUD prefab state is observable and not silently invisible.
- [ ] If fallback remains, fallback `ActorStatusView` and `DamageNumberView` are visually functional.
- [ ] Verification covers missing prefab/fallback behavior or hard failure behavior.

## 4. Generated `.g.cs` files are in the diff

Severity: Medium

Problem:

`LighthouseGenerated` `.g.cs` files changed in the working tree. Generated output changes are expected after scene and namespace changes, but the guideline forbids manual edits to generated files.

Cause:

The scene rename and ScreenStack namespace move require generated code updates. Before commit, this diff must be proven to be generator output, not a manual compatibility edit.

Resolution:

Run the Lighthouse/Unity generation path and confirm these generated files remain exactly as shown. If they were manually edited, update the generation source and regenerate instead.

Evidence:

- `Client/Assets/DungeonInn/Runtime/Scripts/LighthouseGenerated/DungeonInnModuleSceneId.g.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/LighthouseGenerated/ScreenStackEntityFactory.g.cs`
- `docs/guidelines/lighthouse-patterns.md`

Completion criteria:

- [ ] Generated files are reproduced by the generator.
- [ ] No manual-only edits remain under `LighthouseGenerated`.

## 5. Empty lifecycle hook remains in ActorStatusViewPool

Severity: Low

Problem:

`ActorStatusViewPool` implements `IInitializable`, but `Initialize()` is empty.

Cause:

This appears to be leftover lifecycle structure from an earlier version, not current behavior.

Resolution:

Remove `IInitializable` from the pool unless initialization work is actually needed.

Evidence:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ActorStatusViewPool.cs`

Completion criteria:

- [ ] `ActorStatusViewPool` has no empty lifecycle interface.
- [ ] DI registration still resolves the pool and presenter correctly.

## 6. DamageNumberAnimation still uses Canvas terminology

Severity: Low

Problem:

`DamageNumberAnimationSample.AnchoredPosition` uses Canvas terminology even though the implementation is now world-space SpriteRenderer HUD.

Cause:

The animation model was carried over from a Canvas-oriented mental model.

Resolution:

Rename the value to `LocalOffset` or `WorldOffset`, depending on the intended coordinate meaning, and update tests.

Evidence:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/DamageNumberAnimation.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/DamageNumberView.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/DamageNumberAnimationTests.cs`

Completion criteria:

- [ ] Naming reflects world-space HUD behavior.
- [ ] Tests assert the renamed concept.

## Non-findings

- No old `GameHud`, `ActorHUD`, `HUDCanvas`, or screen-position-provider names were found in runtime/editor/tests/design search results.
- No newly duplicated data class was found in the GameHUD/GameUI split. ScreenStack `*WindowData` and `*WindowViewData` still represent different contracts.
- `IDamageNumberViewSpawner` is an acceptable boundary between presenter/event handling and pooled view spawning; it does not appear to exist only for backward compatibility.
- `WorldActorWorldAnchorProvider` no longer culls by camera visibility, which matches the GameHUD responsibility of following active actors rather than deciding screen-space visibility.

## Verification referenced

- Compile: previously passed with `uloop.cmd compile --project-path Client`.
- EditMode tests: previously passed, 357/357.
- Play mode: previously ran for 30 seconds after `[World] GameWorldState initialized` with no error logs.
- Actor status display validation: previously logged `ActorStatusView count=4 active=2`.

## Follow-up Fix 2026-06-02

User requested fixes for findings 1, 2, 5, and 6.

対応済み:

- Finding 1: Fixed UI prefabs were moved to `Runtime/Prefab/GameUI`; `Runtime/Prefab/GameHUD` now keeps world-space HUD prefabs.
- Finding 2: `ActorEffectIconSpriteCatalog` was separated from `GameHUDViewFactory` and changed to a scene-owned serialized sprite catalog.
- Finding 5: Empty `IInitializable` implementation was removed from `ActorStatusViewPool`.
- Finding 6: Damage number animation naming was changed from `AnchoredPosition` to `LocalOffset`.

Verification:

- `uloop.cmd compile --project-path Client`: Success, ErrorCount 0, WarningCount 0.
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: Passed, 357/357.
- Play mode: Title `NewGame` and seed `Start` buttons were invoked through UI components, `[World] GameWorldState initialized` was observed, then the World ran for 30 seconds. Final logs contained no Error entries.

# Milestone 8 after review 1 - Codex

Date: 2026-05-23
Reviewer: Codex
Scope: `git diff HEAD` for Milestone 8 implementation

## Review Summary

Milestone 8 UI features are broadly present in the diff, but I do not recommend treating the milestone as cleanly complete yet. Two architecture items remain high priority: `WorldHudCanvasProvider` still relies on scene-wide search, and UI Presenter registrations still live in `WorldLifetimeScope` instead of the World UI scope.

I also found one DTO aliasing risk and two commit hygiene issues.

## Finding 1 - WorldHudCanvasProvider still uses scene-wide search

Severity: High

Problem:
`WorldHudCanvasProvider.Initialize()` still calls `Object.FindFirstObjectByType<WorldUIModuleScene>()`. This is the exact S5-1 carryover item in `docs/roadmap/milestone8-roadmap.md` and keeps the HUD canvas dependency outside DI.

Cause:
The UI scope split was only partially introduced. `WorldUIModuleScene` is registered in `WorldUILifetimeScope`, but `WorldHudCanvasProvider` is still resolved from `WorldLifetimeScope`, so it cannot receive `WorldUIModuleScene` by constructor injection.

Solution:
Move HUD/UI presenters and `WorldHudCanvasProvider` into the World UI scope, then inject `WorldUIModuleScene` or a narrow `IWorldHudCanvasProvider`/canvas accessor from that scope. Remove the fallback scene search from the production path.

Evidence files:
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldHudCanvasProvider.cs:18`
- `docs/roadmap/milestone8-roadmap.md:146`
- `docs/self-review/milestone8-completion-review-claude.md:71`

Completion conditions:
- `rg -n "FindFirstObjectByType|FindObjectOfType|Object\\.Find" Client/Assets/DungeonInn/Runtime/Scripts` does not report this path.
- `WorldHudCanvasProvider` receives the HUD canvas dependency through DI or a scene-owned serialized reference.
- `uloop.cmd compile --project-path Client` succeeds.
- 30 second Play mode check emits `[World] GameWorldState initialized` without errors.

## Finding 2 - WorldLifetimeScope still owns HUD/UI Presenter registrations

Severity: High

Problem:
`WorldLifetimeScope` still registers HUD/UI classes even though Milestone 8 introduced `WorldUILifetimeScope`. This leaves `WorldLifetimeScope` as both 3D world composition root and UI composition root, which contradicts the M8 architecture direction.

Cause:
The common parent scope (`MainGameLifetimeScope`) was added, but the UI Presenter migration from `WorldLifetimeScope` to `WorldUILifetimeScope` was not completed.

Solution:
Keep `MainGameLifetimeScope` for shared game/application services. Keep `WorldLifetimeScope` for world 3D view only. Move these UI registrations to `WorldUILifetimeScope`: `WorldHudCanvasProvider`, `ActorHUDViewPool`, `WorldActorStatusPresenter`, `ActorDetailPopupPresenter`, `PlayerGameEventLogPresenter`, `WorldHudPresenter`, `InnStatusPanelPresenter`, and `MinimapPresenter`.

Evidence files:
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs:49`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/WorldUI/WorldUILifetimeScope.cs:10`
- `docs/roadmap/milestone8-roadmap.md:59`

Completion conditions:
- `WorldLifetimeScope` no longer registers HUD/UI Presenter classes.
- `WorldUILifetimeScope` registers World UI scene-owned components and UI presenters.
- World UI presenters can resolve shared application services from `MainGameLifetimeScope`.
- S5-1 is resolved as part of the same migration.

## Finding 3 - ActorDetailViewData exposes reused mutable query buffers

Severity: Medium

Problem:
`GetActorDetailQuery` stores `equipmentNameBuffer` and `effectBuffer` as fields and passes those same list instances into `ActorDetailViewData`. Because `ActorDetailViewData` exposes them as `IReadOnlyList`, callers can keep a DTO whose contents silently change after the next query.

Cause:
The previous per-frame `ToArray()` allocation was removed, but the replacement changed the DTO contract from snapshot data to borrowed mutable buffer without documenting or enforcing that lifetime.

Solution:
Avoid aliasing across query results. Good options are fixed equipment fields for the three equipment slots and a consciously scoped effect snapshot/cache, or a documented copy path that is only used when popup content is refreshed. Add a regression test that two consecutive `Query()` results do not share mutable list contents.

Evidence files:
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetActorDetailQuery.cs:22`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetActorDetailQuery.cs:97`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/ActorDetailViewData.cs:47`

Completion conditions:
- Holding the first `ActorDetailViewData` while querying another actor cannot mutate the first DTO's equipment/effect values.
- A test covers the no-aliasing contract.
- The fix does not reintroduce high-frequency hidden allocations in the frame loop.

## Finding 4 - `git diff --check` fails on generated prefab/meta whitespace

Severity: Low

Problem:
`git diff --check HEAD` reports trailing whitespace in new prefab/meta files, especially `WorldHudView.prefab`, `InnStatusPanelView.prefab`, and `MinimapView.prefab`.

Cause:
Unity-generated YAML includes empty serialized fields with a trailing space. This may be harmless at runtime, but it makes diff hygiene checks fail.

Solution:
Either normalize the generated YAML if Unity preserves it safely, or document that Unity prefab YAML is exempt from whitespace checks. Do not leave the repository in a state where the team expects `git diff --check` to pass but generated prefabs always fail.

Evidence files:
- `Client/Assets/DungeonInn/Runtime/Prefab/World/WorldHudView.prefab:58`
- `Client/Assets/DungeonInn/Runtime/Prefab/World/InnStatusPanelView.prefab:58`
- `Client/Assets/DungeonInn/Runtime/Prefab/World/MinimapView.prefab:58`

Completion conditions:
- `git diff --check HEAD` passes, or the project explicitly exempts Unity YAML from this check.

## Finding 5 - Local uLoop tool metadata changed in the milestone diff

Severity: Low

Problem:
`Client/.uloop/tools.json` changed only local/tooling metadata (`updatedAt` and a blocked-description string). This is not part of the M8 gameplay/UI implementation and may create avoidable commit noise.

Cause:
Running uLoop updated a tracked local metadata file.

Solution:
Confirm whether this file is intentionally tracked as project state. If not, revert this file before committing or move it to ignored local state. If yes, mention why the security description change is expected.

Evidence files:
- `Client/.uloop/tools.json:4`
- `Client/.uloop/tools.json:8`

Completion conditions:
- The commit either excludes `Client/.uloop/tools.json`, or the review/commit message explains why this metadata change is intentional.

## Verification

- Reviewed `docs/guidelines/self-review-guidelines.md`, `lighthouse-patterns.md`, `coding-rules.md`, `implementation-quality-guidelines.md`, `application-boundary-guidelines.md`, `domain-design-guidelines.md`, `debugging-policy.md`, and the M8 roadmap/review docs.
- `docs/guidelines/self-review-preset.md` is referenced by AGENTS.md but is currently missing.
- Ran `git diff HEAD --stat`, `git diff HEAD --name-status`, targeted `rg` scans, and `git diff --check HEAD`.
- I did not rerun `uloop.cmd compile`, EditMode tests, or Play mode during this review. Existing `docs/self-review/milestone8-completion-review-claude.md` reports compile pass, EditMode 298/298 pass, and 30 second Play mode pass.

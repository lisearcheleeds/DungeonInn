# Actor Visual Size Tier Design

## Purpose

Actor sprites may have transparent padding and may use different source resolutions. Runtime display size must therefore not be inferred from the raw image pixel height alone.

`ActorVisualSizeTier` defines how tall the sprite canvas should appear in world meters. The tier is a View-side visual contract for actor sprites, not a Domain actor size or gameplay collision size.

## Rules

- `ActorVisualSizeTier` belongs to the World View layer.
- The tier represents the intended world height of the full sprite canvas.
- Sprite resolution changes pixel density only. A 512px and a 1024px sprite in the same tier should display at the same world height.
- Transparent padding inside the canvas is an asset responsibility. Runtime scales the canvas, not the visible opaque bounds.
- Actor ground placement uses the sprite pivot. Current actor sprites use bottom-center pivot, so the default actor Y offset is `0f`.
- Large monsters should use a larger tier instead of per-actor magic Y offsets or per-asset localScale values.

## Current Tiers

| Tier | Canvas height | Intended use |
|---|---:|---|
| `ActorVisualSizeTier.AdventurerS` | 1.5m | Adventurer-sized humanoid sprites |
| `ActorVisualSizeTier.MonsterS` | 1.2m | Small monsters such as goblins or small pets |
| `ActorVisualSizeTier.MonsterL` | 5.0m | Large monsters such as future dragons |

## Implementation Notes

- `ActorSpriteVisualConfigSO.Entry` stores the visual size tier per actor behavior.
- `ActorView` computes `localScale` from `targetCanvasHeightMeters / sprite.bounds.size.y`.
- `sprite.bounds.size.y` includes transparent canvas area, which is intentional for this design.
- Domain `Actor` does not receive this value. If gameplay collision, reach, combat footprint, or pathing size becomes necessary, it should be introduced as a separate Domain concept.

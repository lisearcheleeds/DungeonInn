using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using LighthouseExtends.Addressable;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class VisualConfigLoader : IDisposable
    {
        static readonly ActorAnimationDirection[] AnimationDirections =
        {
            ActorAnimationDirection.NE,
            ActorAnimationDirection.NW,
            ActorAnimationDirection.SE,
            ActorAnimationDirection.SW
        };

        readonly IAssetManager assetManager;
        readonly MapMaterialSetSO mapMaterialSetSO;
        readonly ActorSpriteVisualConfigSO actorSpriteVisualConfigSO;
        readonly Dictionary<TileVisualKind, Material> loadedMaterials = new();
        readonly Dictionary<ActorBehaviorType, ActorSpriteSet> loadedSpriteSets = new();
        readonly List<Sprite> fallbackSprites = new();
        readonly List<Texture2D> fallbackTextures = new();

        IAssetScope assetScope;

        [Inject]
        public VisualConfigLoader(
            IAssetManager assetManager,
            VisualConfigSettings settings)
        {
            this.assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            mapMaterialSetSO = settings.MapMaterialSetSO;
            actorSpriteVisualConfigSO = settings.ActorSpriteVisualConfigSO;
        }

        public async UniTask LoadAsync(CancellationToken ct)
        {
            assetScope = assetManager.CreateScope();

            if (mapMaterialSetSO != null)
            {
                await LoadMaterialsAsync(ct);
            }

            if (actorSpriteVisualConfigSO != null)
            {
                await LoadSpritesAsync(ct);
            }

        }

        public Material GetMaterial(TileVisualKind kind)
        {
            loadedMaterials.TryGetValue(kind, out var material);
            return material;
        }

        public ActorSpriteSet GetSpriteSet(ActorBehaviorType type)
        {
            loadedSpriteSets.TryGetValue(type, out var spriteSet);
            return spriteSet;
        }

        public void Dispose()
        {
            assetScope?.Dispose();

            foreach (var sprite in fallbackSprites)
            {
                if (sprite != null)
                {
                    UnityEngine.Object.Destroy(sprite);
                }
            }

            foreach (var texture in fallbackTextures)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            fallbackSprites.Clear();
            fallbackTextures.Clear();
        }

        async UniTask LoadMaterialsAsync(CancellationToken ct)
        {
            foreach (var entry in mapMaterialSetSO.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.MaterialAddress))
                {
                    continue;
                }

                try
                {
                    var handle = await assetScope.LoadAsync<Material>(entry.MaterialAddress, ct);
                    loadedMaterials[entry.Kind] = handle.Asset;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"[VisualConfigLoader] Failed to load material. Address={entry.MaterialAddress} Error={exception.Message}");
                }
            }
        }

        async UniTask LoadSpritesAsync(CancellationToken ct)
        {
            foreach (var entry in actorSpriteVisualConfigSO.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.AddressPrefix))
                {
                    continue;
                }

                var spriteSet = new ActorSpriteSet(
                    CreatePlaceholderSprite(entry.FallbackColor),
                    entry.VisualSizeTier);
                foreach (var direction in AnimationDirections)
                {
                    await LoadIdleSpriteAsync(entry, spriteSet, direction, ct);
                    await LoadWalkSpriteAsync(entry, spriteSet, direction, 0, ct);
                    await LoadWalkSpriteAsync(entry, spriteSet, direction, 1, ct);
                }

                loadedSpriteSets[entry.BehaviorType] = spriteSet;
            }
        }

        async UniTask LoadIdleSpriteAsync(
            ActorSpriteVisualConfigSO.Entry entry,
            ActorSpriteSet spriteSet,
            ActorAnimationDirection direction,
            CancellationToken ct)
        {
            var address = $"{entry.AddressPrefix}/Idle{direction}";
            try
            {
                var handle = await assetScope.LoadAsync<Sprite>(address, ct);
                spriteSet.SetIdleSprite(direction, handle.Asset);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[VisualConfigLoader] Failed to load sprite. Address={address} Error={exception.Message}");
            }
        }

        async UniTask LoadWalkSpriteAsync(
            ActorSpriteVisualConfigSO.Entry entry,
            ActorSpriteSet spriteSet,
            ActorAnimationDirection direction,
            int frameIndex,
            CancellationToken ct)
        {
            var frameNumber = frameIndex + 1;
            var address = $"{entry.AddressPrefix}/Walk{direction}{frameNumber}";
            try
            {
                var handle = await assetScope.LoadAsync<Sprite>(address, ct);
                spriteSet.SetWalkSprite(direction, frameIndex, handle.Asset);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[VisualConfigLoader] Failed to load sprite. Address={address} Error={exception.Message}");
            }
        }

        Sprite CreatePlaceholderSprite(Color color)
        {
            var sprite = PlaceholderAssetFactory.CreateActorPlaceholder(color, out var texture);
            fallbackTextures.Add(texture);
            fallbackSprites.Add(sprite);
            return sprite;
        }
    }
}

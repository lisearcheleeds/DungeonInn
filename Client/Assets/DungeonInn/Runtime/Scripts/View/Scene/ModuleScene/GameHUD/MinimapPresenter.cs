using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.View.Scene.Bridge;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class MinimapPresenter : IDisposable
    {
        const int ActorDotSize = 3;

        static readonly Color GroundWalkableColor = new(0.7f, 0.7f, 0.7f, 1f);
        static readonly Color GroundBlockedColor = new(0.15f, 0.15f, 0.15f, 1f);
        static readonly Color DungeonWalkableColor = new(0.5f, 0.45f, 0.3f, 1f);
        static readonly Color DungeonBlockedColor = new(0.1f, 0.1f, 0.1f, 1f);
        static readonly Color StairUpColor = new(0.2f, 0.8f, 0.2f, 1f);
        static readonly Color StairDownColor = new(0.8f, 0.2f, 0.2f, 1f);
        static readonly Color AdventurerDotColor = Color.blue;
        static readonly Color MonsterDotColor = Color.red;
        static readonly Color OtherActorDotColor = Color.yellow;

        readonly IWorldMapViewDataProvider mapViewDataProvider;
        readonly IActorSelectionCandidateProvider actorSelectionCandidateProvider;
        readonly IActiveLayerProvider activeLayerProvider;
        readonly GameHUDAddressableViewFactory viewFactory;
        readonly GameHUDModuleScene gameHUDModuleScene;
        readonly List<ActorViewData> actorBuffer = new();

        MinimapView minimapView;
        Texture2D mapTexture;
        Color[] basePixels;
        Color[] workPixels;
        WorldMapLayerViewData currentLayerData;
        int? lastLayerId;

        [Inject]
        public MinimapPresenter(
            IWorldMapViewDataProvider mapViewDataProvider,
            IActorSelectionCandidateProvider actorSelectionCandidateProvider,
            IActiveLayerProvider activeLayerProvider,
            GameHUDAddressableViewFactory viewFactory,
            GameHUDModuleScene gameHUDModuleScene)
        {
            this.mapViewDataProvider = mapViewDataProvider ?? throw new ArgumentNullException(nameof(mapViewDataProvider));
            this.actorSelectionCandidateProvider =
                actorSelectionCandidateProvider ?? throw new ArgumentNullException(nameof(actorSelectionCandidateProvider));
            this.activeLayerProvider = activeLayerProvider ?? throw new ArgumentNullException(nameof(activeLayerProvider));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameHUDModuleScene = gameHUDModuleScene ?? throw new ArgumentNullException(nameof(gameHUDModuleScene));
        }

        public void Initialize()
        {
            EnsureView();
            UpdateMinimap();
        }

        public void UpdateMinimap()
        {
            var currentLayerId = activeLayerProvider.ActiveLayerId;
            if (!currentLayerId.HasValue)
            {
                return;
            }

            EnsureView();
            if (minimapView == null)
            {
                return;
            }

            if (!lastLayerId.HasValue || !currentLayerId.Value.Equals(lastLayerId.Value))
            {
                RebuildMapTexture(currentLayerId.Value);
                lastLayerId = currentLayerId.Value;
            }

            UpdateActorDots(currentLayerId.Value);
        }

        public void Dispose()
        {
            if (minimapView != null)
            {
                UnityEngine.Object.Destroy(minimapView.gameObject);
                minimapView = null;
            }

            if (mapTexture != null)
            {
                UnityEngine.Object.Destroy(mapTexture);
                mapTexture = null;
            }

            basePixels = null;
            workPixels = null;
            actorBuffer.Clear();
            lastLayerId = null;
        }

        void EnsureView()
        {
            if (minimapView != null || gameHUDModuleScene.HUDCanvas == null)
            {
                return;
            }

            minimapView = viewFactory.CreateMinimapView(gameHUDModuleScene.HUDCanvas.transform);
        }

        void RebuildMapTexture(int layerId)
        {
            currentLayerData = mapViewDataProvider.GetLayerById(layerId);
            var width = currentLayerData.Width;
            var height = currentLayerData.Height;
            CreateTextureIfNeeded(width, height);

            for (var z = 0; z < height; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = z * width + x;
                    basePixels[index] = ResolveCellColor(currentLayerData.GetCellKind(x, z));
                }
            }

            mapTexture.SetPixels(basePixels);
            mapTexture.Apply(false);
            minimapView.SetMapTexture(mapTexture);
        }

        void CreateTextureIfNeeded(int width, int height)
        {
            var pixelCount = width * height;
            if (mapTexture != null && mapTexture.width == width && mapTexture.height == height)
            {
                if (basePixels == null || basePixels.Length != pixelCount)
                {
                    basePixels = new Color[pixelCount];
                    workPixels = new Color[pixelCount];
                }

                return;
            }

            if (mapTexture != null)
            {
                UnityEngine.Object.Destroy(mapTexture);
            }

            mapTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            basePixels = new Color[pixelCount];
            workPixels = new Color[pixelCount];
        }

        void UpdateActorDots(int layerId)
        {
            if (mapTexture == null || basePixels == null || workPixels == null)
            {
                return;
            }

            Array.Copy(basePixels, workPixels, basePixels.Length);
            actorSelectionCandidateProvider.CopySelectionCandidatesTo(actorBuffer);
            foreach (var actor in actorBuffer)
            {
                if (actor.Position.LayerId.Value != layerId)
                {
                    continue;
                }

                var x = Mathf.Clamp((int)(actor.Position.X / GameConstants.MapCellWidthMeters), 0, currentLayerData.Width - 1);
                var z = Mathf.Clamp((int)(actor.Position.Z / GameConstants.MapCellWidthMeters), 0, currentLayerData.Height - 1);
                DrawActorDot(x, z, ResolveActorDotColor(actor.BehaviorType));
            }

            mapTexture.SetPixels(workPixels);
            mapTexture.Apply(false);
        }

        void DrawActorDot(int centerX, int centerZ, Color color)
        {
            var radius = ActorDotSize / 2;
            for (var z = centerZ - radius; z <= centerZ + radius; z++)
            {
                if (z < 0 || currentLayerData.Height <= z)
                {
                    continue;
                }

                for (var x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || currentLayerData.Width <= x)
                    {
                        continue;
                    }

                    workPixels[z * currentLayerData.Width + x] = color;
                }
            }
        }

        static Color ResolveCellColor(WorldMapCellViewKind cellKind)
        {
            switch (cellKind)
            {
                case WorldMapCellViewKind.GroundWalkable:
                    return GroundWalkableColor;
                case WorldMapCellViewKind.GroundBlocked:
                    return GroundBlockedColor;
                case WorldMapCellViewKind.DungeonWalkable:
                    return DungeonWalkableColor;
                case WorldMapCellViewKind.DungeonBlocked:
                    return DungeonBlockedColor;
                case WorldMapCellViewKind.StairUp:
                    return StairUpColor;
                case WorldMapCellViewKind.StairDown:
                    return StairDownColor;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cellKind));
            }
        }

        static Color ResolveActorDotColor(ActorBehaviorType behaviorType)
        {
            switch (behaviorType)
            {
                case ActorBehaviorType.Adventurer:
                    return AdventurerDotColor;
                case ActorBehaviorType.Monster:
                    return MonsterDotColor;
                case ActorBehaviorType.None:
                case ActorBehaviorType.GuildStaff:
                case ActorBehaviorType.Pet:
                    return OtherActorDotColor;
                default:
                    throw new ArgumentOutOfRangeException(nameof(behaviorType));
            }
        }
    }
}

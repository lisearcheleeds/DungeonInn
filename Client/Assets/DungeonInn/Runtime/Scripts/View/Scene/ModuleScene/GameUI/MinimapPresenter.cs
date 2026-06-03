using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.View.Scene.Bridge;

namespace DungeonInn.View.Scene.ModuleScene.GameUI
{
    public sealed class MinimapPresenter : IDisposable
    {
        static readonly Color GroundWalkableColor = new(0.7f, 0.7f, 0.7f, 1f);
        static readonly Color GroundBlockedColor = new(0.15f, 0.15f, 0.15f, 1f);
        static readonly Color DungeonWalkableColor = new(0.5f, 0.45f, 0.3f, 1f);
        static readonly Color DungeonBlockedColor = new(0.1f, 0.1f, 0.1f, 1f);

        readonly IWorldMapViewDataProvider mapViewDataProvider;
        readonly IActorSelectionCandidateProvider actorSelectionCandidateProvider;
        readonly IActiveLayerProvider activeLayerProvider;
        readonly IWorldHudCameraProvider worldHudCameraProvider;
        readonly GameUIAddressableViewFactory viewFactory;
        readonly GameUIModuleScene gameUIModuleScene;
        readonly List<ActorViewData> actorBuffer = new();
        readonly List<Vector2> adventurerDotPositions = new();

        MinimapView minimapView;
        Texture2D mapTexture;
        Color[] basePixels;
        WorldMapLayerViewData currentLayerData;
        int? lastLayerId;
        bool hasCurrentLayerData;

        [Inject]
        public MinimapPresenter(
            IWorldMapViewDataProvider mapViewDataProvider,
            IActorSelectionCandidateProvider actorSelectionCandidateProvider,
            IActiveLayerProvider activeLayerProvider,
            IWorldHudCameraProvider worldHudCameraProvider,
            GameUIAddressableViewFactory viewFactory,
            GameUIModuleScene gameUIModuleScene)
        {
            this.mapViewDataProvider = mapViewDataProvider ?? throw new ArgumentNullException(nameof(mapViewDataProvider));
            this.actorSelectionCandidateProvider =
                actorSelectionCandidateProvider ?? throw new ArgumentNullException(nameof(actorSelectionCandidateProvider));
            this.activeLayerProvider = activeLayerProvider ?? throw new ArgumentNullException(nameof(activeLayerProvider));
            this.worldHudCameraProvider =
                worldHudCameraProvider ?? throw new ArgumentNullException(nameof(worldHudCameraProvider));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameUIModuleScene = gameUIModuleScene ?? throw new ArgumentNullException(nameof(gameUIModuleScene));
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

            minimapView.SetMapRotation(worldHudCameraProvider.CameraRotation.eulerAngles.y);
            UpdateAdventurerDots(currentLayerId.Value);
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
            actorBuffer.Clear();
            adventurerDotPositions.Clear();
            lastLayerId = null;
            hasCurrentLayerData = false;
        }

        void EnsureView()
        {
            if (minimapView != null || gameUIModuleScene.UICanvas == null)
            {
                return;
            }

            minimapView = viewFactory.CreateMinimapView(gameUIModuleScene.UICanvas.transform);
        }

        void RebuildMapTexture(int layerId)
        {
            currentLayerData = mapViewDataProvider.GetLayerById(layerId);
            hasCurrentLayerData = true;
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
        }

        void UpdateAdventurerDots(int layerId)
        {
            if (!hasCurrentLayerData)
            {
                return;
            }

            adventurerDotPositions.Clear();
            actorSelectionCandidateProvider.CopySelectionCandidatesTo(actorBuffer);
            foreach (var actor in actorBuffer)
            {
                if (actor.Position.LayerId.Value != layerId ||
                    actor.BehaviorType != ActorBehaviorType.Adventurer)
                {
                    continue;
                }

                adventurerDotPositions.Add(new Vector2(
                    Mathf.Clamp01(actor.Position.X / (currentLayerData.Width * GameConstants.MapCellWidthMeters)),
                    Mathf.Clamp01(actor.Position.Z / (currentLayerData.Height * GameConstants.MapCellWidthMeters))));
            }

            minimapView.SetAdventurerDots(adventurerDotPositions);
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
                case WorldMapCellViewKind.StairDown:
                    return DungeonWalkableColor;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cellKind));
            }
        }
    }
}


using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldMapView : IDisposable
    {
        readonly IWorldMapViewDataProvider viewDataProvider;
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly MapMeshBuildService mapMeshBuildService;
        readonly NavMeshBuildService navMeshBuildService;
        readonly EnvironmentObjectPlacer environmentObjectPlacer;
        readonly IWorldMapViewSettingsRepository worldMapViewSettingsRepository;
        readonly HashSet<int> scheduledLayerIds = new();
        readonly HashSet<int> completedLayerIds = new();
        readonly Dictionary<int, int> remainingChunkCountsByLayer = new();
        readonly Dictionary<int, int> layerBuildVersions = new();
        readonly Queue<MapChunkBuildRequest> pendingChunkBuilds = new();
        readonly List<Mesh> generatedMeshes = new();

        [Inject]
        public WorldMapView(
            IWorldMapViewDataProvider viewDataProvider,
            MapLayerViewRegistry layerViewRegistry,
            MapMeshBuildService mapMeshBuildService,
            NavMeshBuildService navMeshBuildService,
            EnvironmentObjectPlacer environmentObjectPlacer,
            IWorldMapViewSettingsRepository worldMapViewSettingsRepository)
        {
            this.viewDataProvider = viewDataProvider ?? throw new ArgumentNullException(nameof(viewDataProvider));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            this.mapMeshBuildService = mapMeshBuildService ?? throw new ArgumentNullException(nameof(mapMeshBuildService));
            this.navMeshBuildService = navMeshBuildService ?? throw new ArgumentNullException(nameof(navMeshBuildService));
            this.environmentObjectPlacer = environmentObjectPlacer ?? throw new ArgumentNullException(nameof(environmentObjectPlacer));
            this.worldMapViewSettingsRepository =
                worldMapViewSettingsRepository ?? throw new ArgumentNullException(nameof(worldMapViewSettingsRepository));
        }

        public void UpdateVisuals()
        {
            BuildQueuedChunks(worldMapViewSettingsRepository.GetWorldMapViewSettings().ChunkBuildsPerFrame);
        }

        public void NotifyLayerAdded(MapLayerId layerId)
        {
            if (scheduledLayerIds.Contains(layerId.Value))
            {
                return;
            }

            var layerData = viewDataProvider.GetLayer(layerId);
            EnqueueLayerChunks(layerData);
            scheduledLayerIds.Add(layerId.Value);
        }

        public bool IsTileBuildCompleted(MapLayerId layerId)
        {
            return completedLayerIds.Contains(layerId.Value);
        }

        public void InvalidateLayer(MapLayerId layerId)
        {
            scheduledLayerIds.Remove(layerId.Value);
            completedLayerIds.Remove(layerId.Value);
            remainingChunkCountsByLayer.Remove(layerId.Value);
            IncrementLayerBuildVersion(layerId.Value);
            environmentObjectPlacer.InvalidateLayer(layerId);
            layerViewRegistry.DestroyLayerRoot(layerId);
            navMeshBuildService.InvalidateLayer(layerId);
            viewDataProvider.InvalidateLayer(layerId);
        }

        public void Dispose()
        {
            foreach (var mesh in generatedMeshes)
            {
                if (mesh != null)
                {
                    DestroyGeneratedMesh(mesh);
                }
            }

            generatedMeshes.Clear();
            pendingChunkBuilds.Clear();
            scheduledLayerIds.Clear();
            completedLayerIds.Clear();
            remainingChunkCountsByLayer.Clear();
            layerBuildVersions.Clear();
        }

        void EnqueueLayerChunks(WorldMapLayerViewData layerData)
        {
            var layerIdValue = layerData.LayerId.Value;
            var buildVersion = IncrementLayerBuildVersion(layerIdValue);
            var layerRoot = CreateLayerRoot(layerData.LayerName, layerData.LayerId);
            var chunkCount = 0;
            var worldMapViewSettings = worldMapViewSettingsRepository.GetWorldMapViewSettings();
            for (var z = 0; z < layerData.Height; z += worldMapViewSettings.ChunkTileSize)
            {
                for (var x = 0; x < layerData.Width; x += worldMapViewSettings.ChunkTileSize)
                {
                    chunkCount++;
                    pendingChunkBuilds.Enqueue(new MapChunkBuildRequest(
                        layerRoot,
                        layerData,
                        x,
                        z,
                        Math.Min(worldMapViewSettings.ChunkTileSize, layerData.Width - x),
                        Math.Min(worldMapViewSettings.ChunkTileSize, layerData.Height - z),
                        buildVersion));
                }
            }

            remainingChunkCountsByLayer[layerIdValue] = chunkCount;
        }

        int IncrementLayerBuildVersion(int layerId)
        {
            layerBuildVersions.TryGetValue(layerId, out var version);
            version++;
            layerBuildVersions[layerId] = version;
            return version;
        }

        Transform CreateLayerRoot(string layerName, MapLayerId layerId)
        {
            return layerViewRegistry.GetOrCreateTileRoot(layerId, layerName);
        }

        void BuildQueuedChunks(int maxChunkCount)
        {
            for (var count = 0; count < maxChunkCount && 0 < pendingChunkBuilds.Count; count++)
            {
                BuildQueuedChunk(pendingChunkBuilds.Dequeue());
            }
        }

        void BuildQueuedChunk(MapChunkBuildRequest request)
        {
            var layerId = request.LayerData.LayerId.Value;
            if (!layerBuildVersions.TryGetValue(layerId, out var currentVersion) ||
                currentVersion != request.BuildVersion)
            {
                return;
            }

            var chunkMesh = mapMeshBuildService.BuildChunk(
                request.LayerData.LayerId,
                request.StartX,
                request.StartZ,
                request.Width,
                request.Height,
                position => ToTileVisualKind(request.LayerData.GetCellKind(position)));

            CreateChunkObject(
                request.LayerRoot,
                request.LayerData.LayerName,
                request.StartX,
                request.StartZ,
                chunkMesh);
            environmentObjectPlacer.PlaceChunkProps(
                request.LayerRoot,
                request.LayerData,
                request.StartX,
                request.StartZ,
                request.Width,
                request.Height);
            CompleteChunkBuild(request.LayerData.LayerId);
        }

        void CompleteChunkBuild(MapLayerId layerId)
        {
            var layerIdValue = layerId.Value;
            if (!remainingChunkCountsByLayer.TryGetValue(layerIdValue, out var remainingCount))
            {
                return;
            }

            remainingCount--;
            if (0 < remainingCount)
            {
                remainingChunkCountsByLayer[layerIdValue] = remainingCount;
                return;
            }

            remainingChunkCountsByLayer.Remove(layerIdValue);
            completedLayerIds.Add(layerIdValue);
            navMeshBuildService.BakeLayerIfNeeded(layerId);
        }

        static TileVisualKind ToTileVisualKind(WorldMapCellViewKind cellViewKind)
        {
            switch (cellViewKind)
            {
                case WorldMapCellViewKind.GroundWalkable:
                    return TileVisualKind.GroundWalkable;
                case WorldMapCellViewKind.GroundBlocked:
                    return TileVisualKind.GroundBlocked;
                case WorldMapCellViewKind.DungeonWalkable:
                    return TileVisualKind.DungeonWalkable;
                case WorldMapCellViewKind.DungeonBlocked:
                    return TileVisualKind.DungeonBlocked;
                case WorldMapCellViewKind.StairUp:
                    return TileVisualKind.StairUp;
                case WorldMapCellViewKind.StairDown:
                    return TileVisualKind.StairDown;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cellViewKind));
            }
        }

        void CreateChunkObject(
            Transform layerRoot,
            string layerName,
            int startX,
            int startZ,
            MapChunkMesh chunkMesh)
        {
            var chunkObject = new GameObject($"{layerName}_Chunk_{startX}_{startZ}");
            chunkObject.layer = WorldRenderingLayer.Layer;
            chunkObject.transform.SetParent(layerRoot, false);

            var meshFilter = chunkObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = chunkMesh.Mesh;
            generatedMeshes.Add(chunkMesh.Mesh);

            var meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = chunkMesh.Materials;
        }

        readonly struct MapChunkBuildRequest
        {
            public MapChunkBuildRequest(
                Transform layerRoot,
                WorldMapLayerViewData layerData,
                int startX,
                int startZ,
                int width,
                int height,
                int buildVersion)
            {
                LayerRoot = layerRoot;
                LayerData = layerData;
                StartX = startX;
                StartZ = startZ;
                Width = width;
                Height = height;
                BuildVersion = buildVersion;
            }

            public Transform LayerRoot { get; }
            public WorldMapLayerViewData LayerData { get; }
            public int StartX { get; }
            public int StartZ { get; }
            public int Width { get; }
            public int Height { get; }
            public int BuildVersion { get; }
        }

        static void DestroyGeneratedMesh(Mesh mesh)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                return;
            }
#endif
            UnityEngine.Object.Destroy(mesh);
        }
    }
}

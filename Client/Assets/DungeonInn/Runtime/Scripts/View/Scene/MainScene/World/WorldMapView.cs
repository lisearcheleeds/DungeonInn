using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldMapView : IDisposable
    {
        readonly IWorldMapViewDataProvider viewDataProvider;
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly MapMeshBuildService mapMeshBuildService;
        readonly HashSet<int> scheduledLayerIds = new();
        readonly HashSet<int> completedLayerIds = new();
        readonly Dictionary<int, int> remainingChunkCountsByLayer = new();
        readonly Queue<MapChunkBuildRequest> pendingChunkBuilds = new();
        readonly List<Mesh> generatedMeshes = new();

        public WorldMapView(
            IWorldMapViewDataProvider viewDataProvider,
            MapLayerViewRegistry layerViewRegistry,
            MapMeshBuildService mapMeshBuildService)
        {
            this.viewDataProvider = viewDataProvider ?? throw new ArgumentNullException(nameof(viewDataProvider));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            this.mapMeshBuildService = mapMeshBuildService ?? throw new ArgumentNullException(nameof(mapMeshBuildService));
        }

        public void UpdateVisuals()
        {
            EnqueueMissingLayerTiles();
            BuildQueuedChunks(GameConstants.MapChunkBuildsPerFrame);
        }

        public bool IsTileBuildCompleted(MapLayerId layerId)
        {
            return completedLayerIds.Contains(layerId.Value);
        }

        public void Dispose()
        {
            foreach (var mesh in generatedMeshes)
            {
                if (mesh != null)
                {
                    UnityEngine.Object.Destroy(mesh);
                }
            }

            generatedMeshes.Clear();
            pendingChunkBuilds.Clear();
            scheduledLayerIds.Clear();
            completedLayerIds.Clear();
            remainingChunkCountsByLayer.Clear();
        }

        void EnqueueMissingLayerTiles()
        {
            foreach (var layerData in viewDataProvider.GetLayers())
            {
                if (scheduledLayerIds.Contains(layerData.LayerId.Value))
                {
                    continue;
                }

                EnqueueLayerChunks(layerData);
                scheduledLayerIds.Add(layerData.LayerId.Value);
            }
        }

        void EnqueueLayerChunks(WorldMapLayerViewData layerData)
        {
            var layerRoot = CreateLayerRoot(layerData.LayerName, layerData.LayerId);
            var chunkCount = 0;
            for (var z = 0; z < layerData.Height; z += GameConstants.MapChunkTileSize)
            {
                for (var x = 0; x < layerData.Width; x += GameConstants.MapChunkTileSize)
                {
                    chunkCount++;
                    pendingChunkBuilds.Enqueue(new MapChunkBuildRequest(
                        layerRoot,
                        layerData,
                        x,
                        z,
                        Math.Min(GameConstants.MapChunkTileSize, layerData.Width - x),
                        Math.Min(GameConstants.MapChunkTileSize, layerData.Height - z)));
                }
            }

            remainingChunkCountsByLayer[layerData.LayerId.Value] = chunkCount;
        }

        Transform CreateLayerRoot(string layerName, MapLayerId layerId)
        {
            return layerViewRegistry.GetOrCreateTileRoot(layerId, layerName);
        }

        void BuildQueuedChunks(int maxChunkCount)
        {
            for (var count = 0; count < maxChunkCount && pendingChunkBuilds.Count > 0; count++)
            {
                BuildQueuedChunk(pendingChunkBuilds.Dequeue());
            }
        }

        void BuildQueuedChunk(MapChunkBuildRequest request)
        {
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
            if (remainingCount > 0)
            {
                remainingChunkCountsByLayer[layerIdValue] = remainingCount;
                return;
            }

            remainingChunkCountsByLayer.Remove(layerIdValue);
            completedLayerIds.Add(layerIdValue);
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
                int height)
            {
                LayerRoot = layerRoot;
                LayerData = layerData;
                StartX = startX;
                StartZ = startZ;
                Width = width;
                Height = height;
            }

            public Transform LayerRoot { get; }
            public WorldMapLayerViewData LayerData { get; }
            public int StartX { get; }
            public int StartZ { get; }
            public int Width { get; }
            public int Height { get; }
        }
    }
}

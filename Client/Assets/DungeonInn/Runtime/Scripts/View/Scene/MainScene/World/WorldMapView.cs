using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldMapView : IDisposable
    {
        readonly IWorldMapViewDataProvider viewDataProvider;
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly MapMeshBuildService mapMeshBuildService;
        readonly HashSet<int> builtLayerIds = new();
        readonly List<Mesh> generatedMeshes = new();

        const int ChunkTileSize = 16;

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
            BuildMissingLayerTiles();
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
        }

        void BuildMissingLayerTiles()
        {
            foreach (var layerData in viewDataProvider.GetLayers())
            {
                if (builtLayerIds.Contains(layerData.LayerId.Value))
                {
                    continue;
                }

                BuildLayerTiles(layerData);
                builtLayerIds.Add(layerData.LayerId.Value);
            }
        }

        void BuildLayerTiles(WorldMapLayerViewData layerData)
        {
            var layerRoot = CreateLayerRoot(layerData.LayerName, layerData.LayerId);
            BuildLayerChunks(
                layerRoot,
                layerData.LayerId,
                layerData.LayerName,
                layerData.Width,
                layerData.Height,
                position => ToTileVisualKind(layerData.GetCellKind(position)));
        }

        Transform CreateLayerRoot(string layerName, MapLayerId layerId)
        {
            return layerViewRegistry.GetOrCreateTileRoot(layerId, layerName);
        }

        void BuildLayerChunks(
            Transform layerRoot,
            MapLayerId layerId,
            string layerName,
            int layerWidth,
            int layerHeight,
            Func<GridPosition, TileVisualKind> resolveVisualKind)
        {
            for (var z = 0; z < layerHeight; z += ChunkTileSize)
            {
                for (var x = 0; x < layerWidth; x += ChunkTileSize)
                {
                    var width = Math.Min(ChunkTileSize, layerWidth - x);
                    var height = Math.Min(ChunkTileSize, layerHeight - z);
                    var chunkMesh = mapMeshBuildService.BuildChunk(
                        layerId,
                        x,
                        z,
                        width,
                        height,
                        resolveVisualKind);

                    CreateChunkObject(layerRoot, layerName, x, z, chunkMesh);
                }
            }
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
            meshRenderer.sharedMaterials = ToMaterialArray(chunkMesh.Materials);
        }

        static Material[] ToMaterialArray(IReadOnlyList<Material> materials)
        {
            var result = new Material[materials.Count];
            for (var index = 0; index < materials.Count; index++)
            {
                result[index] = materials[index];
            }

            return result;
        }
    }
}

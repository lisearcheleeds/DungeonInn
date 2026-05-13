using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldMapView : IDisposable
    {
        readonly IGameWorldStateReader gameWorldState;
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly MapMeshBuildService mapMeshBuildService;
        readonly HashSet<int> builtLayerIds = new();
        readonly List<Mesh> generatedMeshes = new();

        const int ChunkTileSize = 16;

        public WorldMapView(
            IGameWorldStateReader gameWorldState,
            MapLayerViewRegistry layerViewRegistry,
            MapMeshBuildService mapMeshBuildService)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
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
            if (!builtLayerIds.Contains(MapLayerId.Ground.Value))
            {
                BuildGroundTiles();
                builtLayerIds.Add(MapLayerId.Ground.Value);
            }

            foreach (var pair in gameWorldState.Dungeon.Floors)
            {
                var layerId = MapLayerId.DungeonFloor(pair.Key).Value;
                if (builtLayerIds.Contains(layerId))
                {
                    continue;
                }

                BuildDungeonTiles(pair.Value);
                builtLayerIds.Add(layerId);
            }
        }

        void BuildGroundTiles()
        {
            var layer = gameWorldState.GroundMap.Layer;
            var layerRoot = CreateLayerRoot("Ground", layer.Id);
            BuildLayerChunks(
                layerRoot,
                layer,
                "Ground",
                position => gameWorldState.GroundMap.IsWalkable(position)
                    ? TileVisualKind.GroundWalkable
                    : TileVisualKind.GroundBlocked);
        }

        void BuildDungeonTiles(DungeonFloor floor)
        {
            var layerRoot = CreateLayerRoot($"DungeonFloor{floor.FloorIndex}", floor.Layer.Id);
            BuildLayerChunks(
                layerRoot,
                floor.Layer,
                $"DungeonFloor{floor.FloorIndex}",
                position => ResolveDungeonVisualKind(floor, position));
        }

        Transform CreateLayerRoot(string layerName, MapLayerId layerId)
        {
            return layerViewRegistry.GetOrCreateTileRoot(layerId, layerName);
        }

        void BuildLayerChunks(
            Transform layerRoot,
            MapLayer layer,
            string layerName,
            Func<GridPosition, TileVisualKind> resolveVisualKind)
        {
            for (var z = 0; z < layer.Depth; z += ChunkTileSize)
            {
                for (var x = 0; x < layer.Width; x += ChunkTileSize)
                {
                    var width = Math.Min(ChunkTileSize, layer.Width - x);
                    var depth = Math.Min(ChunkTileSize, layer.Depth - z);
                    var chunkMesh = mapMeshBuildService.BuildChunk(
                        layer,
                        x,
                        z,
                        width,
                        depth,
                        resolveVisualKind);

                    CreateChunkObject(layerRoot, layerName, x, z, chunkMesh);
                }
            }
        }

        static TileVisualKind ResolveDungeonVisualKind(DungeonFloor floor, GridPosition position)
        {
            if (floor.IsStairPosition(position, DungeonStairType.Up))
            {
                return TileVisualKind.StairUp;
            }

            if (floor.IsStairPosition(position, DungeonStairType.Down))
            {
                return TileVisualKind.StairDown;
            }

            return floor.IsWalkable(position)
                ? TileVisualKind.DungeonWalkable
                : TileVisualKind.DungeonBlocked;
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

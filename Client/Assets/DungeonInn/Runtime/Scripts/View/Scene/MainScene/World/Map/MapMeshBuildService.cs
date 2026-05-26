using System;
using System.Collections.Generic;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class MapMeshBuildService
    {
        readonly MapTileVisualConfig tileVisualConfig;
        readonly MapMaterialSet mapMaterialSet;
        readonly IWorldMapViewSettingsRepository worldMapViewSettingsRepository;
        readonly List<Vector3> vertices = new();
        readonly List<Vector2> uv = new();
        readonly Dictionary<TileVisualKind, List<int>> trianglesByKind = new();
        readonly List<Material> materials = new();
        readonly List<TileVisualKind> visualKinds = new();

        [Inject]
        public MapMeshBuildService(
            MapTileVisualConfig tileVisualConfig,
            MapMaterialSet mapMaterialSet,
            IWorldMapViewSettingsRepository worldMapViewSettingsRepository)
        {
            this.tileVisualConfig = tileVisualConfig ?? throw new ArgumentNullException(nameof(tileVisualConfig));
            this.mapMaterialSet = mapMaterialSet ?? throw new ArgumentNullException(nameof(mapMaterialSet));
            this.worldMapViewSettingsRepository =
                worldMapViewSettingsRepository ?? throw new ArgumentNullException(nameof(worldMapViewSettingsRepository));
        }

        public MapChunkMesh BuildChunk(
            MapLayerId layerId,
            int startX,
            int startZ,
            int width,
            int height,
            Func<GridPosition, TileVisualKind> resolveVisualKind)
        {
            if (resolveVisualKind == null)
            {
                throw new ArgumentNullException(nameof(resolveVisualKind));
            }

            ClearBuffers();
            EnsureBufferCapacity(width * height);

            for (var z = startZ; z < startZ + height; z++)
            {
                for (var x = startX; x < startX + width; x++)
                {
                    var position = new GridPosition(x, z);
                    var visualKind = resolveVisualKind(position);
                    var visualDefinition = tileVisualConfig.Get(visualKind);
                    AddTileGeometry(position, visualDefinition);
                }
            }

            var mesh = new Mesh
            {
                name = $"MapChunk_{layerId.Value}_{startX}_{startZ}"
            };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = visualKinds.Count;

            for (var index = 0; index < visualKinds.Count; index++)
            {
                mesh.SetTriangles(trianglesByKind[visualKinds[index]], index);
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return new MapChunkMesh(mesh, ToMaterialArray());
        }

        void ClearBuffers()
        {
            vertices.Clear();
            uv.Clear();
            materials.Clear();
            visualKinds.Clear();

            foreach (var triangles in trianglesByKind.Values)
            {
                triangles.Clear();
            }
        }

        void EnsureBufferCapacity(int tileCount)
        {
            EnsureCapacity(vertices, tileCount * 20);
            EnsureCapacity(uv, tileCount * 20);

            var triangleCount = tileCount * 30;
            foreach (var triangles in trianglesByKind.Values)
            {
                EnsureCapacity(triangles, triangleCount);
            }
        }

        Material[] ToMaterialArray()
        {
            var result = new Material[materials.Count];
            for (var index = 0; index < materials.Count; index++)
            {
                result[index] = materials[index];
            }

            return result;
        }

        static void EnsureCapacity<T>(List<T> list, int capacity)
        {
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }
        }

        void AddTileGeometry(
            GridPosition position,
            TileVisualDefinition visualDefinition)
        {
            switch (visualDefinition.ShapeKind)
            {
                case TileMeshShapeKind.Block:
                    AddBlock(position, visualDefinition);
                    break;
                case TileMeshShapeKind.Ramp:
                    AddRamp(position, visualDefinition);
                    break;
                default:
                    AddPlane(position, visualDefinition);
                    break;
            }
        }

        void AddPlane(
            GridPosition position,
            TileVisualDefinition visualDefinition)
        {
            var cellCenterX = (position.X + 0.5f) * GameConstants.MapCellWidthMeters;
            var cellCenterZ = (position.Z + 0.5f) * GameConstants.MapCellWidthMeters;
            var halfSize = GameConstants.MapCellWidthMeters * 0.5f;

            AddQuad(
                visualDefinition,
                new Vector3(cellCenterX - halfSize, 0f, cellCenterZ - halfSize),
                new Vector3(cellCenterX - halfSize, 0f, cellCenterZ + halfSize),
                new Vector3(cellCenterX + halfSize, 0f, cellCenterZ + halfSize),
                new Vector3(cellCenterX + halfSize, 0f, cellCenterZ - halfSize));
        }

        void AddBlock(
            GridPosition position,
            TileVisualDefinition visualDefinition)
        {
            var cellCenterX = (position.X + 0.5f) * GameConstants.MapCellWidthMeters;
            var cellCenterZ = (position.Z + 0.5f) * GameConstants.MapCellWidthMeters;
            var halfSize = GameConstants.MapCellWidthMeters * 0.5f;
            var wallHeight = worldMapViewSettingsRepository.GetWorldMapViewSettings().TileHeightMeters;

            AddQuad(
                visualDefinition,
                new Vector3(cellCenterX - halfSize, wallHeight, cellCenterZ - halfSize),
                new Vector3(cellCenterX - halfSize, wallHeight, cellCenterZ + halfSize),
                new Vector3(cellCenterX + halfSize, wallHeight, cellCenterZ + halfSize),
                new Vector3(cellCenterX + halfSize, wallHeight, cellCenterZ - halfSize));

            AddQuad(
                visualDefinition,
                new Vector3(cellCenterX - halfSize, 0f, cellCenterZ + halfSize),
                new Vector3(cellCenterX + halfSize, 0f, cellCenterZ + halfSize),
                new Vector3(cellCenterX + halfSize, wallHeight, cellCenterZ + halfSize),
                new Vector3(cellCenterX - halfSize, wallHeight, cellCenterZ + halfSize));

            AddQuad(
                visualDefinition,
                new Vector3(cellCenterX + halfSize, 0f, cellCenterZ - halfSize),
                new Vector3(cellCenterX - halfSize, 0f, cellCenterZ - halfSize),
                new Vector3(cellCenterX - halfSize, wallHeight, cellCenterZ - halfSize),
                new Vector3(cellCenterX + halfSize, wallHeight, cellCenterZ - halfSize));

            AddQuad(
                visualDefinition,
                new Vector3(cellCenterX + halfSize, 0f, cellCenterZ + halfSize),
                new Vector3(cellCenterX + halfSize, 0f, cellCenterZ - halfSize),
                new Vector3(cellCenterX + halfSize, wallHeight, cellCenterZ - halfSize),
                new Vector3(cellCenterX + halfSize, wallHeight, cellCenterZ + halfSize));

            AddQuad(
                visualDefinition,
                new Vector3(cellCenterX - halfSize, 0f, cellCenterZ - halfSize),
                new Vector3(cellCenterX - halfSize, 0f, cellCenterZ + halfSize),
                new Vector3(cellCenterX - halfSize, wallHeight, cellCenterZ + halfSize),
                new Vector3(cellCenterX - halfSize, wallHeight, cellCenterZ - halfSize));
        }

        void AddRamp(
            GridPosition position,
            TileVisualDefinition visualDefinition)
        {
            var cellCenterX = (position.X + 0.5f) * GameConstants.MapCellWidthMeters;
            var cellCenterZ = (position.Z + 0.5f) * GameConstants.MapCellWidthMeters;
            var halfSize = GameConstants.MapCellWidthMeters * 0.5f;
            var wallHeight = worldMapViewSettingsRepository.GetWorldMapViewSettings().TileHeightMeters;

            AddQuad(
                visualDefinition,
                new Vector3(cellCenterX - halfSize, 0f, cellCenterZ - halfSize),
                new Vector3(cellCenterX - halfSize, wallHeight, cellCenterZ + halfSize),
                new Vector3(cellCenterX + halfSize, wallHeight, cellCenterZ + halfSize),
                new Vector3(cellCenterX + halfSize, 0f, cellCenterZ - halfSize));
        }

        void AddQuad(
            TileVisualDefinition visualDefinition,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3)
        {
            if (!trianglesByKind.TryGetValue(visualDefinition.Kind, out var triangles))
            {
                triangles = new List<int>();
                trianglesByKind.Add(visualDefinition.Kind, triangles);
            }

            if (triangles.Count == 0)
            {
                visualKinds.Add(visualDefinition.Kind);
                materials.Add(mapMaterialSet.Get(visualDefinition.Kind));
            }

            var vertexStart = vertices.Count;
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(0f, 1f));
            uv.Add(new Vector2(1f, 1f));
            uv.Add(new Vector2(1f, 0f));

            triangles.Add(vertexStart);
            triangles.Add(vertexStart + 1);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart + 3);
        }
    }
}

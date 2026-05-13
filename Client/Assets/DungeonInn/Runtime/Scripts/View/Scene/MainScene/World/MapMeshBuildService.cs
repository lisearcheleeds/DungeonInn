using System;
using System.Collections.Generic;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class MapMeshBuildService
    {
        readonly MapTileVisualConfig tileVisualConfig;
        readonly List<Vector3> vertices = new();
        readonly List<Vector2> uv = new();
        readonly Dictionary<TileVisualKind, List<int>> trianglesByKind = new();
        readonly List<Material> materials = new();
        readonly List<TileVisualKind> visualKinds = new();

        public MapMeshBuildService(MapTileVisualConfig tileVisualConfig)
        {
            this.tileVisualConfig = tileVisualConfig ?? throw new ArgumentNullException(nameof(tileVisualConfig));
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
                    AddPlane(position, visualDefinition);
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
            EnsureCapacity(vertices, tileCount * 4);
            EnsureCapacity(uv, tileCount * 4);

            var triangleCount = tileCount * 6;
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

        void AddPlane(
            GridPosition position,
            TileVisualDefinition visualDefinition)
        {
            var cellCenterX = (position.X + 0.5f) * GameConstants.MapCellSizeMeters;
            var cellCenterZ = (position.Z + 0.5f) * GameConstants.MapCellSizeMeters;
            var halfSize = GameConstants.MapCellSizeMeters * 0.5f;
            var vertexStart = vertices.Count;

            vertices.Add(new Vector3(cellCenterX - halfSize, 0f, cellCenterZ - halfSize));
            vertices.Add(new Vector3(cellCenterX - halfSize, 0f, cellCenterZ + halfSize));
            vertices.Add(new Vector3(cellCenterX + halfSize, 0f, cellCenterZ + halfSize));
            vertices.Add(new Vector3(cellCenterX + halfSize, 0f, cellCenterZ - halfSize));

            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(0f, 1f));
            uv.Add(new Vector2(1f, 1f));
            uv.Add(new Vector2(1f, 0f));

            if (!trianglesByKind.TryGetValue(visualDefinition.Kind, out var triangles))
            {
                triangles = new List<int>();
                trianglesByKind.Add(visualDefinition.Kind, triangles);
            }

            if (triangles.Count == 0)
            {
                visualKinds.Add(visualDefinition.Kind);
                materials.Add(visualDefinition.RequireMaterial());
            }

            triangles.Add(vertexStart);
            triangles.Add(vertexStart + 1);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart + 3);
        }
    }
}

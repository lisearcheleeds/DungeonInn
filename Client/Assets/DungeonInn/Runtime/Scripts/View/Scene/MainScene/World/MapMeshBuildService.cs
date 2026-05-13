using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class MapMeshBuildService
    {
        readonly MapTileVisualConfig tileVisualConfig;

        public MapMeshBuildService(MapTileVisualConfig tileVisualConfig)
        {
            this.tileVisualConfig = tileVisualConfig ?? throw new ArgumentNullException(nameof(tileVisualConfig));
        }

        public MapChunkMesh BuildChunk(
            MapLayer layer,
            int startX,
            int startZ,
            int width,
            int depth,
            Func<GridPosition, TileVisualKind> resolveVisualKind)
        {
            if (layer == null)
            {
                throw new ArgumentNullException(nameof(layer));
            }

            if (resolveVisualKind == null)
            {
                throw new ArgumentNullException(nameof(resolveVisualKind));
            }

            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var trianglesByKind = new Dictionary<TileVisualKind, List<int>>();
            var materials = new List<Material>();
            var visualKinds = new List<TileVisualKind>();

            for (var z = startZ; z < startZ + depth; z++)
            {
                for (var x = startX; x < startX + width; x++)
                {
                    var position = new GridPosition(x, z);
                    if (!layer.Contains(position))
                    {
                        continue;
                    }

                    var visualKind = resolveVisualKind(position);
                    var visualDefinition = tileVisualConfig.Get(visualKind);
                    AddPlane(layer, position, visualDefinition, vertices, uv, trianglesByKind, materials, visualKinds);
                }
            }

            var mesh = new Mesh
            {
                name = $"MapChunk_{layer.Id.Value}_{startX}_{startZ}"
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
            return new MapChunkMesh(mesh, materials);
        }

        static void AddPlane(
            MapLayer layer,
            GridPosition position,
            TileVisualDefinition visualDefinition,
            List<Vector3> vertices,
            List<Vector2> uv,
            Dictionary<TileVisualKind, List<int>> trianglesByKind,
            List<Material> materials,
            List<TileVisualKind> visualKinds)
        {
            var cellCenter = layer.GetCellCenter(position);
            var halfSize = layer.CellSizeMeters * 0.5f;
            var vertexStart = vertices.Count;

            vertices.Add(new Vector3(cellCenter.X - halfSize, 0f, cellCenter.Z - halfSize));
            vertices.Add(new Vector3(cellCenter.X - halfSize, 0f, cellCenter.Z + halfSize));
            vertices.Add(new Vector3(cellCenter.X + halfSize, 0f, cellCenter.Z + halfSize));
            vertices.Add(new Vector3(cellCenter.X + halfSize, 0f, cellCenter.Z - halfSize));

            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(0f, 1f));
            uv.Add(new Vector2(1f, 1f));
            uv.Add(new Vector2(1f, 0f));

            if (!trianglesByKind.TryGetValue(visualDefinition.Kind, out var triangles))
            {
                triangles = new List<int>();
                trianglesByKind.Add(visualDefinition.Kind, triangles);
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

using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldMapView : IDisposable
    {
        readonly IGameWorldStateReader gameWorldState;
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly MapTileVisualConfig tileVisualConfig;
        readonly HashSet<int> builtLayerIds = new();

        public WorldMapView(
            IGameWorldStateReader gameWorldState,
            MapLayerViewRegistry layerViewRegistry,
            MapTileVisualConfig tileVisualConfig)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            this.tileVisualConfig = tileVisualConfig ?? throw new ArgumentNullException(nameof(tileVisualConfig));
        }

        public void UpdateVisuals()
        {
            BuildMissingLayerTiles();
        }

        public void Dispose()
        {
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
            for (var z = 0; z < layer.Depth; z++)
            {
                for (var x = 0; x < layer.Width; x++)
                {
                    var position = new GridPosition(x, z);
                    var visualKind = gameWorldState.GroundMap.IsWalkable(position)
                        ? TileVisualKind.GroundWalkable
                        : TileVisualKind.GroundBlocked;
                    CreateTile(layerRoot, layer, position, tileVisualConfig.Get(visualKind));
                }
            }
        }

        void BuildDungeonTiles(DungeonFloor floor)
        {
            var layerRoot = CreateLayerRoot($"DungeonFloor{floor.FloorIndex}", floor.Layer.Id);
            for (var z = 0; z < floor.Layer.Depth; z++)
            {
                for (var x = 0; x < floor.Layer.Width; x++)
                {
                    var position = new GridPosition(x, z);
                    if (!floor.IsWalkable(position))
                    {
                        continue;
                    }

                    CreateTile(layerRoot, floor.Layer, position, tileVisualConfig.Get(TileVisualKind.DungeonWalkable));
                }
            }
        }

        Transform CreateLayerRoot(string layerName, MapLayerId layerId)
        {
            return layerViewRegistry.GetOrCreateTileRoot(layerId, layerName);
        }

        void CreateTile(Transform layerRoot, MapLayer layer, GridPosition position, TileVisualDefinition visualDefinition)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var cellCenter = layer.GetCellCenter(position);
            tile.name = $"Tile_{position.X}_{position.Z}";
            tile.transform.SetParent(layerRoot, false);
            tile.transform.localPosition = new Vector3(cellCenter.X, 0f, cellCenter.Z);
            tile.transform.localScale = Vector3.one * (GameConstants.MapCellSizeMeters / 10f);
            RemoveCollider(tile);
            ApplyMaterial(tile, visualDefinition.RequireMaterial());
        }

        static void ApplyMaterial(GameObject target, Material material)
        {
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }
        }
    }
}

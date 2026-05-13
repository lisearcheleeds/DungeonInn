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
        readonly WorldViewRoot viewRoot;
        readonly LayerPositionViewMapper positionMapper;
        readonly HashSet<int> builtLayerIds = new();
        readonly Material groundWalkableMaterial;
        readonly Material groundBlockedMaterial;
        readonly Material dungeonWalkableMaterial;

        public WorldMapView(
            IGameWorldStateReader gameWorldState,
            WorldViewRoot viewRoot,
            LayerPositionViewMapper positionMapper)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.viewRoot = viewRoot ?? throw new ArgumentNullException(nameof(viewRoot));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            groundWalkableMaterial = WorldDebugMaterialFactory.Create(new Color(0.24f, 0.32f, 0.24f, 0.45f));
            groundBlockedMaterial = WorldDebugMaterialFactory.Create(new Color(0.25f, 0.25f, 0.25f, 0.65f));
            dungeonWalkableMaterial = WorldDebugMaterialFactory.Create(new Color(0.18f, 0.20f, 0.26f, 0.65f));
        }

        public void UpdateVisuals()
        {
            BuildMissingLayerTiles();
        }

        public void Dispose()
        {
            WorldDebugMaterialFactory.Dispose(groundWalkableMaterial);
            WorldDebugMaterialFactory.Dispose(groundBlockedMaterial);
            WorldDebugMaterialFactory.Dispose(dungeonWalkableMaterial);
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
                    var material = gameWorldState.GroundMap.IsWalkable(position)
                        ? groundWalkableMaterial
                        : groundBlockedMaterial;
                    CreateTile(layerRoot, layer, position, material);
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

                    CreateTile(layerRoot, floor.Layer, position, dungeonWalkableMaterial);
                }
            }
        }

        Transform CreateLayerRoot(string layerName, MapLayerId layerId)
        {
            return viewRoot.CreateMapLayerRoot(
                layerName,
                new Vector3(0f, positionMapper.ResolveLayerY(layerId), 0f));
        }

        void CreateTile(Transform layerRoot, MapLayer layer, GridPosition position, Material material)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
            tile.name = $"Tile_{position.X}_{position.Z}";
            tile.transform.SetParent(layerRoot, false);
            tile.transform.position = positionMapper.ToUnityPosition(layer.GetCellCenter(position));
            tile.transform.localScale = Vector3.one * (GameConstants.MapCellSizeMeters / 10f);
            RemoveCollider(tile);
            ApplyMaterial(tile, material);
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

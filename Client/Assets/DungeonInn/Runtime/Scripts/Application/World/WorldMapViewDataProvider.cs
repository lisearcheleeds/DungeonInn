using System;
using System.Collections.Generic;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.World
{
    public sealed class WorldMapViewDataProvider : IWorldMapViewDataProvider
    {
        readonly IGameWorldStateReader worldState;
        readonly Dictionary<int, WorldMapLayerViewData> cachedLayers = new();
        readonly List<WorldMapCellViewKind> cellKindBuffer = new();

        [Inject]
        public WorldMapViewDataProvider(IGameWorldStateReader worldState)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
        }

        public WorldMapLayerViewData GetLayer(MapLayerId layerId)
        {
            if (layerId.Value == MapLayerId.Ground.Value)
            {
                return GetOrCreateGroundLayer(worldState.GroundMap);
            }

            var floorIndex = layerId.Value;
            if (worldState.Dungeon.Floors.TryGetValue(floorIndex, out var floor))
            {
                return GetOrCreateDungeonLayer(floor);
            }

            throw new InvalidOperationException($"Layer not found: {layerId.Value}");
        }

        public void InvalidateLayer(MapLayerId layerId)
        {
            cachedLayers.Remove(layerId.Value);
        }

        WorldMapLayerViewData GetOrCreateGroundLayer(GroundMap groundMap)
        {
            if (cachedLayers.TryGetValue(MapLayerId.Ground.Value, out var layerData))
            {
                return layerData;
            }

            var width = groundMap.Layer.Width;
            var height = groundMap.Layer.Depth;
            cellKindBuffer.Clear();
            for (var z = 0; z < height; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    var position = new GridPosition(x, z);
                    cellKindBuffer.Add(groundMap.IsWalkable(position)
                        ? WorldMapCellViewKind.GroundWalkable
                        : WorldMapCellViewKind.GroundBlocked);
                }
            }

            layerData = new WorldMapLayerViewData(MapLayerId.Ground, "Ground", width, height, groundMap.Layer.CellSizeMeters, cellKindBuffer);
            cachedLayers.Add(MapLayerId.Ground.Value, layerData);
            return layerData;
        }

        WorldMapLayerViewData GetOrCreateDungeonLayer(DungeonFloor floor)
        {
            var layerId = MapLayerId.DungeonFloor(floor.FloorIndex);
            if (cachedLayers.TryGetValue(layerId.Value, out var layerData))
            {
                return layerData;
            }

            var width = floor.Layer.Width;
            var height = floor.Layer.Depth;
            cellKindBuffer.Clear();
            for (var z = 0; z < height; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    cellKindBuffer.Add(ResolveDungeonCellKind(floor, new GridPosition(x, z)));
                }
            }

            layerData = new WorldMapLayerViewData(
                layerId,
                $"DungeonFloor{floor.FloorIndex}",
                width,
                height,
                floor.Layer.CellSizeMeters,
                cellKindBuffer);
            cachedLayers.Add(layerId.Value, layerData);
            return layerData;
        }

        static WorldMapCellViewKind ResolveDungeonCellKind(DungeonFloor floor, GridPosition position)
        {
            if (floor.IsStairPosition(position, DungeonStairType.Up))
            {
                return WorldMapCellViewKind.StairUp;
            }

            if (floor.IsStairPosition(position, DungeonStairType.Down))
            {
                return WorldMapCellViewKind.StairDown;
            }

            return floor.IsWalkable(position)
                ? WorldMapCellViewKind.DungeonWalkable
                : WorldMapCellViewKind.DungeonBlocked;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 指定階層のダンジョンフロアを決定的な仮生成ロジックで作成するユースケース。
    /// </summary>
    public sealed class GenerateDungeonFloorUseCase
    {
        /// <summary>
        /// 外周壁、内部通路、仮部屋、上下階段を持つフロアを生成してダンジョンへ追加する。
        /// </summary>
        public UniTask<DungeonFloor> ExecuteAsync(
            Dungeon dungeon,
            int floorIndex,
            IReadOnlyList<DungeonDepthBandConfig> depthBandConfigs)
        {
            if (dungeon.HasFloor(floorIndex))
            {
                throw new InvalidOperationException("Dungeon floor already exists.");
            }

            var settings = ResolveSettings(floorIndex, depthBandConfigs);
            var layer = new MapLayer(
                MapLayerId.DungeonFloor(floorIndex),
                GameConstants.DungeonFloorWidth,
                GameConstants.DungeonFloorDepth,
                GameConstants.MapCellSizeMeters);
            var cells = CreateBaseCells(layer);
            var random = new Random(dungeon.Seed + floorIndex * GameConstants.DungeonFloorSeedMultiplier);
            var upStair = new DungeonStair(DungeonStairType.Up, FindWalkablePosition(layer, cells, random));
            var downStair = new DungeonStair(DungeonStairType.Down, FindWalkablePosition(layer, cells, random));
            var floor = new DungeonFloor(
                floorIndex,
                layer,
                cells,
                upStair,
                downStair,
                settings);

            dungeon.AddFloor(floor);
            return UniTask.FromResult(floor);
        }

        static DungeonFloorGenerationSettings ResolveSettings(
            int floorIndex,
            IReadOnlyList<DungeonDepthBandConfig> depthBandConfigs)
        {
            if (floorIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(floorIndex));
            }

            var config = depthBandConfigs?.FirstOrDefault(x => x.Contains(floorIndex));
            if (config != null)
            {
                return config.GenerationSettings;
            }

            return new DungeonFloorGenerationSettings(
                roomCount: GameConstants.DungeonDefaultRoomCountBase
                    + floorIndex / GameConstants.DungeonRoomCountFloorsPerIncrement,
                minCorridorLength: GameConstants.DungeonDefaultMinCorridorLength,
                maxCorridorLength: GameConstants.DungeonDefaultMaxCorridorLengthBase
                    + floorIndex / GameConstants.DungeonMaxCorridorLengthFloorsPerIncrement,
                themeId: floorIndex / GameConstants.DungeonThemeFloorsPerTheme);
        }

        static DungeonCell[] CreateBaseCells(MapLayer layer)
        {
            var cells = new DungeonCell[layer.Width * layer.Depth];
            for (var z = 0; z < layer.Depth; z++)
            {
                for (var x = 0; x < layer.Width; x++)
                {
                    var position = new GridPosition(x, z);
                    var type = IsOuterWall(layer, position) ? DungeonCellType.Wall : DungeonCellType.Corridor;
                    cells[ToIndex(layer, position)] = new DungeonCell(position, type);
                }
            }

            FillRoom(
                cells,
                layer,
                layer.Width / 2 - GameConstants.DungeonPlaceholderCenterRoomSizeCells / 2,
                layer.Depth / 2 - GameConstants.DungeonPlaceholderCenterRoomSizeCells / 2,
                GameConstants.DungeonPlaceholderCenterRoomSizeCells,
                GameConstants.DungeonPlaceholderCenterRoomSizeCells);
            return cells;
        }

        static bool IsOuterWall(MapLayer layer, GridPosition position)
        {
            return position.X == 0
                || position.Z == 0
                || position.X == layer.Width - 1
                || position.Z == layer.Depth - 1;
        }

        static void FillRoom(DungeonCell[] cells, MapLayer layer, int originX, int originZ, int width, int depth)
        {
            for (var z = originZ; z < originZ + depth; z++)
            {
                for (var x = originX; x < originX + width; x++)
                {
                    var position = new GridPosition(x, z);
                    if (layer.Contains(position))
                    {
                        cells[ToIndex(layer, position)].ChangeType(DungeonCellType.Room);
                    }
                }
            }
        }

        static GridPosition FindWalkablePosition(MapLayer layer, DungeonCell[] cells, Random random)
        {
            for (var attempt = 0; attempt < GameConstants.DungeonStairPlacementMaxAttempts; attempt++)
            {
                var position = new GridPosition(
                    random.Next(1, layer.Width - 1),
                    random.Next(1, layer.Depth - 1));
                if (cells[ToIndex(layer, position)].IsWalkable)
                {
                    return position;
                }
            }

            return new GridPosition(layer.Width / 2, layer.Depth / 2);
        }

        static int ToIndex(MapLayer layer, GridPosition position)
        {
            return position.Z * layer.Width + position.X;
        }
    }
}

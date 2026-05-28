using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.World;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Dungeons
{
    /// <summary>
    /// 持E��階層のダンジョンフロアめESection 経路式�E決定的な生�EロジチE��で作�Eするユースケース、E    /// </summary>
    public sealed class GenerateDungeonFloorUseCase
    {
        const int ReferencePointPlacementMaxAttempts = 32;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        readonly IMasterRepository masterRepository;
        readonly AssignDungeonRoomRolesUseCase assignDungeonRoomRolesUseCase;

        [Inject]
        public GenerateDungeonFloorUseCase(
            IWorldGameSettingsRepository worldGameSettingsRepository,
            IMasterRepository masterRepository,
            AssignDungeonRoomRolesUseCase assignDungeonRoomRolesUseCase)
        {
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.assignDungeonRoomRolesUseCase =
                assignDungeonRoomRolesUseCase ?? throw new ArgumentNullException(nameof(assignDungeonRoomRolesUseCase));
        }

        /// <summary>
        /// Section 経路、E��路、E��屋、上下階段を持つフロアを生成してダンジョンへ追加する、E        /// </summary>
        public UniTask<DungeonFloor> ExecuteAsync(
            Dungeon dungeon,
            int floorIndex)
        {
            if (dungeon.HasFloor(floorIndex))
            {
                throw new InvalidOperationException("Dungeon floor already exists.");
            }

            var generationSettings = worldGameSettingsRepository.GetDungeonMapGenerationSettings();
            var settings = ResolveSettings(floorIndex);
            var layer = new MapLayer(
                MapLayerId.DungeonFloor(floorIndex),
                generationSettings.FloorWidth,
                generationSettings.FloorDepth,
                GameConstants.MapCellWidthMeters);
            var random = new Random(dungeon.Seed + floorIndex * GameConstants.DungeonFloorSeedMultiplier);
            var blueprint = CreateBlueprint(layer, settings, generationSettings, random);
            var rooms = assignDungeonRoomRolesUseCase.Execute(floorIndex, blueprint.Rooms);
            var cells = CreateCells(layer, blueprint);
            var upStair = new DungeonStair(DungeonStairType.Up, blueprint.UpStairPosition);
            var downStair = new DungeonStair(DungeonStairType.Down, blueprint.DownStairPosition);
            var floor = new DungeonFloor(
                floorIndex,
                layer,
                cells,
                upStair,
                downStair,
                rooms,
                settings);

            dungeon.AddFloor(floor);
            return UniTask.FromResult(floor);
        }

        DungeonFloorGenerationSettings ResolveSettings(int floorIndex)
        {
            if (floorIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(floorIndex));
            }

            return masterRepository.GetDungeonDepthBandMasterForFloor(floorIndex).GenerationSettings;
        }

        DungeonFloorBlueprint CreateBlueprint(
            MapLayer layer,
            DungeonFloorGenerationSettings settings,
            DungeonMapGenerationSettings generationSettings,
            Random random)
        {
            var sectionWidth = layer.Width / generationSettings.SectionSizeCells;
            var sectionDepth = layer.Depth / generationSettings.SectionSizeCells;
            var startSection = GetRandomEdgeSection(sectionWidth, sectionDepth, random);
            var endSection = GetRandomEdgeSection(sectionWidth, sectionDepth, random);
            var sectionCount = sectionWidth * sectionDepth;

            if (startSection == endSection)
            {
                endSection = (endSection + 1) % sectionCount;
            }

            var sectionPath = CreateFastSectionPath(startSection, endSection, sectionWidth, sectionDepth);
            sectionPath = DistortSectionPath(
                sectionPath,
                sectionWidth,
                sectionDepth,
                generationSettings.PathDistortBaseStrength,
                random);

            var referencePointsBySection = CreateReferencePointsBySection(
                sectionPath,
                sectionWidth,
                generationSettings,
                random);
            var carvedCells = new HashSet<GridPosition>();
            var roomCells = new HashSet<GridPosition>();
            var rooms = new List<DungeonRoom>();

            CarveMainRoute(referencePointsBySection, carvedCells, random);
            CarveRooms(referencePointsBySection, carvedCells, roomCells, rooms, generationSettings, random);

            return new DungeonFloorBlueprint(
                carvedCells,
                roomCells,
                rooms,
                referencePointsBySection[0][0],
                referencePointsBySection[referencePointsBySection.Count - 1][0]);
        }

        static DungeonCell[] CreateCells(MapLayer layer, DungeonFloorBlueprint blueprint)
        {
            var cells = new DungeonCell[layer.Width * layer.Depth];
            for (var z = 0; z < layer.Depth; z++)
            {
                for (var x = 0; x < layer.Width; x++)
                {
                    var position = new GridPosition(x, z);
                    var type = ResolveCellType(blueprint, position);
                    cells[ToIndex(layer, position)] = new DungeonCell(position, type);
                }
            }

            return cells;
        }

        static DungeonCellType ResolveCellType(DungeonFloorBlueprint blueprint, GridPosition position)
        {
            if (blueprint.RoomCells.Contains(position))
            {
                return DungeonCellType.Room;
            }

            if (blueprint.CarvedCells.Contains(position))
            {
                return DungeonCellType.Corridor;
            }

            return DungeonCellType.Wall;
        }

        static int GetRandomEdgeSection(int sectionWidth, int sectionDepth, Random random)
        {
            var edgeCount = sectionWidth * 2 + sectionDepth * 2 - 4;
            var edgeIndex = random.Next(0, edgeCount);

            if (edgeIndex < sectionWidth)
            {
                return edgeIndex;
            }

            if (edgeIndex < sectionWidth * 2)
            {
                return sectionWidth * (sectionDepth - 1) + edgeIndex - sectionWidth;
            }

            var sideIndex = edgeIndex - sectionWidth * 2;
            if (sideIndex < sectionDepth - 2)
            {
                return sectionWidth * (sideIndex + 1);
            }

            sideIndex -= sectionDepth - 2;
            return sectionWidth * (sideIndex + 1) + sectionWidth - 1;
        }

        static List<int> CreateFastSectionPath(int start, int end, int sectionWidth, int sectionDepth)
        {
            var result = new List<int> { start };
            var endPosition = ToSectionPosition(end, sectionWidth);

            while (result[result.Count - 1] != end)
            {
                var current = result[result.Count - 1];
                var currentPosition = ToSectionPosition(current, sectionWidth);
                var xDistance = Math.Abs(currentPosition.X - endPosition.X);
                var zDistance = Math.Abs(currentPosition.Z - endPosition.Z);

                if (zDistance < xDistance)
                {
                    result.Add(currentPosition.X < endPosition.X
                        ? MoveRightSection(current, sectionWidth, sectionDepth).Value
                        : MoveLeftSection(current, sectionWidth, sectionDepth).Value);
                    continue;
                }

                result.Add(currentPosition.Z < endPosition.Z
                    ? MoveDownSection(current, sectionWidth, sectionDepth).Value
                    : MoveUpSection(current, sectionWidth, sectionDepth).Value);
            }

            return result;
        }

        static List<int> DistortSectionPath(
            List<int> path,
            int sectionWidth,
            int sectionDepth,
            int strength,
            Random random)
        {
            var result = new List<int>(path);

            for (var strengthCount = 0; strengthCount < strength; strengthCount++)
            {
                for (var i = 0; i < result.Count - 1; i++)
                {
                    var current = result[i];
                    var next = result[i + 1];
                    var insertSections = CreateDistortion(current, next, result, sectionWidth, sectionDepth, random);
                    if (insertSections == null)
                    {
                        continue;
                    }

                    result.InsertRange(i + 1, insertSections);
                    i += insertSections.Count;
                }
            }

            return result;
        }

        static List<int> CreateDistortion(
            int current,
            int next,
            List<int> path,
            int sectionWidth,
            int sectionDepth,
            Random random)
        {
            var currentPosition = ToSectionPosition(current, sectionWidth);
            var nextPosition = ToSectionPosition(next, sectionWidth);
            var dx = nextPosition.X - currentPosition.X;
            var dz = nextPosition.Z - currentPosition.Z;

            if (dx == 0 && dz == -1)
            {
                return random.Next(0, 2) == 0
                    ? TryCreateDistortion(path, current, next, MoveRightSection, MoveUpSection, MoveLeftSection, sectionWidth, sectionDepth)
                    : TryCreateDistortion(path, current, next, MoveLeftSection, MoveUpSection, MoveRightSection, sectionWidth, sectionDepth);
            }

            if (dx == 0 && dz == 1)
            {
                return random.Next(0, 2) == 0
                    ? TryCreateDistortion(path, current, next, MoveLeftSection, MoveDownSection, MoveRightSection, sectionWidth, sectionDepth)
                    : TryCreateDistortion(path, current, next, MoveRightSection, MoveDownSection, MoveLeftSection, sectionWidth, sectionDepth);
            }

            if (dx == 1 && dz == 0)
            {
                return random.Next(0, 2) == 0
                    ? TryCreateDistortion(path, current, next, MoveDownSection, MoveRightSection, MoveUpSection, sectionWidth, sectionDepth)
                    : TryCreateDistortion(path, current, next, MoveUpSection, MoveRightSection, MoveDownSection, sectionWidth, sectionDepth);
            }

            if (dx == -1 && dz == 0)
            {
                return random.Next(0, 2) == 0
                    ? TryCreateDistortion(path, current, next, MoveUpSection, MoveLeftSection, MoveDownSection, sectionWidth, sectionDepth)
                    : TryCreateDistortion(path, current, next, MoveDownSection, MoveLeftSection, MoveUpSection, sectionWidth, sectionDepth);
            }

            return null;
        }

        static List<int> TryCreateDistortion(
            List<int> path,
            int current,
            int next,
            Func<int, int, int, int?> move1,
            Func<int, int, int, int?> move2,
            Func<int, int, int, int?> move3,
            int sectionWidth,
            int sectionDepth)
        {
            var first = move1(current, sectionWidth, sectionDepth);
            if (!first.HasValue || path.Contains(first.Value))
            {
                return null;
            }

            var second = move2(first.Value, sectionWidth, sectionDepth);
            if (!second.HasValue || path.Contains(second.Value))
            {
                return null;
            }

            var third = move3(second.Value, sectionWidth, sectionDepth);
            if (!third.HasValue || third.Value != next)
            {
                return null;
            }

            return new List<int> { first.Value, second.Value };
        }

        List<List<GridPosition>> CreateReferencePointsBySection(
            List<int> sectionPath,
            int sectionWidth,
            DungeonMapGenerationSettings generationSettings,
            Random random)
        {
            var result = new List<List<GridPosition>>();

            foreach (var sectionIndex in sectionPath)
            {
                var referencePointCount = 1 + random.Next(0, generationSettings.ExtraReferencePointMaxCount + 1);
                var points = new List<GridPosition>();
                var sectionPosition = ToSectionPosition(sectionIndex, sectionWidth);

                points.Add(CreateReferencePoint(sectionPosition, generationSettings, random));
                for (var i = 1; i < referencePointCount; i++)
                {
                    if (TryCreateAdditionalReferencePoint(sectionPosition, points, generationSettings, random, out var point))
                    {
                        points.Add(point);
                    }
                }

                result.Add(points);
            }

            return result;
        }

        GridPosition CreateReferencePoint(
            GridPosition sectionPosition,
            DungeonMapGenerationSettings generationSettings,
            Random random)
        {
            var min = generationSettings.SectionMarginCells;
            var max = generationSettings.SectionSizeCells - generationSettings.SectionMarginCells;
            return new GridPosition(
                sectionPosition.X * generationSettings.SectionSizeCells + random.Next(min, max),
                sectionPosition.Z * generationSettings.SectionSizeCells + random.Next(min, max));
        }

        bool TryCreateAdditionalReferencePoint(
            GridPosition sectionPosition,
            IReadOnlyList<GridPosition> existingPoints,
            DungeonMapGenerationSettings generationSettings,
            Random random,
            out GridPosition point)
        {
            for (var i = 0; i < ReferencePointPlacementMaxAttempts; i++)
            {
                var candidate = CreateReferencePoint(sectionPosition, generationSettings, random);
                if (IsSeparatedFromAll(candidate, existingPoints))
                {
                    point = candidate;
                    return true;
                }
            }

            point = default;
            return false;
        }

        static bool IsSeparatedFromAll(GridPosition candidate, IReadOnlyList<GridPosition> existingPoints)
        {
            for (var i = 0; i < existingPoints.Count; i++)
            {
                if (Math.Abs(candidate.X - existingPoints[i].X) <= 1
                    || Math.Abs(candidate.Z - existingPoints[i].Z) <= 1)
                {
                    return false;
                }
            }

            return true;
        }

        static void CarveMainRoute(List<List<GridPosition>> referencePointsBySection, HashSet<GridPosition> carvedCells, Random random)
        {
            for (var i = 0; i < referencePointsBySection.Count; i++)
            {
                for (var t = 0; t < referencePointsBySection[i].Count - 1; t++)
                {
                    CarveElbowPath(
                        referencePointsBySection[i][t],
                        referencePointsBySection[i][t + 1],
                        random.Next(0, 2) == 0,
                        carvedCells);
                }

                if (i < referencePointsBySection.Count - 1)
                {
                    CarveElbowPath(
                        referencePointsBySection[i][0],
                        referencePointsBySection[i + 1][0],
                        random.Next(0, 2) == 0,
                        carvedCells);
                }
            }
        }

        void CarveRooms(
            List<List<GridPosition>> referencePointsBySection,
            HashSet<GridPosition> carvedCells,
            HashSet<GridPosition> roomCells,
            List<DungeonRoom> rooms,
            DungeonMapGenerationSettings generationSettings,
            Random random)
        {
            var referencePoints = referencePointsBySection
                .SelectMany((points, routeDepth) => points.Select(point => new ReferencePoint(point, routeDepth)))
                .ToList();

            for (var i = 0; i < referencePoints.Count; i++)
            {
                var referencePoint = referencePoints[i];
                var center = referencePoint.Position;
                var width = random.Next(generationSettings.RoomMinSizeCells, generationSettings.RoomMaxSizeCells + 1);
                var depth = random.Next(generationSettings.RoomMinSizeCells, generationSettings.RoomMaxSizeCells + 1);
                var cells = new List<GridPosition>();

                for (var z = center.Z - depth / 2; z <= center.Z + depth / 2; z++)
                {
                    for (var x = center.X - width / 2; x <= center.X + width / 2; x++)
                    {
                        if (x < 1 || generationSettings.FloorWidth - 1 <= x || z < 1 || generationSettings.FloorDepth - 1 <= z)
                        {
                            continue;
                        }

                        var position = new GridPosition(x, z);
                        carvedCells.Add(position);
                        roomCells.Add(position);
                        cells.Add(position);
                    }
                }

                rooms.Add(
                    new DungeonRoom(
                        i,
                        center,
                        width,
                        depth,
                        referencePoint.RouteDepth,
                        cells));
            }
        }

        static void CarveElbowPath(
            GridPosition from,
            GridPosition to,
            bool horizontalFirst,
            HashSet<GridPosition> carvedCells)
        {
            var current = from;
            carvedCells.Add(current);

            if (horizontalFirst)
            {
                CarveHorizontal(ref current, to.X, carvedCells);
                CarveVertical(ref current, to.Z, carvedCells);
                return;
            }

            CarveVertical(ref current, to.Z, carvedCells);
            CarveHorizontal(ref current, to.X, carvedCells);
        }

        static void CarveHorizontal(ref GridPosition current, int targetX, HashSet<GridPosition> carvedCells)
        {
            var step = current.X < targetX ? 1 : -1;
            while (current.X != targetX)
            {
                current = new GridPosition(current.X + step, current.Z);
                carvedCells.Add(current);
            }
        }

        static void CarveVertical(ref GridPosition current, int targetZ, HashSet<GridPosition> carvedCells)
        {
            var step = current.Z < targetZ ? 1 : -1;
            while (current.Z != targetZ)
            {
                current = new GridPosition(current.X, current.Z + step);
                carvedCells.Add(current);
            }
        }

        static GridPosition ToSectionPosition(int sectionIndex, int sectionWidth)
        {
            return new GridPosition(sectionIndex % sectionWidth, sectionIndex / sectionWidth);
        }

        static int? MoveUpSection(int index, int sectionWidth, int sectionDepth)
        {
            return 0 <= index - sectionWidth ? index - sectionWidth : null;
        }

        static int? MoveDownSection(int index, int sectionWidth, int sectionDepth)
        {
            return index + sectionWidth < sectionWidth * sectionDepth ? index + sectionWidth : null;
        }

        static int? MoveRightSection(int index, int sectionWidth, int sectionDepth)
        {
            return (index + 1) % sectionWidth != 0 ? index + 1 : null;
        }

        static int? MoveLeftSection(int index, int sectionWidth, int sectionDepth)
        {
            return index % sectionWidth != 0 ? index - 1 : null;
        }

        static int ToIndex(MapLayer layer, GridPosition position)
        {
            return position.Z * layer.Width + position.X;
        }

        sealed class DungeonFloorBlueprint
        {
            public HashSet<GridPosition> CarvedCells { get; }
            public HashSet<GridPosition> RoomCells { get; }
            public IReadOnlyList<DungeonRoom> Rooms { get; }
            public GridPosition UpStairPosition { get; }
            public GridPosition DownStairPosition { get; }

            public DungeonFloorBlueprint(
                HashSet<GridPosition> carvedCells,
                HashSet<GridPosition> roomCells,
                IReadOnlyList<DungeonRoom> rooms,
                GridPosition upStairPosition,
                GridPosition downStairPosition)
            {
                CarvedCells = carvedCells ?? throw new ArgumentNullException(nameof(carvedCells));
                RoomCells = roomCells ?? throw new ArgumentNullException(nameof(roomCells));
                Rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
                UpStairPosition = upStairPosition;
                DownStairPosition = downStairPosition;
            }
        }

        readonly struct ReferencePoint
        {
            public GridPosition Position { get; }
            public int RouteDepth { get; }

            public ReferencePoint(GridPosition position, int routeDepth)
            {
                Position = position;
                RouteDepth = routeDepth;
            }
        }
    }
}

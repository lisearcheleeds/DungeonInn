using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Dungeons
{
    /// <summary>
    /// 地上�EチE�Eの初期状態を作�Eするユースケース、E    /// </summary>
    public sealed class InitializeWorldMapUseCase
    {
        /// <summary>
        /// 定数で定義された地上�EチE�Eを作�Eし、中央のダンジョン入口と周辺施設の占有セルを設定する、E        /// </summary>
        public UniTask<GroundMap> ExecuteAsync()
        {
            var layer = new MapLayer(
                MapLayerId.Ground,
                GameConstants.GroundMapWidth,
                GameConstants.GroundMapDepth,
                GameConstants.MapCellSizeMeters);
            var cells = CreateOpenCells(layer);
            var dungeonEntrance = new GridPosition(
                GameConstants.GroundMapWidth / 2,
                GameConstants.GroundMapDepth / 2);
            var innerOrigin = GameConstants.GroundMapWidth / 2
                - GameConstants.GroundFacilityBuildingInnerOffsetCells;
            var outerOrigin = GameConstants.GroundMapWidth / 2
                + GameConstants.GroundFacilityBuildingOuterOffsetCells;

            SetCell(cells, layer, dungeonEntrance, GroundCellType.DungeonEntrance, MapCellBlockType.Walkable);
            FillRectangle(
                cells,
                layer,
                innerOrigin,
                innerOrigin,
                GameConstants.GroundFacilityBuildingSizeCells,
                GameConstants.GroundFacilityBuildingSizeCells,
                GroundCellType.Building,
                MapCellBlockType.Blocked);
            FillRectangle(
                cells,
                layer,
                outerOrigin,
                innerOrigin,
                GameConstants.GroundFacilityBuildingSizeCells,
                GameConstants.GroundFacilityBuildingSizeCells,
                GroundCellType.Building,
                MapCellBlockType.Blocked);
            FillRectangle(
                cells,
                layer,
                innerOrigin,
                outerOrigin,
                GameConstants.GroundFacilityBuildingSizeCells,
                GameConstants.GroundFacilityBuildingSizeCells,
                GroundCellType.Building,
                MapCellBlockType.Blocked);
            FillRectangle(
                cells,
                layer,
                outerOrigin,
                outerOrigin,
                GameConstants.GroundFacilityBuildingSizeCells,
                GameConstants.GroundFacilityBuildingSizeCells,
                GroundCellType.Building,
                MapCellBlockType.Blocked);

            BuildTownWalls(cells, layer);

            return UniTask.FromResult(new GroundMap(layer, dungeonEntrance, cells));
        }

        // Town wall 3 cells from each edge, 2-cell-wide openings at center of each side.
        static void BuildTownWalls(GroundCell[] cells, MapLayer layer)
        {
            const int wallOffset = 3;
            const int openingHalf = 1;
            var cx = layer.Width / 2;
            var cz = layer.Depth / 2;
            var wallS = wallOffset;
            var wallN = layer.Depth - 1 - wallOffset;
            var wallW = wallOffset;
            var wallE = layer.Width - 1 - wallOffset;

            // South wall (z = wallS): x = wallW..cx-openingHalf-1 and cx+openingHalf..wallE
            FillRectangle(cells, layer, wallW, wallS, cx - openingHalf - wallW, 1, GroundCellType.TownWall, MapCellBlockType.Blocked);
            FillRectangle(cells, layer, cx + openingHalf, wallS, wallE - (cx + openingHalf) + 1, 1, GroundCellType.TownWall, MapCellBlockType.Blocked);
            // North wall (z = wallN)
            FillRectangle(cells, layer, wallW, wallN, cx - openingHalf - wallW, 1, GroundCellType.TownWall, MapCellBlockType.Blocked);
            FillRectangle(cells, layer, cx + openingHalf, wallN, wallE - (cx + openingHalf) + 1, 1, GroundCellType.TownWall, MapCellBlockType.Blocked);
            // West wall (x = wallW): z = wallS+1..cz-openingHalf-1 and cz+openingHalf..wallN-1
            FillRectangle(cells, layer, wallW, wallS + 1, 1, cz - openingHalf - (wallS + 1), GroundCellType.TownWall, MapCellBlockType.Blocked);
            FillRectangle(cells, layer, wallW, cz + openingHalf, 1, wallN - (cz + openingHalf), GroundCellType.TownWall, MapCellBlockType.Blocked);
            // East wall (x = wallE)
            FillRectangle(cells, layer, wallE, wallS + 1, 1, cz - openingHalf - (wallS + 1), GroundCellType.TownWall, MapCellBlockType.Blocked);
            FillRectangle(cells, layer, wallE, cz + openingHalf, 1, wallN - (cz + openingHalf), GroundCellType.TownWall, MapCellBlockType.Blocked);
        }

        static GroundCell[] CreateOpenCells(MapLayer layer)
        {
            var cells = new GroundCell[layer.Width * layer.Depth];
            for (var z = 0; z < layer.Depth; z++)
            {
                for (var x = 0; x < layer.Width; x++)
                {
                    var position = new GridPosition(x, z);
                    cells[ToIndex(layer, position)] = new GroundCell(
                        position,
                        GroundCellType.Open,
                        MapCellBlockType.Walkable);
                }
            }

            return cells;
        }

        static void FillRectangle(
            GroundCell[] cells,
            MapLayer layer,
            int originX,
            int originZ,
            int width,
            int depth,
            GroundCellType type,
            MapCellBlockType blockType)
        {
            for (var z = originZ; z < originZ + depth; z++)
            {
                for (var x = originX; x < originX + width; x++)
                {
                    SetCell(cells, layer, new GridPosition(x, z), type, blockType);
                }
            }
        }

        static void SetCell(
            GroundCell[] cells,
            MapLayer layer,
            GridPosition position,
            GroundCellType type,
            MapCellBlockType blockType)
        {
            if (!layer.Contains(position))
            {
                return;
            }

            cells[ToIndex(layer, position)].ChangeType(type, blockType);
        }

        static int ToIndex(MapLayer layer, GridPosition position)
        {
            return position.Z * layer.Width + position.X;
        }
    }
}

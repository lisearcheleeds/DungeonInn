using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 地上マップの初期状態を作成するユースケース。
    /// </summary>
    public sealed class InitializeWorldMapUseCase
    {
        /// <summary>
        /// 定数で定義された地上マップを作成し、中央のダンジョン入口と周辺施設の占有セルを設定する。
        /// </summary>
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

            return UniTask.FromResult(new GroundMap(layer, dungeonEntrance, cells));
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

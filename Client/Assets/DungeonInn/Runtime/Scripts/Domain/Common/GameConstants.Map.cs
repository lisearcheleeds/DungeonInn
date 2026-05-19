namespace DungeonInn.Domain.Common
{
    public static partial class GameConstants
    {
        // グリッド1マスの幅。単位はメートル。
        public const float MapCellWidthMeters = 5f;

        // ブロック・ランプタイルの高さ。単位はメートル。
        public const float MapTileHeightMeters = 2f;

        // 地上マップの横幅。単位はグリッドセル数。
        public const int GroundMapWidth = 30;

        // 地上マップの奥行き。単位はグリッドセル数。
        public const int GroundMapDepth = 30;

        // 地上施設建物の一辺の長さ。単位はグリッドセル数。
        public const int GroundFacilityBuildingSizeCells = 5;

        // ダンジョン入口の中心から内側側施設を配置する時のセルオフセット。
        public const int GroundFacilityBuildingInnerOffsetCells = 7;

        // ダンジョン入口の中心から外側側施設を配置する時のセルオフセット。
        public const int GroundFacilityBuildingOuterOffsetCells = 2;
    }
}

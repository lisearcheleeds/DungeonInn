namespace DungeonInn.Domain.Common
{
    public static partial class GameConstants
    {
        // ダンジョン1フロアの横幅。単位はグリッドセル数。
        public const int DungeonFloorWidth = 50;

        // ダンジョン1フロアの奥行き。単位はグリッドセル数。
        public const int DungeonFloorDepth = 50;

        // 階段配置候補を探す最大試行回数。
        public const int DungeonStairPlacementMaxAttempts = 100;

        // 階層ごとの生成乱数を分散させるためにフロア番号へ掛ける値。
        public const int DungeonFloorSeedMultiplier = 7919;

        // この階層数ごとにダンジョンのテーマIDを切り替える。
        public const int DungeonThemeFloorsPerTheme = 10;

        // ダンジョン生成時にフロアを分割するSectionの一辺の長さ。単位はグリッドセル数。
        public const int DungeonSectionSizeCells = 25;

        // Section内で基準点を作る時、外周から空ける余白。単位はグリッドセル数。
        public const int DungeonSectionMarginCells = 2;

        // メイン経路を歪ませる基本強度。値が大きいほど経路が曲がりやすい。
        public const int DungeonPathDistortBaseStrength = 8;

        // Sectionごとに追加で生成する基準点の最大数。
        public const int DungeonExtraReferencePointMaxCount = 4;

        // Roomの最小サイズ。単位はグリッドセル数。
        public const int DungeonRoomMinSizeCells = 3;

        // Roomの最大サイズ。単位はグリッドセル数。
        public const int DungeonRoomMaxSizeCells = 5;
    }
}

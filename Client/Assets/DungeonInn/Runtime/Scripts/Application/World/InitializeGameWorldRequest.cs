namespace DungeonInn.Application.World
{
    /// <summary>
    /// GameWorldState の初期化に忁E��な固定条件を渡すリクエスト、E    /// </summary>
    public sealed class InitializeGameWorldRequest
    {
        /// <summary>
        /// ダンジョン生�Eに利用するゲーム開始時の乱数シード、E        /// </summary>
        public int DungeonSeed { get; }

        /// <summary>
        /// GameWorldState 初期化リクエストを作�Eする、E        /// </summary>
        public InitializeGameWorldRequest(int dungeonSeed)
        {
            DungeonSeed = dungeonSeed;
        }
    }
}

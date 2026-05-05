using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Dungeon;

namespace DungeonInn.Application.GameLoop
{
    /// <summary>
    /// GameWorldState の初期化に必要な固定条件を渡すリクエスト。
    /// </summary>
    public sealed class InitializeGameWorldRequest
    {
        /// <summary>
        /// ダンジョン生成に利用するゲーム開始時の乱数シード。
        /// </summary>
        public int DungeonSeed { get; }

        /// <summary>
        /// ダンジョン階層ごとの生成設定。
        /// </summary>
        public IReadOnlyList<DungeonDepthBandConfig> DepthBandConfigs { get; }

        /// <summary>
        /// GameWorldState 初期化リクエストを作成する。
        /// </summary>
        public InitializeGameWorldRequest(
            int dungeonSeed,
            IReadOnlyList<DungeonDepthBandConfig> depthBandConfigs)
        {
            DungeonSeed = dungeonSeed;
            DepthBandConfigs = (depthBandConfigs ?? Array.Empty<DungeonDepthBandConfig>()).ToArray();
        }
    }
}

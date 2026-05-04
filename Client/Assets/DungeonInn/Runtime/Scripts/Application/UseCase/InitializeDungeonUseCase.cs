using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Dungeon;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// ダンジョンを初期化し、地下 1 階を生成するユースケース。
    /// </summary>
    public sealed class InitializeDungeonUseCase
    {
        readonly EnsureDungeonFloorGeneratedUseCase ensureDungeonFloorGeneratedUseCase = new();

        /// <summary>
        /// ゲーム開始時シードを持つダンジョンを作成し、初期フロアとして地下 1 階を生成する。
        /// </summary>
        public async UniTask<Dungeon> ExecuteAsync(int seed, IReadOnlyList<DungeonDepthBandConfig> depthBandConfigs)
        {
            var dungeon = new Dungeon(seed);
            await ensureDungeonFloorGeneratedUseCase.ExecuteAsync(dungeon, 1, depthBandConfigs);
            return dungeon;
        }
    }
}

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Dungeon;
using VContainer;

namespace DungeonInn.Application.Dungeons
{
    /// <summary>
    /// ダンジョンを�E期化し、地丁E1 階を生�Eするユースケース、E    /// </summary>
    public sealed class InitializeDungeonOrchestrator
    {
        readonly EnsureDungeonFloorGeneratedOrchestrator ensureDungeonFloorGeneratedUseCase;

        [Inject]
        public InitializeDungeonOrchestrator(EnsureDungeonFloorGeneratedOrchestrator ensureDungeonFloorGeneratedUseCase)
        {
            this.ensureDungeonFloorGeneratedUseCase = ensureDungeonFloorGeneratedUseCase ?? throw new System.ArgumentNullException(nameof(ensureDungeonFloorGeneratedUseCase));
        }

        /// <summary>
        /// ゲーム開始時シードを持つダンジョンを作�Eし、�E期フロアとして地丁E1 階を生�Eする、E        /// </summary>
        public async UniTask<Dungeon> ExecuteAsync(int seed, IReadOnlyList<DungeonDepthBandConfig> depthBandConfigs)
        {
            var dungeon = new Dungeon(seed);
            await ensureDungeonFloorGeneratedUseCase.ExecuteAsync(dungeon, 1, depthBandConfigs);
            return dungeon;
        }
    }
}

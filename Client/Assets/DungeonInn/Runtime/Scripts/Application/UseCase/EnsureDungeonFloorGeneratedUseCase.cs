using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Dungeon;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 指定階層のダンジョンフロアが未生成なら生成するユースケース。
    /// </summary>
    public sealed class EnsureDungeonFloorGeneratedUseCase
    {
        readonly GenerateDungeonFloorUseCase generateDungeonFloorUseCase;

        [Inject]
        public EnsureDungeonFloorGeneratedUseCase(GenerateDungeonFloorUseCase generateDungeonFloorUseCase)
        {
            this.generateDungeonFloorUseCase = generateDungeonFloorUseCase ?? throw new System.ArgumentNullException(nameof(generateDungeonFloorUseCase));
        }

        /// <summary>
        /// 生成済みフロアを返し、未生成の場合はフロア生成を行ってから返す。
        /// </summary>
        public UniTask<DungeonFloor> ExecuteAsync(
            Dungeon dungeon,
            int floorIndex,
            IReadOnlyList<DungeonDepthBandConfig> depthBandConfigs)
        {
            if (dungeon.TryGetFloor(floorIndex, out var floor))
            {
                return UniTask.FromResult(floor);
            }

            return generateDungeonFloorUseCase.ExecuteAsync(dungeon, floorIndex, depthBandConfigs);
        }
    }
}

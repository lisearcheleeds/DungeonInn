using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Dungeon;
using VContainer;

namespace DungeonInn.Application.Dungeons
{
    /// <summary>
    /// 持E��階層のダンジョンフロアが未生�Eなら生成するユースケース、E    /// </summary>
    public sealed class EnsureDungeonFloorGeneratedOrchestrator
    {
        readonly GenerateDungeonFloorUseCase generateDungeonFloorUseCase;

        [Inject]
        public EnsureDungeonFloorGeneratedOrchestrator(GenerateDungeonFloorUseCase generateDungeonFloorUseCase)
        {
            this.generateDungeonFloorUseCase = generateDungeonFloorUseCase ?? throw new System.ArgumentNullException(nameof(generateDungeonFloorUseCase));
        }

        /// <summary>
        /// 生�E済みフロアを返し、未生�Eの場合�Eフロア生�Eを行ってから返す、E        /// </summary>
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

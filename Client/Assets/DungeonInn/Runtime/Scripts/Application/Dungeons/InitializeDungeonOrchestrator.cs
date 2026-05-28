using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Dungeon;
using VContainer;

namespace DungeonInn.Application.Dungeons
{
    public sealed class InitializeDungeonOrchestrator
    {
        readonly GenerateDungeonFloorUseCase generateDungeonFloorUseCase;

        [Inject]
        public InitializeDungeonOrchestrator(GenerateDungeonFloorUseCase generateDungeonFloorUseCase)
        {
            this.generateDungeonFloorUseCase = generateDungeonFloorUseCase ?? throw new ArgumentNullException(nameof(generateDungeonFloorUseCase));
        }

        public async UniTask<Dungeon> ExecuteAsync(int seed)
        {
            var dungeon = new Dungeon(seed);
            await generateDungeonFloorUseCase.ExecuteAsync(dungeon, 1);
            return dungeon;
        }
    }
}

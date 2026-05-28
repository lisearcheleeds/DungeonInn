using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.World;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Dungeons
{
    public sealed class EnsureDungeonFloorGeneratedOrchestrator
    {
        readonly GenerateDungeonFloorUseCase generateDungeonFloorUseCase;
        readonly IEventPublisher eventPublisher;

        [Inject]
        public EnsureDungeonFloorGeneratedOrchestrator(
            GenerateDungeonFloorUseCase generateDungeonFloorUseCase,
            IEventPublisher eventPublisher)
        {
            this.generateDungeonFloorUseCase = generateDungeonFloorUseCase ?? throw new ArgumentNullException(nameof(generateDungeonFloorUseCase));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public async UniTask<DungeonFloor> ExecuteAsync(Dungeon dungeon, int floorIndex)
        {
            if (dungeon.TryGetFloor(floorIndex, out var floor))
            {
                return floor;
            }

            floor = await generateDungeonFloorUseCase.ExecuteAsync(dungeon, floorIndex);
            eventPublisher.Publish(new MapLayerAddedEvent(MapLayerId.DungeonFloor(floorIndex)));
            return floor;
        }
    }
}

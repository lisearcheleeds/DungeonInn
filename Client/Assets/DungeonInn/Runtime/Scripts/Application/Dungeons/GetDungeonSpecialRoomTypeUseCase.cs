using System;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Dungeons
{
    public sealed class GetDungeonSpecialRoomTypeUseCase
    {
        readonly IMasterRepository masterRepository;

        [Inject]
        public GetDungeonSpecialRoomTypeUseCase(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public DungeonSpecialRoomType Execute(int floorIndex)
        {
            return masterRepository.GetDungeonDepthBandMasterForFloor(floorIndex).SpecialRoomType;
        }
    }
}

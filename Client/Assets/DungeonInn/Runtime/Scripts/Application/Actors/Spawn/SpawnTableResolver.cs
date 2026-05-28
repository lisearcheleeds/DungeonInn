using System;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Spawn
{
    public sealed class SpawnTableResolver
    {
        readonly IMasterRepository masterRepository;

        [Inject]
        public SpawnTableResolver(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public SpawnContext CreateDungeonRoomContext(int floorIndex, DungeonRoom room)
        {
            if (room == null)
            {
                throw new ArgumentNullException(nameof(room));
            }

            var depthBandMaster = masterRepository.GetDungeonDepthBandMasterForFloor(floorIndex);
            return new SpawnContext(floorIndex, depthBandMaster.Id, room.Role);
        }

        public SpawnTableMaster ResolveMonsterSpawnTable(SpawnContext context)
        {
            if (context.RoomRole == DungeonRoomRole.Rest)
            {
                return null;
            }

            var depthBandMaster = masterRepository.DungeonDepthBandMasters[context.DungeonDepthBandId];
            return masterRepository.GetSpawnTableMaster(depthBandMaster.MonsterSpawnTableId);
        }

        public SpawnTableMaster ResolveAdventurerSpawnTable(IGameClock gameClock)
        {
            if (gameClock == null)
            {
                throw new ArgumentNullException(nameof(gameClock));
            }

            var selectedBand = masterRepository.GetAdventurerSpawnBandMaster(gameClock.CurrentDay);
            return masterRepository.GetSpawnTableMaster(selectedBand.SpawnTableId);
        }
    }
}

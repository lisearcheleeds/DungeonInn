using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Factory;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class SpawnScheduledAdventurerUseCase
    {
        readonly SpawnAdventurerFromMasterUseCase spawnAdventurerFromMasterUseCase;
        readonly IMasterRepository masterRepository;

        [Inject]
        public SpawnScheduledAdventurerUseCase(
            SpawnAdventurerFromMasterUseCase spawnAdventurerFromMasterUseCase,
            IMasterRepository masterRepository)
        {
            this.spawnAdventurerFromMasterUseCase = spawnAdventurerFromMasterUseCase ?? throw new ArgumentNullException(nameof(spawnAdventurerFromMasterUseCase));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public async UniTask<Actor> ExecuteAsync(IGameWorldState worldState, int currentScheduleTick)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (currentScheduleTick - worldState.SpawnSchedule.LastAdventurerSpawnTick < GameConstants.AdventurerSpawnIntervalTicks)
            {
                return null;
            }

            worldState.SpawnSchedule.LastAdventurerSpawnTick = currentScheduleTick;

            // TODO: SpawnTableMasterから重み付き抽選に変更する
            var spawnTable = masterRepository.GetSpawnTableMaster(1);
            var entry = spawnTable.Entries[0];

            var position = worldState.GroundMap.Layer.GetCellCenter(worldState.GroundMap.DungeonEntrancePosition);

            // TODO: FactionをFactionMasterから取得する
            var faction = new ActorFaction(1, "Adventurer");

            var request = new AdventurerCreateRequest(
                entry.TargetId,
                Guid.NewGuid(),
                position,
                faction,
                new Random().Next());

            var actor = await spawnAdventurerFromMasterUseCase.ExecuteAsync(
                worldState.Guild,
                request,
                currentScheduleTick);

            worldState.RegisterActor(actor);
            return actor;
        }
    }
}

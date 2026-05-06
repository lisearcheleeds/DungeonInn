using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Factory;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
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

            // TODO: 上限をギルドレベルから取得する
            var adventurerCount = worldState.Actors.Count(x => x.Behavior is AdventurerBehavior);
            if (adventurerCount >= GameConstants.InitialMaxAdventurerCount)
            {
                return null;
            }

            // TODO: SpawnTableMasterから重み付き抽選に変更する
            var spawnTable = masterRepository.GetSpawnTableMaster(1);
            var entry = spawnTable.Entries[0];

            // TODO: スポーン地点をマスタから取得する
            var position = worldState.GroundMap.Layer.GetCellCenter(PickRandomEdgePosition());

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

        static GridPosition PickRandomEdgePosition()
        {
            var rng = new Random();
            return rng.Next(4) switch
            {
                0 => new GridPosition(rng.Next(GameConstants.GroundMapWidth), 0),
                1 => new GridPosition(rng.Next(GameConstants.GroundMapWidth), GameConstants.GroundMapDepth - 1),
                2 => new GridPosition(0, rng.Next(GameConstants.GroundMapDepth)),
                _ => new GridPosition(GameConstants.GroundMapWidth - 1, rng.Next(GameConstants.GroundMapDepth)),
            };
        }
    }
}

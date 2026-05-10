using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Factory;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class SpawnScheduledMonsterUseCase
    {
        readonly SpawnMonsterUseCase spawnMonsterUseCase;
        readonly IMasterRepository masterRepository;
        readonly IGameRandom gameRandom;

        [Inject]
        public SpawnScheduledMonsterUseCase(
            SpawnMonsterUseCase spawnMonsterUseCase,
            IMasterRepository masterRepository,
            IGameRandom gameRandom)
        {
            this.spawnMonsterUseCase = spawnMonsterUseCase ?? throw new ArgumentNullException(nameof(spawnMonsterUseCase));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.gameRandom = gameRandom ?? throw new ArgumentNullException(nameof(gameRandom));
        }

        public async UniTask<Actor> ExecuteAsync(IGameWorldState worldState, int currentScheduleTick)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            // TODO: 間隔をSpawnTableMasterから取得する
            if (currentScheduleTick - worldState.SpawnSchedule.LastMonsterSpawnTick < GameConstants.MonsterSpawnIntervalTicks)
            {
                return null;
            }

            worldState.SpawnSchedule.LastMonsterSpawnTick = currentScheduleTick;

            // TODO: 上限をSpawnTableMasterから取得する
            var monsterCount = worldState.Actors.Count(x => x.Behavior is MonsterBehavior);
            if (monsterCount >= GameConstants.InitialMaxMonsterCount)
            {
                return null;
            }

            var generatedFloors = worldState.Dungeon.Floors.Values
                .OrderBy(floor => floor.FloorIndex)
                .ToArray();
            var floor = generatedFloors[gameRandom.Next(generatedFloors.Length)];
            var room = floor.Rooms[currentScheduleTick % floor.Rooms.Count];
            var position = floor.Layer.GetCellCenter(room.Center);

            // TODO: SpawnTableMasterから重み付き抽選に変更する
            var floorExplorationMaster = masterRepository.GetDungeonFloorExplorationMaster(floor.FloorIndex);
            var spawnTable = masterRepository.GetSpawnTableMaster(floorExplorationMaster.MonsterSpawnTableId);
            var entry = spawnTable.Entries[0];

            // TODO: FactionをFactionMasterから取得する
            var faction = new ActorFaction(2, "Monster");

            var request = new MonsterCreateRequest(
                entry.TargetMasterId,
                Guid.NewGuid(),
                position,
                faction,
                gameRandom.Next());

            var actor = await spawnMonsterUseCase.ExecuteAsync(request);
            worldState.RegisterActor(actor);
            return actor;
        }
    }
}

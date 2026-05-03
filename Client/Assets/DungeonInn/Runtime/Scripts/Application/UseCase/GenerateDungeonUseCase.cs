using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.World;

namespace DungeonInn.Application.UseCase
{
    public class GenerateDungeonUseCase
    {
        public UniTask<DungeonMap> ExecuteAsync(IDungeonGenerator generator, DungeonConfigData config, WorldConfigData worldConfig)
        {
            var map = generator.Generate(
                config.FloorSizeX,
                config.FloorSizeZ,
                worldConfig.DungeonFloorCount,
                worldConfig.DungeonFloorHeight,
                config.Seed,
                config.MinStairsCount);
            return UniTask.FromResult(map);
        }
    }
}

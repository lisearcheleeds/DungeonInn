using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonInn.Application.World
{
    public interface IWorldGameSettingsRepository
    {
        UniTask LoadAsync(CancellationToken cancellationToken);
        InitialWorldSettings GetInitialWorldSettings();
        GroundMapGenerationSettings GetGroundMapGenerationSettings();
        DungeonMapGenerationSettings GetDungeonMapGenerationSettings();
        InnBalanceSettings GetInnBalanceSettings();
        ActorSimulationSettings GetActorSimulationSettings();
        SpawnBalanceSettings GetSpawnBalanceSettings();
        AdventurerReturnPolicySettings GetAdventurerReturnPolicySettings();
        CombatBalanceSettings GetCombatBalanceSettings();
    }
}

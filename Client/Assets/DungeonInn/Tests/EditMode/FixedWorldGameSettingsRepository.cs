using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.World;
using DungeonInn.View.Scene.MainScene.World;

namespace DungeonInn.Tests.EditMode
{
    public sealed class FixedWorldGameSettingsRepository :
        IWorldGameSettingsRepository,
        IWorldMapViewSettingsRepository
    {
        readonly InitialWorldSettings initialWorldSettings;
        readonly GroundMapGenerationSettings groundMapGenerationSettings;
        readonly DungeonMapGenerationSettings dungeonMapGenerationSettings;
        readonly InnBalanceSettings innBalanceSettings;
        readonly ActorSimulationSettings actorSimulationSettings;
        readonly SpawnBalanceSettings spawnBalanceSettings;
        readonly AdventurerReturnPolicySettings adventurerReturnPolicySettings;
        readonly CombatBalanceSettings combatBalanceSettings;
        readonly WorldMapViewSettings worldMapViewSettings;

        public FixedWorldGameSettingsRepository(
            InitialWorldSettings initialWorldSettings = null,
            GroundMapGenerationSettings groundMapGenerationSettings = null,
            DungeonMapGenerationSettings dungeonMapGenerationSettings = null,
            InnBalanceSettings innBalanceSettings = null,
            ActorSimulationSettings actorSimulationSettings = null,
            SpawnBalanceSettings spawnBalanceSettings = null,
            AdventurerReturnPolicySettings adventurerReturnPolicySettings = null,
            CombatBalanceSettings combatBalanceSettings = null,
            WorldMapViewSettings worldMapViewSettings = null)
        {
            this.initialWorldSettings = initialWorldSettings ?? InitialWorldSettings.CreateDefault();
            this.groundMapGenerationSettings = groundMapGenerationSettings ?? GroundMapGenerationSettings.CreateDefault();
            this.dungeonMapGenerationSettings = dungeonMapGenerationSettings ?? DungeonMapGenerationSettings.CreateDefault();
            this.innBalanceSettings = innBalanceSettings ?? InnBalanceSettings.CreateDefault();
            this.actorSimulationSettings = actorSimulationSettings ?? ActorSimulationSettings.CreateDefault();
            this.spawnBalanceSettings = spawnBalanceSettings ?? SpawnBalanceSettings.CreateDefault();
            this.adventurerReturnPolicySettings =
                adventurerReturnPolicySettings ?? AdventurerReturnPolicySettings.CreateDefault();
            this.combatBalanceSettings = combatBalanceSettings ?? CombatBalanceSettings.CreateDefault();
            this.worldMapViewSettings = worldMapViewSettings ?? WorldMapViewSettings.CreateDefault();
        }

        public UniTask LoadAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public InitialWorldSettings GetInitialWorldSettings()
        {
            return initialWorldSettings;
        }

        public GroundMapGenerationSettings GetGroundMapGenerationSettings()
        {
            return groundMapGenerationSettings;
        }

        public DungeonMapGenerationSettings GetDungeonMapGenerationSettings()
        {
            return dungeonMapGenerationSettings;
        }

        public InnBalanceSettings GetInnBalanceSettings()
        {
            return innBalanceSettings;
        }

        public ActorSimulationSettings GetActorSimulationSettings()
        {
            return actorSimulationSettings;
        }

        public SpawnBalanceSettings GetSpawnBalanceSettings()
        {
            return spawnBalanceSettings;
        }

        public AdventurerReturnPolicySettings GetAdventurerReturnPolicySettings()
        {
            return adventurerReturnPolicySettings;
        }

        public CombatBalanceSettings GetCombatBalanceSettings()
        {
            return combatBalanceSettings;
        }

        public WorldMapViewSettings GetWorldMapViewSettings()
        {
            return worldMapViewSettings;
        }
    }
}


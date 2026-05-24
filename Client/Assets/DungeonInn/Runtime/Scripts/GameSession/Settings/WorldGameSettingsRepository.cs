using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.World;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.GameSession.Settings
{
    public sealed class WorldGameSettingsRepository : IWorldGameSettingsRepository
    {
        const string Address = "Config/WorldGameSettings";

        readonly IAssetScope assetScope;

        InitialWorldSettings initialWorldSettings;
        GroundMapGenerationSettings groundMapGenerationSettings;
        DungeonMapGenerationSettings dungeonMapGenerationSettings;
        InnBalanceSettings innBalanceSettings;
        ActorSimulationSettings actorSimulationSettings;
        SpawnBalanceSettings spawnBalanceSettings;
        AdventurerReturnPolicySettings adventurerReturnPolicySettings;
        CombatBalanceSettings combatBalanceSettings;
        bool loaded;

        [Inject]
        public WorldGameSettingsRepository(IAssetScope assetScope)
        {
            this.assetScope = assetScope ?? throw new ArgumentNullException(nameof(assetScope));
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken)
        {
            if (loaded)
            {
                return;
            }

            var handle = await assetScope.LoadAsync<WorldGameSettingsSO>(Address, cancellationToken);
            var settingsSO = handle.Asset;
            if (settingsSO == null)
            {
                initialWorldSettings = InitialWorldSettings.CreateDefault();
                groundMapGenerationSettings = GroundMapGenerationSettings.CreateDefault();
                dungeonMapGenerationSettings = DungeonMapGenerationSettings.CreateDefault();
                innBalanceSettings = InnBalanceSettings.CreateDefault();
                actorSimulationSettings = ActorSimulationSettings.CreateDefault();
                spawnBalanceSettings = SpawnBalanceSettings.CreateDefault();
                adventurerReturnPolicySettings = AdventurerReturnPolicySettings.CreateDefault();
                combatBalanceSettings = CombatBalanceSettings.CreateDefault();
                loaded = true;
                return;
            }

            initialWorldSettings = settingsSO.ToInitialWorldSettings();
            groundMapGenerationSettings = settingsSO.ToGroundMapGenerationSettings();
            dungeonMapGenerationSettings = settingsSO.ToDungeonMapGenerationSettings();
            innBalanceSettings = settingsSO.ToInnBalanceSettings();
            actorSimulationSettings = settingsSO.ToActorSimulationSettings();
            spawnBalanceSettings = settingsSO.ToSpawnBalanceSettings();
            adventurerReturnPolicySettings = settingsSO.ToAdventurerReturnPolicySettings();
            combatBalanceSettings = settingsSO.ToCombatBalanceSettings();
            loaded = true;
        }

        public InitialWorldSettings GetInitialWorldSettings()
        {
            EnsureLoaded();
            return initialWorldSettings;
        }

        public GroundMapGenerationSettings GetGroundMapGenerationSettings()
        {
            EnsureLoaded();
            return groundMapGenerationSettings;
        }

        public DungeonMapGenerationSettings GetDungeonMapGenerationSettings()
        {
            EnsureLoaded();
            return dungeonMapGenerationSettings;
        }

        public InnBalanceSettings GetInnBalanceSettings()
        {
            EnsureLoaded();
            return innBalanceSettings;
        }

        public ActorSimulationSettings GetActorSimulationSettings()
        {
            EnsureLoaded();
            return actorSimulationSettings;
        }

        public SpawnBalanceSettings GetSpawnBalanceSettings()
        {
            EnsureLoaded();
            return spawnBalanceSettings;
        }

        public AdventurerReturnPolicySettings GetAdventurerReturnPolicySettings()
        {
            EnsureLoaded();
            return adventurerReturnPolicySettings;
        }

        public CombatBalanceSettings GetCombatBalanceSettings()
        {
            EnsureLoaded();
            return combatBalanceSettings;
        }

        void EnsureLoaded()
        {
            if (!loaded)
            {
                throw new InvalidOperationException("World game settings are not loaded.");
            }
        }
    }
}

using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Common;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldLifetimeScope : LifetimeScope
    {
        [SerializeField] WorldScene worldScene;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(worldScene);
            builder.RegisterComponentInHierarchy<WorldGameLoopEntryPoint>();
            builder.Register<WorldPresenter>(Lifetime.Scoped).AsImplementedInterfaces();
            builder.Register<WorldViewRoot>(Lifetime.Scoped);
            builder.RegisterInstance(new LayerPositionViewSettings()).AsSelf();
            builder.Register<LayerPositionViewMapper>(Lifetime.Scoped);
            builder.Register<MapLayerViewRegistry>(Lifetime.Scoped);
            builder.Register<MapMaterialSet>(Lifetime.Scoped);
            builder.Register<MapTileVisualConfig>(Lifetime.Scoped);
            builder.Register<MapMeshBuildService>(Lifetime.Scoped);
            builder.Register<ActorSpriteVisualConfig>(Lifetime.Scoped);
            builder.RegisterInstance(new WorldCameraSettings()).AsSelf();
            builder.Register<WorldMapView>(Lifetime.Scoped);
            builder.Register<WorldActorViewPool>(Lifetime.Scoped);
            builder.Register<WorldActorViewRegistry>(Lifetime.Scoped);
            builder.Register<WorldActorPresenter>(Lifetime.Scoped);
            builder.Register<WorldCameraController>(Lifetime.Scoped);
            builder.Register<WorldLayerViewController>(Lifetime.Scoped);
#if DEBUG
            builder.RegisterEntryPoint<WorldDebugGameLogPresenter>(Lifetime.Scoped);
#endif

            builder.Register<ActorProfileRegistry>(Lifetime.Scoped).As<IActorProfileRegistry>();
            builder.Register<GameEventHistoryService>(Lifetime.Scoped)
                .As<IGameEventHistoryReader>()
                .As<IGameEventHistoryRecorder>()
                .AsSelf();
            builder.Register<GameEventBus>(Lifetime.Scoped)
                .As<IGameEventBus>()
                .As<IEventPublisher>()
                .As<IEventSubscriber>()
                .AsSelf();
            builder.Register<AdventurerBattleRecordService>(Lifetime.Scoped);
            builder.Register<ActorExplorationAchievementRegistry>(Lifetime.Scoped);
            builder.Register<AdventurerReturnTrackingService>(Lifetime.Scoped);
            builder.Register<AdventurerRecoveryStateService>(Lifetime.Scoped);
            builder.Register<ActorProcessingCandidateService>(Lifetime.Scoped);
            builder.Register<AdventurerExplorationStateService>(Lifetime.Scoped);

            builder.RegisterInstance(new GameRandom(GameConstants.InitialGameRandomSeed)).As<IGameRandom>();
            builder.Register<ActorNavigationService>(Lifetime.Scoped).As<IActorNavigationService>();
            builder.Register<ActorCombatService>(Lifetime.Scoped).As<IActorCombatService>();
            builder.Register<ActorSpatialIndexService>(Lifetime.Scoped);
            builder.Register<ItemSpatialIndexService>(Lifetime.Scoped);

            builder.Register<GameClock>(Lifetime.Scoped).As<IGameClock>();
            builder.Register<GameWorldState>(Lifetime.Scoped)
                .As<IGameWorldState>()
                .As<IGameWorldStateReader>()
                .As<IGameWorldStateWriter>();
            builder.Register<WorldMapViewDataProvider>(Lifetime.Scoped).As<IWorldMapViewDataProvider>();
            builder.Register<ActorViewDataStore>(Lifetime.Scoped)
                .As<IActorViewDataProvider>()
                .AsSelf();
            builder.Register<GameWorldFrameBuffer>(Lifetime.Scoped);
            builder.Register<InitializeWorldMapUseCase>(Lifetime.Scoped);
            builder.Register<GenerateDungeonFloorUseCase>(Lifetime.Scoped);
            builder.Register<EnsureDungeonFloorGeneratedOrchestrator>(Lifetime.Scoped);
            builder.Register<InitializeDungeonOrchestrator>(Lifetime.Scoped);
            builder.Register<InitializeGameWorldOrchestrator>(Lifetime.Scoped);
            builder.Register<GameLoopUseCase>(Lifetime.Scoped).As<IGameLoopUseCase>();
            builder.Register<WorldSimulationOrchestrator>(Lifetime.Scoped).As<IWorldSimulationOrchestrator>();
            builder.Register<SetGameTimeScaleUseCase>(Lifetime.Scoped);
            builder.Register<PauseGameTimeUseCase>(Lifetime.Scoped);
            builder.Register<ResumeGameTimeUseCase>(Lifetime.Scoped);
            builder.Register<ToggleGamePauseUseCase>(Lifetime.Scoped);
            builder.Register<GetGameTimeStateUseCase>(Lifetime.Scoped);
            builder.Register<GetGameEventHistoryUseCase>(Lifetime.Scoped);
            builder.Register<InnEconomyStatisticsService>(Lifetime.Scoped);
            builder.Register<InnDailyReportStore>(Lifetime.Scoped);
            builder.Register<InnEconomyStatusCalculator>(Lifetime.Scoped);
            builder.Register<GetInnEconomyStatusUseCase>(Lifetime.Scoped);
            builder.Register<GetInnEconomyReportUseCase>(Lifetime.Scoped);
            builder.Register<AssignStaffUseCase>(Lifetime.Scoped);
            builder.Register<CalculateScoutCostUseCase>(Lifetime.Scoped);
            builder.Register<CompleteActorSpawnUseCase>(Lifetime.Scoped);
            builder.Register<SpawnAdventurerUseCase>(Lifetime.Scoped);
            builder.Register<SpawnMonsterUseCase>(Lifetime.Scoped);
            builder.Register<SpawnScheduledAdventurerOrchestrator>(Lifetime.Scoped);
            builder.Register<SpawnScheduledMonsterOrchestrator>(Lifetime.Scoped);
            builder.Register<MoveActorTowardDestinationUseCase>(Lifetime.Scoped);
            builder.Register<ActorCombatPowerCalculator>(Lifetime.Scoped);
            builder.Register<UseDungeonStairOrchestrator>(Lifetime.Scoped);
            builder.Register<SelectDungeonExplorationGoalUseCase>(Lifetime.Scoped);
            builder.Register<SelectDungeonTargetFloorUseCase>(Lifetime.Scoped);
            builder.Register<AdvanceActorLifecycleOrchestrator>(Lifetime.Scoped);
            builder.Register<CombatEncounterTargetResolver>(Lifetime.Scoped);
            builder.Register<DetectCombatEncounterUseCase>(Lifetime.Scoped);
            builder.Register<GrantExperienceUseCase>(Lifetime.Scoped);
            builder.Register<DropItemUseCase>(Lifetime.Scoped);
            builder.Register<PickUpItemUseCase>(Lifetime.Scoped);
            builder.Register<UpdateEquipmentUseCase>(Lifetime.Scoped);
            builder.Register<SellItemsUseCase>(Lifetime.Scoped);
            builder.Register<UseConsumableItemUseCase>(Lifetime.Scoped);
            builder.Register<UseRecoveryItemOrchestrator>(Lifetime.Scoped);
            builder.Register<AdvanceActorEffectsUseCase>(Lifetime.Scoped);
            builder.Register<AdvanceCombatUseCase>(Lifetime.Scoped);
            builder.Register<AttackAreaTargetResolver>(Lifetime.Scoped);
            builder.Register<CombatDefeatResolver>(Lifetime.Scoped);
            builder.Register<ActorDefeatOrchestrator>(Lifetime.Scoped);
            builder.Register<CombatDamageResolver>(Lifetime.Scoped);
            builder.Register<CombatEffectExecutor>(Lifetime.Scoped);
            builder.Register<AdvanceProjectileUseCase>(Lifetime.Scoped);
            builder.Register<AdvanceAreaEffectUseCase>(Lifetime.Scoped);
            builder.Register<DecideAdventurerReturnUseCase>(Lifetime.Scoped);
            builder.Register<ChargeInnFeeUseCase>(Lifetime.Scoped);
            builder.Register<DespawnAdventurerUseCase>(Lifetime.Scoped);
            builder.Register<RecoverAdventurerAtInnUseCase>(Lifetime.Scoped);
            builder.Register<AdvanceInnRecoveryOrchestrator>(Lifetime.Scoped);
            builder.Register<PublishInnDailyReportUseCase>(Lifetime.Scoped);
            builder.Register<PayStaffSalaryUseCase>(Lifetime.Scoped);
            builder.Register<ProcessAdventurerSaleUseCase>(Lifetime.Scoped);
            builder.Register<ProcessExchangeOfferUseCase>(Lifetime.Scoped);
            builder.Register<ProcessFacilityUsageUseCase>(Lifetime.Scoped);
            builder.Register<RecruitStaffOrchestrator>(Lifetime.Scoped);

            builder.Register<ActorDecisionScheduler>(Lifetime.Scoped);
            builder.Register<ApplyActorAiDecisionUseCase>(Lifetime.Scoped);
            builder.Register<AdvanceActorAiOrchestrator>(Lifetime.Scoped);
            builder.Register<AdventurerAiPolicy>(Lifetime.Scoped);
            builder.Register<MonsterAiPolicy>(Lifetime.Scoped);
            builder.Register<PetAiPolicy>(Lifetime.Scoped);
            builder.Register<GuildStaffAiPolicy>(Lifetime.Scoped);
        }
    }
}

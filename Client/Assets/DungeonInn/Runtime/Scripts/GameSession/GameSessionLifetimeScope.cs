using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Phase;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Items;
using DungeonInn.Application.SaveLoad;
using DungeonInn.Application.World;
using DungeonInn.GameSession.Settings;
using DungeonInn.View.Scene;
using DungeonInn.View.Scene.Bridge;
using LighthouseExtends.Addressable;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.GameSession
{
    public sealed class GameSessionLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<GameSessionAssetScopeHolder>(Lifetime.Singleton).AsSelf();
            builder.Register(container => container.Resolve<GameSessionAssetScopeHolder>().AssetScope, Lifetime.Singleton)
                .As<IAssetScope>();
            builder.Register<WorldGameSettingsRepository>(Lifetime.Singleton).AsSelf();
            builder.Register(container => container.Resolve<WorldGameSettingsRepository>(), Lifetime.Singleton)
                .As<IWorldGameSettingsRepository>();

            // === View: Shared utilities ===
            builder.Register<ActorScreenPositionProviderProxy>(Lifetime.Singleton)
                .As<IActorScreenPositionProvider>()
                .As<IActorScreenPositionProviderRegistry>();
            builder.Register<ActiveLayerProviderProxy>(Lifetime.Singleton)
                .As<IActiveLayerProvider>()
                .As<IActiveLayerProviderRegistry>();
            builder.Register<NavigationPathProviderProxy>(Lifetime.Singleton)
                .As<INavigationPathProvider>()
                .As<INavigationPathProviderRegistry>();
            builder.Register<ActorSelectionService>(Lifetime.Singleton)
                .As<IActorSelectionReader>()
                .AsSelf();

            // === Application: Selected actor inspector ===
            builder.Register<GetSelectedActorInspectorQuery>(Lifetime.Singleton);

            // === Application: Player log ===
            builder.Register<PlayerEventLogFormatter>(Lifetime.Singleton);
            builder.Register<PlayerEventLogStore>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            // === Application: Events / actor state ===
            builder.Register<ActorProfileRegistry>(Lifetime.Singleton).As<IActorProfileRegistry>();
            builder.Register<GameEventHistoryService>(Lifetime.Singleton)
                .As<IGameEventHistoryReader>()
                .As<IGameEventHistoryRecorder>()
                .AsSelf();
            builder.Register<GameEventBus>(Lifetime.Singleton)
                .As<IGameEventBus>()
                .As<IEventPublisher>()
                .As<IEventSubscriber>()
                .AsSelf();
            builder.Register<AdventurerBattleRecordService>(Lifetime.Singleton);
            builder.Register<ActorExplorationAchievementRegistry>(Lifetime.Singleton);
            builder.Register<AdventurerReturnTrackingService>(Lifetime.Singleton);
            builder.Register<AdventurerRecoveryStateService>(Lifetime.Singleton);
            builder.Register<ActorProcessingCandidateService>(Lifetime.Singleton);
            builder.Register<AdventurerDeathRevivalService>(Lifetime.Singleton);
            builder.Register<AdventurerExplorationStateService>(Lifetime.Singleton);
            builder.Register<HardcodedActorActionPhaseMasterRepository>(Lifetime.Singleton)
                .As<IActorActionPhaseMasterRepository>();
            builder.Register<ActorActionPhaseStateStore>(Lifetime.Singleton)
                .As<IActorActionPhaseStateStore>();

            // === Application: Navigation / spatial queries ===
            builder.Register<ActorNavigationService>(Lifetime.Singleton).As<IActorNavigationService>();
            builder.Register<ActorCombatService>(Lifetime.Singleton).As<IActorCombatService>();
            builder.Register<ActorSpatialIndexService>(Lifetime.Singleton);
            builder.Register<ItemSpatialIndexService>(Lifetime.Singleton);

            // === Application: World state / game loop ===
            builder.Register<GameRandom>(Lifetime.Singleton).As<IGameRandom>().AsSelf();
            builder.Register<GameClock>(Lifetime.Singleton).As<IGameClock>().As<IGameClockRestorer>();
            builder.Register<GameWorldState>(Lifetime.Singleton)
                .As<IGameWorldState>()
                .As<IGameWorldStateReader>()
                .As<IGameWorldStateWriter>();
            builder.Register<WorldMapViewDataProvider>(Lifetime.Singleton).As<IWorldMapViewDataProvider>();
            builder.Register<GetDungeonLayerInfoUseCase>(Lifetime.Singleton);
            builder.Register<DungeonInfoScreenService>(Lifetime.Singleton).As<IDungeonInfoScreenService>();
            builder.Register<ActorViewDataStore>(Lifetime.Singleton)
                .As<IActorViewDataProvider>()
                .As<IActorStatusViewDataProvider>()
                .As<IActorSelectionCandidateProvider>()
                .AsSelf();
            builder.Register<GetActorStatusSummaryQuery>(Lifetime.Singleton);
            builder.Register<GameWorldFrameBuffer>(Lifetime.Singleton);
            builder.Register<InitializeWorldMapUseCase>(Lifetime.Singleton);
            builder.Register<AssignDungeonRoomRolesUseCase>(Lifetime.Singleton);
            builder.Register<GenerateDungeonFloorUseCase>(Lifetime.Singleton);
            builder.Register<EnsureDungeonFloorGeneratedOrchestrator>(Lifetime.Singleton);
            builder.Register<InitializeDungeonOrchestrator>(Lifetime.Singleton);
            builder.Register<InitializeGameWorldOrchestrator>(Lifetime.Singleton);
            builder.Register<GameLoopUseCase>(Lifetime.Singleton).As<IGameLoopUseCase>();
            builder.Register<WorldSimulationOrchestrator>(Lifetime.Singleton).As<IWorldSimulationOrchestrator>();
            builder.Register<SetGameTimeScaleUseCase>(Lifetime.Singleton);
            builder.Register<PauseGameTimeUseCase>(Lifetime.Singleton);
            builder.Register<ResumeGameTimeUseCase>(Lifetime.Singleton);
            builder.Register<ToggleGamePauseUseCase>(Lifetime.Singleton);
            builder.Register<GetGameTimeStateUseCase>(Lifetime.Singleton);
            builder.Register<GetGameClockViewDataUseCase>(Lifetime.Singleton);
            builder.Register<WorldHudScreenService>(Lifetime.Singleton).As<IWorldHudScreenService>();
            builder.Register<ActiveSaveSlotService>(Lifetime.Singleton);
            builder.Register<TutorialProgressService>(Lifetime.Singleton);
            builder.Register<CreateGameSaveSnapshotUseCase>(Lifetime.Singleton);
            builder.Register<RestoreGameSaveSnapshotUseCase>(Lifetime.Singleton);
            builder.Register<SaveGameUseCase>(Lifetime.Singleton);

            // === Application: Economy / inn ===
            builder.Register<GetGameEventHistoryUseCase>(Lifetime.Singleton);
            builder.Register<InnEconomyStatisticsService>(Lifetime.Singleton);
            builder.Register<InnDailyReportStore>(Lifetime.Singleton);
            builder.Register<InnEconomyStatusCalculator>(Lifetime.Singleton);
            builder.Register<GuildCombinedInventoryViewService>(Lifetime.Singleton);
            builder.Register<GuildInventoryWithdrawalService>(Lifetime.Singleton);
            builder.Register<FacilityUpgradePreviewService>(Lifetime.Singleton);
            builder.Register<GetFacilityLineupUseCase>(Lifetime.Singleton);
            builder.Register<FacilityEffectService>(Lifetime.Singleton);
            builder.Register<GuildProgressService>(Lifetime.Singleton);
            builder.Register<GetInnEconomyStatusUseCase>(Lifetime.Singleton);
            builder.Register<GetGuildManagementStatusUseCase>(Lifetime.Singleton);
            builder.Register<GuildManagementScreenService>(Lifetime.Singleton).As<IGuildManagementScreenService>();
            builder.Register<GetFacilityUpgradePreviewUseCase>(Lifetime.Singleton);
            builder.Register<UpgradeFacilityUseCase>(Lifetime.Singleton);
            builder.Register<GetMarketOffersUseCase>(Lifetime.Singleton);
            builder.Register<FulfillMarketOfferUseCase>(Lifetime.Singleton);
            builder.Register<MarketScreenService>(Lifetime.Singleton).As<IMarketScreenService>();
            builder.Register<GetInnGuestListUseCase>(Lifetime.Singleton);
            builder.Register<InnStatusPanelScreenService>(Lifetime.Singleton).As<IInnStatusPanelScreenService>();
            builder.Register<GetInnEconomyReportUseCase>(Lifetime.Singleton);
            builder.Register<AssignStaffUseCase>(Lifetime.Singleton);

            // === Application: Actor spawn ===
            builder.Register<CalculateScoutCostUseCase>(Lifetime.Singleton);
            builder.Register<CompleteActorSpawnUseCase>(Lifetime.Singleton);
            builder.Register<SpawnAdventurerUseCase>(Lifetime.Singleton);
            builder.Register<SpawnMonsterUseCase>(Lifetime.Singleton);
            builder.Register<SpawnTableResolver>(Lifetime.Singleton);
            builder.Register<SpawnScheduledAdventurerOrchestrator>(Lifetime.Singleton);
            builder.Register<SpawnScheduledMonsterOrchestrator>(Lifetime.Singleton);

            // === Application: Actor movement / growth ===
            builder.Register<ActorMovementService>(Lifetime.Singleton);
            builder.Register<MoveActorTowardDestinationUseCase>(Lifetime.Singleton);
            builder.Register<ActorCombatPowerCalculator>(Lifetime.Singleton);
            builder.Register<UseDungeonStairOrchestrator>(Lifetime.Singleton);
            builder.Register<SelectAdventureGoalUseCase>(Lifetime.Singleton);
            builder.Register<SelectDungeonTargetFloorUseCase>(Lifetime.Singleton);
            builder.Register<AdvanceActorLifecycleOrchestrator>(Lifetime.Singleton);

            // === Application: Combat ===
            builder.Register<CombatEncounterTargetResolver>(Lifetime.Singleton);
            builder.Register<DetectCombatEncounterUseCase>(Lifetime.Singleton);
            builder.Register<GrantExperienceUseCase>(Lifetime.Singleton);
            builder.Register<DropItemUseCase>(Lifetime.Singleton);
            builder.Register<PickUpItemUseCase>(Lifetime.Singleton);
            builder.Register<UpdateEquipmentUseCase>(Lifetime.Singleton);
            builder.Register<SellItemsUseCase>(Lifetime.Singleton);
            builder.Register<UseConsumableItemUseCase>(Lifetime.Singleton);
            builder.Register<RecoveryItemCandidateQuery>(Lifetime.Singleton);
            builder.Register<RecoveryEffectEstimator>(Lifetime.Singleton);
            builder.Register<RecoveryItemSelectionPolicy>(Lifetime.Singleton);
            builder.Register<UseRecoveryItemOrchestrator>(Lifetime.Singleton);
            builder.Register<AdvanceActorEffectsUseCase>(Lifetime.Singleton);
            builder.Register<AdvanceCombatUseCase>(Lifetime.Singleton);
            builder.Register<AttackAreaTargetResolver>(Lifetime.Singleton);
            builder.Register<CombatDefeatResolver>(Lifetime.Singleton);
            builder.Register<ActorDefeatOrchestrator>(Lifetime.Singleton);
            builder.Register<CombatDamageResolver>(Lifetime.Singleton);
            builder.Register<CombatEffectExecutor>(Lifetime.Singleton);
            builder.Register<AdvanceProjectileUseCase>(Lifetime.Singleton);
            builder.Register<AdvanceAreaEffectUseCase>(Lifetime.Singleton);

            // === Application: Adventurer return / inn processing ===
            builder.Register<AdventureGoalProgressService>(Lifetime.Singleton);
            builder.Register<DecideAdventurerReturnUseCase>(Lifetime.Singleton);
            builder.Register<ChargeInnFeeUseCase>(Lifetime.Singleton);
            builder.Register<DespawnAdventurerUseCase>(Lifetime.Singleton);
            builder.Register<RecoverAdventurerAtInnUseCase>(Lifetime.Singleton);
            builder.Register<AdvanceInnRecoveryOrchestrator>(Lifetime.Singleton);
            builder.Register<PublishInnDailyReportUseCase>(Lifetime.Singleton);
            builder.Register<PayStaffSalaryUseCase>(Lifetime.Singleton);
            builder.Register<ProcessAdventurerSaleUseCase>(Lifetime.Singleton);
            builder.Register<ProcessExchangeOfferUseCase>(Lifetime.Singleton);
            builder.Register<ProcessFacilityUsageUseCase>(Lifetime.Singleton);
            builder.Register<RecruitStaffOrchestrator>(Lifetime.Singleton);

            // === Application: AI ===
            builder.Register<ActorDecisionScheduler>(Lifetime.Singleton);
            builder.Register<ApplyActorAiDecisionUseCase>(Lifetime.Singleton);
            builder.Register<AdvanceActorAiOrchestrator>(Lifetime.Singleton);
            builder.Register<AdventurerAiPolicy>(Lifetime.Singleton);
            builder.Register<MonsterAiPolicy>(Lifetime.Singleton);
            builder.Register<PetAiPolicy>(Lifetime.Singleton);
            builder.Register<GuildStaffAiPolicy>(Lifetime.Singleton);
        }
    }
}

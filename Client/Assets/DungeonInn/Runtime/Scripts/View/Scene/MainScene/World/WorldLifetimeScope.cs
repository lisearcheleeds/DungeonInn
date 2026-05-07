using DungeonInn.Application.Profiles;
using DungeonInn.Application.AI;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
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
            builder.Register<WorldActorDebugVisualizer>(Lifetime.Scoped);
            builder.RegisterEntryPoint<WorldGameLogPresenter>(Lifetime.Scoped);

            builder.Register<ActorProfileRegistry>(Lifetime.Scoped).As<IActorProfileRegistry>();
            builder.Register<GameEventBus>(Lifetime.Scoped).As<IGameEventBus>().AsSelf();
            builder.Register<AdventurerBattleRecordService>(Lifetime.Scoped);

            builder.RegisterInstance(new GameRandom(GameConstants.InitialGameRandomSeed)).As<IGameRandom>();
            builder.Register<ActorNavigationService>(Lifetime.Scoped).As<IActorNavigationService>();
            builder.Register<ActorCombatService>(Lifetime.Scoped).As<IActorCombatService>();

            builder.Register<GameClock>(Lifetime.Scoped).As<IGameClock>();
            builder.Register<GameWorldState>(Lifetime.Scoped).As<IGameWorldState>();
            builder.Register<InitializeWorldMapUseCase>(Lifetime.Scoped);
            builder.Register<GenerateDungeonFloorUseCase>(Lifetime.Scoped);
            builder.Register<EnsureDungeonFloorGeneratedUseCase>(Lifetime.Scoped);
            builder.Register<InitializeDungeonUseCase>(Lifetime.Scoped);
            builder.Register<InitializeGameWorldUseCase>(Lifetime.Scoped);
            builder.Register<GameLoopUseCase>(Lifetime.Scoped).As<IGameLoopUseCase>();
            builder.Register<SetGameTimeScaleUseCase>(Lifetime.Scoped);
            builder.Register<SpawnAdventurerUseCase>(Lifetime.Scoped);
            builder.Register<SpawnMonsterUseCase>(Lifetime.Scoped);
            builder.Register<SpawnScheduledAdventurerUseCase>(Lifetime.Scoped);
            builder.Register<SpawnScheduledMonsterUseCase>(Lifetime.Scoped);
            builder.Register<MoveActorTowardDestinationUseCase>(Lifetime.Scoped);
            builder.Register<UseDungeonStairUseCase>(Lifetime.Scoped);
            builder.Register<AdvanceActorSimpleLifecycleUseCase>(Lifetime.Scoped);
            builder.Register<DetectCombatEncounterUseCase>(Lifetime.Scoped);
            builder.Register<AdvanceCombatUseCase>(Lifetime.Scoped);

            builder.Register<ActorDecisionScheduler>(Lifetime.Scoped);
            builder.Register<ApplyActorAiDecisionUseCase>(Lifetime.Scoped);
            builder.Register<AdvanceActorAiUseCase>(Lifetime.Scoped);
            builder.Register<AdventurerAiPolicy>(Lifetime.Scoped);
            builder.Register<MonsterAiPolicy>(Lifetime.Scoped);
            builder.Register<PetAiPolicy>(Lifetime.Scoped);
            builder.Register<GuildStaffAiPolicy>(Lifetime.Scoped);
        }
    }
}

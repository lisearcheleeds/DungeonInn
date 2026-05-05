using DungeonInn.Application.AI;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
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

            builder.Register<GameClock>(Lifetime.Scoped).As<IGameClock>();
            builder.Register<GameWorldState>(Lifetime.Scoped).As<IGameWorldState>();
            builder.Register<InitializeWorldMapUseCase>(Lifetime.Scoped);
            builder.Register<GenerateDungeonFloorUseCase>(Lifetime.Scoped);
            builder.Register<EnsureDungeonFloorGeneratedUseCase>(Lifetime.Scoped);
            builder.Register<InitializeDungeonUseCase>(Lifetime.Scoped);
            builder.Register<InitializeGameWorldUseCase>(Lifetime.Scoped);
            builder.Register<GameLoopUseCase>(Lifetime.Scoped).As<IGameLoopUseCase>();
            builder.Register<SetGameTimeScaleUseCase>(Lifetime.Scoped);

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

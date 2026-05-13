using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Orchestration;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceCombatUseCase
    {
        readonly IActorCombatService actorCombatService;
        readonly IGameClock gameClock;
        readonly GameWorldFrameBuffer frameBuffer;
        readonly CombatEffectExecutor combatEffectExecutor;
        readonly ActorDefeatOrchestrator actorDefeatOrchestrator;
        readonly IEventPublisher eventPublisher;
        readonly ActorSpatialIndexService actorSpatialIndexService;
        readonly ActorViewDataStore actorViewDataStore;

        [Inject]
        public AdvanceCombatUseCase(
            IActorCombatService actorCombatService,
            IGameClock gameClock,
            GameWorldFrameBuffer frameBuffer,
            CombatEffectExecutor combatEffectExecutor,
            ActorDefeatOrchestrator actorDefeatOrchestrator,
            IEventPublisher eventPublisher,
            ActorSpatialIndexService actorSpatialIndexService,
            ActorViewDataStore actorViewDataStore)
        {
            this.actorCombatService = actorCombatService
                ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.gameClock = gameClock
                ?? throw new ArgumentNullException(nameof(gameClock));
            this.frameBuffer = frameBuffer
                ?? throw new ArgumentNullException(nameof(frameBuffer));
            this.combatEffectExecutor = combatEffectExecutor
                ?? throw new ArgumentNullException(nameof(combatEffectExecutor));
            this.actorDefeatOrchestrator = actorDefeatOrchestrator
                ?? throw new ArgumentNullException(nameof(actorDefeatOrchestrator));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.actorSpatialIndexService = actorSpatialIndexService
                ?? throw new ArgumentNullException(nameof(actorSpatialIndexService));
            this.actorViewDataStore = actorViewDataStore
                ?? throw new ArgumentNullException(nameof(actorViewDataStore));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var currentGameTimeSeconds = gameClock.ElapsedGameTimeSeconds;
            var actors = frameBuffer.CopyActors(worldState.Actors);
            var bufferedEventPublisher = new BufferedEventPublisher(eventPublisher);

            foreach (var actor in actors)
            {
                if (actor.Hp <= 0 || worldState.FindActor(actor.Id) == null)
                {
                    continue;
                }

                var combatState = actorCombatService.GetOrCreateCombatState(actor.Id);
                if (!combatState.TargetActorId.HasValue)
                {
                    continue;
                }

                var target = worldState.FindActor(combatState.TargetActorId.Value);
                if (target == null || target.Hp <= 0)
                {
                    actorCombatService.ClearTarget(actor.Id);
                    continue;
                }

                if (!IsWithinWeaponRange(actor, target))
                {
                    MoveTowardTarget(actor, target, deltaGameSeconds);

                    continue;
                }

                if (!combatState.IsAttackReady(currentGameTimeSeconds))
                {
                    continue;
                }

                var targetDefeated = combatEffectExecutor.ExecuteAttack(
                    worldState,
                    actor,
                    target,
                    actor.WeaponCombatParams.AttackSpec,
                    bufferedEventPublisher);
                if (targetDefeated && worldState.FindActor(target.Id) != null)
                {
                    actorDefeatOrchestrator.Execute(worldState, actor, target, bufferedEventPublisher);
                }

                combatState.RecordAttack(currentGameTimeSeconds, actor.WeaponCombatParams.AttackIntervalSeconds);
                actorCombatService.MarkCombatParticipation(actor.Id);
            }

            bufferedEventPublisher.Flush();
            return UniTask.CompletedTask;
        }

        void MoveTowardTarget(Actor actor, Actor target, float deltaGameSeconds)
        {
            if (!actor.Position.LayerId.Equals(target.Position.LayerId))
            {
                return;
            }

            var dx = target.Position.X - actor.Position.X;
            var dz = target.Position.Z - actor.Position.Z;
            var distSq = dx * dx + dz * dz;
            if (distSq <= 0f)
            {
                return;
            }

            var dist = (float)Math.Sqrt(distSq);
            var step = Math.Min(dist, GameConstants.ActorMoveSpeedMetersPerSecond * deltaGameSeconds);
            var ratio = step / dist;
            actor.MoveTo(new LayerPosition(
                actor.Position.LayerId,
                actor.Position.X + dx * ratio,
                actor.Position.Z + dz * ratio));
            actorSpatialIndexService.SyncActor(actor);
            actorViewDataStore.SyncActor(actor);
        }

        static bool IsWithinWeaponRange(Actor actor, Actor target)
        {
            if (!actor.Position.LayerId.Equals(target.Position.LayerId))
            {
                return false;
            }

            var range = actor.WeaponCombatParams.RangeMeters;
            return actor.Position.DistanceSquaredTo(target.Position) <= range * range;
        }
    }
}
